using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThMEPWSS.DrainageGeneralPlan.Utils;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThCADCore.NTS;
using NPOI.SS.Formula.PTG;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using ThMEPWSS.Pipe.Model;

using System.Windows.Documents;


namespace ThMEPWSS.DrainageGeneralPlan.Service
{
    enum Direction { none, left,right}
    /// <summary>
    /// 预处理
    /// </summary>
    public class ThDrainageGeneralPlanPreService
    {
        /// <summary>
        /// 提取干管-提取所有polyline和line，输出line
        /// </summary>
        /// <param name="database"></param>
        /// <param name="LayerName"></param>
        /// <returns>所有干管</returns>
        public static List<Line> ExtractMainPiPe(Database database, string LayerName)
        {
            var tempLine = new List<Line>();

            //Line
            var lineService = new ThExtractLineService()
            {
                ElementLayer = LayerName,
            };
            lineService.Extract(database, new Point3dCollection());
            foreach (var i in lineService.Lines)
            {
                i.ProjectOntoXYPlane();//拍平
                tempLine.Add(i);
            }
           
            //PolyLine
            var polyService = new ThExtractPolylineService()
            {
                ElementLayer = LayerName,
            };
            polyService.Extract(database, new Point3dCollection());
            //polyline拆成line
            foreach(var i in polyService.Polys)
            {
                var list=i.ToLines();
                foreach (var j in list)
                {
                    j.ProjectOntoXYPlane();//拍平
                    tempLine.Add(j);
                }
               

                /*for (int j = 0; j < i.NumberOfVertices; j++)//所有点
                {
                    pList.Add(new Point3d(i.GetPoint3dAt(0).X, i.GetPoint3dAt(0).Y, 0));
                }
                for(int j = 0; j < pList.Count-1; j++)//转成line
                {
                    Point3d p1 = new Point3d(i.GetPoint3dAt(j).X, i.GetPoint3dAt(j).Y, 0);
                    Point3d p2 = new Point3d(i.GetPoint3dAt(j+1).X, i.GetPoint3dAt(j+1).Y, 0);
                    Line n = new Line(p1, p2);
                    n.Layer = LayerName;
                    tempLine.Add(n);
                }*/
            }


            return tempLine;
        }

        /// <summary>
        /// 提取出管-提取所有polyline和line，输出polyline
        /// </summary>
        /// <param name="database"></param>
        /// <param name="LayerName"></param>
        /// <returns></returns>
        public static List<Polyline> ExtractOutPiPe(Database database, string LayerName)
        {
            var tempLine = new List<Polyline>();
            var outList = new List<Polyline>();

            //polyLine
            var polyService = new ThExtractPolylineService()
            {
                ElementLayer = LayerName,
            };
            polyService.Extract(database, new Point3dCollection());
            foreach (var i in polyService.Polys)
            {
                i.ProjectOntoXYPlane();//拍平
                tempLine.Add(i);
            }

            //Line
            var lineService = new ThExtractLineService()
            {
                ElementLayer = LayerName,
            };
            lineService.Extract(database, new Point3dCollection());
            foreach (var i in lineService.Lines)
            {
                var p = i.ToPolyline();
                p.ProjectOntoXYPlane();//拍平
                tempLine.Add(p);
            }

            //合并
            var pipeIndex = new ThCADCoreNTSSpatialIndex(tempLine.ToCollection());//pline索引
            while(tempLine.Count>0)
            {
                var line = tempLine[0];//每次拿出一个
                var lBuffer = line.BufferPL(ThDrainageGeneralPlanCommon.OutOutRange)[0] as Polyline;//寻找可以合成polyline的line
                var nearLine = pipeIndex.SelectCrossingPolygon(lBuffer);
                nearLine = checkBuffer(nearLine, line);//删掉不应该被组合的线
                if (nearLine.Count > 1)//包含别的干管
                {
                    var temp = MergeLine(nearLine);
                    tempLine.Add(temp);
                    foreach(var i in nearLine)
                    {
                        var p = i as Polyline;
                        tempLine.Remove(p);
                    }
                    pipeIndex = new ThCADCoreNTSSpatialIndex(tempLine.ToCollection());//pline索引
                }
                else
                {
                    outList.Add(line);//已经找不到了，放入
                    tempLine.RemoveAt(0);
                }
            }
           
            return outList;
        }

