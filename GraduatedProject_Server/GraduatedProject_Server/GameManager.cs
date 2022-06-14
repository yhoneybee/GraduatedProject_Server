using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class GameManager : Singleton<GameManager>
    {
        public ISQL? SQL { get; set; }
        public List<User>? Users { get; set; }
        public List<RoomInfo>? Rooms { get; set; }

        protected override void Init()
        {
            Users = new List<User>();
            Rooms = new List<RoomInfo>();
            SQL = new MySQL();
            SQL.Initilize();
        }
    }
}
