using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IngameScript
{
    partial class Program
    {
        IEnumerator<bool> Manage(StringBuilder log)
        {
            if (!state.Initialized) yield break;

            if (state.masterAssemblers.Count > 0 && state.slaveAssemblers.Count > 0)
            {
                foreach (var id in state.masterAssemblers.ToList())
                {
                    yield return true;
                    var assembler = state.blocks.Assembler(id);
                    assembler.CooperativeMode = false;
                }
                foreach (var id in state.slaveAssemblers.ToList())
                {
                    yield return true;
                    var assembler = state.blocks.Assembler(id);
                    assembler.CooperativeMode = true;
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

            if (state.batteryBlocks.Count > 0 && manageReactorsPower && state.reactors.Count > 0)
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

            if (state.gasGenerators.Count > 0 && manageGasGeneratorsGas && (state.oxygenTanks.Count + state.hydrogenTanks.Count > 0))
            {
                var OxygenCapacity = 0f;
                var OxygenStored = 0d;
                var HydrogenCapacity = 0f;
                var HydrogenStored = 0d;

                foreach (var id in state.oxygenTanks.ToList())
                {
                    yield return true;
                    var gasTank = state.blocks.GasTank(id);
                    OxygenCapacity += gasTank.Capacity;
                    OxygenStored += gasTank.Stored;
                }
                foreach (var id in state.hydrogenTanks.ToList())
                {
                    yield return true;
                    var gasTank = state.blocks.GasTank(id);
                    HydrogenCapacity += gasTank.Capacity;
                    HydrogenStored += gasTank.Stored;
                }
                var StoredGas = Math.Min(OxygenStored / OxygenCapacity, HydrogenStored / HydrogenCapacity);

                var EnableGasGeneratorsAtStoredGas = StoredGas <= enableGasGeneratorsAtStoredGas;
                if (EnableGasGeneratorsAtStoredGas || StoredGas >= disableGasGeneratorsAtStoredGas)
                {
                    foreach (var id in state.gasGenerators.ToList())
                    {
                        yield return true;
                        state.blocks.GasGenerator(id).Enabled = EnableGasGeneratorsAtStoredGas;
                    }
                }

                if (debugMode)
                {
                    log.Append($"StoredGas: {StoredGas.ToPercent()} ({OxygenStored}/{OxygenCapacity} L O) ({HydrogenStored}/{HydrogenCapacity} L H):\n");
                    log.Append($"Oxygen ({state.oxygenTanks.Count}):\n");
                    foreach (var id in state.oxygenTanks.ToList())
                    {
                        yield return true;
                        var gasTank = state.blocks.GasTank(id);
                        log.Append($" - {gasTank.Name}{(gasTank.Enabled ? "" : "*")} {gasTank.FilledRatio.ToPercent()}\n");
                    }
                    log.Append($"Hydrogen ({state.hydrogenTanks.Count}):\n");
                    foreach (var id in state.hydrogenTanks.ToList())
                    {
                        yield return true;
                        var gasTank = state.blocks.GasTank(id);
                        log.Append($" - {gasTank.Name}{(gasTank.Enabled ? "" : "*")} {gasTank.FilledRatio.ToPercent()}\n");
                    }
                    log.Append($"Generators ({state.gasGenerators.Count}):\n");
                    foreach (var id in state.gasGenerators.ToList())
                    {
                        yield return true;
                        var gasGenerator = state.blocks.GasGenerator(id);
                        log.Append($" - {gasGenerator.Name}{(gasGenerator.Enabled ? "" : "*")}\n");
                    }
                }
            }
        }
    }
}
