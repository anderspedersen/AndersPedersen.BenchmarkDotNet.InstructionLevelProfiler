using BenchmarkDotNet.Configs;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

[AttributeUsage(AttributeTargets.Class)]
public class InstructionLevelProfilerAttribute : Attribute, IConfigSource
{
    public InstructionLevelProfilerAttribute(bool performExtraBenchmarksRun = true, int maxCallDepth = 5)
    {
        Config = ManualConfig.CreateEmpty().AddDiagnoser(new InstructionLevelProfiler(new InstructionLevelProfilerConfig(performExtraBenchmarksRun: performExtraBenchmarksRun, maxCallDepth: maxCallDepth)));
    }
    
    public IConfig Config { get; }
}