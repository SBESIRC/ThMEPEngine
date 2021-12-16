using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public class GetPrimitivesService
    {
        public ThMEPOriginTransformer originTransformer;
        public GetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
        }

        public List<Line> GetBeamLine(Polyline polyline)
        {
            var allLines = new List<Line>();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var BeamLines = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == "TH_AI_BEAM")
                .Where(o => o.Length > 100);//过滤小于10cm的梁

                var objs = new DBObjectCollection();
                BeamLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Line;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                allLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();
            }
            return allLines;
        }

        public List<Line> GetSecondaryBeamLine(Polyline polyline)
        {
            var allLines = new List<Line>();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var BeamLines = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == "TH_AICL_BEAM")
                .Where(o => o.Length > 100);//过滤小于10cm的梁

                var objs = new DBObjectCollection();
                BeamLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Line;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                allLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();
            }
            return allLines;
        }

        public List<Line> GetHouseBound(Polyline polyline)
        {
            var allLines = new List<Line>();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var brinkBeam = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == "TH_AI_HOUSEBOUND")
                .Where(o => o.Length > 100);//过滤小于10cm的梁

                var objs = new DBObjectCollection();
                brinkBeam.ForEach(x =>
                {
                    var transCurve = x.Clone() as Line;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                allLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();
            }
            return allLines;
        }

        public List<Line> GetWallBound(Polyline polyline)
        {
            var allLines = new List<Line>();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var WallBounds = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == "TH_AI_WALLBOUND")
                .Where(o => o.Length > 100);//过滤小于10cm的梁

                var objs = new DBObjectCollection();
                WallBounds.ForEach(x =>
                {
                    var transCurve = x.Clone() as Line;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                allLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();
            }
            return allLines;
        }
    }
}
