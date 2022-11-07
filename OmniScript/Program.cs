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
        const int defaultPriority = 0;

        readonly Dictionary<string, string> defaultFilters = new Dictionary<string, string>()
        {
            {"Advanced Cargo Container", ""},
            {"Artillery Turret", "p100 q10"},
            {"Assault Cannon Turret", "p100 q10"},
            {"Assembler", ""},
            {"Basic Assembler", ""},
            {"Basic Refinery", "p100 q1000"},
            {"Cockpit", ""},
            {"Cryo Chamber", ""},
            {"Connector", ""},
            {"Drill", ""},
            {"Fighter Cockpit", ""},
            {"Gatling Gun", "p100 q10"},
            {"Gatling Turret", "p100 q10"},
            {"Grinder", ""},
            {"Hydrogen Tank", ""},
            {"Interior Turret", "p100 q10"},
            {"Large Cargo Container", ""},
            {"Large Reactor", "p100 q100"},
            {"Medium Cargo Container", ""},
            {"Missile Turret", "p100 q10"},
            {"O2/H2 Generator", "ice p100"},
            {"Oxygen Tank", ""},
            {"Refinery", "p100 q1000"},
            {"Reloadable Rocket Launcher", "p100 q10"},
            {"Small Cargo Container", ""},
            {"Small Hydrogen Tank", ""},
            {"Small Reactor", "p100 q50"},
            {"Survival Kit", ""},
            {"Welder", ""},
        };

        const bool manageAssemblers = true;
        const bool manageRefineries = true;
        const bool manageGasGenerators = true;
        const bool manageGasTanks = true;
        const bool manageBatteryBlocks = true;
        const bool manageReactors = true;
        const bool manageShipConnectors = true;
        const bool manageShipWelders = true;

        readonly bool logToEcho = true;
        readonly bool logToSurface = false;
        readonly bool debugMode = false;

        // Valid update frequencies are Update1, Update10 and Update100. Lower is faster.
        // Instruction count is a rough maximum per cycle. Higher means faster operation.
        const UpdateFrequency manageUpdateFrequency = UpdateFrequency.Update100;
        const int manageTargetInstructionCount = 1000;

        const UpdateFrequency transferUpdateFrequency = UpdateFrequency.Update10;
        const int transferTargetInstructionCount = 1000;

        const UpdateFrequency scanUpdateFrequency = UpdateFrequency.Update100;
        const int scanTargetInstructionCount = 1000;

        const UpdateFrequency logUpdateFrequency = UpdateFrequency.Update10;

        const string ignoreTag = "!";
        const string configSectionKey = "os";
        const string parseFilterPrefix = "(";
        const string parseFilterSuffix = " os)";
        const string parseFilterAll = "*";
        const string parseFilterSeparators = "\n;";
        const string parseFilterArgSeparators = " ";
        const char parseFilterPriority = 'p';
        const char parseFilterQuota = 'q';
        const char parseFilterSubtract = '-';

        const string version = "1.0.0";
        #endregion

        public static Program Instance { get; private set; }

        const string myName = "(os)";

        public readonly char[] _parseFilterSeparators = parseFilterSeparators.ToCharArray();
        public readonly char[] _parseFilterArgSeparators = parseFilterArgSeparators.ToCharArray();

        public readonly MyIni ini = new MyIni();
        public readonly ItemTargetComparer itemTargetComparer = new ItemTargetComparer();
        public readonly FilterComparer filterComparer = new FilterComparer();

        public readonly List<IMyTerminalBlock> IMyGridTerminalSystem_GetBlocks_blocks = new List<IMyTerminalBlock>();
        public readonly List<MyItemType> IMyInventory_GetAcceptedItems_itemTypes = new List<MyItemType>();
        public readonly List<MyInventoryItem> IMyInventory_GetItems_items = new List<MyInventoryItem>();
        public readonly Dictionary<MyItemType, MyFixedPoint> IMyInventory_SumItems_items = new Dictionary<MyItemType, MyFixedPoint>();

        readonly UpdateType manageUpdateType = manageUpdateFrequency.ToUpdateType();
        readonly UpdateType transferUpdateType = transferUpdateFrequency.ToUpdateType();
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
        readonly Worker transferrer;
        readonly Worker scanner;

        public Program()
        {
            Instance = this;

            Runtime.UpdateFrequency = manageUpdateFrequency | transferUpdateFrequency | scanUpdateFrequency | logUpdateFrequency;

            if (!Me.CustomName.EndsWith(myName)) Me.CustomName += $" {myName}";

            if (logToSurface)
            {
                Me.GetSurface(0).Prepare();
                Me.GetSurface(1).Prepare(Alignment: TextAlignment.CENTER, FontSize: 4f, TextPadding: 34f);
            }

            state = new State();
            manager = new Worker(Runtime, Manage);
            transferrer = new Worker(Runtime, Transfer);
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
            if ((updateSource & transferUpdateType) != 0) transferrer.Cycle(transferTargetInstructionCount);
            if ((updateSource & logUpdateType) != 0) Log();
        }

        public void Log()
        {
            if (!logToEcho && !logToSurface) return;

            var header = $"OmniScript v{version} [{logAnim[++logAnimIndex % logAnim.Length]}]";
            var content = $"Manager {manager.Log}Scanner {scanner.Log}Transferrer {transferrer.Log}";
            var frame = $"{header}\n\n{content}";
            if (logToEcho) Echo(frame);
            if (logToSurface)
            {
                Me.GetSurface(0).WriteText(content);
                Me.GetSurface(1).WriteText(header);
            }


            // Dump item info in CustomData
            //var ss = new List<string>();
            //foreach (var item in itemTypes.Values)
            //{
            //    ss.Add($"{item.Group}  {item.DisplayName}\n");
            //}
            //ss.Sort();
            //var sb = new StringBuilder();
            //foreach (var s in ss)
            //{
            //    sb.Append(s);
            //}
            //Me.CustomData = sb.ToString();
        }
    }
}
