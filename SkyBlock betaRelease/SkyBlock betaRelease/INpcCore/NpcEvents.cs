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

namespace SkyBlock.INpcCore
{
    public class NpcEvents : Plugin
    {

        public static IDictionary<long, NpcCore> Npcs = new Dictionary<long, NpcCore>();
        //public static IDictionary<BlockCoordinates, BlockEntity> BlockEntities = new Dictionary<BlockCoordinates, BlockEntity>();
        //public static IDictionary<BlockCoordinates, Block> Blocks = new Dictionary<BlockCoordinates, Block>();

        public static event EventHandler<NpcEventArgs> EntityEvent;
        protected virtual void OnEntityEvent(NpcEventArgs e)
        {
            EventHandler<NpcEventArgs> handler = EntityEvent;
            if (handler != null) handler(this, e);
        }

        public static NpcCore GetNpc(string uniType)
        {
            NpcCore Npc = Npcs.FirstOrDefault(npc => npc.Value.UniType == uniType).Value;
            return Npc;
        }

        /*public static event EventHandler<NpcBlockEntityEventArgs> BlockEntityEvent;
        protected virtual void OnBlockEntityEvent(NpcBlockEntityEventArgs e)
        {
            EventHandler<NpcBlockEntityEventArgs> handler = BlockEntityEvent;
            if (handler != null) handler(this, e);
        }

        public static event EventHandler<NpcBlockEventArgs> BlockEvent;
        protected virtual void OnBlockEvent(NpcBlockEventArgs e)
        {
            EventHandler<NpcBlockEventArgs> handler = BlockEvent;
            if (handler != null) handler(this, e);
        }*/

        [PacketHandler, Receive]
        public Package InteractHundler(McpeInteract packet, Player player)
        {
            NpcCore entity;
            if(Npcs.TryGetValue(packet.targetEntityId, out entity) || Npcs.TryGetValue(packet.targetEntityId - 1, out entity))
            {
                OnEntityEvent(new NpcEventArgs(entity, entity.KnownPosition, entity.Text, entity.Level, player));
                return null;
            }
            return packet;
        }

        [PacketHandler, Receive]
        public Package UseItemHundler(McpeUseItem packet, Player player)
        {
            NpcCore entity = Npcs.Values.FirstOrDefault(inc => inc.Block.Coordinates == packet.blockcoordinates);
            if (entity != null && entity.Block != new Air())
            {
                //entity.Player = player;
                OnEntityEvent(new NpcEventArgs(entity, entity.KnownPosition, entity.Text, entity.Level, player));
                return null;
            }
            return packet;
        }
    }
}
