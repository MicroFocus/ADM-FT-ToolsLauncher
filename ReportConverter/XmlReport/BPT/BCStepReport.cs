using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
