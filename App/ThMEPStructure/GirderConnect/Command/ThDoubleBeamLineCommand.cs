using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPStructure.GirderConnect.ConnectMainBeam.BuildMainBeam;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.BuildSecondaryBeam;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

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
                var pts = GetRangePoints();
                if (pts.Count == 0)
                {
                    return;
                }
                var options = new PromptKeywordOptions("\n请选择处理方式:");
                options.Keywords.Add("地下室中板", "Z", "地下室中板(Z)");
                options.Keywords.Add("地下室顶板", "D", "地下室顶板(D)");
                options.Keywords.Default = "地下室中板";
                var result = Active.Editor.GetKeywords(options);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var UserChoice = result.StringResult;

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

                //ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pts[0]);
                //暂时不处理超远问题，因为目前主梁有一些问题，增加了一些不必要的后处理
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                originTransformer.Transform(intersectCollection);
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                Polyline polyline = pts.CreatePolyline();
                originTransformer.Transform(polyline);
                //获取主梁线
                var beamLine = getPrimitivesService.GetBeamLine(polyline);
                if (UserChoice == "地下室顶板")
                {
                    //主梁
                    BuildMainBeam buildMainBeam = new BuildMainBeam(beamLine,new List<Line>(), intersectCollection);
                    var mainBeams = buildMainBeam.Build(result.StringResult);
                    if(beamLine.Count * 2 == mainBeams.Count)
                    {
                        //理论上 双线是原本单线的二倍
                        //还要执行完后把原本主梁线删除，测试阶段暂时先不处理
                    }
                    foreach (var beam in mainBeams)
                    {
                        //beam.Layer = layerName;
                        beam.ColorIndex = 130;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(beam);
                    }
                }
                else if (UserChoice == "地下室中板")
                {
                    //获取次梁线
                    var secondaryBeamLine = getPrimitivesService.GetSecondaryBeamLine(polyline);
                    ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(secondaryBeamLine.Select(o => o.ExtendLine(100)).ToCollection());

                    var beamLineForOwner = beamLine.Where(o => spatialIndex.SelectFence(o).Count > 0).ToList();
                    var beamLineForSecondaryBeam = beamLine.Except(beamLineForOwner).ToList();
                    BuildMainBeam buildMainBeam = new BuildMainBeam(beamLineForOwner, beamLineForSecondaryBeam, intersectCollection);
                    var mainBeams = buildMainBeam.Build(result.StringResult);
                    if (beamLine.Count * 2 == mainBeams.Count)
                    {
                        //理论上 双线是原本单线的二倍
                        //还要执行完后把原本主梁线删除，测试阶段暂时先不处理
                    }

                    BuildSecondaryBeam buildSecondaryBeam = new BuildSecondaryBeam(secondaryBeamLine, mainBeams.ToCollection());
                    var secondartBeams = buildSecondaryBeam.Build();
                    if (secondaryBeamLine.Count * 2 == secondartBeams.Count)
                    {
                        //理论上 双线是原本单线的二倍
                        //还要执行完后把原本次梁线删除，测试阶段暂时先不处理
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
