using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.IO
{
    public class ThParkingStallDataSetFactory : ThMEPDataSetFactory
    {
        private OuterBrder Border { get; set; }
        public ThParkingStallDataSetFactory(OuterBrder border)
        {
            if(border!=null)
            {
                Border = border;
            }
            else
            {
                Border = new OuterBrder();
            }
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            var dataSet = new ThMEPDataSet()
            {
                Container = new List<ThGeometry>(),
            };
            dataSet.Container.AddRange(BuildOuterLines());
            dataSet.Container.AddRange(BuildBuildingLines());
            return dataSet;
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            //ToDO
        }

        private List<ThGeometry> BuildOuterLines()
        {
            var results = new List<ThGeometry>();
            var geometry = new ThGeometry();
            geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, "OuterLine");
            geometry.Boundary = Border.WallLine;
            results.Add(geometry);
            return results; 
        }
        private List<ThGeometry> BuildBuildingLines()
        {
            var results = new List<ThGeometry>();
            Border.Buildings.ForEach(o =>
            {
                var buildingId = Guid.NewGuid().ToString();
                Plines.GetCutters(o).ForEach(p =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, "BuildingLine");
                    geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, buildingId);
                    geometry.Boundary = p;
                    results.Add(geometry);
                });
            });
            return results;
        }
    } 
}
