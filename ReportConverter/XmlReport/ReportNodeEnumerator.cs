using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
