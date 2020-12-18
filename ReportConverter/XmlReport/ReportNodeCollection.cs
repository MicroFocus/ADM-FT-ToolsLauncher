using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport
{
    /// <summary>
    /// Represents a collection of a set of report nodes with the same type.
    /// </summary>
    /// <typeparam name="T">The type of the report node instance which implements the <see cref="IReportNode"/> interface stored in the collection.</typeparam>
    public class ReportNodeCollection<T> : IEnumerable<T>, IEnumerator<T> where T : class, IReportNode
    {
        private List<T> _list;

        /// <summary>
        /// Creates a new instance with the specified owner for a set of report nodes.
        /// </summary>
        /// <param name="owner">The owner of all the report nodes stored in the current <see cref="ReportNodeCollection{T}"/> instance.</param>
        /// <param name="factory">The factory instance which is used to create new report node instance.</param>
        public ReportNodeCollection(IReportNodeOwner owner, IReportNodeFactory factory)
        {
            Owner = owner;
            Factory = factory;

            _list = new List<T>();
        }

        /// <summary>
        /// Gets the owner of the current <see cref="ReportNodeCollection{T}"/> instance.
        /// </summary>
        public IReportNodeOwner Owner { get; private set; }

        /// <summary>
        /// Gets the factory to create the new report node instance.
        /// </summary>
        public IReportNodeFactory Factory { get; private set; }

        /// <summary>
        /// Attempts to parse the specified <see cref="ReportNodeType"/> and add to the collection if successfully parsed.
        /// </summary>
        /// <param name="node">The <see cref="ReportNodeType"/> instance contains the report node data read from the XML report.</param>
        /// <param name="parentNode">The parent <see cref="ReportNodeType"/> instance contains the report node data read from the XML report.</param>
        /// <returns>The newly created report node instance from the <see cref="ReportNodeType"/> instance.</returns>
        public T TryParseAndAdd(ReportNodeType node, ReportNodeType parentNode)
        {
            if (node == null || Factory == null)
            {
                return null;
            }

            // create report node instance via factory
            T reportNode = Factory.Create<T>(node, parentNode, Owner);
            if (reportNode == null)
            {
                return null;
            }

            // try to parse the report node data
            if (!reportNode.TryParse())
            {
                return null;
            }

            _list.Add(reportNode);
            return reportNode;
        }

        /// <summary>
        /// Removes all the report node instances stored in the collection.
        /// </summary>
        public void Clear()
        {
            _list.Clear();
        }

        public int Length { get { return _list.Count; } }

        #region IEnumerable Interface
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion

        #region IEnumerator Interface
        public T Current
        {
            get
            {
                return GetEnumerator().Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return GetEnumerator().Current;
            }
        }

        public void Dispose()
        {
            GetEnumerator().Dispose();
        }

        public bool MoveNext()
        {
            return GetEnumerator().MoveNext();
        }

        public void Reset()
        {
            GetEnumerator().Reset();
        }
        #endregion
    }
}
