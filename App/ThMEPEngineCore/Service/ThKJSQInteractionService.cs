using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    internal class ThKJSQInteractionService
    {
        private ThRoomdata roomData;
        /// <summary>
        /// 通过房间数据造的面
        /// </summary>
        private DBObjectCollection Bounaries { get; set; }
        private ThMEPOriginTransformer Transformer { get; set; }
        public PickUpStatus Status { get; private set; } = PickUpStatus.Cancel;
        /// <summary>
        /// 最终生成的房间框线
        /// </summary>
        private DBObjectCollection RoomOutlines { get; set; }
        /// <summary>
        /// 显示管理
        /// Key存在于RoomOutlines中
        /// </summary>
        private Dictionary<Entity, Entity> RoomOutlineDisplayDict { get; set; }
        public ThKJSQInteractionService()
        {
            Bounaries = new DBObjectCollection();
            RoomOutlines = new DBObjectCollection();
            RoomOutlineDisplayDict = new Dictionary<Entity, Entity>();
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
        }
        public void Process(Database database, Point3dCollection frame)
        {
            // 获取围合房间的数据
            roomData = BuildRoomData(database, frame);
            Transformer = roomData.Transformer; // 房间数据的Transformer是用frame创建的,这儿保持一致，无需再创建

            // 造面
            Bounaries = BuildRoomBounaries(roomData);

            // 将数据移动到近原点处
            roomData.Transform();
            Transformer.Transform(Bounaries);
        }
        public void Process(DBObjectCollection roomDatas)
        {
            // 初始化
            roomData = new ThRoomdata(false); // Useless

            // 将数据移动到近原点处
            Transformer = new ThMEPOriginTransformer(roomDatas); // 房间数据的Transformer是用frame创建的,这儿保持一致，无需再创建
            Transformer.Transform(roomDatas);

            // 造面
            Bounaries = BuildRoomBounaries(roomDatas);
        }
        public void Run()
        {
            var ppo = new PromptPointOptions("\n选择房间内的一点")
            {
                AllowNone = true,
                AllowArbitraryInput = true,
            };
            ppo.Keywords.Add("PARTITION", "PARTITION", "分割(P)");
            while (true)
            {
                var ptRes = Active.Editor.GetPoint(ppo);
                if (ptRes.Status == PromptStatus.OK)
                {
                    using (var acadDb = AcadDatabase.Active())
                    {
                        var pt = ptRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                        var closeOriginPt = Transformer.Transform(pt);
                        if (roomData.IsContatinPoint3d(closeOriginPt))
                        {
                            Active.Editor.WriteMessage("\n选择的点不能在墙、柱等构件中");
                        }
                        else if (roomData.IsCloseToComponents(closeOriginPt, 1.0))
                        {
                            Active.Editor.WriteMessage("\n选择的点不能太靠近墙、柱等构件");
                        }
                        else
                        {
                            // 查询用户选择的点是否存在于已选的房间框线中
                            var existInRoomBoundaries = Query(RoomOutlines, closeOriginPt);
                            if (existInRoomBoundaries.Count == 0)
                            {
                                // 通过选择的点从造的面中查询包含此点的房间框线,并添加到RoomOutlines
                                var boundaries = Query(Bounaries, closeOriginPt);
                                var newAdds = new DBObjectCollection();
                                // 用原始造的面和现有存储的面进行差集，将剩余的面添加到RoomOutlines中
                                boundaries.OfType<Entity>().ForEach(o =>
                                {
                                    var subRooms = Difference(o, RoomOutlines);
                                    //暂时不做这种过滤，以免影响执行效率
                                    //subRooms = subRooms.OfType<Entity>().Where(e => !roomData.IsContains(Buffer(e, -1.0))).ToCollection();
                                    newAdds = newAdds.Union(subRooms);
                                    RoomOutlines = RoomOutlines.Union(subRooms);
                                });

                                // 更新显示
                                var displayObjs = AddToDisplay(newAdds);
                                Transformer.Reset(displayObjs); // 
                                AddToTransient(displayObjs);
                            }
                            else
                            {
                                existInRoomBoundaries.OfType<Entity>().ForEach(o => Remove(o));
                            }
                        }
                    }
                }
                else if (ptRes.Status == PromptStatus.Keyword)
                {
                    if (ptRes.StringResult == "Undo")
                    {
                        Undo();
                    }
                    else if (ptRes.StringResult == "PARTITION")
                    {
                        using (var acadDb = AcadDatabase.Active())
                        {
                            var splitLine = ThMEPPolylineEntityJig.PolylineJig(41, "\n请选择下一个点", false);
                            if (splitLine.Length >= 1.0)
                            {
                                Transformer.Transform(splitLine);
                                Split(splitLine); // 用分割线分割已获取到的房间
                            }
                        }
                    }
                }
                else if (ptRes.Status == PromptStatus.None)
                {
                    Status = PickUpStatus.OK;
                    break;
                }
                else if (ptRes.Status == PromptStatus.Cancel)
                {
                    var pko = new PromptKeywordOptions("\n是否退出并取消本次生成");
                    pko.Keywords.Add("YES", "YES", "是(Y)");
                    pko.Keywords.Add("NO", "NO", "否(N)");
                    pko.Keywords.Default = "NO";
                    var result1 = Active.Editor.GetKeywords(pko);
                    if (result1.Status == PromptStatus.OK)
                    {
                        if (result1.StringResult == "YES")
                        {
                            Status = PickUpStatus.Cancel;
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            ClearTransients();
        }
        public void PrintRooms()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                // 将房间框线移动到原位置
                var rooms = GetRooms();
                Transformer.Reset(rooms);
                var layerId = acadDb.Database.CreateAIRoomOutlineLayer();
                rooms.Cast<Entity>().ForEach(o =>
                {
                    var objs = new DBObjectCollection();
                    if (o is MPolygon mPolygon)
                    {
                        mPolygon.Explode(objs);
                    }
                    else
                    {
                        objs.Add(o);
                    }
                    objs.OfType<Entity>().ForEach(e =>
                    {
                        acadDb.ModelSpace.Add(e);
                        e.LayerId = layerId;
                        e.ColorIndex = (int)ColorIndex.BYLAYER;
                        e.LineWeight = LineWeight.ByLayer;
                        e.Linetype = "ByLayer";
                    });
                });
            }
        }
        private DBObjectCollection Difference(Entity originArea, DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(polygons);
            var objs = spatialIndex.SelectCrossingPolygon(originArea);
            if (objs.Count == 0)
            {
                results.Add(originArea);
            }
            else
            {
                results = ThCADCoreNTSEntityExtension.Difference(originArea, objs, true);
                results = Clean(results);
            }
            return results;
        }
        private DBObjectCollection Buffer(DBObjectCollection polygons, double length)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(o =>
            {
                var newEnt = bufferService.Buffer(o, length);
                if (newEnt != null)
                {
                    results.Add(newEnt);
                }
            });
            return results;
        }
        private Entity Buffer(Entity polygon, double length)
        {
            var bufferService = new ThNTSBufferService();
            return bufferService.Buffer(polygon, length);
        }
        private DBObjectCollection Clean(DBObjectCollection polygons)
        {
            // 处理Polygons
            var results = Rebuild(polygons); // 处理狭长线
            var simplifier = new ThPolygonalElementSimplifier();
            results = results.FilterSmallArea(1.0);
            results = simplifier.Normalize(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.MakeValid(results); //解决自交的Case
            results = results.FilterSmallArea(1.0);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(1.0);
            results = ThCADCoreNTSGeometryFilter.GeometryEquality(results); // 去重
            return results;
        }
        private void Split(Polyline splitLine)
        {
            // 对分割线做一次清理
            splitLine = splitLine.DPSimplify(1.0);

            // 找与splitLine相交的实体
            var intersObjs = FindIntersects(splitLine, RoomOutlines);
            if (intersObjs.Count > 0)
            {
                // 删除原有的显示
                intersObjs.OfType<Entity>().ForEach(o => Remove(o));

                // 用分割线分割房间
                var newRoomOutlines = Split(splitLine, intersObjs);
                newRoomOutlines = newRoomOutlines.Distinct();

                // 把新分割的房间放入，并显示
                RoomOutlines = RoomOutlines.Union(newRoomOutlines);

                // 更新显示
                var displayObjs = AddToDisplay(newRoomOutlines);
                Transformer.Reset(displayObjs); // 
                AddToTransient(displayObjs);
            }
        }
        private DBObjectCollection AddToDisplay(DBObjectCollection newAdds)
        {
            var displayObjs = new DBObjectCollection();
            newAdds
               .OfType<Entity>()
               .ForEach(o =>
               {
                   var displayObj = CreateDisplayBoundary(o);
                   if(RoomOutlineDisplayDict.ContainsKey(o))
                   {
                       if(RoomOutlineDisplayDict[o]!=null)
                       {
                           ClearTransientGraphics(new DBObjectCollection() { RoomOutlineDisplayDict[o] });
                       }
                       RoomOutlineDisplayDict[o] = displayObj;
                   }
                   else
                   {
                       RoomOutlineDisplayDict.Add(o, displayObj);
                   }
                   if (displayObj != null)
                   {
                       displayObjs.Add(displayObj);
                   }
               });
            return displayObjs;
        }

        private DBObjectCollection Split(Polyline splitLine, DBObjectCollection objs)
        {
            var polygonDatas = new DBObjectCollection();
            polygonDatas = polygonDatas.Union(objs);
            polygonDatas.Add(splitLine);

            var builder = new ThRoomOutlineBuilderEngine();
            builder.Build(polygonDatas);
            var results = builder.Areas;
            results = Rebuild(results);
            results = builder.PostProcess(results);
            return results;
        }
        private DBObjectCollection Rebuild(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().Where(e=>e.EntityArea()>=1.0).ForEach(e =>
            {
                if (e is Polyline polyline)
                {
                    var newPolyline = ThMEPFrameService.Rebuild(polyline, 10.0);
                    results.Add(newPolyline);
                }
                else
                {
                    results.Add(e);
                }
            });
            return results;
        }
        private DBObjectCollection FindIntersects(Polyline splitLine, DBObjectCollection objs)
        {
            // 找到与splitLine相交且交点有2个以上的房间轮廓线
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectFence(splitLine);
        }
        private void Undo()
        {
            if (RoomOutlines.Count > 0)
            {
                var last = RoomOutlines[RoomOutlines.Count - 1] as Entity;
                Remove(last);
            }
        }
        private void Remove(Entity roomOutline)
        {
            RoomOutlines.Remove(roomOutline);
            if (RoomOutlineDisplayDict.ContainsKey(roomOutline))
            {
                var disPlayObj = RoomOutlineDisplayDict[roomOutline];
                ClearTransientGraphics(new DBObjectCollection() { disPlayObj });
                RoomOutlineDisplayDict.Remove(roomOutline);
            }
        }
        private DBObjectCollection Query(DBObjectCollection polygons, Point3d point)
        {
            var outlines = ContainsPoint(polygons, point);
            if (outlines.Count == 0)
            {
                return new DBObjectCollection();
            }
            else
            {
                return outlines.Cast<Entity>().OrderByDescending(e => e.EntityArea()).ToCollection();
            }
        }
        private DBObjectCollection ContainsPoint(DBObjectCollection polygons, Point3d point)
        {
            var result = new DBObjectCollection();
            foreach (DBObject obj in polygons)
            {
                if (obj is Polyline polyline && polyline.Contains(point))
                {
                    result.Add(polyline);
                }
                else if (obj is MPolygon polygon && polygon.Contains(point))
                {
                    result.Add(polygon);
                }
            }
            result = result.Distinct();
            return result;
        }
        private ThRoomdata BuildRoomData(Database database, Point3dCollection frame)
        {
            ThRoomdata data = new ThRoomdata(false);
            data.Build(database, frame);
            return data;
        }
        private DBObjectCollection BuildRoomBounaries(ThRoomdata data)
        {
            data.Transform(); // 移动到原点
            data.Preprocess();

            // 用围合房间的数据造面
            var totalDatas = data.MergeData(); // 传入的数据
            var results = BuildRoomBounaries(totalDatas);

            // 将生产的面恢复到原位置
            data.Transformer.Reset(results);
            data.Reset();
            return results;
        }

        private DBObjectCollection BuildRoomBounaries(DBObjectCollection roomDatas)
        {
            var builder = new ThRoomOutlineBuilderEngine();
            builder.Build(roomDatas);
            var results = builder.Areas;
            results = Rebuild(results);
            results = builder.PostProcess(results);
            return results;
        }

        private Entity CreateHatch(Database database, Entity entity, int colorIndex = 21)
        {
            // 填充
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = acadDatabase.Database.CreateAIRoomOutlineLayer();
                if (entity is Polyline)
                {
                    var ObjIds = new ObjectIdCollection();
                    var clone = entity.Clone() as Entity;
                    ObjIds.Add(acadDatabase.ModelSpace.Add(clone));
                    clone.ColorIndex = colorIndex;
                    clone.Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;

                    Hatch oHatch = new Hatch();
                    var normal = new Vector3d(0.0, 0.0, 1.0);
                    oHatch.Normal = normal;
                    oHatch.Elevation = 0.0;
                    oHatch.PatternScale = 2.0;
                    oHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                    oHatch.ColorIndex = colorIndex;
                    oHatch.Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
                    oHatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
                    acadDatabase.ModelSpace.Add(oHatch);
                    //this works ok  
                    oHatch.Associative = true;
                    oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                    oHatch.EvaluateHatch(true);

                    return oHatch;
                }
                else if (entity is MPolygon)
                {
                    var mPolygon = entity.Clone() as MPolygon;
                    mPolygon.Normal = new Vector3d(0.0, 0.0, 1.0);
                    mPolygon.Elevation = 0.0;
                    mPolygon.PatternScale = 2.0;
                    mPolygon.SetPattern(HatchPatternType.PreDefined, "SOLID");
                    mPolygon.Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
                    mPolygon.ColorIndex = colorIndex;
                    mPolygon.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
                    //acadDatabase.ModelSpace.Add(mPolygon);
                    mPolygon.PatternColor = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
                    mPolygon.EvaluateHatch(true);
                    return mPolygon;
                }
                else
                {
                    return null;
                }
            }
        }
        private Entity CreateDisplayBoundary(Entity entity, int colorIndex = 21)
        {
            // 填充
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAIRoomOutlineLayer();
                var newEnt = entity.Clone() as Entity;
                newEnt.LayerId = layerId;
                newEnt.LineWeight = LineWeight.ByLayer;
                newEnt.ColorIndex = (int)ColorIndex.BYLAYER;
                newEnt.Linetype = "ByLayer";
                return newEnt;
            }
        }
        private void AddToTransient(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                objs.OfType<Entity>().ToList().ForEach(o =>
                {
                    tm.AddTransient(o, cadGraph.TransientDrawingMode.Highlight, 1, intCol);
                });
            }
        }
        private void ClearTransientGraphics(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                objs.OfType<Entity>().Where(o=>o!=null).ForEach(o =>
                {
                    tm.EraseTransient(o, intCol);
                });
            }
        }
        private void ClearTransients()
        {
            var displayObjs = RoomOutlineDisplayDict
                .Where(o => o.Value != null)
                .Select(o => o.Value)
                .ToCollection();
            ClearTransientGraphics(displayObjs);
        }
        private DBObjectCollection GetRooms()
        {
            return RoomOutlines.Clone();
        }
    }
    public enum PickUpStatus
    {
        OK,
        Cancel,
    }
}