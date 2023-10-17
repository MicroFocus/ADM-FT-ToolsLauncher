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
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ReportConverter.JUnit
{
    static class Converter
    {
        public static bool ConvertAndSave(CommandArguments args, TestReportBase input)
        {
            ConverterBase conv = null;

            // try to test if the input report is a GUI/API/BPT test report
            GUITestReport guiReport = input as GUITestReport;
            APITestReport apiReport = input as APITestReport;
            BPTReport bptReport = input as BPTReport;

            if (guiReport != null)
            {
                conv = new GUITestReportConverter(args, guiReport);
            }
            else if (apiReport != null)
            {
                conv = new APITestReportConverter(args, apiReport);
            }
            else if (bptReport != null)
            {
                conv = new BPTReportConverter(args, bptReport);
            }
            else
            {
                return false;
            }

            if (!conv.Convert())
            {
                return false;
            }

            if (!conv.SaveFile())
            {
                return false;
            }

            return true;
        }

        public static bool ConvertAndSaveAggregation(CommandArguments args, IEnumerable<TestReportBase> input)
        {
            AggregativeReportConverter converter = new AggregativeReportConverter(args, input);

            if (!converter.Convert())
            {
                return false;
            }

            if (!converter.SaveFile())
            {
                return false;
            }

            return true;
        }
    }

    abstract class ConverterBase
    {
        public ConverterBase(CommandArguments args)
        {
            Arguments = args;
        }

        public CommandArguments Arguments { get; private set; }

        public abstract bool Convert();

        public abstract bool SaveFile();

        protected virtual bool SaveFileInternal(object value)
        {
            try
            {
                if (File.Exists(Arguments.JUnitXmlFile))
                {
                    File.Delete(Arguments.JUnitXmlFile);
                }

                XmlSerializer xs = new XmlSerializer(value.GetType());
                using (FileStream fs = File.OpenWrite(Arguments.JUnitXmlFile))
                {
                    xs.Serialize(fs, value);
                }

                return true;
            }
            catch (Exception ex)
            {
                OutputWriter.WriteLine(Properties.Resources.ErrMsg_Prefix + ex.Message);
                return false;
            }
        }
    }
}
