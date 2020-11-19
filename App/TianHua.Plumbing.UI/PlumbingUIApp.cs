using Catel.IoC;
using Catel.Core;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public class PlumbingUIApp : IExtensionApplication
    {
        private fmSprinklerLayout SprinklerLayout { get; set; }
        public void Initialize()
        {
            ActivateIoc();
            SprinklerLayout = null;
        }

        public void Terminate()
        {
            SprinklerLayout = null;
        }

        private void ActivateIoc()
        {
            var serviceLocator = ServiceLocator.Default;
            serviceLocator.AutoRegisterTypesViaAttributes = true;
        }

        [CommandMethod("TIANHUACAD", "THPL", CommandFlags.Modal)]
        public void ThWSSUI()
        {
            if (SprinklerLayout == null)
            {
                SprinklerLayout = new fmSprinklerLayout();
            }
            AcadApp.ShowModelessDialog(SprinklerLayout);
        }
    }
}
