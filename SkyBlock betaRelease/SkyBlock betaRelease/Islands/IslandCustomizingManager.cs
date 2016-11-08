using System;
using System.Collections.Generic;
using System.IO;
using MiNET.Blocks;
using MiNET.Utils;
using FileSystem;

namespace SkyBlock.Islands
{
    public class IslandCustomizing
    {
        public List<Block> Blocks = new List<Block>();

        public IslandCustomizing(IslandType Type)
        {
            if (!File.Exists("IslandsTypes/" + Type + ".build")) return;
            string blocksString = Files.OpenRead("IslandsTypes/" + Type + ".build");
            string[] blocks = blocksString.Split('|');
            foreach (string StrBlock in blocks)
            {
                string[] BlockConf = StrBlock.Split(',');
                Block block = new Block(Convert.ToByte(BlockConf[0]));
                BlockCoordinates Coordinates = new BlockCoordinates();
                if (BlockConf.Length == 5)
                {
                    block.Metadata = Convert.ToByte(BlockConf[1]);
                    Coordinates = new BlockCoordinates(Convert.ToInt32(BlockConf[2]), Convert.ToInt32(BlockConf[3]), Convert.ToInt32(BlockConf[4]));
                }
                else
                {
                    Coordinates = new BlockCoordinates(Convert.ToInt32(BlockConf[1]), Convert.ToInt32(BlockConf[2]), Convert.ToInt32(BlockConf[3]));
                }
                block.Coordinates = Coordinates;
                Blocks.Add(block);
            }
        }
    }
}
