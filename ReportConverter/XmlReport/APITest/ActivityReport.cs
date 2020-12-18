using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.APITest
{
    public class ActivityReport : GeneralReportNode
    {
        public ActivityReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            SubActivities = new ReportNodeCollection<ActivityReport>(this, ReportNodeFactory.Instance);

            AllActivitiesEnumerator = new ReportNodeEnumerator<ActivityReport>();
        }

        public ReportNodeCollection<ActivityReport> SubActivities { get; private set; }

        public ReportNodeEnumerator<ActivityReport> AllActivitiesEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // update the duration for the last step
            TestReport ownerTest = (TestReport)OwnerTest;
            ActivityReport lastActivity = ownerTest.LastActivity;
            if (lastActivity != null)
            {
                TimeSpan ts = StartTime - lastActivity.StartTime;
                lastActivity.DurationSeconds = (decimal)ts.TotalSeconds;
            }
            ownerTest.LastActivity = this;

            // activity extended data
            string parentDir = Path.GetDirectoryName(ownerTest.ReportFile);
            if (!string.IsNullOrWhiteSpace(Node.Data.Extension.BottomFilePath))
            {
                string bottomFilePath = Path.Combine(parentDir, Node.Data.Extension.BottomFilePath);
                ActivityExtensionData = XmlReportUtilities.LoadXmlFileBySchemaType<ActivityExtData>(bottomFilePath);
            }

            // activity checkpoint extended data
            if (Node.Data.Extension.MergedSTCheckpointData != null &&
                !string.IsNullOrWhiteSpace(Node.Data.Extension.MergedSTCheckpointData.BottomFilePath))
            {
                string bottomFilePath = Path.Combine(parentDir, Node.Data.Extension.MergedSTCheckpointData.BottomFilePath);
                CheckpointData = XmlReportUtilities.LoadXmlFileBySchemaType<CheckpointExtData>(bottomFilePath);
            }

            // sub activities
            SubActivities.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // sub-activities
                    ActivityReport subActivity = SubActivities.TryParseAndAdd(node, this.Node);
                    if (subActivity != null)
                    {
                        AllActivitiesEnumerator.Add(subActivity);
                        AllActivitiesEnumerator.Merge(subActivity.AllActivitiesEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }

        public ActivityExtData ActivityExtensionData { get; private set; }
        public CheckpointExtData CheckpointData { get; private set; }
    }
}
