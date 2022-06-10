using Xbim.Ifc;
using ThMEPTCH.Model;
using ThMEPIFC.Ifc2x3;

namespace ThMEPIFC
{
    public class ThTGL2IFCService
    {
        public IfcStore Model { get; private set; }
        public void GenerateIfcModelAndSave(ThTCHProject project, string file)
        {
            Model = ThTGL2IFC2x3Factory.CreateAndInitModel("ThTGL2IFCProject");
            if (Model != null)
            {
                ThTGL2IFC2x3Builder.BuildIfcModel(Model, project);
                ThTGL2IFC2x3Builder.SaveIfcModel(Model, file);
            }
        }
    }
}
