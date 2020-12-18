using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class ActionReport : GeneralReportNode
    {
        public ActionReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            ActionIterations = new ReportNodeCollection<ActionIterationReport>(this, ReportNodeFactory.Instance);
            Contexts = new ReportNodeCollection<ContextReport>(this, ReportNodeFactory.Instance);
            Steps = new ReportNodeCollection<StepReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();

            OwnerIteration = Owner as IterationReport;
        }

        public ReportNodeCollection<ActionIterationReport> ActionIterations { get; private set; }
        public ReportNodeCollection<ContextReport> Contexts { get; private set; }
        public ReportNodeCollection<StepReport> Steps { get; private set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public IterationReport OwnerIteration { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

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

                    // try to add as an action iteration
                    ActionIterationReport actionIteration = ActionIterations.TryParseAndAdd(node, this.Node);
                    if (actionIteration != null)
                    {
                        AllStepsEnumerator.Merge(actionIteration.AllStepsEnumerator);
                        continue;
                    }
                }

                // update duration for the last step since it is the end of the action
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
