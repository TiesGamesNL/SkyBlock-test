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
    public static class SB
    {
        private static IDictionary<Player, int> money { get; set; } = new Dictionary<Player, int>();
        public static IDictionary<Item, int> Shop = new Dictionary<Item, int>();
        

        public static void Initialization()
        {
            //List<Item> Shop = new List<Item>();
            string ShopString = Files.OpenRead("SkyBlock/Shop.config");
            string[] ShopMass = ShopString.Split('|');
            string[] ItemPar;

            foreach (string ShopItem in ShopMass)
            {
                ItemPar = ShopItem.Split('=');
                Item item = null;
                item = ItemPar[0].ToItem();
                if (item == null || item is ItemAir) continue;
                Shop.Add(item, Convert.ToInt32(ItemPar[1]));
            }
            //return Shop;
        }

        public static int Money(this Player input)
        {
            int m = 0;
            if(!money.TryGetValue(input, out m)) m = input.GetMoneyFromFile();
            money[input] = m;
            return m;
        }

        public static int HaveInBaze(Item item)
        {
            Item key = ItemFactory.GetItem(item.Id, item.Metadata);
            int price = 0;
            Shop.TryGetValue(key, out price);
            return price;
        }

        public static string TrySellItem(this Player input, Item item)
        {
            int price = HaveInBaze(item);
            if (item.Count < 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.negative");
            if (item.Count == 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.null");
            if (item.Id <= 0 || price == 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.item.undefined");
            if (input.HasItem(item) < item.Count) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.hevent");
            //if (item.Count >= 255) return "you can't sell very many items";
            int moneyU = item.Count * price;
            input.Inventory.RemoveItems(item.Id, item.Count, item.Metadata);
            input.MoneyAdd(moneyU);
            return string.Empty;
        }

        public static string TryBuyItem(this Player input, Item item)
        {
            int price = HaveInBaze(item);
            if (item.Count < 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.negative");
            if (item.Count == 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.null");
            if (item.Id <= 0 || price == 0) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.item.undefined");
            int limit = (input.HasItem(new ItemAir()) * 64);
            if (limit > item.Count) item.Count = (byte)limit;
            int moneyU = item.Count * price;
            if (input.Money() < moneyU) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.hevent");
            input.Inventory.SetFirstEmptySlot(item, true, false);
            input.MoneyReduce(moneyU);
            return string.Empty;
        }

        public static string TryPayMoney(this Player input, Player player, int money)
        {
            if(input.Money() < money) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.money.error.hevent");
            input.MoneyReduce(money);
            player.MoneyAdd(money);
            return string.Empty;
        }

        public static Item ToItem(this string input)
        {
            Item item = new ItemAir();
            input = input.Trim(' ' , ';', '{', '}', '(', ')');
            string[] ItemString = input.Split(',');
            if (ItemString.Length == 1) return ItemFactory.GetItem(Convert.ToInt16(ItemString[0]));
            item = ItemFactory.GetItem(Convert.ToInt16(ItemString[0]), Convert.ToInt16(ItemString[1]));
            return item;
        }

        public static void MoneySet(this Player input, int MoneyCount)
        {
            Money(input);
            if (MoneyCount < 0) MoneyCount = 0;
            money[input] = MoneyCount;
            input.SetMoneyInFile();
        }

        public static void MoneyAdd(this Player input, int MoneyCount)
        {
            Money(input);
            money[input] += MoneyCount;
            input.SetMoneyInFile();
        }

        public static void MoneyReduce(this Player input, int MoneyCount)
        {
            int _money = Money(input);
            if (_money < MoneyCount) MoneyCount = _money;
            money[input] -= MoneyCount;
            input.SetMoneyInFile();
        }

        public static void SetMoneyInFile(this Player input)
        {
            int _money = input.Money();
            if (_money < 0) _money = 0;
            //input.MoneySet(money);
            money[input] = _money;
            Files.Create("SkyBlock/" + input.Username + "/Money.config", _money.ToString());
        }

        public static int GetMoneyFromFile(this Player input)
        {
            int _money = 0;
            if(File.Exists("SkyBlock/" + input.Username + "/Money.config"))
            {
                string stringMoney = Files.OpenRead("SkyBlock/" + input.Username + "/Money.config");
                _money = Convert.ToInt32(stringMoney);
            }
            return _money;
        }
    }
}
