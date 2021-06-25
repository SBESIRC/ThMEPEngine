using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThDivideRoomService
    {
        public double TesslateLength { get; set; }
        public double ExtendLength { get; set; }
        private List<Entity> RoomOutlines { get; set; } //可能是洞（只支持一个Shell的洞）
        private List<Entity> ProtectAreas { get; set; }
        private double zeroLength = 1e-4;
        private double PolygonBufferLength = 5.0;
        private double RoomBufferLength = 5.0;
        /// <summary>
        /// 房间轮廓被保护区域分割后的子区域所对应的保护区域
        /// Key->房间原始轮廓，Value->房间被分割的区域，及每个区域所受的保护区域
        /// </summary>
        public Dictionary<Entity,Dictionary<Entity, List<Entity>>> Results { get; private set; }

        public ThDivideRoomService(List<Entity> roomOutlines,List<Entity> protectAreas)
        {
            RoomOutlines = roomOutlines;
            ProtectAreas = protectAreas;
            TesslateLength = 500;
            ExtendLength = 1.0;
            Results = new Dictionary<Entity, Dictionary<Entity, List<Entity>>>();
        }
        public void Divide()
        {   
            // 前处理（打散、去重、合并、延长）
            var objs = Preprocess(); // 返回一堆直线 
            var polygons = objs.Polygons(); //重新分割区域

            // 去重
            var duplicateRemoveService = new ThSimilarityDuplicateRemoveService(
                polygons.Cast<Entity>().ToList());
            duplicateRemoveService.DuplicateRemove();
            polygons = duplicateRemoveService.Results.ToCollection();

            //对分割的Polygons进行内缩,用于判断哪些区域属于房间
            var polygonBufferDic = BufferPolygon(polygons.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength); 
            var spatialIndex = new ThCADCoreNTSSpatialIndex(polygonBufferDic.Keys.ToCollection());
            RoomOutlines.ForEach(o =>
            {
                //查找属于此房间的分割区域
                var splitPolygons =spatialIndex.SelectCrossingPolygon(o);
                var belongedRooms = BelongedRoom(o, splitPolygons.Cast<Entity>().ToList()); //找到哪些分割区域属于房间
                var container = new Dictionary<Entity, List<Entity>>();
                belongedRooms.ForEach(e =>
                {
                    //找出房间分割的区域在哪些保护区域里
                    container.Add(polygonBufferDic[e], BelongedProtectArea(e));
                });
                Results.Add(o, container);
            });
            // 后续可能会根据面积对子区域进行过滤
        }
        private List<Entity> BelongedProtectArea(Entity roomInnerPolygon)
        {
            return ProtectAreas.Where(o => o.IsContains(roomInnerPolygon)).ToList();
        }
        private List<Entity> BelongedRoom(Entity roomOutline,List<Entity> polygons)
        {
            return polygons.Where(o => roomOutline.IsContains(o)).ToList();
        }
        private Dictionary<Entity, Entity> BufferPolygon(List<Entity> polygons,double length)
        {
            var result = new Dictionary<Entity, Entity>();            
            var bufferService = new ThNTSBufferService();
            polygons.ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    var bufferEnt = bufferService.Buffer(o, length);
                    if (bufferEnt != null)
                    {
                        result.Add(bufferEnt, o);
                    }
                }
                else if(o is MPolygon mPolygon)
                {
                    var bufferEnt = bufferService.Buffer(o, length);
                    if (bufferEnt != null)
                    {
                        result.Add(bufferEnt, o);
                    }
                }
            });
            return result;
        }
        private DBObjectCollection Preprocess()
        {           
            // 内缩房间轮廓线，便于分割
            var roomOutlines = BufferPolygon(RoomOutlines, -1.0 * RoomBufferLength);
            // 转成多段线,且已打散
            var polys = new List<Polyline>();
            roomOutlines.Keys.ForEach(o => polys.AddRange(ToPolylines(o)));
            ProtectAreas.ForEach(o => polys.AddRange(ToPolylines(o)));

            var simplifer = new ThElementSimplifier();
            var handleResults = simplifer.MakeValid(polys.ToCollection());
            handleResults = simplifer.Normalize(handleResults);

            // 转成直线
            var lines = new DBObjectCollection();
            handleResults.Cast<Curve>().ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    polyline.ToLines(TesslateLength).ForEach(l => lines.Add(l));
                }
                else if(o is Line line)
                {
                    lines.Add(line);
                }
                else
                {
                    throw new NotSupportedException();
                }
            });

            // 去重
            lines = ClearZeroLines(lines, this.zeroLength); //移除接近于零长度的线
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            lines = spatialIndex.Geometries.Values.ToCollection();

            // 合并
            lines = Merge(lines); 

            // 延长            
            lines = lines.Cast<Line>().Select(o => o.ExtendLine(ExtendLength)).ToCollection();
            return lines;
        }
        private DBObjectCollection Merge(DBObjectCollection lines)
        {
            var results = new DBObjectCollection();
            lines = ClearZeroLines(lines, this.zeroLength);
            lines.LineMerge().Cast<Curve>().ForEach(o =>
                {
                    if(o is Line line)
                    {
                        results.Add(line);
                    }
                    else if(o is Polyline polyline)
                    {
                        polyline.ToLines().ForEach(l=> results.Add(l));
                    }
                });
            return ClearZeroLines(results, this.zeroLength);
        }
        private List<Line> ToLines(Entity entity)
        {
            var results = new List<Line>();
            if (entity is MPolygon mPolygon)
            {
                mPolygon.Loops().ForEach(o => results.AddRange(o.ToLines(TesslateLength)));
            }
            else if(entity is Polyline polyline)
            {
                results.AddRange(polyline.ToLines(TesslateLength));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }
        private List<Polyline> ToPolylines(Entity entity)
        {
            if (entity is MPolygon mPolygon)
            {
                return mPolygon.Loops().Select(o => o.TessellatePolylineWithArc(TesslateLength)).ToList();
            }
            else if (entity is Polyline polyline)
            {
                return new List<Polyline> { polyline.TessellatePolylineWithArc(TesslateLength) };
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private DBObjectCollection ClearZeroLines(DBObjectCollection lines,double baseLength =0.0)
        {
            return lines.Cast<Line>().Where(o => o.Length > baseLength).ToCollection();
        }        
    }
}
