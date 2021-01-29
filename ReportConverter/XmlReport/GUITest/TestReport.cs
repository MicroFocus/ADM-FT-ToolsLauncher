using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class TestReport : TestReportBase
    {
        private const string NodeType_TestRun = "testrun";

        public TestReport(ResultsType root, string reportFile) : base(root, reportFile)
        {
            Iterations = new ReportNodeCollection<IterationReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();
        }

        public ReportNodeCollection<IterationReport> Iterations { get; private set; }

        public StepReport LastStep { get; internal set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // try to parse the second level report nodes, the type shall be 'iteration' for GUI test
            Iterations.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    IterationReport iteration = Iterations.TryParseAndAdd(node, this.Node);
                    if (iteration != null)
                    {
                        AllStepsEnumerator.Merge(iteration.AllStepsEnumerator);
                        continue;
                    }
                }
            }
            if (Iterations.Length == 0)
            {
                // no iteration node is parsed successfully under testrun node, it is not a valid GUI test Xml report
                return false;
            }

            return true;
        }

        protected override ReportNodeType ParseTestReportNode()
        {
            ReportNodeType firstReportNode = Root.ReportNode;
            if (firstReportNode.type.ToLower() != NodeType_TestRun)
            {
                return null;
            }

            return firstReportNode;
        }
    }
}
