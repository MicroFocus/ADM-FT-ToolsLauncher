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

using System;
using System.Collections.Generic;

namespace ReportConverter.XmlReport
{
    public abstract class TestReportBase : IReportNode, IReportNodeOwner
    {
        protected TestReportBase(ResultsType root, string reportFile)
        {
            Root = root;
            ReportFile = reportFile;
        }

        /// <summary>
        /// Gets the XML report file path from which all the report nodes are read.
        /// </summary>
        public string ReportFile { get; protected set; }

        /// <summary>
        /// Gets the root element read from the XML report, that is, the <see cref="ResultsType"/> instance.
        /// </summary>
        public ResultsType Root { get; protected set; }

        /// <summary>
        /// Gets the node represents the test report node read from the XML report. 
        /// </summary>
        public ReportNodeType Node { get; protected set; }

        /// <summary>
        /// Gets the owner of the test report node.
        /// </summary>
        public IReportNodeOwner Owner { get { return null; } }

        /// <summary>
        /// Gets the owner test report instance which is the test report itself.
        /// </summary>
        public TestReportBase OwnerTest { get { return this; } }

        /// <summary>
        /// Attempts to parse the test report node.
        /// </summary>
        /// <returns><c>true</c> if the <see cref="Node"/> is parsed successfully; otherwise, <c>false</c>.</returns>
        public virtual bool TryParse()
        {
            // the first report node is treated as the test run node
            Node = ParseTestReportNode();
            if (Node == null)
            {
                return false;
            }

            // test and report name
            TestName = Node.Data.Name;
            ReportName = Root.GeneralInfo.ResultName;
            if (!string.IsNullOrWhiteSpace(TestName))
            {
                if (!string.IsNullOrWhiteSpace(ReportName))
                {
                    TestAndReportName = string.Format("{0} - {1}", TestName, ReportName);
                }
                else
                {
                    TestAndReportName = TestName;
                }
            }
            else
            {
                TestAndReportName = ReportName;
            }

            // testing tool name and version
            TestingToolName = Root.GeneralInfo.OrchestrationToolName;
            if (string.IsNullOrWhiteSpace(TestingToolName))
            {
                TestingToolName = Properties.Resources.TestingToolName_UFT;
            }
            TestingToolVersion = Root.GeneralInfo.OrchestrationToolVersionStringLiteral;
            TestingToolNameVersion = TestingToolName;
            if (!string.IsNullOrWhiteSpace(TestingToolVersion))
            {
                TestingToolNameVersion = string.Format("{0} {1}", TestingToolName, TestingToolVersion);
            }

            // other fields
            TestRunStartTime = XmlReportUtilities.ToDateTime(Root.GeneralInfo.RunStartTime, Root.GeneralInfo.Timezone);
            TimeZone = Root.GeneralInfo.Timezone;
            HostName = Node.Data.Environment.HostName;
            TestDurationSeconds = Node.Data.DurationSpecified ? Node.Data.Duration : 0;
            Locale = Node.Data.Environment.Locale;
            LoginUser = Node.Data.Environment.User;
            OSInfo = Node.Data.Environment.OSInfo;
            CPUInfo = Node.Data.Environment.CpuInfo;
            CPUCores = Node.Data.Environment.NumberOfCoresSpecified ? Node.Data.Environment.NumberOfCores : 0;
            CPUInfoAndCores = string.Format("{0} x {1}", CPUCores, CPUInfo);
            TotalMemory = string.Format("{0} {1}", Node.Data.Environment.TotalMemory, Properties.Resources.Prop_MemoryUnit);

            TestInputParameters = Node.Data.InputParameters;
            if (TestInputParameters == null) TestInputParameters = new ParameterType[0];

            TestOutputParameters = Node.Data.OutputParameters;
            if (TestOutputParameters == null) TestOutputParameters = new ParameterType[0];

            TestAUTs = Node.Data.TestedApplications;
            if (TestAUTs == null) TestAUTs = new TestedApplicationType[0];

            return true;
        }

        /// <summary>
        /// When implemented, parses the report node for the test report.
        /// </summary>
        /// <returns>The <see cref="ReportNodeType"/> instance represents the test report node.</returns>
        protected abstract ReportNodeType ParseTestReportNode();

        public string TestName { get; protected set; }
        public string ReportName { get; protected set; }
        public string TestAndReportName { get; protected set; }
        public string TestingToolName { get; protected set; }
        public string TestingToolVersion { get; protected set; }
        public string TestingToolNameVersion { get; protected set; }
        public DateTime TestRunStartTime { get; protected set; }
        public string TimeZone { get; protected set; }
        public string HostName { get; protected set; }
        public decimal TestDurationSeconds { get; protected set; }
        public string Locale { get; protected set; }
        public string LoginUser { get; protected set; }
        public string OSInfo { get; protected set; }
        public string CPUInfo { get; protected set; }
        public int CPUCores { get; protected set; }
        public string CPUInfoAndCores { get; protected set; }
        public string TotalMemory { get; protected set; }
        public IEnumerable<ParameterType> TestInputParameters { get; protected set; }
        public IEnumerable<ParameterType> TestOutputParameters { get; protected set; }
        public IEnumerable<TestedApplicationType> TestAUTs { get; protected set; }
    }
}
