using System;
using System.Collections.Generic;
using System.Linq;

namespace HpToolsLauncher.Common
{
    internal static class PathSorter
    {
        private static readonly char[] BackSlash = ['\\'];
        
        public static List<string> SortPaths(List<TestSetItem> items, string orderByCriteria)
        {
            Node root = new Node(string.Empty);

            foreach (TestSetItem ts in items)
            {
                // Path looks like: "Folder1\Folder2\TestSetName"
                string[] segments = ts.Path.Split(BackSlash, StringSplitOptions.RemoveEmptyEntries);
                root.AddPath(segments, 0, ts);
            }

            List<string> result = new List<string>();
            root.SortAndFlatten(result, string.Empty, orderByCriteria);
            return result;
        }

        private sealed class Node
        {
            private readonly string _name;
            private readonly Dictionary<string, Node> _children;
            private readonly List<TestSetItem> _leaves;

            public string Name
            {
                get { return _name; }
            }

            public Node(string name)
            {
                _name = name;
                _children = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
                _leaves = new List<TestSetItem>();
            }

            public void AddPath(string[] segments, int index, TestSetItem item)
            {
                // last segment represents the test set itself
                if (index >= segments.Length - 1)
                {
                    _leaves.Add(item);
                    return;
                }

                string nextSegment = segments[index];
                Node child;

                if (!_children.TryGetValue(nextSegment, out child))
                {
                    child = new Node(nextSegment);
                    _children.Add(nextSegment, child);
                }
                child.AddPath(segments, index + 1, item);
            }

            public void SortAndFlatten(List<string> result, string currentPath, string orderByCriteria)
            {
                // 1) Sort subfolders alphabetically
                List<Node> orderedChildren = _children.Values.OrderBy(n => n.Name).ToList();

                foreach (Node child in orderedChildren)
                {
                    string childPath = string.IsNullOrEmpty(currentPath) ? child.Name : currentPath + "\\" + child.Name;
                    child.SortAndFlatten(result, childPath, orderByCriteria);
                }

                // 2) Sort test sets (leaves)
                IEnumerable<TestSetItem> sortedLeaves;

                if (string.Equals(orderByCriteria, "id", StringComparison.OrdinalIgnoreCase))
                    sortedLeaves = _leaves.OrderBy(l => l.ID);
                else
                    sortedLeaves = _leaves.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase);
                foreach (TestSetItem leaf in sortedLeaves)
                    result.Add(string.IsNullOrEmpty(currentPath) ? leaf.Name : currentPath + "\\" + leaf.Name);
            }
        }
    }
}
