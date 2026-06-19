using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using PlayPane.Core.Capture;

namespace PlayPane.Tests
{
    internal static class ExtensionSignalingServerTests
    {
        public static void RoutesOfferFromSourceToViewer()
        {
            int port = GetFreeTcpPort();
            using (var server = new ExtensionSignalingServer(port))
            using (var source = new ClientWebSocket())
            using (var viewer = new ClientWebSocket())
            {
                server.StartAsync().GetAwaiter().GetResult();
                source.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();
                viewer.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();

                SendText(source, "{\"role\":\"source\",\"type\":\"hello\"}");
                SendText(viewer, "{\"role\":\"viewer\",\"type\":\"hello\"}");

                string offer = "{\"role\":\"source\",\"type\":\"offer\",\"sdp\":\"offer-sdp\"}";
                SendText(source, offer);

                string received = ReceiveText(viewer, TimeSpan.FromSeconds(3));

                TestAssert.Equal(offer, received, "Viewer should receive source offer unchanged.");
                server.StopAsync().GetAwaiter().GetResult();
            }
        }

        public static void QueuesOfferUntilViewerConnects()
        {
            int port = GetFreeTcpPort();
            using (var server = new ExtensionSignalingServer(port))
            using (var source = new ClientWebSocket())
            using (var viewer = new ClientWebSocket())
            {
                server.StartAsync().GetAwaiter().GetResult();
                source.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();

                SendText(source, "{\"role\":\"source\",\"type\":\"hello\"}");
                string offer = "{\"role\":\"source\",\"type\":\"offer\",\"sdp\":\"queued-offer\"}";
                SendText(source, offer);

                viewer.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();
                SendText(viewer, "{\"role\":\"viewer\",\"type\":\"hello\"}");

                string received = ReceiveText(viewer, TimeSpan.FromSeconds(3));

                TestAssert.Equal(offer, received, "Viewer should receive a queued source offer when it connects.");
                server.StopAsync().GetAwaiter().GetResult();
            }
        }

        public static void RoutesViewerReadyFromViewerToSource()
        {
            int port = GetFreeTcpPort();
            using (var server = new ExtensionSignalingServer(port))
            using (var source = new ClientWebSocket())
            using (var viewer = new ClientWebSocket())
            {
                server.StartAsync().GetAwaiter().GetResult();
                source.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();
                viewer.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();

                SendText(source, "{\"role\":\"source\",\"type\":\"hello\"}");
                SendText(viewer, "{\"role\":\"viewer\",\"type\":\"hello\"}");

                string ready = "{\"role\":\"viewer\",\"type\":\"viewer-ready\"}";
                SendText(viewer, ready);

                string received = ReceiveText(source, TimeSpan.FromSeconds(3));

                TestAssert.Equal(ready, received, "Source should receive viewer-ready unchanged.");
                server.StopAsync().GetAwaiter().GetResult();
            }
        }

        public static void QueuesViewerReadyUntilSourceConnects()
        {
            int port = GetFreeTcpPort();
            using (var server = new ExtensionSignalingServer(port))
            using (var source = new ClientWebSocket())
            using (var viewer = new ClientWebSocket())
            {
                server.StartAsync().GetAwaiter().GetResult();
                viewer.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();

                SendText(viewer, "{\"role\":\"viewer\",\"type\":\"hello\"}");
                string ready = "{\"role\":\"viewer\",\"type\":\"viewer-ready\"}";
                SendText(viewer, ready);

                source.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();
                SendText(source, "{\"role\":\"source\",\"type\":\"hello\"}");

                string received = ReceiveText(source, TimeSpan.FromSeconds(3));

                TestAssert.Equal(ready, received, "Source should receive queued viewer-ready when it connects.");
                server.StopAsync().GetAwaiter().GetResult();
            }
        }

        private static void SendText(ClientWebSocket socket, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static string ReceiveText(ClientWebSocket socket, TimeSpan timeout)
        {
            var buffer = new byte[8192];
            using (var timeoutSource = new CancellationTokenSource(timeout))
            using (var message = new System.IO.MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutSource.Token).GetAwaiter().GetResult();
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new InvalidOperationException("Socket closed before a text message was received.");
                    }

                    if (result.Count > 0)
                    {
                        message.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(message.ToArray());
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
