using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace AndersPedersen.BenchmarkDotNet.InstructionLevelProfiler;

internal struct LazyMethodLookup
{
    private readonly TraceCodeAddress _codeAddress;

    private static Dictionary<TraceModuleFile, bool> _missingSymbols = new Dictionary<TraceModuleFile, bool>();
    private static SymbolReader _reader = new SymbolReader(new StringWriter(),new SymbolPath(SymbolPath.SymbolPathFromEnvironment).Add(SymbolPath.MicrosoftSymbolServerPath).ToString());

    public LazyMethodLookup(TraceCodeAddress codeAddress)
    {
        _codeAddress = codeAddress;
    }

    public string GetName()
    {
        if (_codeAddress.Method == null)
        {
            var moduleFile = _codeAddress.ModuleFile;
            if (moduleFile != null)
            {
                if (!_missingSymbols.TryGetValue(moduleFile, out var _))
                {
                    _codeAddress.CodeAddresses.LookupSymbolsForModule(_reader, moduleFile);
                    if (_codeAddress.Method == null)
                    {
                        _missingSymbols[moduleFile] = true;
                    }
                }
            }
        }

        return _codeAddress.FullMethodName;
    }
}