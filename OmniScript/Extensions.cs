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
    static class Extensions
    {
        public static bool ContainsIgnoreCase(this string s, string value) => s.ToLower().Contains(value.ToLower());
        public static string SubStr(this string s, int begin, int end) => s.Substring(begin, end - begin);
        public static string SubStr(this string s, int begin = 0) => s.SubStr(begin, s.Length);

        public static List<IMyTerminalBlock> GetBlocks(this IMyGridTerminalSystem gts)
        {
            Program.Instance.IMyGridTerminalSystem_GetBlocks_blocks.Clear();
            gts.GetBlocksOfType<IMyTerminalBlock>(Program.Instance.IMyGridTerminalSystem_GetBlocks_blocks);
            return Program.Instance.IMyGridTerminalSystem_GetBlocks_blocks;
        }

        public static MyFixedPoint FreeVolume(this IMyInventory i) => i.MaxVolume - i.CurrentVolume;
        public static MyFixedPoint FitItems(this IMyInventory i, MyItemType type) => MyFixedPoint.MultiplySafe(i.FreeVolume(), 1 / type.GetItemInfo().Volume);
        public static MyFixedPoint TransferItemToSafe(this IMyInventory i, IMyInventory dstInventory, MyInventoryItem item, MyFixedPoint amount)
        {
            MyFixedPoint safeAmount = MyFixedPoint.Min(dstInventory.FitItems(item.Type), amount);
            return safeAmount > MyFixedPoint.Zero && i.TransferItemTo(dstInventory, item, safeAmount) ? safeAmount : MyFixedPoint.Zero;
        }
        public static MyFixedPoint TransferItemFromSafe(this IMyInventory i, IMyInventory sourceInventory, MyInventoryItem item, MyFixedPoint amount)
        {
            return sourceInventory.TransferItemToSafe(i, item, amount);
        }
        public static List<MyInventoryItem> GetItems(this IMyInventory i)
        {
            Program.Instance.IMyInventory_GetItems_items.Clear();
            i.GetItems(Program.Instance.IMyInventory_GetItems_items);
            return Program.Instance.IMyInventory_GetItems_items;
        }
        public static List<MyItemType> GetAcceptedItems(this IMyInventory i)
        {
            Program.Instance.IMyInventory_GetAcceptedItems_itemTypes.Clear();
            i.GetAcceptedItems(Program.Instance.IMyInventory_GetAcceptedItems_itemTypes);
            return Program.Instance.IMyInventory_GetAcceptedItems_itemTypes;
        }

        public static string DisplayName(this MyItemType it) => Program.Instance.GetItem(it).DisplayName;
        public static string Group(this MyItemType it) => Program.Instance.GetItem(it).Group;

        public static bool IsOxygen(this IMyGasTank gt) => gt.BlockDefinition.SubtypeId.Length == 0 || gt.BlockDefinition.SubtypeId.Contains("Oxygen");
        public static bool IsHydrogen(this IMyGasTank gt) => gt.BlockDefinition.SubtypeId.Contains("Hydrogen");

        public static UpdateType ToUpdateType(this UpdateFrequency ut)
        {
            switch (ut)
            {
                case UpdateFrequency.Once: return UpdateType.Once;
                case UpdateFrequency.Update1: return UpdateType.Update1;
                case UpdateFrequency.Update10: return UpdateType.Update10;
                case UpdateFrequency.Update100: return UpdateType.Update100;
                default: return UpdateType.None;
            }
        }

        public static void Prepare(this IMyTextSurface ts,
            ContentType ContentType = ContentType.TEXT_AND_IMAGE,
            TextAlignment Alignment = TextAlignment.LEFT,
            string Font = "Debug",
            float FontSize = 1f,
            float TextPadding = 1f
        )
        {
            ts.ContentType = ContentType;
            ts.Alignment = Alignment;
            ts.Font = Font;
            ts.FontSize = FontSize;
            ts.TextPadding = TextPadding;
        }
    }
}
