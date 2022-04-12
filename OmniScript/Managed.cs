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
        public interface IManagedBlock
        {
            IMyTerminalBlock Block { get; }
            string Name { get; }
            string Data { get; }
            string Error { get; }
            bool HasError { get; }
            bool Closed { get; }
            bool Changed { get; }
        }

        public abstract class ManagedBlock<TBlock> : IManagedBlock where TBlock : IMyTerminalBlock
        {
            protected readonly TBlock block;
            protected readonly string name;
            protected readonly string data;
            protected string error = "";

            protected ManagedBlock(TBlock block)
            {
                this.block = block;
                name = block.CustomName;
                data = block.CustomData;
            }

            public IMyTerminalBlock Block => block;
            public string Name => name;
            public string Data => data;
            public string Error => error;
            public bool HasError => error.Length > 0;
            public bool Closed => block.Closed;
            public virtual bool Changed { get { return !block.CustomName.Equals(name) || !block.CustomData.Equals(data); } }
        }

        public class WatchedBlock : ManagedBlock<IMyTerminalBlock>
        {
            public WatchedBlock(IMyTerminalBlock block) : base(block) { }
        }

        public class ManagedBlocks : Dictionary<long, IManagedBlock>
        {
            public ManagedBlocks() : base() { }

            public ManagedAssembler Assembler(long id) => this[id] as ManagedAssembler;
            public ManagedRefinery Refinery(long id) => this[id] as ManagedRefinery;
            public ManagedGasGenerator GasGenerator(long id) => this[id] as ManagedGasGenerator;
            public ManagedReactor Ractor(long id) => this[id] as ManagedReactor;
            public ManagedGasTank GasTank(long id) => this[id] as ManagedGasTank;
            public ManagedBatteryBlock BatteryBlock(long id) => this[id] as ManagedBatteryBlock;
        }

        public interface IManagedInventory
        {
            IManagedBlock Block { get; }
            IMyInventory Inventory { get; }
            Filters Filters { get; }
            bool Ready { get; }
        }

        public class ManagedInventory<TBlock> : IManagedInventory where TBlock : IManagedBlock
        {
            protected readonly TBlock block;
            protected readonly IMyInventory inventory;
            protected readonly Filters filters;

            public ManagedInventory(TBlock block, IMyInventory inventory, Filters filters)
            {
                this.block = block;
                this.inventory = inventory;
                this.filters = filters;
            }

            public IManagedBlock Block => block;
            public IMyInventory Inventory => inventory;
            public Filters Filters => filters;
            public virtual bool Ready => true;
        }
        public class ManagedInventory : ManagedInventory<IManagedBlock>
        {
            public ManagedInventory(IManagedBlock block, IMyInventory inventory, Filters filters) : base(block, inventory, filters) { }
        }

        public abstract class ManagedFilteredBlock<TBlock> : ManagedBlock<TBlock> where TBlock : IMyTerminalBlock
        {
            readonly Filters filters;

            protected ManagedFilteredBlock(TBlock block, string defaultFilter = "") : base(block)
            {
                try
                {
                    filters = Filters.Parse(block.GetInventory(), name, data, defaultFilter);
                }
                catch (FilterException e)
                {
                    filters = Filters.None;
                    error = e.Message;
                }
            }

            public Filters Filters => filters;
        }

        public abstract class ManagedInventoryBlock<TBlock> : ManagedFilteredBlock<TBlock> where TBlock : IMyTerminalBlock
        {
            protected IManagedInventory inventory;

            protected ManagedInventoryBlock(TBlock block, string defaultFilter = "") : base(block, defaultFilter) { }

            public IManagedInventory Inventory
            {
                get { return inventory; }
                protected set { inventory = value; }
            }
        }

        public class ManagedInventoryBlock : ManagedInventoryBlock<IMyTerminalBlock>
        {
            public ManagedInventoryBlock(IMyTerminalBlock block, string defaultFilter = "") : base(block, defaultFilter)
            {
                Inventory = new ManagedInventory(this, block.GetInventory(), Filters);
            }
        }

        public abstract class ManagedProductionBlock<TBlock> : ManagedFilteredBlock<TBlock> where TBlock : IMyProductionBlock
        {
            protected IManagedInventory input;
            protected IManagedInventory output;

            protected ManagedProductionBlock(TBlock block, string defaultFilter = "") : base(block, defaultFilter) { }

            public IManagedInventory Input
            {
                get { return input; }
                protected set { input = value; }
            }
            public IManagedInventory Output
            {
                get { return output; }
                protected set { output = value; }
            }
        }

        public class ManagedProductionBlock : ManagedProductionBlock<IMyProductionBlock>
        {
            public ManagedProductionBlock(IMyProductionBlock block, string defaultFilter = "") : base(block, defaultFilter)
            {
                Input = new ManagedInventory(this, block.InputInventory, Filters);
                Output = new ManagedInventory(this, block.OutputInventory, Filters.None);
            }
        }

        public class ManagedAssemblerInputInventory : ManagedInventory<ManagedAssembler>
        {
            public ManagedAssemblerInputInventory(ManagedAssembler block, Filters filters) : base(block, ((IMyAssembler)block.Block).InputInventory, filters) { }

            public override bool Ready => block.IsQueueEmpty;
        }
        public class ManagedAssembler : ManagedProductionBlock<IMyAssembler>
        {
            public ManagedAssembler(IMyAssembler block) : base(block)
            {
                block.UseConveyorSystem = true;
                Input = new ManagedAssemblerInputInventory(this, Filters);
                Output = new ManagedInventory(this, block.OutputInventory, Filters.None);
            }

            public bool DefinedMaster => name.ContainsIgnoreCase("master");
            public bool IsQueueEmpty => block.IsQueueEmpty;
            public void ClearQueue() => block.ClearQueue();
            public bool CooperativeMode
            {
                get { return block.CooperativeMode; }
                set { block.CooperativeMode = value; }
            }
        }

        public class ManagedRefinery : ManagedProductionBlock<IMyRefinery>
        {
            public ManagedRefinery(IMyRefinery block) : base(block, defaultRefineryFilter)
            {
                block.UseConveyorSystem = false;
                Input = new ManagedInventory(this, block.InputInventory, Filters);
                Output = new ManagedInventory(this, block.OutputInventory, Filters.None);
            }
        }

        public class ManagedGasGenerator : ManagedInventoryBlock<IMyGasGenerator>
        {
            public ManagedGasGenerator(IMyGasGenerator block) : base(block, defaultGasGeneratorFilter)
            {
                block.UseConveyorSystem = false;
                block.AutoRefill = true;
                Inventory = new ManagedInventory(this, block.GetInventory(), Filters);
            }
        }

        public class ManagedReactor : ManagedInventoryBlock<IMyReactor>
        {
            public ManagedReactor(IMyReactor block) : base(block, defaulReactorFilter)
            {
                block.UseConveyorSystem = false;
                Inventory = new ManagedInventory(this, block.GetInventory(), Filters);
            }
        }

        public class ManagedGasTank : ManagedInventoryBlock<IMyGasTank>
        {
            public ManagedGasTank(IMyGasTank block) : base(block)
            {
                Inventory = new ManagedInventory(this, block.GetInventory(), Filters);
            }

            public bool IsOxygen => block.IsOxygen();
            public bool IsHydrogen => block.IsHydrogen();
        }

        public class ManagedBatteryBlock : ManagedBlock<IMyBatteryBlock>
        {
            public ManagedBatteryBlock(IMyBatteryBlock block) : base(block) { }
        }

        public class ManagedShipConnector : ManagedInventoryBlock<IMyShipConnector>
        {
            public ManagedShipConnector(IMyShipConnector block) : base(block)
            {
                Inventory = new ManagedInventory(this, block.GetInventory(), Filters);
            }

            public bool IsConnected => block.Status == MyShipConnectorStatus.Connected;
            public IMyCubeGrid ConnectedCubeGrid => block.OtherConnector.CubeGrid;
        }

        public class ManagedShipWelderInventory : ManagedInventory<ManagedShipWelder>
        {
            public ManagedShipWelderInventory(ManagedShipWelder block, Filters filters) : base(block, ((IMyShipWelder)block.Block).GetInventory(), filters) { }

            public override bool Ready => !block.IsActivated;
        }
        public class ManagedShipWelder : ManagedInventoryBlock<IMyShipWelder>
        {
            public ManagedShipWelder(IMyShipWelder block) : base(block)
            {
                block.UseConveyorSystem = true;
                Inventory = new ManagedShipWelderInventory(this, Filters);
            }

            public bool IsActivated => block.IsActivated;
        }
    }
}
