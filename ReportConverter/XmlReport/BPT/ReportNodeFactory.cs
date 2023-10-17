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
    internal class ReportNodeFactory : IReportNodeFactory
    {
        private const string NodeType_BPTestRun = "Business Process";
        private const string NodeType_Iteration = "iteration";
        private const string NodeType_Group = "group";
        private const string NodeType_Flow = "flow";
        private const string NodeType_BranchCase = "case";
        private const string NodeType_BC = "business component";
        private const string NodeType_Step = "step";
        private const string NodeType_Recovery = "recovery";

        private static readonly ReportNodeFactory _instance = new ReportNodeFactory();

        private ReportNodeFactory()
        {
        }

        static ReportNodeFactory()
        {
        }

        public static ReportNodeFactory Instance { get { return _instance; } }

        T IReportNodeFactory.Create<T>(ReportNodeType node, ReportNodeType parentNode, IReportNodeOwner owner)
        {
            if (node == null)
            {
                return null;
            }

            string nodeType = node.type.ToLower();
            switch (nodeType)
            {
                case NodeType_BPTestRun:
                    return null;

                case NodeType_Iteration:
                    return new IterationReport(node, owner) as T;

                case NodeType_Group:
                    return new GroupReport(node, owner) as T;

                case NodeType_Flow:
                    return new FlowReport(node, owner) as T;

                case NodeType_BranchCase:
                    return new BranchReport(node, owner) as T;

                case NodeType_BC:
                    return new BusinessComponentReport(node, owner) as T;

                case NodeType_Step:
                    if (node.Data.Extension.NodeType != null && node.Data.Extension.NodeType.Trim().ToLower() == NodeType_Recovery)
                    {
                        return new RecoveryStepReport(node, owner) as T;
                    }
                    else
                    {
                        return new GeneralStepReport(node, owner) as T;
                    }

                default:
                    return null;
            }
        }
    }
}
