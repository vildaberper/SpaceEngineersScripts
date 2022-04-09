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

            public Dictionary<long, ManagedBlock> blocks = new Dictionary<long, ManagedBlock>();
            public List<IManagedInventory> sources = new List<IManagedInventory>();
            public List<IManagedInventory> targets = new List<IManagedInventory>();
            public List<ManagedAssemblerInput> masterAssemblers = new List<ManagedAssemblerInput>();
            public List<ManagedAssemblerInput> slaveAssemblers = new List<ManagedAssemblerInput>();
            public List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
            public List<IMyGasTank> hydrogenTanks = new List<IMyGasTank>();
            public List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
            public List<ManagedReactor> reactors = new List<ManagedReactor>();

            public void Update(
                List<IManagedInventory> sources,
                List<IManagedInventory> targets,
                List<ManagedAssemblerInput> masterAssemblers,
                List<ManagedAssemblerInput> slaveAssemblers,
                List<IMyGasTank> oxygenTanks,
                List<IMyGasTank> hydrogenTanks,
                List<IMyBatteryBlock> batteryBlocks,
                List<ManagedReactor> reactors
            )
            {
                Util.Swap(ref this.sources, ref sources);
                Util.Swap(ref this.targets, ref targets);
                Util.Swap(ref this.masterAssemblers, ref masterAssemblers);
                Util.Swap(ref this.slaveAssemblers, ref slaveAssemblers);
                Util.Swap(ref this.oxygenTanks, ref oxygenTanks);
                Util.Swap(ref this.hydrogenTanks, ref hydrogenTanks);
                Util.Swap(ref this.batteryBlocks, ref batteryBlocks);
                Util.Swap(ref this.reactors, ref reactors);
                initialized = true;
            }
        }
    }
}
