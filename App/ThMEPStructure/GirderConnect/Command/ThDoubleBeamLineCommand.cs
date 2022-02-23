using System;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPEngineCore.BeamInfo.Business;
using ThMEPStructure.GirderConnect.Data;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;
using ThMEPStructure.GirderConnect.Service;
using ThMEPStructure.GirderConnect.BuildBeam;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;

namespace ThMEPStructure.GirderConnect.Command
{
    public class ThDoubleBeamLineCommand : ThMEPBaseCommand, IDisposable
    {
        public ThDoubleBeamLineCommand()
        {
            ActionName = "双线生成";
            CommandName = "THSXSC";
        }

        public void Dispose()
        {

        }

        public override void SubExecute()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                // 选择范围
                var pts = new Point3dCollection();
                if (BuildBeamLayoutConfig.RegionSelection==1)
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
                if (pts.Count == 0)
                {
                    return;
                }
                //接入数据
                var dataFactory = new ThBeamConnectorDataFactory();
                dataFactory.Create(acad.Database, pts);
                var columns = dataFactory.Columns;
                var shearwalls = dataFactory.Shearwalls;
                var mainBuildings = dataFactory.MainBuildings.OfType<Entity>().ToList();
                var intersectCollection = columns.Union(shearwalls);
                foreach (var item in mainBuildings)
                {
                    intersectCollection.Add(item);
                }
                ThBeamGeometryPreprocessor.Z0Curves(ref intersectCollection);

                //ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pts[0]);
                //暂时不处理超远问题，因为目前主梁有一些问题，增加了一些不必要的后处理
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                originTransformer.Transform(intersectCollection);
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                Polyline polyline = pts.CreatePolyline();
                originTransformer.Transform(polyline);

                //获取主梁线
                var beamLine = getPrimitivesService.GetBeamLine(polyline, out ObjectIdCollection objIDs);

                //导入主梁文字图层
                ImportService.ImportMainBeamInfo();
                //导入归一图层
                ImportService.ImportUniteBeamInfo();
                //导入文字样式
                ImportService.ImportTextStyle();

                LayerDealer.HiddenLayer(BeamConfig.ErrorLayerName);

                bool CreatGroup = true;//是否分组
                if ((BuildBeamLayoutConfig.EstimateSelection == 1 && BuildBeamLayoutConfig.FormulaEstimateSelection == 1) || BuildBeamLayoutConfig.EstimateSelection == 2 && BuildBeamLayoutConfig.TableEstimateSelection == 1)
                {
                    //主梁
                    ThBuildBeam buildMainBeam = new ThBuildBeam(beamLine, new List<Line>(), new List<Line>(), intersectCollection);
                    var mainBeams = buildMainBeam.build(1);
                    if (beamLine.Count == mainBeams.Count)
                    {
                        List<ObjectIdList> Groups = new List<ObjectIdList>();
                        foreach (var beam in mainBeams)
                        {
                            List<Entity> entities = new List<Entity>();
                            entities.Add(beam.Key.Item1);
                            entities.Add(beam.Key.Item2);
                            entities.Add(beam.Value);
                            var groupIds = ConnectSecondaryBeamService.InsertEntity(entities);
                            Groups.Add(groupIds);
                        }

                        //打组
                        if (CreatGroup)
                        {
                            Groups.ForEach(g => GroupTools.CreateGroup(acad.Database, Guid.NewGuid().ToString(), g));
                        }
                        //删掉旧线
                        ConnectSecondaryBeamService.Erase(objIDs);
                    }

                }
                else
                {
                    //导入次梁文字图层
                    ImportService.ImportSecondaryBeamInfo();

                    //获取次梁线
                    var secondaryBeamLine = getPrimitivesService.GetSecondaryBeamLine(polyline, out ObjectIdCollection SecondaryBeamobjIDs);
                    ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(secondaryBeamLine.Select(o => o.ExtendLine(100)).ToCollection());
                    var beamLineForSecondaryBeam = beamLine.Where(o => spatialIndex.SelectFence(o).Count > 0).ToList();
                    var beamLineForOwner = beamLine.Except(beamLineForSecondaryBeam).ToList();

                    ThBuildBeam buildMainBeam = new ThBuildBeam(beamLineForOwner, beamLineForSecondaryBeam, secondaryBeamLine, intersectCollection);
                    var beams = buildMainBeam.build(2);
                    if (beamLine.Count + secondaryBeamLine.Count == beams.Count)
                    {
                        List<ObjectIdList> Groups = new List<ObjectIdList>();
                        foreach (var beam in beams)
                        {
                            List<Entity> entities = new List<Entity>();
                            entities.Add(beam.Key.Item1);
                            entities.Add(beam.Key.Item2);
                            entities.Add(beam.Value);
                            var groupIds = ConnectSecondaryBeamService.InsertEntity(entities);
                            Groups.Add(groupIds);
                        }

                        //打组
                        if (CreatGroup)
                        {
                            Groups.ForEach(g => GroupTools.CreateGroup(acad.Database, Guid.NewGuid().ToString(), g));
                        }
                        //删掉旧线
                        ConnectSecondaryBeamService.Erase(objIDs);
                        ConnectSecondaryBeamService.Erase(SecondaryBeamobjIDs);
                    }
                }
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
