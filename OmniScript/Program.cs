﻿using Sandbox.Game.EntityComponents;
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

        const UpdateFrequency workUpdateFrequency = UpdateFrequency.Update10;
        const int workTargetInstructionCount = 1000;

        const UpdateFrequency scanUpdateFrequency = UpdateFrequency.Update100;
        const int scanTargetInstructionCount = 1000;

        const UpdateFrequency logUpdateFrequency = UpdateFrequency.Update10;

        const int defaultPriority = 0;

        const string defaultAssemblerFilter = "";
        const string defaultRefineryFilter = "* p100 q1000";
        const string defaultGasGeneratorFilter = "ice p100";
        const string defaulReactorFilter = "* p100 q100";

        const string configSectionKey = "os";
        const string parseFilterPrefix = "(";
        const string parseFilterSuffix = " os)";
        const string parseFilterAll = "*";
        const string parseFilterSeparators = "\n;";
        const string parseFilterArgSeparators = " ";

        const string version = "0.0.1";
        #endregion

        public static Program Instance { get; protected set; }

        const string myName = "(os)";

        public readonly char[] _parseFilterSeparators = parseFilterSeparators.ToCharArray();
        public readonly char[] _parseFilterArgSeparators = parseFilterArgSeparators.ToCharArray();

        public readonly MyIni ini = new MyIni();
        public readonly ItemTargetComparer itemTargetComparer = new ItemTargetComparer();
        public readonly List<IMyTerminalBlock> IMyGridTerminalSystem_GetBlocks_blocks = new List<IMyTerminalBlock>();
        public readonly List<MyItemType> IMyInventory_GetAcceptedItems_itemTypes = new List<MyItemType>();
        public readonly List<MyInventoryItem> IMyInventory_GetItems_items = new List<MyInventoryItem>();
        public readonly Dictionary<MyItemType, Item> itemTypes = new Dictionary<MyItemType, Item>();

        readonly UpdateType scanUpdateType = scanUpdateFrequency.ToUpdateType();
        readonly UpdateType workUpdateType = workUpdateFrequency.ToUpdateType();
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
        readonly Worker worker;
        readonly Worker scanner;

        public Program()
        {
            Instance = this;

            Runtime.UpdateFrequency = workUpdateFrequency | scanUpdateFrequency | logUpdateFrequency;

            if (!Me.CustomName.EndsWith(myName)) Me.CustomName += $" {myName}";

            if (logToSurface)
            {
                Me.GetSurface(0).Prepare();
                Me.GetSurface(1).Prepare(Alignment: TextAlignment.CENTER, FontSize: 4f, TextPadding: 34f);
            }

            state = new State();
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
            if ((updateSource & scanUpdateType) != 0) scanner.Cycle(scanTargetInstructionCount);
            if ((updateSource & workUpdateType) != 0) worker.Cycle(workTargetInstructionCount);
            if ((updateSource & logUpdateType) != 0) Log();
        }

        public void Log()
        {
            var header = $"OmniScript v{version} [{logAnim[++logAnimIndex % logAnim.Length]}]";
            var content = $"Scanner {scanner.Log}\nWorker {worker.Log}";
            var frame = $"{header}\n\n{content}";
            if (logToEcho) Echo(frame);
            if (logToSurface)
            {
                Me.GetSurface(0).WriteText(content);
                Me.GetSurface(1).WriteText(header);
            }
        }

        IEnumerator<bool> Work(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            var errorBlocks = state.blocks.Where(e => e.Value.HasError).ToList();
            if (errorBlocks.Count > 0)
            {
                log.Append($"Errors ({errorBlocks.Count}):\n");
                foreach (var e in errorBlocks)
                {
                    log.Append($" - {e.Value.Name}: {e.Value.Error}\n");
                }
            }

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

            var targets = state.itemTargets;
            foreach (var target in targets) target.Value.Clear();

            Action<MyItemType, IManagedInventory, Filter> add = (type, inventory, filter) =>
            {
                List<ItemTarget> itemTargets;
                if (!targets.TryGetValue(type, out itemTargets)) targets.Add(type, itemTargets = new List<ItemTarget>());

                itemTargets.Add(new ItemTarget(inventory, filter));
            };

            foreach (var target in state.targets.ToList())
            {
                yield return true;
                foreach (var filter in target.Filters)
                {
                    foreach (var type in filter.Types)
                    {
                        add(type, target, filter);
                    }
                }
            }

            foreach (var itemTargets in targets.Values)
            {
                yield return true;
                itemTargets.Sort(itemTargetComparer);
            }

            log.Append($"Sources ({state.sources.Count}):\n");
            foreach (var source in state.sources.ToList())
            {
                yield return true;
                log.Append($" - {source.Block.Name}{(source.Ready ? "" : "*")}\n");
            }
            log.Append($"Targets ({state.targets.Count}):\n");
            foreach (var e in targets)
            {
                yield return true;
                log.Append($" - {e.Key.DisplayName()}:\n");
                foreach (var itemTarget in e.Value)
                {
                    log.Append($"   - p{itemTarget.Priority}{(itemTarget.HasQuota ? $" q{itemTarget.Quota}" : "")} {itemTarget.Inventory.Block.Name}\n");
                }
            }
            /*foreach (var target in state.targets.ToList())
            {
                log.Append($"{target.GetType().Name.SubStr(7)} ({target.Block.Name}){(target.Ready ? "" : "*")}\n");
                foreach (var filter in target.Filters)
                {
                    log.Append($" * {filter.Types.Count} p{filter.Priority}{(filter.HasQuota ? $" q{filter.Quota}" : "")}\n");
                    foreach (var type in filter.Types)
                    {
                        log.Append($"  - {type.DisplayName()} {type.Group()}\n");
                    }
                }
                yield return true;
            }*/
        }

        IEnumerator<bool> Scan(StringBuilder log)
        {
            var blocks = state.blocks;
            var scanned = state.scanned;

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

            foreach (var e in blocks.ToList())
            {
                yield return true;
                if (e.Value.Closed || e.Value.Changed) remove(e.Key);
            }

            foreach (var block in GridTerminalSystem.GetBlocks())
            {
                yield return true;
                var id = block.EntityId;
                if (scanned.Contains(id)) continue;
                scanned.Add(id);

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
                    targets.Add(refinery.Input);
                    sources.Add(refinery.Output);
                    refineries.Add(id);
                }
                else if (block is IMyGasGenerator)
                {
                    var gasGenerator = new ManagedGasGenerator((IMyGasGenerator)block);
                    blocks.Add(id, gasGenerator);
                    sources.Add(gasGenerator.Inventory);
                    targets.Add(gasGenerator.Inventory);
                    gasGenerators.Add(id);
                }
                else if (block is IMyGasTank)
                {
                    var gasTank = new ManagedGasTank((IMyGasTank)block);
                    blocks.Add(id, gasTank);
                    sources.Add(gasTank.Inventory);
                    targets.Add(gasTank.Inventory);
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
                    targets.Add(reactor.Inventory);
                    reactors.Add(id);
                }
                else if (block is IMyShipConnector)
                {
                    var shipConnector = new ManagedShipConnector((IMyShipConnector)block);
                    blocks.Add(id, shipConnector);
                    sources.Add(shipConnector.Inventory);
                    targets.Add(shipConnector.Inventory);
                    shipConnectors.Add(id);
                }
                else if (block is IMyShipWelder)
                {
                    var shipWelder = new ManagedShipWelder((IMyShipWelder)block);
                    blocks.Add(id, shipWelder);
                    sources.Add(shipWelder.Inventory);
                }
                else if (block is IMyCargoContainer)
                {
                    var cargoContainer = (IMyCargoContainer)block;
                    log.Append($"CargoContainer ({block.CustomName})\n");
                }
                else
                {
                    log.Append($"Other {block.GetType().Name} ({block.CustomName})\n");
                }
            }
            state.Update(
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
