using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    internal class BCStepReportNodeFactory : IReportNodeFactory
    {
        private static readonly BCStepReportNodeFactory _instance = new BCStepReportNodeFactory();

        private BCStepReportNodeFactory()
        {
        }

        static BCStepReportNodeFactory()
        {
        }

        public static BCStepReportNodeFactory Instance { get { return _instance; } }

        T IReportNodeFactory.Create<T>(ReportNodeType node, ReportNodeType parentNode, IReportNodeOwner owner)
        {
            if (node == null || typeof(T) != typeof(BCStepReport))
            {
                return null;
            }

            return new BCStepReport(node, owner) as T;
        }
    }
}
