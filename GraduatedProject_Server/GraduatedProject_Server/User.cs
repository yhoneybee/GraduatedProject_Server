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
        public RoomInfo roomInfo;

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
                    REQ_LeaveRoom(packet);
                    break;
                case PacketType.REQ_ROOMS_PACKET:
                    REQ_Rooms(packet);
                    break;
                case PacketType.REQ_USER_PACKET:
                    break;
                case PacketType.REQ_READY_GAME_PACKET:
                    REQ_ReadyGame(packet);
                    break;
                case PacketType.REQ_START_GAME_PACKET:
                    REQ_StartGame(packet);
                    break;
                case PacketType.REQ_SET_WIN_PACKET:
                    break;
                case PacketType.REQ_SET_LOSE_PACKET:
                    break;
                case PacketType.REQ_CHAT_PACKET:
                    REQ_Chat(packet);
                    break;
                case PacketType.REQ_CHARACTOR_PACKET:
                    REQ_Charactor(packet);
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

        private void REQ_Charactor(Packet packet)
        {
            packet.type = ((short)PacketType.RES_CHARACTOR_PACKET);
            if (roomInfo.player1 == userInfo.id)
            {
                K.Users.Where(x => x.userInfo.id == roomInfo.player2).FirstOrDefault()!.token!.Send(packet);
            }
            else if (roomInfo.player2 == userInfo.id)
            {
                K.Users.Where(x => x.userInfo.id == roomInfo.player1).FirstOrDefault()!.token!.Send(packet);
            }
        }

        private void REQ_StartGame(Packet packet)
        {
            Console.Write("REQ_StartGame : ");

            User user1;
            User user2;
            RES res = new RES();

            Refresh(out user1, out user2);

            if (user1 != null && user2 != null)
            {
                if (user1!.roomInfo.player1Ready && user1!.roomInfo.player2Ready && user2!.roomInfo.player1Ready && user2!.roomInfo.player2Ready)
                {
                    res.completed = true;
                    res.reason = "모두 준비 중 게임 시작";

                    K.Send(user1!.token!, PacketType.RES_START_GAME_PACKET, res);
                    K.Send(user2!.token!, PacketType.RES_START_GAME_PACKET, res);
                }
                else
                {
                    res.completed = false;
                    res.reason = "모두 준비 중이 아님";
                }
            }

            Console.WriteLine($"{res.reason}");
        }

        private void REQ_Rooms(Packet packet)
        {
            Console.Write("REQ_Rooms : ");

            var req = packet.GetPacket<REQ_Rooms>();

            RES_Rooms res = new RES_Rooms();
            res.roomInfos = new RoomInfo[9];
            res.completed = true;
            res.reason = "방이 없음";

            List<RoomInfo> roomInfos = new List<RoomInfo>();

            MySqlDataReader reader;
            if (K.SQL.Select(new Query().Select("*", "roominfo"), out reader!))
            {
                res.reason = "방 목록 전송 성공";

                string? name;
                string? player1;
                string? player2;

                while (reader.Read())
                {
                    name = string.Empty;
                    if (reader["name"].ToString() != null) name = reader["name"].ToString();
                    player1 = string.Empty;
                    if (reader["player1"].ToString() != null) player1 = reader["player1"].ToString();
                    player2 = string.Empty;
                    if (reader["player2"].ToString() != null) player2 = reader["player2"].ToString();

                    roomInfos.Add(new RoomInfo
                    {
                        name = name,
                        player1 = player1,
                        player2 = player2,
                    });
                }

                if (req.startIndex < roomInfos.Count)
                {
                    for (int i = req.startIndex; i < roomInfos.Count; i++)
                        res.roomInfos[i] = roomInfos[i];
                }
            }
            K.SQL.SelectEnd(ref reader!);

            K.Send(token!, PacketType.RES_ROOMS_PACKET, res);

            Console.WriteLine($"{res.reason}/방 {res.roomInfos.Length}개 전송");
        }

        private void REQ_ReadyGame(Packet packet)
        {
            Console.Write("REQ_ReadyGame : ");

            var req = packet.GetPacket<REQ>();

            RES res = new RES();
            res.completed = false;
            res.reason = "방이 존재하지 않음";

            User user1 = K.Users.Where(x => x.userInfo.id == roomInfo.player1).FirstOrDefault()!;
            User user2 = K.Users.Where(x => x.userInfo.id == roomInfo.player2).FirstOrDefault()!;

            if (K.Rooms.Where(x => x.name == roomInfo.name).Any())
            {
                res.completed = true;

                MySqlDataReader reader;
                if (K.SQL.Select(new Query().Select("*", "roominfo", $"name = '{roomInfo.name}'"), out reader!))
                {
                    reader!.Read();
                    if (reader["player1"] != null)
                        roomInfo.player1 = reader["player1"].ToString();
                    if (reader["player2"] != null)
                        roomInfo.player2 = reader["player2"].ToString();
                }
                K.SQL.SelectEnd(ref reader!);

                if (roomInfo.player1 == userInfo.id)
                {
                    roomInfo.player1Ready = !roomInfo.player1Ready;

                    if (roomInfo.player2 != string.Empty)
                        Refresh(out user1, out user2);

                    res.reason = "준비";
                    if (!roomInfo.player1Ready)
                        res.reason = "준비 안됨";
                }
                else if (roomInfo.player2 == userInfo.id)
                {
                    roomInfo.player2Ready = !roomInfo.player2Ready;

                    if (roomInfo.player1 != string.Empty)
                        Refresh(out user1, out user2);

                    res.reason = "준비";
                    if (!roomInfo.player2Ready)
                        res.reason = "준비 안됨";
                }
            }

            K.Send(token!, PacketType.RES_READY_GAME_PACKET, res);

            Console.WriteLine($"{userInfo.id}/{res.reason}");
        }

        private void Refresh(out User user1, out User user2)
        {
            user1 = K.Users.Where(x => x.userInfo.id == roomInfo.player1).FirstOrDefault()!;
            user2 = K.Users.Where(x => x.userInfo.id == roomInfo.player2).FirstOrDefault()!;

            if (user1 != null && user2 != null)
            {
                user1.roomInfo.player2 = user2!.roomInfo.player2;
                user2.roomInfo.player1 = user1!.roomInfo.player1;

                user1.roomInfo.player2Ready = user2!.roomInfo.player2Ready;
                user2.roomInfo.player1Ready = user1!.roomInfo.player1Ready;
            }
        }

        private void REQ_LeaveRoom(Packet packet)
        {
            Console.Write($"REQ_LeaveRoom : ");

            var req = packet.GetPacket<REQ>();

            RES res = new RES();

            string updatePlayerColumnName = string.Empty;

            if (roomInfo.player1 == userInfo.id)
            {
                updatePlayerColumnName = "player1";
                roomInfo.player1 = string.Empty;
            }
            else if (roomInfo.player2 == userInfo.id)
            {
                updatePlayerColumnName = "player2";
                roomInfo.player2 = string.Empty;
            }

            if (K.SQL.Query(new Query().Update("roominfo", $"{updatePlayerColumnName} = NULL", $"name = '{roomInfo.name}'")))
            {
                res.completed = true;
                res.reason = "방 떠나기 성공";

                if (roomInfo.player1 == roomInfo.player2)
                {
                    if (K.SQL.Query(new Query().Delete("roominfo", $"name = '{roomInfo.name}'")))
                    {
                        res.completed = true;
                        res.reason += " 방 삭제 성공";
                    }
                    else
                    {
                        res.completed = false;
                        res.reason = "DELETE 실패";
                    }
                }
            }
            else
            {
                res.completed = false;
                res.reason = "UPDATE 실패";
            }

            K.Send(token!, PacketType.RES_LEAVE_ROOM_PACKET, res);

            Console.WriteLine($"{roomInfo.name}->{userInfo.id}/{res.reason}");
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
                res.roomInfo = where.FirstOrDefault();

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

                RES res1 = new RES();
                res1.completed = true;
                res1.reason = userInfo.id;

                if (res.roomInfo.player1 == string.Empty)
                {
                    updateColumnName = "player1";
                    res.roomInfo.player1 = userInfo.id;

                    if (res.roomInfo.player2 != string.Empty)
                    {
                        K.Send(K.Users.Where(x => x.userInfo.id == res.roomInfo.player2).FirstOrDefault()!.token!, PacketType.RES_OTHER_USER_ENTER_ROOM_PACKET, res1);
                    }
                }
                else if (res.roomInfo.player2 == string.Empty)
                {
                    updateColumnName = "player2";
                    res.roomInfo.player2 = userInfo.id;

                    if (res.roomInfo.player1 != string.Empty)
                    {
                        K.Send(K.Users.Where(x => x.userInfo.id == res.roomInfo.player1).FirstOrDefault()!.token!, PacketType.RES_OTHER_USER_ENTER_ROOM_PACKET, res1);
                    }
                }

                if (updateColumnName == string.Empty)
                    res.reason = "방 인원이 다 참";
                else
                {
                    if (K.SQL.Query(new Query().Update("roominfo", $"{updateColumnName} = '{userInfo.id}'", $"name = '{res.roomInfo.name}'")))
                    {
                        res.completed = true;
                        res.reason = "방 입장 성공";
                    }
                    else
                    {
                        res.reason += " UPDATE 실패";
                    }
                }
            }

            roomInfo.name = res.roomInfo.name;
            roomInfo.player1 = res.roomInfo.player1;
            roomInfo.player1Ready = res.roomInfo.player1Ready;
            roomInfo.player2 = res.roomInfo.player2;
            roomInfo.player2Ready = res.roomInfo.player2Ready;

            K.Send(token!, PacketType.RES_ENTER_ROOM_PACKET, res);

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

            K.Send(token!, PacketType.RES_CREATE_ROOM_PACKET, res);

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

            var where = K.Users.Where(x => x.userInfo.isLogined && x.userInfo.id == req.id);

            if (where.Any())
            {
                res.completed = false;
                res.reason = "중복 로그인";

                K.Send(token!, PacketType.RES_LOGIN_PACKET, res);
                return;
            }

            if (!K.SQL!.Select(new Query().Select("id", "useraccount", $"id = '{req.id}' AND pw = sha2('{req.pw}', 256)"), out reader!))
            {
                res.completed = false;
                res.reason = "로그인 정보에 해당하는 유저가 존재하지 않음";

                K.Send(token!, PacketType.RES_LOGIN_PACKET, res);
                K.SQL!.SelectEnd(ref reader!);
                return;
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

            K.Send(token!, PacketType.RES_LOGIN_PACKET, res);

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

                K.Send(token!, PacketType.RES_SIGNIN_PACKET, res);
                return;
            }

            MySqlDataReader reader;
            if (K.SQL!.Select(new Query().Select("*", "useraccount", $"id = '{req.id}'"), out reader!))
            {
                res.completed = false;
                res.reason = "이미 있는 계정임";

                K.Send(token!, PacketType.RES_SIGNIN_PACKET, res);
                K.SQL!.SelectEnd(ref reader!);
                Console.WriteLine($"/{res.reason}");
                return;
            }
            K.SQL!.SelectEnd(ref reader!);

            res.completed = false;
            res.reason = "INSERT 실패";

            if (K.SQL!.Query(new Query().Insert("useraccount", $"'{req.id}', sha2('{req.pw}',256)")))
            {
                res.completed = true;
                res.reason = "회원가입 성공";

                K.SQL!.Query(new Query().Insert("userinfo", $"'{req.id}',0,0"));
            }

            K.Send(token!, PacketType.RES_SIGNIN_PACKET, res);

            Console.WriteLine($"/{res.reason}");
        }

        private void Disconnected(Packet packet)
        {
            Console.WriteLine($"DISCONNECTED");

            RES res = new RES();
            res.completed = true;
            res.reason = "Disonnected";

            K.Send(token!, PacketType.DISCONNECTED, res);
        }

        private void Connected(Packet packet)
        {
            Console.WriteLine($"CONNECTED");

            RES res = new RES();
            res.completed = true;
            res.reason = "Connected";

            K.Send(token!, PacketType.CONNECTED, res);
        }

        private void REQ_Chat(Packet packet)
        {
            REQ_RES_Chat req = packet.GetPacket<REQ_RES_Chat>();

            packet.type = ((short)PacketType.RES_CHAT_PACKET);

            if (req.to == "ALL")
            {
                K.Users.ForEach(user =>
                {
                    user.token!.Send(packet);
                });
            }
            else
            {
                token!.Send(packet);

                var where = K.Users.Where(user => user.userInfo.id == req.id);
                if (!where.Any()) return;
                K.Users.Where(user => user.userInfo.id == req.id).Select(x => x).FirstOrDefault()!.token!.Send(packet);
            }

            Console.WriteLine($"{req.id} chat to {req.to}, {req.chat}");
        }

        public void Init(UserToken token)
        {
            this.token = token;
        }
    }
}
