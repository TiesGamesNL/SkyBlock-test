using System;
using System.Net;
using MiNET;

namespace AuthME
{
	public class PlayerFactoryAuthME : PlayerFactory
	{
		public override Player CreatePlayer(MiNetServer server, IPEndPoint endPoint)
		{
			var player = new PlayerAuth(server, endPoint);
			OnPlayerCreated(new PlayerEventArgs(player));
			return player;
		}
	}
}

