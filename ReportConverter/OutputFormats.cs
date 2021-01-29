using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    [Flags]
    enum OutputFormats
    {
        None = 0,
        JUnit = 1,
        NUnit3 = 2
    }
}
