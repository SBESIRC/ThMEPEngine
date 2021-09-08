using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Engine
{
    public class ThFloorDrainExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public bool BlockObbSwitch { get; set; } // true->获取块的Obb,false->获取块
        public HashSet<string> BlkNames { get; set; }
        public ThFloorDrainExtractionEngine()
        {
            BlockObbSwitch = true;
            BlkNames = new HashSet<string>();
        }
        public void Dispose()
        {     
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThFloorDrainExtractionVisitor()
            {
                BlockObbSwitch = this.BlockObbSwitch,
                BlkNames = BlkNames,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThFloorDrainExtractionVisitor()
            {
                BlockObbSwitch = this.BlockObbSwitch,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }
    }
    public class ThFloorDrainRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public double OffsetDis { get; set; }
        public bool FilterSwitch { get; set; }
        public HashSet<string> BlkNames { get; set; }
        public ThFloorDrainRecognitionEngine()
        {
            OffsetDis = 300;
            FilterSwitch = false;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThFloorDrainExtractionEngine()
            {
                BlkNames = BlkNames,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThFloorDrainExtractionEngine()
            {
                BlkNames = BlkNames,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var center = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            if (newPts.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                objs = spatialIndex.SelectCrossingPolygon(polygon);                
            }
            if (FilterSwitch)
            {
                objs = Filter(objs);
            }
            transformer.Reset(objs);
            // 通过获取的OriginData 分类
            Elements.AddRange(objs.Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x}));
        }
        private DBObjectCollection Filter(DBObjectCollection geometires)
        {
            // 把地漏邻近范围的地漏过滤掉
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
    }
}
