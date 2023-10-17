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
using GUITestReport = ReportConverter.XmlReport.GUITest.TestReport;
using APITestReport = ReportConverter.XmlReport.APITest.TestReport;
using BPTReport = ReportConverter.XmlReport.BPT.TestReport;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportConverter
{
    class Program
    {
        private const string XMLReport_File = "run_results.xml";
        private const string XMLReport_SubDir_Report = "Report";

        static void Main(string[] args)
        {
            string[] parseErrors;
            CommandArguments arguments = ArgHelper.Instance.ParseCommandArguments(args, out parseErrors);

            bool isTitlePrinted = false;
            // errors when parsing arguments
            if (parseErrors != null && parseErrors.Length > 0)
            {
                OutputWriter.WriteTitle();
                OutputWriter.WriteLines(parseErrors);
                isTitlePrinted = true;
            }

            // show help?
            if (args.Length == 0 || arguments.ShowHelp)
            {
                OutputWriter.WriteTitle();
                OutputWriter.WriteCommandUsage();
                ProgramExit.Exit(ExitCode.Success);
                return;
            }

            // show version?
            if (arguments.ShowVersion)
            {
                OutputWriter.WriteVersion();
                ProgramExit.Exit(ExitCode.Success);
                return;
            }

            if (!isTitlePrinted)
            {
                OutputWriter.WriteTitle();
            }
            Convert(arguments);
        }

        static void Convert(CommandArguments args)
        {
            // input
            IEnumerable<TestReportBase> testReports = ReadInput(args);

            // output - none
            if (args.OutputFormats == OutputFormats.None)
            {
                ProgramExit.Exit(ExitCode.UnknownOutputFormat, true);
                return;
            }

            // output - junit
            if ((args.OutputFormats & OutputFormats.JUnit) == OutputFormats.JUnit)
            {
                // the output JUnit path must be NOT an exist directory
                if (Directory.Exists(args.JUnitXmlFile))
                {
                    OutputWriter.WriteLine(Properties.Resources.ErrMsg_JUnit_OutputCannotDir);
                    ProgramExit.Exit(ExitCode.InvalidArgument);
                    return;
                }

                // if not an aggregation report output, then only convert for the first report
                if (!args.Aggregation)
                {
                    TestReportBase testReport = testReports.First();
                    if (testReport == null)
                    {
                        ProgramExit.Exit(ExitCode.CannotReadFile);
                        return;
                    }

                    // the output JUnit file path must be NOT same as the input file
                    FileInfo fiInput = new FileInfo(testReport.ReportFile);
                    FileInfo fiOutput = new FileInfo(args.JUnitXmlFile);
                    if (fiInput.FullName == fiOutput.FullName)
                    {
                        OutputWriter.WriteLine(Properties.Resources.ErrMsg_JUnit_OutputSameAsInput);
                        ProgramExit.Exit(ExitCode.InvalidArgument);
                        return;
                    }

                    // convert
                    if (!JUnit.Converter.ConvertAndSave(args, testReport))
                    {
                        ProgramExit.Exit(ExitCode.GeneralError);
                    }
                    else
                    {
                        OutputWriter.WriteLine(Properties.Resources.InfoMsg_JUnit_OutputGenerated, fiOutput.FullName);
                    }
                }
                else
                {
                    // an aggregation report output
                    if (!JUnit.Converter.ConvertAndSaveAggregation(args, testReports))
                    {
                        ProgramExit.Exit(ExitCode.GeneralError);
                    }
                    else
                    {
                        FileInfo fiOutput = new FileInfo(args.JUnitXmlFile);
                        OutputWriter.WriteLine(Properties.Resources.InfoMsg_JUnit_OutputGenerated, fiOutput.FullName);
                    }
                }
            }

            // output - nunit 3
            if ((args.OutputFormats & OutputFormats.NUnit3) == OutputFormats.NUnit3)
            {
            }
        }

        static IEnumerable<TestReportBase> ReadInput(CommandArguments args)
        {
            if (string.IsNullOrWhiteSpace(args.InputPath))
            {
                ProgramExit.Exit(ExitCode.InvalidInput, true);
                yield break;
            }

            ExitCode.ExitCodeData errorCode = ExitCode.Success;
            bool anyFailures = false;
            foreach (string path in args.AllPositionalArgs)
            {
                TestReportBase testReport = ReadInputInternal(path, ref errorCode);
                if (testReport != null)
                {
                    yield return testReport;
                }
                else
                {
                    anyFailures = true;
                }
            }

            if (anyFailures && args.AllPositionalArgs.Count == 1)
            {
                // only one test report and it failed to read, exit
                ProgramExit.Exit(errorCode);
                yield break;
            }
        }

        static TestReportBase ReadInputInternal(string path, ref ExitCode.ExitCodeData errorCode)
        {
            string xmlReportFile = path;
            if (!File.Exists(xmlReportFile))
            {
                // the input path could be a folder, try to detect it
                string dir = xmlReportFile;
                xmlReportFile = Path.Combine(dir, XMLReport_File);
                if (!File.Exists(xmlReportFile))
                {
                    // still not find, may be under "Report" sub folder? try it
                    xmlReportFile = Path.Combine(dir, XMLReport_SubDir_Report, XMLReport_File);
                    if (!File.Exists(xmlReportFile))
                    {
                        OutputWriter.WriteLine(Properties.Resources.ErrMsg_CannotFindXmlReportFile + " " + path);
                        errorCode = ExitCode.FileNotFound;
                        return null;
                    }
                }
            }

            // load XML from file with the specified XML schema
            ResultsType root = XmlReportUtilities.LoadXmlFileBySchemaType<ResultsType>(xmlReportFile);
            if (root == null)
            {
                errorCode = ExitCode.CannotReadFile;
                return null;
            }

            // try to load the XML data as a GUI test report
            GUITestReport guiReport = new GUITestReport(root, xmlReportFile);
            if (guiReport.TryParse())
            {
                return guiReport;
            }

            // try to load as API test report
            APITestReport apiReport = new APITestReport(root, xmlReportFile);
            if (apiReport.TryParse())
            {
                return apiReport;
            }

            // try to load as BP test report
            BPTReport bptReport = new BPTReport(root, xmlReportFile);
            if (bptReport.TryParse())
            {
                return bptReport;
            }

            OutputWriter.WriteLine(Properties.Resources.ErrMsg_Input_InvalidFirstReportNode + " " + path);
            errorCode = ExitCode.InvalidInput;
            return null;
        }
    }
}
