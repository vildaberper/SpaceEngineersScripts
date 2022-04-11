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
        public class Item
        {
            readonly string displayName, group, matchName, matchGroup;

            public Item(MyItemType it)
            {
                displayName = MakeDisplayName(it);
                group = MakeGroup(it);
                matchName = displayName.Replace(" ", "").ToLower();
                matchGroup = group.Replace(" ", "").ToLower();
            }

            public string DisplayName => displayName;
            public string Group => group;

            public bool Match(string s)
            {
                if (s == parseFilterAll || matchGroup == s) return true;

                var i = s.IndexOf('/');
                return i >= 0
                    ? matchGroup.StartsWith(s.SubStr(0, i)) && matchName.StartsWith(s.SubStr(i + 1))
                    : matchName.StartsWith(s);
            }

            private static string MakeDisplayName(MyItemType it)
            {
                switch (it.ToString())
                {
                    case "MyObjectBuilder_Ingot/Stone":
                        return "Gravel";
                }

                var s = it.SubtypeId;
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
                return sb.ToString();
            }

            private static string MakeGroup(MyItemType it)
            {
                switch (it.TypeId)
                {
                    case "MyObjectBuilder_ConsumableItem":
                        return "Consumable";
                    case "MyObjectBuilder_GasContainerObject":
                        return "Bottle";
                    case "MyObjectBuilder_OxygenContainerObject":
                        return "Bottle";
                    case "MyObjectBuilder_Datapad":
                    case "MyObjectBuilder_Package":
                    case "MyObjectBuilder_PhysicalObject":
                        return "Other";
                }

                var info = it.GetItemInfo();
                return
                    info.IsAmmo ? "Ammo" :
                    info.IsComponent ? "Component" :
                    info.IsIngot ? "Ingot" :
                    info.IsOre ? "Ore" :
                    info.IsTool ? "Tool" :
                    it.TypeId.SubStr(it.TypeId.LastIndexOf("_") + 1);
            }
        }

        public static class Items
        {

            public static Item Get(MyItemType it)
            {
                Item item;
                if (!Instance.itemTypes.TryGetValue(it, out item)) Instance.itemTypes.Add(it, item = new Item(it));
                return item;
            }

            public static bool Match(List<MyItemType> from, ref HashSet<MyItemType> types, string s, bool add)
            {
                var startCount = types.Count;
                foreach (var it in from)
                {
                    if (!Get(it).Match(s)) continue;
                    if (add) types.Add(it);
                    else types.Remove(it);
                }
                return startCount != types.Count;
            }
        }
    }
}
