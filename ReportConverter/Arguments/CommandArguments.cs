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
            AllPositionalArgs = new List<string>();

            JUnitXmlFile = string.Empty;
            NUnit3XmlFile = string.Empty;
            ShowVersion = false;
            ShowHelp = false;
            InputPath = string.Empty;
        }

        #region Optional arguments
        [OptionalArg("junit", "j")]
        [OptionalArgValue("<file>")]
        [ArgDescription(ResourceName = "ArgDesc_JUnitFileOption")]
        public string JUnitXmlFile { get; set; }

        //[OptionalArg(new string[] { "nunit3", "nunit", "n" })]
        //[OptionalArgValue("<file>")]
        //[ArgDescription("The file path to save the converted report in NUnit 3 XML format.")]
        public string NUnit3XmlFile { get; set; }

        [OptionalArg(new string[] { "aggregate", "aggregation", "a" })]
        [ArgDescription(ResourceName = "ArgDesc_AggregationOption")]
        [ArgSample("ReportConverter -j \"output.xml\" --aggregate \"report1\" \"report2\"")]
        public bool Aggregation { get; set; }

        [OptionalArg("version", "V")]
        [ArgDescription(ResourceName = "ArgDesc_ShowVersionOption")]
        public bool ShowVersion { get; set; }

        [OptionalArg(new string[] { "help", "h", "?" })]
        [ArgDescription(ResourceName = "ArgDesc_ShowHelpOption")]
        public bool ShowHelp { get; set; }
        #endregion

        #region Positional arguments
        [PositionalArg(1, "<directory> [...]")]
        [ArgDescription(ResourceName = "ArgDesc_InputFile")]
        [ArgSample("ReportConverter -j \"output.xml\" --aggregate \"report1\" \"report2\"")]
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

        public IList<string> AllPositionalArgs { get; private set; }
    }
}
