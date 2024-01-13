namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class InstructionLevelProfilerConfig
{
    public bool PerformExtraBenchmarksRun { get; }
    public int MaxRecursionDepth { get; }

    public InstructionLevelProfilerConfig(bool performExtraBenchmarksRun, int maxRecursionDepth)
    {
        PerformExtraBenchmarksRun = performExtraBenchmarksRun;
        MaxRecursionDepth = maxRecursionDepth;
    }
}