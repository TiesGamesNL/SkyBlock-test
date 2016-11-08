using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET;
using MiNET.Utils;
using MiNET.Items;
using MiNET.Blocks;
using MiNET.BlockEntities;
using MiNET.Worlds;

namespace SkyBlock.Utils
{
    public static class Utils
    {
        public static bool Contains(this String str, String substring, StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException("substring",
                                                "substring cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                            "comp");

            return str.IndexOf(substring, comp) >= 0;
        }

        public static int HasItem(this Player input, Item item)
        {
            ItemStacks Slots = input.Inventory.GetSlots();
            int count = 0;
            for (int i = 0; i < Slots.Count; i++)
            {
                if ((Slots[i]).Id == item.Id)
                {
                    count = count + Slots[i].Count;
                }
            }
            return count;
        }

        public static bool TrySendReason(this Player input, string reason)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                input.SendMessage(reason);
                return true;
            }
            return false;
        }

        public static Block GetBlock(this BlockEntity input)
        {
            Block Block = new Air();
            switch (input.Id)
            {
                case "Sign":
                    Block = new StandingSign();
                    break;
                case "Chest":
                    Block = new Chest();
                    break;
                case "EnchantTable":
                    Block = new EnchantingTable();
                    break;
                case "Furnace":
                    Block = new Furnace();
                    break;
                case "Skull":
                    Block = new Skull();
                    break;
                case "ItemFrame":
                    Block = new ItemFrame();
                    break;
            }
            return Block;
        }

        public static BlockCoordinates GetNewCoordinatesFromFace(this BlockCoordinates target, BlockFace face)
        {
            switch (face)
            {
                case BlockFace.Down:
                    return target + Level.Down;
                case BlockFace.Up:
                    return target + Level.Up;
                case BlockFace.East:
                    return target + Level.East;
                case BlockFace.West:
                    return target + Level.West;
                case BlockFace.North:
                    return target + Level.South;
                case BlockFace.South:
                    return target + Level.North;
                default:
                    return target;
            }
        }

        public static InvisibleType ToInvisibleType(this string input)
        {
            switch (input)
            {
                case "anyone":
                    return InvisibleType.Anyone;
                case "Anyone":
                    return InvisibleType.Anyone;
                case "all":
                    return InvisibleType.All;
                case "All":
                    return InvisibleType.All;
                case "0":
                    return InvisibleType.Anyone;
                case "2":
                    return InvisibleType.All;
                default:
                    return InvisibleType.AdditionMembers;
            }
        }
    }
}
