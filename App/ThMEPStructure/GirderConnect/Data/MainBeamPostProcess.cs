using System;
using System.Collections.Generic;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
using AcHelper;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;

namespace ThMEPStructure.GirderConnect.Data
{
    class MainBeamPostProcess
    {
        /// <summary>
        /// 对主梁连接算法结果的后续处理
        /// </summary>
        public static void MPostProcess(Dictionary<Point3d, HashSet<Point3d>> dicTuples, DBObjectCollection intersectCollection)
        {
            //var unifiedTyples = UnifyTuples(dicTuples);
            var tuples = LineDealer.DicTuplesToTuples(dicTuples);
            var lines = TuplesToLines(tuples);
            Output(lines);
        }
        public static void Output(List<Line> lines)
        {
            using (var acdb = AcadDatabase.Active())
            {
                lines.ForEach(line =>
                {
                    line.Layer = BeamConfig.MainBeamLayerName;
                    if (line.Length > 9000) { line.ColorIndex = 7; }
                    else { line.ColorIndex = (int)ColorIndex.BYLAYER; }
                    line.Linetype = "ByLayer";
                    acdb.ModelSpace.Add(line);
                });
            }
        }

        /// <summary>
        /// DCEL的双向线转换为单线
        /// </summary>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static HashSet<Tuple<Point3d, Point3d>> UnifyTuples(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var ansTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var kv in dicTuples)
            {
                var ptSet = kv.Value;
                foreach(var pt in ptSet)
                {
                    if (kv.Key.DistanceTo(pt) <= 10) continue;

                    var positiveTuple = new Tuple<Point3d, Point3d>(kv.Key, pt);
                    var negativeTuple = new Tuple<Point3d, Point3d>(pt, kv.Key);

                    if (!ansTuples.Contains(positiveTuple) && !ansTuples.Contains(negativeTuple))
                    {
                        ansTuples.Add(positiveTuple);
                    }
                }
            }
            return ansTuples;
        }

        public static List<Line> TuplesToLines(HashSet<Tuple<Point3d, Point3d>> tuples)
        {
            List<Line> lines = new List<Line>();
            foreach(var tuple in tuples)
            {
                lines.Add(new Line(tuple.Item1, tuple.Item2));
            }
            lines = lines.Where(o => o.Length > 10).ToList();
            var linesObjs = lines.ToCollection();
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(linesObjs);
            var newLines = new List<Line>();
            lines.ForEach(o=>
            {
                try
                {
                    var polylione = o.ExtendLine(1).Buffer(1);
                    var objs = spatialIndex.SelectWindowPolygon(polylione);
                    newLines.Add(objs.Cast<Line>().OrderBy(x => x.Length).First());
                }
                catch(Exception ex)
                {

                }
            });
            return newLines.Distinct().ToList();
        }
    }
}
