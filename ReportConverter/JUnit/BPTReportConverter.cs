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
using ReportConverter.XmlReport.BPT;
using System.Collections.Generic;

namespace ReportConverter.JUnit
{
    /// <summary>
    /// Junit-testsuites <==> BPT
    /// Junit-testsuite <==> Business Component (BC)
    /// Junit-testcase <==> GUI Step / API Activity
    /// </summary>
    class BPTReportConverter : ConverterBase
    {
        public BPTReportConverter(CommandArguments args, TestReport input) : base(args)
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
            List<testsuitesTestsuite> list = new List<testsuitesTestsuite>();

            int index = -1;
            EnumerableReportNodes<BusinessComponentReport> bcs = new EnumerableReportNodes<BusinessComponentReport>(Input.AllBCsEnumerator);
            foreach (BusinessComponentReport bcReport in bcs)
            {
                // business component -> testsuite
                index++;
                list.Add(ConvertTestsuite(bcReport, index));
            }

            TestSuites.testsuite = list.ToArray();
            return true;
        }

        /// <summary>
        /// Converts the specified <see cref="BusinessComponentReport"/> to the corresponding JUnit <see cref="testsuitesTestsuite"/>.
        /// </summary>
        /// <param name="bcReport">The <see cref="BusinessComponentReport"/> instance contains the data of a business component.</param>
        /// <param name="index">The index, starts from 0, to identify the order of the testsuites.</param>
        /// <returns>The converted JUnit <see cref="testsuitesTestsuite"/> instance.</returns>
        private testsuitesTestsuite ConvertTestsuite(BusinessComponentReport bcReport, int index)
        {
            // a BPT business component is converted to a JUnit testsuite
            testsuitesTestsuite ts = new testsuitesTestsuite();

            ts.id = index; // Starts at '0' for the first testsuite and is incremented by 1 for each following testsuite 
            ts.package = Input.TestAndReportName; // Derived from testsuite/@name in the non-aggregated documents

            // sample: BC-00001: BC123 (Iteration 1 / Flow3 / Case: not match / Group1)
            ts.name = string.Format("BC-{0,5:00000}: {1}", index + 1, GetBCHierarchyName(bcReport));

            // other JUnit required fields
            ts.timestamp = bcReport.StartTime;
            ts.hostname = Input.HostName;
            if (string.IsNullOrWhiteSpace(ts.hostname)) ts.hostname = "localhost";
            ts.time = bcReport.DurationSeconds;

            // properties
            List<testsuiteProperty> properties = new List<testsuiteProperty>(ConvertTestsuiteCommonProperties(bcReport));
            properties.AddRange(ConvertTestsuiteProperties(bcReport));
            ts.properties = properties.ToArray();

            // JUnit testcases
            int testcaseCount = 0;
            int failureCount = 0;
            ts.testcase = ConvertTestcases(bcReport, out testcaseCount, out failureCount);
            ts.tests = testcaseCount;
            ts.failures = failureCount;

            return ts;
        }

        public static string GetBCHierarchyName(BusinessComponentReport bcReport, string split = " / ")
        {
            string hierarchyName = bcReport.Name;

            GeneralReportNode parentReport = bcReport.Owner as GeneralReportNode;
            while (parentReport != null)
            {
                string name = parentReport.Name;

                // if it is iteration, prepend "Iteration" term
                IterationReport iterationReport = parentReport as IterationReport;
                if (iterationReport != null)
                {
                    name = Properties.Resources.PropName_Iteration + iterationReport.Name;
                }

                // if it is branch, use the case name since the branch case node name is empty
                BranchReport branchReport = parentReport as BranchReport;
                if (branchReport != null)
                {
                    name = Properties.Resources.PropName_Prefix_BPTBranchCase + branchReport.CaseName;
                }

                // concat hierarchy name
                hierarchyName = name + split + hierarchyName;

                parentReport = parentReport.Owner as GeneralReportNode;
            }

            return hierarchyName;
        }

