using System;

namespace HpToolsLauncher
{
    public class TestRunResults
    {
        public TestRunResults()
        {
            FatalErrors = -1;
        }

        private TestState m_enmTestState = TestState.Unknown;
        private TestState m_enmPrevTestState = TestState.Unknown;
        private bool m_hasWarnings = false;

        public bool HasWarnings
        {
            get { return m_hasWarnings; }
            set { m_hasWarnings = value; }
        }
     
        public string TestPath { get; set; }
        public string TestName { get; set; }
        public string TestGroup { get; set; }
        public string ErrorDesc { get; set; }
        public string FailureDesc { get; set; }
        public string ConsoleOut { get; set; }
        public string ConsoleErr { get; set; }
        public TimeSpan Runtime { get; set; }
        public string TestType { get; set; }
        public string ReportLocation { get; set; }
        public int FatalErrors { get; set; }
        public TestState TestState
        {
            get { return m_enmTestState; }
            set { m_enmTestState = value; }
        }

        public TestState PrevTestState
        {
            get { return m_enmPrevTestState; }
            set { m_enmPrevTestState = value; }
        }

        public int PrevRunId { get; set; }
        public TestInfo TestInfo { get; set; }
    }
}
