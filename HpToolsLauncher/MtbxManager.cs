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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
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
        public static List<TestInfo> LoadMtbx(string mtbxContent, string testGroup)
        {
            return LoadMtbx(mtbxContent, null, testGroup);
        }

        private static XAttribute GetAttribute(XElement x, XName attributeName)
        {
            return x.Attributes().FirstOrDefault(a => a.Name.Namespace == attributeName.Namespace
            && string.Equals(a.Name.LocalName, attributeName.LocalName, StringComparison.OrdinalIgnoreCase));
        }

        private static XElement GetElement(XElement x, XName eName)
        {
            return x.Elements().FirstOrDefault(a => a.Name.Namespace == eName.Namespace
             && string.Equals(a.Name.LocalName, eName.LocalName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<XElement> GetElements(XElement x, XName eName)
        {
            return x.Elements().Where(a => a.Name.Namespace == eName.Namespace
             && string.Equals(a.Name.LocalName, eName.LocalName, StringComparison.OrdinalIgnoreCase));
        }

        public static List<TestInfo> Parse(string mtbxFileName, Dictionary<string, string> jenkinsEnvironmentVars, string testGroupName)
        {
            string xmlContent;
            try
            {
                xmlContent = File.ReadAllText(mtbxFileName);
                if (string.IsNullOrWhiteSpace(xmlContent))
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
            StringBuilder sb = new StringBuilder();

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
                xmlContent = ReplaceString(xmlContent, "%" + varName + "%", value);
                xmlContent = ReplaceString(xmlContent, "${" + varName + "}", value);
            }

            if (jankinsEnvironmentVars != null)
            {
                foreach (string varName in jankinsEnvironmentVars.Keys)
                {
                    string value = jankinsEnvironmentVars[varName];
                    xmlContent = ReplaceString(xmlContent, "%" + varName + "%", value);
                    xmlContent = ReplaceString(xmlContent, "${" + varName + "}", value);
                }
            }

            List<TestInfo> retval = new List<TestInfo>();
            XDocument doc = XDocument.Parse(xmlContent);

            XmlSchemaSet schemas = new XmlSchemaSet();

            var assembly = Assembly.GetExecutingAssembly();

            var schemaStream = assembly.GetManifestResourceStream("HpToolsLauncher.MtbxSchema.xsd");

            XmlSchema schema = XmlSchema.Read(schemaStream, null);

            schemas.Add(schema);

            string validationMessages = string.Empty;
            doc.Validate(schemas, (o, e) =>
            {
                validationMessages += e.Message + Environment.NewLine;
                ConsoleWriter.ErrorSummaryLines.Add(e.Message);
            });

            if (!string.IsNullOrWhiteSpace(validationMessages))
            {
                ConsoleWriter.WriteLine("mtbx schema validation errors: " + validationMessages);
            }
            try
            {
                var root = doc.Root;
                foreach (var test in GetElements(root, "Test"))
                {
                    string path = GetAttribute(test, "path").Value;
                    if (string.IsNullOrWhiteSpace(path))
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
                    XAttribute xReportBasePath = GetAttribute(test, "reportPath");
                    string reportBasePath = null;
                    if (xReportBasePath != null)
                    {
                        reportBasePath = xReportBasePath.Value;
                    }

                    // optional report directory path attribute (exact report path)
                    XAttribute xReportExactPath = GetAttribute(test, "reportExactPath");
                    string reportExactPath = null;
                    if (xReportExactPath != null)
                    {
                        reportExactPath = xReportExactPath.Value;
                    }

                    XAttribute xname = GetAttribute(test, "name");
                    string name = string.IsNullOrWhiteSpace(xname?.Value) ? "Unnamed Test" : xname.Value;
                    TestInfo col = new TestInfo(path, name, testGroupName)
                    {
                        ReportBaseDirectory = reportBasePath,
                        ReportPath = reportExactPath
                    };

                    HashSet<string> paramNames = new HashSet<string>();

                    foreach (var param in GetElements(test, "Parameter"))
                    {
                        string pname = GetAttribute(param, "name").Value;
                        string pval = GetAttribute(param, "value").Value;
                        XAttribute xptype = GetAttribute(param, "type");
                        string ptype = "string";

                        if (xptype != null)
                            ptype = xptype.Value;

                        var testParam = new TestParameterInfo() { Name = pname, Type = ptype, Value = pval };
                        if (!paramNames.Contains(testParam.Name))
                        {
                            paramNames.Add(testParam.Name);
                            col.ParameterList.Add(testParam);
                        }
                        else
                        {
                            string line = string.Format(Resources.GeneralDuplicateParameterWarning, pname, path);
                            ConsoleWriter.WriteLine(line);
                        }
                    }

                    XElement dataTable = GetElement(test, "DataTable");
                    if (dataTable != null)
                    {
                        col.DataTablePath = GetAttribute(dataTable, "path").Value;
                    }

                    XElement iterations = GetElement(test, "Iterations");
                    if (iterations != null)
                    {
                        IterationInfo ii = new IterationInfo();
                        XAttribute modeAttr = GetAttribute(iterations, "mode");
                        if (modeAttr != null)
                        {
                            ii.IterationMode = modeAttr.Value;
                        }
                        XAttribute startAttr = GetAttribute(iterations, "start");
                        if (startAttr != null)
                        {
                            ii.StartIteration = startAttr.Value;
                        }
                        XAttribute endAttr = GetAttribute(iterations, "end");
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
