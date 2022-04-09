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

        static readonly List<IMyTerminalBlock> IMyGridTerminalSystem_GetBlocks_blocks = new List<IMyTerminalBlock>();
        public static List<IMyTerminalBlock> GetBlocks(this IMyGridTerminalSystem gts)
        {
            IMyGridTerminalSystem_GetBlocks_blocks.Clear();
            gts.GetBlocksOfType<IMyTerminalBlock>(IMyGridTerminalSystem_GetBlocks_blocks);
            return IMyGridTerminalSystem_GetBlocks_blocks;
        }

        public static MyFixedPoint FreeVolume(this IMyInventory i)
        {
            return i.MaxVolume - i.CurrentVolume;
        }
        public static bool CanItemsBeAdded(this IMyInventory i, MyItemType type)
        {
            return i.CanItemsBeAdded(MyFixedPoint.SmallestPossibleValue, type);
        }
        public static MyFixedPoint FitItems(this IMyInventory i, MyItemType type)
        {
            return i.CanItemsBeAdded(type) ? MyFixedPoint.MultiplySafe(i.FreeVolume(), 1 / type.GetItemInfo().Volume) : MyFixedPoint.Zero;
        }
        public static MyFixedPoint TransferItemToSafe(this IMyInventory i, IMyInventory dstInventory, MyInventoryItem item, MyFixedPoint amount)
        {
            MyFixedPoint safeAmount = MyFixedPoint.Min(dstInventory.FitItems(item.Type), amount);
            return safeAmount > MyFixedPoint.Zero && i.TransferItemTo(dstInventory, item, safeAmount) ? safeAmount : MyFixedPoint.Zero;
        }
        public static MyFixedPoint TransferItemFromSafe(this IMyInventory i, IMyInventory sourceInventory, MyInventoryItem item, MyFixedPoint amount)
        {
            return sourceInventory.TransferItemToSafe(i, item, amount);
        }
        static readonly List<MyInventoryItem> IMyInventory_GetItems_items = new List<MyInventoryItem>();
        public static List<MyInventoryItem> GetItems(this IMyInventory i)
        {
            IMyInventory_GetItems_items.Clear();
            i.GetItems(IMyInventory_GetItems_items);
            return IMyInventory_GetItems_items;
        }

        public static string Name(this MyItemType it)
        {
            return it.SubtypeId;
        }
        static readonly Dictionary<string, string> MyItemType_DisplayName_cache = new Dictionary<string, string>();
        static string MyItemType_DisplayName_cache_insert(string s)
        {
            var l = s.EndsWith("Item") ? s.Length - 4 : s.Length;
            var sb = new StringBuilder(s[0].ToString());
            for (int i = 1; i < l; ++i)
            {
                if (s[i] == 'G' && i + 8 < l && s.Substring(i, 8) == "Gun_Mag_")
                {
                    sb.Append(" Magazine");
                    break;
                }
                if (
                    (char.IsNumber(s[i]) && char.IsLetter(s[i - 1]) && (i < 2 || !char.IsNumber(s[i - 2]))) ||
                    (char.IsUpper(s[i]) && char.IsLower(s[i - 1]))
                ) sb.Append(' ');
                sb.Append(s[i]);
            }
            var displayName = sb.ToString();
            MyItemType_DisplayName_cache.Add(s, displayName);
            return displayName;
        }
        public static string DisplayName(this MyItemType it)
        {
            string displayName;
            MyItemType_DisplayName_cache.TryGetValue(it.SubtypeId, out displayName);
            return displayName ?? MyItemType_DisplayName_cache_insert(it.SubtypeId);
        }
        static readonly Dictionary<string, string> MyItemType_Group_cache = new Dictionary<string, string>()
        {
            { "MyObjectBuilder_ConsumableItem", "Consumable" },
            { "MyObjectBuilder_Datapad", "Other" },
            { "MyObjectBuilder_GasContainerObject", "Bottle" },
            { "MyObjectBuilder_OxygenContainerObject", "Bottle" },
            { "MyObjectBuilder_Package", "Other" },
            { "MyObjectBuilder_PhysicalObject", "Other" },
        };
        static string MyItemType_Group_cache_insert(MyItemType it)
        {
            var info = it.GetItemInfo();
            var group =
                info.IsAmmo ? "Ammo" :
                info.IsComponent ? "Component" :
                info.IsIngot ? "Ingot" :
                info.IsOre ? "Ore" :
                info.IsTool ? "Tool" :
                it.TypeId.Substring(it.TypeId.LastIndexOf("_") + 1);
            MyItemType_Group_cache.Add(it.TypeId, group);
            return group;
        }
        public static string Group(this MyItemType it)
        {
            string group;
            MyItemType_Group_cache.TryGetValue(it.TypeId, out group);
            return group ?? MyItemType_Group_cache_insert(it);
        }

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
