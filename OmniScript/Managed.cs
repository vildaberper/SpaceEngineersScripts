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
            bool Closed { get; }
            bool Changed { get; }
        }

        public abstract class ManagedBlock<TBlock> : IManagedBlock where TBlock : IMyTerminalBlock
        {
            protected readonly TBlock block;
            protected readonly string name;
            protected readonly string data;

            protected ManagedBlock(TBlock block)
            {
                this.block = block;
                name = block.CustomName;
                data = block.CustomData;
            }

            public IMyTerminalBlock Block => block;
            public string Name => name;
            public string Data => data;
            public bool Closed => block.Closed;
            public virtual bool Changed { get { return !block.CustomName.Equals(name) || !block.CustomData.Equals(data); } }
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
            bool Ready { get; }
        }

        public class ManagedInventory<TBlock> : IManagedInventory where TBlock : IManagedBlock
        {
            protected readonly TBlock block;
            protected readonly IMyInventory inventory;

            public ManagedInventory(TBlock block, IMyInventory inventory)
            {
                this.block = block;
                this.inventory = inventory;
            }

            public IManagedBlock Block => block;
            public IMyInventory Inventory => inventory;
            public virtual bool Ready => true;
        }
        public class ManagedInventory : ManagedInventory<IManagedBlock>
        {
            public ManagedInventory(IManagedBlock block, IMyInventory inventory) : base(block, inventory) { }
        }

        public abstract class ManagedInventoryBlock<TBlock> : ManagedBlock<TBlock> where TBlock : IMyTerminalBlock
        {
            protected IManagedInventory inventory;

            protected ManagedInventoryBlock(TBlock block) : base(block) { }

            public IManagedInventory Inventory
            {
                get { return inventory; }
                protected set { inventory = value; }
            }
        }

        public abstract class ManagedProductionBlock<TBlock> : ManagedBlock<TBlock> where TBlock : IMyProductionBlock
        {
            protected IManagedInventory input;
            protected IManagedInventory output;

            protected ManagedProductionBlock(TBlock block) : base(block) { }

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

        public class ManagedAssemblerInputInventory : ManagedInventory<ManagedAssembler>
        {
            public ManagedAssemblerInputInventory(ManagedAssembler block) : base(block, block.Input.Inventory) { }

            public override bool Ready => block.IsQueueEmpty;
        }
        public class ManagedAssembler : ManagedProductionBlock<IMyAssembler>
        {
            public ManagedAssembler(IMyAssembler block) : base(block)
            {
                block.UseConveyorSystem = true;
                Input = new ManagedAssemblerInputInventory(this);
                Output = new ManagedInventory(this, block.OutputInventory);
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
            public ManagedRefinery(IMyRefinery block) : base(block)
            {
                block.UseConveyorSystem = false;
                Input = new ManagedInventory(this, block.InputInventory);
                Output = new ManagedInventory(this, block.OutputInventory);
            }
        }

        public class ManagedGasGenerator : ManagedInventoryBlock<IMyGasGenerator>
        {
            public ManagedGasGenerator(IMyGasGenerator block) : base(block)
            {
                block.UseConveyorSystem = false;
                block.AutoRefill = true;
                Inventory = new ManagedInventory(this, block.GetInventory());
            }
        }

        public class ManagedReactor : ManagedInventoryBlock<IMyReactor>
        {
            public ManagedReactor(IMyReactor block) : base(block)
            {
                block.UseConveyorSystem = false;
                Inventory = new ManagedInventory(this, block.GetInventory());
            }
        }

        public class ManagedGasTank : ManagedBlock<IMyGasTank>
        {
            public ManagedGasTank(IMyGasTank block) : base(block) { }

            public bool IsOxygen => block.IsOxygen();
            public bool IsHydrogen => block.IsHydrogen();
        }

        public class ManagedBatteryBlock : ManagedBlock<IMyBatteryBlock>
        {
            public ManagedBatteryBlock(IMyBatteryBlock block) : base(block) { }
        }
    }
}
