using MyPacket;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class User
    {
        public UserInfo userInfo;
        public string? to;

        public UserToken? token;

        public void ProcessPacket(Packet packet)
        {
            switch ((PacketType)packet.type)
            {
                case PacketType.CONNECTED:
                    Connected(packet);
                    break;
                case PacketType.REQ_SIGNIN_PACKET:
                    REQ_Signin(packet);
                    break;
                case PacketType.REQ_LOGIN_PACKET:
                    REQ_Login(packet);
                    break;
                case PacketType.REQ_CREATE_ROOM_PACKET:
                    break;
                case PacketType.REQ_ENTER_ROOM_PACKET:
                    break;
                case PacketType.REQ_LEAVE_ROOM_PACKET:
                    break;
                case PacketType.REQ_ROOMS_PACKET:
                    break;
                case PacketType.REQ_USER_PACKET:
                    break;
                case PacketType.REQ_READY_GAME_PACKET:
                    break;
                case PacketType.REQ_START_GAME_PACKET:
                    break;
                case PacketType.REQ_SET_WIN_PACKET:
                    break;
                case PacketType.REQ_SET_LOSE_PACKET:
                    break;
                case PacketType.REQ_CHAT_PACKET:
                    REQ_Chat(packet);
                    break;
                case PacketType.REQ_CHARACTOR_PACKET:
                    break;
                case PacketType.REQ_LOGOUT_PACKET:
                    break;
                case PacketType.DISCONNECTED:
                    Disconnected(packet);
                    break;
                case PacketType.END:
                    break;
            }
        }

        private void REQ_Login(Packet packet)
        {
            Console.Write("REQ_Login : ");

            var req = packet.GetPacket<REQ_Login>();

            RES res = new RES();

            MySqlDataReader reader;
            GameManager.Instance.SQL!.Select(new Query().Select("id", "useraccount", $"id = '{req.id}' AND pw = sha2('{req.pw}', 256)"), out reader!);

            res.completed = true;
            res.reason = "로그인 성공";

            if (!reader.HasRows)
            {
                res.completed = false;
                res.reason = "로그인 정보에 해당하는 유저가 존재하지 않음";
            }

            GameManager.Instance.SQL!.SelectEnd(ref reader!);

            packet.SetData(PacketType.RES_LOGIN_PACKET, Data<RES>.Serialize(res));
            token!.Send(packet);

            Console.WriteLine($"{req.id}");
        }

        private void REQ_Signin(Packet packet)
        {
            Console.WriteLine("REQ_Signin");

            var req = packet.GetPacket<REQ_Signin>();

            RES res = new RES();

            if (req.pw != req.pwAgain)
            {
                res.completed = false;
                res.reason = "비밀번호와 확인비밀번호가 일치하지 않음";

                packet.SetData(PacketType.RES_SIGNIN_PACKET, Data<RES>.Serialize(res));
                token!.Send(packet);
                return;
            }

            res.completed = false;
            res.reason = "INSERT 실패";

            if (GameManager.Instance.SQL!.Query(new Query().Insert("useraccount", $"'{req.id}', sha2('{req.pw}',256)")))
            {
                res.completed = true;
                res.reason = "회원가입 성공";

                GameManager.Instance.SQL!.Query(new Query().Insert("userinfo", $"'{req.id}',0,0,0"));
            }

            packet.SetData(PacketType.RES_SIGNIN_PACKET, Data<RES>.Serialize(res));
            token!.Send(packet);
        }

        private void Disconnected(Packet packet)
        {
            Console.WriteLine($"DISCONNECTED");

            RES res = new RES();
            res.completed = true;
            res.reason = "Disonnected";

            packet.SetData(PacketType.CONNECTED, Data<RES>.Serialize(res));
            token!.Send(packet);
        }

        private void Connected(Packet packet)
        {
            Console.WriteLine($"CONNECTED");

            RES res = new RES();
            res.completed = true;
            res.reason = "Connected";

            packet.SetData(PacketType.CONNECTED, Data<RES>.Serialize(res));
            token!.Send(packet);
        }

        private void REQ_Chat(Packet packet)
        {
            REQ_RES_Chat chat = packet.GetPacket<REQ_RES_Chat>();
            if (chat.to == "ALL")
            {
                GameManager.Instance.Users!.ForEach(user =>
                {
                    user.token!.Send(packet);
                });
            }
            else
            {
                packet.type = ((short)PacketType.RES_CHAT_PACKET);
                token!.Send(packet);

                var where = GameManager.Instance.Users!.Where(user => user.userInfo.id == chat.id);
                if (!where.Any()) return;
                GameManager.Instance.Users!.Where(user => user.userInfo.id == chat.id).Select(x => x).FirstOrDefault()!.token!.Send(packet);
            }
        }

        public void Init(UserToken token)
        {
            this.token = token;
        }
    }
}
