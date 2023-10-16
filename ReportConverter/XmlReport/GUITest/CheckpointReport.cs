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

namespace ReportConverter.XmlReport.GUITest
{
    /// <summary>
    /// The <see cref="CheckpointReport"/> class provides the additional checkpoint related information
    /// on the top of the <see cref="StepReport"/>.
    /// </summary>
    public class CheckpointReport
    {
        private const string Checkpoint_NodeType = "checkpoint";

        /// <summary>
        /// Attempts to create a <see cref="CheckpointReport"/> instance which contains the checkpoint data
        /// from the specified StepReport.
        /// </summary>
        /// <param name="stepReport">The <see cref="StepReport"/> which may contain the checkpoint data.</param>
        /// <returns>A <see cref="CheckpointReport"/> instance if checkpoint data is found; otherwise, <c>null</c>.</returns>
        public static CheckpointReport FromStepReport(StepReport stepReport)
        {
            if (stepReport == null)
            {
                return null;
            }

            CheckpointReport cp = new CheckpointReport(stepReport);
            if (cp.TryParse())
            {
                return cp;
            }

            return null;
        }

        private CheckpointReport(StepReport stepReport)
        {
            StepReport = stepReport;
        }

        public StepReport StepReport { get; private set; }

        private bool TryParse()
        {
            // node type
            if (this.StepReport.Node.type.Trim().ToLower() != Checkpoint_NodeType)
            {
                // node is not a checkpoint at all
                return false;
            }

            Name = this.StepReport.Name;
            Status = this.StepReport.Status;

            // checkpoint data
            CheckpointExtType checkpoint = this.StepReport.Node.Data.Extension.Checkpoint;
            if (checkpoint != null)
            {
                CheckpointType = !string.IsNullOrWhiteSpace(checkpoint.Type) ? checkpoint.Type : string.Empty;
                CheckpointSubType = !string.IsNullOrWhiteSpace(checkpoint.CheckpointSubType) ? checkpoint.CheckpointSubType : string.Empty;

                if (this.StepReport.Status == ReportStatus.Failed)
                {
                    FailedDescription = DetermineFailedDescription(checkpoint);
                }
            }

            if (string.IsNullOrWhiteSpace(FailedDescription))
            {
                FailedDescription = this.StepReport.Description;
            }

            return true;
        }

        private static string DetermineFailedDescription(CheckpointExtType checkpoint)
        {
            string failedDescription = string.Empty;

            string trueVal = Properties.Resources.GUITest_Checkpoint_TrueValue;
            string falseVal = Properties.Resources.GUITest_Checkpoint_FalseValue;

            string type = !string.IsNullOrWhiteSpace(checkpoint.Type) ? checkpoint.Type.Trim().ToLower() : string.Empty;
            string subType = !string.IsNullOrWhiteSpace(checkpoint.CheckpointSubType) ? checkpoint.CheckpointSubType.Trim().ToLower() : string.Empty;
            switch (type)
            {
                case "bitmap checkpoint":
                    failedDescription = Properties.Resources.GUITest_BitmapCheckpoint_ExpectedImage + checkpoint.bmpChkPointFileExpected + "; \n";
                    failedDescription += Properties.Resources.GUITest_BitmapCheckpoint_ActualImage + checkpoint.bmpChkPointFileActual + "; \n";
                    failedDescription += Properties.Resources.GUITest_BitmapCheckpoint_DiffImage + checkpoint.bmpChkPointFileDifferent;
                    return failedDescription;

                case "accessibility checkpoint":
                    switch (subType)
                    {
                        case "altcheck":
                            failedDescription = Properties.Resources.GUITest_AccCheckpoint_ResultXml + checkpoint.resultxml + "; \n";
                            failedDescription += Properties.Resources.GUITest_AccCheckpoint_ResultXsl + checkpoint.resultxsl;
                            return failedDescription;
                    }
                    break;

                case "text checkpoint":
                    failedDescription = Properties.Resources.GUITest_TextCheckpoint_Captured + checkpoint.Captured + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_Expected + checkpoint.Expected + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_TextBefore + checkpoint.TextBefore + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_TextAfter + checkpoint.TextAfter + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_IsRegexp + (checkpoint.Regex ? trueVal : falseVal) + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_MatchCase + (checkpoint.MatchCase ? trueVal : falseVal) + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_ExactMatch + (checkpoint.ExactMatch ? trueVal : falseVal) + "; \n";
                    failedDescription += Properties.Resources.GUITest_TextCheckpoint_IgnoreSpaces + (checkpoint.IgnoreSpaces ? trueVal : falseVal);
                    return failedDescription;

                case "standard checkpoint":
                    switch (subType)
                    {
                        case "std image checkpoint":
                            foreach (CheckpointPropertyExtType property in checkpoint.Properties)
                            {
                                // sample: [Failed] prop1: Actual=v1; Expected=exp1; Regular expression=Yes; Use formula=No
                                failedDescription += "[";
                                if (property.ImageChkPointPropertyCheckPass)
                                {
                                    failedDescription += Properties.Resources.GUITest_Checkpoint_CheckPassed;
                                }
                                else
                                {
                                    failedDescription += Properties.Resources.GUITest_Checkpoint_CheckFailed;
                                }
                                failedDescription += string.Format("] {0}: ", property.ImageChkPointPropertyName);
                                failedDescription += string.Format("{0}={1}; ", Properties.Resources.GUITest_Checkpoint_ActualValue, property.ImageChkPointPropertyActualValue);
                                failedDescription += string.Format("{0}={1}; ", Properties.Resources.GUITest_Checkpoint_ExpectedValue, property.ImageChkPointPropertyExpectedValue);
                                failedDescription += string.Format("{0}={1}; ", Properties.Resources.GUITest_Checkpoint_IsRegexp, property.ImageChkPointPropertyIsRegExp ? trueVal : falseVal);
                                failedDescription += string.Format("{0}={1}; \n", Properties.Resources.GUITest_Checkpoint_UseFormula, property.ImageChkPointPropertyIsUseFormula ? trueVal : falseVal);
                            }
                            return failedDescription;

                        default:
                            if (checkpoint.Properties != null && checkpoint.Properties.Length > 0)
                            {
                                foreach (CheckpointPropertyExtType property in checkpoint.Properties)
                                {
                                    // sample: prop1: Actual=v1; Expected=exp1
                                    failedDescription += property.StdChkPointPropertyName + ": ";
                                    failedDescription += string.Format("{0}={1}; ", Properties.Resources.GUITest_Checkpoint_ActualValue, property.StdChkPointPropertyActualValue);
                                    failedDescription += string.Format("{0}={1}; \n", Properties.Resources.GUITest_Checkpoint_ExpectedValue, property.StdChkPointPropertyExpectedValue);
                                }
                                return failedDescription;
                            }
                            break;
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(checkpoint.ShortDescription))
            {
                failedDescription = checkpoint.ShortDescription;
            }
            return failedDescription;
        }

        public string Name { get; private set; }
        public string FailedDescription { get; private set; }
        public ReportStatus Status { get; private set; }

        public string CheckpointType { get; private set; }
        public string CheckpointSubType { get; private set; }
    }
}
