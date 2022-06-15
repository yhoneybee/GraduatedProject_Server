using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public static class K
    {
        public static List<User> Users { get; set; } = new List<User>();
        public static List<RoomInfo> Rooms { get; set; } = new List<RoomInfo>();
        public static ISQL SQL { get; set; } = new MySQL();
    }
}
