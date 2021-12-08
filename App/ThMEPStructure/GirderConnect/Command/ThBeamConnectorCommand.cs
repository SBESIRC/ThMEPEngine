using System;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;

namespace ThMEPStructure.GirderConnect.Command
{
    public class ThBeamConnectorCommand : ThMEPBaseCommand, IDisposable
    {
        public ThBeamConnectorCommand()
        {
            ActionName = "生成主梁";
            CommandName = "THSCZL";
        }

        public void Dispose()
        {

        }

        public override void SubExecute()
        {
#if (ACAD2016 || ACAD2018)
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
                //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(columns.OfType<Entity>().ToList(), acdb.Database, 5);
                //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(shearwalls.OfType<Entity>().ToList(), acdb.Database, 6);
                //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(mainBuildings, acdb.Database, 7);

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

                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(outlineWalls.SelectMany(o=>o.Value.ToList()).OfType<Entity>().ToList(), acdb.Database, 1);

                //计算
                var dicTuples = Connect.Calculate(clumnPts, outlineWalls, outlineClumns, acdb);

                //处理算法输出
                MainBeamPostProcess.MPostProcess(dicTuples);
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
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
