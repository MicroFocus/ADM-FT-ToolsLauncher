using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HpToolsLauncher
{
    public interface IXmlBuilder
    {
        string XmlName { get; set; }
        CultureInfo Culture { get; set; }
        bool TestNameOnly { get; set; }
        bool CreateXmlFromRunResults(TestSuiteRunResults results, out string error);
    }
}
