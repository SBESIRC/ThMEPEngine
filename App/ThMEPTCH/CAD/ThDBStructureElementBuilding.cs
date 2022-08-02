using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class ThDBStructureElementBuilding
    {
        public void BuildingFromMS(Database database)
        {
            var engine = new ThDBStructureElementExtractionEngine();
            engine.ExtractFromMS(database);
            var walls = engine.Results.Where(o => o.Data is THStructureWall);
            var column = engine.Results.Where(o => o.Data is THStructureColumn);
            var beams = engine.Results.Where(o => o.Data is THStructureBeam);
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var beam in beams)
                {
                    beam.Geometry.ColorIndex = 1;
                    acad.ModelSpace.Add(beam.Geometry);
                }
            }
            //Building(engine.Results, polygon);
        }
    }
}
