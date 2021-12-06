﻿using System;
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
using NFox.Cad;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Data
{
    class LayerDealer
    {
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
                    //if (line.Length > 9000)
                    //{
                    //    line.ColorIndex = 3;
                    //}
                    //else
                    {
                        line.ColorIndex = (int)ColorIndex.BYLAYER;
                    }
                    HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                });
            }
        }
        public static void Output(Dictionary<Point3d, Point3d> tuples, string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                tuples.ForEach(o =>
                {
                    var line = new Line(o.Key, o.Value);
                    line.Layer = layerName;
                    //if (line.Length > 9000)
                    //{
                    //    line.ColorIndex = 6;
                    //}
                    //else
                    {
                        line.ColorIndex = (int)ColorIndex.BYLAYER;
                    }
                    HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
                });
            }
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
