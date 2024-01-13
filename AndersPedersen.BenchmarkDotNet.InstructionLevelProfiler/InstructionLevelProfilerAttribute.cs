using BenchmarkDotNet.Configs;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

[AttributeUsage(AttributeTargets.Class)]
public class InstructionLevelProfilerAttribute : Attribute, IConfigSource
{
    public InstructionLevelProfilerAttribute(bool performExtraBenchmarksRun = true, int maxRecursionDepth = 5)
    {
        Config = ManualConfig.CreateEmpty().AddDiagnoser(new InstructionLevelProfiler(new InstructionLevelProfilerConfig(performExtraBenchmarksRun: performExtraBenchmarksRun, maxRecursionDepth: maxRecursionDepth)));
    }
    
    public IConfig Config { get; }
}