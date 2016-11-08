using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Reflection;
using fNbt;
using log4net;
using MiNET;
using MiNET.Blocks;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;
using MiNET.Worlds.Structures;
using MiNET.Net;
using MiNET.BlockEntities;
using MiNET.Entities;
using MiNET.Entities.World;
using MiNET.Entities.Passive;
using MiNET.Items;
using MiNET.Sounds;
using SkyBlock.Utils;
using SkyBlock.Islands;
using SkyBlock.Managers;
using SkyBlock.INpcCore;
using LangM;
using FileSystem;

namespace SkyBlock
{
    [Plugin(PluginName = "SkyBlock", Description = "Sky Block survival mode for MiNET core", PluginVersion = "1.7.3 beta", Author = "Aleksey DerkLex")]
    public class SkyBlock : Plugin
    {
        static ILog Log = LogManager.GetLogger(typeof(SkyBlock));

        public static IDictionary<string, Island> Islands = new Dictionary<string, Island>();
        //public static IDictionary<Player, InventoryMenager> InventoryMenager = new Dictionary<Player, InventoryMenager>();
        public static IDictionary<string, int> HungerBuffer = new Dictionary<string, int>();
        public int IsMenager { get; set; }

        public static LangManager LangManager { get; set; }

        public string[][] MoneyHelpStrings = new string[][]
        {
            new string[]{ "ALL" , "[§aSB§f]/is money - you money" },
            new string[]{ "OWN" , "[§aSB§f]/is money add <player name> <money count> - add money to player" },
            new string[]{ "ALL" , "[§aSB§f]/is money help - money commands" },
            new string[]{ "ALL" , "[§aSB§f]/is money pay <player name> <money count> - pay money" },
            new string[]{ "OWN" , "[§aSB§f]/is money set <player name> <money count> - set player money" },
            new string[]{ "OWN" , "[§aSB§f]/is money view <player name> - get money by player" }
        };
        
        public IDictionary<List<string>, string> HelpStrings = new Dictionary<List<string>, string>()
        {
            { new List<string>() { "ALL" }, "/is - Basic island command" },
            { new List<string>() { "ALL" }, "/is buy <id> <count> - buy new items" },
            { new List<string>() { "GUEST", "HELPER" }, "/is create <island name> - create new you island" },
            { new List<string>() { "VIP", "PREMIUM", "DELUXE", "MOD", "GMOD", "ADMIN", "OWNER" }, "/is create <island name> <island type> - create new you island" },
            { new List<string>() { "GUEST", "VIP", "PREMIUM", "DELUXE", "HELPER", "MOD", "GMOD" }, "/is list <username> - informations to player island" },
            { new List<string>() { "ADMIN", "OWNER" }, "/is list <username> <player name> - informations to player island" },
            { new List<string>() { "GUEST", "VIP", "PREMIUM", "DELUXE", "HELPER", "MOD", "GMOD", "ADMIN" }, "/is member <add | remove> <player name> - edit members list you island" },
            { new List<string>() { "OWNER" }, "/is member <add | remove> <player name> <island owner name> - edit members list you island" },
            { new List<string>() { "ALL" }, "/is money - money command" },
            { new List<string>() { "ADMIN", "OWNER" }, "/is minereset <pvp | pve> - respawn pvp or pve mines" },
            { new List<string>() { "ALL" }, "/is out - out from members other island" },
            { new List<string>() { "GUEST", "VIP", "PREMIUM", "DELUXE", "HELPER", "MOD", "GMOD", "ADMIN" }, "/is remove - remove you island" },
            { new List<string>() { "OWNER" }, "/is remove <island owner name> - remove island" },
            { new List<string>() { "GUEST", "VIP", "PREMIUM", "DELUXE", "HELPER", "MOD", "GMOD" }, "/is resethome - reset you islans spawn position" },
            { new List<string>() { "ADMIN", "OWNER" }, "/is resethome <island owner name> - reset islans spawn position" },
            { new List<string>() { "ALL" }, "/is sell <id> <count> - sell you items" },
            { new List<string>() { "ALL" }, "/is spawn - teleport to spawn" }
        };

        public static string[] ops { get; set; }

        public static int Timer_InfoAlerts_SaveChanks { get; private set; }

        public List<string> AlreadyCreate = new List<string>();

        public Level Level { get; set; }

