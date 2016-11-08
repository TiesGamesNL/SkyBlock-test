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
using SkyBlock.Managers;
using FileSystem;

namespace SkyBlock
{
    public class Island
    {
        public int Position { get; set; } = -1;
        public IslandZone Zone { get; set; }
        public BlockCoordinates RespawnPosition { get; set; }

        public string Name { get; set; }

        public string Owner { get; set; }
        public List<string> Members { get; set; } = new List<string>();
        public int MembersLimit { get; set; } = 5;

        public Configurations Configurations { get; set; }

        public IslandType Type { get; set; }
        public bool Removing { get; set; }

        public Level Level { get; set; }

        public Island(Level world)
        {
            Level = world;
            return;
        }

        public Island(Player player, Level world)
        {
            Level = world;
            Configurations = new Configurations(player);
            if (Configurations == new Configurations()) return;
            if (Configurations.Config == string.Empty) return;
            Position = Configurations.GetPosition();
            RespawnPosition = Configurations.GetRespawnPosition();
            Zone = Configurations.GetZone();
            Name = Configurations.GetIslandName();
            Type = Configurations.GetIslandType();

            MembersLimit = 5 + (int)Type * 2;

            Owner = Configurations.Owner;
            foreach (string Member in Configurations.GetMembers())
            {
                Members.Add(Member);
            }
        }

        public string CreateIsland(Player creator, string islandName, int position, IslandType type = IslandType.Default)
        {
            Configurations = new Configurations() { Position = position, IslandName = islandName };
            
            string reason = Configurations.CreateConfigFile(creator, islandName, position, type);
            if (reason != string.Empty) return reason;

            Owner = creator.Username;
            Position = Configurations.Position;
            Zone = Configurations.GetZone();
            Name = Configurations.GetIslandName();
            RespawnPosition = Configurations.GetRespawnPosition();
            Type = type;

            MembersLimit = 5 + (int)Type * 2;

            //Block block = new Block(2) { Coordinates = RespawnPosition };
            //Level.SetBlock(block);
            CreateIslandCustomizing(Zone.First, Type);

            return string.Empty;
        }

        public string RemoveIsland(Player creator)
        {
            if (Configurations.Config == string.Empty) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.havent_1");

            if (creator.Username != Configurations.Owner) return SkyBlock.LangManager.getLang("eng").getString("skyblock.permission.havent");
            //string reason = Configurations.RemoveConfigFile(creator);
            //if (reason != string.Empty) return reason;
            Removing = true;
            return string.Empty;
        }

        public string AddMember(Player owner, Player member)
        {
            if (owner.Username != Configurations.Owner) return SkyBlock.LangManager.getLang("eng").getString("skyblock.permission.havent");
            if (Members.Count >= MembersLimit) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.members.havelimit");
            string reason = Configurations.CreateMemberFiles(owner, member);
            if (reason != string.Empty) return reason;

            Members.Add(member.Username);

            return string.Empty;
        }

        public string RemoveMember(Player owner, Player member)
        {
            if (owner.Username != Configurations.Owner) return SkyBlock.LangManager.getLang("eng").getString("skyblock.permission.havent");
            string reason = Configurations.RemoveMemberFiles(owner.Username, member);
            if (reason != string.Empty) return reason;

            Members.Remove(member.Username);

            return string.Empty;
        }

        public string SetRespawnPosition(Player owner, BlockCoordinates respawnPosition)
        {
            if (owner.Username != Configurations.Owner) return SkyBlock.LangManager.getLang("eng").getString("skyblock.permission.havent");
            if (!IsOnGround(respawnPosition)) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.position.notstandingonground");
            
            string reason = Configurations.SetRespawnPosition(owner, respawnPosition);
            if (reason != string.Empty) return reason;

            RespawnPosition = Configurations.RespawnPosition;

            return string.Empty;
        }

        public string SetRespawnPosition(Player owner, PlayerLocation KnownPosition)
        {
            BlockCoordinates respawnPosition = new BlockCoordinates((int)KnownPosition.X, (int)KnownPosition.Y - 1, (int)KnownPosition.Z);
            
            return SetRespawnPosition(owner, respawnPosition);
        }
        
        public static int[] NoBlockIds { get; private set; } = { 0, 6, 8, 9, 10, 11, 26, 27, 28, 31, 32, 37, 38, 39, 40, 50,
            55, 59, 63, 64, 66, 69, 70, 71, 72, 75, 76, 77, 78, 81, 83, 89, 93 ,94, 104, 105, 111, 115, 126, 140, 141, 142, 143, 146, 147,
            148, 149, 150, 171, 175, 193, 194, 195, 196, 197, 244 };

        private bool IsOnGround(BlockCoordinates position)
        {
            Block block = Level.GetBlock(new BlockCoordinates(position));

            for (var i = 0; i < NoBlockIds.Length; i++)
            {
                if (block.Id == NoBlockIds[i] || block is Flowing || block is Stationary || block is Air)
                {
                    Level.BroadcastMessage(block.Id + "");
                    break;
                }
                if (i == NoBlockIds.Length - 1) return true;
            }
            return false;
        }

