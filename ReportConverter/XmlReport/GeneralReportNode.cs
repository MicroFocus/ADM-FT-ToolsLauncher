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
using System.Collections.Generic;

namespace ReportConverter.XmlReport
{
    public class GeneralReportNode : IReportNode, IReportNodeOwner
    {
        public GeneralReportNode(ReportNodeType node, IReportNodeOwner owner)
        {
            Node = node;
            Owner = owner;
            OwnerTest = Owner != null ? Owner.OwnerTest : null;
        }

        public IReportNodeOwner Owner { get; protected set; }
        public TestReportBase OwnerTest { get; protected set; }
        public ReportNodeType Node { get; protected set; }

        public virtual bool TryParse()
        {
            if (Node == null)
            {
                return false;
            }

            Name = Node.Data.Name;
            Description = Node.Data.Description;
            Status = Node.Status;
            StartTime = XmlReportUtilities.ToDateTime(Node.Data.StartTime, OwnerTest.TimeZone);
            DurationSeconds = Node.Data.DurationSpecified ? Node.Data.Duration : 0;

            InputParameters = Node.Data.InputParameters;
            if (InputParameters == null)
            {
                InputParameters = new ParameterType[0];
            }

            OutputParameters = Node.Data.OutputParameters;
            if (OutputParameters == null)
            {
                OutputParameters = new ParameterType[0];
            }

            AUTs = Node.Data.TestedApplications;
            if (AUTs == null)
            {
                AUTs = new TestedApplicationType[0];
            }

            if (Status == ReportStatus.Failed)
            {
                ErrorText = Node.Data.ErrorText;
                if (string.IsNullOrWhiteSpace(Node.Data.ErrorText))
                {
                    ErrorText = Description;
                }
                ErrorCode = Node.Data.ExitCodeSpecified ? Node.Data.ExitCode : 0;
            }

            return true;
        }

        public virtual void UpdateDuration(decimal seconds)
        {
            DurationSeconds = seconds;
        }

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public ReportStatus Status { get; protected set; }
        public DateTime StartTime { get; protected set; }
        public decimal DurationSeconds { get; protected set; }
        public IEnumerable<ParameterType> InputParameters { get; protected set; }
        public IEnumerable<ParameterType> OutputParameters { get; protected set; }
        public IEnumerable<TestedApplicationType> AUTs { get; protected set; }
        public string ErrorText { get; protected set; }
        public int ErrorCode { get; protected set; }
    }
}
