using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThControlLibraryWPF.ControlUtils
{
    public class FormUtil
    {
        public static void EnableForm(System.Windows.Controls.Grid form)
        {
            form.IsEnabled = true;
            CommonUtil.DoEvents();
        }
        public static void DisableForm(System.Windows.Controls.Grid form)
        {
            form.IsEnabled = false;
            CommonUtil.DoEvents();
        }
    }
}
