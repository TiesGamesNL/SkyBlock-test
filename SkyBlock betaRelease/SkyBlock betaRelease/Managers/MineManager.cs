using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using fNbt;
using log4net;
using LibNoise;
using LibNoise.Primitive;
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
using MiNET.Items;
using SkyBlock.Utils;
using SkyBlock.Islands;
using FileSystem;

namespace SkyBlock.Managers
{
    public class MineManager
    {
        //public Level Level { get; set; }

        public static Random rnd { get; private set; } = new Random();

        static public int pvpTimeReset { get; set; } = 10;
        static public int pveTimeReset { get; set; } = 10;

        //public MineManager(Level level)
        //{
        //    Level = level;
        //}

        static public void SendMinesTime(Level Level)
        {
            Player[] players = Level.Players.Values.ToArray();
            if (players.Length >= 1)//изменить на 3
            {
                pveTimeReset--;
            }
            if (players.Length >= 1)//изменить на 10
            {
                pvpTimeReset--;
            }
            if (pveTimeReset <= 0)
            {
                pveTimeReset = 300;
                MineReset("pve", Level);
            }
            if (pvpTimeReset <= 0)
            {
                pvpTimeReset = 600;
                MineReset("pvp", Level);
            }
        }

        static public void MineReset(string type, Level Level, Player sender = null)
        {
            if (type == "pvp")
            {
                for (var x = -199990; x < -199975; x++)
                {
                    for (var y = 80; y < 100; y++)
                    {
                        for (var z = -199965; z < -199950; z++)
                        {
                            BlockCoordinates coordinates = new BlockCoordinates(x, y, z);
                            Block block = new Block(1);
                            int random = 8;
                            for (var i = 0; i < rnd.Next(0, 60); i++) { random = rnd.Next(0, 200); }
                            if (random == 13)
                            { block = new Block(14);/*золото*/}
                            else if (random == 0)
                            { block = new Block(56);/*алмазы*/}
                            else if (random == 14 || random == 1)
                            { block = new Block(21);/*лазурит*/}
                            else if (random == 2 || random == 12)
                            { block = new Block(73);/*редстоун*/}
                            else if (random == 4 || random == 5 || random == 6 || random == 7 || random == 3 || random == 9 || random == 10 || random == 11 || random >= 110 || random >= 112 || random >= 113 || random >= 114)
                            { block = new Block(3);/*земля*/}
                            //else
                            //{ block = new Block(1);/*камень*/}
                            block.Coordinates = coordinates;
                            Level.SetBlock(block, true, false);
                        }
                    }
                }
                var players = Level.Players.Values.ToArray();
                foreach (var player in players)
                {
                    if (player.KnownPosition.X < -199975 && player.KnownPosition.X > -199990 && player.KnownPosition.Z < -199950 && player.KnownPosition.Z > -199965 && player.KnownPosition.Y < 100 && player.KnownPosition.Y >= 80)
                    {
                        player.Teleport(new PlayerLocation
                        {
                            X = -199980,
                            Y = 101,
                            Z = -199960
                        });
                        player.SendMessage("§7[§4PVP§7]§f" + SkyBlock.LangManager.getLang("eng").getString("skyblock.mine.wasrestored"));
                    }
                    if (sender != null)
                        if (player.Username == sender.Username)
                            sender.SendMessage("§7[§4PVP§7]§f" + SkyBlock.LangManager.getLang("eng").getString("skyblock.mine.wasrestored"));
                }
            }
            else if (type == "pve")
            {
                for (var x = -199990; x < -199975; x++)
                {
                    for (var y = 80; y < 100; y++)
                    {
                        for (var z = -199990; z < -199975; z++)
                        {
                            BlockCoordinates coordinates = new BlockCoordinates(x, y, z);
                            Block block = new Block(1) { Coordinates = coordinates };
                            int random = 9;
                            for (var i = 0; i < rnd.Next(0, 60); i++) { random = rnd.Next(0, 110); }
                            if (random == 0)
                            { block = new Block(15);/*железо*/}
                            else if (random == 1)
                            { block = new Block(16);/*уголь*/}
                            else if (random == 2 || random == 3 || random == 10)
                            { block = new Block(13);/*гравий*/}
                            else if (random == 4 || random == 5)
                            { block = new Block(12);/*песок*/}
                            else if (random == 6 || random == 7 || random == 8 || random >= 85)
                            { block = new Block(3);/*земля*/}
                            block.Coordinates = coordinates;
                            Level.SetBlock(block, true, false);
                            //else
                            //{ block = new Block(1);/*камень*/}
                        }
                    }
                }
                var players = Level.Players.Values.ToArray();
                foreach (var player in players)
                {
                    if (player.KnownPosition.X < -199975 && player.KnownPosition.X > -199990 && player.KnownPosition.Z < -199975 && player.KnownPosition.Z > -199990 && player.KnownPosition.Y < 100 && player.KnownPosition.Y >= 80)
                    {
                        //ThreadPool.QueueUserWorkItem(delegate (object state)
                        //{
                        player.Teleport(new PlayerLocation
                        {
                            X = -199980,
                            Y = 101,
                            Z = -199980
                        });
                        //}, null);
                        player.SendMessage("§7[§2PVE§7]§f" + SkyBlock.LangManager.getLang("eng").getString("skyblock.mine.wasrestored"));
                    }
                    if (sender != null)
                        if (player.Username == sender.Username)
                            player.SendMessage("§7[§2PVE§7]§f" + SkyBlock.LangManager.getLang("eng").getString("skyblock.mine.wasrestored"));
                }
            }
            else if (sender != null) { sender.SendMessage(SkyBlock.LangManager.getLang("eng").getString("skyblock.mine.notfound")); }
        }
    }
}
