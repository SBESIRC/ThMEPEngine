using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public class PresenterFireBlockConvert : Presenter<IFireBlockConvert>
    {
        public PresenterFireBlockConvert(IFireBlockConvert View) : base(View)
        {
        }

        public override void OnViewEvent()
        {
        }

        public override void OnViewLoaded()
        {

            View.m_ListLayingRatio = InitListLayingRatio();

        }

        private List<ViewGdvEidtData> InitListLayingRatio()
        {
            List<ViewGdvEidtData> _List = new List<ViewGdvEidtData>();
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:25", ValueMember = "25" });
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:50", ValueMember = "50" });
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:75", ValueMember = "75" });
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:100", ValueMember = "100" });
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:150", ValueMember = "150" });
            _List.Add(new ViewGdvEidtData() { DisplayMember = "1:200", ValueMember = "200" });
            return _List;

        }
    }
}
