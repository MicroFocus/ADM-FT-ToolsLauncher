using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HpToolsLauncher
{
    public class JunitXmlBuilder : IXmlBuilder
    {
        private string _xmlName = "APIResults.xml";
        private CultureInfo _culture;

        public string XmlName
        {
            get { return _xmlName; }
            set { _xmlName = value; }
        }
        public CultureInfo Culture
        {
            get { return _culture; }
            set { _culture = value; }
        }
        public bool TestNameOnly { get; set; }
        //public const string ClassName = "uftRunner";
        public const string ClassName = "FTToolsLauncher";
        public const string RootName = "uftRunnerRoot";

        XmlSerializer _serializer = new XmlSerializer(typeof(testsuites));

        testsuites _testSuites = new testsuites();


        public JunitXmlBuilder()
        {
            _testSuites.name = RootName;
        }

        /// <summary>
        /// converts all data from the test results in to the Junit xml format and writes the xml file to disk.
        /// </summary>
        /// <param name="results"></param>
        public bool CreateXmlFromRunResults(TestSuiteRunResults results, out string error)
        {
            error = string.Empty;

            _testSuites = new testsuites();

            testsuite uftts = new testsuite
            {
                errors = IntToString(results.NumErrors),
                tests = IntToString(results.NumTests),
                failures = IntToString(results.NumFailures),
                name = results.SuiteName,
                package = ClassName,
                time = DoubleToString(results.TotalRunTime.TotalSeconds)
            };
            foreach (TestRunResults testRes in results.TestRuns)
            {
                if (testRes.TestType == TestType.LoadRunner.ToString())
                {
                    testsuite lrts = CreateXmlFromLRRunResults(testRes);
                    _testSuites.AddTestsuite(lrts);
                }
                else
                {
                    testcase ufttc = CreateXmlFromUFTRunResults(testRes);
                    uftts.AddTestCase(ufttc);
                }
            }
            if (uftts.testcase.Length > 0)
            {
                _testSuites.AddTestsuite(uftts);
            }

            try
            {
                if (File.Exists(XmlName))
                {
                    File.Delete(XmlName);
                }

                using (Stream s = File.OpenWrite(XmlName))
                {
                    _serializer.Serialize(s, _testSuites);
                }

                return File.Exists(XmlName);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private testsuite CreateXmlFromLRRunResults(TestRunResults testRes)
        {
            testsuite lrts = new testsuite();
            int totalTests = 0, totalFailures = 0, totalErrors = 0;

            // two LR report files may be generated: RunReport.xml, SLA.xml
            string lrRunReportFile = Path.Combine(testRes.ReportLocation, "RunReport.xml");
            string lrSLAFile = Path.Combine(testRes.ReportLocation, "SLA.xml");

            LRRunGeneralInfo generalInfo = new LRRunGeneralInfo();
            List<LRRunSLAGoalResult> slaGoals = new List<LRRunSLAGoalResult>();

            try
            {
                XmlDocument xdoc = new XmlDocument();
                XmlElement slaNode = null;
                if (File.Exists(lrRunReportFile))
                {
                    xdoc.Load(lrRunReportFile);

                    // General node
                    var generalNode = xdoc.DocumentElement.SelectSingleNode("General");
                    if (generalNode != null)
                    {
                        var vUsersNode = generalNode.SelectSingleNode("VUsers") as XmlElement;
                        if (vUsersNode != null)
                        {
                            if (vUsersNode.HasAttribute("Count"))
                            {
                                int vUsersCount = 0;
                                if (int.TryParse(vUsersNode.Attributes["Count"].Value, out vUsersCount))
                                {
                                    generalInfo.VUsersCount = vUsersCount;
                                }
                            }
                        }
                    }

                    // SLA node
                    slaNode = xdoc.DocumentElement.SelectSingleNode("SLA") as XmlElement;
                }
                else if (File.Exists(lrSLAFile))
                {
                    xdoc.Load(lrSLAFile);
                    slaNode = xdoc.DocumentElement;
                }

                if (slaNode != null)
                {
                    var slaGoalNodes = slaNode.SelectNodes("SLA_GOAL");
                    if (slaGoalNodes != null)
                    {
                        foreach (var slaGoalNode in slaGoalNodes)
                        {
                            var slaGoalElement = slaGoalNode as XmlElement;
                            if (slaGoalElement != null)
                            {
                                slaGoals.Add(new LRRunSLAGoalResult
                                {
                                    TransactionName = slaGoalElement.GetAttribute("TransactionName"),
                                    Percentile = slaGoalElement.GetAttribute("Percentile"),
                                    FullName = slaGoalElement.GetAttribute("FullName"),
                                    Measurement = slaGoalElement.GetAttribute("Measurement"),
                                    GoalValue = slaGoalElement.GetAttribute("GoalValue"),
                                    ActualValue = slaGoalElement.GetAttribute("ActualValue"),
                                    Status = slaGoalElement.InnerText
                                });
                            }
                        }
                    }
                }
            }
            catch (XmlException)
            {

            }

            lrts.name = testRes.TestPath;

            // testsuite properties
            lrts.properties = new property[]
                {
                    new property{ name = "Total vUsers", value = IntToString(generalInfo.VUsersCount) }
                };

            double totalSeconds = testRes.Runtime.TotalSeconds;
            lrts.time = DoubleToString(totalSeconds);

            // testcases
            foreach (var slaGoal in slaGoals)
            {
                testcase tc = new testcase
                {
                    name = slaGoal.TransactionName,
                    classname = slaGoal.FullName + ": " + slaGoal.Percentile,
                    report = testRes.ReportLocation,
                    type = testRes.TestType,
                    time = DoubleToString(totalSeconds / slaGoals.Count)
                };

                switch (slaGoal.Status.Trim().ToLowerInvariant())
                {
                    case "failed":
                    case "fail":
                        tc.status = "fail";
                        tc.AddFailure(new failure
                        {
                            message = string.Format("The goal value '{0}' does not equal to the actual value '{1}'", slaGoal.GoalValue, slaGoal.ActualValue)
                        });
                        totalFailures++;
                        break;
                    case "error":
                    case "err":
                        tc.status = "error";
                        tc.AddError(new error
                        {
                            message = testRes.ErrorDesc
                        });
                        totalErrors++;
                        break;
                    case "warning":
                    case "warn":
                        tc.status = "warning";
                        break;
                    default:
                        tc.status = "pass";
                        break;
                }

                lrts.AddTestCase(tc);
                totalTests++;
            }

            lrts.tests = IntToString(totalTests);
            lrts.errors = IntToString(totalErrors);
            lrts.failures = IntToString(totalFailures);

            return lrts;
        }

        private testcase CreateXmlFromUFTRunResults(TestRunResults testRes)
        {
            string testcaseName = testRes.TestPath;
            if (TestNameOnly)
            {
                testcaseName = string.IsNullOrEmpty(testRes.TestName) ? new DirectoryInfo(testRes.TestPath).Name : testRes.TestName;
            }

            testcase tc = new testcase
            {
                systemout = testRes.ConsoleOut,
                systemerr = testRes.ConsoleErr,
                report = testRes.ReportLocation,
                classname = "All-Tests." + ((testRes.TestGroup == null) ? "" : testRes.TestGroup.Replace(".", "_")),
                name = testcaseName,
                type = testRes.TestType,
                time = DoubleToString(testRes.Runtime.TotalSeconds)
            };

            if (!string.IsNullOrWhiteSpace(testRes.FailureDesc))
                tc.AddFailure(new failure { message = testRes.FailureDesc });

            switch (testRes.TestState)
            {
                case TestState.Passed:
                    tc.status = "pass";
                    break;
                case TestState.Failed:
                    tc.status = "fail";
                    break;
                case TestState.Error:
                    tc.status = "error";
                    break;
                case TestState.Warning:
                    tc.status = "warning";
                    break;
                default:
                    tc.status = "pass";
                    break;
            }
            if (!string.IsNullOrWhiteSpace(testRes.ErrorDesc))
                tc.AddError(new error { message = testRes.ErrorDesc });
            return tc;
        }

        private string DoubleToString(double value)
        {
            return _culture == null ? value.ToString() : value.ToString(_culture);
        }

        private string IntToString(int value)
        {
            return _culture == null ? value.ToString() : value.ToString(_culture);
        }


        private class LRRunGeneralInfo
        {
            public int VUsersCount { get; set; }
        }

        private class LRRunSLAGoalResult
        {
            public string TransactionName { get; set; }
            public string Percentile { get; set; }
            public string FullName { get; set; }
            public string Measurement { get; set; }
            public string GoalValue { get; set; }
            public string ActualValue { get; set; }
            public string Status { get; set; }
        }

    }
}
