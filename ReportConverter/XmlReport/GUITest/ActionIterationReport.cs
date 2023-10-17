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
