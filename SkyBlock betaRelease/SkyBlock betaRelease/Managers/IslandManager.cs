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
    public class Configurations
    {
        public string Config { get; set; } = string.Empty;
        public string Owner { get; set; }
        public string IslandName { get; set; }
        public IslandType islandType { get; set; }
        public BlockCoordinates RespawnPosition { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int Position { get; set; } = -1;

        public Configurations()
        {
            return;
        }

        public Configurations(Player player)
        {
            if (File.Exists("SkyBlock/" + player.Username + "/IsConfigurations.player"))
            {
                Owner = player.Username;
                Reason = "Owner";
            }
            else if (File.Exists("SkyBlock/" + player.Username + "/Member.player"))
            {
                Owner = Files.OpenRead("SkyBlock/" + player.Username + "/Member.player");
                Reason = "Member";
            }
            else
            {
                return;
            }
            Config = Files.OpenRead("SkyBlock/" + Owner + "/IsConfigurations.player");
        }

        public string TakeConfig(string islandName, int position, IslandType type)
        {
            IslandName = islandName;
            Position = position;
            islandType = type;
            IslandZone zone = GetZone();
            PositionEdge respawn = zone.First + new PositionEdge(50, 50);
            RespawnPosition = respawn.ToBlockCoordinates(74);
            return TakeConfig();
        }

        public string TakeConfig()
        {
            if (IslandName == string.Empty || Position == -1 || RespawnPosition == new BlockCoordinates() || islandType == IslandType.None) return string.Empty;
            
            string respawn = RespawnPosition.X + "," + RespawnPosition.Y + "," + RespawnPosition.Z;
            Config = IslandName + "|" + Position + "|" + respawn + "|" + islandType;
            return Config;
        }

        public string CreateConfigFile(Player creator, string islandName, int position, IslandType type)
        {
            
            if (TakeConfig(islandName, position, type) == string.Empty) return "empty config";

            Configurations config = new Configurations(creator);
            if (config.Reason != string.Empty)
            {
                if (config.Reason == "Owner")
                    return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.have");
                if (config.Reason == "Member")
                    return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.liveonanother");
                return "reason error : " + config.Reason;
            }
            Owner = creator.Username;
            Files.Create("SkyBlock/" + Owner + "/IsConfigurations.player", Config);
            return string.Empty;
        }

        public string RemoveConfigFile(Player creator)
        {
            File.Delete("SkyBlock/" + creator.Username + "/IsConfigurations.player");
            if (File.Exists("SkyBlock/" + Owner + "/Members.config"))
            {
                foreach (string member in GetMembers())
                {
                    File.Delete("SkyBlock/" + member + "/Member.player");
                }
                File.Delete("SkyBlock/" + Owner + "/Members.config");
            }

            return string.Empty;
        }

        public string CreateMemberFiles(Player owner, Player member)
        {
            if (string.IsNullOrEmpty(Config)) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.havent_1");

            string[] members = GetMembers();
            foreach (string memberName in members)
            {
                if (memberName == member.Username) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.words.player") + " " + member.Username + " " + SkyBlock.LangManager.getLang("eng").getString("skyblock.island.members.alreadylive");
            }

            Configurations config = new Configurations(member);
            if (config != null && config.Config != string.Empty)
            {
                if (config.Reason == "Owner")
                    return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.words.player") + " " + member.Username + " " + SkyBlock.LangManager.getLang("eng").getString("skyblock.island.members.alreadyhaveisland");
                if (config.Reason == "Member")
                    return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.words.player") + " " + member.Username + " " + SkyBlock.LangManager.getLang("eng").getString("skyblock.island.members.alreadyliveonanother");
                return "reason error";
            }

            string memberString = "," + member.Username;
            if(!File.Exists("SkyBlock/" + Owner + "/Members.config"))
                memberString = member.Username;
            if (member.Length == 0) memberString = member.Username;
            
            Files.OpenWrite("SkyBlock/" + Owner + "/Members.config", memberString);
            Files.Create("SkyBlock/" + member.Username + "/Member.player", Owner);

            return string.Empty;
        }

        public string RemoveMemberFiles(string owner, Player member)
        {
            if (Config == string.Empty) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.havent_1");

            bool IsLive = false;

            string[] members = GetMembers();
            string NewMembersString = string.Empty;
            for(var i = 0; i < members.Length; i++)
            {
                if (members[i] == member.Username)
                {
                    IsLive = true;
                    continue;
                }
                //return "player " + member.Username + " no longer lives on on your island";
                if (NewMembersString == string.Empty)
                {
                    NewMembersString = members[i];
                    continue;
                }
                NewMembersString += ("," + members[i]);
            }

            if (!IsLive) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.words.player") + " " + member.Username + " " + SkyBlock.LangManager.getLang("eng").getString("skyblock.island.members.doesntlive");

            if (NewMembersString == string.Empty)
            {
                File.Delete("SkyBlock/" + Owner + "/Members.config");
            }
            else
            {
                Files.Create("SkyBlock/" + Owner + "/Members.config", NewMembersString);
            }
            File.Delete("SkyBlock/" + member.Username + "/Member.player");

            return string.Empty;
        }

        public string GetStatus(Player player)
        {
            if (player.Username == Owner) return "Owner";
            return "Member";
        }
        

        public IslandZone GetZone()
        {
            if (Position <= -1)
                GetPosition();
            if (Position <= -1) return new IslandZone();

            PositionEdge Buffer = new PositionEdge(-200000, -200000);
            PositionEdge First;
            PositionEdge Seckond;

            for (var i = 0; i < Position; i++)
            {
                if (Buffer.X == 200000)
                {
                    Buffer.Z += 100;
                    Buffer.X = -200000;
                }
                else
                {
                    Buffer.X += 100;
                }
            }
            First = Buffer;
            Seckond = Buffer + new PositionEdge(99, 99);

            return new IslandZone(First, Seckond);
        }

        public int GetPosition()
        {
            if (Config == string.Empty) return 0;
            string position = Config.Split('|')[1];

            Position = Convert.ToInt32(position);

            return Position;
        }
 
        public BlockCoordinates GetRespawnPosition()
        {
            if (Config == string.Empty) return new BlockCoordinates();
            string Position = Config.Split('|')[2];
            string[] Coordinates = Position.Split(',');
            int x = Convert.ToInt32(Coordinates[0]);
            int y = Convert.ToInt32(Coordinates[1]);
            int z = Convert.ToInt32(Coordinates[2]);

            return new BlockCoordinates(x, y, z);
        }

        public string SetRespawnPosition(Player owner, BlockCoordinates respawnPosition)
        {
            if (Config == string.Empty) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.havent_1");

            if (respawnPosition == new BlockCoordinates()) return SkyBlock.LangManager.getLang("eng").getString("skyblock.island.position.unknown");
            RespawnPosition = respawnPosition;

            string[] Configs = Config.Split('|');
            string position = RespawnPosition.X + "," + RespawnPosition.Y + "," + RespawnPosition.Z;

            Config = Configs[0] + "|" + Configs[1] + "|" + position + "|" + Configs[3];

            Files.Create("SkyBlock/" + Owner + "/IsConfigurations.player", Config);

            return string.Empty;
        }

        public string[] GetMembers()
        {
            if (Config == string.Empty) return new string[] { };
            if (!File.Exists("SkyBlock/" + Owner + "/Members.config")) return new string[] { };
            string Members = Files.OpenRead("SkyBlock/" + Owner + "/Members.config");
            if (Members == string.Empty) return new string[] { };
            return Members.Split(',');
        }

        public string GetIslandName()
        {
            if (Config == string.Empty) return string.Empty;
            IslandName = Config.Split('|')[0];
            return IslandName;
        }

        public IslandType GetIslandType()
        {
            if (Config == string.Empty) return IslandType.None;
            IslandType islandType = Config.Split('|')[3].ToIslandType();
            return islandType;
        }

        /*public static bool operator ==(Configurations left, Configurations right)
        {
            if (left.Config != right.Config) return false;
            if (left.IslandName != right.IslandName) return false;
            if (left.islandType != right.islandType) return false;
            if (left.Owner != right.Owner) return false;
            if (left.Position != right.Position) return false;
            if (left.Reason != right.Reason) return false;
            if (left.RespawnPosition != right.RespawnPosition) return false;
            return true;
        }

        public static bool operator !=(Configurations left, Configurations right)
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine(left.Config + " " + right.Config);
            if (left.Config == right.Config) return false;
            Console.WriteLine(left.IslandName + " " + right.IslandName);
            if (left.IslandName == right.IslandName) return false;
            Console.WriteLine(left.islandType + " " + right.islandType);
            if (left.islandType == right.islandType) return false;
            Console.WriteLine(left.Owner + " " + right.Owner);
            if (left.Owner == right.Owner) return false;
            Console.WriteLine(left.Position + " " + right.Position);
            if (left.Position == right.Position) return false;
            Console.WriteLine(left.Reason + " " + right.Reason);
            if (left.Reason == right.Reason) return false;
            Console.WriteLine(left.RespawnPosition + " " + right.RespawnPosition);
            if (left.RespawnPosition == right.RespawnPosition) return false;
            return true;
        }*/
    }
}