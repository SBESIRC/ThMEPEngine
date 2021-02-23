﻿using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractDrainageFacilityService:ThExtractService
    {
        public List<Curve> Facilities { get; set; }
        public ThExtractDrainageFacilityService()
        {
            Facilities = new List<Curve>();
        }
        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {                
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsDrainageFacilityLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            Facilities.Add(newPolyline);
                        }
                    }
                    else if(ent is Line line)
                    {
                        if (IsDrainageFacilityLayer(line.Layer))
                        {
                            var newLine = line.Clone() as Line;
                            Facilities.Add(newLine);
                        }
                    }
                }
            }
        }        

        private bool IsDrainageFacilityLayer(string layerName)
        {
            return layerName == "排水设施";
        }
    }
}
