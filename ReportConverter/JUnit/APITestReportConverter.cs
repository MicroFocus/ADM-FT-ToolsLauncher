using ReportConverter.XmlReport;
using ReportConverter.XmlReport.APITest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.JUnit
{
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

            // sample: Iteration 1
            ts.name = string.Format("{0} {1}", Properties.Resources.PropName_Iteration, index);

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

        private testsuiteTestcase[] ConvertTestcases(IterationReport iterationReport, out int count, out int numOfFailures)
        {
            count = 0;
            numOfFailures = 0;

            List<testsuiteTestcase> list = new List<testsuiteTestcase>();
            EnumerableReportNodes<ActivityReport> activities = new EnumerableReportNodes<ActivityReport>(iterationReport.AllActivitiesEnumerator);
            foreach (ActivityReport activity in activities)
            {
                list.Add(ConvertTestcase(activity, count + 1));
                if (activity.Status == ReportStatus.Failed)
                {
                    numOfFailures++;
                }
                count++;
            }

            return list.ToArray();
        }

        private testsuiteTestcase ConvertTestcase(ActivityReport activityReport, int index)
        {
            testsuiteTestcase tc = new testsuiteTestcase();
            tc.name = string.Format("#{0,5:00000}: {1}", index, activityReport.Name);
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
