using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    class CommandArguments
    {
        public CommandArguments()
        {
            JUnitXmlFile = string.Empty;
            NUnit3XmlFile = string.Empty;
            ShowVersion = false;
            ShowHelp = false;
            InputPath = string.Empty;
        }

        #region Optional arguments
        [OptionalArg("junit", "j")]
        [OptionalArgValue("<file>")]
        [ArgDescription("The file path to save the converted report in JUnit XML format.")]
        public string JUnitXmlFile { get; set; }

        [OptionalArg(new string[] { "nunit3", "nunit", "n" })]
        [OptionalArgValue("<file>")]
        [ArgDescription("The file path to save the converted report in NUnit 3 XML format.")]
        public string NUnit3XmlFile { get; set; }

        [OptionalArg("version", "V")]
        [ArgDescription("Show program version.")]
        public bool ShowVersion { get; set; }

        [OptionalArg(new string[] { "help", "h", "?" })]
        [ArgDescription("Show program help.")]
        public bool ShowHelp { get; set; }
        #endregion

        #region Positional arguments
        [PositionalArg(1, "<directory>")]
        [ArgDescription("The path to a directory where the raw XML report can be found.")]
        public string InputPath { get; set; }
        #endregion

        public OutputFormats OutputFormats
        {
            get
            {
                OutputFormats of = OutputFormats.None;
                
                if (!string.IsNullOrWhiteSpace(JUnitXmlFile))
                {
                    of |= OutputFormats.JUnit;
                }

                if (!string.IsNullOrWhiteSpace(NUnit3XmlFile))
                {
                    of |= OutputFormats.NUnit3;
                }

                return of;
            }
        }
    }
}
