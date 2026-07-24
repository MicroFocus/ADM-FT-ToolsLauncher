/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2026 Open Text
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using HpToolsLauncher.Common;
using HpToolsLauncher.Properties;
using HpToolsLauncher.TestRunners;

namespace HpToolsLauncher
{
    public class MtbxManager
    {
        //the xml format of an mtbx file below:
        /*
         <Mtbx>
            <Test Name="test1" path="${workspace}\test1">
                <Parameter Name="mee" Value="12" Type="Integer"/>
                <Parameter Name="mee1" Value="12.0" Type="Double"/>
                <Parameter Name="mee2" Value="abc" Type="String"/>
                <DataTable path="c:\tables\my_data_table.xls"/>
                <Iterations mode="rngIterations|rngAll|oneIteration" start="2" end="3"/>
            </Test>
            <Test Name="test2" path="${workspace}\test2">
                <Parameter Name="mee" Value="12" Type="Integer"/>
                <Parameter Name="mee1" Value="12.0" Type="Double"/>
                <Parameter Name="mee2" Value="abc" Type="String"/>
            </Test>
         </Mtbx>
        */

        private const string HTL_MTBXSCHEMA_XSD = "HpToolsLauncher.Properties.MtbxSchema.xsd";
        private const string TEST = "Test";
        private const string PATH = "path";
        private const string REPORT_PATH = "reportPath";
        private const string REPORT_EXACT_PATH = "reportExactPath";
        private const string NAME = "name";
        private const string UNNAMED_TEST = "Unnamed Test";
        private const string PARAMETER = "Parameter";
        private const string VALUE = "value";
        private const string TYPE = "type";
        private const string STRING = "string";
        private const string DATA_TABLE = "DataTable";
        private const string ITERATIONS = "Iterations";
        private const string MODE = "mode";
        private const string START = "start";
        private const string END = "end";

        public static List<TestInfo> LoadMtbx(string mtbxContent, string testGroup)
        {
            return LoadMtbx(mtbxContent, null, testGroup);
        }

        private static XAttribute GetAttribute(XElement x, XName attributeName)
        {
            return x.Attributes().FirstOrDefault(a => a.Name.Namespace == attributeName.Namespace && a.Name.LocalName.EqualsIgnoreCase(attributeName.LocalName));
        }

        private static XElement GetElement(XElement x, XName eName)
        {
            return x.Elements().FirstOrDefault(a => a.Name.Namespace == eName.Namespace && a.Name.LocalName.EqualsIgnoreCase(eName.LocalName));
        }

        private static IEnumerable<XElement> GetElements(XElement x, XName eName)
        {
            return x.Elements().Where(a => a.Name.Namespace == eName.Namespace && a.Name.LocalName.EqualsIgnoreCase(eName.LocalName));
        }

