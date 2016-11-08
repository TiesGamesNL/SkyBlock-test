using System;
using System.Net;
using log4net;
using MiNET;

namespace AuthME
{
    public class PlayerAuth : Player
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlayerAuth));

        public string Prefix { get; set; }

        public string Perm { get; set; }

        public bool Mute { get; set; }

        public int Mute_time { get; set; }

        public string Lang { get; set; }

        public bool Auth { get; set; }

        public PlayerAuth(MiNetServer server, IPEndPoint endPoint) : base(server, endPoint)
        {

        }

        public void setData(string p, string perm, bool m, int m_t, string l)
        {
            Prefix = p;
            Perm = perm;
            Mute = m;
            Mute_time = m_t;
            Lang = l;
        }


    }
}

