using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    internal class ReportNodeFactory : IReportNodeFactory
    {
        private const string NodeType_BPTestRun = "Business Process";
        private const string NodeType_Iteration = "iteration";
        private const string NodeType_Group = "group";
        private const string NodeType_Flow = "flow";
        private const string NodeType_BranchCase = "case";
        private const string NodeType_BC = "business component";
        private const string NodeType_Step = "step";
        private const string NodeType_Recovery = "recovery";

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
                case NodeType_BPTestRun:
                    return null;

                case NodeType_Iteration:
                    return new IterationReport(node, owner) as T;

                case NodeType_Group:
                    return new GroupReport(node, owner) as T;

                case NodeType_Flow:
                    return new FlowReport(node, owner) as T;

                case NodeType_BranchCase:
                    return new BranchReport(node, owner) as T;

                case NodeType_BC:
                    return new BusinessComponentReport(node, owner) as T;

                case NodeType_Step:
                    if (node.Data.Extension.NodeType != null && node.Data.Extension.NodeType.Trim().ToLower() == NodeType_Recovery)
                    {
                        return new RecoveryStepReport(node, owner) as T;
                    }
                    else
                    {
                        return new GeneralStepReport(node, owner) as T;
                    }

                default:
                    return null;
            }
        }
    }
}
