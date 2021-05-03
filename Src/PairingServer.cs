using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BluetoothDevicePairing
{
    class PairingServer
    {
        private const int SERVICE_PORT = 11000;
        private const int BUF_SIZE = 4 * 1024;

        private readonly Socket ServerSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public delegate void MessageHandler(PairingServer pairingServer, Socket socket, EndPoint epFrom, string newMessage);
        public event MessageHandler NewMessageReceived;

        public PairingServer()
        {
        }

        public void Start()
        {
            IPEndPoint localEndPoint = new(IPAddress.Parse("127.0.0.1"), SERVICE_PORT);

            try
            {
                ServerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                ServerSocket.Bind(localEndPoint);
                Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue");
            Console.ReadKey();
        }

        public void SendTo(EndPoint epTo, uint value)
        {
            try { 
                State BufferState = new();
                byte[] data = Bit​Converter.GetBytes(value);
                ServerSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epTo, (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = ServerSocket.EndSendTo(ar);
                    Console.WriteLine("SEND: {0}, {1}", bytes, value);
                }, BufferState);
            } catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
        }

        public void SendTo(EndPoint epTo, string text)
        {
            State BufferState = new();
            byte[] data = Encoding.ASCII.GetBytes(text);
            ServerSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epTo, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = ServerSocket.EndSendTo(ar);
                Console.WriteLine("SEND: {0}, {1}", bytes, text);
            }, BufferState);
        }

        private void Receive()
        {
            State BufferState = new();
            AsyncCallback InteralSockCallback = null;
            EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);

            ServerSocket.BeginReceiveFrom(BufferState.buffer, 0, BUF_SIZE, SocketFlags.None, ref epFrom, InteralSockCallback = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = ServerSocket.EndReceiveFrom(ar, ref epFrom);
                ServerSocket.BeginReceiveFrom(so.buffer, 0, BUF_SIZE, SocketFlags.None, ref epFrom, InteralSockCallback, so);

                string receivedMessage = Encoding.ASCII.GetString(so.buffer, 0, bytes);
                Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, receivedMessage);
                NewMessageReceived?.Invoke(this, ServerSocket, epFrom, receivedMessage);
            }, BufferState);
        }

        // BufferState
        public class State
        {
            public byte[] buffer = new byte[BUF_SIZE];
        }
    }
}
