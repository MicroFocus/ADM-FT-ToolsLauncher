using System;
using System.Collections.Generic;

namespace HpToolsLauncher
{
    public class TestSuiteRunResults
    {
        private List<TestRunResults> m_testRuns = new List<TestRunResults>();
        private int m_numErrors = 0;
        private int m_numFailures = 0;
        private int m_numTests = 0;
        private TimeSpan m_totalRunTime = TimeSpan.Zero;

        public string SuiteName { get; set; }

        public int NumFailures
        {
            get { return m_numFailures; }
            set { m_numFailures = value; }
        }

        public int NumTests
        {
            get { return m_numTests; }
            set { m_numTests = value; }
        }

        public TimeSpan TotalRunTime
        {
            get { return m_totalRunTime; }
            set { m_totalRunTime = value; }
        }

        public List<TestRunResults> TestRuns
        {
            get { return m_testRuns; }
            set { m_testRuns = value; }
        }

        public int NumErrors
        {
            get { return m_numErrors; }
            set { m_numErrors = value; }
        }


        internal void AppendResults(TestSuiteRunResults desc)
        {
            this.TestRuns.AddRange(desc.TestRuns);
            this.TotalRunTime += desc.TotalRunTime;
            this.NumErrors += desc.NumErrors;
            this.NumFailures += desc.NumFailures;
            this.NumTests += desc.NumTests;
        }
    }
}
