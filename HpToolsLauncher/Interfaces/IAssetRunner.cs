using System;

namespace HpToolsLauncher
{
    public interface IAssetRunner : IDisposable
    {
        TestSuiteRunResults Run();
        bool RunWasCancelled { get; set; }
    }
}