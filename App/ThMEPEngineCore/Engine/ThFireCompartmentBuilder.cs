using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentBuilder
    {
        public List<string> LayerFilter { get; set; }

        public Dictionary<Entity, List<string>> InvalidResults { get; set; }

        public ThFireCompartmentBuilder()
        {
            LayerFilter = new List<string>();
            InvalidResults = new Dictionary<Entity, List<string>>();
        }

        //
        public List<ThFireCompartment> BuildFromMS(Database db, Point3dCollection pts)
        {
            var outlineEngine = new ThFireCompartmentOutlineRecognitionEngine()
            {
                LayerFilter = this.LayerFilter,
            };
            outlineEngine.RecognizeMS(db, pts);
            var FireCompartmentGeometry = outlineEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThFireCompartmentMarkRecognitionEngine()
            {
                LayerFilter = this.LayerFilter,
            };
            markEngine.RecognizeMS(db, pts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            return Build(FireCompartmentGeometry, marks);
        }
        public List<ThFireCompartment> BuildFromMS(Database db, ObjectIdCollection objs)
        {
            var outlineEngine = new ThFireCompartmentOutlineRecognitionEngine()
            {
                LayerFilter = this.LayerFilter,
            };
            outlineEngine.RecognizeMS(db, objs);
            var FireCompartmentGeometry = outlineEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThFireCompartmentMarkRecognitionEngine()
            {
                LayerFilter = this.LayerFilter
            };
            markEngine.RecognizeMS(db, new Point3dCollection());
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            return Build(FireCompartmentGeometry, marks);
        }
        private List<ThFireCompartment> Build(List<ThIfcRoom> rooms, List<ThIfcTextNote> marks)
        {
            if (rooms.Count == 0)
                return new List<ThFireCompartment>();
            List<Polyline> choisemark = new List<Polyline>();
            var dbDbTextObjs = marks.Select(o => o.Geometry).ToCollection();
            ThCADCoreNTSSpatialIndex DbTextspatialIndex = new ThCADCoreNTSSpatialIndex(dbDbTextObjs);
            List<Polyline> FireCompartmentData = rooms.Select(o => o.Boundary as Polyline).ToList();
            var Holes = CalHoles(FireCompartmentData);
            var ThFireCompartments = FireCompartmentData.Select(x => new ThFireCompartment() { Boundary = Holes.Keys.Contains(x) ? GetMpolygon(Holes.FirstOrDefault(o => o.Key == x)) : x }).ToList();
            foreach (var FireCompartment in ThFireCompartments)
            {
                var objs = DbTextspatialIndex.SelectCrossingPolygon(FireCompartment.Boundary);
                if (objs.Count > 0)
                {
                    var mark = marks.Where(o => objs.Contains(o.Geometry) & !choisemark.Contains(o.Geometry)).ToList();
                    var FindMarkCount = mark.Count();
                    if (FindMarkCount == 0)
                    {
                        //没有找到防火分区名称，说明设计师没画
                        //do not
                    }
                    else if (FindMarkCount == 1)
                    {
                        //找到防火分区名称，为防火分区命名
                        FireCompartment.Number = mark.First().Text;
                        choisemark.AddRange(mark.Select(o=>o.Geometry)); 
                    }
                    else
                    {
                        //防火分区有多个名称，属于设计师画错，填充到错误Map
                        choisemark.AddRange(mark.Select(o => o.Geometry));
                        InvalidResults.Add(FireCompartment.Boundary, mark.Select(o => o.Text).ToList());
                    }
                }
            }
            return ThFireCompartments;
        }
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();
            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);
                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
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
            if (polylines.Count == 1)
                return polylines[0];
            var rGeometry = polylines[0].ToNTSPolygon().Difference(polylines.Skip(1).ToCollection().UnionGeometries());
            if (rGeometry is Polygon polygon)
            {
                var newEntity = polygon.ToDbEntity();
                return newEntity;
            }
            else
            {
                //说明有边相邻，产生了杂边，需要剔除杂边
                var diffobjs = rGeometry.ToDbCollection();
                var samePolys = diffobjs.Cast<Polyline>()
                    .Select(x => x.Buffer(-1))
                    .Where(x => x.Count > 0)
                    .Select(x => x[0] as Polyline)
                    .Select(x => x.Buffer(1)[0] as Polyline);
                return GetMpolygon(samePolys.ToList());
            }
        }
    }
}
