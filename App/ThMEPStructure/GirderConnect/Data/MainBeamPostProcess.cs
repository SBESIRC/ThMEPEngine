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

namespace ThMEPStructure.GirderConnect.Data
{
    class MainBeamPostProcess
    {
        /// <summary>
        /// 对主梁连接算法结果的后续处理
        /// </summary>
        public static void MPostProcess(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            string beamLayer = "TH_AI_BEAM";
            AddLayer(beamLayer, 4);

            var unifiedTyples = UnifyTuples(dicTuples);
            Output(unifiedTyples, beamLayer);
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

        /// <summary>
        /// DCEL的双向线转换为单线
        /// </summary>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static HashSet<Tuple<Point3d, Point3d>> UnifyTuples(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var ansTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                foreach(var pt in dicTuple.Value)
                {
                    var tuple = new Tuple<Point3d, Point3d>(dicTuple.Key, pt);
                    var converseTuple = new Tuple<Point3d, Point3d>(pt, dicTuple.Key);
                    if (!ansTuples.Contains(converseTuple))
                    {
                        ansTuples.Add(tuple);
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
    }
}