        /// <summary>
        /// 判断index得到的线是否应该合成polyline
        /// </summary>
        /// <param name="list"></param>
        private static DBObjectCollection checkBuffer(DBObjectCollection list,Polyline origin)
        {
            Tolerance t = new Tolerance(ThDrainageGeneralPlanCommon.OutOutRange, ThDrainageGeneralPlanCommon.OutOutRange);
            List<Point3d> point3Ds = new List<Point3d>();
            DBObjectCollection temp = new DBObjectCollection();
            for (int i=0;i< origin.NumberOfVertices; i++)
            {
                point3Ds.Add(origin.GetPoint3dAt(i));
            }

            for (int i = 0; i < list.Count; i++)
            {
                var line = list[i] as Polyline;
                for(int j = 0; j < line.NumberOfVertices; j++)
                {
                    if (isExist(point3Ds, line.GetPoint3dAt(j), t))
                    {
                        temp.Add(line);
                        break;
                    }  
                }
            }
          
            return temp;
        }
        /// <summary>
        /// 将Line合成PolyLine
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static Polyline MergeLine(DBObjectCollection list)
        {
            List<Point3d> point3Ds = new List<Point3d>();
            Tolerance t = new Tolerance(ThDrainageGeneralPlanCommon.OutOutRange, ThDrainageGeneralPlanCommon.OutOutRange);
            var pl = list[0] as Polyline;
            point3Ds.Add(pl.StartPoint);
            point3Ds.Add(pl.EndPoint);
            for (int i=1;i<list.Count;i++)
            {
                var l= list[i] as Polyline;
                if (!isExist(point3Ds, l.StartPoint, t))
                {
                    point3Ds.Add(l.StartPoint);
                }
                if (!isExist(point3Ds, l.EndPoint, t))
                {
                    point3Ds.Add(l.EndPoint);
                }
            }

            Polyline frame = new Polyline { Closed = false };
            for(int i=0;i<point3Ds.Count;i++)
            {
                frame.AddVertexAt(i, point3Ds[i].ToPoint2D(), 0, 0, 0);
            }

            return frame;
        }

        /// <summary>
        /// pt是否在list里，精度为0.01
        /// </summary>
        /// <param name="list"></param>
        /// <param name="pt"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool isExist(List<Point3d> list, Point3d pt, Tolerance t)
        {
            foreach (var i in list)
            {
                if (i.IsEqualTo(pt, t))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 提取多段线
        /// </summary>
        /// <param name="database"></param>
        /// <param name="LayerName"></param>
        /// <returns></returns>
        public static List<Polyline> ExtractLayerPolyline(Database database, string LayerName)
        {
            var polylineList = new List<Polyline>();
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = LayerName,
            };
            extractService.Extract(database, new Point3dCollection());
            /*var tempPoly = new List<Polyline>();
            tempPoly.AddRange(extractService.Polys);

            foreach (var pl in tempPoly)
            {
                //var plTemp = ThHVACHandleNonClosedPolylineService.Handle(pl);
                var plTemp = pl;
                plTemp.DPSimplify(1);
                if (plTemp.Closed == false)
                {
                    //plTemp = plTemp.BufferPL(1).OfType<Polyline>().FirstOrDefault();
                }
                if (plTemp != null)
                {
                    polylineList.Add(plTemp);
                }
            }*/

            return extractService.Polys;
            //return tempPoly;
        }

        /// <summary>
        /// 提取线段
        /// </summary>
        /// <param name="database"></param>
        /// <param name="LayerName"></param>
        /// <returns></returns>
        public static List<Line> ExtractLayerLine(Database database, string LayerName)
        {
            var lineList = new List<Polyline>();
            var extractService = new ThExtractLineService()
            {
                ElementLayer = LayerName,
            };
            extractService.Extract(database, new Point3dCollection());
            //var tempLine = new List<Line>();
            //tempLine.AddRange(extractService.Lines);

       
            //lineList.AddRange(tempLine.Select(x => x.BufferSquare(1)).ToList());

            //return lineList;
            return extractService.Lines;
        }



        public static void LoadBlockLayerToDocument(Database database, List<string> blockNames, List<string> layerNames)
        {
            //插入模版图块时调用了WblockCloneObjects方法。需要之后做QueueForGraphicsFlush更新transaction。并且最后commit此transaction
            //参考
            //https://adndevblog.typepad.com/autocad/2015/01/using-wblockcloneobjects-copied-modelspace-entities-disappear-in-the-current-drawing.html

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LoadBlockLayerToDocumentWithoutTrans(database, blockNames, layerNames);
                transaction.TransactionManager.QueueForGraphicsFlush();
                transaction.Commit();
            }
        }

        private static void LoadBlockLayerToDocumentWithoutTrans(Database database, List<string> blockNames, List<string> layerNames)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                //解锁0图层，后面块有用0图层的
                DbHelper.EnsureLayerOn("0");
                DbHelper.EnsureLayerOn("DEFPOINTS");
            }
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.PSZTPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, true);
                }

                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);

                    LayerTools.UnLockLayer(database, item);
                    LayerTools.UnFrozenLayer(database, item);
                    LayerTools.UnOffLayer(database, item);
                    LayerTools.SetPrintLayer(database, item, layer.IsPlottable);

                    /*ThFloorHeatingCommon.LayerLineType.TryGetValue(item, out var lineType);
                    if (lineType == null || lineType == "")
                    {
                        continue;
                    }
                    var lineTypesTemplate = blockDb.Linetypes.ElementOrDefault(lineType);
                    if (null == lineTypesTemplate)
                        continue;
                    currentDb.Linetypes.Import(lineTypesTemplate, true);*/

                }
            }
        }

    }

    
}
