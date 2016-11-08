using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Linq;
using System.Net;
using System.Data;
using System.Diagnostics;
using System.Threading;
using log4net;
using MiNET;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Blocks;
using MiNET.Worlds;
using MiNET.Security;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MySql.Data.MySqlClient;

namespace AuthME
{
    public class BanManager
    {
        static ILog Log = LogManager.GetLogger(typeof(BanManager));

        public AuthME AuthME;

        public PluginContext Context;

        public BanManager(AuthME auth, PluginContext context)
        {
            AuthME = auth;
        }

        [PacketHandler, Receive]
        public Package ChatHandler(McpeText text, Player player)
        {
            if (text.message.StartsWith("/") || text.message.StartsWith(".")) return text;
            PlayerData user = Pool.getPlayer(player.Username);
            if (user == null || !AuthME.isAuthenticated(player))
            {
                Log.Info("chat1");
                return null;
            }
            if (user.muted)
            {
                if ((long)user.mute_time > Database.UnixTime())
                {
                    long mute = (long)user.mute_time - Database.UnixTime();
                    player.SendMessage("[✘] Вы заблокированы в чате на " + mute + " секунд\n[✘] You are locked in a chat with " + mute + " seconds");
                    return null;
                }
                else
                {
                    player.SendMessage("[v] Вас разблокировали в чате!");
                    Pool.editPlayer(player.Username, "muted", "no");
                    Pool.editPlayer(player.Username, "mute_time", "", 0);

                }
            }
            string chatFormat = "{COLOR_GRAY}[" + user.prefix + "{COLOR_GRAY}]{COLOR_WHITE}{user_name} : {message}";
            chatFormat = chatFormat.Replace("{user_name}", player.Username);
            chatFormat = addColors(chatFormat);
            chatFormat = chatFormat.Replace("{message}", text.message);
            player.Level.BroadcastMessage(chatFormat);
            return null;
        }

        [Command(Command = "lang")]
        public void lang(Player player, string lang)
        {
            switch (lang)
            {
                case "eng":
                    AuthME.Database.Update("UPDATE userdata SET lang = '" + lang + "' WHERE user_low = '" + player.Username.ToLower() + "'");
                    break;
                case "rus":
                    AuthME.Database.Update("UPDATE userdata SET lang = '" + lang + "' WHERE user_low = '" + player.Username.ToLower() + "'");
                    break;
                case "por":
                    AuthME.Database.Update("UPDATE userdata SET lang = '" + lang + "' WHERE user_low = '" + player.Username.ToLower() + "'");
                    break;
            }
        }

        [Command(Command = "mute")]
        public void mute(Player player, string username, int time, string reason)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (Permission(player) < 4)
            {
                player.SendMessage("Вы не можете забанить игрока с этими полномочиями!!!");
                return;
            }
            int times = time * 60;
            int m_time = Database.UnixTime() + times;
            AuthME.Database.Update("UPDATE userdata SET muteadmin = '" + player.Username + "', muted = 'yes', mute_time = '" + m_time + "' WHERE user_low = '" + username.ToLower() + "'");
            Pool.editPlayer(username, "muted", "yes");
            Pool.editPlayer(username, "mute_time", "", m_time);
            string line = ("§cИгрок " + username + " получил бан чата на " + time + " минут от " + player.Username);
            player.Level.BroadcastMessage(line);
            stopwatch.Stop();
            Log.Info(stopwatch.Elapsed + " Mute");
        }

        [Command(Command = "unmute")]
        public void UnMute(Player player, string username)
        {
            if (Permission(player) < 4) return;
            AuthME.Database.Update("UPDATE userdata SET muted = 'no', mute_time = '0' WHERE user_low = '" + username.ToLower() + "'");
            Pool.editPlayer(username, "muted", "no");
            Pool.editPlayer(username, "mute_time", "", 0);
            string line = ("§cИгроку " + username + " снят бан чата от " + player.Username);
            player.Level.BroadcastMessage(line);
        }

        [Command(Command = "kick")]
        public void kick(Player player, string username)
        {
            if (Permission(player) < 4)
                return;
            //Context.Server.ServerInfo.PlayerSessions
            foreach (var pl in Context.Server.ServerInfo.PlayerSessions)
            {
                if (pl.Value.MessageHandler is Player)
                {
                    var PlayerSession = pl.Value.MessageHandler as Player;
                    if (PlayerSession.Username == username)
                    {
                        PlayerSession.Disconnect("Oopps, kick. Admin = " + player.Username);
                    }
                }
            }
            //player.username.Disconnect("Oopps, banned the wrong player. See ya soon!!");
        }

