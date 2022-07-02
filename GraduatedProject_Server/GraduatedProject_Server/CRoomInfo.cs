using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyPacket;

namespace GraduatedProject_Server
{
    public class CRoomInfo
    {
        public RoomInfo roomInfo;

        Timer? timer;

        int time = 99;

        public void StartTimer()
        {
            time = 100;
            timer = new Timer(OnTimer, null, 0, 1000);
        }

        private void OnTimer(object? state)
        {
            --time;

            if (time < 0)
            {
                timer?.Dispose();
                return;
            }

            RES_GameTime res = new RES_GameTime();
            res.gameTime = time;
            res.completed = true;
            res.reason = "";

            K.Send(K.Users.Find(x => x.userInfo.id == roomInfo.player1)?.token!, PacketType.RES_GAME_TIME_PACKET, res);
            K.Send(K.Users.Find(x => x.userInfo.id == roomInfo.player2)?.token!, PacketType.RES_GAME_TIME_PACKET, res);
        }

        ~CRoomInfo()
        {
            timer?.Dispose();
        }
    }
}
