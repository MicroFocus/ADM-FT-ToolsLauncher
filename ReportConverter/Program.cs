using ReportConverter.XmlReport;
using GUITestReport = ReportConverter.XmlReport.GUITest.TestReport;
using APITestReport = ReportConverter.XmlReport.APITest.TestReport;
using BPTReport = ReportConverter.XmlReport.BPT.TestReport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    class Program
    {
        private const string XMLReport_File = "run_results.xml";
        private const string XMLReport_SubDir_Report = "Report";

        static void Main(string[] args)
        {
            CommandArguments arguments = ArgHelper.Instance.ParseCommandArguments(args);

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

            OutputWriter.WriteTitle();
            Convert(arguments);
        }

        static void Convert(CommandArguments args)
        {
            // input - Xml report
            TestReportBase testReport = ReadInput(args);
            if (testReport == null)
            {
                ProgramExit.Exit(ExitCode.CannotReadFile);
                return;
            }

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

                // the output JUnit file path must be NOT same as the input file
                FileInfo fiInput = new FileInfo(Path.Combine(args.InputPath, XMLReport_File));
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

            // output - nunit 3
            if ((args.OutputFormats & OutputFormats.NUnit3) == OutputFormats.NUnit3)
            {
            }
        }

        static TestReportBase ReadInput(CommandArguments args)
        {
            if (string.IsNullOrWhiteSpace(args.InputPath))
            {
                ProgramExit.Exit(ExitCode.MissingArgument);
                return null;
            }

            string xmlReportFile = args.InputPath;
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
                        OutputWriter.WriteLine(Properties.Resources.ErrMsg_CannotFindXmlReportFile);
                        ProgramExit.Exit(ExitCode.FileNotFound);
                        return null;
                    }
                }
            }

            // load XML from file with the specified XML schema
            ResultsType root = XmlReportUtilities.LoadXmlFileBySchemaType<ResultsType>(xmlReportFile);
            if (root == null)
            {
                ProgramExit.Exit(ExitCode.CannotReadFile);
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

            OutputWriter.WriteLine(Properties.Resources.ErrMsg_Input_InvalidFirstReportNode);
            ProgramExit.Exit(ExitCode.InvalidInput);
            return null;
        }
    }
}
