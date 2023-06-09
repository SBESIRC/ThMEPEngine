﻿using System;
using System.Collections.Generic;
using Linq2Acad;
using NFox.Cad;
using AcHelper;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Data
{
    public class LayerDealer
    {
        /// <summary>
        /// 输出结果，转换结果作为次梁输入
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="layerName"></param>
        public static void Output(HashSet<Tuple<Point3d, Point3d>> tuples, string layerName, ThMEPOriginTransformer transformer)
        {
            using (var acdb = AcadDatabase.Active())
            {
                tuples.ForEach(o =>
                {
                    var line = new Line(o.Item1, o.Item2);
                    transformer.Reset(line);
                    line.Layer = layerName;
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
                acdb.Database.UnPrintLayer(layerName);
            }
        }

        public static void HiddenLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.OffLayer(layerName);
                acdb.Database.FrozenLayer(layerName);
            }
        }
    }
}
