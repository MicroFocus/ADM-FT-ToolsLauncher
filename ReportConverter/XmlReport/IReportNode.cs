using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport
{
    /// <summary>
    /// Represents a report node that reads from the XML report.
    /// </summary>
    public interface IReportNode
    {
        /// <summary>
        /// Gets the <see cref="ReportNodeType"/> instance read from the XML report.
        /// </summary>
        ReportNodeType Node { get; }

        /// <summary>
        /// Attempts to parse the <see cref="Node"/>.
        /// </summary>
        /// <returns><c>true</c> if the <see cref="Node"/> is parsed successfully; otherwise, <c>false</c>.</returns>
        bool TryParse();
    }
}
