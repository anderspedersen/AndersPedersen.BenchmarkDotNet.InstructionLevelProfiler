using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class Exporter : ExporterBase
{
    private readonly IReadOnlyDictionary<BenchmarkCase, string> results;

    public Exporter(IReadOnlyDictionary<BenchmarkCase, string> results)
    {
        this.results = results;
    }

    protected override string FileExtension => "txt";
    protected override string FileCaption => "ilp";
    
    public override void ExportToLog(Summary summary, ILogger logger)
    {
        foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
        {
            logger.WriteLine(summary[benchmarkCase].ToString());

            logger.WriteLine(results[benchmarkCase]);
        }
    }
}