using ReportConverter.XmlReport;
using GUITestReport = ReportConverter.XmlReport.GUITest.TestReport;
using APITestReport = ReportConverter.XmlReport.APITest.TestReport;
using BPTReport = ReportConverter.XmlReport.BPT.TestReport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
