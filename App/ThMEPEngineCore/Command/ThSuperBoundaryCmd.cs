using System;
using System.IO;
using System.Linq;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPEngineCore.Command
{
    public class ThSuperBoundaryCmd : ThMEPBaseCommand, IDisposable
    {
        private SBND_Mode _mode;
        private DBObjectCollection _collectObjs;
        private DBObjectCollection _modelSpaceEnties;
        private ThCADCoreNTSSpatialIndex _roomTextSpatialIndex;
        private DBObjectCollection _roomBoundaries; // 已提交到Db中
        private bool _keepHole;

        public DBObjectCollection RoomBoundaries => _roomBoundaries;

        public ThSuperBoundaryCmd(DBObjectCollection modelSpaceEnties, SBND_Mode mode = SBND_Mode.SBND_PICK,bool keepHole = false)
        {
            ActionName = "提取房间框线";
            CommandName = "THEROC";
            _mode = mode;
            _keepHole = keepHole;
            _modelSpaceEnties = modelSpaceEnties;            
            _collectObjs = new DBObjectCollection();
            _roomTextSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        }

        public ThSuperBoundaryCmd(DBObjectCollection modelSpaceEnties, DBObjectCollection roomNameTexts, 
            SBND_Mode mode = SBND_Mode.SBND_ALL, bool keepHole = false) :this(modelSpaceEnties, mode, keepHole)
        {
            _roomTextSpatialIndex = new ThCADCoreNTSSpatialIndex(roomNameTexts);
        }

        public void Dispose()
        {
            Erase(_collectObjs);
            _collectObjs.MDispose();
        }

        public override void SubExecute()
        {
            // 导入配置
            ExportConfig();

            // 首先清空现有的PickFirst选择集
            Active.Editor.SetImpliedSelection(new ObjectId[0]);
            // 接着将模型添加到PickFirst选择集
            Active.Editor.SetImpliedSelection(_modelSpaceEnties.OfType<Entity>().Select(o => o.ObjectId).ToArray());

            Active.Database.ObjectAppended += Database_ObjectAppended;
            if(_mode == SBND_Mode.SBND_PICK)
            {
                Active.Editor.PostCommand("SBND_PICK ");
            }
            else
            {
                Active.Editor.PostCommand("SBND_ALL ");
            }
            Active.Database.ObjectAppended -= Database_ObjectAppended;

            _roomBoundaries = new DBObjectCollection();
            if(_mode == SBND_Mode.SBND_PICK)
            {
                _roomBoundaries = GetRoomBoundaries(_collectObjs,_keepHole);
            }
            else
            {
                _roomBoundaries = GetRoomBoundariesByName(_collectObjs, _keepHole);
            }

            if(_roomBoundaries.Count>0)
            {
                _roomBoundaries = Clean(_roomBoundaries);
            }           

            PrintRooms(_roomBoundaries);
        }

        private void ExportConfig()
        {
            var path = ThCADCommon.SuperBoundaryIniPath();
            if(File.Exists(path))
            {
                //object[] parameters = new object[] { "SBND_CONFIG", "IM", path };
                //Active.Editor.Command(parameters);
                var cmd = "SBND_CONFIG"+" " +"IM"+" "+ path+ "\n";
                Active.Editor.PostCommand(cmd);
            }
        }

        private DBObjectCollection Clean(DBObjectCollection roomBoundaries)
        {
            var transformer = new ThMEPOriginTransformer(roomBoundaries);
            transformer.Transform(roomBoundaries);
            var roomSimplifer = new ThRoomOutlineSimplifier();
            var results = roomSimplifer.Filter(roomBoundaries);

            results = roomSimplifer.Normalize(results); // 处理狭长线
            results = roomSimplifer.Filter(results);

            results = roomSimplifer.MakeValid(results); // 处理自交
            results = roomSimplifer.Filter(results);

            results = roomSimplifer.Simplify(results);  // 处理简化线
            results = roomSimplifer.Filter(results);

            results = roomSimplifer.OverKill(results);  // 去重
            transformer.Reset(results);

            return results;
        }

        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Hatch || e.DBObject is Curve)
            {
                _collectObjs.Add(e.DBObject);
            }
        }

        private DBObjectCollection GetRoomBoundaries(DBObjectCollection objs,bool keepHole)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                if(e is Polyline || e is Circle circle || e is Ellipse)
                {
                    results.Add(e.Clone() as Curve);
                }
                else if (e is Hatch hatch)
                {                    
                    var polygons = ToDBObjectCollection(hatch);
                    polygons.OfType<Entity>()
                    .Where(o => (o is Polyline p && p.Area > 1e-6) || (o is MPolygon m && m.Area > 1e-6))
                    .ForEach(o =>
                    {
                        if(o is MPolygon polygon)
                        {
                            if(keepHole)
                            {
                                results.Add(polygon);
                            }
                            else
                            {
                                var shell = polygon.Shell();
                                var holes = polygon.Holes();
                                results.Add(shell);
                                holes.ForEach(h => results.Add(h));
                            }                           
                        }
                        else
                        {
                            results.Add(o);
                        }                        
                    });
                }
                else 
                {
                    // not support
                }
            });
            return results;
        }

        private DBObjectCollection GetRoomBoundariesByName(DBObjectCollection objs,bool keepHole)
        {
            var results = new DBObjectCollection();            
            objs.OfType<Entity>().ForEach(e =>
            {
                if (e is Polyline || e is Circle circle || e is Ellipse)
                {
                    if(HasRoomName(e))
                    {
                        results.Add(e.Clone() as Curve);
                    }
                }
                else if (e is Hatch hatch)
                {
                    var polygons = ToDBObjectCollection(hatch); 
                    polygons
                    .OfType<Entity>()
                    .Where(o=>(o is Polyline p && p.Area>1e-6) || (o is MPolygon m && m.Area > 1e-6))
                    .ForEach(k =>
                    {
                        if(k is Polyline poly)
                        {
                            if(HasRoomName(poly))
                            {
                                results.Add(poly);
                            }
                        }
                        else if(k is MPolygon polygon)
                        {
                            if (HasRoomName(polygon))
                            {
                                if(keepHole)
                                {
                                    results.Add(polygon);
                                }
                                else
                                {
                                    var shell = polygon.Shell();
                                    var holes = polygon.Holes();
                                    results.Add(shell);
                                    holes.ForEach(h => results.Add(h));
                                }
                            }
                        }
                        else
                        {
                            // not support
                        }
                    });
                }
                else
                {
                    // not support
                }
            });
            return results;
        }

        //private DBObjectCollection ToDBObjectCollection(Hatch hatch)
        //{
        //    var boundaries = hatch.Boundaries().ToCollection();
        //    // 不稳定
        //    var roomBuilder = new ThRoomOutlineBuilderEngine();
        //    roomBuilder.Build(boundaries);
        //    return FilterPolygonEdges(roomBuilder.Areas);
        //}

        private DBObjectCollection ToDBObjectCollection(Hatch hatch, double tolerance = 1e-4, double areaTolerance = 1e-6)
        {
            try
            {
                var polygons = HatchToPolygons(hatch);
                var roomBuilder = new ThRoomOutlineBuilderEngine();
                return roomBuilder.PostProcess(polygons);
            }
            catch (Exception ex)
            {
                //
            }
            return new DBObjectCollection();
        }

        private DBObjectCollection HatchToPolygons(Hatch hatch, double tolerance = 1e-4,double areaTolerance=1e-6)
        {
            // 理想情况，一个Hatch对应一个Curve<Polyline,Circle..>或一个Mpolygon
            var results = new DBObjectCollection();
            if (hatch == null || hatch.NumberOfLoops==0)
            {
                return results;
            }
            Plane plane = hatch.GetPlane();
            if (hatch.NumberOfLoops==1)
            {
                var loopOutline = HatchLoopToEdge(hatch.GetLoopAt(0), plane, tolerance);
                if(loopOutline.Area>= areaTolerance)
                {
                    results.Add(loopOutline);
                }                
            }
            else
            {
                int shellLoopIndex =-1;
                for (int index = 0; index < hatch.NumberOfLoops; index++)
                {
                    var hatchLoop = hatch.GetLoopAt(index);
                    if(hatchLoop.LoopType == HatchLoopTypes.Outermost)
                    {
                        shellLoopIndex = index;
                        break;
                    }  
                    else if(hatchLoop.LoopType == (HatchLoopTypes.External| HatchLoopTypes.Polyline))
                    {
                        shellLoopIndex = index;
                        break;
                    }
                }
                if(shellLoopIndex!=-1)
                {
                    Curve shell = null;
                    var holes = new List<Curve>();
                    for (int index = 0; index < hatch.NumberOfLoops; index++)
                    {
                        var hatchLoop = hatch.GetLoopAt(index);
                        if (index == shellLoopIndex)
                        {
                            shell = HatchLoopToEdge(hatchLoop, plane, tolerance);                            
                        }
                        else if(hatchLoop.LoopType == HatchLoopTypes.NotClosed)
                        {
                            continue;
                        }
                        else 
                        {
                            var hole = HatchLoopToEdge(hatchLoop,plane, tolerance);
                            if(hole.Area>=areaTolerance)
                            {
                                holes.Add(hole);
                            }                          
                        }
                    }
                    if(shell != null && shell.Area>= areaTolerance)
                    {
                        if(holes.Count>0)
                        {
                            results.Add(ThMPolygonTool.CreateMPolygon(shell, holes));
                        }    
                    }
                }
                else
                {
                    for (int index = 0; index < hatch.NumberOfLoops; index++)
                    {
                        var hatchLoop = hatch.GetLoopAt(index);
                        if (hatchLoop.LoopType == HatchLoopTypes.NotClosed)
                        {
                            continue;
                        }
                        else
                        {
                            var loopOutline = HatchLoopToEdge(hatchLoop, plane, tolerance);
                            if (loopOutline.Area >= areaTolerance)
                            {
                                results.Add(loopOutline);
                            }                            
                        }
                    }
                }
            }
            return results;
        }

        private Curve HatchLoopToEdge(HatchLoop hatchLoop, Plane plane, double tolerance = 1e-4)
        {
            if (hatchLoop.IsPolyline)
            {
                var vertices = hatchLoop.Polyline;
                var pline = new Polyline(vertices.Count)
                {
                    Closed = true,
                };
                for (int i = 0; i < vertices.Count; i++)
                {
                    pline.AddVertexAt(i, vertices[i].Vertex, vertices[i].Bulge, 0, 0);
                }
                return pline;
            }
            else
            {
                // 单独处理圆的情况
                if (hatchLoop.Curves.Count == 1
                    && hatchLoop.Curves[0].IsClosed()
                    && hatchLoop.Curves[0] is CircularArc2d circularArc)
                {
                    return circularArc.ToCircle();
                }

                // 暂时只处理线和圆弧组合的情况
                if (hatchLoop.Curves.Count == 2)
                {
                    var circle = ThHatchTool.ToCircle(hatchLoop.Curves);
                    if (circle.Area <= 1e-6)
                    {
                        var poly = ThHatchTool.ToPolyline(hatchLoop.Curves, plane, tolerance);
                        if (poly != null)
                        {
                            return poly;
                        }
                        else
                        {
                            return new Polyline() { Closed = true };
                        }
                    }
                    else
                    {
                        return circle;
                    }
                }
                else
                {
                    var poly = ThHatchTool.ToPolyline(hatchLoop.Curves, plane, tolerance);
                    if (poly != null)
                    {
                        return poly;
                    }
                    else
                    {
                        return new Polyline() { Closed = true };
                    }
                }
            }
        }

        private DBObjectCollection FilterPolygonEdges(DBObjectCollection polygons)
        {
            var mPolygons = polygons.OfType<MPolygon>().ToCollection();
            var polylines = polygons.OfType<Polyline>().ToCollection();
            var mPolygonEdges = mPolygons.OfType<MPolygon>().SelectMany(o => o.Loops()).ToCollection();
            var roomSimplifer = new ThRoomOutlineSimplifier();
            var filterEdges = roomSimplifer.OverKill(mPolygonEdges, polylines).OfType<DBObject>().ToHashSet();
            mPolygonEdges.MDispose();
            return polygons.OfType<DBObject>().Where(o => !filterEdges.Contains(o)).ToCollection();
        }

        private bool HasRoomName(Entity room)
        {
            var roomTags = _roomTextSpatialIndex.SelectWindowPolygon(room);
            return roomTags.Count > 0;
        }

        private void PrintRooms(DBObjectCollection roomBoundaries)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                // 将房间框线移动到原位置
                var layerId = acadDb.Database.CreateAIRoomOutlineLayer();
                roomBoundaries.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.LayerId = layerId;
                    e.ColorIndex = (int)ColorIndex.BYLAYER;
                    e.LineWeight = LineWeight.ByLayer;
                    e.Linetype = "ByLayer";
                });
            }
        }

        private void Erase(DBObjectCollection objs)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    var entity = acadDb.Element<Entity>(e.Id, true);
                    if (!entity.IsErased && !entity.IsDisposed)
                    {
                        entity.Erase();
                    }
                });
            }
        }
    }
    public enum SBND_Mode
    {
        SBND_PICK,
        SBND_ALL
    }
}

