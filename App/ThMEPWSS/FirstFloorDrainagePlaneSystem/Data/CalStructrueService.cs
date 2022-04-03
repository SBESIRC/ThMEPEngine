using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Engine;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Data
{
    public static class CalStructrueService
    {
        static List<List<string>> containMarkLayers = new List<List<string>>()      //包含条件的图层
        {
            new List<string>(){ "W-", "-EQPM" },
            new List<string>(){ "W-", "-NOTE" },
            new List<string>(){ "W-", "-DIMS" },
        };
        static List<List<string>> macthMarkLayers = new List<List<string>>()        //匹配条件的图层
        {
            new List<string>(){ "DIM_***" },
        };

        /// <summary>
        /// 获取框线
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ObjectIdCollection> GetFrame(AcadDatabase acadDatabase)
        {
            Dictionary<Polyline, ObjectIdCollection> frameLst = new Dictionary<Polyline, ObjectIdCollection>();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return frameLst;
            }

            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<BlockReference>(obj);
                var objs = new DBObjectCollection();
                frame.Explode(objs);
                var boundary = objs.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                frameLst.Add(boundary, new ObjectIdCollection() { obj });
            }
            return frameLst;
        }

        /// <summary>
        /// 通过框选获取框线
        /// </summary>
        /// <returns></returns>
        public static Dictionary<Polyline, ObjectIdCollection> GetFrameByCrosing(AcadDatabase acadDatabase)
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                Dictionary<Polyline, ObjectIdCollection> frameLst = new Dictionary<Polyline, ObjectIdCollection>();
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return frameLst;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                frameLst.Add(frame, new ObjectIdCollection() { frame.Id });

                return frameLst;
            }
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acdb"></param>
        /// <param name="originTransformer"></param>
        /// <returns></returns>
        public static List<ThIfcRoom> GetRoomInfo(this Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var roomEngine = new ThAIRoomOutlineExtractionEngine();
            roomEngine.ExtractFromMS(acdb.Database);
            roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

            var markEngine = new ThAIRoomMarkExtractionEngine();
            markEngine.ExtractFromMS(acdb.Database);
            markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

            var boundaryEngine = new ThAIRoomOutlineRecognitionEngine();
            boundaryEngine.Recognize(roomEngine.Results, polyline.Vertices());
            var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markRecEngine = new ThAIRoomMarkRecognitionEngine();
            markRecEngine.Recognize(markEngine.Results, polyline.Vertices());
            var marks = markRecEngine.Elements.Cast<ThIfcTextNote>().ToList();
            var builder = new ThRoomBuilderEngine();
            builder.Build(rooms, marks);

            return rooms;
        }

        /// <summary>
        /// 获取用户绘制出户框线
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static List<Polyline> GetUserFrame(this Polyline polyline, AcadDatabase acadDatabase, ThMEPOriginTransformer originTransformer)
        {
            List<Polyline> frameLst = new List<Polyline>();
            var dxfNames = new string[]
            {
                 RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutFrameLayerName };
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                                                 o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
            var result = Active.Editor.SelectAll(filterlist);
            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<Polyline>(obj).Clone() as Polyline;
                originTransformer.Transform(frame);
                if (polyline.Contains(frame) || polyline.IsIntersects(frame))
                {
                    frameLst.Add(frame);
                }
            }

            return frameLst;
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        public static void GetStructureInfo(this Polyline pFrame, AcadDatabase acdb, out List<Polyline> columns, out List<Polyline> walls, ThMEPOriginTransformer originTransformer)
        {
            var outFrame = pFrame.Clone() as Polyline;
            originTransformer.Reset(outFrame);
            var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, outFrame.Vertices());

            //获取柱
            columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(outFrame).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(outFrame).Cast<Polyline>().ToList();

            //建筑构建
            using (var archWallEngine = new ThDB3ArchWallRecognitionEngine())
            {
                //建筑墙
                archWallEngine.Recognize(acdb.Database, outFrame.Vertices());
                var arcWall = archWallEngine.Elements.Select(x => x.Outline).Where(x => x is Polyline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                arcWall.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls.AddRange(thCADCoreNTSSpatialIndex.SelectCrossingPolygon(outFrame).Cast<Polyline>().ToList());
            }

            columns.ForEach(x => originTransformer.Transform(x));
            walls.ForEach(x => originTransformer.Transform(x));
        }

        /// <summary>
        /// 根据房间创建虚拟墙
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="userFrames"></param>
        /// <returns></returns>
        public static List<MPolygon> GetRoomWall(List<Polyline> rooms, List<Polyline> userFrames)
        {
            var roomWallLst = new List<MPolygon>();
            //var bufferFrames = userFrames.Select(x => x.Buffer(100)[0] as Polyline).ToList();
            foreach (var room in rooms)
            {
                var bufferRoom = room.Buffer(50)[0] as Polyline;
                var resMPoly = ThMPolygonTool.CreateMPolygon(bufferRoom, new List<Curve>() { room });
                roomWallLst.Add(resMPoly);
                //var checkFrame = bufferFrames.Where(x => room.IsIntersects(x)).ToList();
                //if (checkFrame.Count > 0)
                //{
                //    var bufferRoom = room.Buffer(150)[0] as Polyline;
                //    var resMPoly = ThMPolygonTool.CreateMPolygon(bufferRoom, new List<Curve>() { room });
                //    roomWallLst.Add(resMPoly);
                //}
            }

            return roomWallLst;
        }

        /// <summary>
        /// 获取立管（所有可能的立管图元）
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        public static List<Entity> GetVerticalPipe(this Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var dxfNames = new string[]
            {
                 ThCADCommon.DxfName_TCH_Pipe,
                 RXClass.GetClass(typeof(Circle)).DxfName,
            };
            var pipes = new List<Entity>();
            var blocks = acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.GetEffectiveName() == ThWSSCommon.VerticalPipe_BlockName1 ||
                                o.GetEffectiveName() == ThWSSCommon.VerticalPipe_BlockName2 ||
                                o.GetEffectiveName() == ThWSSCommon.VerticalPipe_BlockName3)
                    .Select(x => x.Clone() as BlockReference)
                    .ToList();
            pipes.AddRange(blocks);
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
            var allpipes = Active.Editor.SelectAll(filterlist);
            if (allpipes.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allpipes.Value.GetObjectIds())
                {
                    var ent = acdb.Element<Entity>(obj).Clone() as Entity;
                    if (ent is Circle circle)
                    {
                        if (circle.Radius == 50 || circle.Radius == 75 || circle.Radius == 100)
                        {
                            pipes.Add(ent);
                        }
                    }
                    else
                    {
                        pipes.Add(ent);
                    }
                }
            }
            pipes.ForEach(x => originTransformer.Transform(x));
            pipes = pipes.Where(o =>
            {
                var pts = o.GeometricExtents;
                var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                return polyline.Contains(position);
            }).ToList();

            return pipes;
        }

        /// <summary>
        /// 获取标注
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        public static List<Entity> GetPipeMarks(this Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var allLayers = acdb.Layers.Select(x => x.Name).ToList();
            var markLayers = allLayers.Where(x => containMarkLayers.Any(y => y.All(z => x.Contains(z))) || macthMarkLayers.Any(y => y.First().Matching(x))).ToList();
            var dxfNames = new string[]
            {
                 ThCADCommon.DxfName_TCH_MLeader,
                 RXClass.GetClass(typeof(DBText)).DxfName,
            };
            var layerNames = markLayers.ToArray();
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) | o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
            var allpipes = Active.Editor.SelectAll(filterlist);
            var marks = new List<Entity>();
            if (allpipes.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allpipes.Value.GetObjectIds())
                {
                    var ent = acdb.Element<Entity>(obj).Clone() as Entity;
                    marks.Add(ent);
                }
            }
            marks.ForEach(x => originTransformer.Transform(x));
            marks = marks.Where(o =>
            {
                var position = Point3d.Origin;
                if (o is DBText bText)
                {
                    position = bText.Position;
                }
                else
                {
                    try
                    {
                        var pts = o.GeometricExtents;
                        position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                    }
                    catch (System.Exception)
                    {
                        return false;
                    }
                    
                }
                return polyline.Contains(position);
            }).ToList();

            return marks;
        }

        /// <summary>
        /// 识别立管
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="pipes"></param>
        /// <param name="marks"></param>
        /// <returns></returns>
        public static List<VerticalPipeModel> RecognizeVerticalPipe(this Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var pipes = polyline.GetVerticalPipe(acdb, originTransformer);
            var marks = polyline.GetPipeMarks(acdb, originTransformer);
            VerticalPipeRecognizeEngine verticalPipeRecognize = new VerticalPipeRecognizeEngine();
            var resModels = verticalPipeRecognize.Recognize(pipes, marks);
            return resModels;
        }

        /// <summary>
        /// 识别洁具立管
        /// </summary>
        /// <param name="layerNames"></param>
        /// <param name="polyline"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public static List<VerticalPipeModel> RecognizeSanitaryWarePipe(this Polyline polyline, Dictionary<string, List<string>> layerNames, List<Polyline> walls, ThMEPOriginTransformer originTransformer)
        {
            DrainingPointRecognizeEngine pointRecognizeEngine = new DrainingPointRecognizeEngine(layerNames);
            var equipmentModels = pointRecognizeEngine.Recognize(polyline, walls, originTransformer);
            var resModels = new List<VerticalPipeModel>();
            foreach (var model in equipmentModels)
            {
                var vModel = new VerticalPipeModel();
                vModel.Position = model.DiranPoint;
                vModel.PipeCircle = new Circle(vModel.Position, Vector3d.ZAxis, 100);
                vModel.IsEuiqmentPipe = true;
                if (model.EnumEquipmentType == EnumEquipmentType.toilet)
                {
                    vModel.PipeType = VerticalPipeType.WasteWaterPipe;
                }
                else
                {
                    vModel.PipeType = VerticalPipeType.SewagePipe;
                }
                resModels.Add(vModel);
            }
            return resModels;
        }

        /// <summary>
        /// 提取室外污水主管
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static List<Polyline> GetSewageDrainageMainPipe(this Polyline polyline, AcadDatabase acadDatabase, ThMEPOriginTransformer originTransformer)
        {
            List<Polyline> pipeLst = new List<Polyline>();
            var dxfNames = new string[]
            {
                 RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutdoorSewagePipeLayerName };
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                                                 o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
            var result = Active.Editor.SelectAll(filterlist);
            if (result.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var pipe = acadDatabase.Element<Polyline>(obj).Clone() as Polyline;
                    originTransformer.Transform(pipe);
                    if (polyline.Intersects(pipe))
                    {
                        pipeLst.Add(pipe);
                    }
                }
            }

            return pipeLst.SelectMany(x=> polyline.Trim(x).OfType<Polyline>()).ToList();
        }

        /// <summary>
        /// 提取室外雨水主管
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static List<Polyline> GetRainDrainageMainPipe(this Polyline polyline, AcadDatabase acadDatabase, ThMEPOriginTransformer originTransformer)
        {
            List<Polyline> pipeLst = new List<Polyline>();
            var dxfNames = new string[]
            {
                 RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutdoorRainPipeLayerName };
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                                                 o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
            var result = Active.Editor.SelectAll(filterlist);
            if (result.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var pipe = acadDatabase.Element<Polyline>(obj).Clone() as Polyline;
                    originTransformer.Transform(pipe);
                    if (polyline.Contains(pipe))
                    {
                        pipeLst.Add(pipe);
                    }
                }
            }

            return pipeLst;
        }

        /// <summary>
        /// 获取轴网线
        /// </summary>
        /// <param name="polyline"></param>
        public static List<Curve> GetAxis(this Polyline polyline, AcadDatabase acadDatabase, ThMEPOriginTransformer originTransformer)
        {
            var outFrame = polyline.Clone() as Polyline;
            originTransformer.Reset(outFrame);
            var axisEngine = new ThAXISLineRecognitionEngine();
            axisEngine.Recognize(acadDatabase.Database, outFrame.Vertices());
            var retAxisCurves = new List<Curve>();
            foreach (var item in axisEngine.Elements)
            {
                if (item == null || item.Outline == null)
                    continue;
                if (item.Outline is Curve curve)
                {
                    var copy = (Curve)curve.Clone();
                    originTransformer.Transform(copy);
                    retAxisCurves.Add(copy);
                }
            }
            return retAxisCurves;
        }
    }
}