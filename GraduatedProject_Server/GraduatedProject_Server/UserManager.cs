using MyPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduatedProject_Server
{
    public class UserManager : Singleton<UserManager>
    {
        public List<User>? Users { get; set; }

        protected override void Init()
        {
            Users = new List<User>();
        }
    }
}
