using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class ContextReport : GeneralReportNode
    {
        public ContextReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            SubContexts = new ReportNodeCollection<ContextReport>(this, ReportNodeFactory.Instance);
            Steps = new ReportNodeCollection<StepReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();
        }

        public ReportNodeCollection<ContextReport> SubContexts { get; private set; }
        public ReportNodeCollection<StepReport> Steps { get; private set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // sub-contexts and steps
            SubContexts.Clear();
            Steps.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // try to add as a step report
                    StepReport step = Steps.TryParseAndAdd(node, this.Node);
                    if (step != null)
                    {
                        AllStepsEnumerator.Add(step);
                        AllStepsEnumerator.Merge(step.AllStepsEnumerator);
                        continue;
                    }

                    // try to add as a sub context report
                    ContextReport subContext = SubContexts.TryParseAndAdd(node, this.Node);
                    if (subContext != null)
                    {
                        AllStepsEnumerator.Merge(subContext.AllStepsEnumerator);
                        continue;
                    }
                }

                // update duration for the last step since it is the end of the context
                StepReport lastStep = ((TestReport)OwnerTest).LastStep;
                if (lastStep != null)
                {
                    TimeSpan ts = lastStep.StartTime - StartTime;
                    lastStep.UpdateDuration(DurationSeconds - (decimal)ts.TotalSeconds);
                }
            }

            return true;
        }
    }
}
