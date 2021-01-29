using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter.XmlReport.GUITest
{
    public class IterationReport : GeneralReportNode
    {
        public IterationReport(ReportNodeType node, IReportNodeOwner owner) : base(node, owner)
        {
            Actions = new ReportNodeCollection<ActionReport>(this, ReportNodeFactory.Instance);

            AllStepsEnumerator = new ReportNodeEnumerator<StepReport>();
        }

        public ReportNodeCollection<ActionReport> Actions { get; private set; }

        public ReportNodeEnumerator<StepReport> AllStepsEnumerator { get; private set; }

        public override bool TryParse()
        {
            if (!base.TryParse())
            {
                return false;
            }

            // iteration index
            Index = Node.Data.IndexSpecified ? Node.Data.Index : 0;
            if (Index <= 0)
            {
                OutputWriter.WriteLine(Properties.Resources.ErrMsg_Input_InvalidGUITestIterationIndex, Index.ToString());
                return false;
            }

            // actions
            Actions.Clear();
            ReportNodeType[] childNodes = Node.ReportNode;
            if (childNodes != null)
            {
                foreach (ReportNodeType node in childNodes)
                {
                    ActionReport action = Actions.TryParseAndAdd(node, this.Node);
                    if (action != null)
                    {
                        AllStepsEnumerator.Merge(action.AllStepsEnumerator);
                        continue;
                    }
                }
            }
            if (Actions.Length == 0)
            {
                // no action node is parsed successfully under the iteration node, it is not a valid GUI test Xml report
                return false;
            }

            return true;
        }

        public int Index { get; private set; }
    }
}
