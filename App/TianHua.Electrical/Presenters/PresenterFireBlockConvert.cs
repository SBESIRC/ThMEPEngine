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
            View.m_ListWeakBlockConvert = InitListWeakBlockConver();
            View.m_ListStrongBlockConvert = InitListStrongBlockConver();
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
