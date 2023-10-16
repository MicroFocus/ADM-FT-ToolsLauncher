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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ReportConverter.XmlReport
{
    static class XmlReportUtilities
    {
        public static DateTime ToDateTime(string dateTime, string timeZone, DateTime def = default(DateTime))
        {
            string tz = timeZone; // might be in format "-08:00:00", to be parsed, remain first two parts
            string[] tzComps = timeZone.Split(':');
            if (tzComps != null && tzComps.Length > 2)
            {
                tz = string.Format("{0}:{1}", tzComps[0], tzComps[1]);
            }

            string fullStr = string.Format("{0} {1}", dateTime, tz).Trim();

            DateTime dt = def;
            if (!DateTime.TryParse(fullStr, out dt))
            {
                dt = def;
            }
            return dt;
        }

        public static T LoadXmlFileBySchemaType<T>(string file) where T : class
        {
            // try to load XmlReport via XML schema
            try
            {
                if (!File.Exists(file))
                {
                    return null;
                }

                T root;
                XmlSerializer xs = new XmlSerializer(typeof(T));
                using (Stream s = File.OpenRead(file))
                {
                    root = xs.Deserialize(s) as T;
                }
                return root;
            }
            catch (Exception ex)
            {
                OutputWriter.WriteLine("ERR: " + ex.Message);
                return null;
            }
        }
    }

    public partial class ReportNodeType
    {
        [System.Xml.Serialization.XmlIgnore()]
        public ReportStatus Status
        {
            get
            {
                switch (Data.Result.ToLower())
                {
                    case "failed":
                        return ReportStatus.Failed;
                    case "warning":
                        return ReportStatus.Warning;
                    case "information":
                        return ReportStatus.Information;
                    case "passed":
                        return ReportStatus.Passed;
                    case "done":
                        return ReportStatus.Done;
                    default:
                        return ReportStatus.Unknown;
                }
            }
        }
    }

    public partial class ParameterType
    {
        public string NameAndType
        {
            get
            {
                return string.Format("{0} ({1})", nameField, typeField);
            }
        }
    }

    public partial class ExtensionType
    {
        public string BottomFilePath { get; set; }
        public string HtmlBottomFilePath { get; set; }
        public MergedSTCheckpointDataExtType MergedSTCheckpointData { get; set; }
        public TestObjectExtType TestObject { get; set; }
        public CheckpointExtType Checkpoint { get; set; }
        public SmartIdentificationInfoExtType SmartIdentificationInfo { get; set; }
        public string NodeType { get; set; }
    }

    #region Extension - TestObject
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class TestObjectExtType
    {
        public string Repository { get; set; }

        [System.Xml.Serialization.XmlArrayItemAttribute("Object", IsNullable = false)]
        public TestObjectPathObjectExtType[] Path { get; set; }

        public string Operation { get; set; }
        public string OperationData { get; set; }

        [System.Xml.Serialization.XmlArrayItemAttribute("Property", IsNullable = false)]
        public TestObjectPropertyExtType[] Properties { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class TestObjectPathObjectExtType
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class TestObjectPropertyExtType
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    #endregion

    #region Extension - MergedSTCheckpointData
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class MergedSTCheckpointDataExtType
    {
        public string BottomFilePath { get; set; }
        public string HtmlBottomFilePath { get; set; }
    }
    #endregion

    #region Extension - Checkpoint
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class CheckpointExtType
    {
        public string Type { get; set; }
        public string CheckpointSubType { get; set; }
        public string ShortDescription { get; set; }

        public double MaxTimeout { get; set; }
        public double UsedTimeout { get; set; }

        /// <summary>
        /// Bitmap CheckPoint
        /// </summary>
        public string bmpChkPointFileExpected { get; set; }
        /// <summary>
        /// Bitmap CheckPoint
        /// </summary>
        public string bmpChkPointFileActual { get; set; }
        /// <summary>
        /// Bitmap CheckPoint
        /// </summary>
        public string bmpChkPointFileDifferent { get; set; }

        /// <summary>
        /// Accessibility Checkpoint (AltCheck)
        /// </summary>
        public string resultxml { get; set; }
        /// <summary>
        /// Accessibility Checkpoint (AltCheck)
        /// </summary>
        public string resultxsl { get; set; }

        /// <summary>
        /// Text Checkpoint
        /// </summary>
        public string Captured { get; set; }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        public string Expected { get; set; }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        public string TextBefore { get; set; }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        public string TextAfter { get; set; }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool Regex { get; set; }
        [System.Xml.Serialization.XmlElement("Regex")]
        public string RegexField
        {
            get
            {
                return Regex ? "True" : "False";
            }
            set
            {
                if (value == "True")
                    Regex = true;
                else if (value == "False")
                    Regex = false;
                else
                    Regex = XmlConvert.ToBoolean(value);
            }
        }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool MatchCase { get; set; }
        [System.Xml.Serialization.XmlElement("MatchCase")]
        public string MatchCaseField
        {
            get
            {
                return MatchCase ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    MatchCase = true;
                else if (string.Compare(value, "False", true) == 0)
                    MatchCase = false;
                else
                    MatchCase = XmlConvert.ToBoolean(value);
            }
        }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool ExactMatch { get; set; }
        [System.Xml.Serialization.XmlElement("ExactMatch")]
        public string ExactMatchField
        {
            get
            {
                return ExactMatch ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    ExactMatch = true;
                else if (string.Compare(value, "False", true) == 0)
                    ExactMatch = false;
                else
                    ExactMatch = XmlConvert.ToBoolean(value);
            }
        }
        /// <summary>
        /// Text Checkpoint
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool IgnoreSpaces { get; set; }
        [System.Xml.Serialization.XmlElement("IgnoreSpaces")]
        public string IgnoreSpacesField
        {
            get
            {
                return IgnoreSpaces ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    IgnoreSpaces = true;
                else if (string.Compare(value, "False", true) == 0)
                    IgnoreSpaces = false;
                else
                    IgnoreSpaces = XmlConvert.ToBoolean(value);
            }
        }

        [System.Xml.Serialization.XmlArrayItemAttribute("Property", IsNullable = false)]
        public CheckpointPropertyExtType[] Properties { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class CheckpointPropertyExtType
    {
        // Standard Checkpoint
        public string StdChkPointPropertyName { get; set; }
        public string StdChkPointPropertyActualValue { get; set; }
        public string StdChkPointPropertyExpectedValue { get; set; }

        // Standard Checkpoint (Std Image CheckPoint)
        public string ImageChkPointPropertyName { get; set; }
        public string ImageChkPointPropertyActualValue { get; set; }
        public string ImageChkPointPropertyExpectedValue { get; set; }
        [System.Xml.Serialization.XmlIgnore]
        public bool ImageChkPointPropertyCheckPass { get; set; }
        [System.Xml.Serialization.XmlElement("ImageChkPointPropertyCheckPass")]
        public string ImageChkPointPropertyCheckPassField
        {
            get
            {
                return ImageChkPointPropertyCheckPass ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    ImageChkPointPropertyCheckPass = true;
                else if (string.Compare(value, "False", true) == 0)
                    ImageChkPointPropertyCheckPass = false;
                else
                    ImageChkPointPropertyCheckPass = XmlConvert.ToBoolean(value);
            }
        }
        [System.Xml.Serialization.XmlIgnore]
        public bool ImageChkPointPropertyIsRegExp { get; set; }
        [System.Xml.Serialization.XmlElement("ImageChkPointPropertyIsRegExp")]
        public string ImageChkPointPropertyIsRegExpField
        {
            get
            {
                return ImageChkPointPropertyIsRegExp ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    ImageChkPointPropertyIsRegExp = true;
                else if (string.Compare(value, "False", true) == 0)
                    ImageChkPointPropertyIsRegExp = false;
                else
                    ImageChkPointPropertyIsRegExp = XmlConvert.ToBoolean(value);
            }
        }
        [System.Xml.Serialization.XmlIgnore]
        public bool ImageChkPointPropertyIsUseFormula { get; set; }
        [System.Xml.Serialization.XmlElement("ImageChkPointPropertyIsUseFormula")]
        public string ImageChkPointPropertyIsUseFormulaField
        {
            get
            {
                return ImageChkPointPropertyIsUseFormula ? "True" : "False";
            }
            set
            {
                if (string.Compare(value, "True", true) == 0)
                    ImageChkPointPropertyIsUseFormula = true;
                else if (string.Compare(value, "False", true) == 0)
                    ImageChkPointPropertyIsUseFormula = false;
                else
                    ImageChkPointPropertyIsUseFormula = XmlConvert.ToBoolean(value);
            }
        }
    }
    #endregion

    #region Extension - Smart Identification
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SmartIdentificationInfoExtType
    {
        public SIDBasicPropertiesExtType SIDBasicProperties { get; set; }

        [System.Xml.Serialization.XmlArrayItemAttribute("Property", IsNullable = false)]
        public SIDOptionalPropertyExtType[] SIDOptionalProperties { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SIDBasicPropertiesExtType
    {
        [System.Xml.Serialization.XmlElement("Property", IsNullable = false)]
        public SIDBasicPropertyExtType[] Properties { get; set; }

        public int BasicMatch { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SIDBasicPropertyExtType
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SIDOptionalPropertyExtType
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Info { get; set; }
        public int Matches { get; set; }
    }
    #endregion
}
