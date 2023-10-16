/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

namespace ReportConverter.XmlReport.BPT
{
    public class RecoveryStepReport : GeneralReportNode
    {
        public RecoveryStepReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Groups = new ReportNodeCollection<GroupReport>(this, ReportNodeFactory.Instance);
            Flows = new ReportNodeCollection<FlowReport>(this, ReportNodeFactory.Instance);
            Branches = new ReportNodeCollection<BranchReport>(this, ReportNodeFactory.Instance);
            BusinessComponents = new ReportNodeCollection<BusinessComponentReport>(this, ReportNodeFactory.Instance);
            RecoverySteps = new ReportNodeCollection<RecoveryStepReport>(this, ReportNodeFactory.Instance);
            GeneralSteps = new ReportNodeCollection<GeneralStepReport>(this, ReportNodeFactory.Instance);

            AllBCsEnumerator = new ReportNodeEnumerator<BusinessComponentReport>();
        }

        public ReportNodeCollection<GroupReport> Groups { get; private set; }
        public ReportNodeCollection<FlowReport> Flows { get; private set; }
        public ReportNodeCollection<BranchReport> Branches { get; private set; }
        public ReportNodeCollection<BusinessComponentReport> BusinessComponents { get; private set; }
        public ReportNodeCollection<RecoveryStepReport> RecoverySteps { get; private set; }
        public ReportNodeCollection<GeneralStepReport> GeneralSteps { get; private set; }

        public ReportNodeEnumerator<BusinessComponentReport> AllBCsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // name
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = Properties.Resources.Test_Recovery;
            }

            // groups, flows, branches, bcs
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
                    // business component
                    BusinessComponentReport bc = BusinessComponents.TryParseAndAdd(node, this.Node);
                    if (bc != null)
                    {
                        AllBCsEnumerator.Add(bc);
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

                    // recovery steps
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
    }
}
