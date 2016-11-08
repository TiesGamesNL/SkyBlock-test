using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Threading;
using MiNET.Utils;
using log4net;

using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Entities;
using MiNET;
using MiNET.Net;
using MiNET.Blocks;
using MiNET.Worlds;
using MiNET.Security;
using System.Linq;
using System.Net;
using LangM;
using Microsoft.AspNet.Identity;


namespace AuthME
{
    [Plugin(Author = "Overlord", Description = "AuthME and connection MySQL.", PluginName = "AuthME", PluginVersion = "0.0.1")]
    public class AuthME : Plugin
    {
        public List<Player> authenticated = new List<Player>();

        public Database Database { get; set; }

        public LangManager LangManager { get; set; }

        static ILog Log = LogManager.GetLogger(typeof(AuthME));

        protected /*override*/ void OnEnable()
        {
            //Context.Server.UserManager = new UserManager<MiNET.Security.User>(new DefaultUserStore());
            //Please do not use the default level
            /*if(_notdefaultlevel = Context.LevelManager.Levels.Count != 0)
			{
                foreach (var level in Context.LevelManager.Levels){
                    level.BlockBreak += OnBreak;
                    level.BlockPlace += OnPlace;
                }
            }*/
            //BanManager = new BanManager ();
            //Context.Server.PlayerFactory = new PlayerFactoryAuthME();

            var server = Context.Server;
            server.LevelManager.LevelCreated += (sender, args) =>
            {
                Level level = args.Level;
                level.BlockBreak += OnBreak;
                level.BlockPlace += OnPlace;
            };
            server.PlayerFactory.PlayerCreated += (sender, args) =>
            {
                Player player = args.Player;
                player.PlayerJoin += OnPlayerJoin;
                player.PlayerLeave += OnPlayerLeave;
            };
            var instance = new BanManager(this, Context);
            Context.PluginManager.LoadPacketHandlers(instance);
            Context.PluginManager.LoadCommands(instance);
            Database = new Database();
            Database.open();
            LangManager = new LangManager();
            LangManager.addLang("eng", new Lang("eng", MiNET.Utils.Config.GetProperty("PluginDirectory", "plugins") + "/lang/AuthME/eng.ini"));
            //LangManager.addLang("por", new Lang("eng", MiNET.Utils.Config.GetProperty ("PluginDirectory", "plugins") + "\\lang\\AuthME\\por.ini"));
            //LangManager.addLang("rus", new Lang("rus", MiNET.Utils.Config.GetProperty ("PluginDirectory", "plugins") + "\\lang\\AuthME\\rus.ini"));
            Log.Info("AuthME Enable");
        }

        public Database getDatabase()
        {
            return Database;
        }

        public /*override*/ void OnDisable()
        {
            Database.close();
            Log.Info("AuthME Disable");
        }

