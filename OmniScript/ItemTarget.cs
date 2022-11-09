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
        public class ItemTarget
        {
            readonly IManagedInventory inventory;
            readonly Filter filter;

            public ItemTarget(IManagedInventory inventory, Filter filter)
            {
                this.inventory = inventory;
                this.filter = filter;
            }

            public IManagedInventory Inventory => inventory;
            public Filter Filter => filter;
            public int Priority => filter.Priority;
            public MyFixedPoint Quota => filter.Quota;
            public bool HasQuota => filter.HasQuota;
        }

        public class ItemTargetComparer : IComparer<ItemTarget>
        {
            public int Compare(ItemTarget a, ItemTarget b) =>
                b.Priority != a.Priority ? b.Priority - a.Priority :
                b.HasQuota != a.HasQuota ? b.HasQuota ? 1 : -1 :
                b.HasQuota && a.HasQuota && b.Quota != a.Quota ? b.Quota < a.Quota ? 1 : -1 :
                b.Inventory.Inventory.MaxVolume != a.Inventory.Inventory.MaxVolume ? b.Inventory.Inventory.MaxVolume < a.Inventory.Inventory.MaxVolume ? 1 : -1 :
                b.Inventory.Block.Block.EntityId != a.Inventory.Block.Block.EntityId ? b.Inventory.Block.Block.EntityId > a.Inventory.Block.Block.EntityId ? 1 : -1 :
                0;
        }
    }
}
