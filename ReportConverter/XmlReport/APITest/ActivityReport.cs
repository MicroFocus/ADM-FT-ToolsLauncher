/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using System;
using System.IO;

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
