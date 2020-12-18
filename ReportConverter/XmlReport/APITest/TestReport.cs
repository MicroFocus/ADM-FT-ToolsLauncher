using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.APITest
{
    public class TestReport : TestReportBase
    {
        private const string NodeType_TestRun = "testrun";
        private const string NodeType_Step = "step";
        private const string NodeType_Action = "action";

        public TestReport(ResultsType root, string reportFile) : base(root, reportFile)
        {
            Iterations = new ReportNodeCollection<IterationReport>(this, ReportNodeFactory.Instance);

            AllActivitiesEnumerator = new ReportNodeEnumerator<ActivityReport>();
        }

        public ReportNodeCollection<IterationReport> Iterations { get; private set; }

        public ActivityReport LastActivity { get; internal set; }

        public ReportNodeEnumerator<ActivityReport> AllActivitiesEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // try to parse the second level report nodes, the type shall be 'Step' for API test
            Iterations.Clear();
            ReportNodeType[] secondLevelNodes = Node.ReportNode;
            if (secondLevelNodes != null)
            {
                foreach (ReportNodeType secondLvNode in secondLevelNodes)
                {
                    // test if the second-level node is Step type
                    if (secondLvNode.type.ToLower() != NodeType_Step)
                    {
                        continue;
                    }

                    // try to parse the third-level node and test if it is 'Action' type
                    ReportNodeType[] actionNodes = secondLvNode.ReportNode;
                    if (actionNodes == null)
                    {
                        continue;
                    }
                    foreach (ReportNodeType actionNode in actionNodes)
                    {
                        // test if the third-level node is Action type
                        if (actionNode.type.ToLower() != NodeType_Action)
                        {
                            continue;
                        }

                        // try to parse the fourth-level node and test if it is 'Iteration' type
                        ReportNodeType[] iterationNodes = actionNode.ReportNode;
                        if (iterationNodes == null)
                        {
                            continue;
                        }
                        foreach (ReportNodeType iterationNode in iterationNodes)
                        {
                            // try to add as an iteration report
                            IterationReport iteration = Iterations.TryParseAndAdd(iterationNode, actionNode);
                            if (iteration != null)
                            {
                                AllActivitiesEnumerator.Merge(iteration.AllActivitiesEnumerator);
                                continue;
                            }
                        }
                    }
                }
            }
            if (Iterations.Length == 0)
            {
                // no iteration node is parsed successfully, it is not a valid API test Xml report
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
