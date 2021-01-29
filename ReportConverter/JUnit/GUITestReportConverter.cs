using ReportConverter.XmlReport;
using ReportConverter.XmlReport.GUITest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.JUnit
{
    class GUITestReportConverter : ConverterBase
    {
        public GUITestReportConverter(CommandArguments args, TestReport input) : base(args)
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
            foreach (IterationReport iterationReport in Input.Iterations)
            {
                foreach (ActionReport actionReport in iterationReport.Actions)
                {
                    if (actionReport.ActionIterations.Length == 0)
                    {
                        // action -> testsuite
                        index++;
                        list.Add(ConvertTestsuite(actionReport, index));
                        continue;
                    }

                    foreach (ActionIterationReport actionIterationReport in actionReport.ActionIterations)
                    {
                        // action iteration -> testsuite
                        index++;
                        list.Add(ConvertTestsuite(actionIterationReport, index));
                        continue;
                    }
                }
            }

            TestSuites.testsuite = list.ToArray();
            return true;
        }

        /// <summary>
        /// Converts the specified <see cref="ActionReport"/> to the corresponding JUnit <see cref="testsuitesTestsuite"/>.
        /// </summary>
        /// <param name="actionReport">The <see cref="ActionReport"/> instance contains the data of an action.</param>
        /// <param name="index">The index, starts from 0, to identify the order of the testsuites.</param>
        /// <returns>The converted JUnit <see cref="testsuitesTestsuite"/> instance.</returns>
        private testsuitesTestsuite ConvertTestsuite(ActionReport actionReport, int index)
        {
            // get owner iteration data
            int iterationIndex = 0;
            if (actionReport.OwnerIteration != null)
            {
                iterationIndex = actionReport.OwnerIteration.Index;
            }

            // a GUI test action is converted to a JUnit testsuite
            testsuitesTestsuite ts = new testsuitesTestsuite();

            ts.id = index; // Starts at '0' for the first testsuite and is incremented by 1 for each following testsuite 
            ts.package = Input.TestAndReportName; // Derived from testsuite/@name in the non-aggregated documents

            // sample: GUI-00012: Iteration 1 / Action 3
            ts.name = string.Format("GUI-{0,5:00000}: {1} {2} / {3}", 
                index + 1,
                Properties.Resources.PropName_Iteration,
                iterationIndex,
                actionReport.Name);

            // other JUnit required fields
            ts.timestamp = actionReport.StartTime;
            ts.hostname = Input.HostName;
            if (string.IsNullOrWhiteSpace(ts.hostname)) ts.hostname = "localhost";
            ts.time = actionReport.DurationSeconds;

            // properties
            List<testsuiteProperty> properties = new List<testsuiteProperty>(ConvertTestsuiteCommonProperties(actionReport));
            properties.AddRange(ConvertTestsuiteProperties(actionReport));
            ts.properties = properties.ToArray();

            // JUnit testcases
            int testcaseCount = 0;
            int failureCount = 0;
            ts.testcase = ConvertTestcases(actionReport, out testcaseCount, out failureCount);
            ts.tests = testcaseCount;
            ts.failures = failureCount;

            return ts;
        }

        /// <summary>
        /// Converts the specified <see cref="ActionIterationReport"/> to the corresponding JUnit <see cref="testsuitesTestsuite"/>.
        /// </summary>
        /// <param name="actionIterationReport">The <see cref="ActionIterationReport"/> instance contains the data of an action iteration.</param>
        /// <param name="index">The index, starts from 0, to identify the order of the testsuites.</param>
        /// <returns>The converted JUnit <see cref="testsuitesTestsuite"/> instance.</returns>
        private testsuitesTestsuite ConvertTestsuite(ActionIterationReport actionIterationReport, int index)
        {
            // get owner action and iteration data
            string actionName = string.Empty;
            int iterationIndex = 0;
            if (actionIterationReport.OwnerAction != null)
            {
                actionName = actionIterationReport.OwnerAction.Name;

                // owner iteration
                if (actionIterationReport.OwnerAction.OwnerIteration != null)
                {
                    iterationIndex = actionIterationReport.OwnerAction.OwnerIteration.Index;
                }
            }

            // a GUI test action iteration is converted to a JUnit testsuite
            testsuitesTestsuite ts = new testsuitesTestsuite();

            ts.id = index; // Starts at '0' for the first testsuite and is incremented by 1 for each following testsuite 
            ts.package = Input.TestAndReportName; // Derived from testsuite/@name in the non-aggregated documents

            // sample: GUI-00012: Iteration 1 / Action 3 / Action Iteration 2
            ts.name = string.Format("GUI-{0,5:00000}: {1} {2} / {3} / {4} {5}",
                index + 1,
                Properties.Resources.PropName_Iteration,
                iterationIndex,
                actionName,
                Properties.Resources.PropName_ActionIteration,
                actionIterationReport.Index);

            // other JUnit required fields
            ts.timestamp = actionIterationReport.StartTime;
            ts.hostname = Input.HostName;
            if (string.IsNullOrWhiteSpace(ts.hostname)) ts.hostname = "localhost";
            ts.time = actionIterationReport.DurationSeconds;

            // properties
            List<testsuiteProperty> properties = new List<testsuiteProperty>(ConvertTestsuiteCommonProperties(actionIterationReport));
            properties.AddRange(ConvertTestsuiteProperties(actionIterationReport));
            ts.properties = properties.ToArray();

            // JUnit testcases
            int testcaseCount = 0;
            int failureCount = 0;
            ts.testcase = ConvertTestcases(actionIterationReport, out testcaseCount, out failureCount);
            ts.tests = testcaseCount;
            ts.failures = failureCount;

            return ts;
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

        private static IEnumerable<testsuiteProperty> ConvertTestsuiteProperties(IterationReport iterationReport)
        {
            List<testsuiteProperty> list = new List<testsuiteProperty>();

            // iteration index
            list.Add(new testsuiteProperty(Properties.Resources.PropName_IterationIndex, iterationReport.Index.ToString()));

            // iteration input/output parameters
            foreach (ParameterType pt in iterationReport.InputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_IterationInputParam + pt.NameAndType, pt.value));
            }
            foreach (ParameterType pt in iterationReport.OutputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_IterationOutputParam + pt.NameAndType, pt.value));
            }

            // iteration AUTs
            int i = 0;
            foreach (TestedApplicationType aut in iterationReport.AUTs)
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

        private static IEnumerable<testsuiteProperty> ConvertTestsuiteProperties(ActionReport actionReport)
        {
            List<testsuiteProperty> list = new List<testsuiteProperty>();

            // action input/output parameters
            foreach (ParameterType pt in actionReport.InputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_ActionInputParam + pt.NameAndType, pt.value));
            }
            foreach (ParameterType pt in actionReport.OutputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_ActionInputParam + pt.NameAndType, pt.value));
            }

            // owner - iteration
            IterationReport iterationReport = actionReport.OwnerIteration;
            if (iterationReport != null)
            {
                // iteration properties
                list.AddRange(ConvertTestsuiteProperties(iterationReport));
            }

            return list.ToArray();
        }

        private static IEnumerable<testsuiteProperty> ConvertTestsuiteProperties(ActionIterationReport actionIterationReport)
        {
            List<testsuiteProperty> list = new List<testsuiteProperty>();

            // action iteration index
            list.Add(new testsuiteProperty(Properties.Resources.PropName_ActionIterationIndex, actionIterationReport.Index.ToString()));

            // iteration input/output parameters
            foreach (ParameterType pt in actionIterationReport.InputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_ActionIterationInputParam + pt.NameAndType, pt.value));
            }
            foreach (ParameterType pt in actionIterationReport.OutputParameters)
            {
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Prefix_ActionIterationOutputParam + pt.NameAndType, pt.value));
            }

            // owner - action
            ActionReport actionReport = actionIterationReport.OwnerAction;
            if (actionReport != null)
            {
                // action name
                list.Add(new testsuiteProperty(Properties.Resources.PropName_Action, actionReport.Name));
                // action properties
                list.AddRange(ConvertTestsuiteProperties(actionReport));
            }

            return list;
        }

        private static testsuiteTestcase[] ConvertTestcases(ActionReport actionReport, out int count, out int numOfFailures)
        {
            count = 0;
            numOfFailures = 0;

            List<testsuiteTestcase> list = new List<testsuiteTestcase>();
            EnumerableReportNodes<StepReport> steps = new EnumerableReportNodes<StepReport>(actionReport.AllStepsEnumerator);
            foreach (StepReport step in steps)
            {
                testsuiteTestcase tc = ConvertTestcase(step, count);
                if (tc == null)
                {
                    continue;
                }

                list.Add(tc);
                if (step.Status == ReportStatus.Failed)
                {
                    numOfFailures++;
                }
                count++;
            }

            return list.ToArray();
        }

        private static testsuiteTestcase[] ConvertTestcases(ActionIterationReport actionIterationReport, out int count, out int numOfFailures)
        {
            count = 0;
            numOfFailures = 0;

            List<testsuiteTestcase> list = new List<testsuiteTestcase>();
            EnumerableReportNodes<StepReport> steps = new EnumerableReportNodes<StepReport>(actionIterationReport.AllStepsEnumerator);
            foreach (StepReport step in steps)
            {
                testsuiteTestcase tc = ConvertTestcase(step, count);
                if (tc == null)
                {
                    continue;
                }

                list.Add(tc);
                if (step.Status == ReportStatus.Failed)
                {
                    numOfFailures++;
                }
                count++;
            }

            return list.ToArray();
        }

        /// <summary>
        /// Converts the specified <see cref="StepReport"/> to the corresponding JUnit <see cref="testsuiteTestcase"/>.
        /// </summary>
        /// <param name="stepReport">The <see cref="StepReport"/> instance contains the data of a GUI test step.</param>
        /// <param name="index">The index, starts from 0, to identify the order of the testcases.</param>
        /// <returns>The converted JUnit <see cref="testsuiteTestcase"/> instance.</returns>
        private static testsuiteTestcase ConvertTestcase(StepReport stepReport, int index)
        {
            // the step might be a checkpoint
            CheckpointReport checkpointReport = CheckpointReport.FromStepReport(stepReport);
            if (checkpointReport != null)
            {
                return ConvertTestcase(checkpointReport, index);
            }

            // a step with smart identification?
            if (stepReport.SmartIdentification != null)
            {
                return ConvertTestcaseWithSmartIdentificationInfo(stepReport, index);
            }

            // a general step
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

        private static testsuiteTestcase ConvertTestcase(CheckpointReport checkpointReport, int index)
        {
            if (checkpointReport == null || string.IsNullOrWhiteSpace(checkpointReport.CheckpointType))
            {
                // not a checkpoint or checkpoint type is empty - ignore
                return null;
            }

            // sample: Standard Checkpoint (DB Checkpoint) - "checkpoint 1"
            string checkpointDisplayName = checkpointReport.CheckpointType;
            if (!string.IsNullOrWhiteSpace(checkpointReport.CheckpointSubType))
            {
                checkpointDisplayName += string.Format(" ({0})", checkpointReport.CheckpointSubType);
            }
            checkpointDisplayName += " - " + checkpointReport.Name;

            testsuiteTestcase tc = new testsuiteTestcase();
            tc.name = string.Format("#{0,5:00000}: {1}", index + 1, checkpointDisplayName);
            tc.classname = checkpointReport.StepReport.TestObjectPath;
            tc.time = checkpointReport.StepReport.DurationSeconds;

            if (checkpointReport.Status == ReportStatus.Failed)
            {
                testsuiteTestcaseFailure failure = new testsuiteTestcaseFailure();
                failure.message = checkpointReport.FailedDescription;
                failure.type = string.Empty;
                tc.Item = failure;
            }

            return tc;
        }

        private static StepReport _lastSkippedSIDStep;

        private static testsuiteTestcase ConvertTestcaseWithSmartIdentificationInfo(StepReport stepReport, int index)
        {
            SmartIdentificationInfoExtType sid = stepReport.SmartIdentification;
            if (sid == null)
            {
                throw new ArgumentNullException("stepReport.SmartIdentification");
            }

            if (stepReport.Status == ReportStatus.Warning)
            {
                // a step with smart identification info and warning status can be ignored
                // since the next step is the official smart identification info report node
                _lastSkippedSIDStep = stepReport;
                return null;
            }

            // a step with smart identification info
            int basicMatches = 0;
            if (sid.SIDBasicProperties != null)
            {
                basicMatches = sid.SIDBasicProperties.BasicMatch;
            }

            List<string> optList = new List<string>();
            if (sid.SIDOptionalProperties != null)
            {
                foreach (SIDOptionalPropertyExtType property in sid.SIDOptionalProperties)
                {
                    if (property.Matches > 0)
                    {
                        optList.Add(string.Format("{0}=\"{1}\"", property.Name, property.Value));
                    }
                }
            }
            string sidDesc = string.Format(Properties.Resources.GUITest_SID_Description, basicMatches, string.Join(", ", optList));
            string sidName = stepReport.Node.Data.Name;

            testsuiteTestcase tc = new testsuiteTestcase();
            tc.name = string.Format("#{0,5:00000}: {1} ({2})", index + 1, sidName, sidDesc);
            tc.classname = stepReport.TestObjectPath;
            tc.time = stepReport.DurationSeconds + (_lastSkippedSIDStep != null ? _lastSkippedSIDStep.DurationSeconds : 0);

            // clear last skipped SID step
            _lastSkippedSIDStep = null;

            return tc;
        }
    }
}
