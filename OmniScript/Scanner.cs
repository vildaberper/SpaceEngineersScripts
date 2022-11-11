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
        IEnumerator<bool> Scan(StringBuilder log)
        {
            var blocks = state.blocks;
            var scanned = state.scanned;

            var targetsUpdated = false;
            var sources = new List<IManagedInventory>(state.sources);
            var targets = new List<IManagedInventory>(state.targets);
            var masterAssemblers = new HashSet<long>(state.masterAssemblers);
            var slaveAssemblers = new HashSet<long>(state.slaveAssemblers);
            var refineries = new HashSet<long>(state.refineries);
            var gasGenerators = new HashSet<long>(state.gasGenerators);
            var oxygenTanks = new HashSet<long>(state.oxygenTanks);
            var hydrogenTanks = new HashSet<long>(state.hydrogenTanks);
            var batteryBlocks = new HashSet<long>(state.batteryBlocks);
            var reactors = new HashSet<long>(state.reactors);
            var shipConnectors = new HashSet<long>(state.shipConnectors);

            Action<long> remove = (id) =>
            {
                blocks.Remove(id);
                scanned.Remove(id);
                sources.RemoveAll(source => source.Block.Block.EntityId == id);
                targets.RemoveAll(target => target.Block.Block.EntityId == id);
                masterAssemblers.Remove(id);
                slaveAssemblers.Remove(id);
                refineries.Remove(id);
                gasGenerators.Remove(id);
                oxygenTanks.Remove(id);
                hydrogenTanks.Remove(id);
                batteryBlocks.Remove(id);
                reactors.Remove(id);
                shipConnectors.Remove(id);
            };

            var targetsCount = targets.Count;
            foreach (var e in blocks.ToList())
            {
                yield return true;
                if (e.Value.Closed || e.Value.Changed) remove(e.Key);
            }
            if (targetsCount != targets.Count) targetsUpdated = true;

            foreach (var block in GridTerminalSystem.GetBlocks())
            {
                yield return true;
                var id = block.EntityId;
                var local = block.CubeGrid == Me.CubeGrid;
                if (scanned.Contains(id)) continue;
                scanned.Add(id);

                if (block.CustomName.IndexOf(ignoreTag) >= 0)
                {
                    blocks.Add(id, new WatchedBlock(block));
                    continue;
                }

                if (manageAssemblers && block is IMyAssembler)
                {
                    var assembler = new ManagedAssembler((IMyAssembler)block);
                    blocks.Add(id, assembler);
                    sources.Add(assembler.Input);
                    sources.Add(assembler.Output);
                    if (assembler.Input.Filters.Count > 0) targets.Add(assembler.Input);
                    if (local) (assembler.DefinedMaster ? masterAssemblers : slaveAssemblers).Add(id);
                }
                else if (manageRefineries && block is IMyRefinery)
                {
                    var refinery = new ManagedRefinery((IMyRefinery)block);
                    blocks.Add(id, refinery);
                    sources.Add(refinery.Input);
                    if (refinery.Input.Filters.Count > 0) targets.Add(refinery.Input);
                    sources.Add(refinery.Output);
                    if (local) refineries.Add(id);
                }
                else if (manageGasGenerators && block is IMyGasGenerator)
                {
                    var gasGenerator = new ManagedGasGenerator((IMyGasGenerator)block);
                    blocks.Add(id, gasGenerator);
                    sources.Add(gasGenerator.Inventory);
                    if (gasGenerator.Inventory.Filters.Count > 0) targets.Add(gasGenerator.Inventory);
                    if (local) gasGenerators.Add(id);
                }
                else if (manageGasTanks && block is IMyGasTank)
                {
                    var gasTank = new ManagedGasTank((IMyGasTank)block);
                    blocks.Add(id, gasTank);
                    sources.Add(gasTank.Inventory);
                    if (gasTank.Inventory.Filters.Count > 0) targets.Add(gasTank.Inventory);
                    if (local)
                    {
                        if (gasTank.IsOxygen) oxygenTanks.Add(id);
                        else if (gasTank.IsHydrogen) hydrogenTanks.Add(id);
                    }
                }
                else if (manageBatteryBlocks && block is IMyBatteryBlock)
                {
                    var batteryBlock = new ManagedBatteryBlock((IMyBatteryBlock)block);
                    blocks.Add(id, batteryBlock);
                    if(local) batteryBlocks.Add(id);
                }
                else if (manageReactors && block is IMyReactor)
                {
                    var reactor = new ManagedReactor((IMyReactor)block);
                    blocks.Add(id, reactor);
                    sources.Add(reactor.Inventory);
                    if (reactor.Inventory.Filters.Count > 0) targets.Add(reactor.Inventory);
                    if (local) reactors.Add(id);
                }
                else if (manageShipConnectors && block is IMyShipConnector)
                {
                    var shipConnector = new ManagedShipConnector((IMyShipConnector)block);
                    blocks.Add(id, shipConnector);
                    sources.Add(shipConnector.Inventory);
                    if (shipConnector.Inventory.Filters.Count > 0) targets.Add(shipConnector.Inventory);
                    if (local) shipConnectors.Add(id);
                }
                else if (manageShipWelders && block is IMyShipWelder)
                {
                    var shipWelder = new ManagedShipWelder((IMyShipWelder)block);
                    blocks.Add(id, shipWelder);
                    sources.Add(shipWelder.Inventory);
                    if (shipWelder.Inventory.Filters.Count > 0) targets.Add(shipWelder.Inventory);
                }
                else if (block.HasInventory && defaultFilters.ContainsKey(block.DefinitionDisplayNameText))
                {
                    if (block is IMyProductionBlock)
                    {
                        var productionBlock = new ManagedProductionBlock((IMyProductionBlock)block);
                        blocks.Add(id, productionBlock);
                        sources.Add(productionBlock.Output);
                        if (productionBlock.Input.Filters.Count > 0) targets.Add(productionBlock.Input);
                    }
                    else
                    {
                        var inventoryBlock = new ManagedInventoryBlock(block);
                        blocks.Add(id, inventoryBlock);
                        sources.Add(inventoryBlock.Inventory);
                        if (inventoryBlock.Inventory.Filters.Count > 0) targets.Add(inventoryBlock.Inventory);
                    }
                }
                else if (block.HasInventory)
                {
                    blocks.Add(id, new WatchedBlock(block));
                    // scanned.Remove(id);
                    // log.Append($"{block.DefinitionDisplayNameText} ({block.CustomName})\n");
                }
            }
            if (targetsCount != targets.Count) targetsUpdated = true;

            var errorBlocks = blocks.Where(e => e.Value.HasError).ToList();
            if (errorBlocks.Count > 0)
            {
                log.Append($"Errors ({errorBlocks.Count}):\n");
                foreach (var e in errorBlocks)
                {
                    yield return true;
                    log.Append($" - {e.Value.Name}: {e.Value.Error}\n");
                }
            }

            state.Update(
                targetsUpdated,
                sources,
                targets,
                masterAssemblers,
                slaveAssemblers,
                refineries,
                gasGenerators,
                oxygenTanks,
                hydrogenTanks,
                batteryBlocks,
                reactors,
                shipConnectors
            );
        }
    }
}
