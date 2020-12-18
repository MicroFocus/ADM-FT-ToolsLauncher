using ReportConverter.XmlReport;
using ReportConverter.XmlReport.BPT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.JUnit
{
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

        private string GetBCHierarchyName(BusinessComponentReport bcReport, string split = " / ")
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

        private IEnumerable<testsuiteProperty> ConvertTestsuiteProperties(BusinessComponentReport bcReport)
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

        private testsuiteTestcase[] ConvertTestcases(BusinessComponentReport bcReport, out int count, out int numOfFailures)
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
        private testsuiteTestcase ConvertTestcase(BCStepReport stepReport, int index)
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
