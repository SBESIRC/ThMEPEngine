using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Extension;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using GeometryExtensions;
using NFox.Cad;
using ThMEPWSS.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore;

namespace ThMEPWSS.Command
{
    public class ThSprinklerLayoutCmdUtils
    {
        /// <summary>
        /// 计算排布方向
        /// </summary>
        /// <returns></returns>
        public static bool CalWCSLayoutDirection(out Matrix3d matrix)
        {
            matrix = Active.Editor.CurrentUserCoordinateSystem;
            PromptPointOptions options = new PromptPointOptions("请选择排布方向起始点");
            var sResult = Active.Editor.GetPoint(options);

            if (sResult.Status == PromptStatus.OK)
            {
                var startPt = sResult.Value;
                var transPt = startPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var endPt = Interaction.GetLineEndPoint("请选择终止点", transPt);

                if (System.Double.IsNaN(endPt.X) || System.Double.IsNaN(endPt.Y) || System.Double.IsNaN(endPt.Z))
                {
                    return false;
                }

                transPt = new Point3d(transPt.X, transPt.Y, 0);
                endPt = new Point3d(endPt.X, endPt.Y, 0);
                Vector3d xDir = (endPt - transPt).GetNormal();
                Vector3d yDir = xDir.GetPerpendicularVector().GetNormal();
                Vector3d zDir = Vector3d.ZAxis;

                matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, transPt.X,
                    xDir.Y, yDir.Y, zDir.Y, transPt.Y,
                    xDir.Z, yDir.Z, zDir.Z, transPt.Z,
                    0.0, 0.0, 0.0, 1.0});

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取框线
        /// </summary>
        /// <returns></returns>
        public static List<Polyline> GetFrames()
        {
            var resPolys = new List<Polyline>();
            var options = new PromptKeywordOptions("\n选择处理方式");
            options.Keywords.Add("框选范围", "K", "框选范围(K)");
            options.Keywords.Add("选择多段线", "P", "选择多段线(P)");
            options.Keywords.Default = "框选范围";
            var result = Active.Editor.GetKeywords(options);
            if (result.Status != PromptStatus.OK)
            {
                return resPolys;
            }

            if (result.StringResult == "框选范围")
            {
                resPolys = GetFrameByCrosing();
            }
            else if (result.StringResult == "选择多段线")
            {
                resPolys = GetFrameBySelectPolyline();
            }

            var clonePoly = resPolys.Select(x => x.Clone() as Polyline).ToList();
            return clonePoly;
        }

        /// <summary>
        /// 通过框选获取框线
        /// </summary>
        /// <returns></returns>
        private static List<Polyline> GetFrameByCrosing()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                var resPolys = new List<Polyline>();
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return resPolys;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                var frames = new List<Polyline>() { frame };

                resPolys.AddRange(GetAllFramePolys(frames));
                return resPolys;
            }
        }

        /// <summary>
        /// 通过点选获取框线
        /// </summary>
        /// <returns></returns>
        private static List<Polyline> GetFrameBySelectPolyline()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var resPolys = new List<Polyline>();
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                    RXClass.GetClass(typeof(MPolygon)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThMEPEngineCoreLayerUtils.ROOMOUTLINE,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames/*, layerNames*/);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return resPolys;
                }

                List<Polyline> polylines = new List<Polyline>();
                List<Polyline> mpolyHoles = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var ent = acdb.Element<Entity>(frame);
                    if (ent is Polyline plBack)
                    {
                        var plFrame = ThMEPFrameService.Normalize(plBack);
                        polylines.Add(plFrame);
                    }
                    else if (ent is MPolygon mPl)
                    {
                        polylines.Add(mPl.Shell());
                        mpolyHoles.AddRange(mPl.Holes());
                    }
                }

                CalHolesService calHolesService = new CalHolesService();
                polylines = calHolesService.RemoveHoles(polylines);

                resPolys.AddRange(GetAllFramePolys(polylines));
                resPolys.AddRange(mpolyHoles);
                return resPolys;
            }
        }

        /// <summary>
        /// 获取所有框线包括洞口
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private static List<Polyline> GetAllFramePolys(List<Polyline> frames)
        {
            var resPolys = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThMEPEngineCoreLayerUtils.ROOMOUTLINE,
                };
                var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                    o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
                var polys = new List<Polyline>();
                var status = Active.Editor.SelectAll(filterlist);
                if (status.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in status.Value.GetObjectIds())
                    {
                        var plBack = acadDatabase.Element<Polyline>(obj);
                        if (plBack.Area > 10)
                        {
                            var plFrame = ThMEPFrameService.Normalize(plBack);
                            polys.Add(plFrame);
                        }
                    }
                }

                foreach (var frame in frames)
                {
                    var checkFrame = frame.Buffer(5)[0] as Polyline;
                    polys.Where(o =>
                    {
                        return o.Area > 0 && checkFrame.Contains(o) && (frame.Area - o.Area) > 50;
                    })
                   .Cast<Polyline>()
                   .ForEachDbObject(o => resPolys.Add(o));
                    resPolys.Add(frame);
                }
            }

            return resPolys;
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        public static void GetStructureInfo(AcadDatabase acdb, Polyline polyline, Polyline pFrame, out List<Polyline> columns, out List<Entity> beams, out List<Polyline> walls)
        {
            var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, polyline.Vertices());

            //获取柱
            columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();

            //获取梁
            #region  原始获取梁的方式(暂不要)
            //var thBeams = allStructure.BeamEngine.Elements.Cast<ThIfcLineBeam>().ToList();
            //thBeams.ForEach(x => x.ExtendBoth(20, 20));
            //beams = thBeams.Select(o => o.Outline).Cast<Entity>().ToList();
            //objs = new DBObjectCollection();
            //beams.ForEach(x => objs.Add(x));
            //thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            //beams = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Entity>().ToList();
            #endregion
            var engine = new ThBeamAreaBuilderEngine();
            engine.Build(acdb.Database, pFrame.Vertices());
            beams = engine.BeamAreas.OfType<Entity>().ToList();

            //获取剪力墙
            walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();

            //建筑构建
            using (var archWallEngine = new ThDB3ArchWallRecognitionEngine())
            {
                //建筑墙
                archWallEngine.Recognize(acdb.Database, polyline.Vertices());
                var arcWall = archWallEngine.Elements.Select(x => x.Outline).Where(x => x is Polyline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                arcWall.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls.AddRange(thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList());
            }
        }
    }
}
