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
        public class FilterException : Exception
        {
            public FilterException(string filterString) : base($"Failed to parse filter '{filterString}'") { }
        }

        public class Filter
        {
            readonly HashSet<MyItemType> types;
            readonly int priority;
            readonly MyFixedPoint quota;

            public Filter(HashSet<MyItemType> types, int priority, MyFixedPoint quota)
            {
                this.types = types;
                this.priority = priority;
                this.quota = quota;
            }

            public HashSet<MyItemType> Types => types;
            public int Priority => priority;
            public MyFixedPoint Quota => quota;
            public bool HasQuota => quota != MyFixedPoint.MaxValue;

            public static bool TryParse(List<MyItemType> from, string s, out Filter result)
            {
                var args = s.ToLower().Split(Instance._parseFilterArgSeparators, StringSplitOptions.RemoveEmptyEntries);

                int priority = 0, quota = 0;
                bool hasPriority = false, hasQuota = false;
                var types = new HashSet<MyItemType>();
                foreach (var arg in args)
                {
                    if (arg[0] == 'p' && int.TryParse(arg.SubStr(1), out priority)) hasPriority = true;
                    else if (arg[0] == 'q' && int.TryParse(arg.SubStr(1), out quota)) hasQuota = true;
                    else
                    {
                        var add = arg[0] != '-';
                        if (!Instance.MatchItem(from, ref types, add ? arg : arg.SubStr(1), add))
                        {
                            result = null;
                            return false;
                        }
                    }
                }
                result = new Filter(types, hasPriority ? priority : defaultPriority, hasQuota ? quota : MyFixedPoint.MaxValue);
                return true;
            }
        }

        public class Filters : List<Filter>
        {
            public Filters() : base() { }

            public static Filters None = new Filters();

            /// <exception cref="FilterException"></exception>
            public static Filters Parse(IMyInventory inventory, string name, string data, string defaultFilter = "")
            {
                var filterStrings = new List<string>();

                MyIniParseResult result;
                string fromDataString;
                if (Instance.ini.TryParse(data, out result) && Instance.ini.Get(configSectionKey, "filter").TryGetString(out fromDataString)) filterStrings.AddArray(fromDataString.Split(Instance._parseFilterSeparators));

                string fromNameString;
                if (Util.StringSection(name, parseFilterPrefix, parseFilterSuffix, out fromNameString)) filterStrings.AddArray(fromNameString.Split(Instance._parseFilterSeparators));

                if (filterStrings.Count == 0 && defaultFilter.Length > 0) filterStrings.AddArray(defaultFilter.Split(Instance._parseFilterSeparators));

                var from = inventory.GetAcceptedItems();
                var filters = new Filters();
                foreach (string filterString in filterStrings)
                {
                    Filter filter;
                    if (Filter.TryParse(from, filterString, out filter)) filters.Add(filter);
                    else throw new FilterException(filterString);
                }

                return filters;
            }
        }
    }
}
