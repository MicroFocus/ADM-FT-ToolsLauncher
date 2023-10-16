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
