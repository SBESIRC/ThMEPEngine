using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
using ThMEPEngineCore.Command;
using NFox.Cad;

namespace ThMEPStructure.GirderConnect.Command
{
    internal class ThBeamConnectorCommand : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {

        }

        public override void SubExecute()
        {
            using (var acdb = AcadDatabase.Active())
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                // 选择范围
                var pts = GetRangePoints();

                //接入数据
                var dataFactory = new ThBeamConnectorDataFactory();
                dataFactory.Create(acdb.Database, pts);
                var columns = dataFactory.Columns;
                var shearwalls = dataFactory.Shearwalls;
                var mainBuildings = dataFactory.MainBuildings.OfType<Entity>().ToList();

                // print extract data
                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(columns.OfType<Entity>().ToList(), acdb.Database, 5);
                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(shearwalls.OfType<Entity>().ToList(), acdb.Database, 6);
                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(mainBuildings, acdb.Database, 7);

                // 分组 
                var columnGroupService = new ThGroupService(mainBuildings, columns);
                var columnGroupDict = columnGroupService.Groups;
                var outsideColumns = columnGroupService.OutsideObjs;

                var shearwallGroupService = new ThGroupService(mainBuildings, shearwalls);
                var shearwallGroupDict = shearwallGroupService.Groups;
                var outsideShearwall = shearwallGroupService.OutsideObjs;
                
                Point3dCollection clumnPts = new Point3dCollection();
                var outlineWalls = new Dictionary<Polyline, HashSet<Polyline>>();
                var outlineClumns = new Dictionary<Polyline, HashSet<Point3d>>();

                //处理算法输入
                MainBeamPreProcess.MPreProcess(outsideColumns, shearwallGroupDict, columnGroupDict, 
                    outsideShearwall, clumnPts, ref outlineWalls, outlineClumns);

                //计算
                var tuples = Connect.Calculate(clumnPts, outlineWalls, outlineClumns, acdb);

                //处理算法输出
                MainBeamPostProcess.MPostProcess(tuples);

                #region pretest
                ////获取柱点
                //var clumnPts = GetObject.GetCenters(acdb);
                //Dictionary<Polyline, List<Polyline>> outlineWalls = new Dictionary<Polyline, List<Polyline>>();
                //Dictionary<Polyline, HashSet<Point3d>> outlineClumns = new Dictionary<Polyline, HashSet<Point3d>>();
                //for (int i = 0; i < 6; ++i)
                //{
                //    //获取某个墙外边框
                //    Polyline outline = GetObject.GetPolyline(acdb);
                //    if (outline == null)
                //    {
                //        return;
                //    }
                //    if (!outlineClumns.ContainsKey(outline))
                //    {
                //        outlineClumns.Add(outline, new HashSet<Point3d>());
                //    }
                //    //获取此多边形包含的墙
                //    List<Polyline> walls = GetObject.GetPolylines(acdb);
                //    if (walls == null)
                //    {
                //        return;
                //    }
                //    outlineWalls.Add(outline, walls);
                //}
                //GetObject.FindPointsInOutline(clumnPts, outlineClumns);

                ////预处理
                //Point3dCollection ptsInOutline = new Point3dCollection();
                //foreach (var sets in outlineClumns.Values)
                //{
                //    foreach (Point3d pt in sets)
                //    {
                //        ptsInOutline.Add(pt);
                //    }
                //}
                //Point3dCollection newClumnPts = PointsDealer.RemoveSimmilerPoint(clumnPts, ptsInOutline);

                ////计算
                //Connect.Calculate(newClumnPts, outlineWalls, outlineClumns, acdb);
                #endregion
            }
        }

        /// <summary>
        /// 框取范围
        /// </summary>
        /// <returns></returns>
        private Point3dCollection GetRangePoints()
        {
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }
    }
}
