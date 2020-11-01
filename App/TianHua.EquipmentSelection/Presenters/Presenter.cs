using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TianHua.FanSelection
{
    /// <summary>
    /// Presenter基础类
    /// </summary>
    /// <typeparam name="IView"></typeparam>
    public class Presenter<IView>
    {

        public IView View { get; set; }

        public Presenter(IView view)
        {
            this.View = view;

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
