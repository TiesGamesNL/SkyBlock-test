using System;
using System.Collections.Generic;
using MiNET;

namespace SkyBlock
{
    public class InvisibleMe
    {
        public bool IsInvisible;
        public static IDictionary<string, InvisibleType> Type = new Dictionary<string, InvisibleType>();

        /// <summary>
        /// интерфейс для увеличения производительности клиентов и удобства игроков
        /// </summary>
        /// <param name="player"> игрок, для которого происходит обновление интерфейса </param>
        /// <param name="playersList"> все те игроки сущности который обновляются для "player" </param>
        /// <param name="type"> тип изменённого интерфейса </param>
        public static void TakeInvisible(Player player, Player[] playersList, InvisibleType type = InvisibleType.AdditionMembers)
        {
            if (type == InvisibleType.None)
            {
                Type.TryGetValue(player.Username, out type);
            }
            else
            {
                Type[player.Username] = type;
            }
            //if (type == InvisibleType.All || type == InvisibleType.AdditionMembers)
            //{
            bool Continue = false;
            foreach (Player p in playersList)
            {
                if (p.Permission != "GUEST")
                {
                    continue;
                }
                //this code can be edited <begin>
                Island island = null;
                SkyBlock.Islands.TryGetValue(player.Username, out island);
                if (island != null && island != new Island(player.Level) && type != InvisibleType.All)
                {
                    if (p.Username == island.Owner)
                    {
                        p.SpawnToPlayers(new Player[] { player });
                        continue;
                    }
                    foreach (string member in island.Members)
                    {
                        if (member == p.Username)
                        {
                            Console.WriteLine("bag1");
                            p.SpawnToPlayers(new Player[] { player });
                            Continue = true;
                            break;
                        }
                    }
                }
                //this code can be edited <end>
                if (Continue)
                {
                    Continue = false;
                    continue;
                }

                if (type == InvisibleType.All || type == InvisibleType.AdditionMembers)
                {
                    p.DespawnFromPlayers(new Player[] { player });
                }
                if (type == InvisibleType.Anyone)
                {
                    p.SpawnToPlayers(new Player[] { player });
                }
            }
            //}
            //if (type == InvisibleType.Anyone)
            //{
            //    foreach (Player p in playersList)
            //    {
            //        p.SpawnToPlayers(new Player[] { player });
            //    }
            //}
        }

        /// <summary>
        /// взаимообновление интерфейсов
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playersList"></param>
        public static void UpdateInvisibleToAllFromPlayer(Player player, Player[] playersList)
        {
            if (Type[player.Username] != InvisibleType.Anyone)
                TakeInvisible(player, playersList, InvisibleType.None);
            foreach (Player p in playersList)
            {
                if (Type[p.Username] != InvisibleType.Anyone)
                {
                    TakeInvisible(p, new Player[] { player }, InvisibleType.None);
                }
            }
        }
    }

    public enum InvisibleType
    {
        Anyone = 0,
        AdditionMembers = 1,
        All = 2,
        None = 255
    }
}
