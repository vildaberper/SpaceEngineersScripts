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
        void AddItemTarget(MyItemType type, IManagedInventory inventory, Filter filter)
        {
            List<ItemTarget> itemTargets;
            if (!state.itemTargets.TryGetValue(type, out itemTargets)) state.itemTargets.Add(type, itemTargets = new List<ItemTarget>());
            itemTargets.Add(new ItemTarget(inventory, filter));
        }

        IEnumerator<bool> Transfer(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            if (state.targetsUpdated)
            {
                foreach (var target in state.itemTargets) target.Value.Clear();

                foreach (var target in state.targets.ToList())
                {
                    foreach (var filter in target.Filters)
                    {
                        foreach (var type in filter.Types)
                        {
                            yield return true;
                            AddItemTarget(type, target, filter);
                        }
                    }
                }

                foreach (var itemTargets in state.itemTargets.Values)
                {
                    yield return true;
                    itemTargets.Sort(itemTargetComparer);
                }
            }

            foreach (var source in state.sources.ToList())
            {
                yield return true;
                if (!source.Ready) continue;

                foreach (var e in source.Inventory.SumItems().ToList())
                {
                    yield return true;

                    var type = e.Key;
                    List<ItemTarget> targets;
                    if (!state.itemTargets.TryGetValue(type, out targets)) continue;

                    var sourceAmount = e.Value;
                    var sourceFilters = source.ApplyFilters ? source.Filters.Where(filter => filter.Types.Contains(type)) : Filters.None;

                    foreach (var target in targets.ToList())
                    {
                        yield return true;
                        if (!target.Inventory.Ready || !target.Inventory.ApplyFilters || source.Inventory == target.Inventory.Inventory || target.Inventory.Inventory.IsFull) continue;

                        var targetAmount = target.Inventory.Inventory.GetItemAmount(type);

                        if (target.HasQuota && targetAmount >= target.Quota) continue;

                        var toTransfer = sourceAmount;
                        if (target.HasQuota) toTransfer = MyFixedPoint.Min(toTransfer, target.Quota - targetAmount);

                        foreach (var sourceFilter in sourceFilters)
                        {
                            if (sourceFilter.Priority < target.Filter.Priority) continue;
                            else if (sourceFilter.Priority == target.Filter.Priority && (sourceFilter.HasQuota || target.Filter.HasQuota))
                            {
                                if (sourceFilter.HasQuota && (!target.Filter.HasQuota || sourceFilter.Quota <= target.Filter.Quota))
                                {
                                    toTransfer = MyFixedPoint.Min(toTransfer, sourceAmount - sourceFilter.Quota);
                                }
                            }
                            else
                            {
                                toTransfer = MyFixedPoint.Min(toTransfer, sourceFilter.HasQuota ? sourceAmount - sourceFilter.Quota : MyFixedPoint.Zero);
                            }

                            if (toTransfer <= MyFixedPoint.Zero) break;
                        }
                        if (toTransfer <= MyFixedPoint.Zero) continue;

                        sourceAmount -= source.Inventory.TransferItemTypeTo(target.Inventory.Inventory, type, toTransfer);
                        if (sourceAmount <= MyFixedPoint.Zero) break;
                    }
                }
            }

            if (debugMode)
            {
                log.Append($"Sources ({state.sources.Count}):\n");
                foreach (var source in state.sources.ToList())
                {
                    yield return true;
                    log.Append($" - {source.Block.Name}{(source.Ready ? "" : "*")}\n");
                }
                log.Append($"Targets ({state.targets.Count}):\n");
                foreach (var e in state.itemTargets)
                {
                    yield return true;
                    if (e.Value.Count == 0) continue;
                    log.Append($" - {e.Key.DisplayName()} ({e.Key.Group()}):\n");
                    foreach (var itemTarget in e.Value)
                    {
                        log.Append($"   - {parseFilterPriority}{itemTarget.Priority}{(itemTarget.HasQuota ? $" {parseFilterQuota}{itemTarget.Quota}" : "")} {itemTarget.Inventory.Block.Name}\n");
                    }
                }
            }
        }
    }
}
