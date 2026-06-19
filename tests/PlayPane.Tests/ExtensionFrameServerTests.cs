using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using PlayPane.Core.Capture;

namespace PlayPane.Tests
{
    internal static class ExtensionFrameServerTests
    {
        public static void ReceivesBinaryFrameOverWebSocket()
        {
            int port = GetFreeTcpPort();
            var store = new ExtensionFrameStore();
            using (var server = new ExtensionFrameServer(store, port))
            using (var socket = new ClientWebSocket())
            {
                server.StartAsync().GetAwaiter().GetResult();
                socket.ConnectAsync(server.WebSocketUri, CancellationToken.None).GetAwaiter().GetResult();

                byte[] bytes = ExtensionFrameStoreTests.CreateImageBytes(12, 8);
                socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None).GetAwaiter().GetResult();

                WaitUntil(delegate { return store.HasFrame; }, TimeSpan.FromSeconds(3));
                TestAssert.True(store.HasFrame, "Server should store a frame received over WebSocket.");

                server.StopAsync().GetAwaiter().GetResult();
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

        private static void WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return;
                }

                Thread.Sleep(25);
            }
        }
    }
}
