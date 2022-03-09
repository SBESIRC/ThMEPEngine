using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThCanvas.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSCircuitGraphCanvas : UserControl
    {
        public ThPDSCircuitGraphCanvas()
        {
            InitializeComponent();
        }
        public void Draw(DBObjectCollection objs)
        {
            using (var adb = AcadDatabase.Active())
            {
                foreach (var obj in objs.OfType<Entity>())
                {
                    adb.ModelSpace.Add(obj);
                }
            }
        }
    }
}
