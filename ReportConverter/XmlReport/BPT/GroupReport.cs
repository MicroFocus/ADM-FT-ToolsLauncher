using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    public class GroupReport : GeneralReportNode
    {
        public GroupReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            SubGroups = new ReportNodeCollection<GroupReport>(this, ReportNodeFactory.Instance);
            Flows = new ReportNodeCollection<FlowReport>(this, ReportNodeFactory.Instance);
            Branches = new ReportNodeCollection<BranchReport>(this, ReportNodeFactory.Instance);
            BusinessComponents = new ReportNodeCollection<BusinessComponentReport>(this, ReportNodeFactory.Instance);
            RecoverySteps = new ReportNodeCollection<RecoveryStepReport>(this, ReportNodeFactory.Instance);

            AllBCsEnumerator = new ReportNodeEnumerator<BusinessComponentReport>();
        }

        public ReportNodeCollection<GroupReport> SubGroups { get; private set; }
        public ReportNodeCollection<FlowReport> Flows { get; private set; }
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

            // sub-groups, flows, branches, bcs
            SubGroups.Clear();
            Flows.Clear();
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

                    // sub-group
                    GroupReport group = SubGroups.TryParseAndAdd(node, this.Node);
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
