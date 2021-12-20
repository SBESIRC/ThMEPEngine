using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public class GetPrimitivesService
    {
        public ThMEPOriginTransformer originTransformer;
        public GetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
        }

        public List<Line> GetBeamLine(Polyline polyline, out ObjectIdCollection objIDs)
        {
            var allLines = new List<Line>();
            objIDs = new ObjectIdCollection();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var BeamLines = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == SecondaryBeamLayoutConfig.MainBeamLayerName);
                var BeamLineDic = BeamLines.ToDictionary(key => key.Clone() as Line, value => value.Id);
                var objs = new DBObjectCollection();
                BeamLineDic.ForEach(x =>
                {
                    var transCurve = x.Key;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var dbobjs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline);
                allLines = dbobjs.Cast<Line>().Where(o => o.Length > 100).ToList();//过滤小于10cm的梁
                objIDs =BeamLineDic.Where(o => dbobjs.Contains(o.Key)).Select(o => o.Value).ToObjectIdCollection();
            }
            return allLines;
        }

        public List<Line> GetSecondaryBeamLine(Polyline polyline, out ObjectIdCollection objIDs)
        {
            var allLines = new List<Line>();
            objIDs = new ObjectIdCollection();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var BeamLines = acad.ModelSpace
                .OfType<Line>()
                .Where(o => o.Layer == SecondaryBeamLayoutConfig.SecondaryBeamLayerName);
                var BeamLineDic = BeamLines.ToDictionary(key => key.Clone() as Line, value => value.Id);
                var objs = new DBObjectCollection();
                BeamLineDic.ForEach(x =>
                {
                    var transCurve = x.Key;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var dbobjs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline);
                allLines = dbobjs.Cast<Line>().Where(o => o.Length > 100).ToList();//过滤小于10cm的梁
                objIDs =BeamLineDic.Where(o => dbobjs.Contains(o.Key)).Select(o => o.Value).ToObjectIdCollection();
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
