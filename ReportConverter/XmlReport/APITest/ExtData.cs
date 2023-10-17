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

namespace ReportConverter.XmlReport.APITest
{
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("report", Namespace = "", IsNullable = false)]
    public partial class ActivityExtData : ExtData
    {
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("report", Namespace = "", IsNullable = false)]
    public partial class CheckpointExtData
    {
        [System.Xml.Serialization.XmlElement(ElementName = "Checkpoint")]
        public ExtData[] Checkpoints { get; set; }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class ExtData
    {
        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Type")]
        public ExtDataItem VTDType { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Name")]
        public ExtDataItem VTDName { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Message")]
        public ExtDataItem VTDMessage { get; set; }


        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Status")]
        public ExtDataItem VTDStatus { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Details")]
        public ExtDataItem VTDDetails { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Operation")]
        public ExtDataItem VTDOperation { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Expected")]
        public ExtDataItem VTDExpected { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Actual")]
        public ExtDataItem VTDActual { get; set; }

        [System.Xml.Serialization.XmlElement(ElementName = "VTD_Xpath")]
        public ExtDataItem VTDXPath { get; set; }

        public ExtDataItem Name { get; set; }

        public ExtDataItem Comment { get; set; }

        [System.Xml.Serialization.XmlTextAttribute()]
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlNode[] Any { get; set; }

        public VTDStatus KnownVTDStatus
        {
            get
            {
                if (this.VTDStatus == null)
                {
                    return APITest.VTDStatus.Unknown;
                }
                string status = this.VTDStatus.Value.ToLower();
                string code = this.VTDStatus.code.ToLower();
                if (status == "failure" || code == "failure")
                {
                    return APITest.VTDStatus.Failure;
                }
                else if (status == "success" || code == "success")
                {
                    return APITest.VTDStatus.Success;
                }
                else if (status == "done")
                {
                    return APITest.VTDStatus.Done;
                }
                else
                {
                    return APITest.VTDStatus.Unknown;
                }
            }
        }
    }

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class ExtDataItem
    {
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string code { get; set; }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
    }
}
