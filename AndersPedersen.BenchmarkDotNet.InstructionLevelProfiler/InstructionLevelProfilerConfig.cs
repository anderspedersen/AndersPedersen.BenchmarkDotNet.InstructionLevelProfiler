namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class InstructionLevelProfilerConfig
{
    public bool PerformExtraBenchmarksRun { get; }
    public int MaxCallDepth { get; }

    public InstructionLevelProfilerConfig(bool performExtraBenchmarksRun, int maxCallDepth)
    {
        PerformExtraBenchmarksRun = performExtraBenchmarksRun;
        MaxCallDepth = maxCallDepth;
    }
}