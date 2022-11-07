using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class State
        {
            bool initialized = false;
            public bool Initialized => initialized;

            public ManagedBlocks blocks = new ManagedBlocks();
            public HashSet<long> scanned = new HashSet<long>();
            public Dictionary<MyItemType, List<ItemTarget>> itemTargets = new Dictionary<MyItemType, List<ItemTarget>>();

            public bool targetsUpdated = false;
            public List<IManagedInventory> sources = new List<IManagedInventory>();
            public List<IManagedInventory> targets = new List<IManagedInventory>();
            public HashSet<long> masterAssemblers = new HashSet<long>();
            public HashSet<long> slaveAssemblers = new HashSet<long>();
            public HashSet<long> refineries = new HashSet<long>();
            public HashSet<long> gasGenerators = new HashSet<long>();
            public HashSet<long> oxygenTanks = new HashSet<long>();
            public HashSet<long> hydrogenTanks = new HashSet<long>();
            public HashSet<long> batteryBlocks = new HashSet<long>();
            public HashSet<long> reactors = new HashSet<long>();
            public HashSet<long> shipConnectors = new HashSet<long>();

            public void Update(
                bool targetsUpdated,
                List<IManagedInventory> sources,
                List<IManagedInventory> targets,
                HashSet<long> masterAssemblers,
                HashSet<long> slaveAssemblers,
                HashSet<long> refineries,
                HashSet<long> gasGenerators,
                HashSet<long> oxygenTanks,
                HashSet<long> hydrogenTanks,
                HashSet<long> batteryBlocks,
                HashSet<long> reactors,
                HashSet<long> shipConnectors
            )
            {
                this.targetsUpdated = targetsUpdated;
                Util.Swap(ref this.sources, ref sources);
                Util.Swap(ref this.targets, ref targets);
                Util.Swap(ref this.masterAssemblers, ref masterAssemblers);
                Util.Swap(ref this.slaveAssemblers, ref slaveAssemblers);
                Util.Swap(ref this.refineries, ref refineries);
                Util.Swap(ref this.gasGenerators, ref gasGenerators);
                Util.Swap(ref this.oxygenTanks, ref oxygenTanks);
                Util.Swap(ref this.hydrogenTanks, ref hydrogenTanks);
                Util.Swap(ref this.batteryBlocks, ref batteryBlocks);
                Util.Swap(ref this.reactors, ref reactors);
                Util.Swap(ref this.shipConnectors, ref shipConnectors);
                initialized = true;
            }
        }
    }
}
