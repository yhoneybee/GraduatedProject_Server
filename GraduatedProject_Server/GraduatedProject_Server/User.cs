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
                    REQ_CreateRoom(packet);
                    break;
                case PacketType.REQ_ENTER_ROOM_PACKET:
                    REQ_EnterRoom(packet);
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

        private void REQ_EnterRoom(Packet packet)
        {
            Console.Write("REQ_EnterRoom : ");

            var req = packet.GetPacket<REQ_CreateEnterRoom>();

            RES_EnterRoom res = new RES_EnterRoom();
            res.roomInfo.name = req.roomName;

            var where = K.Rooms!.Where(x => x.name == req.roomName);

            res.completed = false;
            res.reason = "존재하지 않는 방";

            if (where.Any())
            {
                MySqlDataReader reader;
                if (K.SQL.Select(new Query().Select("*", "roominfo", $"name = '{req.roomName}'"), out reader!))
                {
                    reader!.Read();
                    if (reader["player1"] != null)
                        res.roomInfo.player1 = reader["player1"].ToString();
                    if (reader["player2"] != null)
                        res.roomInfo.player2 = reader["player2"].ToString();
                }

                K.SQL.SelectEnd(ref reader!);

                string updateColumnName = string.Empty;

                if (res.roomInfo.player1 == string.Empty)
                {
                    updateColumnName = "player1";
                    res.roomInfo.player1 = userInfo.id;
                }
                else if (res.roomInfo.player2 == string.Empty)
                {
                    updateColumnName = "player2";
                    res.roomInfo.player2 = userInfo.id;
                }

                if (updateColumnName == string.Empty)
                    res.reason = "방 인원이 다 참";

                if (K.SQL.Query(new Query().Update("roominfo", $"{updateColumnName} = '{res.roomInfo.player1}'", $"name = '{res.roomInfo.name}'")))
                {
                    res.completed = true;
                    res.reason = "방 입장 성공";
                }
                else
                {
                    res.reason += ", UPDATE 실패";
                }
            }

            packet.SetData(PacketType.RES_ENTER_ROOM_PACKET, Data<RES_EnterRoom>.Serialize(res));
            token!.Send(packet);

            Console.WriteLine($"{req.roomName}/{res.reason}");
        }

        private void REQ_CreateRoom(Packet packet)
        {
            Console.Write("REQ_CreateRoom : ");

            var req = packet.GetPacket<REQ_CreateEnterRoom>();

            RES_CreateRoom res = new RES_CreateRoom();

            var where = K.Rooms!.Where(x => x.name == req.roomName);

            res.completed = false;
            res.reason = "존재하는 Room이름임";
            res.roomName = req.roomName;

            if (!where.Any())
            {
                res.reason = "INSERT 실패";

                if (K.SQL!.Query(new Query().Insert("roominfo", "name", $"'{req.roomName}'")))
                {
                    res.completed = true;
                    res.reason = "방 생성 성공";
                    K.Rooms.Add(new RoomInfo { name = res.roomName, player1 = string.Empty, player2 = string.Empty });
                }
            }

            packet.SetData(PacketType.RES_CREATE_ROOM_PACKET, Data<RES_CreateRoom>.Serialize(res));
            token!.Send(packet);

            Console.WriteLine($"{req.roomName}/{res.reason}");
        }

        private void REQ_Login(Packet packet)
        {
            Console.Write("REQ_Login : ");

            var req = packet.GetPacket<REQ_Login>();

            RES res = new RES();

            MySqlDataReader reader;

            res.completed = true;
            res.reason = "로그인 성공";

            if (!K.SQL!.Select(new Query().Select("id", "useraccount", $"id = '{req.id}' AND pw = sha2('{req.pw}', 256)"), out reader!))
            {
                res.completed = false;
                res.reason = "로그인 정보에 해당하는 유저가 존재하지 않음";
            }

            K.SQL!.SelectEnd(ref reader!);

            userInfo.id = req.id;
            if (K.SQL.Select(new Query().Select("*", "userinfo", $"id = '{userInfo.id}'"), out reader!))
            {
                reader!.Read();
                userInfo.win = (ushort)reader["win"];
                userInfo.lose = (ushort)reader["lose"];
            }
            K.SQL!.SelectEnd(ref reader!);

            packet.SetData(PacketType.RES_LOGIN_PACKET, Data<RES>.Serialize(res));
            token!.Send(packet);

            Console.WriteLine($"{req.id}/{res.reason}");
        }

        private void REQ_Signin(Packet packet)
        {
            Console.Write("REQ_Signin");

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

            if (K.SQL!.Query(new Query().Insert("useraccount", $"'{req.id}', sha2('{req.pw}',256)")))
            {
                res.completed = true;
                res.reason = "회원가입 성공";

                K.SQL!.Query(new Query().Insert("userinfo", $"'{req.id}',0,0,0"));
            }

            packet.SetData(PacketType.RES_SIGNIN_PACKET, Data<RES>.Serialize(res));
            token!.Send(packet);

            Console.WriteLine($"/{res.reason}");
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
                K.Users.ForEach(user =>
                {
                    user.token!.Send(packet);
                });
            }
            else
            {
                packet.type = ((short)PacketType.RES_CHAT_PACKET);
                token!.Send(packet);

                var where = K.Users.Where(user => user.userInfo.id == chat.id);
                if (!where.Any()) return;
                K.Users.Where(user => user.userInfo.id == chat.id).Select(x => x).FirstOrDefault()!.token!.Send(packet);
            }
        }

        public void Init(UserToken token)
        {
            this.token = token;
        }
    }
}
