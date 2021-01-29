using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class StepReport : GeneralReportNode
    {
        public StepReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Contexts = new ReportNodeCollection<ContextReport>(this, ReportNodeFactory.Instance);
            SubSteps = new ReportNodeCollection<StepReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();
        }

        public ReportNodeCollection<ContextReport> Contexts { get; private set; }
        public ReportNodeCollection<StepReport> SubSteps { get; private set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // update the duration for the last step
            TestReport ownerTest = (TestReport)OwnerTest;
            StepReport lastStep = ownerTest.LastStep;
            if (lastStep != null)
            {
                TimeSpan ts = StartTime - lastStep.StartTime;
                lastStep.DurationSeconds = (decimal)ts.TotalSeconds;
            }
            ownerTest.LastStep = this;

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

            // smart identification
            SmartIdentification = Node.Data.Extension.SmartIdentificationInfo;

            // contexts and sub-steps
            SubSteps.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    // sub-steps
                    StepReport subStep = SubSteps.TryParseAndAdd(node, this.Node);
                    if (subStep != null)
                    {
                        AllStepsEnumerator.Add(subStep);
                        AllStepsEnumerator.Merge(subStep.AllStepsEnumerator);
                        continue;
                    }

                    // contexts
                    ContextReport context = Contexts.TryParseAndAdd(node, this.Node);
                    if (context != null)
                    {
                        AllStepsEnumerator.Merge(context.AllStepsEnumerator);
                        continue;
                    }
                }
            }

            return true;
        }

        public IEnumerable<TestObjectPathObjectExtType> TestObjectPathObjects { get; private set; }
        public string TestObjectPath { get; private set; }
        public string TestObjectOperation { get; private set; }
        public string TestObjectOperationData { get; private set; }
        public SmartIdentificationInfoExtType SmartIdentification { get; private set; }
    }
}