        private void OnPlayerJoin(object o, PlayerEventArgs eventArgs)
        {
            Level level = eventArgs.Level;
            if (level == null) throw new ArgumentNullException(nameof(eventArgs.Level));

            Player player = eventArgs.Player as PlayerAuth;
            if (player == null) throw new ArgumentNullException(nameof(eventArgs.Player));

            foreach (var pl in Context.Server.ServerInfo.PlayerSessions)
            {
                if (pl.Value.MessageHandler is Player)
                {
                    var PlayerSession = pl.Value.MessageHandler as Player;
                    if (PlayerSession != player && PlayerSession.Username.ToLower() == player.Username.ToLower())
                    {
                        if (isAuthenticated(PlayerSession))
                        {
                            player.Disconnect("already logged in");
                            return;
                        }
                    }
                }
            }

            Dictionary<string, string> userdata = getPlayer(player.Username);
            if (userdata == null)
            {
                //player.setData ("Player", "GUEST", false, 0, "eng");
                Pool.addPlayer(player.Username, "Player", "GUEST", false, 0, player, "eng");
                player.AddPopup(new Popup()
                {
                    MessageType = MessageType.Tip,
                    //Message = LangManager.getLang(PlayerPool.getPlayer(player.Username).lang).getString("authme.register.message"),
                    Message = LangManager.getLang(Pool.getPlayer(player.Username).lang).getString("authme.register.message"),
                    Duration = 15 * 60,
                });
            }
            else
            {
                int a = Convert.ToInt32(userdata["mute_time"]);
                //player.setData (userdata["prefix"], userdata["perm"], bool.Parse(userdata["muted"]), a, userdata["lang"]);
                Pool.addPlayer(player.Username, userdata["prefix"], userdata["perm"], bool.Parse(userdata["muted"]), a, player, userdata["lang"]);
                if (player.EndPoint.Address.ToString() == userdata["ip"])
                {
                    if (!isAuthenticated(player))
                    {
                        authenticated.Add(player);
                        player.ClearPopups();
                        player.AddPopup(new Popup()
                        {
                            Message = LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.success"),
                            Duration = 20 * 10,
                            MessageType = MessageType.Tip
                        });
                    }
                    //player.ClearPopups();
                }
                if (!isAuthenticated(player))
                {
                    player.AddPopup(new Popup()
                    {
                        MessageType = MessageType.Tip,
                        Message = LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.usage"),
                        Duration = 15 * 60,
                    });
                }
            }
            //level.BroadcastMessage($"{ChatColors.Gold}[{ChatColors.Green}+{ChatColors.Gold}]{ChatFormatting.Reset} {player.Username}");
        }

        private void OnPlayerLeave(object o, PlayerEventArgs eventArgs)
        {
            Level level = eventArgs.Level;
            if (level == null) throw new ArgumentNullException(nameof(eventArgs.Level));

            Player player = eventArgs.Player;
            if (player == null) throw new ArgumentNullException(nameof(eventArgs.Player));

            //deAuth (player);
            Pool.removePlayer(player.Username);
        }

        public void onBan(McpeLogin package, Player player)
        {
            string sql = string.Format("SELECT admin, reason, time FROM banip WHERE player='{0}' OR ip='{1}' OR clientid='{2}';", player.Username.ToLower(), player.EndPoint.Address, GenerateMD5Hash(player.ClientUuid.ToString()));
            List<object[]> rows = Database.ExecuteQuery(sql);
            if (rows == null || !rows.Any())
                return;
            if (rows[0][0].ToString() != null)
            {
                //Log.Info (rows);
                int time = Convert.ToInt32(rows[0][2].ToString()) / 86400;
                player.Disconnect(string.Format(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.ban.message.banned"), rows[0][0].ToString(), time, rows[0][1].ToString()), true);
            }
        }

        public bool isAuthenticated(Player player)
        {
            if (authenticated.Contains(player))
                return true;
            return false;
        }

        public void deAuth(Player player)
        {
            if (isAuthenticated(player))
                authenticated.Remove(player);
        }

        [Command(Command = "reg")]
        public void Register(Player player, string password)
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            string Name = player.Username;
            if (getPlayer(Name) == null)
            {
                register(player, GetPasswordHash(password));
            }
            else
            {
                player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.register.error.registered"));
            }
            stop.Stop();
            Log.Info(stop.ElapsedTicks + " === Register");
        }

        [Command(Command = "log")]
        public void Auth(Player player, string password)
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            //string Name = player.Username;
            Dictionary<string, string> userdata = getPlayer(player.Username);
            if (userdata != null)
            {
                if (userdata["pass"] == GetPasswordHash(password).ToString())
                {
                    if (!isAuthenticated(player))
                    {
                        authenticated.Add(player);
                    }
                    else
                    {
                        player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.error.auth"));
                        return;
                    }
                }
                if (isAuthenticated(player))
                {
                    player.ClearPopups();
                    player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.success"));
                    player.AddPopup(new Popup()
                    {
                        Message = LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.success"),
                        Duration = 20 * 10,
                        MessageType = MessageType.Tip
                    });
                    //player.SendMessage(ChatColors.Red + "Wrong password!!\n" + ChatColors.Red + "Не правильный пароль!!");
                }
                else
                {
                    player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.error.password"));
                }
            }
            else
            {
                player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.login.error.password"));
            }
        }

