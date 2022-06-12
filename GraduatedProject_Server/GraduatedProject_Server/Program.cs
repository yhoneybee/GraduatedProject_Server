using System.Net;
using System.Net.Sockets;
using MyPacket;

namespace GraduatedProject_Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            Listener listener = new Listener();
            listener.Start(6475, 100);
        }
    }
}