        public void CreateIslandCustomizing(PositionEdge First, IslandType Type)
        {
            IslandCustomizing Customizing = new IslandCustomizing(Type);
            Block[] blocks = Customizing.Blocks.ToArray();
            foreach(Block block in blocks)
            {
                block.Coordinates += First.ToBlockCoordinates();
                Level.SetBlock(block, true, false);
            }
            PlaceChest(First);
        }

        public void PlaceChest(PositionEdge postion)
        {
            Chest chest = new Chest
            {
                Coordinates = new BlockCoordinates(postion.X + 47, 75, postion.Z + 50),
                Metadata = (byte)5
            };
            Level.SetBlock(chest);
            ChestBlockEntity chestBlockEntity = new ChestBlockEntity
            {
                Coordinates = new BlockCoordinates(postion.X + 47, 75, postion.Z + 50)
            };
            Level.SetBlockEntity(chestBlockEntity);
            Inventory inventory = Level.InventoryManager.GetInventory(new BlockCoordinates(postion.X + 47, 75, postion.Z + 50));
            byte c = 0;
            inventory.Slots[c++] = ItemFactory.GetItem(6, 0, 2);
            //inventory.Slots[c++] = ItemFactory.GetItem(6, 1, 2);
            //inventory.Slots[c++] = ItemFactory.GetItem(6, 2, 1);
            //inventory.Slots[c++] = ItemFactory.GetItem(6, 3, 2);
            //inventory.Slots[c++] = ItemFactory.GetItem(6, 4, 2);
            //inventory.Slots[c++] = ItemFactory.GetItem(6, 5, 2);
            inventory.Slots[c++] = ItemFactory.GetItem(8, 0, 2);
            inventory.Slots[c++] = ItemFactory.GetItem(10, 0, 1);
            inventory.Slots[c++] = ItemFactory.GetItem(17, 0, 15);
            inventory.Slots[c++] = ItemFactory.GetItem(39, 0, 3);
            inventory.Slots[c++] = ItemFactory.GetItem(40, 0, 3);
            inventory.Slots[c++] = ItemFactory.GetItem(58, 0, 1);
            inventory.Slots[c++] = ItemFactory.GetItem(81, 0, 1);
            inventory.Slots[c++] = ItemFactory.GetItem(391, 0, 3);
            inventory.Slots[c++] = ItemFactory.GetItem(392, 0, 3);
            inventory.Slots[c++] = ItemFactory.GetItem(260, 0, 10);
            inventory.Slots[c++] = ItemFactory.GetItem(325, 0, 1);
            inventory.Slots[c++] = ItemFactory.GetItem(352, 0, 5);
            inventory.Slots[c++] = ItemFactory.GetItem(361, 0, 2);
            inventory.Slots[c++] = ItemFactory.GetItem(362, 0, 2);
            inventory.Slots[c++] = ItemFactory.GetItem(338, 0, 1);
            inventory.SendInventory();
        }
        
        public static bool HavePurchasedIsland(Player player)
        {
            return File.Exists("SkyBlock/" + player.Username + "/PurchasedIsland.key");
        }

        //this method does not work properly
        public bool HaveIsland(string IslandName)
        {
            //This method does not work properly

            //string[] Usernames = Directory.GetDirectories("SkyBlock");
            //
            //foreach (string Username in Usernames)
            //{
            //    string folder = Username.Replace('\\', '/');
            //    if (!File.Exists(folder + "/IsConfigurations.player")) continue;
            //    string islandName = Files.OpenRead(folder + "/IsConfigurations.player");
            //    if (islandName == IslandName.Split('|')[0]) return true;
            //}
            return false;
        }

        /*public static bool operator ==(Island left, Island right)
        {
            if (left.Configurations != right.Configurations) return false;
            if (left.Level != right.Level) return false;
            if (left.Members != right.Members) return false;
            if (left.MembersLimit != right.MembersLimit) return false;
            if (left.Name != right.Name) return false;
            if (left.Owner != right.Owner) return false;
            if (left.Position != right.Position) return false;
            if (left.RespawnPosition != right.RespawnPosition) return false;
            if (left.Type != right.Type) return false;
            if (left.Zone != right.Zone) return false;
            return true;
        }

        public static bool operator !=(Island left, Island right)
        {
            if (left.Configurations == right.Configurations) return false;
            if (left.Level == right.Level) return false;
            if (left.Members == right.Members) return false;
            if (left.MembersLimit == right.MembersLimit) return false;
            if (left.Name == right.Name) return false;
            if (left.Owner == right.Owner) return false;
            if (left.Position == right.Position) return false;
            if (left.RespawnPosition == right.RespawnPosition) return false;
            if (left.Type == right.Type) return false;
            if (left.Zone == right.Zone) return false;
            return true;
        }*/
    }
}
