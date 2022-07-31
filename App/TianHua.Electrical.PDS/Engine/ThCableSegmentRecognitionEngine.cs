using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCableSegmentRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public ThCableSegmentRecognitionEngine()
        {
            Results = new List<ThPDSEntityInfo>();
        }

        public List<ThPDSEntityInfo> Results { get; protected set; }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThCableSegmentExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var entityInfos = new List<ThPDSEntityInfo>();
            datas.Select(data => data.Data as Curve)
                .Where(o => o.GetLength() > 1.0)
                .Where(o => !o.Closed)
                .Select(data => new ThPDSEntityInfo(data, true))
                .ForEach(info =>
                {
                    if (info.Entity is Line)
                    {
                        entityInfos.Add(info);
                    }
                    else if (info.Entity is Polyline pline)
                    {
                        var curves = new DBObjectCollection();
                        pline.Explode(curves);
                        foreach (var curve in curves.OfType<Curve>().Where(c => c.GetLength() > 1.0))
                        {
                            if (curve is Line line)
                            {
                                entityInfos.Add(new ThPDSEntityInfo(line, info));
                            }
                            else if (curve is Arc arc)
                            {
                                HandleArc(arc, info, entityInfos);
                            }
                        }
                    }
                    else if (info.Entity is Arc arc)
                    {
                        HandleArc(arc, info, entityInfos);
                    }
                });
            var lines = entityInfos.Select(e => e.Entity).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
                lines = spatialIndex.SelectCrossingPolygon(polygon);
            }
            entityInfos.ForEach(e =>
            {
                if (lines.Contains(e.Entity))
                {
                    Results.Add(e);
                }
            });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        private void HandleArc(Arc arc, ThPDSEntityInfo info, List<ThPDSEntityInfo> entityInfos)
        {
            var arcToPolyline = arc.TessellateArcWithArc(100.0);
            var arcCurves = new DBObjectCollection();
            arcToPolyline.Explode(arcCurves);
            arcCurves.OfType<Line>().ForEach(o => entityInfos.Add(new ThPDSEntityInfo(o, info)));
        }
    }
}
