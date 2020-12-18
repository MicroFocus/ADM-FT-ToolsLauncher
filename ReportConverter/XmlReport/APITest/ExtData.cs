using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
