using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    public class TestReport : TestReportBase
    {
        private const string NodeType_BPTestRun = "Business Process";

        public TestReport(ResultsType root, string reportFile) : base(root, reportFile)
        {
            Iterations = new ReportNodeCollection<IterationReport>(this, ReportNodeFactory.Instance);
            Groups = new ReportNodeCollection<GroupReport>(this, ReportNodeFactory.Instance);
            Flows = new ReportNodeCollection<FlowReport>(this, ReportNodeFactory.Instance);
            Branches = new ReportNodeCollection<BranchReport>(this, ReportNodeFactory.Instance);
            BusinessComponents = new ReportNodeCollection<BusinessComponentReport>(this, ReportNodeFactory.Instance);
            RecoverySteps = new ReportNodeCollection<RecoveryStepReport>(this, ReportNodeFactory.Instance);
            GeneralSteps = new ReportNodeCollection<GeneralStepReport>(this, ReportNodeFactory.Instance);

            AllBCsEnumerator = new ReportNodeEnumerator<BusinessComponentReport>();
        }

        public ReportNodeCollection<IterationReport> Iterations { get; private set; }
        public ReportNodeCollection<GroupReport> Groups { get; private set; }
        public ReportNodeCollection<FlowReport> Flows { get; private set; }
        public ReportNodeCollection<BranchReport> Branches { get; private set; }
        public ReportNodeCollection<BusinessComponentReport> BusinessComponents { get; private set; }
        public ReportNodeCollection<RecoveryStepReport> RecoverySteps { get; private set; }
        public ReportNodeCollection<GeneralStepReport> GeneralSteps { get; private set; }

        public ReportNodeEnumerator<BusinessComponentReport> AllBCsEnumerator { get; private set; }


        internal BusinessComponentReport LastBusinessComponent { get; set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // try to parse the second level report nodes
            Iterations.Clear();
            Groups.Clear();
            Flows.Clear();
            Branches.Clear();
            BusinessComponents.Clear();
            RecoverySteps.Clear();
            GeneralSteps.Clear();

            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // iteration
                    IterationReport iteration = Iterations.TryParseAndAdd(node, this.Node);
                    if (iteration != null)
                    {
                        AllBCsEnumerator.Merge(iteration.AllBCsEnumerator);
                        continue;
                    }

                    // group
                    GroupReport group = Groups.TryParseAndAdd(node, this.Node);
                    if (group != null)
                    {
                        AllBCsEnumerator.Merge(group.AllBCsEnumerator);
                        continue;
                    }

                    // flow
                    FlowReport flow = Flows.TryParseAndAdd(node, this.Node);
                    if (flow != null)
                    {
                        AllBCsEnumerator.Merge(flow.AllBCsEnumerator);
                        continue;
                    }

                    // branch
                    BranchReport branch = Branches.TryParseAndAdd(node, this.Node);
                    if (branch != null)
                    {
                        AllBCsEnumerator.Merge(branch.AllBCsEnumerator);
                        continue;
                    }

                    // business component
                    BusinessComponentReport bc = BusinessComponents.TryParseAndAdd(node, this.Node);
                    if (bc != null)
                    {
                        AllBCsEnumerator.Add(bc);
                        continue;
                    }

                    // recovery step
                    RecoveryStepReport recovery = RecoverySteps.TryParseAndAdd(node, this.Node);
                    if (recovery != null)
                    {
                        AllBCsEnumerator.Merge(recovery.AllBCsEnumerator);
                        continue;
                    }

                    // general step
                    GeneralStepReport generalStep = GeneralSteps.TryParseAndAdd(node, this.Node);
                    if (generalStep != null)
                    {
                        AllBCsEnumerator.Merge(generalStep.AllBCsEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }

        protected override ReportNodeType ParseTestReportNode()
        {
            ReportNodeType firstReportNode = Root.ReportNode;
            if (firstReportNode.type.ToLower() != NodeType_BPTestRun.ToLower())
            {
                return null;
            }

            return firstReportNode;
        }
    }
}
