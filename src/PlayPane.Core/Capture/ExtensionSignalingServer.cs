using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace PlayPane.Core.Capture
{
    public sealed class ExtensionSignalingServer : IDisposable
    {
        public const int DefaultPort = 17632;
        public const string WebSocketPath = "/playpane";

        private const string SourceRole = "source";
        private const string ViewerRole = "viewer";
        private const int MaxPendingSignalsPerRole = 64;

        private readonly object _syncRoot = new object();
        private readonly int _port;
        private readonly Queue<string> _pendingForSource = new Queue<string>();
        private readonly Queue<string> _pendingForViewer = new Queue<string>();
        private WebApplication _app;
        private CancellationTokenSource _stopTokenSource;
        private WebSocket _sourceSocket;
        private WebSocket _viewerSocket;

        public ExtensionSignalingServer()
            : this(DefaultPort)
        {
        }

        public ExtensionSignalingServer(int port)
        {
            _port = port;
        }

        public event EventHandler ClientConnected;

        public event EventHandler ClientDisconnected;

        public bool IsRunning
        {
            get { return _app != null; }
        }

        public Uri WebSocketUri
        {
            get { return new Uri("ws://127.0.0.1:" + _port + WebSocketPath); }
        }

        public async Task StartAsync()
        {
            if (_app != null)
            {
                return;
            }

            _stopTokenSource = new CancellationTokenSource();
            var builder = WebApplication.CreateSlimBuilder(Array.Empty<string>());
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.None);
            builder.WebHost.UseUrls("http://127.0.0.1:" + _port);

            WebApplication app = builder.Build();
            app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });
            app.MapGet("/status", () => Results.Text("PlayPane WebRTC signaling server ready"));
            app.Map(WebSocketPath, HandleWebSocketAsync);

            await app.StartAsync(_stopTokenSource.Token).ConfigureAwait(false);
            _app = app;
        }

        public async Task StopAsync()
        {
            WebApplication app = _app;
            if (app == null)
            {
                return;
            }

            _app = null;
            try
            {
                _stopTokenSource.Cancel();
                ClearClients();
                using (var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    await app.StopAsync(shutdown.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                await app.DisposeAsync().ConfigureAwait(false);
                _stopTokenSource.Dispose();
                _stopTokenSource = null;
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task HandleWebSocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Expected a WebSocket request.").ConfigureAwait(false);
                return;
            }

            using (WebSocket socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
            {
                string role = null;
                OnClientConnected();
                try
                {
                    await ReceiveSignalsAsync(socket, delegate(string receivedRole) { role = receivedRole; }, _stopTokenSource.Token).ConfigureAwait(false);
                }
                finally
                {
                    UnregisterClient(role, socket);
                    OnClientDisconnected();
                }
            }
        }

        private async Task ReceiveSignalsAsync(WebSocket socket, Action<string> setRole, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                string message = await ReceiveTextAsync(socket, cancellationToken).ConfigureAwait(false);
                if (message == null)
                {
                    return;
                }

                string role = TryReadString(message, "role");
                if (!IsKnownRole(role))
                {
                    continue;
                }

                setRole(role);
                RegisterClient(role, socket);
                await FlushPendingAsync(role, socket, cancellationToken).ConfigureAwait(false);

                if (TryReadString(message, "type") == "hello")
                {
                    continue;
                }

                await RouteSignalAsync(role, message, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[16 * 1024];
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken).ConfigureAwait(false);
                        return null;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        return null;
                    }

                    if (result.Count > 0)
                    {
                        stream.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private async Task RouteSignalAsync(string sourceRole, string message, CancellationToken cancellationToken)
        {
            string targetRole = sourceRole == SourceRole ? ViewerRole : SourceRole;
            WebSocket targetSocket;
            lock (_syncRoot)
            {
                targetSocket = targetRole == SourceRole ? _sourceSocket : _viewerSocket;
                if (targetSocket == null || targetSocket.State != WebSocketState.Open)
                {
                    EnqueuePendingSignal(targetRole, message);
                    return;
                }
            }

            try
            {
                await SendTextAsync(targetSocket, message, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                lock (_syncRoot)
                {
                    EnqueuePendingSignal(targetRole, message);
                }
            }
        }

        private async Task FlushPendingAsync(string role, WebSocket socket, CancellationToken cancellationToken)
        {
            while (true)
            {
                string message;
                lock (_syncRoot)
                {
                    if ((role == SourceRole && _sourceSocket != socket) || (role == ViewerRole && _viewerSocket != socket))
                    {
                        return;
                    }

                    Queue<string> queue = role == SourceRole ? _pendingForSource : _pendingForViewer;
                    if (queue.Count == 0)
                    {
                        return;
                    }

                    message = queue.Dequeue();
                }

                await SendTextAsync(socket, message, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task SendTextAsync(WebSocket socket, string message, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

        private void RegisterClient(string role, WebSocket socket)
        {
            lock (_syncRoot)
            {
                if (role == SourceRole)
                {
                    _sourceSocket = socket;
                }
                else if (role == ViewerRole)
                {
                    _viewerSocket = socket;
                }
            }
        }

        private void UnregisterClient(string role, WebSocket socket)
        {
            lock (_syncRoot)
            {
                if (role == SourceRole && _sourceSocket == socket)
                {
                    _sourceSocket = null;
                }
                else if (role == ViewerRole && _viewerSocket == socket)
                {
                    _viewerSocket = null;
                }
            }
        }

        private void ClearClients()
        {
            lock (_syncRoot)
            {
                _sourceSocket = null;
                _viewerSocket = null;
                _pendingForSource.Clear();
                _pendingForViewer.Clear();
            }
        }

        private void EnqueuePendingSignal(string targetRole, string message)
        {
            Queue<string> queue = targetRole == SourceRole ? _pendingForSource : _pendingForViewer;
            while (queue.Count >= MaxPendingSignalsPerRole)
            {
                queue.Dequeue();
            }

            queue.Enqueue(message);
        }

        private static bool IsKnownRole(string role)
        {
            return role == SourceRole || role == ViewerRole;
        }

        private static string TryReadString(string json, string propertyName)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement value;
                    if (document.RootElement.ValueKind == JsonValueKind.Object &&
                        document.RootElement.TryGetProperty(propertyName, out value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private void OnClientConnected()
        {
            EventHandler handler = ClientConnected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnClientDisconnected()
        {
            EventHandler handler = ClientDisconnected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
