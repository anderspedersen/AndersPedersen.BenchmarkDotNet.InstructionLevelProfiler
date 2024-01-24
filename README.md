# InstructionLevelProfiler

InstructionLevelProfiler is a diagnoser for BenchmarkDotNet that uses CPU sampling during benchmarking to collect profile data, and then outputs the disassembled code showing the number of samples collected at the assembly instruction level.

## How to use

To use instructionLevelProfiler you simply need to use the `InstructionLevelProfiler` attribute with your benchmark class.
```
[InstructionLevelProfiler(performExtraBenchmarksRun: true, maxCallDepth: 5)]
public class Benchmark
{
    //...
}
```

The attribute takes two parameters
- performExtraBenchmarksRun - If set to true BenchmarkDotNet will perform an extra run to gather profile data, so the overhead doesn't affect the other results.
- maxCallDepth - What is the maximum call depth we want to show profile data for.

## Understanding the profile output

>[!IMPORTANT]
>Modern super-scalar processors are complex, and might execute multiple instructions at the same time. Because of this you should be careful when interpreting instruction level profile data.

Let's sat you want to profile the `Dictionary<TKey,TValue>.TryGet`-method, so you build a benchmark like this:
```
[InstructionLevelProfiler]
public class Benchmark
{
    private Dictionary<string, int> _dict = new Dictionary<string, int>();
    private string _key = "MyKey";

    [GlobalSetup]
    public void Setup()
    {
        _dict[_key] = 9;
    }
    
    [Benchmark]
    public int TryGetValue()
    {
        _dict.TryGetValue(_key, out var val);
        return val;
    }
}
```

Let's look at the small sample of the output:

```
TestProject.Benchmark.TryGetValue() : 10447 samples (100%)
00007ffa41e3cef0 sub rsp,0x28 : 252 samples (2.41%)
[...]
-System.Collections.Generic.Dictionary`2[System.__Canon,System.Int32].FindValue(!0) : 9649 samples (92.36%)
-00007ffa41e3cf80 push r15 : 218 samples (2.26%)
[...]
```

The first line contain the signature of our benchmark and the number of samples that was collected that hit this method. In parenthesis we have the percentage of the samples that hit this method (for the first line this will always be 100%).

The next line contains our first assembly instruction, `sub rsp,0x28`, and we can see that the instruction pointer (IP) was pointing at this instruction for 252 of the samples and that it is 2.41% of the 10447 total samples.

Later we find a call to `Dictionary<TKey,TValue>.FindValue`, and we can see that 9649 samples had this call as part of their call stack, and that is 92.36% of the total samples.

The next line contains the first instruction of `Dictionary<TKey,TValue>.FindValue`, we can use the indentation to see that it belongs to this method. We can also see the number of samples where the IP was pointing at this instruction, but this time the percentage is not relative to the total number of samples, but rather to the number of samples that hit this method. In general the percentages will always be relative to the current method.