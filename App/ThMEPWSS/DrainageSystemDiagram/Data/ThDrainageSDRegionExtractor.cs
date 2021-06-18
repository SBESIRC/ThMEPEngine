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

    public class ThDrainageSDRegionExtractor : ThExtractorBase
    {

        public List<Polyline> ToiletGroups { get; private set; }
        public Dictionary<Polyline, string> ToiletGroupId { get; private set; }
        public List<ThRawIfcBuildingElementData> Region { get; private set; }

        public string ElementLayer { get; set; }

        private const string AlignmentVectorPropertyName = "AlignmentVector";
        private const string NeibourIdsPropertyName = "NeighborIds";
        private const string IdPropertyName = "Id";

        public ThDrainageSDRegionExtractor()
        {
            ToiletGroups = new List<Polyline>();
            ToiletGroupId = new Dictionary<Polyline, string>();
            Region = new List<ThRawIfcBuildingElementData>();
            Category = "Region";
            ElementLayer = "卫生间分组";
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThDrainageSDRegionExtractEngine()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database);
            instance.Results.ForEach(x =>
              {
                  var pl = x.Geometry as Polyline;
                  ToiletGroups.Add(pl);
              });

            ToiletGroups.ForEach(o => ToiletGroupId.Add(o, Guid.NewGuid().ToString()));
            var originData = instance.Results;

            //transforme
            //

            using (var recEngine = new ThDrainageSDRegionRecognitionEngine())
            {
                recEngine.Recognize(originData, pts);
                Region.AddRange(recEngine.Result);
            }
        }


        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Region.ForEach(o =>
            {
                var geometry = new ThGeometry();
                var pl = o.Geometry as Polyline;
                geometry.Properties.Add(IdPropertyName, ToiletGroupId[pl]);
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(AlignmentVectorPropertyName, new double[] { 1.000000, 0.000000 });
                geometry.Properties.Add(NeibourIdsPropertyName, new string[] { });
                geometry.Boundary = pl;
                geos.Add(geometry);
            });
            return geos;
        }

    }


    public class ThDrainageSDRegionExtractEngine : ThBuildingElementExtractionEngine
    {
        public string ElementLayer { get; set; }


        public ThDrainageSDRegionExtractEngine()
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
                    if (selectObject[i] is Polyline pl)
                    {
                        var regionFrame = new ThRawIfcBuildingElementData()
                        { Geometry = pl };
                        Results.Add(regionFrame);
                    }
                }
            }
        }

        public bool IsElementLayer(string layer)
        {
            return layer == ElementLayer;
        }
    }


    public class ThDrainageSDRegionRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<ThRawIfcBuildingElementData> Result { get; private set; }

        public ThDrainageSDRegionRecognitionEngine()
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
