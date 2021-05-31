using NFox.Cad;
using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Engine
{
    public class ThFloorDrainExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public bool BlockObbSwitch { get; set; } // true->获取块的Obb,false->获取块
        public ThFloorDrainExtractionEngine()
        {
            BlockObbSwitch = true;
        }
        public void Dispose()
        {     
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThFloorDrainVisitor()
            {
                BlockObbSwitch = this.BlockObbSwitch,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
    public class ThFloorDrainRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public bool FilterSwitch { get; set; }
        public double OffsetDis { get; set; }
        public ThFloorDrainRecognitionEngine()
        {
            FilterSwitch = false;
            OffsetDis = 300;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThFloorDrainExtractionEngine();
            engine.Extract(database);
            var originDatas = engine.Results;
            if (polygon.Count > 0)
            {
                var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }

            if (FilterSwitch)
            {
                var filters = Filter(originDatas.Select(o => o.Geometry).ToCollection());
                originDatas = originDatas.Where(o => filters.Contains(o.Geometry)).ToList();
            }

            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }

        private DBObjectCollection Filter(DBObjectCollection geometires)
        {
            var results = new DBObjectCollection();
            while (geometires.Count > 0)
            {
                var obj = geometires[0];
                geometires.RemoveAt(0);
                results.Add(obj);

                var rec = obj is BlockReference ? ((BlockReference)obj).GeometricExtents.ToRectangle() : obj as Polyline;
                var bufferObjs = rec.Buffer(OffsetDis);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(geometires);
                var range = new Polyline();
                if (bufferObjs.Count > 0)
                {
                    range = bufferObjs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
                }
                else
                {
                    range = rec;
                }
                var searchObjs = spatialIndex.SelectCrossingPolygon(range);
                searchObjs.Cast<Entity>().ForEach(o => geometires.Remove(o));
            }
            return results;
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
