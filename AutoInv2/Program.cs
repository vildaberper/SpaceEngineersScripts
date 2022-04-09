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

            if (!Me.CustomName.EndsWith("(ai)")) Me.CustomName += " (ai)";

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
            var title = $"AutoInv2 v{version} [{logAnim[++logAnimIndex % logAnim.Length]}]";
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
                foreach (var assembler in state.masterAssemblers.ToList())
                {
                    yield return true;
                    assembler.CooperativeMode = false;
                    isQueueEmpty &= assembler.IsQueueEmpty;
                }
                foreach (var assembler in state.slaveAssemblers.ToList())
                {
                    yield return true;
                    assembler.CooperativeMode = true;
                    if (isQueueEmpty) assembler.ClearQueue();
                }
            }

            log.Append($"Sources ({state.sources.Count}):\n");
            foreach (var source in state.sources.ToList())
            {
                log.Append($"{source.GetType().Name.Substring(7)} ({source.Block.CustomName}){(source.Ready ? "" : "*")}\n");
                yield return true;
            }
            log.Append($"Targets ({state.targets.Count}):\n");
            foreach (var target in state.targets.ToList())
            {
                log.Append($"{target.GetType().Name.Substring(7)} ({target.Block.CustomName}){(target.Ready ? "" : "*")}\n");
                yield return true;
            }
        }

        IEnumerator<bool> Scan(StringBuilder log)
        {
            var sources = new List<IManagedInventory>();
            var targets = new List<IManagedInventory>();
            var masterAssemblers = new List<ManagedAssemblerInput>();
            var slaveAssemblers = new List<ManagedAssemblerInput>();
            var oxygenTanks = new List<IMyGasTank>();
            var hydrogenTanks = new List<IMyGasTank>();
            var batteryBlocks = new List<IMyBatteryBlock>();
            var reactors = new List<ManagedReactor>();

            foreach (var block in GridTerminalSystem.GetBlocks())
            {
                yield return true;
                if (block is IMyAssembler)
                {
                    var assembler = (IMyAssembler)block;
                    var input = new ManagedAssemblerInput(assembler);
                    sources.Add(input);
                    sources.Add(new ManagedOutput(assembler));
                    if (block.IsSameConstructAs(Me)) (input.DefinedMaster ? masterAssemblers : slaveAssemblers).Add(input);
                    log.Append($"Assembler ({block.CustomName})\n");
                }
                else if (block is IMyRefinery)
                {
                    var refinery = (IMyRefinery)block;
                    var input = new ManagedRefineryInput(refinery);
                    sources.Add(input);
                    targets.Add(input);
                    sources.Add(new ManagedOutput(refinery));
                    log.Append($"Refinery ({block.CustomName})\n");
                }
                else if (block is IMyGasGenerator)
                {
                    var gasGenerator = (IMyGasGenerator)block;
                    var managed = new ManagedGasGenerator(gasGenerator);
                    sources.Add(managed);
                    targets.Add(managed);
                    log.Append($"GasGenerator ({block.CustomName})\n");
                }
                else if (block is IMyCargoContainer)
                {
                    log.Append($"CargoContainer ({block.CustomName})\n");
                }
                else if (block is IMyReactor)
                {
                    var reactor = (IMyReactor)block;
                    var managed = new ManagedReactor(reactor);
                    sources.Add(managed);
                    targets.Add(managed);
                    if (block.IsSameConstructAs(Me)) reactors.Add(managed);
                    log.Append($"Reactor ({block.CustomName})\n");
                }
                else if (block is IMyGasTank)
                {
                    var gasTank = (IMyGasTank)block;
                    if (block.IsSameConstructAs(Me))
                    {
                        if (gasTank.IsOxygen()) oxygenTanks.Add(gasTank);
                        else if (gasTank.IsHydrogen()) hydrogenTanks.Add(gasTank);
                    }
                    log.Append($"GasTank '{(gasTank.IsOxygen() ? "Oxygen" : gasTank.IsHydrogen() ? "Hydrogen" : "Other")}' ({block.CustomName})\n");
                }
                else if (block is IMyBatteryBlock)
                {
                    var batteryBlock = (IMyBatteryBlock)block;
                    if (block.IsSameConstructAs(Me)) batteryBlocks.Add(batteryBlock);
                    log.Append($"BatteryBlock ({block.CustomName})\n");
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
                oxygenTanks,
                hydrogenTanks,
                batteryBlocks,
                reactors
            );
        }
    }
}
