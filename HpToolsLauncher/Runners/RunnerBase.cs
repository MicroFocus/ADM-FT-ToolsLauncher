using System;

namespace HpToolsLauncher
{
    public class RunnerBase: IAssetRunner
    {
        
        public virtual void Dispose()
        {
        }
        protected bool _blnRunCancelled = false;

        public bool RunWasCancelled
        {
            get { return _blnRunCancelled; }
            set { _blnRunCancelled = value; }
        }

        public virtual TestSuiteRunResults Run()
        {
            throw new NotImplementedException();
        }
        
    }
}
