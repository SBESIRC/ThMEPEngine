using AcHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public class DrawingParameterSetVM
    {
        public DrawingParameterSetModel Model { get; set; }
        public DrawingParameterSetVM()
        {
            Model = new DrawingParameterSetModel();
        }
        public void Run()
        {
            Model.Write();
        }
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
