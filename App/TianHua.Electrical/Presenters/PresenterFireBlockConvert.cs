using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public class PresenterFireBlockConver : Presenter<IFireBlockConvert>
    {
        public PresenterFireBlockConver(IFireBlockConvert View) : base(View)
        {

        }

        public override void OnViewEvent()
        {

        }

        public override void OnViewLoaded()
        {
            View.m_ListFireBlockConver = InitListFireBlockConvert();
        }

        private List<ViewFireBlockConvert> InitListFireBlockConvert()
        {
            return new List<ViewFireBlockConvert>();
        }
    }
}
