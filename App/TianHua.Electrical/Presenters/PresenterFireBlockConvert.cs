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
        }
    }
}
