using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    public class FlowReport : GeneralReportNode
    {
        public FlowReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Iterations = new ReportNodeCollection<IterationReport>(this, ReportNodeFactory.Instance);
            Groups = new ReportNodeCollection<GroupReport>(this, ReportNodeFactory.Instance);
            SubFlows = new ReportNodeCollection<FlowReport>(this, ReportNodeFactory.Instance);
            Branches = new ReportNodeCollection<BranchReport>(this, ReportNodeFactory.Instance);
            BusinessComponents = new ReportNodeCollection<BusinessComponentReport>(this, ReportNodeFactory.Instance);
            RecoverySteps = new ReportNodeCollection<RecoveryStepReport>(this, ReportNodeFactory.Instance);

            AllBCsEnumerator = new ReportNodeEnumerator<BusinessComponentReport>();
        }

        public ReportNodeCollection<IterationReport> Iterations { get; private set; }
        public ReportNodeCollection<GroupReport> Groups { get; private set; }
        public ReportNodeCollection<FlowReport> SubFlows { get; private set; }
        public ReportNodeCollection<BranchReport> Branches { get; private set; }
        public ReportNodeCollection<BusinessComponentReport> BusinessComponents { get; private set; }
        public ReportNodeCollection<RecoveryStepReport> RecoverySteps { get; private set; }

        public ReportNodeEnumerator<BusinessComponentReport> AllBCsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // iterations, groups, flows, branches, bcs
            Iterations.Clear();
            Groups.Clear();
            SubFlows.Clear();
            Branches.Clear();
            BusinessComponents.Clear();

            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // business component
                    BusinessComponentReport bc = BusinessComponents.TryParseAndAdd(node, this.Node);
                    if (bc != null)
                    {
                        AllBCsEnumerator.Add(bc);
                        continue;
                    }

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
                    FlowReport flow = SubFlows.TryParseAndAdd(node, this.Node);
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

                    // recovery step
                    RecoveryStepReport recovery = RecoverySteps.TryParseAndAdd(node, this.Node);
                    if (recovery != null)
                    {
                        AllBCsEnumerator.Merge(recovery.AllBCsEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }
    }
}
