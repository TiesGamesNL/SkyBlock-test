using MiNET;

namespace SkyBlock.Islands
{
    public static class IslandTypeManager
    {
        public static IslandType ToIslandType(this string input)
        {
            switch (input)
            {
                case "Default":
                    return IslandType.Default;
                case "Purchased":
                    return IslandType.PurchasedIsland;
                case "Vip":
                    return IslandType.VipIsland;
                case "Premium":
                    return IslandType.PremiumIsland;
                case "Deluxe":
                    return IslandType.DeluxeIsland;
                case "default":
                    return IslandType.Default;
                case "purchased":
                    return IslandType.PurchasedIsland;
                case "vip":
                    return IslandType.VipIsland;
                case "premium":
                    return IslandType.PremiumIsland;
                case "deluxe":
                    return IslandType.DeluxeIsland;
                case "0":
                    return IslandType.Default;
                case "1":
                    return IslandType.PurchasedIsland;
                case "2":
                    return IslandType.VipIsland;
                case "3":
                    return IslandType.PremiumIsland;
                case "4":
                    return IslandType.DeluxeIsland;
                default:
                    return IslandType.None;
            }
        }

        public static IslandType ToIslandType(this int input)
        {
            switch (input)
            {
                case 0:
                    return IslandType.Default;
                case 1:
                    return IslandType.PurchasedIsland;
                case 2:
                    return IslandType.VipIsland;
                case 3:
                    return IslandType.PremiumIsland;
                case 4:
                    return IslandType.DeluxeIsland;
                default:
                    return IslandType.None;
            }
        }

        public static IslandType GetIslandTypeByPermission(this Player player)
        {
            IslandType islandType = IslandType.Default;
            switch (player.Permission)
            {
                case "VIP":
                    islandType = IslandType.VipIsland;
                    break;
                case "MOD":
                    islandType = IslandType.VipIsland;
                    break;
                case "GMOD":
                    islandType = IslandType.VipIsland;
                    break;
                case "PREMIUM":
                    islandType = IslandType.PremiumIsland;
                    break;
                case "ADMIN":
                    islandType = IslandType.PremiumIsland;
                    break;
                case "DELUXE":
                    islandType = IslandType.DeluxeIsland;
                    break;
                case "OWNER":
                    islandType = IslandType.DeluxeIsland;
                    break;
            }
            return islandType;
        }
    }
}
