using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    internal class ReportNodeFactory : IReportNodeFactory
    {
        private const string NodeType_TestRun = "testrun";
        private const string NodeType_Iteration = "iteration";
        private const string NodeType_Action = "action";
        private const string NodeType_Context = "context";

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
                    if (parentNode != null && parentNode.type.ToLower() == NodeType_Action)
                    {
                        // create action iteration report
                        return new ActionIterationReport(node, owner) as T;
                    }
                    return new IterationReport(node, owner) as T;

                case NodeType_Action:
                    return new ActionReport(node, owner) as T;

                case NodeType_Context:
                    return new ContextReport(node, owner) as T;

                default:
                    return new StepReport(node, owner) as T;
            }
        }
    }
}
