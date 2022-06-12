using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class User
    {
        public string? id;
        public string? to;

        public UserToken? token;

        public void ProcessPacket(Packet packet)
        {
            switch ((PacketType)packet.type)
            {
                case PacketType.CHAT_PACKET:
                    ChatPacket(packet);
                    break;
                case PacketType.END:
                    break;
            }
        }

        private void ChatPacket(Packet packet)
        {
            ChatPacket chat = packet.GetPacket<ChatPacket>();
            switch (chat.chatType)
            {
                case ChatType.ALL:
                    UserManager.Instance.Users!.ForEach(user => 
                    {
                        user.token!.Send(packet);
                    });
                    break;
                case ChatType.PERSON:
                    break;
                case ChatType.END:
                    break;
            }
        }

        public void Init(UserToken token)
        {
            this.token = token;
        }
    }
}
