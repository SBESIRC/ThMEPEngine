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
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;
using ThMEPStructure.GirderConnect.Service;

namespace ThMEPStructure.GirderConnect.Command
{
    public class SecondaryBeamConnectCmd : ThMEPBaseCommand, IDisposable
    {
        public SecondaryBeamConnectCmd()
        {
            ActionName = "生成次梁";
            CommandName = "THCLSC";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                // 选择范围
                var pts = new Point3dCollection();
                if (SecondaryBeamLayoutConfig.RegionSelection==1)
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
                if (SecondaryBeamLayoutConfig.FloorSelection == 1)
                {
                    Active.WriteMessage("\n建议采用框架大板、加腋大板方式");
                }
                else if (SecondaryBeamLayoutConfig.FloorSelection == 2)
                {
                    if(SecondaryBeamLayoutConfig.DirectionSelection == 2)
                    {
                        var peo = new PromptEntityOptions("\n拾取次梁方向线");
                        peo.Keywords.Add("选两点", "P", "选两点(P)");
                        PromptEntityResult result = Active.Editor.GetEntity(peo);
                        if (result.Status == PromptStatus.OK)
                        {
                            var Obj = acad.Element<Entity>(result.ObjectId);
                            if(Obj is Line line)
                            {
                                SecondaryBeamLayoutConfig.MainDir = line.LineDirection();
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if(result.Status == PromptStatus.Keyword)
                        {
                            //选择插入点
                            PromptPointOptions options = new PromptPointOptions("请选择方向起始点");
                            var sResult = Active.Editor.GetPoint(options);

                            if (sResult.Status == PromptStatus.OK)
                            {
                                var startPt = sResult.Value;
                                var transPt = startPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                                var endPt = Interaction.GetLineEndPoint("请选择终止点", transPt);

                                if (System.Double.IsNaN(endPt.X) || System.Double.IsNaN(endPt.Y) || System.Double.IsNaN(endPt.Z))
                                {
                                    return;
                                }
                                SecondaryBeamLayoutConfig.MainDir = startPt.GetVectorTo(endPt).GetNormal();
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    //ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pts[0]);
                    //暂时不处理超远问题，因为目前主梁有一些问题，增加了一些不必要的后处理
                    ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                    GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);

                    Polyline polyline = pts.CreatePolyline();
                    originTransformer.Transform(polyline);
                    //获取主梁线和边框线
                    var beamLine = getPrimitivesService.GetBeamLine(polyline, out ObjectIdCollection objs);
                    var wallBound = getPrimitivesService.GetWallBound(polyline);
                    beamLine = beamLine.Union(wallBound).ToList();
                    var houseBound = getPrimitivesService.GetHouseBound(polyline);

                    //导入图层
                    ImportService.ImportSecondaryBeamInfo();

                    //次梁计算
                    var SecondaryBeamLines = ConnectSecondaryBeamService.ConnectSecondaryBeam(beamLine, houseBound);

                    //写入次梁
                    var collection = SecondaryBeamLines.ToCollection();
                    originTransformer.Reset(collection);
                    ConnectSecondaryBeamService.DrawGraph(collection.Cast<Line>().ToList());
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
