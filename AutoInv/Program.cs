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
/*
* Configuration
*/

// Whether the script should automatically pull from all inventories (those listed in ALWAYS_PULL are always pulled from)
        const bool ONLY_MANAGE_WITH_TAG = true;

// This is used to allow multiple parts (subgrids) of a ship/station count as the same.
        public HashSet<string> SAME_GRID = new HashSet<string>()
        {
    //"PortableBatteryShip"
        };

// R, G, B values 0-7
        static readonly char COLOR_BACKGROUND = Pixel(0, 0, 0);
        static readonly char COLOR_GRAPH = Pixel(5, 5, 5);
        static readonly char COLOR_POWER = Pixel(0, 0, 7);

/*
* Advanced configuration
*/

// Always pull from thease types, even if they have no tag
        public HashSet<string> ALWAYS_PULL = new HashSet<string>()
{
    "Sandbox.Game.Entities.Blocks.MyCryoChamber",
    "Sandbox.Game.Entities.Cube.MyShipConnector",
    "Sandbox.Game.Entities.MyCockpit",
    "Sandbox.Game.Weapons.MyShipDrill",
    "Sandbox.Game.Weapons.MyShipGrinder"
};

// Default priority of target inventories.
        const int DEFAULT_PRIORITY = -1000000;

/*
* ONLY EDIT BELOW THIS LINE IF YOU KNOW WHAT YOU ARE DOING
*/

        readonly bool OUTPUT_IN_TERMINAL = false;

        const int LCD_WIDTH = 178;
        const int LCD_HEIGHT = 178;
        const int LCD_WIDE_WIDTH = LCD_WIDTH * 2;

        const string FONT = "Debug";
        const string LCDFONT = "Monospace";
        const string ALL = "*";
        const char BEGINTAG = '(';
        const string ENDTAG = "ai)";
        const string TAG = "(ai)";
        const string IGNORE_TAG = "(!ai)";
        const char NEW_FILTER_CHAR = ';';

        const int INSTRUCTION_TARGET = 1000;
        const int INSTRUCTION_TARGET_IDLE = 1;

        static MyFixedPoint MIN_TRANSFER = (MyFixedPoint)0.001;

        const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update10;

        TimeSpan UPDATE_TICKABLE_TIME = TimeSpan.FromSeconds(10);

        public HashSet<string> WIDE_LCD_TYPES = new HashSet<string>()
{
    "LargeLCDPanelWide"
};

        public List<string> TYPES = new List<string>()
{
    "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm",
    "MyObjectBuilder_AmmoMagazine/LargeCalibreAmmo",
    "MyObjectBuilder_AmmoMagazine/MediumCalibreAmmo",
    "MyObjectBuilder_AmmoMagazine/AutocannonClip",
    "MyObjectBuilder_AmmoMagazine/NATO_25x184mm",
    "MyObjectBuilder_AmmoMagazine/LargeRailgunAmmo",
    "MyObjectBuilder_AmmoMagazine/Missile200mm",
    "MyObjectBuilder_AmmoMagazine/AutomaticRifleGun_Mag_20rd",
    "MyObjectBuilder_AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd",
    "MyObjectBuilder_AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd",
    "MyObjectBuilder_AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd",
    "MyObjectBuilder_AmmoMagazine/SemiAutoPistolMagazine",
    "MyObjectBuilder_AmmoMagazine/ElitePistolMagazine",
    "MyObjectBuilder_AmmoMagazine/FullAutoPistolMagazine",
    "MyObjectBuilder_AmmoMagazine/SmallRailgunAmmo",
    "MyObjectBuilder_Component/BulletproofGlass",
    "MyObjectBuilder_Component/Canvas",
    "MyObjectBuilder_Component/Computer",
    "MyObjectBuilder_Component/Construction",
    "MyObjectBuilder_Component/Detector",
    "MyObjectBuilder_Component/Display",
    "MyObjectBuilder_Component/Explosives",
    "MyObjectBuilder_Component/Girder",
    "MyObjectBuilder_Component/GravityGenerator",
    "MyObjectBuilder_Component/InteriorPlate",
    "MyObjectBuilder_Component/LargeTube",
    "MyObjectBuilder_Component/Medical",
    "MyObjectBuilder_Component/MetalGrid",
    "MyObjectBuilder_Component/Motor",
    "MyObjectBuilder_Component/PowerCell",
    "MyObjectBuilder_Component/RadioCommunication",
    "MyObjectBuilder_Component/Reactor",
    "MyObjectBuilder_Component/SmallTube",
    "MyObjectBuilder_Component/SolarCell",
    "MyObjectBuilder_Component/SteelPlate",
    "MyObjectBuilder_Component/Superconductor",
    "MyObjectBuilder_Component/Thrust",
    "MyObjectBuilder_Component/ZoneChip",
    "MyObjectBuilder_Ingot/Cobalt",
    "MyObjectBuilder_Ingot/Gold",
    "MyObjectBuilder_Ingot/Iron",
    "MyObjectBuilder_Ingot/Magnesium",
    "MyObjectBuilder_Ingot/Nickel",
    "MyObjectBuilder_Ingot/Platinum",
    "MyObjectBuilder_Ingot/Silicon",
    "MyObjectBuilder_Ingot/Silver",
    "MyObjectBuilder_Ingot/Uranium",
    "MyObjectBuilder_Ore/Cobalt",
    "MyObjectBuilder_Ore/Gold",
    "MyObjectBuilder_Ore/Ice",
    "MyObjectBuilder_Ore/Iron",
    "MyObjectBuilder_Ore/Magnesium",
    "MyObjectBuilder_Ore/Nickel",
    "MyObjectBuilder_Ore/Organic", // What is this?
    "MyObjectBuilder_Ore/Platinum",
    "MyObjectBuilder_Ore/Scrap",
    "MyObjectBuilder_Ore/Silicon",
    "MyObjectBuilder_Ore/Silver",
    "MyObjectBuilder_Ore/Stone",
    "MyObjectBuilder_Ore/Uranium",
    "MyObjectBuilder_ConsumableItem/ClangCola",
    "MyObjectBuilder_ConsumableItem/CosmicCoffee",
    "MyObjectBuilder_Datapad/Datapad",
    "MyObjectBuilder_ConsumableItem/Medkit",
    "MyObjectBuilder_Package/Package",
    "MyObjectBuilder_ConsumableItem/Powerkit",
    "MyObjectBuilder_PhysicalObject/SpaceCredit",
    "MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item",
    "MyObjectBuilder_PhysicalGunObject/HandDrill4Item",
    "MyObjectBuilder_PhysicalGunObject/Welder4Item",
    "MyObjectBuilder_PhysicalGunObject/AngleGrinder2Item",
    "MyObjectBuilder_PhysicalGunObject/HandDrill2Item",
    "MyObjectBuilder_PhysicalGunObject/Welder2Item",
    "MyObjectBuilder_PhysicalGunObject/AngleGrinderItem",
    "MyObjectBuilder_PhysicalGunObject/HandDrillItem",
    "MyObjectBuilder_GasContainerObject/HydrogenBottle",
    "MyObjectBuilder_PhysicalGunObject/AutomaticRifleItem",
    "MyObjectBuilder_PhysicalGunObject/UltimateAutomaticRifleItem",
    "MyObjectBuilder_PhysicalGunObject/RapidFireAutomaticRifleItem",
    "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem",
    "MyObjectBuilder_OxygenContainerObject/OxygenBottle",
    "MyObjectBuilder_PhysicalGunObject/AdvancedHandHeldLauncherItem",
    "MyObjectBuilder_PhysicalGunObject/AngleGrinder3Item",
    "MyObjectBuilder_PhysicalGunObject/HandDrill3Item",
    "MyObjectBuilder_PhysicalGunObject/Welder3Item",
    "MyObjectBuilder_PhysicalGunObject/BasicHandHeldLauncherItem",
    "MyObjectBuilder_PhysicalGunObject/SemiAutoPistolItem",
    "MyObjectBuilder_PhysicalGunObject/ElitePistolItem",
    "MyObjectBuilder_PhysicalGunObject/FullAutoPistolItem",
    "MyObjectBuilder_PhysicalGunObject/WelderItem",

    // Low priority
    "MyObjectBuilder_Ingot/Stone"
};

        public Dictionary<string, HashSet<string>> alias = new Dictionary<string, HashSet<string>>()
{
    { "gravel", new HashSet<string>(){ "MyObjectBuilder_Ingot/Stone" } }
};
        public void BuildAlias()
        {
            alias.Add(ALL.ToLower(), new HashSet<string>(TYPES));
            foreach (string type in TYPES)
            {
                string[] ss = type.Substring(type.IndexOf('_') + 1).ToLower().Split('/');
                if (!alias.ContainsKey(ss[1])) alias.Add(ss[1], new HashSet<string>() { type });
                if (!alias.ContainsKey(ss[0])) alias.Add(ss[0], new HashSet<string>() { type });
                else alias[ss[0]].Add(type);
            }
            alias.Add("ammo", alias["ammomagazine"]);
            alias.Add("components", alias["component"]);
            alias.Add("hydrogen", alias["gascontainerobject"]);
            alias.Add("gas", alias["gascontainerobject"]);
            alias.Add("ingots", alias["ingot"]);
            alias.Add("ores", alias["ore"]);
            alias.Add("oxygen", alias["oxygencontainerobject"]);
            alias.Add("tool", alias["physicalgunobject"]);
            alias.Add("tools", alias["physicalgunobject"]);
        }

        static bool Matches(string s, string type)
        {
            return type.Substring(type.IndexOf('_') + 1).StartsWith(s);
        }
        static HashSet<string> MatchType(string s)
        {
            string sl;
            return instance.alias.ContainsKey(sl = s.ToLower()) ? instance.alias[sl] : new HashSet<string>(instance.TYPES.FindAll(type => Matches(s, type)));
        }
        static bool MatchType(string s, HashSet<string> types, bool add = true)
        {
            int count = types.Count;
            if (add) types.UnionWith(MatchType(s));
            else types.ExceptWith(MatchType(s));
            return count != types.Count;
        }

        static int SearchForIndex<T>(List<T> list, T e) where T : IComparable
        {
            int i = 0; // TODO binary search
            while (i < list.Count)
            {
                if (e.CompareTo(list[i]) > 0) break;
                ++i;
            }
            return i;
        }

        class Filter
        {
            public HashSet<string> types;
            public int priority;
            public MyFixedPoint quota;

            public Filter(string s, int priority)
            {
                types = new HashSet<string>();
                quota = MyFixedPoint.MaxValue;
                this.priority = priority;

                int parsedInt;
                double parsedDouble;
                bool setPriority = false;
                bool setQuota = false;
                foreach (string arg in s.Split('\n', ' '))
                {
                    if (arg.Length == 0) continue;
                    char first = char.ToUpper(arg[0]);

                    if (first.Equals('P') && int.TryParse(arg.Substring(1), out parsedInt))
                    {
                        this.priority = parsedInt;
                        setPriority = true;
                    }
                    else if (first.Equals('Q') && double.TryParse(arg.Substring(1), out parsedDouble))
                    {
                        quota = (MyFixedPoint)parsedDouble;
                        setQuota = true;
                    }
                    else
                    {
                        bool add = !first.Equals('-');
                        if (!MatchType(arg.Substring(add ? 0 : 1), types, add))
                        {
                            Warn("Invalid/moot filter: " + s);
                        }
                    }
                }
                if (!setPriority && setQuota)
                {
                    ++this.priority;
                }
            }
        }

        class ManagedBlock
        {

            public IMyTerminalBlock block;
            public string customData;
            public string name;
            public long entityId;

            public ManagedBlock(IMyTerminalBlock block)
            {
                this.block = block;
                customData = block.CustomData;
                name = block.CustomName;
                entityId = block.EntityId;
            }

            public virtual bool Valid()
            {
                return instance.Exists(block) && block.CustomName.Equals(name) && block.CustomData.Equals(customData);
            }

        }

        class SourceBlock : ManagedBlock
        {

            public IMyInventory sourceInventory;

            public SourceBlock(IMyTerminalBlock block) : base(block)
            {
                sourceInventory = block.GetInventory(block.InventoryCount - 1);
            }

            public override bool Valid()
            {
                return sourceInventory != null && base.Valid();
            }

        }

        class TargetBlock : SourceBlock
        {

            public IMyInventory targetInventory;
            public List<Filter> filter;

            public TargetBlock(IMyTerminalBlock block, string nameFilter) : base(block)
            {
                targetInventory = block.GetInventory(0);
                filter = new List<Filter>();

                foreach (string s in (nameFilter + NEW_FILTER_CHAR + customData).Split(NEW_FILTER_CHAR)) filter.Add(new Filter(s, DEFAULT_PRIORITY));

                if (filter.Count == 0)
                {
                    Warn("No filter: " + name);
                }
            }

            public override bool Valid()
            {
                return targetInventory != null && base.Valid();
            }

            public MyFixedPoint Free()
            {
                return targetInventory.MaxVolume - targetInventory.CurrentVolume;
            }

            public MyFixedPoint MaxTransfer(MyItemType type)
            {
                return targetInventory.CanItemsBeAdded(MyFixedPoint.SmallestPossibleValue, type) ? (MyFixedPoint)((double)Free() / type.GetItemInfo().Volume) : MyFixedPoint.Zero;
            }

            public MyFixedPoint Balance(MyItemType type)
            {
                int i = filter.FindIndex(f => f.types.Contains(type.ToString()));
                if (i < 0) return MyFixedPoint.MaxValue;
                return filter[i].quota - targetInventory.GetItemAmount(type);
            }

            public MyFixedPoint Accept(SourceBlock source, MyInventoryItem item, MyFixedPoint amount, MyItemType type)
            {
                MyFixedPoint toTransfer = MyFixedPoint.Min(MyFixedPoint.Min(Balance(type), amount), MaxTransfer(type));
                if (toTransfer > MyFixedPoint.Zero && source.sourceInventory.TransferItemTo(targetInventory, item, toTransfer > MIN_TRANSFER ? toTransfer : MIN_TRANSFER))
                {
                    return toTransfer;
                }
                return MyFixedPoint.Zero;
            }

        }

        class PriorityTarget : IComparable
        {

            public int p;
            public TargetBlock tb;

            public PriorityTarget(int p, TargetBlock tb)
            {
                this.p = p;
                this.tb = tb;
            }

            public int CompareTo(object o)
            {
                if (o == null) return 1;
                PriorityTarget pt = o as PriorityTarget;
                if (pt == null) return 1;
                return p.CompareTo(pt.p);
            }

        }

        class ByPriority
        {

            public List<PriorityTarget> targets;

            public ByPriority()
            {
                targets = new List<PriorityTarget>();
            }

            public bool Remove(long id)
            {
                targets.RemoveAll(pt => pt.tb.entityId == id);
                return targets.Count > 0;
            }

            public void Set(TargetBlock tb, int p)
            {
                PriorityTarget pt = new PriorityTarget(p, tb);

                int i = SearchForIndex(targets, pt);
                if (i > targets.Count) targets.Add(pt);
                else if (i < 0) targets.Insert(0, pt);
                else targets.Insert(i, pt);
            }

        }

        class ByType
        {

            public Dictionary<string, ByPriority> types;

            public ByType()
            {
                types = new Dictionary<string, ByPriority>();
            }

            public void Remove(long id)
            {
                types = types.Where(e => e.Value.Remove(id)).ToDictionary(e => e.Key, e => e.Value);
            }

            public void Set(TargetBlock tb, int p, string t)
            {
                ByPriority bp;
                if (!types.TryGetValue(t, out bp))
                {
                    types.Add(t, bp = new ByPriority());
                }
                bp.Set(tb, p);
            }

            public bool Get(string t, out ByPriority bt)
            {
                return types.TryGetValue(t, out bt);
            }

        }

        class TickableBlock : ManagedBlock
        {

            public TimeSpan lastTick;
            public float dt;

            public TickableBlock(IMyTerminalBlock block, TimeSpan elapsedTime) : base(block)
            {
                lastTick = elapsedTime;
            }

            public virtual void Tick(TimeSpan elapsedTime)
            {
                dt = (float)(elapsedTime - lastTick).TotalSeconds;
                lastTick = elapsedTime;
            }

        }

        class BatteryBlock : TickableBlock
        {

            public IMyBatteryBlock batteryBlock;

            public float maxPower;
            public float currentPower;
            public float currentChange;
            public TimeSpan toFull;
            public TimeSpan toEmpty;

            public BatteryBlock(IMyBatteryBlock block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                batteryBlock = block;
                maxPower = block.MaxStoredPower;
                currentPower = block.CurrentStoredPower;
                toFull = TimeSpan.Zero;
                toEmpty = TimeSpan.Zero;
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
                if (dt <= 0.0f) return;
                float currentPower = batteryBlock.CurrentStoredPower;
                if (dt > 0.0f) currentChange = /*(*/batteryBlock.CurrentInput * 0.8f - batteryBlock.CurrentOutput/*currentPower - this.currentPower) / dt*/;
                this.currentPower = currentPower;
                toFull = currentChange <= 0.0f ? TimeSpan.Zero : TimeSpan.FromSeconds((maxPower - currentPower) / currentChange);
                toEmpty = currentChange >= 0.0f ? TimeSpan.Zero : TimeSpan.FromSeconds(currentPower / -currentChange);
            }

        }

        class LCDBlock : TickableBlock
        {

            public IMyTextPanel lcdBlock;
            public bool wide;

            public LCDBlock(IMyTextPanel block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                lcdBlock = block;
                wide = instance.WIDE_LCD_TYPES.Contains(block.BlockDefinition.SubtypeId);
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
            }

        }

        class LCDTextBlock : LCDBlock
        {

            string text;

            public LCDTextBlock(IMyTextPanel block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                text = lcdBlock.GetText();
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
            }

            public void Write(string text)
            {
                if (text.Equals(this.text)) return;
                lcdBlock.WriteText(this.text = text);
            }

        }

        class AILCDBlock : LCDTextBlock
        {

            public AILCDBlock(IMyTextPanel block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                if (!lcdBlock.GetPublicTitle().Equals("AI"))
                {
                    lcdBlock.WritePublicTitle("AI");
                    lcdBlock.FontColor = Color.White;
                    lcdBlock.Font = FONT;
                    lcdBlock.FontSize = 0.8f;
                }
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
                Write(instance.lcdAi);
            }

        }

        class LCDBitmapBlock : LCDTextBlock
        {

            public LCDBitmapBlock(IMyTextPanel block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                lcdBlock.Font = LCDFONT;
                lcdBlock.FontSize = 0.1f;
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
            }

            public void Draw(Bitmap bitmap)
            {
                Write(bitmap.Text());
            }

        }

        class PowerLCDBlock : LCDBitmapBlock
        {

            public PowerLCDBlock(IMyTextPanel block, TimeSpan elapsedTime) : base(block, elapsedTime)
            {
                if (!lcdBlock.GetPublicTitle().Equals("AI Power"))
                {
                    lcdBlock.WritePublicTitle("AI Power");
                }
            }

            public override void Tick(TimeSpan elapsedTime)
            {
                base.Tick(elapsedTime);
                Draw(wide ? instance.powerGraphWide.bitmap : instance.powerGraph.bitmap);
            }

        }

        static char Pixel(byte r, byte g, byte b)
        {
            return (char)(0xe100 + (r << 6) + (g << 3) + b);
        }

        class Bitmap
        {
            static readonly StringBuilder stringBuilder = new StringBuilder();

            public int w;
            public int h;
            public int s;
            public char[] pixels;
            string cache;

            public Bitmap(int w = LCD_WIDTH, int h = LCD_HEIGHT)
            {
                pixels = new char[(s = ((this.w = w) + 1)) * (this.h = h) - 1];
                Clear();
            }

            public int Index(int x, int y)
            {
                return y * s + x;
            }

            private void P(int x, int y, char c)
            {
                pixels[Index(x, y)] = c;
            }

            private void Line_(int x0, int y0, int x1, int y1, char c)
            {
                int dx = x1 - x0;
                int dy = y1 - y0;

                int px = 2 * dy - dx;
                int py = 2 * dx - dy;

                int x, y;
                int xe, ye;

                if (dy <= dx)
                {
                    x = x0;
                    y = y0;
                    xe = x1;

                    P(x, y, c);
                    while (x < xe)
                    {
                        ++x;
                        if (px < 0) px += 2 * dy;
                        else
                        {
                            if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) ++y;
                            else --y;
                            px += 2 * (dy - dx);
                        }
                        P(x, y, c);
                    }
                }
                else
                {
                    x = x0;
                    y = y0;
                    ye = y1;

                    P(x, y, c);
                    while (y < ye)
                    {
                        ++y;
                        if (py <= 0) py += 2 * dx;
                        else
                        {
                            if (dx > 0 && dy > 0) ++x;
                            else --x;
                            py += 2 * (dx - dy);
                        }
                        P(x, y, c);
                    }
                }
            }
            public void Line(int x0, int y0, int x1, int y1, char c)
            {
                Line_(x0 > x1 ? x1 : x0, y0 > y1 ? y1 : y0, x0 > x1 ? x0 : x1, y0 > y1 ? y0 : y1, c);
                cache = null;
            }

            public void Rect(int x, int y, int w, int h, char c, bool fill = true)
            {
                char[] cs = instance.ColorArray(c);
                if (fill)
                {
                    for (int i = 0; i < h; ++i)
                    {
                        Array.Copy(cs, 0, pixels, Index(x, y + i), w);
                    }
                }
                else
                {
                    for (int i = 0; i <= 1; ++i) Array.Copy(cs, 0, pixels, Index(x, y + h * i), w);
                    for (int i = 0; i <= 1; ++i) for (int j = 0; j <= h; ++j) P(x + w * i, y + j, cs[0]);
                }
                cache = null;
            }

            public void Dot(int x, int y, char c)
            {
                P(x, y, c);
                cache = null;
            }

            public string Text()
            {
                if (cache == null)
                {
                    cache = stringBuilder.Append(pixels).ToString();
                    stringBuilder.Clear();
                }
                return cache;
            }

            public void Clear()
            {
                Array.Copy(instance.emptyPixelArray, 0, pixels, 0, pixels.Length);
                for (int i = 0; i < h - 1; ++i) pixels[i * s + w] = '\n';
                cache = null;
            }
        }

        class Graph
        {

            public Bitmap bitmap;
            readonly int[] ys;
            int n;
            readonly int gx;
            readonly int gy;
            readonly int gw;
            readonly int gh;

            public Graph(int w = 178)
            {
                bitmap = new Bitmap(w);
                ys = new int[gw = (w - 1/* - 22*/)];
                gx = 0;//11;
                gy = 0;// 11;
                gh = bitmap.h - 1;// - 22;
                n = 0;
                //bitmap.rect(10, 10, gw + 2, gh + 2, COLOR_GRAPH, false);
            }

            public void Value(float f)
            {
                f = f > 1.0f ? 1.0f : (f < 0.0f ? 0.0f : f);
                int cur = n % gw;
                int prev = (cur - 1 + gw) % gw;
                int next = (cur + 1) % gw;

                if (next != 0) bitmap.Line(gx + next, ys[next], gx + cur, ys[cur], COLOR_BACKGROUND);
                ys[cur] = gy + gh - (int)Math.Round((float)gh * f);
                if (cur != 0) bitmap.Line(gx + prev, ys[prev], gx + cur, ys[cur], COLOR_POWER);

                bitmap.Line(gx + prev, gy + gh, gx + prev, gy + gh - 10, COLOR_BACKGROUND);
                bitmap.Line(gx + cur, gy + gh, gx + cur, gy + gh - 10, COLOR_GRAPH);
                ++n;
            }

        }

        class State
        {
            public int state;

            public List<IMyTerminalBlock> blocks;
            public List<MyInventoryItem> items;
            public SourceBlock sourceBlock;
            public TargetBlock targetBlock;
            public ByPriority bp;
            public MyFixedPoint? amount;
            public List<long> ll;
            public List<string> ss;

            public int i, j, k;

            public State()
            {
                Reset();
            }

            public void Clear()
            {
                blocks = null;
                items = null;
                sourceBlock = null;
                targetBlock = null;
                bp = null;
                amount = null;
                ll = null;
                ss = null;
                i = 0;
                j = 0;
                k = 0;
            }

            public void Reset()
            {
                Clear();
                state = 0;
            }

            public void Next()
            {
                Clear();
                ++state;
            }

            public string Progress()
            {
                return i + " " + j + " " + k;
            }
        }

        public LinkedList<string> logHistory = new LinkedList<string>();
        static void Log(string message)
        {
            instance.logHistory.AddFirst("" + instance.ticks + " " + message);
            if (instance.logHistory.Count > 10) instance.logHistory.RemoveLast();
        }

        public LinkedList<string> warnHistory = new LinkedList<string>();
        static void Warn(string message)
        {
            instance.warnHistory.AddFirst("" + instance.ticks + " " + message);
            if (instance.warnHistory.Count > 5) instance.warnHistory.RemoveLast();
        }

        public bool Go()
        {
            return instance.Runtime.CurrentInstructionCount < (Idle() ? INSTRUCTION_TARGET_IDLE : INSTRUCTION_TARGET);
        }

        public void ResetManagedBlocks()
        {
            updateBlocks = new Dictionary<long, IMyTerminalBlock>();
            targetBlocksToFilter = new List<TargetBlock>();
            toRemove = new List<long>();
            managedBlocks = new Dictionary<long, ManagedBlock>();
            sourceBlocks = new Dictionary<long, SourceBlock>();
            targetBlocks = new Dictionary<long, TargetBlock>();
            tickableBlocks = new Dictionary<long, TickableBlock>();
            batteryBlocks = new Dictionary<long, BatteryBlock>();
            lcdBlocks = new Dictionary<long, LCDBlock>();
            byType = new ByType();

            powerLCDBlocks = new HashSet<long>();
            powerLCDBlocksWide = new HashSet<long>();

            ResetBatteryStats();
        }

        public void ResetBatteryStats()
        {
            batteriesMaxPower = 0.0f;
            batteriesCurrentPower = 0.0f;
            batteriesCurrentChange = 0.0f;
        }

        public void ScanSourceOrTarget(IMyTerminalBlock block)
        {
            SourceBlock sb = null;
            TargetBlock tb = null;
            string name = block.CustomName;
            int i0 = name.LastIndexOf(BEGINTAG, name.Length - ENDTAG.Length);
            int i1 = name.LastIndexOf(ENDTAG[0], name.Length - ENDTAG.Length);
            string filterString = (i0 >= 0 && i1 >= 0 && i1 > i0) ? name.Substring(i0 + 1, i1 - i0 - 1) : "";

            if (filterString.Length > 0 || block.CustomData.Length > 0)
            {
                Log("+T: " + name);
                tb = new TargetBlock(block, filterString);
                targetBlocks.Add(block.EntityId, tb);
                targetBlocksToFilter.Add(tb);
            }
            if (tb == null || !tb.sourceInventory.Equals(tb.targetInventory))
            {
                Log("+S: " + name);
                sb = new SourceBlock(block);
                sourceBlocks.Add(block.EntityId, sb);
            }
            managedBlocks.Add(block.EntityId, tb ?? sb);
        }

        public void Scan(IMyTerminalBlock block)
        {
            bool sameGrid_ = SameGrid(block);
            if (!Idle() || sameGrid_)
            {
                if (IsSourceOrTarget(block)) ScanSourceOrTarget(block); // TODO ref & ass first
            }
            if (sameGrid_)
            {
                if (block is IMyBatteryBlock)
                {
                    BatteryBlock bb = new BatteryBlock((IMyBatteryBlock)block, elapsedTime);
                    batteryBlocks.Add(block.EntityId, bb);
                    managedBlocks.Add(block.EntityId, bb);
                    tickableBlocks.Add(block.EntityId, bb);
                    Log("+B: " + block.CustomName);
                }
                else if (block is IMyTextPanel)
                {
                    if (block.CustomName.EndsWith(TAG))
                    {
                        AILCDBlock alb = new AILCDBlock((IMyTextPanel)block, elapsedTime);
                        managedBlocks.Add(block.EntityId, alb);
                        tickableBlocks.Add(block.EntityId, alb);
                        lcdBlocks.Add(block.EntityId, alb);
                        Log("+L: " + block.CustomName);
                    }
                    else if (block.CustomName.EndsWith(BEGINTAG + "power " + ENDTAG, StringComparison.OrdinalIgnoreCase))
                    {
                        PowerLCDBlock plb = new PowerLCDBlock((IMyTextPanel)block, elapsedTime);
                        managedBlocks.Add(block.EntityId, plb);
                        tickableBlocks.Add(block.EntityId, plb);
                        lcdBlocks.Add(block.EntityId, plb);

                        if (plb.wide) powerLCDBlocksWide.Add(block.EntityId);
                        else powerLCDBlocks.Add(block.EntityId);

                        Log("+L: " + block.CustomName);
                    }
                }

            }
        }

        bool ShouldBeManaged(IMyTerminalBlock block)
        {
            return Exists(block) && !managedBlocks.ContainsKey(block.EntityId);
        }

        // TODO Add refinery, lcd, assembler
        bool IsSourceOrTarget(IMyTerminalBlock block)
        {
            return block.HasInventory && (!ONLY_MANAGE_WITH_TAG || ALWAYS_PULL.Contains(block.GetType().ToString()) || block.CustomName.EndsWith(ENDTAG, true, null)) && !block.CustomName.EndsWith(IGNORE_TAG, true, null);
        }

        bool AssertType(MyItemType type)
        {
            /*bool result = TYPES.Contains(type.ToString());
                    if (!result)
                    {
                        warn("Missing type: " + type.ToString());
                        TYPES.Add(type.ToString());
                        resetManagedBlocks();
                        state.reset();
                    }
                    return result;*/

            // TODO List.Contains is unnecessarily expensive

            return true;
        }

        bool Exists(IMyTerminalBlock block)
        {
            return block != null && GridTerminalSystem.GetBlockWithId(block.EntityId) != null;
        }

        bool Exists(ManagedBlock block)
        {
            return block != null && block.block != null && GridTerminalSystem.GetBlockWithId(block.block.EntityId) != null;
        }

        bool SameGrid(IMyTerminalBlock block)
        {
            return block != null && (SAME_GRID.Contains(block.CubeGrid.CustomName) || Me.CubeGrid.IsSameConstructAs(block.CubeGrid));
        }

        bool Idle()
        {
            return otherScriptBlock != null;
        }

        bool IdleNext()
        {
            bool result = Idle();

            if (result) state.Next();

            return result;
        }

        bool Tick()
        {
            if (state.state == 0) // Check invalid
            {
                ++handledStates;
                work += "CI ";

                if (state.ll == null) state.ll = new List<long>(managedBlocks.Keys);
                for (; state.i < state.ll.Count; ++state.i)
                {
                    ManagedBlock mb;
                    if (!managedBlocks.TryGetValue(state.ll[state.i], out mb))
                    {
                        if (!Go())
                        {
                            ++state.i;
                            return true;
                        }
                        continue;
                    }
                    if (!mb.Valid())
                    {
                        toRemove.Add(state.ll[state.i]);
                        if (Exists(mb.block) && !updateBlocks.ContainsKey(state.ll[state.i])) updateBlocks.Add(state.ll[state.i], mb.block);
                    }
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }
                state.Next();

            }
            if (state.state == 1) // Remove invalid
            {
                ++handledStates;
                work += "RI ";

                for (; state.i < toRemove.Count; ++state.i)
                {
                    string name = managedBlocks.ContainsKey(toRemove[state.i]) ? managedBlocks[toRemove[state.i]].name : "";

                    if (targetBlocks.Remove(toRemove[state.i]))
                    {
                        byType.Remove(toRemove[state.i]);
                        Log("-T: " + name);
                    }
                    if (sourceBlocks.Remove(toRemove[state.i]))
                    {
                        Log("-S: " + name);
                    }
                    if (batteryBlocks.Remove(toRemove[state.i]))
                    {
                        Log("-B: " + name);
                    }
                    if (lcdBlocks.Remove(toRemove[state.i]))
                    {
                        powerLCDBlocks.Remove(toRemove[state.i]);
                        powerLCDBlocksWide.Remove(toRemove[state.i]);
                        Log("-L: " + name);
                    }
                    managedBlocks.Remove(toRemove[state.i]);
                    tickableBlocks.Remove(toRemove[state.i]);
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }
                toRemove.Clear();

                state.Next();
            }
            if (state.state == 2) // Scan updated blocks
            {
                ++handledStates;
                work += "SU ";

                if (state.ll == null) state.ll = new List<long>(updateBlocks.Keys);
                for (; state.i < state.ll.Count; ++state.i)
                {
                    IMyTerminalBlock block;
                    if (!updateBlocks.TryGetValue(state.ll[state.i], out block) || !Exists(block))
                    {
                        if (!Go())
                        {
                            ++state.i;
                            return true;
                        }
                        continue;
                    }
                    if (!ShouldBeManaged(block)) continue;
                    Scan(block);
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }
                updateBlocks.Clear();

                state.Next();

            }
            if (state.state == 3) // Scan all blocks
            {
                ++handledStates;
                work += "SA ";

                if (state.blocks == null)
                {
                    state.blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(state.blocks);
                }
                for (; state.i < state.blocks.Count; ++state.i)
                {
                    if (state.blocks[state.i] is IMyProgrammableBlock && state.blocks[state.i].IsWorking && state.blocks[state.i].EntityId < Me.EntityId && state.blocks[state.i].CustomName.EndsWith(TAG))
                    {
                        otherScriptBlock = state.blocks[state.i].EntityId;
                        ++state.i;
                        return true;
                    }
                    if (ShouldBeManaged(state.blocks[state.i])) Scan(state.blocks[state.i]);
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }
                state.Next();

            }
            if (state.state == 4) // Add target blocks to filter
            {
                ++handledStates;
                work += "TF ";

                for (; state.i < targetBlocksToFilter.Count; ++state.i)
                {
                    for (; state.j < targetBlocksToFilter[state.i].filter.Count; ++state.j)
                    {
                        if (state.ss == null) state.ss = targetBlocksToFilter[state.i].filter[state.j].types.ToList();
                        for (; state.k < state.ss.Count; ++state.k)
                        {
                            byType.Set(targetBlocksToFilter[state.i], targetBlocksToFilter[state.i].filter[state.j].priority, state.ss[state.k]);
                            if (!Go())
                            {
                                ++state.k;
                                return true;
                            }
                        }

                        state.k = 0;
                        state.ss = null;
                        if (!Go())
                        {
                            ++state.j;
                            return true;
                        }
                    }

                    state.j = 0;
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }
                targetBlocksToFilter.Clear();

                state.Next();
            }
            if (state.state == 5) // Pull from sources
            {
                ++handledStates;
                if (IdleNext()) return false;
                work += "PS ";

                if (state.ll == null) state.ll = new List<long>(sourceBlocks.Keys);
                for (; state.i < state.ll.Count; ++state.i)
                {
                    if ((state.sourceBlock == null && !sourceBlocks.TryGetValue(state.ll[state.i], out state.sourceBlock)) || !Exists(state.sourceBlock))
                    {
                        if (!Go())
                        {
                            ++state.i;
                            return true;
                        }
                        continue;
                    }
                    if (state.items == null)
                    {
                        state.items = new List<MyInventoryItem>();
                        state.sourceBlock.sourceInventory.GetItems(state.items);
                    }

                    for (; state.j < state.items.Count; ++state.j)
                    {
                        if (!AssertType(state.items[state.j].Type)) return true;
                        if (state.bp != null || byType.Get(state.items[state.j].Type.ToString(), out state.bp))
                        {
                            if (state.amount == null) state.amount = state.items[state.j].Amount;

                            for (; state.k < state.bp.targets.Count; ++state.k)
                            {
                                if (!Exists(state.bp.targets[state.k].tb) || !state.sourceBlock.sourceInventory.CanTransferItemTo(state.bp.targets[state.k].tb.targetInventory, state.items[state.j].Type))
                                {
                                    if (!Go())
                                    {
                                        ++state.k;
                                        return true;
                                    }
                                    continue;
                                }

                                MyFixedPoint accepted = state.bp.targets[state.k].tb.Accept(state.sourceBlock, state.items[state.j], state.amount.Value, state.items[state.j].Type);

                                if (accepted == MyFixedPoint.Zero)
                                {
                                    if (!Go())
                                    {
                                        ++state.k;
                                        return true;
                                    }
                                    continue;
                                }
                                state.amount -= accepted;
                                if (state.amount <= MyFixedPoint.Zero) break;
                                if (!Go())
                                {
                                    ++state.k;
                                    return true;
                                }
                            }

                        }

                        state.bp = null;
                        state.amount = null;
                        state.k = 0;
                        if (!Go())
                        {
                            ++state.j;
                            return true;
                        }
                    }

                    state.j = 0;
                    state.sourceBlock = null;
                    state.items = null;
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }

                state.Next();

            }
            if (state.state == 6) // Pull from targets
            {
                ++handledStates;
                if (IdleNext()) return false;
                work += "PT ";

                if (state.ll == null) state.ll = new List<long>(targetBlocks.Keys);
                for (; state.i < state.ll.Count; ++state.i)
                {
                    if ((state.targetBlock == null && !targetBlocks.TryGetValue(state.ll[state.i], out state.targetBlock)) || !Exists(state.targetBlock))
                    {
                        if (!Go())
                        {
                            ++state.i;
                            return true;
                        }
                        continue;
                    }
                    if (state.items == null)
                    {
                        state.items = new List<MyInventoryItem>();
                        state.targetBlock.sourceInventory.GetItems(state.items);
                    }

                    for (; state.j < state.items.Count; ++state.j)
                    {
                        if (!AssertType(state.items[state.j].Type)) return true;
                        if (state.bp != null || byType.Get(state.items[state.j].Type.ToString(), out state.bp))
                        {
                            if (state.amount == null) state.amount = state.items[state.j].Amount;

                            for (; state.k < state.bp.targets.Count; ++state.k)
                            {
                                if (!Exists(state.bp.targets[state.k].tb) || state.bp.targets[state.k].tb.targetInventory.Equals(state.targetBlock.sourceInventory))
                                {
                                    state.amount = MyFixedPoint.Min(state.amount.Value, -state.targetBlock.Balance(state.items[state.j].Type));
                                    if (!Go())
                                    {
                                        ++state.k;
                                        return true;
                                    }
                                    continue;
                                }
                                if (!state.targetBlock.sourceInventory.CanTransferItemTo(state.bp.targets[state.k].tb.targetInventory, state.items[state.j].Type))
                                {
                                    if (!Go())
                                    {
                                        ++state.k;
                                        return true;
                                    }
                                    continue;
                                }

                                MyFixedPoint accepted = state.bp.targets[state.k].tb.Accept(state.targetBlock, state.items[state.j], state.amount.Value, state.items[state.j].Type);

                                if (accepted == MyFixedPoint.Zero)
                                {
                                    if (!Go())
                                    {
                                        ++state.k;
                                        return true;
                                    }
                                    continue;
                                }
                                state.amount -= accepted;
                                if (state.amount <= MyFixedPoint.Zero) break;
                                if (!Go())
                                {
                                    ++state.k;
                                    return true;
                                }
                            }
                        }

                        state.bp = null;
                        state.amount = null;
                        state.k = 0;
                        if (!Go())
                        {
                            ++state.j;
                            return true;
                        }
                    }

                    state.j = 0;
                    state.targetBlock = null;
                    state.sourceBlock = null;
                    state.items = null;
                    if (!Go())
                    {
                        ++state.i;
                        return true;
                    }
                }

                state.Next();

            }
            if (state.state == 7) // Total inventory
            {
                ++handledStates;

                work += "TI ";

                // TODO

                state.Next();
            }
            if (state.state == 8) // Tick blocks
            {
                ++handledStates;

                work += "TB ";
                if (updateTickableTimer >= UPDATE_TICKABLE_TIME)
                {
                    float dt = (float)updateTickableTimer.TotalSeconds;

                    if (state.i == 0)
                    {
                        if (state.ll == null) state.ll = new List<long>(batteryBlocks.Keys);
                        ResetBatteryStats();
                        for (; state.j < state.ll.Count; ++state.j)
                        {
                            BatteryBlock batteryBlock;
                            if (!batteryBlocks.TryGetValue(state.ll[state.j], out batteryBlock) || !Exists(batteryBlock))
                            {
                                if (!Go())
                                {
                                    ++state.j;
                                    return true;
                                }
                                continue;
                            }
                            batteryBlock.Tick(elapsedTime);
                            batteriesMaxPower += batteryBlock.maxPower;
                            batteriesCurrentPower += batteryBlock.currentPower;
                            batteriesCurrentChange += batteryBlock.currentChange;
                            if (!Go())
                            {
                                ++state.j;
                                return true;
                            }
                        }
                        float power = batteriesMaxPower > 0.0f ? batteriesCurrentPower / batteriesMaxPower : 0.0f;
                        if (powerLCDBlocksWide.Count > 0) powerGraphWide.Value(power);
                        if (powerLCDBlocks.Count > 0) powerGraph.Value(power);
                        ++state.i;
                        state.j = 0;
                        state.ll = null;
                    }

                    if (state.i == 1)
                    {
                        /*if (state.ll == null) state.ll = new List<long>(lcdBlocks.Keys);
                                for (; state.j < state.ll.Count; ++state.j)
                                {
                                    LCDBlock lcdBlock;
                                    if (!lcdBlocks.TryGetValue(state.ll[state.j], out lcdBlock) || !exists(lcdBlock))
                                    {
                                        if (!go())
                                        {
                                            ++state.j;
                                            return true;
                                        }
                                        continue;
                                    }
                                    lcdBlock.tick(elapsedTime);
                                    if (!go())
                                    {
                                        ++state.j;
                                        return true;
                                    }
                                }
                                ++state.i;
                                state.j = 0;*/
                        //state.ll = null;
                        ++state.i;
                    }

                    if (state.i == 2)
                    {
                        if (state.ll == null) state.ll = new List<long>(lcdBlocks.Keys);
                        for (; state.j < state.ll.Count; ++state.j)
                        {
                            LCDBlock lcdBlock;
                            if (!lcdBlocks.TryGetValue(state.ll[state.j], out lcdBlock) || !Exists(lcdBlock))
                            {
                                if (!Go())
                                {
                                    ++state.j;
                                    return true;
                                }
                                continue;
                            }
                            lcdBlock.Tick(elapsedTime);
                            if (!Go())
                            {
                                ++state.j;
                                return true;
                            }
                        }
                        ++state.i;
                        state.j = 0;
                        state.ll = null;
                    }

                    updateTickableTimer = TimeSpan.Zero;
                }

                state.Next();
            }

            if (state.state > numStates) state.state = 0;
            return false;
        }
        const int numStates = 8;
        int handledStates;

        static Program instance;

        List<long> toRemove;
        List<TargetBlock> targetBlocksToFilter;
        Dictionary<long, IMyTerminalBlock> updateBlocks;
        Dictionary<long, ManagedBlock> managedBlocks;
        Dictionary<long, SourceBlock> sourceBlocks;
        Dictionary<long, TargetBlock> targetBlocks;
        Dictionary<long, TickableBlock> tickableBlocks;
        Dictionary<long, BatteryBlock> batteryBlocks;
        Dictionary<long, LCDBlock> lcdBlocks;

        HashSet<long> powerLCDBlocks;
        HashSet<long> powerLCDBlocksWide;
        ByType byType;

        float batteriesMaxPower;
        float batteriesCurrentPower;
        float batteriesCurrentChange;
        readonly char[] emptyPixelArray;
        readonly Graph powerGraph;
        readonly Graph powerGraphWide;
        readonly Dictionary<char, char[]> colorArrays;
        char[] ColorArray(char c)
        {
            char[] cs;
            if (!colorArrays.TryGetValue(c, out cs)) colorArrays.Add(c, cs = Enumerable.Repeat(c, LCD_WIDE_WIDTH).ToArray());
            return cs;
        }

        TimeSpan elapsedTime;
        TimeSpan timeSinceLastRun;
        TimeSpan updateTickableTimer;

        long? otherScriptBlock;
        readonly State state;

        long ticks;
        int maxInstructions;
        string work;
        readonly StringBuilder lcdAiBuilder;
        string lcdAi;

        public Program()
        {
            instance = this;
            Runtime.UpdateFrequency = UPDATE_FREQUENCY;
            ticks = 0;
            maxInstructions = 0;

            elapsedTime = TimeSpan.Zero;

            updateTickableTimer = UPDATE_TICKABLE_TIME;

            otherScriptBlock = null;

            state = new State();
            lcdAiBuilder = new StringBuilder();
            lcdAi = "";

            if (!Me.CustomName.EndsWith(TAG)) Me.CustomName += " " + TAG;

            ResetManagedBlocks();

            BuildAlias();

            emptyPixelArray = Enumerable.Repeat(COLOR_BACKGROUND, (LCD_WIDE_WIDTH + 1) * LCD_HEIGHT).ToArray();
            colorArrays = new Dictionary<char, char[]>();
            instance.ColorArray(COLOR_BACKGROUND);
            instance.ColorArray(COLOR_POWER);

            powerGraph = new Graph();
            powerGraphWide = new Graph(LCD_WIDE_WIDTH);
        }

        public void Print(string message)
        {
            if (OUTPUT_IN_TERMINAL) Echo(message);
            lcdAiBuilder.Append(message + '\n');
        }

        public void Main(string argument, UpdateType updateSource)
        {
            lcdAiBuilder.Clear();
            if (otherScriptBlock != null)
            {
                IMyTerminalBlock block = GridTerminalSystem.GetBlockWithId(otherScriptBlock.Value);
                if (block == null || !block.IsWorking)
                {
                    otherScriptBlock = null;
                }
            }

            Print("Ticks:" + (++ticks) + (Idle() ? " (idle)" : ""));
            Print("M:" + managedBlocks.Count + " S:" + sourceBlocks.Count + " T:" + targetBlocks.Count + " F:" + byType.types.Count + " U:" + updateBlocks.Count + " TR:" + toRemove.Count);
            Print("TB:" + tickableBlocks.Count + " B:" + batteryBlocks.Count);

            timeSinceLastRun = Runtime.TimeSinceLastRun;
            elapsedTime += timeSinceLastRun;
            updateTickableTimer += timeSinceLastRun;

            work = "State: ";

            handledStates = 0;
            while (!Tick() && Go() && handledStates < numStates) ;

            Print(work + state.Progress());
            int instructions = Runtime.CurrentInstructionCount;
            if (instructions > maxInstructions) maxInstructions = instructions;
            Print("Instructions: " + instructions + " Max: " + maxInstructions);

            if (warnHistory.Count > 0) Print("Warning:");
            foreach (string line in warnHistory) Print(line);
            if (logHistory.Count > 0) Print("Log:");
            foreach (string line in logHistory) Print(line);
            lcdAi = lcdAiBuilder.ToString();
        }
    }
}
