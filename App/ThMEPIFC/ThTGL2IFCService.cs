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
            Model = ThTGL2IFC2x3Factory.CreateAndInitModel("ThTGL2IFCProject", project.Uuid);
            if (Model != null)
            {
                ThTGL2IFC2x3Builder.BuildIfcModel(Model, project);
                ThTGL2IFC2x3Builder.SaveIfcModel(Model, file);
                Model.Dispose();
            }
        }

        public void GenerateIfcModelAndSave(ThTCHProjectData project, string file)
        {
            Model = ThTGL2IFC2x3Factory.CreateAndInitModel("ThTGL2IFCProject", project.Root.GlobalId);
            if (Model != null)
            {
                ThProtoBuf2IFC2x3Builder.BuildIfcModel(Model, project);
                ThTGL2IFC2x3Builder.SaveIfcModel(Model, file);
                Model.Dispose();
            }
        }
    }
}
