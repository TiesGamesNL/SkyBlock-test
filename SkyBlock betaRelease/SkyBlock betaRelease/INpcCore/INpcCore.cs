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
using FileSystem;

namespace SkyBlock.INpcCore
{
    public class NpcCore : Entity
    {
        public BlockEntity BlockEntity { get; set; } = null;
        public Block Block { get; set; } = new Air();
        public Entity TextEntity { get; set; }

        public string Text { get; set; }
        public string DefaultText { get; set; }
        public string UniType { get; set; } = "Default";

        public Player Player { get; set; }

        public NpcCore(int EntityTypeId, string text, Player player) : base(EntityTypeId, player.Level)
        {
            Player = player;
            Text = text;
            DefaultText = text;
        }

        public NpcCore(int EntityTypeId, string text, Level level) : base(EntityTypeId, level)
        {
            Text = text;
            DefaultText = text;
        }

        public NpcCore(BlockEntity blockEntity, string text, Player player) : base(0, player.Level)
        {
            Player = player;
            Text = text;
            DefaultText = text;
            BlockEntity = blockEntity;
        }

        public NpcCore(BlockEntity blockEntity, string text, Level level) : base(0, level)
        {
            Text = text;
            DefaultText = text;
            BlockEntity = blockEntity;
        }

        public NpcCore(Block block, string text, Player player) : base(0, player.Level)
        {
            Player = player;
            Text = text;
            DefaultText = text;
            Block = block;
        }

        public NpcCore(Block block, string text, Level level) : base(0, level)
        {
            Text = text;
            DefaultText = text;
            Block = block;
        }

        public void SendTextEntity()
        {
            if (Player == null) return;
            TextEntity.DespawnFromPlayers(new Player[] { Player });
            TextEntity.NameTag = Text;
            TextEntity.SpawnToPlayers(new Player[] { Player });
            TextEntity.NameTag = DefaultText;
            Text = DefaultText;
        }

        public void SendEntity()
        {
            base.SpawnToPlayers(Level.Players.Values.ToArray());
        }

        public override void SpawnEntity()
        {
            NameTag = string.Empty;
            Block.Coordinates = new BlockCoordinates(KnownPosition);
            TextEntity = new PlayerMob(NameTag, Level) { NameTag = Text, KnownPosition = KnownPosition, Width = 0, IsInvisible = true, Gravity = 0, Height = 0, HideNameTag = false };

            if (Player != null)
                TextEntity.SpawnToPlayers(new Player[] { Player });

            if (BlockEntity == null)
            {
                if (Block is Air)
                {
                    base.SpawnEntity();
                    //TextEntity.EntityId = (EntityId + 1);
                    Level.EntityManager.AddEntity(null, TextEntity);
                    NpcEvents.Npcs.Add(EntityId, this);
                    return;
                }
                NpcEvents.Npcs.Add(EntityId, this);
                Level.SetBlock(Block);
            }
            Block = BlockEntity.GetBlock();
            NpcEvents.Npcs.Add(EntityId, this);
            BlockEntity.Coordinates = Block.Coordinates;
            Level.SetBlockEntity(BlockEntity);
            Level.SetBlock(Block);
        }

        public override void DespawnEntity()
        {
            if (Player != null)
                TextEntity.DespawnFromPlayers(new Player[] { Player });
            base.DespawnEntity();
        }
    }

    public class NpcEventArgs : EventArgs
    {
        public string Text { get; set; }
        public Level Level { get; set; }
        public Player Player { get; set; }

        public PlayerLocation KnownPosition { get; set; }

        public NpcCore NpcStructure { get; set; }

        public NpcEventArgs(NpcCore npcStructure, PlayerLocation knownPosition, string text, Level level, Player player)
        {
            NpcStructure = npcStructure;
            Player = player;
            KnownPosition = knownPosition;
            Level = level;
            Text = text;
        }
    }

    /*public class NpcEntityEventArgs : NpcEventArgs
    {
        public Entity NpcStructure { get; set; }
        

        public NpcEntityEventArgs(Entity npcStructure, PlayerLocation knownPosition, string text, Level level, Player player) : base(knownPosition, text, level, player)
        {
            //Player = player;
            NpcStructure = npcStructure;
            //KnownPosition = knownPosition;
            //Level = level;
            //Text = text;
        }
    }

    public class NpcBlockEntityEventArgs : NpcEventArgs
    {
        public BlockEntity NpcStructure { get; set; }

        public NpcBlockEntityEventArgs(BlockEntity npcStructure, PlayerLocation knownPosition, string text, Level level, Player player) : base(knownPosition, text, level, player)
        {
            //Player = player;
            NpcStructure = npcStructure;
            //KnownPosition = knownPosition;
            //Level = level;
            //Text = text;
        }
    }

    public class NpcBlockEventArgs : NpcEventArgs
    {
        public Block NpcStructure { get; set; }

        public NpcBlockEventArgs(Block npcStructure, PlayerLocation knownPosition, string text, Level level, Player player) : base(knownPosition, text, level, player)
        {
            //Player = player;
            NpcStructure = npcStructure;
            //KnownPosition = knownPosition;
            //Level = level;
            //Text = text;
        }
    }*/
}
