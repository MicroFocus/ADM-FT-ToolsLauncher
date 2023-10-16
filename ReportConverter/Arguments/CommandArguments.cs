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

using System.Collections.Generic;

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
