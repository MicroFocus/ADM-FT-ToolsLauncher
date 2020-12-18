using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            catch
            {
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
    }

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

    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class MergedSTCheckpointDataExtType
    {
        public string BottomFilePath { get; set; }
        public string HtmlBottomFilePath { get; set; }
    }
}
