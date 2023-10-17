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
    /// Represents an iteration over multiple report nodes and report node iterators.
    /// </summary>
    /// <typeparam name="T">The type of the report node which implements the <see cref="IReportNode"/> interface.</typeparam>
    public class ReportNodeEnumerator<T> : IEnumerator<T> where T : class, IReportNode
    {
        private List<EnumItem> _list;
        private int _index;

        /// <summary>
        /// Creates a new instance of an iteration over one or more enumerators.
        /// </summary>
        /// <param name="enumerators">One or more enumerators to be associated with the current enumerator.</param>
        public ReportNodeEnumerator()
        {
            _list = new List<EnumItem>();
            _index = -1;
        }

        public void Add(T item)
        {
            _list.Add(new EnumItem(item));
        }

        public void Add(IEnumerator<T> enumerator)
        {
            _list.Add(new EnumItem(enumerator));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items != null)
            {
                foreach (T item in items)
                {
                    _list.Add(new EnumItem(item));
                }
            }
        }

        public void AddRange(IEnumerable<IEnumerator<T>> enumerators)
        {
            if (enumerators != null)
            {
                foreach (IEnumerator<T> enumerator in enumerators)
                {
                    _list.Add(new EnumItem(enumerator));
                }
            }
        }

        /// <summary>
        /// Merges the specified <see cref="ReportNodeEnumerator{T}"/> instance <paramref name="src"/>
        /// to the current <see cref="ReportNodeEnumerator{T}"/> by appending all the enum items from the source.
        /// </summary>
        /// <param name="src">The source <see cref="ReportNodeEnumerator{T}"/> instance to be merged to this instance.</param>
        public void Merge(ReportNodeEnumerator<T> src)
        {
            if (src != null)
            {
                foreach (EnumItem item in src._list)
                {
                    _list.Add(item.Clone());
                }
            }
        }

        #region IEnumerator<T> interface
        public T Current
        {
            get
            {
                if (_index < 0 || _index >= _list.Count)
                {
                    return null;
                }

                EnumItem item = _list[_index];
                return item.IsSingleItem ? item.SingleItem : item.EnumeratorItem.Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public void Dispose()
        {
            foreach (EnumItem item in _list)
            {
                if (!item.IsSingleItem)
                {
                    item.EnumeratorItem.Dispose();
                }
            }
            _list.Clear();
            _index = -1;
        }

        public bool MoveNext()
        {
            if (_list.Count == 0 || _index >= _list.Count)
            {
                return false;
            }

            if (_index < 0)
            {
                _index = 0;
            }
            else
            {
                // if the current enum item is a single item, move next shall increase index by 1
                EnumItem currentItem = _list[_index];
                if (currentItem.IsSingleItem)
                {
                    _index++;
                }
            }

            while (_index < _list.Count)
            {
                EnumItem item = _list[_index];
                if (item.IsSingleItem)
                {
                    return true;
                }

                // try to call MoveNext on the current enumerator item
                if (item.EnumeratorItem.MoveNext())
                {
                    return true;
                }

                // if passed out of the current enumerator, try to use next enumerator
                _index++;
            }

            return false;
        }

        public void Reset()
        {
            foreach (EnumItem item in _list)
            {
                if (!item.IsSingleItem)
                {
                    item.EnumeratorItem.Reset();
                }
            }
            _index = -1;
        }
        #endregion

        private class EnumItem
        {
            private EnumItem()
            {
                IsSingleItem = false;
            }

            public EnumItem(T item)
            {
                IsSingleItem = true;
                SingleItem = item;
            }

            public EnumItem(IEnumerator<T> item)
            {
                IsSingleItem = false;
                EnumeratorItem = item;
            }

            public EnumItem Clone()
            {
                return new EnumItem
                {
                    IsSingleItem = this.IsSingleItem,
                    SingleItem = this.SingleItem,
                    EnumeratorItem = this.EnumeratorItem
                };
            }

            public bool IsSingleItem { get; private set; }
            public T SingleItem { get; private set; }
            public IEnumerator<T> EnumeratorItem { get; private set; }
        }
    }

    /// <summary>
    /// Represents a wrapper of an enumerable class which iterates over the report nodes via <see cref="ReportNodeEnumerator{T}"/> instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumerableReportNodes<T> : IEnumerable<T>, IEnumerator<T> where T : class, IReportNode
    {
        private ReportNodeEnumerator<T> _enumerator;

        public EnumerableReportNodes(ReportNodeEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        #region IEnumerable<T> interface
        public IEnumerator<T> GetEnumerator()
        {
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumerator;
        }
        #endregion

        #region IEnumerator<T> interface
        public T Current
        {
            get
            {
                return _enumerator.Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _enumerator.Current;
            }
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }
        #endregion
    }
}
