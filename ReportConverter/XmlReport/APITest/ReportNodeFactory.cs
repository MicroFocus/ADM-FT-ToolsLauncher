using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.APITest
{
    internal class ReportNodeFactory : IReportNodeFactory
    {
        private const string NodeType_TestRun = "testrun";
        private const string NodeType_Iteration = "iteration";

        private static readonly ReportNodeFactory _instance = new ReportNodeFactory();

        private ReportNodeFactory()
        {
        }

        static ReportNodeFactory()
        {
        }

        public static ReportNodeFactory Instance { get { return _instance; } }

        T IReportNodeFactory.Create<T>(ReportNodeType node, ReportNodeType parentNode, IReportNodeOwner owner)
        {
            if (node == null)
            {
                return null;
            }

            string nodeType = node.type.ToLower();
            switch (nodeType)
            {
                case NodeType_TestRun:
                    return null;

                case NodeType_Iteration:
                    return new IterationReport(node, owner) as T;

                default:
                    return new ActivityReport(node, owner) as T;
            }
        }
    }
}
