using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThBeamLineCleaner
    {
        public static List<ThGeometry> Clean(List<ThGeometry> beamGeos,DBObjectCollection polygons,DBObjectCollection beamMarks)
        {
            // 用polygons对 beamGeosc裁剪
            var results = new List<ThGeometry>();
            var linearBeamGeos = beamGeos.Where(o => o.Boundary is Line).ToList();
            results.AddRange(beamGeos.Except(linearBeamGeos));

            // do clip
            var newBeamGeos = Clip(linearBeamGeos, polygons);

            // 合并相同线型的线
            newBeamGeos = MergeBeams(newBeamGeos);

            // 调整较短梁线的线型，再合并
            newBeamGeos = HandleShortBeams(newBeamGeos);

            // 处理插入的梁线，再合并
            newBeamGeos = HandleInsertBeam(newBeamGeos, beamMarks);

            results.AddRange(newBeamGeos);
            return results;
        }
        private static List<ThGeometry> Clip(List<ThGeometry> linearBeamGeos, DBObjectCollection polygons)
        {
            using (var hander = new ThBeamLineClipper(polygons))
            {
                var results = new List<ThGeometry>();
                var beamGroups = GroupBeamByLineType(linearBeamGeos);
                beamGroups.ForEach(g =>
                {
                    var beamLines = g.Select(o => o.Boundary).ToCollection();
                    var newBeamLines = hander.Handle(beamLines);
                    newBeamLines.OfType<Line>().ForEach(l => results.Add(ThGeometry.Create(l, Clone(g.First().Properties))));
                });
                return results;
            }
        }

        private static Dictionary<string,object> Clone(Dictionary<string, object> properties)
        {
            var newProperties = new Dictionary<string, object>();
            foreach(var item in properties)
            {
                newProperties.Add(item.Key, item.Value);
            }
            return newProperties;
        }

        private static List<ThGeometry> HandleInsertBeam(List<ThGeometry> beamGeos,DBObjectCollection beamMarks)
        {
            var handler = new ThInsertBeamHandler(beamGeos, beamMarks);
            handler.Handle();
            return MergeBeams(beamGeos);
        }

        private static List<ThGeometry> MergeBeams(List<ThGeometry> linearBeamGeos)
        {
            var results = new List<ThGeometry>();
            var beamGroups = GroupBeamByLineType(linearBeamGeos);
            beamGroups.ForEach(g =>
            {
                var beamLines = g.Select(o => o.Boundary).OfType<Line>().Where(o=>o.Length>0.0).ToCollection();
                var newBeamLines = beamLines.CollinearMerge();
                newBeamLines.OfType<Line>().ForEach(l => results.Add(ThGeometry.Create(l, Clone(g.First().Properties))));
            });
            return results;
        }

        private static List<ThGeometry> HandleShortBeams(List<ThGeometry> linearBeamGeos)
        {
            var service = new ThAdjustShortBeamLTypeService(linearBeamGeos);
            service.Adjust();
            return MergeBeams(linearBeamGeos);
        }

        private static List<List<ThGeometry>> GroupBeamByLineType(List<ThGeometry> beamGeos)
        {
            var results = new List<List<ThGeometry>>();
            var groups = beamGeos.GroupBy(o => o.Properties.GetLineType());
            foreach (var group in groups)
            {
                results.Add(group.ToList());
            }
            return results;
        }
    }
}
