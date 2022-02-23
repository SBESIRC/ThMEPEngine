﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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
        public static List<Polyline> GetUserFrame(this Polyline polyline, AcadDatabase acadDatabase)
        {
            List<Polyline> frameLst = new List<Polyline>();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutFrameLayerName };
            var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return frameLst;
            }

            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<Polyline>(obj);
                if (polyline.Contains(frame))
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
        public static void GetStructureInfo(this Polyline pFrame, AcadDatabase acdb, out List<Polyline> columns, out List<Polyline> walls)
        {
            var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, pFrame.Vertices());

            //获取柱
            columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();

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
                archWallEngine.Recognize(acdb.Database, pFrame.Vertices());
                var arcWall = archWallEngine.Elements.Select(x => x.Outline).Where(x => x is Polyline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                arcWall.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls.AddRange(thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList());
            }
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
            var bufferFrames = userFrames.Select(x => x.Buffer(100)[0] as Polyline).ToList();
            foreach (var room in rooms)
            {
                var checkFrame = bufferFrames.Where(x => room.IsIntersects(x)).ToList();
                if (checkFrame.Count > 0)
                {
                    var bufferRoom = room.Buffer(300)[0] as Polyline;
                    var resMPoly = ThMPolygonTool.CreateMPolygon(room, new List<Curve>() { bufferRoom });
                    roomWallLst.Add(resMPoly);
                }
            }

            return roomWallLst;
        }

        /// <summary>
        /// 获取立管（所有可能的立管图元）
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        public static List<Entity> GetVerticalPipe(this Polyline polyline, AcadDatabase acdb)
        {
            var dxfNames = new string[]
            {
                 ThCADCommon.DxfName_VerticalPipe,
                 RXClass.GetClass(typeof(Circle)).DxfName,
                 ThWSSCommon.VerticalPipe_BlockName1,
                 ThWSSCommon.VerticalPipe_BlockName2,
                 ThWSSCommon.VerticalPipe_BlockName3,
            };
            var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
            var pipes = new List<Entity>();
            var allpipes = Active.Editor.SelectAll(filterlist);
            if (allpipes.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allpipes.Value.GetObjectIds())
                {
                    var ent = acdb.Element<Entity>(obj);
                    if (ent is Circle circle)
                    {
                        if (circle.Radius == 100 || circle.Radius == 150 || circle.Radius == 200)
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
        public static List<Entity> GetPipeMarks(this Polyline polyline, AcadDatabase acdb)
        {
            var allLayers = acdb.Layers.Select(x => x.Name).ToList();
            var markLayers = allLayers.Where(x => containMarkLayers.Any(y => y.All(z => x.Contains(z))) || macthMarkLayers.Any(y => y.First().Matching(x))).ToList();
            var marks = acdb.ModelSpace
                  .OfType<Entity>()
                  .Where(o => markLayers.Contains(o.Layer))
                  .ToList();
            var dbText = acdb.ModelSpace
                  .OfType<DBText>()
                  .ToList();
            marks.AddRange(dbText);
            marks = marks.Where(o =>
            {
                var pts = o.GeometricExtents;
                var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
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
        public static List<VerticalPipeModel> RecognizeVerticalPipe(this Polyline polyline, AcadDatabase acdb)
        {
            var pipes = polyline.GetVerticalPipe(acdb);
            var marks = polyline.GetPipeMarks(acdb);
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
        public static List<DrainingEquipmentModel> RecognizeSanitaryWarePipe(this Polyline polyline, Dictionary<string, List<string>> layerNames,  List<Polyline> walls)
        {
            DrainingPointRecognizeEngine pointRecognizeEngine = new DrainingPointRecognizeEngine(layerNames);
            var resModels = pointRecognizeEngine.Recognize(polyline, walls);
            return resModels;
        }

        /// <summary>
        /// 提取室外污水主管
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static List<Polyline> GetSewageDrainageMainPipe(this Polyline polyline, AcadDatabase acadDatabase)
        {
            List<Polyline> resPolys = new List<Polyline>();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutdoorSewagePipeLayerName };
            var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return resPolys;
            }

            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<Polyline>(obj);
                if (polyline.Contains(frame))
                {
                    resPolys.Add(frame);
                }
            }
            return resPolys;
        }

        /// <summary>
        /// 提取室外雨水主管
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static List<Polyline> GetRainDrainageMainPipe(this Polyline polyline, AcadDatabase acadDatabase)
        {
            List<Polyline> resPolys = new List<Polyline>();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layerNames = new string[] { ThWSSCommon.OutdoorRainPipeLayerName };
            var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return resPolys;
            }

            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<Polyline>(obj);
                if (polyline.Contains(frame))
                {
                    resPolys.Add(frame);
                }
            }
            return resPolys;
        }
    }
}