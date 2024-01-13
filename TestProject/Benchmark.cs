using AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;
using BenchmarkDotNet.Attributes;

namespace TestProject;

[InstructionLevelProfiler]
public class Benchmark
{
    [Benchmark]
    public void TestBenchmark()
    {
        // Put test benchmark here
    }
}