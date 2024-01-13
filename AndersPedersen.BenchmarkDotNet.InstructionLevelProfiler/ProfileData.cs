using System.Collections.Concurrent;
using System.Text;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class ProfileData
{
    private readonly string _rootMethodName;
    private long _samples;
    private MethodSamples? _rootMethodSamples;
    private int _pid;

    public ProfileData(string rootMethodName)
    {
        _rootMethodName = rootMethodName;
        _samples = 0;
    }

    public void SetPid(int pid)
    {
        _pid = pid;
    }

    public void AddStack(Span<StackFrameData> callStack)
    {
        callStack = SkipStackFramesUntilRootMethod(callStack);

        if (callStack.Length > 0)
        {
            _samples++;
            if (_rootMethodSamples is null)
            {
                Interlocked.CompareExchange(ref _rootMethodSamples, new MethodSamples(callStack[0].MethodLookup), null);
            }
            _rootMethodSamples.AddSample(callStack);
        }
    }

    private Span<StackFrameData> SkipStackFramesUntilRootMethod(Span<StackFrameData> callStack)
    {
        var i = 0;
        while (i < callStack.Length && !callStack[i].MethodName.Contains(_rootMethodName))
        {
            i++;
        }

        return callStack.Slice(i);
    }

    public void BuildString(StringBuilder sb, int maxRecursionDepth)
    {
        if (_rootMethodSamples is null)
        {
            sb.Append("No CPU samples hit method, ").AppendLine(_rootMethodName);
            return;
        }

        using var disasm = new ProcessDisassembler(_pid);
        _rootMethodSamples.BuildString(sb, disasm, _samples, maxRecursionDepth);
    }

    private class MethodSamples
    {
        private readonly LazyMethodLookup? _method;
        private readonly SortedDictionary<ulong, long> _addressCount;
        private readonly ConcurrentDictionary<ulong, MethodSamples> _calls;
        private long _samples;

        public MethodSamples(LazyMethodLookup? method)
        {
            _method = method;
            _addressCount = new SortedDictionary<ulong, long>();
            _calls = new ConcurrentDictionary<ulong, MethodSamples>();
            _samples = 0;
        }

        public void AddSample(Span<StackFrameData> stack)
        {
            _samples++;
            var first = stack[0];
            
            if (stack.Length > 1 && IsUserspace(stack[1]))
            {
                var method = stack[1].MethodLookup;
                var nextMethod = _calls.GetOrAdd(first.Address, (x) => new MethodSamples(method));
                nextMethod.AddSample(stack.Slice(1));
            }
            else
            {
                _addressCount.TryGetValue(first.Address, out var count);
                count += 1;
                _addressCount[first.Address] = count;
            }


        }

        private static bool IsUserspace(StackFrameData stackFrameData)
        {
            return stackFrameData.Address < 0xFFFF000000000000;
        }

        public void BuildString(StringBuilder sb, ProcessDisassembler disasm, long prevsamples, int maxRecursionDepth, int spaces = 0)
        {
            if (!_addressCount.Any())
                return;
            
            if (_method?.GetName() is not "")
            {
                AddSpace(spaces, sb);
                sb.Append(_method?.GetName())
                    .Append(" : ")
                    .Append(_samples)
                    .Append(" samples (")
                    .Append((100d * _samples / prevsamples).ToString("0.##"))
                    .Append("%)");
                sb.AppendLine();
            }

            if (spaces > maxRecursionDepth)
            {
                AddSpace(spaces, sb);
                sb.Append("Max recursion depth of ").Append(maxRecursionDepth).AppendLine(" reached.");
                return;
            }

            foreach (var (firstInstruction, lastInstruction) in GetAddressPairs(_addressCount, 100))
            {
                BuildStringForAddressRange(sb, disasm, spaces, firstInstruction, lastInstruction, maxRecursionDepth);
            }
        }

        private static IEnumerable<(ulong firstInstruction, ulong lastInstruction)> GetAddressPairs(SortedDictionary<ulong, long> addresses, ulong maxDistance)
        {
            ulong firstInstruction = addresses.First().Key;
            ulong lastInstruction = firstInstruction;
                
            foreach (var address in addresses)
            {

                if (address.Key - firstInstruction > maxDistance)
                {
                    yield return (firstInstruction, lastInstruction);
                    firstInstruction = address.Key;
                }
                
                lastInstruction = address.Key;
            }
            
            yield return (firstInstruction, lastInstruction);
        }

        private void BuildStringForAddressRange(StringBuilder sb, ProcessDisassembler disasm, int spaces,
            ulong firstInstruction, ulong lastInstruction, int maxRecursionDepth)
        {
            var instructions = disasm.GetInstructions(firstInstruction, lastInstruction);

            foreach (var (address, instruction) in instructions)
            {
                if (address > lastInstruction)
                    return;
                
                if (_calls.TryGetValue(address, out var nextMethod))
                {
                    nextMethod.BuildString(sb, disasm, _samples, maxRecursionDepth, spaces + 1);
                }

                _addressCount.TryGetValue(address, out var samples);
                AddSpace(spaces, sb);
                sb.Append(address.ToString("x16"))
                    .Append(" ")
                    .Append(instruction)
                    .Append(" : ")
                    .Append(samples)
                    .Append(" samples (")
                    .Append((100d * samples / _samples).ToString("0.##"))
                    .Append("%)")
                    .AppendLine();
            }
        }

        private static void AddSpace(int spaces, StringBuilder sb)
        {
            for (int i = 0; i < spaces; i++)
                sb.Append('-');
        }
    }
}