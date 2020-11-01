using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.UI
{
    public partial class CtlExhaustControlBase : UserControl
    {
        //public CtlExhaustControlBase()
        //{
        //    InitializeComponent();
        //}

        public virtual void InitForm(FanDataModel _FanDataModel,Action action)
        {
            //
        }
         public virtual void UpdateCalcAirVolum(ExhaustCalcModel model)
        {
            //
        }
    }
}
