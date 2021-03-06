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
        public CRoomInfo? roomInfo;

        public UserToken? token;

        REQ_RES_Charactor? lastCharactorPacket;

        public void ProcessPacket(Packet packet)
        {
            switch ((PacketType)packet.type)
            {
                case PacketType.REQ_CONNECTED:
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
                    REQ_User(packet);
                    break;
                case PacketType.REQ_READY_GAME_PACKET:
                    REQ_ReadyGame(packet);
                    break;
                case PacketType.REQ_START_GAME_PACKET:
                    REQ_StartGame(packet);
                    break;
                case PacketType.REQ_GAME_WIN_PACKET:
                    REQ_GameWin(packet);
                    break;
                case PacketType.REQ_GAME_LOSE_PACKET:
                    REQ_GameLose(packet);
                    break;
                case PacketType.REQ_CHAT_PACKET:
                    REQ_Chat(packet);
                    break;
                case PacketType.REQ_SELECTCHARACTOR:
                    REQ_Select(packet);
                    break;
                case PacketType.REQ_CHARACTOR_PACKET:
                    REQ_Charactor(packet);
                    break;
                case PacketType.REQ_LOGOUT_PACKET:
                    REQ_Logout(packet);
                    break;
                case PacketType.REQ_DISCONNECTED:
                    Disconnected(packet);
                    break;
                case PacketType.END:
                    break;
            }
        }

        private void REQ_GameLose(Packet packet)
        {
            Console.Write("REQ_GameLose : ");

            userInfo.lose++;

            RefreshUserInfo();
        }

        private void REQ_GameWin(Packet packet)
        {
            Console.Write("REQ_GameWin : ");

            userInfo.win++;

            RefreshUserInfo();
        }

        private void RefreshUserInfo()
        {
            RES res = new RES();
            res.completed = false;
            res.reason = "Update failed";
            if (K.SQL.Query(new Query().Update("userinfo", $"win = {userInfo.win}, lose = {userInfo.lose}", $"id = '{userInfo.id}'")))
            {
                res.completed = true;
                res.reason = "Update completed";
            }
            K.Send(token!, PacketType.RES_GAME_END_PACKET, res);

            RES_User res1 = new RES_User();
            res1.completed = true;
            res1.reason = "info refresh";
            res1.userInfo = userInfo;
            K.Send(token!, PacketType.RES_USER_PACKET, res1);

            Console.WriteLine($"{res.reason}");
        }

        private void REQ_Select(Packet packet)
        {
            packet.type = ((short)PacketType.RES_SELECTCHARACTOR);
            TossPacketToOther(packet);
        }

        private void REQ_User(Packet packet)
        {
            var req = packet.GetPacket<REQ_User>();
            var user = K.Users.Find(x => x.userInfo.id == req.id);

            RES_User res = new RES_User();
            res.completed = true;
            res.reason = "userinfo load success";

            if (user != null)
                res.userInfo = user.userInfo;
            else
                res.userInfo = new();

            K.Send(token!, PacketType.RES_USER_PACKET, res);
            Console.WriteLine($"{res.reason}");
        }

        private void REQ_Logout(Packet packet)
        {
            Console.Write("REQ_Logout : ");

            RES res = new RES();

            if (UpdateUserIsLogin(false))
            {
                res.completed = true;
                res.reason = "logout success";
                userInfo = new();
            }
            else
            {
                res.completed = false;
                res.reason = "logout failed";
            }

            K.Send(token!, PacketType.RES_LOGOUT_PACKET, res);

            Console.WriteLine($"{res.reason}");
        }

        private void REQ_Charactor(Packet packet)
        {
            var req = packet.GetPacket<REQ_RES_Charactor>();

            RefreshPacketDataAndSend(packet, req);

            //if (lastCharactorPacket == null)
            //{
            //    RefreshPacketDataAndSend(packet, req);
            //}
            //else if (req.charactorState != lastCharactorPacket.charactorState ||
            //    req.posX != lastCharactorPacket.posX ||
            //    req.hp != lastCharactorPacket.hp ||
            //    req.dir != lastCharactorPacket.dir)
            //{
            //    RefreshPacketDataAndSend(packet, req);
            //}
        }

        private void RefreshPacketDataAndSend(Packet packet, REQ_RES_Charactor req)
        {
            lastCharactorPacket = req;
            packet.type = ((short)PacketType.RES_CHARACTOR_PACKET);
            TossPacketToOther(packet);
        }

        private void REQ_StartGame(Packet packet)
        {
            Console.Write("REQ_StartGame : ");

            RES_StartGame res = new RES_StartGame();

            if (CheckReady())
            {
                res.completed = true;
                res.reason = "game start";

                res.playerNum = roomInfo!.roomInfo.player1 == userInfo.id ? 0 : 1;
                K.Send(token!, PacketType.RES_START_GAME_PACKET, res);
                res.playerNum = res.playerNum == 1 ? 0 : 1;
                K.Send(GetOther()?.token!, PacketType.RES_START_GAME_PACKET, res);

                K.Rooms.Find(x => x.roomInfo.name == roomInfo.roomInfo.name)?.StartTimer();
            }
            else
            {
                res.completed = false;
                res.reason = "game start failed";
            }

            Console.WriteLine($"{res.reason}");
        }

        private void REQ_Rooms(Packet packet)
        {
            Console.Write("REQ_Rooms : ");

            var req = packet.GetPacket<REQ_Rooms>();

            int startIndex = req.page * 9;
            int count = 0;

            RES_Rooms res = new RES_Rooms();
            res.roomInfos = new RoomInfo[9];
            res.completed = true;

            for (int i = 0; i < 9; i++)
            {
                res.roomInfos[i] = new RoomInfo();
                res.roomInfos[i].name = res.roomInfos[i].player1 = res.roomInfos[i].player2 = string.Empty;
            }

            for (int i = 0; i < K.Rooms.Count; i++)
            {
                if (i >= startIndex)
                {
                    ++count;
                    res.roomInfos[i] = K.Rooms[i].roomInfo;
                    if (count == 9)
                        break;
                }
            }

            res.reason = $"{count} of rooms send success";

            K.Send(token!, PacketType.RES_ROOMS_PACKET, res);

            Console.WriteLine($"{res.reason}");
        }

        private void REQ_ReadyGame(Packet packet)
        {
            Console.Write("REQ_ReadyGame : ");

            RES res = new RES();
            res.completed = true;

            ReverseReady();
            UpdateReady();
            res.reason = $"{roomInfo!.roomInfo.player1Ready},{roomInfo!.roomInfo.player2Ready}";

            K.Send(token!, PacketType.RES_READY_GAME_PACKET, res);

            packet.SetData(PacketType.RES_READY_GAME_PACKET, Data<RES>.Serialize(res));
            GetOther()?.token?.Send(packet);

            Console.WriteLine($"{userInfo.id}/{res.reason}");
        }

        private void REQ_LeaveRoom(Packet packet)
        {
            Console.Write($"REQ_LeaveRoom : ");

            var req = packet.GetPacket<REQ>();

            RES res = new RES();

            var room = GetRoomInfo(roomInfo!.roomInfo.name);

            RES_OtherUser res1 = new RES_OtherUser();
            res1.completed = true;
            res1.reason = "leaved player info";
            res1.roomInfo = room?.roomInfo ?? new();

            if (room == null)
            {
                res.completed = true;
                res.reason = "non room";
            }
            else
            {
                if (room.roomInfo.player1 == string.Empty || room.roomInfo.player2 == string.Empty)
                {
                    res.completed = false;
                    if (DeleteRoom())
                    {
                        res.completed = true;
                        res.reason = $"{room.roomInfo.name} room was deleted";
                        K.Rooms.Remove(room);
                    }
                }
                else
                {
                    var updateColumnName = string.Empty;

                    if (room.roomInfo.player1 == userInfo.id)
                    {
                        updateColumnName = "player1";
                        room.roomInfo.player1 = string.Empty;
                        res1.player2 = GetOther()?.userInfo ?? new();
                    }
                    else if (room.roomInfo.player2 == userInfo.id)
                    {
                        updateColumnName = "player2";
                        room.roomInfo.player2 = string.Empty;
                        res1.player1 = GetOther()?.userInfo ?? new();
                    }

                    res.completed = false;
                    if (LeaveRoom(updateColumnName))
                    {
                        res.completed = true;
                        res.reason = $"{room.roomInfo.name} was leave room";

                        packet.SetData(PacketType.RES_OTHER_USER_LEAVE_ROOM_PACKET, Data<RES_OtherUser>.Serialize(res1));
                        GetOther()?.token?.Send(packet);
                    }
                }
            }

            K.Send(token!, PacketType.RES_LEAVE_ROOM_PACKET, res);

            Console.WriteLine($"{userInfo.id}/{res.reason}");
        }

        private void REQ_EnterRoom(Packet packet)
        {
            Console.Write("REQ_EnterRoom : ");

            var req = packet.GetPacket<REQ_CreateEnterRoom>();

            RES_EnterRoom res = new RES_EnterRoom();
            res.completed = false;
            res.reason = "join failed";

            var room = GetRoomInfo(req.roomName);

            string updateColumnName = string.Empty;

            RES_OtherUser res1 = new RES_OtherUser();
            res1.completed = true;
            res1.reason = "joined player info";
            res1.roomInfo = room.roomInfo;

            roomInfo = room;
            if (room.roomInfo.name == req.roomName)
            {
                if (room.roomInfo.player1 == string.Empty)
                {
                    room.roomInfo.player1 = userInfo.id;
                    res1.player1 = res.player1 = userInfo;
                    res1.player2 = res.player2 = GetOther()?.userInfo ?? new();
                    res.host = true;
                    updateColumnName = "player1";
                }
                else if (room.roomInfo.player2 == string.Empty)
                {
                    room.roomInfo.player2 = userInfo.id;
                    res1.player1 = res.player1 = GetOther()?.userInfo ?? new();
                    res1.player2 = res.player2 = userInfo;
                    res.host = false;
                    updateColumnName = "player2";
                }
            }

            if (UpdateRoomInfo(updateColumnName, req.roomName, false))
            {
                res.completed = true;
                res.reason = "join success";

                packet.SetData(PacketType.RES_OTHER_USER_ENTER_ROOM_PACKET, Data<RES_OtherUser>.Serialize(res1));
                GetOther()?.token?.Send(packet);
            }

            res.roomInfo = room.roomInfo;

            K.Send(token!, PacketType.RES_ENTER_ROOM_PACKET, res);

            Console.WriteLine($"{req.roomName}/{res.reason}");
        }

        private void REQ_CreateRoom(Packet packet)
        {
            Console.Write("REQ_CreateRoom : ");

            var req = packet.GetPacket<REQ_CreateEnterRoom>();

            if (req.roomName == string.Empty)
            {
                Console.WriteLine($"name was empty");
                return;
            }

            RES_CreateRoom res = new RES_CreateRoom();

            var where = K.Rooms!.Where(x => x.roomInfo.name == req.roomName);

            res.completed = false;
            res.reason = "was overlap room name";
            res.roomName = req.roomName;

            if (!where.Any())
            {
                res.reason = "INSERT failed";

                if (K.SQL!.Query(new Query().Insert("roominfo", "name", $"'{req.roomName}'")))
                {
                    res.completed = true;
                    res.reason = "room create success";
                    var room = new CRoomInfo();
                    room.roomInfo = new RoomInfo { name = res.roomName, player1 = string.Empty, player2 = string.Empty };
                    K.Rooms.Add(room);
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

            if (K.SQL.Select(new Query().Select("*", "useraccount", $"id = '{req.id}' AND pw = sha2('{req.pw}', 256)"), out reader!))
            {
                K.SQL.SelectEnd(ref reader!);

                if (K.SQL.Select(new Query().Select("*", "userinfo", $"id = '{req.id}'"), out reader!))
                {
                    reader.Read();
                    userInfo.win = (uint)reader["win"];
                    userInfo.lose = (uint)reader["lose"];
                    var isLogined = Convert.ToBoolean(reader["isLogined"]);

                    K.SQL.SelectEnd(ref reader!);

                    if (isLogined)
                    {
                        res.completed = false;
                        res.reason = "overlap login";
                    }
                    else
                    {
                        res.completed = false;
                        res.reason = "login failed";
                        userInfo.id = req.id;

                        if (UpdateUserIsLogin(true))
                        {
                            res.completed = true;
                            res.reason = "login success";
                        }
                    }
                }
            }
            else
            {
                res.completed = false;
                res.reason = "not found user for login info";
            }
            K.SQL.SelectEnd(ref reader!);

            K.Send(token!, PacketType.RES_LOGIN_PACKET, res);

            Console.WriteLine($"{req.id}/{res.reason}");
        }

        private void REQ_Signin(Packet packet)
        {
            Console.Write("REQ_Signin : ");

            var req = packet.GetPacket<REQ_Signin>();

            if (req.id == string.Empty)
            {
                Console.WriteLine($"id was empty");
                return;
            }
            else if (req.pw == string.Empty)
            {
                Console.WriteLine($"pw was empty");
                return;
            }

            RES res = new RES();

            if (req.pw != req.pwAgain)
            {
                res.completed = false;
                res.reason = "not same pw, pw again";

                K.Send(token!, PacketType.RES_SIGNIN_PACKET, res);
                return;
            }

            MySqlDataReader reader;
            if (K.SQL!.Select(new Query().Select("*", "useraccount", $"id = '{req.id}'"), out reader!))
            {
                res.completed = false;
                res.reason = "overlap account";

                K.Send(token!, PacketType.RES_SIGNIN_PACKET, res);
                K.SQL!.SelectEnd(ref reader!);
                Console.WriteLine($"/{res.reason}");
                return;
            }
            K.SQL!.SelectEnd(ref reader!);

            res.completed = false;
            res.reason = "INSERT failed";

            if (K.SQL!.Query(new Query().Insert("useraccount", $"'{req.id}', sha2('{req.pw}',256)")))
            {
                res.completed = true;
                res.reason = "sign success";

                K.SQL!.Query(new Query().Insert("userinfo", $"'{req.id}',0,0,0,0"));
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

            K.Send(token!, PacketType.RES_DISCONNECTED, res);
        }

        private void Connected(Packet packet)
        {
            Console.WriteLine($"CONNECTED");

            RES res = new RES();
            res.completed = true;
            res.reason = "Connected";

            roomInfo = new CRoomInfo();

            K.Send(token!, PacketType.RES_CONNECTED, res);
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

        public bool UpdateUserIsLogin(bool isLogin)
            => K.SQL.Query(new Query().Update("userinfo", $"isLogined = {(isLogin ? 1 : 0)}", $"id = '{userInfo.id}'"));

        public void TossPacketToOther(Packet packet)
            => GetOther()?.token?.Send(packet);

        public User? GetOther()
        {
            if (roomInfo!.roomInfo.player1 == userInfo.id)
                return K.Users.Where(x => x.userInfo.id == roomInfo!.roomInfo.player2).FirstOrDefault()!;
            else if (roomInfo!.roomInfo.player2 == userInfo.id)
                return K.Users.Where(x => x.userInfo.id == roomInfo!.roomInfo.player1).FirstOrDefault()!;
            else
                return null;
        }

        public void UpdateReady()
        {
            var other = GetOther();

            if (other == null) return;

            if (roomInfo!.roomInfo.player1 == userInfo.id)
                roomInfo!.roomInfo.player2Ready = other!.roomInfo!.roomInfo.player2Ready;
            else if (roomInfo!.roomInfo.player2 == userInfo.id)
                roomInfo!.roomInfo.player1Ready = other!.roomInfo!.roomInfo.player1Ready;
        }

        public void ReverseReady()
        {
            if (roomInfo!.roomInfo.player1 == userInfo.id)
                roomInfo!.roomInfo.player1Ready = !roomInfo!.roomInfo.player1Ready;
            else if (roomInfo!.roomInfo.player2 == userInfo.id)
                roomInfo!.roomInfo.player2Ready = !roomInfo!.roomInfo.player2Ready;
        }

        public bool CheckReady()
        {
            UpdateReady();

            return roomInfo!.roomInfo.player1Ready && roomInfo!.roomInfo.player2Ready;
        }

        public CRoomInfo GetRoomInfo(string name)
            => K.Rooms!.Where(x => x.roomInfo.name == name)!.FirstOrDefault()!;

        public bool UpdateRoomInfo(string updateColumnName, string roomName, bool overlapToNull)
            => K.SQL.Query(new Query().Update("roominfo", $"{updateColumnName} = '{(overlapToNull ? "NULL" : userInfo.id)}'", $"name = '{roomName}'"));

        public bool DeleteRoom()
            => K.SQL.Query(new Query().Delete("roominfo", $"name = '{roomInfo!.roomInfo.name}'"));

        public bool LeaveRoom(string updateColumnName)
            => K.SQL.Query(new Query().Update("roominfo", $"{updateColumnName} = NULL", $"name = '{roomInfo!.roomInfo.name}'"));

        public void Init(UserToken token)
        {
            this.token = token;
        }
    }
}
