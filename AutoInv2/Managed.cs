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
            public bool Closed => block.Closed;
            public virtual bool Changed { get { return !block.CustomName.Equals(name) || !block.CustomData.Equals(data); } }
        }
        public class ManagedBlock : ManagedBlock<IMyTerminalBlock>
        {
            public ManagedBlock(IMyTerminalBlock block) : base(block) { }
        }

        public interface IManagedInventory : IManagedBlock
        {
            IMyInventory Inventory { get; }
            bool Ready { get; }
        }

        public abstract class ManagedInventory<TBlock> : ManagedBlock<TBlock>, IManagedInventory where TBlock : IMyTerminalBlock
        {
            protected readonly IMyInventory inventory;

            protected ManagedInventory(TBlock block, IMyInventory inventory) : base(block)
            {
                this.inventory = inventory;
            }
            protected ManagedInventory(TBlock block) : this(block, block.GetInventory()) { }

            public IMyInventory Inventory => inventory;
            public virtual bool Ready => true;
        }
        public class ManagedInventory : ManagedInventory<IMyTerminalBlock>
        {
            public ManagedInventory(IMyTerminalBlock block) : base(block) { }
        }

        public class ManagedOutput : ManagedInventory<IMyProductionBlock>
        {
            public ManagedOutput(IMyProductionBlock block) : base(block, block.OutputInventory) { }
        }

        public class ManagedAssemblerInput : ManagedInventory<IMyAssembler>
        {
            public ManagedAssemblerInput(IMyAssembler block) : base(block, block.InputInventory)
            {
                block.UseConveyorSystem = true;
            }
            public override bool Ready => IsQueueEmpty;

            public bool DefinedMaster => name.ContainsIgnoreCase("master");
            public bool IsQueueEmpty => block.IsQueueEmpty;
            public void ClearQueue() => block.ClearQueue();
            public bool CooperativeMode
            {
                get { return block.CooperativeMode; }
                set { block.CooperativeMode = value; }
            }
        }

        public class ManagedRefineryInput : ManagedInventory<IMyRefinery>
        {
            public ManagedRefineryInput(IMyRefinery block) : base(block, block.InputInventory)
            {
                block.UseConveyorSystem = false;
            }
        }

        public class ManagedGasGenerator : ManagedInventory<IMyGasGenerator>
        {
            public ManagedGasGenerator(IMyGasGenerator block) : base(block)
            {
                block.UseConveyorSystem = false;
                block.AutoRefill = true;
            }
        }

        public class ManagedReactor : ManagedInventory<IMyReactor>
        {
            public ManagedReactor(IMyReactor block) : base(block)
            {
                block.UseConveyorSystem = false;
            }
        }
    }
}
