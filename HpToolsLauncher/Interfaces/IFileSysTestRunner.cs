using System;
namespace HpToolsLauncher
{
    public delegate bool RunCancelledDelegate();
    public interface IFileSysTestRunner
    {
        TestRunResults RunTest(TestInfo fileName, ref string errorReason, RunCancelledDelegate runCancelled);
        void CleanUp();
    }
}