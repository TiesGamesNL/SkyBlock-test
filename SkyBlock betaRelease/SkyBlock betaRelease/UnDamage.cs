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
using MiNET.Entities.World;
using MiNET.Entities.Passive;
using MiNET.Items;
using MiNET.Sounds;
using SkyBlock.Utils;
using SkyBlock.Islands;
using SkyBlock.Managers;
using SkyBlock.INpcCore;
using FileSystem;

namespace SkyBlock
{
    public class UnHungerManager : HungerManager
    {
        public UnHungerManager(Player player) : base(player)
        {
        }

        public override void ResetHunger()
        {
            //base.ResetHunger();
            int hunger = Hunger;
            //if(hunger <= 0)
                //hunger = SkyBlock.InventoryMenager[Player].Hunger;
            //SkyBlock.HungerBuffer.TryGetValue(Player.Username, out hunger);
            if (hunger < 6) hunger = 6;//;
            Hunger = hunger;
            Saturation = (Hunger/10);
            Exhaustion = 0;
        }
    }

    public class UnHealthManager : HealthManager
    {
        public UnHealthManager(Player player) : base(player)
        {
        }

        public override void TakeHit(Entity source, int damage = 1, DamageCause cause = DamageCause.Unknown)
        {
            Player deather = Entity as Player;
            Player killer = source as Player;
            Island island;
            if (killer != null)
            {
                if (SkyBlock.IsOp(killer.Username))
                {
                    base.TakeHit(source, damage, cause);
                    return;
                }
                if (deather != null)
                {
                    if (killer.KnownPosition.X < -199975 && killer.KnownPosition.X > -199990 && killer.KnownPosition.Z < -199950 && killer.KnownPosition.Z > -199965)
                    {
                        if (deather.KnownPosition.X < -199975 && deather.KnownPosition.X > -199990 && deather.KnownPosition.Z < -199950 && deather.KnownPosition.Z > -199965)
                        {
                            base.TakeHit(source, damage, cause);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        killer.SendPlayerInventory();
                        return;
                    }
                }
                else if (SkyBlock.Islands.TryGetValue(killer.Username, out island) && Entity.KnownPosition.X < island.Zone.Seckond.X && Entity.KnownPosition.X > island.Zone.First.X && Entity.KnownPosition.Z < island.Zone.Seckond.Z && Entity.KnownPosition.Z > island.Zone.First.Z)
                {
                    base.TakeHit(source, damage, cause);
                }
                else
                {
                    killer.SendPlayerInventory();
                    return;
                }
            }
            else
            {
                base.TakeHit(source, damage, cause);
                return;
            }
        }
    }
}
