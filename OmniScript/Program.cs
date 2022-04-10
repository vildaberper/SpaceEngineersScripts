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

        const UpdateFrequency workUpdateFrequency = UpdateFrequency.Update10;
        const int workTargetInstructionCount = 1000;

        const UpdateFrequency scanUpdateFrequency = UpdateFrequency.Update100;
        const int scanTargetInstructionCount = 100;

        const UpdateFrequency logUpdateFrequency = UpdateFrequency.Update10;

        const string version = "0.0.1";
        #endregion

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
            Runtime.UpdateFrequency = workUpdateFrequency | scanUpdateFrequency | logUpdateFrequency;

            if (!Me.CustomName.EndsWith("(os)")) Me.CustomName += " (os)";

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
            var title = $"OmniScript v{version} [{logAnim[++logAnimIndex % logAnim.Length]}]";
            var content = $"Scanner {scanner.Log}\nWorker {worker.Log}";
            var frame = $"{title}\n{content}";
            if (logToEcho) Echo(frame);
            if (logToSurface)
            {
                Me.GetSurface(0).WriteText(content);
                Me.GetSurface(1).WriteText(title);
            }
        }

        class MyItemTypeComparer : IComparer<MyItemType>
        {
            public int Compare(MyItemType x, MyItemType y)
            {
                var g = x.Group().CompareTo(y.Group());
                return g != 0 ? g : x.Name().CompareTo(y.Name());
            }
        }
        readonly MyItemTypeComparer myItemTypeComparer = new MyItemTypeComparer();

        IEnumerator<bool> Work(StringBuilder log)
        {
            /*var itemSet = new HashSet<MyItemType>();

            foreach (var block in GridTerminalSystem.GetBlocks())
            {
                // var blockType = block.DefinitionDisplayNameText;

                for (int i = 0; i < block.InventoryCount; ++i)
                {
                    foreach (var item in block.GetInventory(i).GetItems())
                    {
                        var unused = !itemSet.Contains(item.Type) && itemSet.Add(item.Type);
                    }
                }
                yield return true;
            }

            var itemList = itemSet.ToList();
            itemList.Sort(myItemTypeComparer);

            var groupSet = new HashSet<string>();

            workLogBuilder.Append($"Found {itemList.Count} items:\n");
            foreach (var item in itemList)
            {
                var unused = !groupSet.Contains(item.Group()) && groupSet.Add(item.Group());
                workLogBuilder.Append($"{item.DisplayName()} ({item.Group()})\n");
                yield return true;
            }

            var groupList = groupSet.ToList();
            groupList.Sort();

            workLogBuilder.Append($"Found {groupList.Count} groups:\n");
            foreach(var group in groupList)
            {
                workLogBuilder.Append($"{group}\n");
                yield return true;
            }*/
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

            log.Append($"Sources ({state.sources.Count}):\n");
            foreach (var source in state.sources.ToList())
            {
                log.Append($"{source.GetType().Name.Substring(7)} ({source.Block.Name}){(source.Ready ? "" : "*")}\n");
                yield return true;
            }
            log.Append($"Targets ({state.targets.Count}):\n");
            foreach (var target in state.targets.ToList())
            {
                log.Append($"{target.GetType().Name.Substring(7)} ({target.Block.Name}){(target.Ready ? "" : "*")}\n");
                yield return true;
            }
        }

        IEnumerator<bool> Scan(StringBuilder log)
        {
            var blocks = state.blocks;
            var sources = new List<IManagedInventory>();
            var targets = new List<IManagedInventory>();
            var masterAssemblers = new HashSet<long>(state.masterAssemblers);
            var slaveAssemblers = new HashSet<long>(state.slaveAssemblers);
            var refineries = new HashSet<long>(state.refineries);
            var gasGenerators = new HashSet<long>(state.gasGenerators);
            var oxygenTanks = new HashSet<long>(state.oxygenTanks);
            var hydrogenTanks = new HashSet<long>(state.hydrogenTanks);
            var batteryBlocks = new HashSet<long>(state.batteryBlocks);
            var reactors = new HashSet<long>(state.reactors);

            Action<long> remove = (id) =>
            {
                blocks.Remove(id);
                masterAssemblers.Remove(id);
                slaveAssemblers.Remove(id);
                refineries.Remove(id);
                gasGenerators.Remove(id);
                oxygenTanks.Remove(id);
                hydrogenTanks.Remove(id);
                batteryBlocks.Remove(id);
                reactors.Remove(id);
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
                if (blocks.ContainsKey(id)) continue;

                if (block is IMyAssembler)
                {
                    var assembler = new ManagedAssembler((IMyAssembler)block);
                    blocks.Add(id, assembler);
                    //sources.Add(assembler.Input);
                    //sources.Add(assembler.Output);
                    if (block.IsSameConstructAs(Me)) (assembler.DefinedMaster ? masterAssemblers : slaveAssemblers).Add(id);
                    log.Append($"Assembler ({block.CustomName})\n");
                }
                else if (block is IMyRefinery)
                {
                    var refinery = new ManagedRefinery((IMyRefinery)block);
                    blocks.Add(id, refinery);
                    //sources.Add(refinery.Input);
                    //targets.Add(refinery.Input);
                    //sources.Add(refinery.Output);
                    if (block.IsSameConstructAs(Me)) refineries.Add(id);
                    log.Append($"Refinery ({block.CustomName})\n");
                }
                else if (block is IMyGasGenerator)
                {
                    var gasGenerator = new ManagedGasGenerator((IMyGasGenerator)block);
                    blocks.Add(id, gasGenerator);
                    //sources.Add(gasGenerator.Inventory);
                    //targets.Add(gasGenerator.Inventory);
                    if (block.IsSameConstructAs(Me)) gasGenerators.Add(id);
                    log.Append($"GasGenerator ({block.CustomName})\n");
                }
                else if (block is IMyGasTank)
                {
                    var gasTank = new ManagedGasTank((IMyGasTank)block);
                    blocks.Add(id, gasTank);
                    if (block.IsSameConstructAs(Me))
                    {
                        if (gasTank.IsOxygen) oxygenTanks.Add(id);
                        else if (gasTank.IsHydrogen) hydrogenTanks.Add(id);
                    }
                    log.Append($"GasTank '{(gasTank.IsOxygen ? "Oxygen" : gasTank.IsHydrogen ? "Hydrogen" : "Other")}' ({block.CustomName})\n");
                }
                else if (block is IMyBatteryBlock)
                {
                    var batteryBlock = new ManagedBatteryBlock((IMyBatteryBlock)block);
                    blocks.Add(id, batteryBlock);
                    if (block.IsSameConstructAs(Me)) batteryBlocks.Add(id);
                    log.Append($"BatteryBlock ({block.CustomName})\n");
                }
                else if (block is IMyReactor)
                {
                    var reactor = new ManagedReactor((IMyReactor)block);
                    blocks.Add(id, reactor);
                    //sources.Add(reactor.Inventory);
                    //targets.Add(reactor.Inventory);
                    if (block.IsSameConstructAs(Me)) reactors.Add(id);
                    log.Append($"Reactor ({block.CustomName})\n");
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
                reactors
            );
        }
    }
}
