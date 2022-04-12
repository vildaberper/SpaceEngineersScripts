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
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        readonly bool logToEcho = true;
        readonly bool logToSurface = false;

        const UpdateFrequency manageUpdateFrequency = UpdateFrequency.Update100;
        const int manageTargetInstructionCount = 1000;

        const UpdateFrequency workUpdateFrequency = UpdateFrequency.Update10;
        const int workTargetInstructionCount = 1000;

        const UpdateFrequency scanUpdateFrequency = UpdateFrequency.Update100;
        const int scanTargetInstructionCount = 1000;

        const UpdateFrequency logUpdateFrequency = UpdateFrequency.Update10;

        const int defaultPriority = 0;
        const string defaultRefineryFilter = "* p100 q1000";
        const string defaultGasGeneratorFilter = "ice p100";
        const string defaulReactorFilter = "* p100 q100";

        readonly Dictionary<string, string> defaultFilters = new Dictionary<string, string>()
        {
            {"Advanced Cargo Container",""},
            {"Large Cargo Container",""},
            {"Small Cargo Container",""},
            {"Cryo Chamber",""},
            {"Gatling Turret","* p100 q10"},
            {"Missile Turret","* p100 q10"},
            {"Artillery Turret","* p100 q10"},
            {"Assault Cannon Turret","* p100 q10"},
            {"Interior Turret","* p100 q10"},
            {"Cockpit",""},
            {"Drill",""},
            {"Grinder",""}
        };

        const string ignoreTag = "!";
        const string configSectionKey = "os";
        const string parseFilterPrefix = "(";
        const string parseFilterSuffix = " os)";
        const string parseFilterAll = "*";
        const string parseFilterSeparators = "\n;";
        const string parseFilterArgSeparators = " ";

        const string version = "0.0.1";
        #endregion

        public static Program Instance { get; private set; }

        const string myName = "(os)";

        public readonly char[] _parseFilterSeparators = parseFilterSeparators.ToCharArray();
        public readonly char[] _parseFilterArgSeparators = parseFilterArgSeparators.ToCharArray();

        public readonly MyIni ini = new MyIni();
        public readonly ItemTargetComparer itemTargetComparer = new ItemTargetComparer();

        public readonly List<IMyTerminalBlock> IMyGridTerminalSystem_GetBlocks_blocks = new List<IMyTerminalBlock>();
        public readonly List<MyItemType> IMyInventory_GetAcceptedItems_itemTypes = new List<MyItemType>();
        public readonly List<MyInventoryItem> IMyInventory_GetItems_items = new List<MyInventoryItem>();

        readonly UpdateType manageUpdateType = manageUpdateFrequency.ToUpdateType();
        readonly UpdateType workUpdateType = workUpdateFrequency.ToUpdateType();
        readonly UpdateType scanUpdateType = scanUpdateFrequency.ToUpdateType();
        readonly UpdateType logUpdateType = logUpdateFrequency.ToUpdateType();

        readonly string[] logAnim = new string[] {
            "....",
            ":...",
            "::..",
            ":::.",
            "::::",
            ".:::",
            "..::",
            "...:",
            "....",
            "...:",
            "..::",
            ".:::",
            "::::",
            ":::.",
            "::..",
            ":...",
        };
        int logAnimIndex = -1;

        readonly State state;
        readonly Worker manager;
        readonly Worker worker;
        readonly Worker scanner;

        public Program()
        {
            Instance = this;

            Runtime.UpdateFrequency = manageUpdateFrequency | workUpdateFrequency | scanUpdateFrequency | logUpdateFrequency;

            if (!Me.CustomName.EndsWith(myName)) Me.CustomName += $" {myName}";

            if (logToSurface)
            {
                Me.GetSurface(0).Prepare();
                Me.GetSurface(1).Prepare(Alignment: TextAlignment.CENTER, FontSize: 4f, TextPadding: 34f);
            }

            state = new State();
            manager = new Worker(Runtime, Manage);
            worker = new Worker(Runtime, Work);
            scanner = new Worker(Runtime, Scan);

            // Load Storage (string)

            // https://github.com/malware-dev/MDK-SE/wiki/Handling-configuration-and-storage
            // Config in CustomData?
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means.
            // 
            // This method is optional and can be removed if not
            // needed.

            // https://github.com/malware-dev/MDK-SE/wiki/The-Storage-String
            // Storage = string
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & manageUpdateType) != 0) manager.Cycle(manageTargetInstructionCount);
            if ((updateSource & scanUpdateType) != 0) scanner.Cycle(scanTargetInstructionCount);
            if ((updateSource & workUpdateType) != 0) worker.Cycle(workTargetInstructionCount);
            if ((updateSource & logUpdateType) != 0) Log();
        }

        public void Log()
        {
            var header = $"OmniScript v{version} [{logAnim[++logAnimIndex % logAnim.Length]}]";
            var content = $"Manager {manager.Log}\nScanner {scanner.Log}\nWorker {worker.Log}";
            var frame = $"{header}\n\n{content}";
            if (logToEcho) Echo(frame);
            if (logToSurface)
            {
                Me.GetSurface(0).WriteText(content);
                Me.GetSurface(1).WriteText(header);
            }
        }

        IEnumerator<bool> Manage(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            if (state.masterAssemblers.Count > 0)
            {
                var isQueueEmpty = true;
                foreach (var id in state.masterAssemblers.ToList())
                {
                    yield return true;
                    var assembler = state.blocks.Assembler(id);
                    assembler.CooperativeMode = false;
                    isQueueEmpty &= assembler.IsQueueEmpty;
                }
                foreach (var id in state.slaveAssemblers.ToList())
                {
                    yield return true;
                    var assembler = state.blocks.Assembler(id);
                    assembler.CooperativeMode = true;
                    if (isQueueEmpty) assembler.ClearQueue();
                }
            }
        }

        private void AddItemTarget(MyItemType type, IManagedInventory inventory, Filter filter)
        {
            List<ItemTarget> itemTargets;
            if (!state.itemTargets.TryGetValue(type, out itemTargets)) state.itemTargets.Add(type, itemTargets = new List<ItemTarget>());
            itemTargets.Add(new ItemTarget(inventory, filter));
        }
        IEnumerator<bool> Work(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            if (state.targetsUpdated)
            {
                foreach (var target in state.itemTargets) target.Value.Clear();

                foreach (var target in state.targets.ToList())
                {
                    yield return true;
                    foreach (var filter in target.Filters)
                    {
                        foreach (var type in filter.Types)
                        {
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
                log.Append($" - {e.Key.DisplayName()}:\n");
                foreach (var itemTarget in e.Value)
                {
                    log.Append($"   - p{itemTarget.Priority}{(itemTarget.HasQuota ? $" q{itemTarget.Quota}" : "")} {itemTarget.Inventory.Block.Name}\n");
                }
            }
        }

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
                // Performance hit?
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
                if (scanned.Contains(id)) continue;
                scanned.Add(id);

                if (block.CustomName.IndexOf(ignoreTag) >= 0)
                {
                    blocks.Add(id, new WatchedBlock(block));
                    continue;
                }

                string filter;

                if (block is IMyAssembler)
                {
                    var assembler = new ManagedAssembler((IMyAssembler)block);
                    blocks.Add(id, assembler);
                    sources.Add(assembler.Input);
                    sources.Add(assembler.Output);
                    (assembler.DefinedMaster ? masterAssemblers : slaveAssemblers).Add(id);
                }
                else if (block is IMyRefinery)
                {
                    var refinery = new ManagedRefinery((IMyRefinery)block);
                    blocks.Add(id, refinery);
                    sources.Add(refinery.Input);
                    if (refinery.Input.Filters.Count > 0) targets.Add(refinery.Input);
                    sources.Add(refinery.Output);
                    refineries.Add(id);
                }
                else if (block is IMyGasGenerator)
                {
                    var gasGenerator = new ManagedGasGenerator((IMyGasGenerator)block);
                    blocks.Add(id, gasGenerator);
                    sources.Add(gasGenerator.Inventory);
                    if (gasGenerator.Inventory.Filters.Count > 0) targets.Add(gasGenerator.Inventory);
                    gasGenerators.Add(id);
                }
                else if (block is IMyGasTank)
                {
                    var gasTank = new ManagedGasTank((IMyGasTank)block);
                    blocks.Add(id, gasTank);
                    sources.Add(gasTank.Inventory);
                    if (gasTank.Inventory.Filters.Count > 0) targets.Add(gasTank.Inventory);
                    if (gasTank.IsOxygen) oxygenTanks.Add(id);
                    else if (gasTank.IsHydrogen) hydrogenTanks.Add(id);
                }
                else if (block is IMyBatteryBlock)
                {
                    var batteryBlock = new ManagedBatteryBlock((IMyBatteryBlock)block);
                    blocks.Add(id, batteryBlock);
                    batteryBlocks.Add(id);
                }
                else if (block is IMyReactor)
                {
                    var reactor = new ManagedReactor((IMyReactor)block);
                    blocks.Add(id, reactor);
                    sources.Add(reactor.Inventory);
                    if (reactor.Inventory.Filters.Count > 0) targets.Add(reactor.Inventory);
                    reactors.Add(id);
                }
                else if (block is IMyShipConnector)
                {
                    var shipConnector = new ManagedShipConnector((IMyShipConnector)block);
                    blocks.Add(id, shipConnector);
                    sources.Add(shipConnector.Inventory);
                    if (shipConnector.Inventory.Filters.Count > 0) targets.Add(shipConnector.Inventory);
                    shipConnectors.Add(id);
                }
                else if (block is IMyShipWelder)
                {
                    var shipWelder = new ManagedShipWelder((IMyShipWelder)block);
                    blocks.Add(id, shipWelder);
                    sources.Add(shipWelder.Inventory);
                    if (shipWelder.Inventory.Filters.Count > 0) targets.Add(shipWelder.Inventory);
                }
                else if (block.HasInventory && defaultFilters.TryGetValue(block.DefinitionDisplayNameText, out filter))
                {
                    if (block is IMyProductionBlock)
                    {
                        var productionBlock = new ManagedProductionBlock((IMyProductionBlock)block, filter);
                        blocks.Add(id, productionBlock);
                        sources.Add(productionBlock.Output);
                        if (productionBlock.Input.Filters.Count > 0) targets.Add(productionBlock.Input);
                    }
                    else
                    {
                        var inventoryBlock = new ManagedInventoryBlock(block, filter);
                        blocks.Add(id, inventoryBlock);
                        sources.Add(inventoryBlock.Inventory);
                        if (inventoryBlock.Inventory.Filters.Count > 0) targets.Add(inventoryBlock.Inventory);
                    }
                }
                else
                {
                    // blocks.Add(id, new WatchedBlock(block));
                    // scanned.Remove(id);
                    log.Append($"{block.DefinitionDisplayNameText} ({block.CustomName})\n");
                }
            }
            if (targetsCount != targets.Count) targetsUpdated = true;

            var errorBlocks = blocks.Where(e => e.Value.HasError).ToList();
            if (errorBlocks.Count > 0)
            {
                log.Append($"Errors ({errorBlocks.Count}):\n");
                foreach (var e in errorBlocks)
                {
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
