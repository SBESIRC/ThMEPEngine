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
            View.m_ListStrongBlockConver = InitListStrongBlockConver();

            View.m_ListWeakBlockConver = InitListWeakBlockConver();
        }

        private List<ViewFireBlockConvert> InitListWeakBlockConver()
        {
            return new List<ViewFireBlockConvert>();
        }

        private List<ViewFireBlockConvert> InitListStrongBlockConver()
        {
            return new List<ViewFireBlockConvert>();
        }
    }
}
