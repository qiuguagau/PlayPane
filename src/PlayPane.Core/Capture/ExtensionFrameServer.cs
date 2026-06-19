using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace PlayPane.Core.Capture
{
    public sealed class ExtensionFrameServer : IDisposable
    {
        public const int DefaultPort = 17632;
        public const string WebSocketPath = "/playpane";

        private readonly ExtensionFrameStore _frameStore;
        private readonly int _port;
        private WebApplication _app;
        private CancellationTokenSource _stopTokenSource;

        public ExtensionFrameServer(ExtensionFrameStore frameStore)
            : this(frameStore, DefaultPort)
        {
        }

        public ExtensionFrameServer(ExtensionFrameStore frameStore, int port)
        {
            _frameStore = frameStore;
            _port = port;
        }

        public event EventHandler ClientConnected;

        public event EventHandler ClientDisconnected;

        public event EventHandler<ExtensionFrameReceivedEventArgs> FrameReceived;

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
            app.MapGet("/status", () => Results.Text("PlayPane extension capture server ready"));
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
                OnClientConnected();
                try
                {
                    await ReceiveFramesAsync(socket, _stopTokenSource.Token).ConfigureAwait(false);
                }
                finally
                {
                    OnClientDisconnected();
                }
            }
        }

        private async Task ReceiveFramesAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];

            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using (var stream = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        if (result.Count > 0)
                        {
                            stream.Write(buffer, 0, result.Count);
                        }
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Binary && stream.Length > 0)
                    {
                        byte[] bytes = stream.ToArray();
                        _frameStore.Update(bytes);
                        OnFrameReceived(bytes.Length);
                    }
                }
            }
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

        private void OnFrameReceived(int byteCount)
        {
            EventHandler<ExtensionFrameReceivedEventArgs> handler = FrameReceived;
            if (handler != null)
            {
                handler(this, new ExtensionFrameReceivedEventArgs(byteCount));
            }
        }
    }
}
