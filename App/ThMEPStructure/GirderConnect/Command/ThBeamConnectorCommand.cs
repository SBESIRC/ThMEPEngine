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
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;
using ThMEPEngineCore.BeamInfo.Business;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPStructure.GirderConnect.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.GirderConnect.Command
{
    public class ThBeamConnectorCommand : ThMEPBaseCommand, IDisposable
    {
        private DBObjectCollection results;

        public ThBeamConnectorCommand()
        {
            ActionName = "生成主梁";
            CommandName = "THZLSC";
        }

        public void Dispose()
        {

        }

        public override void SubExecute()
        {
#if (ACAD2016 || ACAD2018)
            using (var acdb = AcadDatabase.Active())
            //using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                // 选择范围
                var pts = new Point3dCollection();
                if (MainBeamLayoutConfig.RegionSelection == true)
                {
                    pts = GetRangePoints();
                }
                else
                {
                    using (var pc = new PointCollector(PointCollector.Shape.Polygon, new List<string>()))
                    {
                        try
                        {
                            pc.Collect();
                        }
                        catch
                        {
                            return;
                        }
                        pts = pc.CollectedPoints.Cast<Point3d>().ToCollection();
                    }
                }
                if(pts.Count == 0)
                {
                    return;
                }
                //接入数据
                var dataFactory = new ThBeamConnectorDataFactory();
                dataFactory.Create(acdb.Database, pts);
                var columns = dataFactory.Columns;
                var shearwalls = dataFactory.Shearwalls;
                var buildings = dataFactory.MainBuildings;

                // 支持超远，将输入数据移动到近似原点处
                var center = pts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(center);
                transformer.Transform(columns);
                transformer.Transform(shearwalls);
                transformer.Transform(buildings);

                ThBeamGeometryPreprocessor.Z0Curves(ref columns);
                ThBeamGeometryPreprocessor.Z0Curves(ref shearwalls);
                ThBeamGeometryPreprocessor.Z0Curves(ref buildings);
                var mainBuildings = buildings.OfType<Entity>().ToList();

                //分组 
                var columnGroupService = new ThGroupService(mainBuildings, columns);
                var columnGroupDict = columnGroupService.Groups;
                var outsideColumns = columnGroupService.OutsideObjs;

                var shearwallGroupService = new ThGroupService(mainBuildings, shearwalls);
                var shearwallGroupDict = shearwallGroupService.Groups;
                var outsideShearwall = shearwallGroupService.OutsideObjs;

                Point3dCollection clumnPts = new Point3dCollection();
                var outlineWalls = new Dictionary<Polyline, HashSet<Polyline>>();
                var outlineClumns = new Dictionary<Polyline, HashSet<Point3d>>();
                var outerWalls = new Dictionary<Polyline, HashSet<Polyline>>();
                var olCrossPts = new Dictionary<Polyline, HashSet<Point3d>>();
                var outline2OriOutline = new Dictionary<Polyline, Polyline>();
                var allColumnPts = new HashSet<Point3d>();

                //处理输入
                MainBeamPreProcess.MPreProcess(outsideColumns, shearwallGroupDict, columnGroupDict, outsideShearwall,
                    clumnPts, ref outlineWalls, outlineClumns, ref outerWalls, ref olCrossPts, ref outline2OriOutline, ref allColumnPts);

                //计算
                var connectService = new Connect();
                connectService.SimilarAngle = MainBeamLayoutConfig.SimilarAngle;
                connectService.SimilarPointsDis = MainBeamLayoutConfig.SimilarPointsDis;
                connectService.SamePointsDis = MainBeamLayoutConfig.SamePointsDis;
                connectService.MaxBeamLength = MainBeamLayoutConfig.MaxBeamLength;
                connectService.SplitArea = MainBeamLayoutConfig.SplitSelection == true ? MainBeamLayoutConfig.SplitArea : 0;

                var dicTuples = connectService.Calculate(clumnPts, outlineWalls, outlineClumns, outerWalls, outline2OriOutline, ref olCrossPts, transformer, allColumnPts);

                DBObjectCollection intersectCollection = new DBObjectCollection();
                outlineWalls.ForEach(o => intersectCollection.Add(o.Key));
                outlineWalls.ForEach(o => o.Value.ForEach(p => intersectCollection.Add(p)));
                outerWalls.ForEach(o => intersectCollection.Add(o.Key));
                outsideColumns.ForEach(o => intersectCollection.Add(o as Polyline));

                //处理输出
                var lines = MainBeamPostProcess.MPostProcess(dicTuples, intersectCollection);

                //还原到原始位置
                lines.ForEach(o => transformer.Reset(o));
                transformer.Reset(columns);
                transformer.Reset(shearwalls);
                transformer.Reset(buildings);

                //打印到Cad                
                ImportService.ImportMainBeamInfo(); //导入主梁信息
                double standardLength = MainBeamLayoutConfig.OverLengthSelection == true ? MainBeamLayoutConfig.OverLength : 0;
                MainBeamPostProcess.Output(lines, standardLength);
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