        private static System.Timers.Timer aTimer;
        private void SetTimer()
        {
            aTimer = new System.Timers.Timer(1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        protected override void OnEnable()
        {
            try
            {
                LangManager = new LangManager();
                LangManager.addLang("eng", new Lang("eng", Config.GetProperty("PluginDirectory", "plugins") + "/lang/" + GetType().ToString().Split('.')[0] + "/eng.ini"));
                //LangManager.addLang("por", new Lang("por", Config.GetProperty ("PluginDirectory", "plugins") + "/lang/" + GetType().ToString().Split('.')[0] + "/por.ini"));
                LangManager.addLang("rus", new Lang("rus", Config.GetProperty("PluginDirectory", "plugins") + "/lang/" + GetType().ToString().Split('.')[0] + "/rus.ini"));
                Timer_InfoAlerts_SaveChanks = 0;
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
                //player.HealthManager.PlayerTakeHit += OnTakeHit;
            };
                Log.Info("SkyBlock Enable");
                SetTimer();
                Files.Create("World/players/ops.config", Files.OpenRead("World/players/ops.config"));
                ops = Files.OpenRead("players/ops.config").Split(',');
                Files.Create("World/players/white-list.config", Files.OpenRead("World/players/white-list.config"));
                Files.Create("World/players/baned.config", Files.OpenRead("World/players/baned.config"));
                IsMenager = Convert.ToInt32(Files.OpenRead("SkyBlock/IsMenager.config"));
                Level l = Context.LevelManager.GetLevel(null, "Default");
                Level = l;
                Level.WillBeSaved = true;
                Level.LoadPlayerData = true;
                SB.Initialization();
                Level.SpawnPoint = new PlayerLocation(-199945, 100, -199970);
                DailyBonus.Initialize(Level);
                //Villager = new NpcCore(15, "§acollect the daily bonus!", Level) { KnownPosition = new PlayerLocation(-199949.5, 100, -199953.5), Gravity = 0, Height = 0, HideNameTag = false };
                //Villager.SpawnEntity();

                DailyBonus.SpawnVillager();
                //DailyBonusNpc = new PlayerMob("§acollect the daily bonus!", Level) { NameTag = "§acollect the daily bonus!", KnownPosition = new PlayerLocation(-199949.5, 100, -199953.5), Width = 0, IsInvisible = true, Gravity = 0, Height = 0, HideNameTag = false };
                NpcEvents.EntityEvent += OnNpcEntityEvent;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static bool IsOp(string Username)
        {
            foreach (string op in ops)
            {
                if (op == Username) return true;
            }
            return false;
        }

        private void OnNpcEntityEvent(Object source, NpcEventArgs e)
        {
            if (e.NpcStructure.UniType == DailyBonus.UniType)
            {
                if (!DailyBonus.DailyBonusClaimed[e.Player.Username].Status)
                {
                    DailyBonus.AddClaimedDay(e.Player);
                    e.NpcStructure.Player = e.Player;
                    e.NpcStructure.Text = string.Format(LangManager.getLang("eng").getString("skyblock.npc_dailybonus.npc_txt_2"));
                    e.NpcStructure.SendTextEntity();
                    e.Player.SendMessage(" ");
                    e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_2"));
                    e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.yougot") + " " + DailyBonus.GetBonus(e.Player));
                    e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_2"));
                }
                else
                {
                    e.Player.SendMessage(" ");
                    e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_3"));
                    e.Player.SendMessage("§0   " + LangManager.getLang("eng").getString("skyblock.npc_dailybonus.failure"));
                    e.Player.SendMessage(" ");
                    e.Player.SendMessage("§0             " + LangManager.getLang("eng").getString("skyblock.npc_dailybonus.until_bonus"));
                    e.Player.SendMessage("§0  " + DailyBonus.GetTimeToClaim(e.Player));
                    e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_3"));
                }
            }
        }

        public override void OnDisable()
        {
            //Level._worldProvider.SaveChunks();
            //Player[] players = Context.LevelManager.Levels[0].Players.Values.ToArray();
            //foreach (Player player in players)
            //{
                //InventoryMenager[player].SaveInventory(player);
            //}
            Log.Info("SkyBlock Disable");
        }

        //public int UpdatingDailyBonusNps = 0;
        
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Timer_InfoAlerts_SaveChanks++;
            var players = Level.Players.Values.ToArray();
            if (Timer_InfoAlerts_SaveChanks >= 200)
            {
                Timer_InfoAlerts_SaveChanks = 0;
                
                int messageId = new Random((int)(DateTime.UtcNow.Ticks)).Next(1, 5);
                foreach (Player player in players)
                    player.SendMessage("§8[§l§2CristalixPE§r§8]§3" + LangManager.getLang("eng").getString("skyblock.pc.infomessage_" + messageId));
            }
            MineManager.SendMinesTime(Level);
        }

        private void OnPlayerLeave(object package, PlayerEventArgs e)
        {
        }

        private void OnPlayerJoin(object packege, PlayerEventArgs e)
        {
            try
            {
                Player player = e.Player;
                //player.EnableCommands = false;
                player.HungerManager = new UnHungerManager(player);
                player.HealthManager = new UnHealthManager(player);
                player.SendUpdateAttributes();
                Player[] players = Context.LevelManager.Levels[0].Players.Values.ToArray();
                //bool PlayerInGame = false;
                foreach (Player InPlayer in players)
                {
                    if (InPlayer != player && InPlayer.Username == player.Username)
                    {
                        player.Disconnect(LangManager.getLang("eng").getString("skyblock.playerjoin.failure_1"));
                        return;
                    }
                }

                string[] BanedUsernames = Files.OpenRead("players/baned.config").Split(',');

                for (var i = 0; i < BanedUsernames.Length; i++)
                {
                    if (BanedUsernames[i] == player.Username)
                    {
                        player.Disconnect("You are baned!");
                        return;
                    }
                }

                player.Username = player.Username.Replace(' ', '_');
                string WhiteListTrim = Files.OpenRead("players/white-list.config").Trim(' ');
                string[] WhiteList = WhiteListTrim.Split(',');
                if (WhiteList.Length != 1 || WhiteList[0] != "")
                {
                    for (var i = 0; i < WhiteList.Length; i++)
                    {
                        if (player.Username == WhiteList[i]) { break; }
                        if (i == WhiteList.Length - 1) { player.Disconnect(Config.GetProperty("WhiteListMessage", "White-list")); Console.WriteLine("Player " + player.Username + " was kicked by white-list"); }
                    }
                }

                if (IsOp(player.Username))
                {
                    player.SetNameTag("§7[§4OWN§7]§f" + player.Username);
                    //player.NameTag = "§7[§4OWN§7]§f" + player.Username;
                    player.Permission = "OWNER";
                }
                else
                {

                    player.SetNameTag("§7[§8Player§7]§f" + player.Username);
                    //player.NameTag = "§7[§8Player§7]§f" + player.Username;
                }
                //player.SetNameTag();
                //player.SpawnToPlayers(Level.Players.Values.ToArray());
                Island island = new Island(player, e.Level);
                if (island.Configurations.Config != string.Empty)
                    Islands[player.Username] = island;
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_1"));
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.joinmessage") + " §l§bCristalixPE §2SkyBlock");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.line_1"));
                //if (Context.LevelManager.GetLevel(null, "Default").GetBlock(-199950, 99, -199950).Id == 0)
                //{
                //    e.Level.SetBlock(new Block(2) { Coordinates = new BlockCoordinates(-199950, 99, -199950) });
                //}
                NpcCore Villager = NpcEvents.GetNpc(DailyBonus.UniType);
                Villager.Text = DailyBonus.GetStatus(player);
                Villager.SendTextEntity();
                Villager.SendEntity();
                InvisibleType type = InvisibleType.None;
                if (!InvisibleMe.Type.TryGetValue(player.Username, out type)) InvisibleMe.Type.Add(player.Username, InvisibleType.Anyone);
                InvisibleMe.UpdateInvisibleToAllFromPlayer(player, Level.Players.Values.ToArray());
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        private void OnPlace(object sender, BlockPlaceEventArgs e)
        {
            if (!IsOp(e.Player.Username))
            {
                Island island = new Island(e.Level);
                if (Islands.TryGetValue(e.Player.Username, out island) && e.ExistingBlock.Coordinates.X < island.Zone.Seckond.X && e.ExistingBlock.Coordinates.X > island.Zone.First.X && e.ExistingBlock.Coordinates.Z < island.Zone.Seckond.Z && e.ExistingBlock.Coordinates.Z > island.Zone.First.Z)
                {
                    BlockEntity blockEntity = Level.GetBlockEntity(e.TargetBlock.Coordinates);
                    if (blockEntity != null)
                    {
                        if (blockEntity is ChestBlockEntity || blockEntity is FurnaceBlockEntity)
                        {
                            for (var i = 0; i <= 5; i++)
                            {
                                Sign sign = Level.GetBlockEntity(e.TargetBlock.Coordinates.GetNewCoordinatesFromFace((BlockFace)i)) as Sign;
                                if (sign != null)
                                {
                                    if (sign.Text1 == "[Private]" || sign.Text1 == "[private]" || sign.Text1 == "[PRIVATE]" || sign.Text1.Contains("[private]", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (island.Owner != e.Player.Username)
                                        {
                                            if (sign.Text2 != e.Player.Username && sign.Text3 != e.Player.Username && sign.Text3 != e.Player.Username)
                                            {
                                                e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.openinventory.failure"));
                                                e.Cancel = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //e.Cancel = false;
                }
                else if (e.ExistingBlock.Coordinates.X < -199975 && e.ExistingBlock.Coordinates.X > -199991 && e.ExistingBlock.Coordinates.Z < -199975 && e.ExistingBlock.Coordinates.Z > -199991 && e.ExistingBlock.Coordinates.Y < 100 && e.ExistingBlock.Coordinates.Y >= 80)
                {
                    e.Cancel = false;
                }
                else if (e.ExistingBlock.Coordinates.X < -199975 && e.ExistingBlock.Coordinates.X > -199991 && e.ExistingBlock.Coordinates.Z < -199950 && e.ExistingBlock.Coordinates.Z > -199966 && e.ExistingBlock.Coordinates.Y < 100 && e.ExistingBlock.Coordinates.Y >= 80)
                {
                    e.Cancel = false;
                }
                else
                {
                    //e.Player.SendPlayerInventory();
                    //var message = McpeUpdateBlock.CreateObject();
                    //message.blockId = e.Player.Level.GetBlock(e.ExistingBlock.Coordinates.X, e.ExistingBlock.Coordinates.Y, e.ExistingBlock.Coordinates.Z).Id;
                    //message.x = e.ExistingBlock.Coordinates.X;
                    //message.y = (byte)e.ExistingBlock.Coordinates.Y;
                    //message.z = e.ExistingBlock.Coordinates.Z;
                    //message.blockMetaAndPriority = (byte)(0xb << 4 | (e.Player.Level.GetBlock(e.ExistingBlock.Coordinates.X, e.ExistingBlock.Coordinates.Y, e.ExistingBlock.Coordinates.Z).Metadata & 0xf));
                    //e.Player.SendPackage(message);
                    //e.Player.SendMessage("§7[§4X§7]§fВы не можете ставить здесь блоки§4!");
                    e.Cancel = true;
                    //return;
                }
            }
        }

        private void OnBreak(object sender, BlockBreakEventArgs e)
        {
            if (!IsOp(e.Player.Username))
            {
                Island island = new Island(e.Level);
                if (Islands.TryGetValue(e.Player.Username, out island) && e.Block.Coordinates.X < island.Zone.Seckond.X && e.Block.Coordinates.X > island.Zone.First.X && e.Block.Coordinates.Z < island.Zone.Seckond.Z && e.Block.Coordinates.Z > island.Zone.First.Z)
                {
                    BlockEntity blockEntity = Level.GetBlockEntity(e.Block.Coordinates);
                    if (blockEntity != null)
                    {
                        if (blockEntity is ChestBlockEntity || blockEntity is FurnaceBlockEntity)
                        {
                            for (var i = 0; i <= 5; i++)
                            {
                                Sign sign = Level.GetBlockEntity(e.Block.Coordinates.GetNewCoordinatesFromFace((BlockFace)i)) as Sign;
                                if (sign != null)
                                {
                                    if (sign.Text1 == "[Private]" || sign.Text1 == "[private]" || sign.Text1 == "[PRIVATE]" || sign.Text1.Contains("[private]", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (island.Owner != e.Player.Username)
                                        {
                                            if (sign.Text2 != e.Player.Username && sign.Text3 != e.Player.Username && sign.Text3 != e.Player.Username)
                                            {
                                                e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.breakblock.failure"));
                                                e.Cancel = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if(blockEntity is Sign)
                        {
                            Sign sign = blockEntity as Sign;
                            if (sign.Text1 == "[Private]" || sign.Text1 == "[private]" || sign.Text1 == "[PRIVATE]" || sign.Text1.Contains("[private]", StringComparison.OrdinalIgnoreCase))
                            {
                                if (island.Owner != e.Player.Username)
                                {
                                    if (sign.Text2 != e.Player.Username && sign.Text3 != e.Player.Username && sign.Text3 != e.Player.Username)
                                    {
                                        e.Player.SendMessage(LangManager.getLang("eng").getString("skyblock.breakblock.sign.failure"));
                                        e.Cancel = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (e.Block.Coordinates.X < -199975 && e.Block.Coordinates.X > -199991 && e.Block.Coordinates.Z < -199975 && e.Block.Coordinates.Z > -199991 && e.Block.Coordinates.Y < 100 && e.Block.Coordinates.Y >= 80)
                {
                    e.Cancel = false;
                }
                else if (e.Block.Coordinates.X < -199975 && e.Block.Coordinates.X > -199991 && e.Block.Coordinates.Z < -199950 && e.Block.Coordinates.Z > -199966 && e.Block.Coordinates.Y < 100 && e.Block.Coordinates.Y >= 80)
                {
                    e.Cancel = false;
                }
                else
                {
                    //e.Player.SendMessage("§7[§4X§7]§fВы не можете ломать здесь блоки§4!");
                    e.Cancel = true;
                    //return;
                }
            }
        }

        [PacketHandler, Receive]
        public Package TextHundler(McpeText packet, Player player)
        {
            Island island = new Island(player.Level);
            if (Islands.TryGetValue(player.Username, out island))
            {
                if (island.Removing)
                {
                    if (packet.message == "yes")
                    {
                        island.Configurations.RemoveConfigFile(player);
                        SendAllMembersWithRemove(island);
                        Islands.Remove(player.Username);
                        IsBasic2p(player, "spawn");
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.remove.answer"));
                    }
                    else
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.remove.cancel"));
                    }
                    island.Removing = false;
                    return null;
                }
            }
            string text = packet.message;

            if (text == null) return null;

            if (text.StartsWith("/") || text.StartsWith("."))
            {
                Context.Server.PluginManager.HandleCommand(Context.Server.UserManager, text, player);
            }
            else
            {
                Level.BroadcastMessage(player.NameTag + ": " + text);
            }
            return null;
        }

        public void SendAllMembersWithRemove(Island island)
        {
            foreach (Player p in Level.Players.Values.ToArray())
            {
                foreach (string member in island.Members.ToArray())
                {
                    if (p.Username == member)
                    {
                        Islands.Remove(member);
                        IsBasic2p(p, "spawn");
                    }
                }
            }
        }

        [PacketHandler, Receive]
        public Package OpenHundler(McpeCraftingEvent packet, Player player)
        {
            //player.SendMessage("is crafting");
            return packet;
        }
        
        [PacketHandler, Send]//to respawn for anty helth!!!!!!
        public Package RespawnHundler(McpeRespawn packet, Player player)
        {
            int hunger;
            if (!HungerBuffer.TryGetValue(player.Username, out hunger)) HungerBuffer.Add(player.Username, 20);
            HungerBuffer[player.Username] = player.HungerManager.Hunger;
            return packet;
        }

        [PacketHandler, Receive]
        public Package InteractHundler(McpeInteract packet, Player player)
        {
            Entity target = player.Level.GetEntity(packet.targetEntityId);
            if (!IsOp(player.Username))
            {
                Player p = target as Player;
                if (p == null)
                {
                    Island island;
                    if (SkyBlock.Islands.TryGetValue(player.Username, out island) && target.KnownPosition.X < island.Zone.Seckond.X && target.KnownPosition.X > island.Zone.First.X && target.KnownPosition.Z < island.Zone.Seckond.Z && target.KnownPosition.Z > island.Zone.First.Z)
                    {
                    }
                    else
                    {
                        //return null;
                    }
                }
            }
            return packet;
        }

        [PacketHandler, Receive]
        public Package UseItemHundler(McpeUseItem packet, Player player)
        {
            //player.StrikeLightning();
            //player.SendMessage("light is striking");

            Sound s = new Sound(3003, packet.blockcoordinates);
            s.Spawn(player.Level);

            if (!IsOp(player.Username))
            {
                Island island = new Island(player.Level);
                if (Islands.TryGetValue(player.Username, out island) && packet.blockcoordinates.X < island.Zone.Seckond.X && packet.blockcoordinates.X > island.Zone.First.X && packet.blockcoordinates.Z < island.Zone.Seckond.Z && packet.blockcoordinates.Z > island.Zone.First.Z)
                {
                }
                else if (packet.blockcoordinates.X < -199975 && packet.blockcoordinates.X > -199991 && packet.blockcoordinates.Z < -199975 && packet.blockcoordinates.Z > -199990 && packet.blockcoordinates.Y < 100 && packet.blockcoordinates.Y >= 80)
                {
                }
                else if (packet.blockcoordinates.X < -199975 && packet.blockcoordinates.X > -199991 && packet.blockcoordinates.Z < -199950 && packet.blockcoordinates.Z > -199965 && packet.blockcoordinates.Y < 100 && packet.blockcoordinates.Y >= 80)
                {
                }
                else
                {
                    //player.SendPlayerInventory();
                    var message = McpeUpdateBlock.CreateObject();
                    message.blockId = player.Level.GetBlock(packet.blockcoordinates.X, packet.blockcoordinates.Y, packet.blockcoordinates.Z).Id;
                    message.coordinates = packet.blockcoordinates;
                    message.blockMetaAndPriority = (byte)(0xb << 4 | (player.Level.GetBlock(packet.blockcoordinates.X, packet.blockcoordinates.Y, packet.blockcoordinates.Z).Metadata & 0xf));
                    //player.Level.SetBlock(new Air() { Coordinates = packet.blockcoordinates.GetNewCoordinatesFromFace((BlockFace)packet.face) });
                    player.SendPackage(message);
                    //return null;
                }
                //ChunkColumn chunk = player.Level._worldProvider.GenerateChunkColumn(new ChunkCoordinates(packet.blockcoordinates.X >> 4, packet.blockcoordinates.Z >> 4));
                //chunk.isDirty = true;
            }
            return packet;
        }

        [Command(Command = "tell")]
        public void tellMessage(Player sender, string Username, params string[] Message)
        {
            foreach(Player player in sender.Level.Players.Values.ToArray())
            {
                if (player.Username.Contains(Username, StringComparison.OrdinalIgnoreCase))
                {
                    if (player.Username == sender.Username) continue;
                    string message = "";
                    for(var i = 0; i < Message.Length; i++)
                    {
                        message += (" " + Message[i]);
                    }
                    player.SendMessage("[Me <= " + sender.Username + "]" + message);
                    sender.SendMessage("[Me => " + player.Username + "]" + message);
                    return;
                }
            }
            sender.SendMessage(LangManager.getLang("eng").getString("skyblock.player.offline"));
        }
        
        [Command(Command = "w")]
        public void writePrivateMessage(Player sender, string Username, params string[] Message)
        {
            tellMessage(sender, Username, Message);
        }

        [Command(Command = "invisible")]
        public void invisible(Player player)
        {
            //if (InvisibleMe.Type[player.Username] == InvisibleType.AdditionMembers) return;
            InvisibleMe.TakeInvisible(player, Level.Players.Values.ToArray());
            player.SendMessage(LangManager.getLang("eng").getString("skyblock.invisibleme.allis_invisible"));
        }

        [Command(Command = "visible")]
        public void visible(Player player)
        {
            //if (InvisibleMe.Type[player.Username] == InvisibleType.Anyone) return;
            InvisibleMe.TakeInvisible(player, Level.Players.Values.ToArray(), InvisibleType.Anyone);
            player.SendMessage(LangManager.getLang("eng").getString("skyblock.invisibleme.allis_visible"));
        }

        [Command(Command = "invisible")]
        public void invisible(Player player, string invType)
        {
            InvisibleType type = invType.ToInvisibleType();
            if (type == InvisibleMe.Type[player.Username]) return;
            InvisibleMe.TakeInvisible(player, Level.Players.Values.ToArray(), type);
            player.SendMessage(LangManager.getLang("eng").getString("skyblock.invisibleme.settings_changed"));
        }

        [Command(Command = "save")]
        public void save(Player player)
        {
            if (IsOp(player.Username))
                Context.LevelManager.Levels[0]._worldProvider.SaveChunks();
        }

        [Command(Command = "gamemode")]
        public void gm(Player player, int gameMode)
        {
            if (IsOp(player.Username))
                player.SetGameMode((GameMode)gameMode);
        }

        [Command(Command = "op")]
        public void op(Player player, string Username)
        {
            if (!IsOp(player.Username)) return;
            foreach (Player p in Level.Players.Values.ToArray())
                if (p.Username.Contains(Username, StringComparison.OrdinalIgnoreCase))
                {
                    var l = ops.ToList();
                    l.Add(p.Username);
                    ops = l.ToArray();
                }

        }

        [Command(Command = "tp")]
        public void tp(Player player, string x, string y, string z)
        {
            if (IsOp(player.Username))
            {
                try
                {
                    float X, Y, Z;
                    if (x.Contains("~")) if (x.Length == 1) X = player.KnownPosition.X; else X = player.KnownPosition.X + Convert.ToInt64(x.Trim('~')); else X = (float)Convert.ToDouble(x);
                    if (X == 0) return;
                    if (y.Contains("~")) if (y.Length == 1) Y = player.KnownPosition.Y; else Y = player.KnownPosition.Y + Convert.ToInt64(y.Trim('~')); else Y = (float)Convert.ToDouble(y);
                    if (Y == 0) return;
                    if (z.Contains("~")) if (z.Length == 1) Z = player.KnownPosition.Z; else Z = player.KnownPosition.Z + Convert.ToInt64(z.Trim('~')); else Z = (float)Convert.ToDouble(z);
                    if (Z == 0) return;
                    player.Teleport(new PlayerLocation(X, Y, Z));
                }
                catch// (Exception e)
                {
                    //Log.Error(e);
                    player.SendMessage("§cError with coordinates!");
                }
            }
        }

        [Command(Command = "tp")]
        public void tp(Player player, string Username, string x, string y, string z)
        {
            try
            {
                if (IsOp(player.Username))
                {
                    foreach (Player p in Level.Players.Values.ToArray())
                        if (p.Username.Contains(Username, StringComparison.OrdinalIgnoreCase))
                            tp(p, x, y, z);
                }
            }
            catch// (Exception e)
            {
                //Log.Error(e);
                player.SendMessage("§cError with coordinates!");
            }
        }

        [Command(Command = "tp")]
        public void tp(Player player, string username)
        {
            if (IsOp(player.Username))
            {
                Player playerTo = null;
                foreach (Player p in Level.Players.Values.ToArray())
                    if (p.Username.Contains(username, StringComparison.OrdinalIgnoreCase) && playerTo == null)
                        playerTo = p;
                if (playerTo == null) return;
                player.Teleport(playerTo.KnownPosition);
            }
        }

        [Command(Command = "tp")]
        public void tp(Player player, string username1, string username2)
        {
            if (IsOp(player.Username))
            {
                Player playerAt = null;
                Player playerTo = null;
                foreach (Player p in Level.Players.Values.ToArray())
                {
                    if (p.Username.Contains(username1, StringComparison.OrdinalIgnoreCase) && playerAt == null)
                        playerAt = p;
                    if (p.Username.Contains(username2, StringComparison.OrdinalIgnoreCase) && playerTo == null)
                        playerTo = p;
                }
                if (playerTo == null) return;
                if (playerAt == null) return;
                playerAt.Teleport(playerTo.KnownPosition);
            }
        }

        [Command(Command = "setblock")]
        public void setblock(Player player, int x, int y, int z, byte id)
        {
            if (IsOp(player.Username))
                player.Level.SetBlock(new Block(id) { Coordinates = new BlockCoordinates(x, y, z) });
        }

        [Command(Command = "is")]
        public void IsBasic1p(Player player)
        {
            player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
        }

        [Command(Command = "is")]
        public void IsBasic2p(Player player, string par1)
        {
            if (par1 == "create")
            {
                Island island = new Island(player.Level);
                string islandName = player.Username + "Island";
                //Don't use this!
                //for (var i = 0; i < 10; i++)
                //{
                //    if (!island.HaveIsland(islandName)) break;
                //    islandName += "_";
                //}
                if (player.Permission != "OWNER")
                {
                    foreach (string username in AlreadyCreate)
                        if (username == player.Username)
                        {
                            player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.create.alreadycreate"));
                            return;
                        }
                }
                IslandType type = player.GetIslandTypeByPermission();
                if (Island.HavePurchasedIsland(player)) type = IslandType.PurchasedIsland;
                if (player.TrySendReason(island.CreateIsland(player, islandName, IsMenager, type))) return;
                Islands[player.Username] = island;
                IsMenager++;
                Files.Create("SkyBlock/IsMenager.config", IsMenager.ToString());
                AlreadyCreate.Add(player.Username);
                IsBasic2p(player, "home");
            }
            else if(par1 == "home")
            {
                Island island = new Island(player.Level);
                if (!Islands.TryGetValue(player.Username, out island))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                    return;
                }
                if (player.Level.GetBlock(island.RespawnPosition).IsReplacible || !player.Level.GetBlock(island.RespawnPosition).IsSolid)
                    player.Level.SetBlock(new Cobblestone() { Coordinates = island.RespawnPosition });
                player.SpawnPosition = new PlayerLocation((island.RespawnPosition + new BlockCoordinates(0, 1, 0)));
                player.Teleport(player.SpawnPosition);
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.tphome"));
            }
            else if(par1 == "list")
            {
                Island island = new Island(player.Level);
                if (!Islands.TryGetValue(player.Username, out island))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                    return;
                }
                player.SendMessage(" ");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.islandtype") + " " + island.Type);
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.island") + island.Name);
                string owner = island.Owner;
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.owner") + owner);
                player.SendMessage(" ");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.members") + Files.OpenRead("SkyBlock/" + owner + "/Members.config"));
            }
            else if(par1 == "spawn")
            {
                Island island;
                if (!Islands.TryGetValue(player.Username, out island))
                    player.SpawnPosition = Level.SpawnPoint;
                player.Teleport(Level.SpawnPoint);
            }
            else if(par1 == "out")
            {
                Island island = new Island(player.Level);
                if (!Islands.TryGetValue(player.Username, out island))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                    return;
                }
                island.RemoveMember(new Player(null, null) { Username = island.Owner }, player);
                Islands.Remove(player.Username);
                InvisibleMe.UpdateInvisibleToAllFromPlayer(player, Level.Players.Values.ToArray());
                IsBasic2p(player, "spawn");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.left"));
            }
            else if(par1 == "remove")
            {
                Island island = new Island(player.Level);
                if (!Islands.TryGetValue(player.Username, out island))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                    return;
                }
                if(player.TrySendReason(island.RemoveIsland(player))) return;
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.remove.answermessage"));
            }
            else if(par1 == "money")
            {
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.youmoney") + player.Money() + "§bSB");
            }
            else if(par1 == "resethome")
            {
                Island island = new Island(player.Level);
                if (!Islands.TryGetValue(player.Username, out island))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                    return;
                }
                BlockCoordinates respawnPosition = new BlockCoordinates(player.KnownPosition) - new BlockCoordinates(0, 1, 0);
                if (respawnPosition.X < island.Zone.Seckond.X && respawnPosition.X > island.Zone.First.X && respawnPosition.Z < island.Zone.Seckond.Z && respawnPosition.Z > island.Zone.First.Z) { }
                else { player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.position.notstandingonyouisland")); return; }
                if (player.TrySendReason(island.SetRespawnPosition(player, respawnPosition))) return;
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.resethome"));
            }
            else if(par1 == "help")
            {
                IsBasic3p(player, par1, "1");
            }
            else if (par1 == "member")
            {
                player.SendMessage("/is member <add|remove> <username>");
            }
            else
            {
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
            }
        }

        [Command(Command = "is")]
        public void IsBasic3p(Player player, string par1, string par2)
        {
            if (par1 == "create")
            {
                Island island = new Island(player.Level);
                //Don't use this!
                //if (island.HaveIsland(par2))
                //{
                //    player.SendMessage("this name is already taken");
                //    return;
                //}
                if (player.Permission != "OWNER")
                {
                    foreach (string username in AlreadyCreate)
                        if (username == player.Username)
                        {
                            player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.create.alreadycreate"));
                            return;
                        }
                }
                IslandType type = player.GetIslandTypeByPermission();
                if (Island.HavePurchasedIsland(player)) type = IslandType.PurchasedIsland;
                if (player.TrySendReason(island.CreateIsland(player, par2, IsMenager, type))) return;
                Islands[player.Username] = island;
                IsMenager++;
                Files.Create("SkyBlock/IsMenager.config", IsMenager.ToString());
                AlreadyCreate.Add(player.Username);
                IsBasic2p(player, "home");
            }
            else if (par1 == "help")
            {
                int page = Convert.ToInt32(par2);
                List<string> _HelpStrings = new List<string>();
                foreach (var HelpStringValue in HelpStrings)
                {
                    string Permission = HelpStringValue.Key.FirstOrDefault(perm => player.Permission.Equals(perm, StringComparison.InvariantCultureIgnoreCase));
                    if (player.Permission.Equals(Permission, StringComparison.InvariantCultureIgnoreCase) || HelpStringValue.Key[0] == "ALL")
                    {
                        _HelpStrings.Add(HelpStringValue.Value);
                    }
                }
                int result;
                int maxPage = Math.DivRem(_HelpStrings.Count, 5, out result);
                if (result > 0) maxPage++;
                if (page > maxPage || page <= 0)
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.help.info.pagenotfound"));
                    return;
                }

                int maxPageValue = (((page - 1) * 5) + 5);
                if (page == maxPage) maxPageValue = _HelpStrings.Count;

                player.SendMessage(" ");
                player.SendMessage("Help page §7 " + page + " : " + maxPage);
                player.SendMessage(" ");

                for (var i = (5 * (page - 1)); i < maxPageValue; i++)
                    player.SendMessage(_HelpStrings[i]);
            }
            else if (par1 == "money")
            {
                if (par2 == "help")
                {
                    player.SendMessage("[§aSB§f]Money help page");
                    player.SendMessage(" ");
                    string permission = player.Permission;
                    foreach (var HelpValue in MoneyHelpStrings)
                    {
                        if (HelpValue[0] == player.Permission || HelpValue[0] == "ALL")
                            player.SendMessage(HelpValue[1]);
                    }
                }
                else
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.money.error.undefined"));
                }
            }
            else if (par1 == "minereset")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }
                MineManager.MineReset(par2, Level, player);
            }
            else if (par1 == "watch")
            {
                string IslandStringName = string.Empty;
                if ("default".Contains(par2, StringComparison.OrdinalIgnoreCase) || par2 == "1")
                    IslandStringName = "IslandDefault.(822364)";
                if ("vip".Contains(par2, StringComparison.OrdinalIgnoreCase) || par2 == "2")
                    IslandStringName = "IslandVip.(933740)";
                if ("premium".Contains(par2, StringComparison.OrdinalIgnoreCase) || par2 == "3")
                    IslandStringName = "IslandPremium.(187947)";
                if ("deluxe".Contains(par2, StringComparison.OrdinalIgnoreCase) || par2 == "4")
                    IslandStringName = "IslandDeluxe.(734027)";
                if ("purchased".Contains(par2, StringComparison.OrdinalIgnoreCase) || par2 == "5")
                    IslandStringName = "IslandPurchased.(620576)";
                if (string.IsNullOrEmpty(IslandStringName))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.watch.undefined"));
                    return;
                }
                Island island = new Island(new Player(null, null) { Username = IslandStringName }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined"));
                    return;
                }
                player.Teleport(new PlayerLocation(island.RespawnPosition + new BlockCoordinates(0, 1, 0)));
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.tphome"));
            }
            else if (par1 == "remove")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }
                Island island = new Island(new Player(null, null) { Username = par2 }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined") + " " + island.Name);
                    return;
                }
                foreach (Player p in Level.Players.Values.ToArray())
                    if (p.Username == par2)
                        IsBasic2p(p, "spawn");
                Islands.Remove(par2);
                if (par2 != island.Owner)
                {
                    player.TrySendReason(island.RemoveMember(new Player(null, null) { Username = island.Owner }, new Player(null, null) { Username = par2 }));
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.opout"), par2, island.Owner));
                    return;
                }
                SendAllMembersWithRemove(island);
                player.TrySendReason(island.RemoveIsland(new Player(null, null) { Username = island.Owner }));
                player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.opremove"), island.Name, island.Owner));
            }
            else if(par1 == "resethome")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER" && player.Permission != "GMOD")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }
                Island island = new Island(new Player(null, null) { Username = par2 }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined"));
                    return;
                }
                Island Out;
                if (Islands.TryGetValue(par2, out Out))
                    Islands[par2] = island;

                player.TrySendReason(island.SetRespawnPosition(new Player(null, null) { Username = island.Owner }, new BlockCoordinates(player.KnownPosition) - new BlockCoordinates(0, 1, 0)));
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.resethome"));
            }
            else if(par1 == "toisland")
            {
                if(player.Permission != "ADMIN" && player.Permission != "OWNER" && player.Permission != "GMOD" && player.Permission != "MOD")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }

                Island island = new Island(new Player(null, null) { Username = par2 }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined"));
                    return;
                }

                player.Teleport((island.Zone.Seckond / 2).ToPlayerLocation(76));
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.tphometo") + island.Owner);
            }
            else if(par1 == "home")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER" && player.Permission != "GMOD" && player.Permission != "MOD")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }

                Island island = new Island(new Player(null, null) { Username = par2 }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined"));
                    return;
                }

                player.Teleport(new PlayerLocation(island.RespawnPosition + new BlockCoordinates(0, 1, 0)));
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.tphometo") + island.Owner);
            }
            else if(par1 == "list")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER" && player.Permission != "GMOD" && player.Permission != "MOD")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }

                Island island = new Island(new Player(null, null) { Username = par2 }, Level);
                if (string.IsNullOrEmpty(island.Configurations.Config))
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.undefined"));
                    return;
                }

                player.SendMessage(" ");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.islandtype") + " " + island.Type);
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.island") + island.Name);
                string owner = island.Owner;
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.owner") + owner);
                player.SendMessage(" ");
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.members") + Files.OpenRead("SkyBlock/" + owner + "/Members.config"));
            }
            else if (par1 == "member")
            {
                player.SendMessage("/is member <add|remove> <username>");
            }
            else
            {
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
            }
        }

        [Command(Command = "is")]
        public void IsBasic4p(Player player, string par1, string par2, string par3)
        {
            if (par1 == "create")
            {
                Island island = new Island(player.Level);
                //Don't use this!
                //if (island.HaveIsland(par2))
                //{
                //    player.SendMessage("this name is already taken");
                //    return;
                //}
                if (player.Permission != "OWNER")
                {
                    foreach (string username in AlreadyCreate)
                        if (username == player.Username)
                        {
                            player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.create.alreadycreate"));
                            return;
                        }
                }
                IslandType type = par3.ToIslandType();
                if(type == IslandType.PurchasedIsland)
                {
                    int price = 1000000;
                    if (player.Money() < price)
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.buyisland.money.havnt"));
                        return;
                    }
                    player.MoneyReduce(price);
                    Files.Create("SkyBlock/" + player.Username + "/PurchasedIsland.key", null);
                }
                if(player.TrySendReason(island.CreateIsland(player, par2, IsMenager, type))) return;
                Islands[player.Username] = island;
                IsMenager++;
                Files.Create("SkyBlock/IsMenager.config", IsMenager.ToString());
                AlreadyCreate.Add(player.Username);
                IsBasic2p(player, "home");
            }
            else if(par1 == "member")
            {
                if(par2 == "add")
                {
                    Island island = new Island(player.Level);
                    if (!Islands.TryGetValue(player.Username, out island))
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                        return;
                    }
                    if(player.TrySendReason(island.AddMember(player, new Player(null, null) { Username = par3 }))) return;
                    foreach (Player p in player.Level.Players.Values.ToArray())
                    {
                        if (p.Username == par3)
                        {
                            Island isl;
                            if(!Islands.TryGetValue(p.Username, out isl)) Islands.Add(p.Username, new Island(player.Level));
                            Islands[p.Username] = new Island(p, player.Level);
                            InvisibleMe.UpdateInvisibleToAllFromPlayer(p, Level.Players.Values.ToArray());
                            IsBasic2p(p, "home");
                            return;
                        }
                    }
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.members.add"), par3));
                }
                else if(par2 == "remove")
                {
                    Island island = new Island(player.Level);
                    if (!Islands.TryGetValue(player.Username, out island))
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.havent_1"));
                        return;
                    }
                    if(player.TrySendReason(island.RemoveMember(player, new Player(null, null) { Username = par3 }))) return;
                    foreach (Player p in player.Level.Players.Values.ToArray())
                    {
                        if (p.Username == par3)
                        {
                            Islands.Remove(par3);
                            InvisibleMe.UpdateInvisibleToAllFromPlayer(p, Level.Players.Values.ToArray());
                            IsBasic2p(p, "spawn");
                            return;
                        }
                    }
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.members.remove"), par3));
                }
                else
                {
                    player.SendMessage("/is member <add|remove> <username>");
                }
            }
            else if(par1 == "buy")
            {
                if (player.KnownPosition.X < -199800 && player.KnownPosition.X > -200100 && player.KnownPosition.Z < -199800 && player.KnownPosition.Z > -200100)
                {
                    string itemString = par2.Replace(':', ',');
                    Item item = itemString.ToItem();
                    item.Count = Convert.ToByte(par3);
                    if(player.TrySendReason(player.TryBuyItem(item))) return;
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.words.moneyplayer"), par3, par2));
                }
            }
            else if(par1 == "sell")
            {
                string itemString = par2.Replace(':', ',');
                Item item = itemString.ToItem();
                item.Count = Convert.ToByte(par3);
                if(player.TrySendReason(player.TrySellItem(item))) return;
                player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.words.moneyplayer"), par3, par2));
            }
            else if(par1 == "money")
            {
                if (par2 == "view")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.moneyplayer") + par3 + "§f: §c" + SB.Money(new Player(null, null) { Username = par3 }) + "§bSB");
                }
            }
            else
            {
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
            }
        }

        [Command(Command = "is")]
        public void IsBasic5p(Player player, string par1, string par2, string par3, string par4)
        {
            if (par1 == "money")
            {
                if (par2 == "pay")
                {
                    if (player.TrySendReason(player.TryPayMoney(new Player(null, null) { Username = par3 }, Convert.ToInt32(par4)))) return;
                    Player recipient = player.Level.Players.Values.FirstOrDefault(recipientVal => recipientVal.Username == par3);
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.island.money.istransferred"));
                    if (recipient.Username == par3)
                    {
                        recipient.SendMessage(LangManager.getLang("eng").getString("skyblock.island.words.gavemoney") + " " + par4 + "§bSB");
                        return;
                    }
                }
                else if (par2 == "add")
                {
                    if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                        return;
                    }
                    SB.MoneyAdd(new Player(null, null) { Username = par3 }, Convert.ToInt32(par4));
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.money.add"));
                }
                else if (par2 == "set")
                {
                    if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                        return;
                    }
                    SB.MoneySet(new Player(null, null) { Username = par3 }, Convert.ToInt32(par4));
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.money.set"));
                }
                else if (par2 == "reduce")
                {
                    if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                    {
                        player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                        return;
                    }
                    SB.MoneyReduce(new Player(null, null) { Username = par3 }, Convert.ToInt32(par4));
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.money.reduce"));
                }
            }
            else if(par1 == "member")
            {
                if (player.Permission != "ADMIN" && player.Permission != "OWNER")
                {
                    player.SendMessage(LangManager.getLang("eng").getString("skyblock.havenotpermissions"));
                    return;
                }
                if (par2 == "add")
                {
                    Player own = new Player(null, null) { Username = par4 };
                    Island island = new Island(own, player.Level);
                    if (player.TrySendReason(island.AddMember(own, new Player(null, null) { Username = par3 }))) return;
                    foreach (Player p in player.Level.Players.Values.ToArray())
                    {
                        if (p.Username == par3)
                        {
                            Island isl;
                            if (Islands.TryGetValue(p.Username, out isl))// Islands.Add(p.Username, new Island(player.Level));
                                Islands[p.Username] = island;
                            InvisibleMe.UpdateInvisibleToAllFromPlayer(p, Level.Players.Values.ToArray());
                            IsBasic2p(p, "home");
                            return;
                        }
                    }
                    Island Out;
                    if (Islands.TryGetValue(par4, out Out))
                        Islands[par4] = island;
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.members.opadd"), par3, island.Owner));
                }
                else if(par2 == "remove")
                {
                    Player own = new Player(null, null) { Username = par4 };
                    Island island = new Island(own, player.Level);
                    if (player.TrySendReason(island.RemoveMember(own, new Player(null, null) { Username = par3 }))) return;
                    
                    foreach (Player p in player.Level.Players.Values.ToArray())
                    {
                        if (p.Username == par3)
                        {
                            Islands.Remove(par3);
                            InvisibleMe.UpdateInvisibleToAllFromPlayer(p, Level.Players.Values.ToArray());
                            IsBasic2p(p, "spawn");
                            return;
                        }
                    }
                    Island Out;
                    if (Islands.TryGetValue(par4, out Out))
                        Islands[par4] = island;
                    player.SendMessage(string.Format(LangManager.getLang("eng").getString("skyblock.island.members.opremove"), par3, island.Owner));
                }
            }
            else
            {
                player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
            }
        }

        [Command(Command = "is")]
        public void IsBasic6p(Player player, string par1, string par2, string par3, string par4, string par5)
        {
            player.SendMessage(LangManager.getLang("eng").getString("skyblock.commands.error.undefined"));
        }
    }
}