        public static List<TestInfo> Parse(string mtbxFileName, Dictionary<string, string> jenkinsEnvironmentVars, string testGroupName)
        {
            string xmlContent;
            try
            {
                xmlContent = File.ReadAllText(mtbxFileName);
                if (xmlContent.IsNullOrWhiteSpace())
                {
                    string err = string.Format(Resources.EmptyFileProvided, mtbxFileName);
                    ConsoleWriter.WriteLine($"Error: {err}");
                    ConsoleWriter.ErrorSummaryLines.Add(err);
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                    return null;
                }
            }
            catch(Exception ex)
            {
                ConsoleWriter.WriteException("Mtbx file read error", ex);
                return null;
            }

            return LoadMtbx(xmlContent, jenkinsEnvironmentVars, testGroupName);
        }
        private static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            StringBuilder sb = new();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        public static List<TestInfo> LoadMtbx(string xmlContent, Dictionary<string, string> jankinsEnvironmentVars, string testGroupName)
        {
            var localEnv = Environment.GetEnvironmentVariables();

            foreach (string varName in localEnv.Keys)
            {
                string value = (string)localEnv[varName];
                xmlContent = ReplaceString(xmlContent, $"%{varName}%", value);
                xmlContent = ReplaceString(xmlContent, $"${{{varName}}}", value);
            }

            if (jankinsEnvironmentVars != null)
            {
                foreach (string varName in jankinsEnvironmentVars.Keys)
                {
                    string value = jankinsEnvironmentVars[varName];
                    xmlContent = ReplaceString(xmlContent, $"%{varName}%", value);
                    xmlContent = ReplaceString(xmlContent, $"${{{varName}}}", value);
                }
            }

            List<TestInfo> retval = [];
            XDocument doc = XDocument.Parse(xmlContent);

            XmlSchemaSet schemas = new();
            var assembly = Assembly.GetExecutingAssembly();
            var schemaStream = assembly.GetManifestResourceStream(HTL_MTBXSCHEMA_XSD);
            XmlSchema schema = XmlSchema.Read(schemaStream, null);
            schemas.Add(schema);

            string validationMessages = string.Empty;
            doc.Validate(schemas, (o, e) =>
            {
                validationMessages += $"{e.Message}{Environment.NewLine}";
                ConsoleWriter.ErrorSummaryLines.Add(e.Message);
            });

            if (!validationMessages.IsNullOrWhiteSpace())
            {
                ConsoleWriter.WriteLine($"mtbx schema validation errors: {validationMessages}");
            }
            try
            {
                var root = doc.Root;
                foreach (var test in GetElements(root, TEST))
                {
                    string path = GetAttribute(test, PATH).Value;
                    if (path.IsNullOrWhiteSpace())
                    {
                        string err = string.Format(Resources.EmptyPathAttributeValue, path);
                        ConsoleWriter.WriteLine($"Error: {err}");
                        ConsoleWriter.ErrorSummaryLines.Add(err);
                        Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                        continue;
                    }
                    if (!Directory.Exists(path))
                    {
                        string err = string.Format(Resources.GeneralFileNotFound, path);
                        ConsoleWriter.WriteLine($"Error: {err}");
                        ConsoleWriter.ErrorSummaryLines.Add(err);
                        Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                        continue;
                    }

                    // optional report path attribute (report base directory, for backward compatibility)
                    XAttribute xReportBasePath = GetAttribute(test, REPORT_PATH);
                    string reportBasePath = null;
                    if (xReportBasePath != null)
                    {
                        reportBasePath = xReportBasePath.Value;
                    }

                    // optional report directory path attribute (exact report path)
                    XAttribute xReportExactPath = GetAttribute(test, REPORT_EXACT_PATH);
                    string reportExactPath = null;
                    if (xReportExactPath != null)
                    {
                        reportExactPath = xReportExactPath.Value;
                    }

                    XAttribute xname = GetAttribute(test, NAME);
                    string name = xname?.Value.IsNullOrWhiteSpace() == true ? UNNAMED_TEST : xname.Value;
                    TestInfo col = new(path, name, testGroupName)
                    {
                        ReportBaseDirectory = reportBasePath,
                        ReportPath = reportExactPath
                    };

                    HashSet<string> paramNames = [];

                    foreach (var param in GetElements(test, PARAMETER))
                    {
                        string pname = GetAttribute(param, NAME).Value;
                        string pval = GetAttribute(param, VALUE).Value;
                        XAttribute xptype = GetAttribute(param, TYPE);
                        string ptype = STRING;

                        if (xptype != null)
                            ptype = xptype.Value;

                        TestParameterInfo testParam = new() { Name = pname, Type = ptype, Value = pval };
                        if (!paramNames.Contains(testParam.Name))
                        {
                            paramNames.Add(testParam.Name);
                            col.Params.Add(testParam);
                        }
                        else
                        {
                            string line = string.Format(Resources.GeneralDuplicateParameterWarning, pname, path);
                            ConsoleWriter.WriteLine(line);
                        }
                    }

                    XElement dataTable = GetElement(test, DATA_TABLE);
                    if (dataTable != null)
                    {
                        col.DataTablePath = GetAttribute(dataTable, PATH).Value;
                    }

                    XElement iterations = GetElement(test, ITERATIONS);
                    if (iterations != null)
                    {
                        IterationInfo ii = new();
                        XAttribute modeAttr = GetAttribute(iterations, MODE);
                        if (modeAttr != null)
                        {
                            ii.IterationMode = modeAttr.Value;
                        }
                        XAttribute startAttr = GetAttribute(iterations, START);
                        if (startAttr != null)
                        {
                            ii.StartIteration = startAttr.Value;
                        }
                        XAttribute endAttr = GetAttribute(iterations, END);
                        if (endAttr != null)
                        {
                            ii.EndIteration = endAttr.Value;
                        }

                        col.IterationInfo = ii;
                    }

                    retval.Add(col);
                }
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteException("Problem while parsing Mtbx file", ex);
            }
            return retval;
        }
    }
}
