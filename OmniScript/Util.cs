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
        public static class Util
        {
            public static void Swap<T>(ref T a, ref T b)
            {
                var c = a;
                a = b;
                b = c;
            }

            public static bool StringSection(string value, string prefix, string suffix, out string result)
            {
                var end = value.LastIndexOf(suffix);
                if (end < 0) goto Fail;

                var begin = value.LastIndexOf(prefix, end - prefix.Length);
                if (begin < 0) goto Fail;

                result = value.SubStr(begin + prefix.Length, end);
                return true;

            Fail:
                result = null;
                return false;
            }
        }
    }
}
