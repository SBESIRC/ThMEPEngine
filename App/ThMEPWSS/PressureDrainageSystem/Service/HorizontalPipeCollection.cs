using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Common;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Utils;
using ThMEPWSS.Uitl;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class HorizontalPipeCollection
    {
        public HorizontalPipeCollection(PressureDrainageDataCollectionService service)
        {
            CollectedData = service.CollectedData;
            SelectedBound = service.SelectedBound;
            Entities = service.Entities;
            Lines = service.Lines;
            Polylines = service.Polylines;
            BlockReferences = service.BlockReferences;
            TangentElements = service.TangentElements;
        }
        public PressureDrainageGeoData CollectedData { get; set; }
        private Polyline SelectedBound { get; set; }
        private List<Entity> Entities = new List<Entity>();
        private List<Line> Lines = new List<Line>();
        private List<Polyline> Polylines = new List<Polyline>();
        private List<BlockReference> BlockReferences = new List<BlockReference>();
        private List<Entity> TangentElements = new List<Entity>();


        bool MatchLayer(Entity entity)
        {
            var cond = entity.Layer == "W-FRPT-DRAI-PIPE" || entity.Layer == "W-RAIN-PIPE";
            cond = cond ||
                (entity.Layer.Contains("W") && entity.Layer.Contains("DRAI") && entity.Layer.Contains("PIPE"));
            cond = cond ||
                (entity.Layer.Contains("W") && entity.Layer.Contains("RAIN") && entity.Layer.Contains("PIPE"));
            return cond;
        }
        Horizontal HorizontalFromLine(Line e, string layer = "")
        {
            var line = new Line(e.StartPoint.Project(), e.EndPoint.Project());
            if (layer != "")
                line.Layer = layer;
            else
                line.Layer = e.Layer;
            return new Horizontal(line);
        }
        void Collect()
        {
            CollectedData.HorizontalPipes = new List<Horizontal>();
            CollectedData.HorizontalPipes.AddRange(Lines
                .Where(e => MatchLayer(e))
                .Where(e => e.Length > 0)
                .Select(e => HorizontalFromLine(e)));
            foreach (var pl in Polylines.Where(e => MatchLayer(e)).Where(e => e.Length > 0))
            {
                var segs = pl.GetEdges();
                CollectedData.HorizontalPipes.AddRange(segs.Select(e => HorizontalFromLine(e,pl.Layer)));
            }
            foreach (var element in TangentElements.Where(e => MatchLayer(e)))
            {

                var explodeobjs = element.ExplodeToDBObjectCollection().Cast<Entity>();
                var layer = element.Layer;
                var explode_lines = explodeobjs.OfType<Line>().Where(ln => ln.Length > 0).ToList();
                if (explode_lines.Count() == 1)
                    CollectedData.HorizontalPipes.Add(HorizontalFromLine(explode_lines.First(), layer));
                else if (explode_lines.Count() > 1)
                {
                    CollectedData.HorizontalPipes.Add(HorizontalFromLine(explode_lines.First(), layer));
                    Point3d pt1 = default, pt2 = default;
                    for (int i = 1; i < explode_lines.Count(); i++)
                    {
                        CollectedData.HorizontalPipes.Add(HorizontalFromLine(explode_lines[i], layer));
                        pt1 = explode_lines[i - 1].EndPoint.Project();
                        pt2 = explode_lines[i].StartPoint.Project();
                        var tmpLine = new Line(pt1, pt2);
                        if (tmpLine.Length > 0)
                            CollectedData.HorizontalPipes.Add(HorizontalFromLine(tmpLine, layer));
                    }
                }
                var explode_plys = explodeobjs.OfType<Polyline>().Where(ln => ln.Length > 0).ToList();
                if (explode_plys.Count > 0)
                {
                    foreach (var pl in explode_plys)
                    {
                        var segs = pl.GetEdges();
                        CollectedData.HorizontalPipes.AddRange(segs.Select(e => HorizontalFromLine(e, layer)));
                    }
                }
            }
        }
        void Process()
        {
            List<Line> lines = new List<Line>();
            CollectedData.HorizontalPipes.ForEach(o => lines.Add(o.Line));
            List<Point3d> pts = new List<Point3d>();
            CollectedData.VerticalPipes.ForEach(o => pts.Add(o.Center));
            CollectedData.SubmergedPumps.ForEach(o => pts.Add(o.Extents.Centroid()));
            List<Line> mergedLines = new();
            lines.ForEach(o => mergedLines.Add(o));
            //ConnectBrokenLine(lines,/*pts*/new List<Point3d>() { }).Where(o => o.Length > 0).ForEach(o => mergedLines.Add(o));
            var objs = new DBObjectCollection();
            mergedLines.ForEach(o => objs.Add(o));
            var processedLines = mergedLines;
            RemoveDuplicatedAndInvalidLanes(ref processedLines);
            processedLines = ConnectPerpLineInTolerance(processedLines, 100);
            JoinLines(processedLines);
            InterrptLineByPoints(processedLines, pts);
            CollectedData.HorizontalPipes.Clear();
            processedLines.ForEach(o => CollectedData.HorizontalPipes.Add(new Horizontal(o)));
        }
        public void Execute()
        {
            Collect();
            Process();
        }
    }
}
