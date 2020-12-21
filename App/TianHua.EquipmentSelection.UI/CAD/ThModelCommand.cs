using AcHelper;
using AcHelper.Commands;

namespace TianHua.FanSelection.UI.CAD
{
    public abstract class ThModelCommand : IAcadCommand
    {
        public abstract void Execute();

        protected void SetFocusToDwgView()
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
