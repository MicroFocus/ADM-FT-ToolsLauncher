using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class ActionIterationReport : GeneralReportNode
    {
        public ActionIterationReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Contexts = new ReportNodeCollection<ContextReport>(this, ReportNodeFactory.Instance);
            Steps = new ReportNodeCollection<StepReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();

            OwnerAction = owner as ActionReport;
        }

        public ReportNodeCollection<ContextReport> Contexts { get; private set; }
        public ReportNodeCollection<StepReport> Steps { get; private set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public ActionReport OwnerAction { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // action iteration index
            Index = Node.Data.IndexSpecified ? Node.Data.Index : 0;

            // contexts and steps
            Contexts.Clear();
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

                    // try to add as a context report
                    ContextReport context = Contexts.TryParseAndAdd(node, this.Node);
                    if (context != null)
                    {
                        AllStepsEnumerator.Merge(context.AllStepsEnumerator);
                        continue;
                    }
                }

                // update duration for the last step since it is the end of the action iteration
                StepReport lastStep = ((TestReport)OwnerTest).LastStep;
                if (lastStep != null)
                {
                    TimeSpan ts = lastStep.StartTime - StartTime;
                    lastStep.UpdateDuration(DurationSeconds - (decimal)ts.TotalSeconds);
                }
            }

            return true;
        }

        public int Index { get; private set; }
    }
}
