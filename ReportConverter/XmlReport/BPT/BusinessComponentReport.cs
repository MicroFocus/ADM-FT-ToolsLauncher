using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.BPT
{
    public class BusinessComponentReport : GeneralReportNode
    {
        public BusinessComponentReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            BCSteps = new ReportNodeCollection<BCStepReport>(this, BCStepReportNodeFactory.Instance);

            AllBCStepsEnumerator = new ReportNodeEnumerator<BCStepReport>();
        }

        public ReportNodeCollection<BCStepReport> BCSteps { get; private set; }

        public ReportNodeEnumerator<BCStepReport> AllBCStepsEnumerator { get; private set; }

        internal BCStepReport LastBCStep { get; set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // update the duration for the last business component
            TestReport ownerTest = (TestReport)OwnerTest;
            BusinessComponentReport lastBC = ownerTest.LastBusinessComponent;
            if (lastBC != null)
            {
                TimeSpan ts = StartTime - lastBC.StartTime;
                lastBC.DurationSeconds = (decimal)ts.TotalSeconds;
            }
            ownerTest.LastBusinessComponent = this;

            // steps
            BCSteps.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // sub-steps
                    BCStepReport step = BCSteps.TryParseAndAdd(node, this.Node);
                    if (step != null)
                    {
                        AllBCStepsEnumerator.Add(step);
                        AllBCStepsEnumerator.Merge(step.AllBCStepsEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }
    }
}