        [Command(Command = "ban")]
        public void Ban(Player player, string username, long time, string reason)
        {

        }

        public int Permission(Player player)
        {
            switch (Pool.getPlayer(player.Username).perm)
            {
                case "GUEST":
                    return 0;
                case "VIP":
                    return 1;
                case "PREMIUM":
                    return 2;
                case "DELUXE":
                    return 3;
                case "HELPER":
                    return 4;
                case "MOD":
                    return 5;
                case "GMOD":
                    return 6;
                case "ADMIN":
                    return 7;
                case "OWNER":
                    return 8;
            }
            return 0;
        }

        public string addColors(string chatFormat)
        {
            chatFormat = chatFormat.Replace("{COLOR_BLACK}", "§0");
            chatFormat = chatFormat.Replace("{COLOR_DARK_BLUE}", "§1");
            chatFormat = chatFormat.Replace("{COLOR_DARK_GREEN}", "§2");
            chatFormat = chatFormat.Replace("{COLOR_DARK_AQUA}", "§3");
            chatFormat = chatFormat.Replace("{COLOR_DARK_RED}", "§4");
            chatFormat = chatFormat.Replace("{COLOR_DARK_PURPLE}", "§5");
            chatFormat = chatFormat.Replace("{COLOR_GOLD}", "§6");
            chatFormat = chatFormat.Replace("{COLOR_GRAY}", "§7");
            chatFormat = chatFormat.Replace("{COLOR_DARK_GRAY}", "§8");
            chatFormat = chatFormat.Replace("{COLOR_BLUE}", "§9");
            chatFormat = chatFormat.Replace("{COLOR_GREEN}", "§a");
            chatFormat = chatFormat.Replace("{COLOR_AQUA}", "§b");
            chatFormat = chatFormat.Replace("{COLOR_RED}", "§c");
            chatFormat = chatFormat.Replace("{COLOR_LIGHT_PURPLE}", "§d");
            chatFormat = chatFormat.Replace("{COLOR_YELLOW}", "§e");
            chatFormat = chatFormat.Replace("{COLOR_WHITE}", "§f");

            return chatFormat;
        }
    }

    public static class Pool
    {

        private static ConcurrentDictionary<string, PlayerData> Players = new ConcurrentDictionary<string, PlayerData>();

        public static PlayerData getPlayer(string username)
        {
            if (Players.ContainsKey(username.ToLower()))
            {
                return Players[username.ToLower()];
            }
            return null;
        }

        public static PlayerData getPlayer(Player player)
        {
            if (Players.ContainsKey(player.Username.ToLower()))
            {
                return Players[player.Username.ToLower()];
            }
            return null;
        }

        public static void addPlayer(string u, string pref, string perm, bool muted, long mute_time, Player player, string lang)
        {
            //users[username.ToLower()] = new User(prefix, perm, muted, mute_time, player);
            Players.TryAdd(u.ToLower(), new PlayerData(pref, perm, muted, mute_time, player, lang));
        }

        /*public static void removePlayer(Player player)
		{
			removePlayer (player.Username);
		}*/

        public static void removePlayer(string username)
        {
            PlayerData removed;
            if (Players.TryRemove(username.ToLower(), out removed))
            {

            }
        }

        public static void editPlayer(string player, string line, string strings, long time = 0)
        {
            switch (line)
            {
                case "perm":
                    break;
                case "muted":
                    getPlayer(player.ToLower()).muted = bool.Parse(strings);
                    break;
                case "mute_time":
                    getPlayer(player.ToLower()).mute_time = time;
                    break;
            }
        }
    }

    public class PlayerData
    {
        public string prefix { get; set; }

        public Player player { get; set; }

        public string lang { get; set; }
        public string perm { get; set; }
        public bool muted { get; set; }
        public long mute_time { get; set; }

        public PlayerData(string prefix, string perm, bool muted, long mute_timed, Player player, string lang)
        {
            this.prefix = prefix;
            this.perm = perm;
            this.muted = muted;
            this.mute_time = mute_timed;
            this.player = player;
            this.lang = lang;
        }
    }
}

