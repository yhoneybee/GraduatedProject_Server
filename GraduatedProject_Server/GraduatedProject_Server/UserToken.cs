using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class UserToken
    {
        SocketAsyncEventArgs? receiveEventArgs;
        MessageResolver? messageResolver;
        public Socket? socket;

        List<Packet> packetList = new List<Packet>(5);
        object mutexPacketList = new object();

        SocketAsyncEventArgs? sendEventArgs;

        Queue<Packet> sendPacketQueue = new Queue<Packet>(100);
        object mutexSendPacketQueue = new object();

        public User? user;

        Timer? timer;

        public UserToken()
        {
            messageResolver = new MessageResolver();
        }

        public void Init()
        {
            receiveEventArgs = SocketAsyncEventArgsPool.Instance.Pop();
            receiveEventArgs.Completed += OnReceiveCompleted;
            receiveEventArgs.UserToken = this;


            sendEventArgs = SocketAsyncEventArgsPool.Instance.Pop();
            sendEventArgs.Completed += OnSendCompleted;
            sendEventArgs.UserToken = this;


            BufferManager.Instance.SetBuffer(receiveEventArgs);
            BufferManager.Instance.SetBuffer(sendEventArgs);

            timer = new Timer(Update, null, 0, 5);
        }

        public void Update(object? state)
        {
            if (packetList.Count > 0)
            {
                lock (mutexPacketList)
                {
                    try
                    {
                        foreach (Packet packet in packetList)
                            user!.ProcessPacket(packet);
                        packetList.Clear();
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }

        public void AddPacket(Packet packet)
        {
            lock (mutexPacketList)
            {
                packetList.Add(packet);
            }
        }

        public void StartReceive()
        {
            bool pending = socket!.ReceiveAsync(receiveEventArgs!);
            if (!pending)
                OnReceiveCompleted(this, receiveEventArgs!);
        }

        public void Send(Packet packet)
        {
            if (socket == null) return;

            lock (mutexSendPacketQueue)
            {
                if (sendPacketQueue.Count < 1)
                {
                    sendPacketQueue.Enqueue(packet);
                    SendProcess();
                    return;
                }

                if (sendPacketQueue.Count < 100)
                    sendPacketQueue.Enqueue(packet);
            }
        }

        private void SendProcess()
        {
            if (socket == null)
                return;

            Packet packet = sendPacketQueue.Peek();
            byte[] send_data = packet.GetSendBytes();

            int data_len = send_data.Length;

            if (data_len > Defines.SOCKET_BUFFER_SIZE)
            {
                SocketAsyncEventArgs send_event_args = SocketAsyncEventArgsPool.Instance.Pop();
                send_event_args.Completed += OnSendCompletedPooling;
                send_event_args.UserToken = this;
                send_event_args.SetBuffer(send_data, 0, send_data.Length);

                bool pending = socket.SendAsync(send_event_args);
                if (!pending)
                    OnSendCompletedPooling(null, send_event_args);
            }
            else
            {
                sendEventArgs!.SetBuffer(sendEventArgs.Offset, send_data.Length);
                Array.Copy(send_data, 0, sendEventArgs.Buffer!, sendEventArgs.Offset, send_data.Length);

                bool pending = socket.SendAsync(sendEventArgs);
                if (!pending)
                    OnSendCompleted(null, sendEventArgs);
            }
        }

        void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                lock (mutexSendPacketQueue)
                {
                    if (sendPacketQueue.Count > 0)
                        sendPacketQueue.Dequeue();

                    if (sendPacketQueue.Count > 0)
                        SendProcess();
                }
            }
            else
            {

            }
        }

        void OnSendCompletedPooling(object? sender, SocketAsyncEventArgs e)
        {
            if (e.BufferList != null)
            {
                e.BufferList = null;
            }
            e.SetBuffer(null, 0, 0);
            e.UserToken = null;
            e.RemoteEndPoint = null;

            e.Completed -= OnSendCompletedPooling;
            SocketAsyncEventArgsPool.Instance.Push(e);

            if (e.SocketError == SocketError.Success)
            {
                lock (mutexSendPacketQueue)
                {
                    if (sendPacketQueue.Count > 0)
                        sendPacketQueue.Dequeue();

                    if (sendPacketQueue.Count > 0)
                        SendProcess();
                }
            }
            else
            {

            }
        }

        void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                messageResolver!.OnReceive(e.Buffer!, e.Offset, e.BytesTransferred, OnMessageCompleted);

                StartReceive();
            }
            else
            {

            }
        }

        void OnMessageCompleted(Packet packet)
        {
            AddPacket(packet);
        }

        public void Close()
        {
            try
            {
                if (socket != null)
                    socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (socket != null)
                    socket.Close();
            }

            socket = null;
            user = null;
            messageResolver!.ClearBuffer();

            lock (mutexPacketList)
            {
                packetList.Clear();
            }

            lock (mutexSendPacketQueue)
            {
                sendPacketQueue.Clear();
            }

            BufferManager.Instance.FreeBuffer(receiveEventArgs!);
            receiveEventArgs!.SetBuffer(null, 0, 0);
            if (receiveEventArgs.BufferList != null)
                receiveEventArgs.BufferList = null;

            receiveEventArgs.UserToken = null;
            receiveEventArgs.RemoteEndPoint = null;

            receiveEventArgs.Completed -= OnReceiveCompleted;

            SocketAsyncEventArgsPool.Instance.Push(receiveEventArgs);

            receiveEventArgs = null;
            BufferManager.Instance.FreeBuffer(sendEventArgs!);
            sendEventArgs!.SetBuffer(null, 0, 0);
            if (sendEventArgs!.BufferList != null)
                sendEventArgs.BufferList = null;

            sendEventArgs.UserToken = null;
            sendEventArgs.RemoteEndPoint = null;

            sendEventArgs.Completed -= OnSendCompleted;

            SocketAsyncEventArgsPool.Instance.Push(sendEventArgs);

            sendEventArgs = null;

            timer!.Dispose();
        }
    }
}
