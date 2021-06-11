using NFox.Cad;
using DotNetARX;
using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentExtractionEngine : ThSpatialElementExtractionEngine
    {
        public List<string> LayerFilter { get; set; }
        public ThFireCompartmentExtractionEngine()
        {
            LayerFilter = new List<string>();
        }
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }
        public override void ExtractFromMS(Database database)
        {
            //获取防火分区边框访问者
            var visitor = new ThFireCompartmentExtractionVisitor()
            {
                LayerFilter = this.LayerFilter,
            };
            //获取防火分区编号访问者
            var visitorDbText = new ThFireCompartmentNameExtractionVisitor()
            {
                LayerFilter = this.LayerFilter,
            };

            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Accept(visitorDbText);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
            Results.AddRange(visitorDbText.Results);
        }
    }


    public class ThFireCompartmentRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public List<string> LayerFilter { get; set; }
        public ThFireCompartmentRecognitionEngine()
        {
            LayerFilter = new List<string>();
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThFireCompartmentExtractionEngine()
            {
                LayerFilter = this.LayerFilter,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var dbPolylineObjs = datas.Where(o => o.Geometry is Polyline).Select(o => o.Geometry).ToCollection();
            var dbDbTextObjs = datas.Where(o => o.Geometry is DBText || o.Geometry is MText).Select(o => o.Geometry).ToCollection();
            ThCADCoreNTSSpatialIndex DbTextspatialIndex = new ThCADCoreNTSSpatialIndex(dbDbTextObjs);
            if (polygon.Count > 0)
            {
                var PolyLinespatialIndex = new ThCADCoreNTSSpatialIndex(dbPolylineObjs);
                //DbTextspatialIndex = new ThCADCoreNTSSpatialIndex(dbDbTextObjs);
                dbPolylineObjs = PolyLinespatialIndex.SelectCrossingPolygon(polygon);
            }
            datas = datas.Where(o => dbPolylineObjs.Contains(o.Geometry)).ToList();
            List<Polyline> FireCompartmentData = datas.Select(x => x.Geometry as Polyline).ToList();
            var Holes = CalHoles(FireCompartmentData);
            // 通过获取的OriginData 分类
            var ThFireCompartments = FireCompartmentData.Select(x => new ThFireCompartment() { Boundary = Holes.Keys.Contains(x) ? GetMpolygon(Holes.FirstOrDefault(o => o.Key == x)) : x }).ToList();
            foreach (var FireCompartment in ThFireCompartments)
            {
                var objs = DbTextspatialIndex.SelectCrossingPolygon(FireCompartment.Boundary);
                if(objs.Count>0)
                {
                    FireCompartment.Number = objs[0] is DBText dBText ? dBText.TextString : (objs[0] as MText).Contents;
                }
            }
            Elements.AddRange(ThFireCompartments);
        }

        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);
                //firFrame = firFrame.DPSimplify(1);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));
                if (holes.Count > 0)
                    holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        private Entity GetMpolygon(KeyValuePair<Polyline, List<Polyline>> keyValuePair)
        {
            List<Polyline> polylines = new List<Polyline>();
            polylines.Add(keyValuePair.Key);
            polylines.AddRange(keyValuePair.Value);
            return GetMpolygon(polylines);
        }
        private Entity GetMpolygon(List<Polyline> polylines)
        {
            var objs = new DBObjectCollection();

            foreach (var obj in  polylines)
            {
                objs.Add(obj);
            }

            var mPolygon = objs.BuildArea();
            if (mPolygon.Count == 1 && mPolygon[0] is MPolygon)
                return mPolygon[0] as MPolygon;
            else
                throw new ArgumentException();//参数无效
        }
    }
}
