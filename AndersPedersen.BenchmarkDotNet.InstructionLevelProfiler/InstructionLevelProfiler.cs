using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Session;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class InstructionLevelProfiler : IDiagnoser
{
    private StringBuilder _sb = new StringBuilder();
    private BenchmarkProfileDataCollector _session;
    private readonly InstructionLevelProfilerConfig _config;
    private readonly Dictionary<BenchmarkCase, string> _results;

    public InstructionLevelProfiler(InstructionLevelProfilerConfig config)
    {
        _config = config;
        _results = new Dictionary<BenchmarkCase, string>();
        Exporters = [new Exporter(_results)];
    }

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => _config.PerformExtraBenchmarksRun ? RunMode.ExtraRun : RunMode.NoOverhead;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
    {
        switch (signal)
        {
            case HostSignal.BeforeAnythingElse:
                var mi = parameters.BenchmarkCase.Descriptor.WorkloadMethod;
                _session = new BenchmarkProfileDataCollector(GetMethodFullName(mi));
                _session.Setup(parameters.Process.Id);
                break;
            case HostSignal.BeforeActualRun:
                _session.Start();
                break;
            case HostSignal.AfterActualRun:
                _session.Stop();
                _sb.Clear();
                try
                {
                    _session.BuildString(_sb, _config.MaxRecursionDepth);
                }
                catch (Exception e)
                {
                    _sb.Append(e);
                }
                _results.Add(parameters.BenchmarkCase, _sb.ToString());
                break;
        }
    }

    private static string GetMethodFullName(MethodInfo mi)
    {
        return mi.DeclaringType.FullName + "." + mi.Name;
    }

    public void DisplayResults(ILogger logger)
    {
        logger.WriteInfo("Profile data got exported to \".\\BenchmarkDotNet.Artifacts\\results\\*-ilp.txt\"");
    }

    public IEnumerable<string> Ids { get; } = new[] { nameof(InstructionLevelProfiler) };

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => Array.Empty<Metric>();
    
    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new ValidationError(true, "InstructionLevelProfiler is supported only on Windows");
            yield break;
        }

        if (TraceEventSession.IsElevated() != true)
            yield return new ValidationError(true, "Must be elevated (Admin) to use ETW Kernel Session (required for InstructionLevelProfiler).");
        
        var processArchitecture = RuntimeInformation.ProcessArchitecture;
        foreach (var benchmark in validationParameters.Benchmarks)
        {
            if (!SupportedArchitectures(processArchitecture, benchmark))
            {
                yield return new ValidationError(true, "Benchmark and benchmark runner must use same architecture (X64 or X86).", benchmark);
            }
        }
    }

    private static bool SupportedArchitectures(Architecture processArchitecture, BenchmarkCase benchmark)
    {
        return (processArchitecture == Architecture.X86 && benchmark.Job.Environment.Platform is Platform.X86 or Platform.AnyCpu) || (processArchitecture == Architecture.X64 && benchmark.Job.Environment.Platform is Platform.X64 or Platform.AnyCpu);
    }

    public IEnumerable<IExporter> Exporters { get; }
    public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
}