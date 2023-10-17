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
