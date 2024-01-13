using System.Runtime.InteropServices;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal sealed class ProcessDisassembler : IDisposable
{
    private readonly DataTarget _dataTarget;
    const ulong LongestInstructionLength = 15;
    
    private static readonly NasmFormatter _formatter = new NasmFormatter
    {
        Options =
        {
            HexPrefix = "0x",
            HexSuffix = null,
            UppercaseHex = false
        }
    };

    public ProcessDisassembler(int processId)
    {
        _dataTarget = DataTarget.AttachToProcess(processId, false);
    }

    public IEnumerable<(ulong, string)> GetInstructions(ulong firstInstruction, ulong lastInstruction)
    {
        var codeLength = lastInstruction - firstInstruction + 1 + LongestInstructionLength;
        var machineCode = new byte[codeLength];
        for (ulong i = 0; i < codeLength; i++)
        {
            machineCode[i] = _dataTarget.DataReader.Read<byte>(firstInstruction + i);
        }

        return Create(machineCode, firstInstruction);
    }

    private static IEnumerable<(ulong, string)> Create(byte[] machineCode, ulong startAddress)
    {
        var formatter = _formatter;
        var output = new StringOutput();
        var codeReader = new ByteArrayCodeReader(machineCode);
        var decoder = Decoder.Create(GetBitness(), codeReader);
        decoder.IP = startAddress;
        var endRip = decoder.IP + (uint) machineCode.Length;

        while (decoder.IP < endRip)
        {
            output.Reset();
            var instruction = decoder.Decode();
            formatter.Format(instruction, output);
            yield return (instruction.IP, output.ToString());
        }
    }

    private static int GetBitness()
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.X64 ? 64 : 32;
    }

    public void Dispose()
    {
        _dataTarget.Dispose();
    }
}