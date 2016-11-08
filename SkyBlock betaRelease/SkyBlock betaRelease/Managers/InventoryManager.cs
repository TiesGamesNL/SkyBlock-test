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
    public class InventoryManager
    {
        public bool IsJoin { get; set; } = false;

        public int Health { get; set; } = 200;
        public int Hunger { get; set; } = 20;

        public Level Level { get; set; }

        public InventoryManager(Level world)
        {
            Level = world;
        }

        public void SaveInventory(Player player)
        {
            if (IsJoin)
            {
                player.SendPlayerInventory();
                int Gamemode = 0;
                switch (Convert.ToString(player.GameMode))
                {
                    case "Survival":
                        Gamemode = 0;
                        break;
                    case "Creative":
                        Gamemode = 1;
                        break;
                    case "Adventure":
                        Gamemode = 2;
                        break;
                    case "Spectator":
                        Gamemode = 3;
                        break;
                }

                string opsions = (player.HealthManager.Health) + "," + player.HungerManager.Hunger + "," + Gamemode;
                string Inv = player.Inventory.GetSlots()[0].Id + "," + player.Inventory.GetSlots()[0].Metadata + "," + player.Inventory.GetSlots()[0].Count;
                for (var i = 1; i < player.Inventory.Slots.Count; i++)
                {
                    Inv = Inv + "|" + player.Inventory.GetSlots()[i].Id + "," + player.Inventory.GetSlots()[i].Metadata + "," + player.Inventory.GetSlots()[i].Count;
                }
                string Arm = player.Inventory.Boots.Id + "," + player.Inventory.Boots.Metadata + "|" + player.Inventory.Leggings.Id + "," + player.Inventory.Leggings.Metadata + "|" + player.Inventory.Chest.Id + "," + player.Inventory.Chest.Metadata + "|" + player.Inventory.Helmet.Id + "," + player.Inventory.Helmet.Metadata;
                if (File.Exists("players/" + player.Username + "/player.conf"))
                    File.Delete("players/" + player.Username + "/player.conf");
                for (var i = 1; i < player.Inventory.Slots.Count; i++) { player.Inventory.Slots[i] = new ItemAir(); }
                player.SendPlayerInventory();
                Files.OpenWrite("players/" + player.Username + "/player.conf", opsions + "&" + Inv + "&" + Arm);
            }
        }

        public void OpenInventory(Player player)
        {
            if (File.Exists("players/" + player.Username + "/player.conf"))
            {
                //player.HungerManager.ResetHunger();
                string[] Player = Files.OpenRead("players/" + player.Username + "/player.conf").Split('&');
                player.SetGameMode((GameMode)Convert.ToInt32(Player[0].Split(',')[2]));
                Health = Convert.ToInt32(Player[0].Split(',')[0]);
                player.HealthManager.Health = Health;
                Hunger = Convert.ToInt32(Player[0].Split(',')[1]);
                player.HungerManager.Hunger = Hunger;
                string[] PlayerInv = Player[1].Split('|');
                Item item;
                for (var i = 0; i < player.Inventory.Slots.Count; i++)
                {
                    item = ItemFactory.GetItem(Convert.ToInt16(PlayerInv[i].Split(',')[0]), Convert.ToInt16(PlayerInv[i].Split(',')[1]), Convert.ToByte(PlayerInv[i].Split(',')[2]));
                    if (item.Count != 0 && item.Id != 0)
                        player.Inventory.Slots[i] = item;
                }
                string[] PlayerArm = Player[2].Split('|');
                player.Inventory.Boots = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[0].Split(',')[0]), Convert.ToInt16(PlayerArm[0].Split(',')[1]));
                player.Inventory.Leggings = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[1].Split(',')[0]), Convert.ToInt16(PlayerArm[1].Split(',')[1]));
                player.Inventory.Chest = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[2].Split(',')[0]), Convert.ToInt16(PlayerArm[2].Split(',')[1]));
                player.Inventory.Helmet = ItemFactory.GetItem(Convert.ToInt16(PlayerArm[3].Split(',')[0]), Convert.ToInt16(PlayerArm[3].Split(',')[1]));
                player.SendPlayerInventory();
            }
        }
    }
}
