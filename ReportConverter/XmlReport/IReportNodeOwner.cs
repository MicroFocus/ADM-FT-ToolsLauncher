using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport
{
    /// <summary>
    /// Represents the owner of a report node.
    /// </summary>
    public interface IReportNodeOwner
    {
        /// <summary>
        /// Gets the owner of the current report node.
        /// </summary>
        IReportNodeOwner Owner { get; }
        /// <summary>
        /// Gets the associated owner test report instance which derived from the <see cref="TestReportBase"/> class.
        /// </summary>
        TestReportBase OwnerTest { get; }
    }
}
