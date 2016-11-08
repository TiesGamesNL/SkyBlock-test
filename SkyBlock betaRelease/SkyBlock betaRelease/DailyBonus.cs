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
using System.Numerics;
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
using MiNET.Entities.Hostile;
using MiNET.Entities.Passive;
using MiNET.Items;
using SkyBlock.Utils;
using SkyBlock.Islands;
using SkyBlock.Managers;
using SkyBlock.INpcCore;
using FileSystem;

namespace SkyBlock
{
    public static class DailyBonus
    {
        public static IDictionary<string, BonusStatus> DailyBonusClaimed = new Dictionary<string, BonusStatus>();
        public static string UniType { get; } = "deily_bonus_villager_npc";
        
        public static Level Level { get; set; }

        public static void Initialize(Level level)
        {
            Level = level;
        }

        public static string GetTimeToClaim(Player player)
        {
            string Out;
            if (!File.Exists("SkyBlock/" + player.Username + "/DailyBonus.config")) return string.Empty;
            DateTime lastUpdate = File.GetLastWriteTimeUtc("SkyBlock/" + player.Username + "/DailyBonus.config");

            string file = Files.OpenRead("SkyBlock/" + player.Username + "/DailyBonus.config");

            int daysClaimed = Convert.ToInt32(file);

            int lastDay = lastUpdate.Day;
            DateTime DayNow = DateTime.UtcNow;

            TimeSpan ratio1 = DayNow - lastUpdate;
            TimeSpan ratio = new TimeSpan(24, 0, 0) - ratio1;
            Out = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.until_bonus.timeformat"), ratio.Hours, ratio.Minutes, ratio.Seconds);
            return Out;
        }

        public static string GetStatus(Player player)
        {
            NpcCore Villager = NpcEvents.GetNpc(UniType);
            if (Villager == null)
                return string.Empty;
            BonusStatus status;
            bool isClaimed = false;
            //if (!DailyBonusClaimed.TryGetValue(player.Username, out status)) DailyBonusClaimed.Add(player.Username, GetStatusFromFile(player));
            //SetStatusInFile(player);
            DailyBonusClaimed[player.Username] = GetStatusFromFile(player);
            status = DailyBonusClaimed[player.Username];
            isClaimed = status.Status;
            
            Villager.Player = player;
            string text = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.npc_txt_1"));
            if (isClaimed)
                text = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.npc_txt_2"));
            return text;
        }

        public static void AddClaimedDay(Player player)
        {
            DailyBonusClaimed[player.Username].Status = true;
            DailyBonusClaimed[player.Username].DaysClaimed++;
            SetStatusInFile(player);
        }

        public static BonusStatus GetStatusFromFile(Player player)
        {
            if (!File.Exists("SkyBlock/" + player.Username + "/DailyBonus.config")) return new BonusStatus(false, 0);
            DateTime lastUpdate = File.GetLastWriteTimeUtc("SkyBlock/" + player.Username + "/DailyBonus.config");

            string file = Files.OpenRead("SkyBlock/" + player.Username + "/DailyBonus.config");

            int daysClaimed = Convert.ToInt32(file);

            int lastDay = lastUpdate.Day;
            DateTime DayNow = DateTime.UtcNow;
            
            TimeSpan ratio = DayNow - lastUpdate;
            if (ratio.Days <= 0)
            {
                return new BonusStatus(true, daysClaimed);
            }
            if (ratio.Days > 1)
                return new BonusStatus(false, 0);
            if(daysClaimed >= 7)
                return new BonusStatus(false, 0);
            return new BonusStatus(false, daysClaimed);
        }

        public static void UpdateDataAndText()
        {

        }

        public static void SetStatusInFile(Player player)
        {
            BonusStatus status;
            if (!DailyBonusClaimed.TryGetValue(player.Username, out status)) DailyBonusClaimed.Add(player.Username, GetStatusFromFile(player));
            Files.Create("SkyBlock/" + player.Username + "/DailyBonus.config", DailyBonusClaimed[player.Username].DaysClaimed.ToString());
        }

