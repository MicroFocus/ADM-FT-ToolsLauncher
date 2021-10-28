using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HpToolsLauncher
{
    public static class ConsoleWriter
    {
        static TestRunResults activeTestRun = null;
        static List<string> _errSummaryLines = new List<string>();

        public static void Initialize()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static bool EnableVerboseOutput { get; set; }

        /// <summary>
        /// lines to append to the summary at the end (used for files/dirs not found)
        /// </summary>
        public static List<string> ErrorSummaryLines
        {
            get { return _errSummaryLines; }
            set { _errSummaryLines = value; }
        }

        public static TestRunResults ActiveTestRun
        {
            get { return activeTestRun; }
            set { activeTestRun = value; }
        }

        public static void WriteException(string message, Exception ex)
        {
            Console.Out.WriteLine(message);
            Console.Out.WriteLine(ex.Message);
            Console.Out.WriteLine(ex.StackTrace);
            if (activeTestRun != null)
                activeTestRun.ConsoleErr += message + "\n" + ex.Message + "\n" + ex.StackTrace + "\n";
        }

        public static void WriteErrLine(string message)
        {
            Console.Error.WriteLine(message); // send errors to STDERR to allow custom handling on caller side (i.e. Azure plugin)

            if (activeTestRun != null)
            {
                activeTestRun.ConsoleErr += string.Format("Error: {0}\n", message);
            }
        }

        public static void WriteRawErrLine(string message)
        {
            WriteLine(message);

            if (activeTestRun != null)
            {
                activeTestRun.ConsoleErr += message + "\n";
            }
        }

        private static Regex _badXmlCharsReg = new Regex(@"[\u0000-\u0008]|[\u000B-\u000C]|[\u000E-\u001F]|[\uD800-\uDFFF]", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// filters out any bad characters that may affect xml generation/parsing
        /// </summary>
        /// <param name="subjectString"></param>
        /// <returns></returns>
        public static string FilterXmlProblematicChars(string subjectString)
        {
            //allowed chars: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            return _badXmlCharsReg.Replace(subjectString, "?");
        }

        public static void WriteLine(string[] messages)
        {
            foreach (string message in messages)
            {

                WriteLine(message);
                if (activeTestRun != null)
                {
                    activeTestRun.ConsoleOut += message + "\n";
                }
            }
        }

        public static void WriteLine(string message)
        {
            message = FilterXmlProblematicChars(message);
            //File.AppendAllText("c:\\stam11.stam", message);
            Console.WriteLine(message);
            if (activeTestRun != null)
                activeTestRun.ConsoleOut += message + "\n";
        }

        public static void WriteVerboseLine(string message)
        {
            if (EnableVerboseOutput && !string.IsNullOrWhiteSpace(message))
            {
                WriteLine("Debug: " + message);
            }
        }
    }
}
