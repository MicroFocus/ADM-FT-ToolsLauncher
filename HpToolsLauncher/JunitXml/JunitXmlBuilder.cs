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
        private const string DATETIME_PATTERN = "yyyy-MM-dd HH:mm:ss";
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
        public bool UnifiedTestClassname { get; set; }
        //public const string ClassName = "uftRunner";
        public const string ClassName = "FTToolsLauncher";
        public const string RootName = "uftRunnerRoot";

        XmlSerializer _serializer = new(typeof(testsuites));

        testsuites _testSuites = new();

        private static readonly char[] _slashes = ['/', '\\'];

        public JunitXmlBuilder()
        {
            _testSuites.name = RootName;
        }

        public testsuites TestSuites
        {
            get { return _testSuites; }
        }

        /// <summary>
        /// converts all data from the test results in to the Junit xml format and writes the xml file to disk.
        /// </summary>
        /// <param name="results"></param>
        public bool CreateXmlFromRunResults(TestSuiteRunResults results, out string error)
        {
            error = string.Empty;

            _testSuites = new testsuites();

            testsuite uftts = new()
            {
                errors = results.NumErrors,
                tests = results.NumTests,
                failures = results.NumFailures,
                skipped = results.NumSkipped,
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
                    //Console.WriteLine("CreateXmlFromRunResults, UFT test");
                    testcase ufttc = ConvertUFTRunResultsToTestcase(testRes);
                    uftts.AddTestCase(ufttc);
                }
            }
            if (uftts.testcase.Length > 0)
            {
                //Console.WriteLine("CreateXmlFromRunResults, add test case to test suite");
                _testSuites.AddTestsuite(uftts);
            }
            else
            {
                //Console.WriteLine("CreateXmlFromRunResults, no uft test case to write");
            }

            try
            {
                if (File.Exists(XmlName))
                {
                    //Console.WriteLine("CreateXmlFromRunResults, file exist - delete file");
                    File.Delete(XmlName);
                }
                // else
                //{
                //Console.WriteLine("CreateXmlFromRunResults, file does not exist");
                // }

                using (Stream s = File.OpenWrite(XmlName))
                {
                    //Console.WriteLine("CreateXmlFromRunResults, write test results to xml file");
                    //Console.WriteLine("_testSuites: " + _testSuites.name + " tests: " + _testSuites.tests);
                    //Console.WriteLine("_testSuites: " + _testSuites.ToString());
                    _serializer.Serialize(s, _testSuites);
                }

                return File.Exists(XmlName);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            //Console.WriteLine("CreateXmlFromRunResults, XmlName: " + XmlName);
            /*if (File.Exists(XmlName))
            {
                Console.WriteLine("CreateXmlFromRunResults, results file was created");
            } else
            {
                Console.WriteLine("CreateXmlFromRunResults, results file was not created");
            }*/
        }

        /// <summary>
        /// Create or update the xml report. This function is called in a loop after each test execution in order to get the report built progressively
        /// If the job is aborted by user we still can provide the (partial) report with completed tests results.
        /// </summary>
        /// <param name="ts">reference to testsuite object, existing or going to be added to _testSuites collection</param>
        /// <param name="testRes">test run results to be converted</param>
        /// <param name="addToTestSuites">flag to indicate if the first param (of type testsuite) must be added to the testsuites collection</param>
        public void CreateOrUpdatePartialXmlReport(testsuite ts, TestRunResults testRes, bool addToTestSuites)
        {
            try
            {
                testcase tc = ConvertUFTRunResultsToTestcase(testRes);
                ts.AddTestCase(tc);
                if (addToTestSuites)
                {
                    _testSuites.AddTestsuite(ts);
                }

                // NOTE: if the file already exists it will be overwritten / replaced, the entire _testSuites will be serialized every time
                using (Stream s = File.Open(_xmlName, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    _serializer.Serialize(s, _testSuites);
                }
            }
            catch(Exception ex)
            {
                ConsoleWriter.WriteErrLine(ex.ToString());
            }
        }

        private testsuite CreateXmlFromLRRunResults(TestRunResults testRes)
        {
            testsuite lrts = new();
            int totalTests = 0, totalFailures = 0, totalErrors = 0;

            // two LR report files may be generated: RunReport.xml, SLA.xml
            string lrRunReportFile = Path.Combine(testRes.ReportLocation, "RunReport.xml");
            string lrSLAFile = Path.Combine(testRes.ReportLocation, "SLA.xml");

            LRRunGeneralInfo generalInfo = new();
            List<LRRunSLAGoalResult> slaGoals = [];

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
            lrts.properties =
                [
                    new property{ name = "Total vUsers", value = IntToString(generalInfo.VUsersCount) }
                ];

            double totalSeconds = testRes.Runtime.TotalSeconds;
            lrts.time = DoubleToString(totalSeconds);

            // testcases
            foreach(var slaGoal in slaGoals)
            {
                testcase tc = new()
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
                            message =  testRes.ErrorDesc
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

            lrts.tests = totalTests;
            lrts.errors = totalErrors;
            lrts.failures = totalFailures;

            return lrts;
        }

        private testcase ConvertUFTRunResultsToTestcase(TestRunResults testRes)
        {
            string testcaseName = testRes.TestPath;
            if (TestNameOnly)
            {
                testcaseName = string.IsNullOrEmpty(testRes.TestName) ? new DirectoryInfo(testRes.TestPath).Name : testRes.TestName;
            }
            string classname;
            if (UnifiedTestClassname)
            {
                string fullPathParentFolder = Path.GetDirectoryName(testRes.TestPath.TrimEnd(_slashes));
                try
                {
                    classname = new Uri(fullPathParentFolder).AbsoluteUri;
                }
                catch
                {
                    classname = fullPathParentFolder;
                }
            }
            else
            {
                classname = "All-Tests." + ((testRes.TestGroup == null) ? string.Empty : testRes.TestGroup.Replace(".", "_"));
            }

            testcase tc = new testcase
            {
                systemout = testRes.ConsoleOut,
                systemerr = testRes.ConsoleErr,
                report = testRes.ReportLocation,
                classname = classname,
                name = testcaseName,
                type = testRes.TestType,
                time = DoubleToString(testRes.Runtime.TotalSeconds),
                startExecDateTime = testRes.StartDateTime.HasValue ? testRes.StartDateTime.Value.ToString(DATETIME_PATTERN) : string.Empty
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
                case TestState.NoRun:
                    tc.status = "skipped";
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
