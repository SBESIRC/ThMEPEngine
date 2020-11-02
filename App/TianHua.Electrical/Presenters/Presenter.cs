using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    /// <summary>
    /// Presenter基础类
    /// </summary>
    /// <typeparam name="IView"></typeparam>
    public class Presenter<IView>
    {
        public IView View { get; set; }

        public Presenter(IView _View)
        {
            this.View = _View;

            this.OnViewEvent();

            this.OnViewLoaded();
        }

        virtual public void OnViewLoaded()
        {

        }

        virtual public void OnViewEvent()
        {

        }
    }
}
