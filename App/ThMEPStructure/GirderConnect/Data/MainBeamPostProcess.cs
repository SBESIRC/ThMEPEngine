using System;
using System.Collections.Generic;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
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
        public static List<Line> MPostProcess(Dictionary<Point3d, HashSet<Point3d>> dicTuples, DBObjectCollection intersectCollection)
        {
            //var unifiedTyples = UnifyTuples(dicTuples);
            var tuples = TypeConvertor.DicTuples2Tuples(dicTuples);
            return TuplesToLines(tuples);
        }

        public static void Output(List<Line> lines, double standardLength)
        {
            using (var acdb = AcadDatabase.Active())
            {
                lines.ForEach(line =>
                {
                    if (line.Length < 18000) {
                        line.Layer = BeamConfig.MainBeamLayerName;
                        if (standardLength == 0) { line.ColorIndex = 1; }
                        else
                        {
                            if (line.Length > standardLength) { line.ColorIndex = 1; }
                            else { line.ColorIndex = (int)ColorIndex.BYLAYER; }
                        }
                        line.Linetype = "ByLayer";
                        acdb.ModelSpace.Add(line);
                    }
                });
            }
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
