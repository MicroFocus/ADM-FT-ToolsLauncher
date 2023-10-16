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

using ReportConverter.XmlReport;
using ReportConverter.XmlReport.APITest;
using System.Collections.Generic;
using System.Text;

namespace ReportConverter.JUnit
{
    /// <summary>
    /// Junit-testsuites <==> API test
    /// Junit-testsuite <==> Iteration
    /// Junit-testcase <==> Activity
    /// </summary>
    class APITestReportConverter : ConverterBase
    {
        public APITestReportConverter(CommandArguments args, TestReport input) : base(args)
        {
            Input = input;
            TestSuites = new testsuites();
        }

        public TestReport Input { get; private set; }

        public testsuites TestSuites { get; private set; }

        public override bool SaveFile()
        {
            return SaveFileInternal(TestSuites);
        }

        public override bool Convert()
        {
            bool success = true;
            int index = 0;
            List<testsuitesTestsuite> list = new List<testsuitesTestsuite>();
            foreach (IterationReport iterationReport in Input.Iterations)
            {
                index++;
                testsuitesTestsuite ts = ConvertIterationReport(iterationReport, index);
                if (ts != null)
                {
                    list.Add(ts);
                }
                else
                {
                    success = false;
                }
            }
            TestSuites.testsuite = list.ToArray();
            return success;
        }

        private testsuitesTestsuite ConvertIterationReport(IterationReport iterationReport, int index)
        {
            // a API test iteration is converted to a JUnit testsuite
            testsuitesTestsuite ts = new testsuitesTestsuite();

            ts.id = index - 1; // Starts at '0' for the first testsuite and is incremented by 1 for each following testsuite 
            ts.package = Input.TestAndReportName; // Derived from testsuite/@name in the non-aggregated documents

            // sample: API-00012: Iteration 1
            ts.name = string.Format("API-{0,5:00000}: {1} {2}", index, Properties.Resources.PropName_Iteration, index);

            // other JUnit required fields
            ts.timestamp = iterationReport.StartTime;
            ts.hostname = Input.HostName;
            if (string.IsNullOrWhiteSpace(ts.hostname)) ts.hostname = "localhost";
            ts.time = iterationReport.DurationSeconds;

            // properties
            ts.properties = ConvertProperties(iterationReport, index);

            // JUnit testcases
            int testcaseCount = 0;
            int failureCount = 0;
            ts.testcase = ConvertTestcases(iterationReport, out testcaseCount, out failureCount);
            ts.tests = testcaseCount;
            ts.failures = failureCount;

            return ts;
        }

        private testsuiteProperty[] ConvertProperties(IterationReport iterationReport, int index)
        {
            List<testsuiteProperty> list = new List<testsuiteProperty>(new testsuiteProperty[]
            {
                new testsuiteProperty(Properties.Resources.PropName_Iteration, index.ToString()),
                new testsuiteProperty(Properties.Resources.PropName_TestingTool, Input.TestingToolNameVersion),
                new testsuiteProperty(Properties.Resources.PropName_OSInfo, Input.OSInfo),
                new testsuiteProperty(Properties.Resources.PropName_Locale, Input.Locale),
                new testsuiteProperty(Properties.Resources.PropName_LoginUser, Input.LoginUser),
                new testsuiteProperty(Properties.Resources.PropName_CPUInfo, Input.CPUInfoAndCores),
                new testsuiteProperty(Properties.Resources.PropName_Memory, Input.TotalMemory)
            });

            return list.ToArray();
        }

        public static testsuiteTestcase[] ConvertTestcases(IterationReport iterationReport, out int count, out int numOfFailures)
        {
            count = 0;
            numOfFailures = 0;

            List<testsuiteTestcase> list = new List<testsuiteTestcase>();
            EnumerableReportNodes<ActivityReport> activities = new EnumerableReportNodes<ActivityReport>(iterationReport.AllActivitiesEnumerator);
            foreach (ActivityReport activity in activities)
            {
                list.Add(ConvertTestcase(activity, count));
                if (activity.Status == ReportStatus.Failed)
                {
                    numOfFailures++;
                }
                count++;
            }

            return list.ToArray();
        }

        public static testsuiteTestcase ConvertTestcase(ActivityReport activityReport, int index)
        {
            testsuiteTestcase tc = new testsuiteTestcase();
            tc.name = string.Format("#{0,5:00000}: {1}", index + 1, activityReport.Name);
            if (activityReport.ActivityExtensionData != null && activityReport.ActivityExtensionData.VTDType != null)
            {
                tc.classname = activityReport.ActivityExtensionData.VTDType.Value;
            }
            tc.time = activityReport.DurationSeconds;

            if (activityReport.Status == ReportStatus.Failed)
            {
                testsuiteTestcaseFailure failure = new testsuiteTestcaseFailure();

                if (activityReport.CheckpointData != null && activityReport.CheckpointData.Checkpoints != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (ExtData data in activityReport.CheckpointData.Checkpoints)
                    {
                        if (data.KnownVTDStatus == VTDStatus.Failure)
                        {
                            string actualValue = data.VTDActual != null ? data.VTDActual.Value : string.Empty;
                            string expectedValue = data.VTDExpected != null ? data.VTDExpected.Value : string.Empty;
                            string operation = data.VTDOperation != null ? data.VTDOperation.Value : string.Empty;
                            if (string.IsNullOrEmpty(actualValue) && string.IsNullOrEmpty(expectedValue) && !string.IsNullOrEmpty(operation))
                            {
                                // sample: [Checkpoint 1] Arguments[1]: Array - Fixed (compound)
                                sb.AppendFormat(Properties.Resources.APITest_Checkpoint_CompoundValue,
                                    data.VTDName != null ? data.VTDName.Value : string.Empty,
                                    data.VTDXPath != null ? data.VTDXPath.Value : string.Empty,
                                    !string.IsNullOrEmpty(operation) ? operation : Properties.Resources.APITest_Checkpoint_NoOperation);
                            }
                            else
                            {
                                // sample: [Checkpoint 2] StatusCode: 404 (actual)  =  200 (expected)
                                sb.AppendFormat(Properties.Resources.APITest_Checkpoint_ActExp,
                                    data.VTDName != null ? data.VTDName.Value : string.Empty,
                                    data.VTDXPath != null ? data.VTDXPath.Value : string.Empty,
                                    !string.IsNullOrEmpty(actualValue) ? actualValue : Properties.Resources.APITest_Checkpoint_EmptyValue,
                                    !string.IsNullOrEmpty(operation) ? operation : Properties.Resources.APITest_Checkpoint_NoOperation,
                                    !string.IsNullOrEmpty(expectedValue) ? expectedValue : Properties.Resources.APITest_Checkpoint_EmptyValue);
                            }
                            sb.AppendLine();
                        }
                    }
                    failure.message = sb.ToString();
                }

                if (string.IsNullOrWhiteSpace(failure.message))
                {
                    failure.message = activityReport.Description;
                }
                failure.type = string.Empty;
                tc.Item = failure;
            }

            return tc;
        }
    }
}
