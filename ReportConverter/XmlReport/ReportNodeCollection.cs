/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using System.Collections;
using System.Collections.Generic;

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
