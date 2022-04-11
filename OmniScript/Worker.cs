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
        public class Worker
        {
            readonly IMyGridProgramRuntimeInfo runtime;
            readonly Func<StringBuilder, IEnumerator<bool>> taskFn;
            IEnumerator<bool> task = null;
            bool? current = null;
            readonly StringBuilder logBuilder = new StringBuilder();
            string log = "pending...\n";
            int cycleCountInProgress, cycleCount = -1;
            int instructionCountInProgress, instructionCount = -1;
            int totalRuns = 0;
            int maxInstructionCount = -1;

            public string Log => log;
            public int CycleCount => cycleCount;
            public int InstructionCount => instructionCount;
            public int TotalRuns => totalRuns;
            public int MaxInstructionCount => maxInstructionCount;

            public Worker(IMyGridProgramRuntimeInfo runtime, Func<StringBuilder, IEnumerator<bool>> taskFn)
            {
                this.runtime = runtime;
                this.taskFn = taskFn;
            }

            public void Cycle(int targetInstructionCount)
            {
                if (task == null)
                {
                    task = taskFn(logBuilder);
                    current = null;
                    logBuilder.Clear();
                    cycleCountInProgress = 0;
                    instructionCountInProgress = 0;
                }

                var startInstructionCount = runtime.CurrentInstructionCount;
                bool hasNext;

                while (hasNext = task.MoveNext())
                {
                    current = task.Current;
                    if (runtime.CurrentInstructionCount >= targetInstructionCount) break;
                }
                ++cycleCountInProgress;
                var cycleInstructionCount = runtime.CurrentInstructionCount - startInstructionCount;
                if (cycleInstructionCount > maxInstructionCount) maxInstructionCount = cycleInstructionCount;
                instructionCountInProgress += cycleInstructionCount;

                if (!hasNext)
                {
                    task.Dispose();
                    task = null;
                    if (current == true)
                    {
                        cycleCount = cycleCountInProgress;
                        instructionCount = instructionCountInProgress;
                        log = $"({maxInstructionCount}/{instructionCount}/{cycleCount}) [{++totalRuns}]{(logBuilder.Length > 0 ? ":" : ".")}\n{logBuilder}";
                    }
                }
            }
        }
    }
}
