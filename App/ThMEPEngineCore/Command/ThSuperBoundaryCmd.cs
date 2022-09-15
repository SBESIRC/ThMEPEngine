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

namespace ThMEPEngineCore.Command
{
    public class ThSuperBoundaryCmd : ThMEPBaseCommand, IDisposable
    {
        private SBND_Mode _mode;
        private DBObjectCollection _collectObjs;
        private DBObjectCollection _modelSpaceEnties;
        private ThCADCoreNTSSpatialIndex _roomTextSpatialIndex;
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

            var roomBoundaries = new DBObjectCollection();
            if(_mode == SBND_Mode.SBND_PICK)
            {
                roomBoundaries = GetRoomBoundaries(_collectObjs);
            }
            else
            {
                roomBoundaries = GetRoomBoundariesByName(_collectObjs);
            }

            if(roomBoundaries.Count>0)
            {
                roomBoundaries = Clean(roomBoundaries);
            }           

            PrintRooms(roomBoundaries);
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
                    var curves = hatch.Boundaries();
                    curves.ForEach(c => results.Add(c));
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
                    var polygons = hatch.BoundariesEx();
                    polygons.OfType<Entity>().ForEach(k =>
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
                                polygon.Loops().ForEach(l => results.Add(l));
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

