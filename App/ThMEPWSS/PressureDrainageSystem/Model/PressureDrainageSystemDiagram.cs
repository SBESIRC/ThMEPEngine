using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Service;
namespace ThMEPWSS.PressureDrainage.Model
{
    public class ThPressureDrainageSystemDiagram : ThWSystemDiagram
    {
        private PressureDrainageModelData _modeldatas;
        private List<PipeLineSystemUnitClass> _pipeLineSystemUnits;
        public ThPressureDrainageSystemDiagram(PressureDrainageModelData modeldatas)
        {
            _modeldatas = modeldatas;
        }
        public void Init()
        {
            var pplSysUnitConstrServ = new PipeLineSystemUnitConstructionService(_modeldatas);
            _pipeLineSystemUnits= pplSysUnitConstrServ.ConstructPipeLineSystemUnits();
        }
        public void Draw(Point3d refpt)
        {
            var dwgPDrainSysDiagServ = new PressureDrainageSystemDiagramService(_pipeLineSystemUnits,_modeldatas,refpt);
            dwgPDrainSysDiagServ.Draw();
        }
    }
}