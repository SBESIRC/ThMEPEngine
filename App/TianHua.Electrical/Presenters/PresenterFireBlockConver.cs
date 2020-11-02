using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public class PresenterFireBlockConver : Presenter<IFireBlockConver>
    {
        public PresenterFireBlockConver(IFireBlockConver View) : base(View)
        {

        }

        public override void OnViewEvent()
        {

        }

        public override void OnViewLoaded()
        {
            View.m_ListFireBlockConver = InitListFireBlockConver();
        }

        private List<ViewFireBlockConver> InitListFireBlockConver()
        {
            return new List<ViewFireBlockConver>();
        }
    }
}