        public static void SpawnVillager()
        {
            NpcCore Villager = new NpcCore(15, string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.npc_txt_1")), Level) { KnownPosition = new PlayerLocation(-199945.5, 97, -199990.5, 300, 300), Gravity = 0, Height = 0, HideNameTag = true, UniType = UniType, NoAi = true };
            Villager.SpawnEntity();
        }

        public static string GetBonus(Player player, int Seed = -1)
        {
            if (Seed <= -1)
                Seed = DailyBonusClaimed[player.Username].DaysClaimed;
            string OutString = string.Empty;
            if (Seed == 7)
            {
                player.MoneyAdd(10000);
                OutString = "§c" + 10000 + "§bSB!!!";
            }
            else
            {
                Random rnd = new Random((int)DateTime.UtcNow.Ticks + new Random((int)DateTime.UtcNow.Ticks).Next(0, 10000));
                Item item = ItemFactory.GetItem(4, 0, (byte)rnd.Next(20, 64));
                switch (rnd.Next((Seed * 2), ((Seed * 2) + 4)))
                {

                    case 3:
                            switch (new Random().Next(0, 25))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(2, 0, (byte)rnd.Next(5, 20));
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(3, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(1, 0, (byte)rnd.Next(15, 35));
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(6, 0, (byte)rnd.Next(5, 15));
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(12, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(13, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(17, 0, (byte)rnd.Next(15, 30));
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(35, (short)rnd.Next(0, 15), (byte)rnd.Next(25, 64));
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(81, 0, (byte)rnd.Next(5, 10));
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(86, 0, (byte)rnd.Next(3, 6));
                                    break;
                                case 10:
                                    item = ItemFactory.GetItem(103, 0, (byte)rnd.Next(3, 6));
                                    break;
                                case 11:
                                    item = ItemFactory.GetItem(170, 0, (byte)rnd.Next(5, 15));
                                    break;
                                case 12:
                                    item = ItemFactory.GetItem(172, (short)rnd.Next(0, 15), (byte)rnd.Next(25, 64));
                                    break;
                                case 13:
                                    item = ItemFactory.GetItem(268, 0, 1);
                                    break;
                                case 14:
                                    item = ItemFactory.GetItem(269, 0, 1);
                                    break;
                                case 15:
                                    item = ItemFactory.GetItem(270, 0, 1);
                                    break;
                                case 16:
                                    item = ItemFactory.GetItem(271, 0, 1);
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        break;
                    case 5:
                        {
                            switch (new Random().Next(0, 15))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(260, 0, (byte)rnd.Next(5, 25));
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(295, 0, (byte)rnd.Next(5, 25));
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(272, 0, 1);
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(273, 0, 1);
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(274, 0, 1);
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(275, 0, 1);
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(298, 0, 1);
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(299, 0, 1);
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(300, 0, 1);
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(301, 0, 1);
                                    break;
                                case 10:
                                    item = ItemFactory.GetItem(296, 0, (byte)rnd.Next(15, 30));
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    case 7:
                        {
                            switch (new Random().Next(0, 4))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(264, 0, (byte)rnd.Next(5, 25));
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(318, 0, (byte)rnd.Next(5, 25));
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(287, 0, (byte)rnd.Next(5, 20));
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(289, 0, (byte)rnd.Next(5, 20));
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(291, 0, (byte)rnd.Next(10, 30));
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    case 9:
                        {
                            switch (new Random().Next(0, 14))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(302, 0, 1);
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(303, 0, 1);
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(304, 0, 1);
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(305, 0, 1);
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(319, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(260, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(392, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(411, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(458, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(291, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 10:
                                    item = ItemFactory.GetItem(361, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 11:
                                    item = ItemFactory.GetItem(362, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 12:
                                    item = ItemFactory.GetItem(363, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 13:
                                    item = ItemFactory.GetItem(365, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 14:
                                    item = ItemFactory.GetItem(338, 0, (byte)rnd.Next(10, 30));
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    case 11:
                        {
                            switch (new Random().Next(0, 10))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(256, 0, 1);
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(257, 0, 1);
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(258, 0, 1);
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(267, 0, 1);
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(306, 0, 1);
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(307, 0, 1);
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(308, 0, 1);
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(309, 0, 1);
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(325, 0, (byte)rnd.Next(1, 5));
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(340, 0, (byte)rnd.Next(5, 25));
                                    break;
                                case 10:
                                    item = ItemFactory.GetItem(347, 0, 1);
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    case 13:
                        {
                            switch (new Random().Next(0, 17))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(351, (byte)rnd.Next(0, 15), (byte)rnd.Next(10, 32));
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(359, 0, 1);
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(265, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(266, 0, (byte)rnd.Next(10, 30));
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(264, 0, (byte)rnd.Next(3, 7));
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(331, 0, (byte)rnd.Next(5, 30));
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(348, 0, (byte)rnd.Next(5, 30));
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(320, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(350, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(357, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 10:
                                    item = ItemFactory.GetItem(360, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 11:
                                    item = ItemFactory.GetItem(364, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 12:
                                    item = ItemFactory.GetItem(366, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 13:
                                    item = ItemFactory.GetItem(393, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 14:
                                    item = ItemFactory.GetItem(400, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 15:
                                    item = ItemFactory.GetItem(412, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 16:
                                    item = ItemFactory.GetItem(424, 0, (byte)rnd.Next(10, 20));
                                    break;
                                case 17:
                                    item = ItemFactory.GetItem(463, 0, (byte)rnd.Next(10, 20));
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    case 15:
                        {
                            switch (new Random().Next(0, 10))
                            {
                                case 0:
                                    item = ItemFactory.GetItem(276, 0, 1);
                                    break;
                                case 1:
                                    item = ItemFactory.GetItem(277, 0, 1);
                                    break;
                                case 2:
                                    item = ItemFactory.GetItem(278, 0, 1);
                                    break;
                                case 3:
                                    item = ItemFactory.GetItem(279, 0, 1);
                                    break;
                                case 4:
                                    item = ItemFactory.GetItem(310, 0, 1);
                                    break;
                                case 5:
                                    item = ItemFactory.GetItem(311, 0, 1);
                                    break;
                                case 6:
                                    item = ItemFactory.GetItem(312, 0, 1);
                                    break;
                                case 7:
                                    item = ItemFactory.GetItem(313, 0, 1);
                                    break;
                                case 8:
                                    item = ItemFactory.GetItem(388, 0, (byte)rnd.Next(5, 15));
                                    break;
                                case 9:
                                    item = ItemFactory.GetItem(264, 0, (byte)rnd.Next(5, 15));
                                    break;
                            }
                            player.Inventory.SetFirstEmptySlot(item, true, false);
                            OutString = string.Format(SkyBlock.LangManager.getLang("eng").getString("skyblock.npc_dailybonus.bonus.block"), item.Count, (item.Metadata == 0 ? (item.Id + ":" + item.Metadata) : (item.Id + "")));
                        }
                        break;
                    default:
                        int coins = new Random((int)DateTime.UtcNow.Ticks).Next(Seed * 25, Seed * 50 + 150);
                        player.MoneyAdd(coins);
                        OutString = "§c" + coins + "§bSB!!!";
                        break;
                }
            }
            return OutString;
        }
    }

    public class BonusStatus
    {
        public bool Status { get; set; } = false;
        public int DaysClaimed { get; set; } = 0;

        public BonusStatus(bool status, int daysClaimed)
        {
            Status = status;
            DaysClaimed = daysClaimed;
        }
    }
}
