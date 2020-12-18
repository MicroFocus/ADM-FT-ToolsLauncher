using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.JUnit
{
    static class JUnitUtilities
    {
        public static string ToDateTimeISO8601String(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd'T'HH:mm:ssK");
        }
    }

    public partial class testsuiteProperty
    {
        public testsuiteProperty()
        {

        }

        public testsuiteProperty(string name, string value)
        {
            nameField = name;
            valueField = value;
        }
    }
}
