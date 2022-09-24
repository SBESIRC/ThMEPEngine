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

namespace ThMEPEngineCore.Command
{
    public class ThSuperBoundaryCmd : ThMEPBaseCommand, IDisposable
    {
        private SBND_Mode _mode;
        private DBObjectCollection _collectObjs;
        private DBObjectCollection _modelSpaceEnties;
        private ThCADCoreNTSSpatialIndex _roomTextSpatialIndex;
        private DBObjectCollection _roomBoundaries; // 已提交到Db中

        public DBObjectCollection RoomBoundaries => _roomBoundaries;

        public ThSuperBoundaryCmd(DBObjectCollection modelSpaceEnties, SBND_Mode mode = SBND_Mode.SBND_PICK)
        {
            ActionName = "提取房间框线";
            CommandName = "THEROC";
            _mode = mode;
            _modelSpaceEnties = modelSpaceEnties;            
            _collectObjs = new DBObjectCollection();
            _roomTextSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        }

        public ThSuperBoundaryCmd(DBObjectCollection modelSpaceEnties, DBObjectCollection roomNameTexts, 
            SBND_Mode mode = SBND_Mode.SBND_ALL) :this(modelSpaceEnties, mode)
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
                _roomBoundaries = GetRoomBoundaries(_collectObjs);
            }
            else
            {
                _roomBoundaries = GetRoomBoundariesByName(_collectObjs);
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

        private DBObjectCollection GetRoomBoundaries(DBObjectCollection objs)
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
                            var holes = polygon.Holes();
                            if (holes.Count > 0)
                            {
                                results.Add(polygon);
                                holes.ForEach(h => h.Dispose());
                            }
                            else
                            {
                                results.Add(polygon.Shell());
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

        private DBObjectCollection GetRoomBoundariesByName(DBObjectCollection objs)
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
                                var holes = polygon.Holes();
                                if (holes.Count > 0)
                                {
                                    results.Add(polygon);
                                    holes.ForEach(h => h.Dispose());
                                }
                                else
                                {
                                    results.Add(polygon.Shell());
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

        private DBObjectCollection ToDBObjectCollection(Hatch hatch)
        {
            var boundaries = hatch.Boundaries().ToCollection();
            var roomBuilder = new ThRoomOutlineBuilderEngine();
            roomBuilder.Build(boundaries);
            return FilterPolygonEdges(roomBuilder.Areas);
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

