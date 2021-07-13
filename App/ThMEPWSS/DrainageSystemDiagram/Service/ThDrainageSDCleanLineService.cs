using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCleanLineService
    {
        public static List<Line> simplifyLine(List<Line> allPipeOri)
        {
            var lines = new List<Line>();

            var simplifiedLines = ThMEPLineExtension.LineSimplifier(allPipeOri.ToCollection(), 500, 20, 2, Math.PI / 180.0);
            DrawUtils.ShowGeometry(simplifiedLines, "l061link", 0);


            lines = breakLine(simplifiedLines);
            DrawUtils.ShowGeometry(lines, "l062link", 1);

            return lines;
        }

        public static List<Line> simplifyLineTest(List<Line> allPipeOri)
        {
            var lines = new List<Line>();

            var pipeCurve = new List<Curve>();
            pipeCurve.AddRange(allPipeOri);

            var simplifiedLines = simplifiyLine2(pipeCurve);
            lines = simplifiedLines;
            //DrawUtils.ShowGeometry(simplifiedLines, "l061link", 0);


            //lines = breakLine(simplifiedLines);
            //DrawUtils.ShowGeometry(lines, "l29link", 190);


            return lines;
        }


        private static List<Line> simplifiyLine2(List<Curve> allPipeOri)
        {
            var lines = new List<Line>();
            var curves = allPipeOri.ToCollection();

            // 配置参数
            ThLaneLineEngine.extend_distance = 1;
            ThLaneLineEngine.collinear_gap_distance = 2;

            // 合并处理
            var mergedLines = ThLaneLineEngine.Explode(curves);
            mergedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l21link", 1);
            lines.Clear();

            mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
            mergedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l22link", 2);
            lines.Clear();

            mergedLines = ThLaneLineEngine.Noding(mergedLines);
            mergedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l23link", 3);
            lines.Clear();

            mergedLines = ThLaneLineEngine.CleanZeroCurves(mergedLines);
            mergedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l24link", 4);
            lines.Clear();

            // 延伸处理
            var extendedLines = ThLaneLineExtendEngine.Extend(mergedLines);
            extendedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l25link", 5);
            lines.Clear();

            //extendedLines = ThLaneLineMergeExtension.Merge(extendedLines);
            //extendedLines.Cast<Line>().ForEach(x => lines.Add(x));
            //DrawUtils.ShowGeometry(lines, "l26link", 6);
            //lines.Clear();

            extendedLines = ThLaneLineEngine.Noding(extendedLines);
            extendedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l27link", 7);
            lines.Clear();

            extendedLines = ThLaneLineEngine.CleanZeroCurves(extendedLines);
            extendedLines.Cast<Line>().ForEach(x => lines.Add(x));
            DrawUtils.ShowGeometry(lines, "l28link", 40);


            return lines;
        }

        private static List<Line> breakLine(List<Line> lines)
        {
            var objs = new DBObjectCollection();
            lines.ForEach(x => objs.Add(x));
            var nodeGeo = objs.ToNTSNodedLineStrings();
            var brokeLines = new List<Line>();
            if (nodeGeo != null)
            {
                brokeLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .Where(x => x.Length > 2)
                .ToList();
            }

            return brokeLines;
        }


        public static void cleanNoUseLines(List<Point3d> pts, ref List<Line> lines)
        {
            var ptDict = getPtCount(lines);

            var pt0 = ptDict.Where(x => x.Value.Count == 1);
            var tol = new Tolerance(10, 10);

            var ptClean = new List<Line>();

            foreach (var pt in pt0)
            {
                var isSupplyPt = pts.Where(p => pt.Key.IsEqualTo(p, tol));
                if (isSupplyPt.Count() == 0)
                {
                    ptClean.AddRange(pt.Value);
                }
            }


            lines.RemoveAll(l => ptClean.Contains(l));

        }

        private static Dictionary<Point3d, List<Line>> getPtCount(List<Line> lines)
        {
            var tol = new Tolerance(10, 10);
            var ptDict = new Dictionary<Point3d, List<Line>>();
            foreach (var l in lines)
            {
                var pt = l.StartPoint;
                var ptD = ptDict.Where(x => x.Key.IsEqualTo(pt, tol));
                if (ptD.Count() == 0)
                {
                    ptDict.Add(pt, new List<Line> { l });
                }
                else
                {
                    ptDict[ptD.First().Key].Add(l);
                }

                pt = l.EndPoint;
                ptD = ptDict.Where(x => x.Key.IsEqualTo(pt, tol));
                if (ptD.Count() == 0)
                {
                    ptDict.Add(pt, new List<Line> { l });
                }
                else
                {
                    ptDict[ptD.First().Key].Add(l);
                }
            }

            return ptDict;
        }




    }
}
