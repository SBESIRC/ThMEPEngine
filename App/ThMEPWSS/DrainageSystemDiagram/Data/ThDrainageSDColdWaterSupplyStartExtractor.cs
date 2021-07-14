using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetARX;
using Linq2Acad;
using NFox.Cad;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDColdWaterSupplyStartExtractor : ThExtractorBase , IAreaId
    {
        public List<ThRawIfcBuildingElementData> ColdWaterSupplyStarts { get; private set; }
        public string AreaId { get;private set; }

        public ThDrainageSDColdWaterSupplyStartExtractor()
        {
            Category = "WaterSupplyStartPoint";
            ElementLayer = "给水起点";
            ColdWaterSupplyStarts = new List<ThRawIfcBuildingElementData>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThDrainageSDColdWaterSupplyStartExtractEngine()
            {
                ElementLayer = ElementLayer,
            };
            instance.Extract(database);
            var originData = instance.Results;

            //transforme
            //

            using (var recEngine = new ThDrainageSDColdWaterSupplyStartRecognitionEngine())
            {
                recEngine.Recognize(originData, pts);
                ColdWaterSupplyStarts.AddRange(recEngine.Result);
            }
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            results.AddRange(BuildColdWaterSupplyStartExtractor());
            return results;
        }

        public List<ThGeometry> BuildColdWaterSupplyStartExtractor()
        {
            var geos = new List<ThGeometry>();

            ColdWaterSupplyStarts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThDrainageSDCommon.ProAreaId,AreaId);

                if (o.Geometry is DBPoint polyline)
                {
                    geometry.Boundary = o.Geometry;
                }

                geos.Add(geometry);
            });

            return geos;
        }

        public void setAreaId(string groupId)
        {
            if (GroupSwitch)
            {
                AreaId = groupId;
            }
        }

    }



    public class ThDrainageSDColdWaterSupplyStartExtractEngine : ThBuildingElementExtractionEngine
    {
        public string ElementLayer { get; set; }
        public ThDrainageSDColdWaterSupplyStartExtractEngine()
        {

        }

        public override void Extract(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var selectObject = acadDatabase.ModelSpace
                     .OfType<Curve>()
                     .Where(o => IsElementLayer(o.Layer))
                     .Select(o => o.Clone() as Curve)
                     .ToList();

                for (int i = 0; i < selectObject.Count; i++)
                {
                    if (selectObject[i] is Circle circle)
                    {
                        var waterSupply = new ThRawIfcBuildingElementData()
                        { Geometry = new DBPoint(circle.Center) };
                        Results.Add(waterSupply);
                    }
                }
            }
        }

        public bool IsElementLayer(string layer)
        {
            return layer == ElementLayer;
        }
    }


    public class ThDrainageSDColdWaterSupplyStartRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<ThRawIfcBuildingElementData> Result { get; private set; }

        public ThDrainageSDColdWaterSupplyStartRecognitionEngine()
        {

        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {

        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            Result = new List<ThRawIfcBuildingElementData>();
            if (polygon.Count >= 3)
            {
                var dbObjs = datas.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                Result.AddRange(datas.Where(o => dbObjs.Contains(o.Geometry)).ToList());
            }
        }
    }


}
