using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport
{
    /// <summary>
    /// Represents a factory that creates the new report node instance.
    /// </summary>
    public interface IReportNodeFactory
    {
        /// <summary>
        /// Creates a new instance of type <typeparamref name="T"/> from the specified
        /// <see cref="ReportNodeType"/> instance read from the XML report.
        /// </summary>
        /// <typeparam name="T">The type of the instance that implements the <see cref="IReportNode"/> interface.</typeparam>
        /// <param name="node">The <see cref="ReportNodeType"/> instance read from the XML report.</param>
        /// <param name="parentNode">The parent <see cref="ReportNodeType"/> instance read from the XML report.</param>
        /// <param name="owner">The owner of the newly created report node instance.</param>
        /// <returns>
        /// The new report node instance of type <typeparamref name="T"/> which implements the <see cref="IReportNode"/> interface
        /// or <see cref="null"/> if failed to create the instance from the <see cref="ReportNodeType"/> instance.
        /// </returns>
        T Create<T>(ReportNodeType node, ReportNodeType parentNode, IReportNodeOwner owner) where T : class, IReportNode;
    }
}
