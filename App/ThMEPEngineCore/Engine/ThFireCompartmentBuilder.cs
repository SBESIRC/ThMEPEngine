using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using Linq2Acad;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentBuilder
    {
        public List<string> LayerFilter { get; set; }

        public ThFireCompartmentBuilder()
        {
            LayerFilter = new List<string>();
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


        public List<ThFireCompartment> Build(List<ThIfcRoom> rooms, List<ThIfcTextNote> marks)
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
                    var mark = marks.First(o => objs.Contains(o.Geometry) & !choisemark.Contains(o.Geometry));
                    if (!mark.IsNull())
                    {
                        choisemark.Add(mark.Geometry);
                        FireCompartment.Number = mark.Text;
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