        private IEnumerable<testsuiteProperty> ConvertTestsuiteCommonProperties(GeneralReportNode reportNode)
        {
            return new testsuiteProperty[]
            {
                new testsuiteProperty(Properties.Resources.PropName_TestingTool, Input.TestingToolNameVersion),
                new testsuiteProperty(Properties.Resources.PropName_OSInfo, Input.OSInfo),
                new testsuiteProperty(Properties.Resources.PropName_Locale, Input.Locale),
                new testsuiteProperty(Properties.Resources.PropName_LoginUser, Input.LoginUser),
                new testsuiteProperty(Properties.Resources.PropName_CPUInfo, Input.CPUInfoAndCores),
                new testsuiteProperty(Properties.Resources.PropName_Memory, Input.TotalMemory)
            };
        }

        private static IEnumerable<testsuiteProperty> ConvertTestsuiteProperties(BusinessComponentReport bcReport)
        {
            List<testsuiteProperty> list = new List<testsuiteProperty>();

            // business component path
            list.Add(new testsuiteProperty(Properties.Resources.PropName_BPTBCPath, GetBCHierarchyName(bcReport)));

            // business component input/output parameters
            foreach (ParameterType pt in bcReport.InputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_BCInputParam + pt.NameAndType, pt.value));
            }
            foreach (ParameterType pt in bcReport.OutputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_BCOutputParam + pt.NameAndType, pt.value));
            }

            // business component AUTs
            int i = 0;
            foreach (TestedApplicationType aut in bcReport.AUTs)
            {
                i++;
                string propValue = aut.Name;
                if (!string.IsNullOrWhiteSpace(aut.Version))
                {
                    propValue += string.Format(" {0}", aut.Version);
                }
                if (!string.IsNullOrWhiteSpace(aut.Path))
                {
                    propValue += string.Format(" ({0})", aut.Path);
                }
                list.Add(new testsuiteProperty(string.Format("{0} {1}", Properties.Resources.PropName_Prefix_AUT, i), propValue));
            }

            return list;
        }

        public static testsuiteTestcase[] ConvertTestcases(BusinessComponentReport bcReport, out int count, out int numOfFailures)
        {
            count = 0;
            numOfFailures = 0;

            List<testsuiteTestcase> list = new List<testsuiteTestcase>();
            EnumerableReportNodes<BCStepReport> steps = new EnumerableReportNodes<BCStepReport>(bcReport.AllBCStepsEnumerator);
            foreach (BCStepReport step in steps)
            {
                if (step.IsContext)
                {
                    continue;
                }

                list.Add(ConvertTestcase(step, count));
                if (step.Status == ReportStatus.Failed)
                {
                    numOfFailures++;
                }
                count++;
            }

            return list.ToArray();
        }

        /// <summary>
        /// Converts the specified <see cref="BCStepReport"/> to the corresponding JUnit <see cref="testsuiteTestcase"/>.
        /// </summary>
        /// <param name="stepReport">The <see cref="BCStepReport"/> instance contains the data of a BPT business component step.</param>
        /// <param name="index">The index, starts from 0, to identify the order of the testcases.</param>
        /// <returns>The converted JUnit <see cref="testsuiteTestcase"/> instance.</returns>
        public static testsuiteTestcase ConvertTestcase(BCStepReport stepReport, int index)
        {
            testsuiteTestcase tc = new testsuiteTestcase();
            tc.name = string.Format("#{0,5:00000}: {1}", index + 1, stepReport.Name);
            tc.classname = stepReport.TestObjectPath;
            tc.time = stepReport.DurationSeconds;

            if (stepReport.Status == ReportStatus.Failed)
            {
                testsuiteTestcaseFailure failure = new testsuiteTestcaseFailure();
                failure.message = stepReport.ErrorText;
                failure.type = string.Empty;
                tc.Item = failure;
            }

            return tc;
        }
    }
}
