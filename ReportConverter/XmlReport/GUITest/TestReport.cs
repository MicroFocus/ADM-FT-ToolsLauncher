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
                // no iteration node is parsed successfully under testrun node,
                // it might because the GUI test is run with one iteration only
                // which omits the iteration node in the report Xml
                // here create a temporary iteration node so that the nodes read from the Xml
                // can be processed properly
                ReportNodeType iterationNode = new ReportNodeType
                {
                    type = "Iteration",
                    Data = new DataType
                    {
                        Name = "Action0",
                        IndexSpecified = true,
                        Index = 1,
                        Result = "Done",
                        StartTime = Node.Data.StartTime,
                        DurationSpecified = Node.Data.DurationSpecified,
                        Duration = Node.Data.Duration
                    },
                    ReportNode = Node.ReportNode
                };
                IterationReport iteration = Iterations.TryParseAndAdd(iterationNode, this.Node);
                if (iteration != null)
                {
                    AllStepsEnumerator.Merge(iteration.AllStepsEnumerator);
                }

                if (Iterations.Length == 0)
                {
                    // failed to parse at least one iteration, not a valid GUI test
                    return false;
                }
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
