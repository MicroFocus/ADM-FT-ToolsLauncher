using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
