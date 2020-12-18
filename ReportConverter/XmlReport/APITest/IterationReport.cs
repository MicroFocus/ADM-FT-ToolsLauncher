using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.APITest
{
    public class IterationReport : GeneralReportNode
    {
        public IterationReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Activities = new ReportNodeCollection<ActivityReport>(this, ReportNodeFactory.Instance);

            AllActivitiesEnumerator = new ReportNodeEnumerator<ActivityReport>();
        }

        public ReportNodeCollection<ActivityReport> Activities { get; private set; }

        public ReportNodeEnumerator<ActivityReport> AllActivitiesEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // activities
            Activities.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // try to add as an activity report
                    ActivityReport activity = Activities.TryParseAndAdd(node, this.Node);
                    if (activity != null)
                    {
                        AllActivitiesEnumerator.Add(activity);
                        AllActivitiesEnumerator.Merge(activity.AllActivitiesEnumerator);
                        continue;
                    }
                }

                // update duration for the last activity since it is the end of the iteration
                ActivityReport lastActivity = ((TestReport)OwnerTest).LastActivity;
                if (lastActivity != null)
                {
                    TimeSpan ts = lastActivity.StartTime - StartTime;
                    lastActivity.UpdateDuration(DurationSeconds - (decimal)ts.TotalSeconds);
                }
            }

            return true;
        }
    }
}
