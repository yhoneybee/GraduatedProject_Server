using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class Listener
    {
        SocketAsyncEventArgs? acceptEventArgs;
        Socket? listenSocket;
        AutoResetEvent? flowControlEvent;
        bool ThreadLive { get; set; }

        public delegate void NewClientHandler(Socket clientSocket, object token);
        public NewClientHandler? onNewClient;

        public Listener()
        {
            onNewClient = null;
            ThreadLive = true;
        }

        public void Start(short port, short backlog)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.NoDelay = true;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

            try
            {
                listenSocket.Bind(endPoint);
                listenSocket.Listen(backlog);

                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

                Thread listenThread = new Thread(DoListen);
                listenThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        void DoListen()
        {
            flowControlEvent = new AutoResetEvent(false);

            while (ThreadLive)
            {
                acceptEventArgs!.AcceptSocket = null;
                bool pending = true;

                try
                {
                    pending = listenSocket!.AcceptAsync(acceptEventArgs);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                if (!pending)
                {
                    OnAcceptCompleted(null, acceptEventArgs);
                }

                flowControlEvent.WaitOne();
            }
        }

        void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket client = e.AcceptSocket!;

                UserToken token = new();
                token.Init();

                User user = new();
                user.Init(token);

                token.user = user;
                user.token!.socket = client;

                user.token.socket.NoDelay = true;
                user.token.socket.ReceiveTimeout = 60 * 1000;
                user.token.socket.SendTimeout = 60 * 1000;

                user.token.StartReceive();

                UserManager.Instance.Users!.Add(user);
            }
            else
            {

            }

            flowControlEvent?.Set();
        }


        public void Close()
        {
            listenSocket?.Close();
        }
    }
}
