using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tianhua.Platform3D.UI.ViewModels
{
    class FunctionTabItem
    {
        public string TabItemName { get; }
        public UserControl UControl { get; }
        public FunctionTabItem(string name, UserControl uControl) 
        {
            TabItemName = name;
            UControl = uControl;
        }
    }
}
