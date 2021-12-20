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
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

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
                if (result.StringResult == "地下室顶板")
                {
                    Active.WriteMessage("\n建议采用框架大板、加腋大板方式");
                }
                else if (result.StringResult == "地下室中板")
                {
                    //ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pts[0]);
                    //暂时不处理超远问题，因为目前主梁有一些问题，增加了一些不必要的后处理
                    ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                    GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);

                    Polyline polyline = pts.CreatePolyline();
                    originTransformer.Transform(polyline);
                    //获取主梁线和边框线
                    var beamLine = getPrimitivesService.GetBeamLine(polyline , out ObjectIdCollection objs);
                    var wallBound = getPrimitivesService.GetWallBound(polyline);
                    beamLine = beamLine.Union(wallBound).ToList();
                    var houseBound = getPrimitivesService.GetHouseBound(polyline);

                    //次梁计算
                    var SecondaryBeamLines = ConnectSecondaryBeamService.ConnectSecondaryBeam(beamLine, houseBound);

                    //创建图层
                    ConnectSecondaryBeamService.CreateSecondaryBeamLineLayer(acad.Database);

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
