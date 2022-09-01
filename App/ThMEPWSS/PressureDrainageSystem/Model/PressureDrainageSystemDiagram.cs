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
        private bool Debug = false;
        public ThPressureDrainageSystemDiagram(PressureDrainageModelData modeldatas, bool debug)
        {
            _modeldatas = modeldatas;
            Debug = debug;
        }
        public void Init()
        {
            var pplSysUnitConstrServ = new PipeLineSystemUnitConstructionService(_modeldatas,Debug);
            _pipeLineSystemUnits = pplSysUnitConstrServ.ConstructPipeLineSystemUnits();
        }
        public void Draw(Point3d refpt)
        {
            var dwgPDrainSysDiagServ = new PressureDrainageSystemDiagramService(_pipeLineSystemUnits, _modeldatas, refpt, Debug);
            dwgPDrainSysDiagServ.Draw();
        }
    }
}