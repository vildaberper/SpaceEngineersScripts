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
        IEnumerator<bool> Manage(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            if (state.masterAssemblers.Count > 0 && state.slaveAssemblers.Count > 0)
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
                if (debugMode)
                {
                    log.Append($"Master Assemblers ({state.masterAssemblers.Count}):\n");
                    foreach (var id in state.masterAssemblers.ToList())
                    {
                        yield return true;
                        var assembler = state.blocks.Assembler(id);
                        log.Append($" - {assembler.Name}{(assembler.Enabled ? "" : "*")}\n");
                    }
                    log.Append($"Slave Assemblers ({state.slaveAssemblers.Count}):\n");
                    foreach (var id in state.slaveAssemblers.ToList())
                    {
                        yield return true;
                        var assembler = state.blocks.Assembler(id);
                        log.Append($" - {assembler.Name}{(assembler.Enabled ? "" : "*")}\n");
                    }
                }
            }

            if(state.batteryBlocks.Count > 0 && manageReactorsPower && state.reactors.Count > 0)
            {
                var CurrentStoredPower = 0f;
                var MaxStoredPower = 0f;
                var CurrentOutput = 0f;
                var MaxOutput = 0f;
                foreach (var id in state.batteryBlocks.ToList())
                {
                    yield return true;
                    var battery = state.blocks.BatteryBlock(id);
                    CurrentStoredPower += battery.CurrentStoredPower;
                    MaxStoredPower += battery.MaxStoredPower;
                    CurrentOutput += battery.CurrentOutput;
                    MaxOutput += battery.MaxOutput;
                }
                var StoredPower = CurrentStoredPower / MaxStoredPower;
                var Output = CurrentOutput / MaxOutput;

                var EnableReactorsAtStoredPower = Output >= enableReactorsAtOutput || StoredPower <= enableReactorsAtStoredPower;
                if (EnableReactorsAtStoredPower || StoredPower >= disableReactorsAtStoredPower)
                {
                    foreach (var id in state.reactors.ToList())
                    {
                        yield return true;
                        state.blocks.Reactor(id).Enabled = EnableReactorsAtStoredPower;
                    }
                }

                if (debugMode)
                {
                    log.Append($"StoredPower: {StoredPower.ToPercent()} ({CurrentStoredPower}/{MaxStoredPower} MWh):\n");
                    log.Append($"Batteries ({state.batteryBlocks.Count}):\n");
                    foreach (var id in state.batteryBlocks.ToList())
                    {
                        yield return true;
                        var battery = state.blocks.BatteryBlock(id);
                        log.Append($" - {battery.Name}{(battery.Enabled ? "" : "*")} {(battery.CurrentStoredPower / battery.MaxStoredPower).ToPercent()}\n");
                    }
                    log.Append($"Reactors ({state.reactors.Count}):\n");
                    foreach (var id in state.reactors.ToList())
                    {
                        yield return true;
                        var reactor = state.blocks.Reactor(id);
                        log.Append($" - {reactor.Name}{(reactor.Enabled ? "" : "*")}\n");
                    }
                }
            }
        }
    }
}
