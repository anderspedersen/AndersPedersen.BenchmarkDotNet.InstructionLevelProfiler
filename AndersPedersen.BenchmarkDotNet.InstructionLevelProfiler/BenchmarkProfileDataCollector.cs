using System.Text;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal class BenchmarkProfileDataCollector
{
    private TraceEventSession? _session;
    private bool _started = false;
    private bool _dataCollected = false;
    private readonly string _benchmarkMethodName;
    private ProfileData _pd;

    public BenchmarkProfileDataCollector(string benchmarkMethodName)
    {
        _benchmarkMethodName = benchmarkMethodName;
        _pd = new ProfileData(benchmarkMethodName);
    }

    public void Start()
    {
        _started = true;
    }

    public void Setup(int pid)
    {
        string sessionName = "InstructionLevelProfiler+" + Guid.NewGuid().ToString();
        _session = new TraceEventSession(sessionName, TraceEventSessionOptions.Create);
        if (!EnableProviders(_session))
        {
            _session.Dispose();
            throw new Exception();
        }

        _pd.SetPid(pid);

        Task.Run(() =>
        {
            using var source = TraceLog.CreateFromTraceEventSession(_session);
            {
                source.Kernel.PerfInfoSample += data =>
                {
                    _dataCollected = true;
                    if (data.ProcessID != pid || !_started)
                        return;

                    var callstack = data.CallStack();
                    if (callstack is null)
                        return;

                    HandleCallstack(callstack);
                };
                source.Process();
            }
        });
    }

    private void HandleCallstack(TraceCallStack callstack)
    {
        var sf = new StackFrameData[callstack.Depth];
        var containsBmMethod = false;

        for (var i = callstack.Depth - 1; i > 0 || callstack is not null; i--)
        {
            if (callstack.CodeAddress.FullMethodName.Contains(_benchmarkMethodName))
                containsBmMethod = true;

            sf[i] = new StackFrameData
            {
                Address = callstack.CodeAddress.Address,
                MethodName = callstack.CodeAddress.FullMethodName,
                MethodLookup = new LazyMethodLookup(callstack.CodeAddress)
            };
            callstack = callstack.Caller;
        }

        if (containsBmMethod)
        {
            _pd.AddStack(sf);
        }
    }

    public void Stop()
    {
        _started = false;
        _session?.Dispose();
    }

    public void BuildString(StringBuilder sb, int maxRecursionDepth)
    {
        if (_dataCollected)
        {
            _pd.BuildString(sb, maxRecursionDepth);
        }
        else
        {
            sb.AppendLine("Failed to collect any CPU samples.");
        }
    }

    private static bool EnableProviders(TraceEventSession session)
    {
        session.BufferSizeMB = 256;

        var success = session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.ImageLoad |
            KernelTraceEventParser.Keywords.Process |
            KernelTraceEventParser.Keywords.Profile,
            stackCapture: KernelTraceEventParser.Keywords.Profile
        );
        if (!success) return false;

        session.EnableProvider(
            ClrTraceEventParser.ProviderGuid,
            TraceEventLevel.Verbose,
            (ulong) (
                ClrTraceEventParser.Keywords.Jit |
                ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                ClrTraceEventParser.Keywords.Loader |
                ClrTraceEventParser.Keywords.StartEnumeration
            )
        );

        session.EnableProvider(
            ClrRundownTraceEventParser.ProviderGuid,
            TraceEventLevel.Verbose,
            (ulong) (
                ClrTraceEventParser.Keywords.Jit |
                ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                ClrTraceEventParser.Keywords.Loader |
                ClrTraceEventParser.Keywords.StartEnumeration
            ));

        return true;
    }
}

internal class StackFrameData
{
    public ulong Address;
    public string MethodName;
    public LazyMethodLookup MethodLookup;
}