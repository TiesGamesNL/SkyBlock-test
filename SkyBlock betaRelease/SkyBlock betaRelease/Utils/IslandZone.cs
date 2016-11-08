using MiNET.Utils;

namespace SkyBlock.Utils
{
    public class IslandZone
    {
        public int Position { get; set; }

        public PositionEdge First { get; set; }
        public PositionEdge Seckond { get; set; }

        public IslandZone()
        {
        }

        public IslandZone(PositionEdge first, PositionEdge seckond)
        {
            First = first;
            Seckond = seckond;
        }
    }

    public class PositionEdge
    {
        public int X { get; set; }
        public int Z { get; set; }

        public PositionEdge()
        {
        }

        public PositionEdge(int x, int z)
        {
            X = x;
            Z = z;
        }

        public BlockCoordinates ToBlockCoordinates(int Y = 0)
        {
            return new BlockCoordinates(X, Y, Z);
        }

        //public BlockCoordinates ToBlockCoordinates(int Y)
        //{
        //    return ToBlockCoordinates(Y);
        //}

        //public PlayerLocation ToPlayerLocation(int Y)
        //{
        //    return new PlayerLocation(X, Y, Z);
        //}

        public PlayerLocation ToPlayerLocation(float Y = 0, float headYaw = 0, float yaw = 0, float pitch = 0)
        {
            return new PlayerLocation(X, Y, Z, headYaw, yaw, pitch);
        }

        //public PlayerLocation ToPlayerLocation()
        //{
        //    return ToPlayerLocation(0);
        //}

        public static PositionEdge operator +(PositionEdge left, PositionEdge right)
        {
            return new PositionEdge(left.X + right.X, left.Z + right.Z);
        }

        public static PositionEdge operator /(PositionEdge left, int integer)
        {
            return new PositionEdge(left.X / integer, left.Z / integer);
        }

        public static PositionEdge operator *(PositionEdge left, int integer)
        {
            return new PositionEdge(left.X * integer, left.Z * integer);
        }

        public static PositionEdge operator -(PositionEdge left, PositionEdge right)
        {
            return new PositionEdge(left.X - right.X, left.Z - right.Z);
        }
    }
}
