using Xbim.Ifc;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    public class ThTGL2IFCService
    {
        public IfcStore Model { get; private set; }
        public void GenerateIfcModelAndSave(ThTCHProject project, string file)
        {
            Model = ThTGL2IFCFactory.CreateAndInitModel("ThTGL2IFCProject");
            if (Model != null)
            {
                ThTGL2IFCBuilder.BuildIfcModel(Model, project);
                ThTGL2IFCBuilder.SaveIfcModel(Model, file);
            }
        }
    }
}