        private void OnBreak(object sender, BlockBreakEventArgs e)
        {
            e.Cancel = !isAuthenticated(e.Player);
        }

        private void OnPlace(object sender, BlockPlaceEventArgs e)
        {
            e.Cancel = !isAuthenticated(e.Player);
        }

        private string GetPasswordHash(string password)
        {
            string md5 = GenerateMD5Hash(password);
            string md52 = GenerateMD5Hash(md5);
            return md52;
        }

        public string GenerateMD5Hash(string rawText)
        {
            // создаем экземпляр провайдера MD5 шифрования
            MD5CryptoServiceProvider md5Hash = new MD5CryptoServiceProvider();
            // конвертируем string в массив byte
            byte[] randByte = Encoding.UTF8.GetBytes(rawText);
            // вычисляем хэш массива байтов randByte
            byte[] computeHash = md5Hash.ComputeHash(randByte);
            // инициализируем переменную resultHash
            string resultHash = String.Empty;
            // перебираем каждый байт
            foreach (byte currentByte in computeHash)
            {
                // конвертируем байт в string
                resultHash += currentByte.ToString("x2");
            }
            // возвращаем результирующий string
            return resultHash;
        }

        private void register(Player player, string password)
        {
            if (!isAuthenticated(player))
            {
                authenticated.Add(player);
            }
            //string sql11 = "INSERT INTO userdata (user, user_low, ip, clientid, pass, reg_date) VALUES ";
            string sql = string.Format("INSERT INTO test (user, user_low, ip, clientid, pass, reg_date) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}');", player.Username, player.Username.ToLower(), player.EndPoint.Address.ToString(), player.ClientUuid, password, Database.UnixTime());
            Database.Insert(sql);
            player.ClearPopups();
            player.SendMessage(LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.register.success"));
            player.AddPopup(new Popup()
            {
                Message = LangManager.getLang(Pool.getPlayer(player).lang).getString("authme.register.success"),
                Duration = 20 * 10,
                MessageType = MessageType.Popup
            });
        }

        //private List<object[]>  getPlayer(string player)
        private Dictionary<string, string> getPlayer(string player)
        {
            //Stopwatch stop = new Stopwatch ();
            //stop.Start ();
            string sql = string.Format("SELECT user_low, pass, ip, clientid, prefix, perm, muted, mute_time, lang FROM test WHERE user_low='{0}';", player);
            List<object[]> rows = Database.ExecuteQuery(sql);
            if (rows == null || !rows.Any())
            {
                //stop.Stop ();
                //Log.Info (stop.ElapsedTicks + " === getPlayer(1)");
                return null;
            }
            if (rows[0][0].ToString() == player.ToLower())
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("user", rows[0][0].ToString());
                data.Add("pass", rows[0][1].ToString());
                data.Add("ip", rows[0][2].ToString());
                data.Add("clientid", rows[0][3].ToString());
                data.Add("prefix", rows[0][4].ToString());
                data.Add("perm", rows[0][5].ToString());
                data.Add("muted", rows[0][6].ToString());
                data.Add("mute_time", rows[0][7].ToString());
                data.Add("lang", rows[0][8].ToString());
                //stop.Stop ();
                //Log.Info (stop.ElapsedTicks + " === getPlayer(2)");
                //return rows;
                return data;
            }
            //stop.Stop ();
            //Log.Info (stop.ElapsedTicks + " === getPlayer(3)");
            return null;
        }

        public PlayerData getPlayerPool(Player player)
        {
            return Pool.getPlayer(player.Username);
        }
    }
}
