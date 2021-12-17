using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Service;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Geometries;
using NFox.Cad;
using ThMEPStructure.GirderConnect.ConnectMainBeam.BuildMainBeam;

namespace ThMEPStructure.GirderConnect.Data
{
    class MainBeamPostProcess
    {
        /// <summary>
        /// 对主梁连接算法结果的后续处理
        /// </summary>
        public static void MPostProcess(Dictionary<Point3d, HashSet<Point3d>> dicTuples, DBObjectCollection intersectCollection)
        {
            string beamLayer = "TH_AI_BEAM";
            AddLayer(beamLayer, 4);

            //var unifiedTyples = UnifyTuples(dicTuples);
            var tuples = LineDealer.DicTuplesToTuples(dicTuples);
            var lines = TuplesToLines(tuples);
            Output(lines, beamLayer, intersectCollection);
        }

        /// <summary>
        /// 输出结果，转换结果作为次梁输入
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="layerName"></param>
        public static void Output(HashSet<Tuple<Point3d, Point3d>> tuples, string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                tuples.ForEach(o =>
                {
                    var line = new Line(o.Item1, o.Item2);
                    line.Layer = layerName;
                    if(line.Length < 18000 && line.Length > 9000)
                    {
                        line.ColorIndex = 7;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                    }
                    else if (line.Length > 10 && line.Length <= 9000)
                    {
                        line.ColorIndex = (int)ColorIndex.BYLAYER;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                    }
                });
            }
        }
        public static void Output(Dictionary<Point3d, HashSet<Point3d>> tuples, string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                tuples.ForEach(o =>
                {
                    o.Value.ForEach(k =>
                    {
                        var line = new Line(o.Key, k);
                        line.Layer = layerName;
                        if (line.Length > 9000)
                        {
                            line.ColorIndex = 7;
                        }
                        else
                        {
                            line.ColorIndex = (int)ColorIndex.BYLAYER;
                        }
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                    });
                });
            }
        }
        public static void Output(List<Line> lines, string layerName, DBObjectCollection intersectCollection)
        {
            using (var acdb = AcadDatabase.Active())
            {
                //BuildMainBeam buildMainBeam = new BuildMainBeam(lines, intersectCollection);
                //var mainBeams = buildMainBeam.Build("地下室顶板");
                //foreach (var beam in mainBeams)
                //{
                //    beam.Layer = layerName;
                //    beam.ColorIndex = 130;
                //    HostApplicationServices.WorkingDatabase.AddToModelSpace(beam);
                //}
                lines.ForEach(line =>
                {
                    line.Layer = layerName;
                    if (line.Length < 18000 && line.Length > 9000)
                    {
                        line.ColorIndex = 7;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                    }
                    else if (line.Length > 10 && line.Length <= 9000)
                    {
                        line.ColorIndex = (int)ColorIndex.BYLAYER;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                    }
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

        /// <summary>
        /// 创建一个新的图层
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="colorIndex"></param>
        public static void AddLayer(string layerName, short colorIndex)
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(layerName))
                {
                    acdb.Database.AddLayer(layerName);
                    acdb.Database.SetLayerColor(layerName, colorIndex);
                }
                acdb.Database.UnLockLayer(layerName);
                acdb.Database.UnOffLayer(layerName);
                acdb.Database.UnFrozenLayer(layerName);
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
