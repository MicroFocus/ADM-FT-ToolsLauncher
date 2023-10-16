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
using System.Linq;

namespace ReportConverter.XmlReport.BPT
{
    public class BCStepReport : GeneralReportNode
    {
        private const string NodeType_Context = "context";

        public BCStepReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            SubBCSteps = new ReportNodeCollection<BCStepReport>(this, BCStepReportNodeFactory.Instance);

            AllBCStepsEnumerator = new ReportNodeEnumerator<BCStepReport>();

            OwnerBusinessComponent = owner as BusinessComponentReport;
        }

        public ReportNodeCollection<BCStepReport> SubBCSteps { get; private set; }

        public ReportNodeEnumerator<BCStepReport> AllBCStepsEnumerator { get; private set; }

        public BusinessComponentReport OwnerBusinessComponent { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // context?
            IsContext = Node.type.ToLower() == NodeType_Context;

            // update the duration for the last business component step
            if (OwnerBusinessComponent != null && !IsContext)
            {
                BCStepReport lastStep = OwnerBusinessComponent.LastBCStep;
                if (lastStep != null)
                {
                    TimeSpan ts = StartTime - lastStep.StartTime;
                    lastStep.DurationSeconds = (decimal)ts.TotalSeconds;
                }
                OwnerBusinessComponent.LastBCStep = this;
            }

            // test object path, operation and operation data
            TestObjectExtType testObj = Node.Data.Extension.TestObject;
            if (testObj != null)
            {
                TestObjectOperation = testObj.Operation;

                TestObjectOperationData = testObj.OperationData;
                if (!string.IsNullOrWhiteSpace(TestObjectOperationData) && Node.Status != ReportStatus.Failed)
                {
                    Name += " " + testObj.OperationData;
                }

                TestObjectPathObjects = testObj.Path;
                if (TestObjectPathObjects != null && TestObjectPathObjects.Count() > 0)
                {
                    TestObjectPath = string.Empty;
                    foreach (TestObjectPathObjectExtType pathObj in TestObjectPathObjects)
                    {
                        // sample of pathObjStr: Window("Notepad")
                        string pathObjStr = string.Empty;
                        if (!string.IsNullOrWhiteSpace(pathObj.Type))
                        {
                            pathObjStr = pathObj.Type;
                        }
                        if (!string.IsNullOrWhiteSpace(pathObj.Name))
                        {
                            if (string.IsNullOrWhiteSpace(pathObjStr))
                            {
                                pathObjStr = pathObj.Name;
                            }
                            else
                            {
                                pathObjStr += string.Format(" (\"{0}\")", pathObj.Name);
                            }
                        }
                        // sample of TestObjectPath: Window("Notepad").WinMenu("Menu")
                        if (!string.IsNullOrWhiteSpace(pathObjStr))
                        {
                            if (!string.IsNullOrWhiteSpace(TestObjectPath))
                            {
                                TestObjectPath += ".";
                            }
                            TestObjectPath += pathObjStr;
                        }
                    }
                }
            }

            // sub-steps
            SubBCSteps.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // sub-steps
                    BCStepReport subStep = SubBCSteps.TryParseAndAdd(node, this.Node);
                    if (subStep != null)
                    {
                        AllBCStepsEnumerator.Add(subStep);
                        AllBCStepsEnumerator.Merge(subStep.AllBCStepsEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }

        public bool IsContext { get; private set; }

        public IEnumerable<TestObjectPathObjectExtType> TestObjectPathObjects { get; private set; }
        public string TestObjectPath { get; private set; }
        public string TestObjectOperation { get; private set; }
        public string TestObjectOperationData { get; private set; }
    }
}
