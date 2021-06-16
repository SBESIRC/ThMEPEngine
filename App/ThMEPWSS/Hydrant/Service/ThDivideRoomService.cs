using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThDivideRoomService
    {
        public double TesslateLength { get; set; }
        public double ExtendLength { get; set; }
        private Entity RoomOutline { get; set; } //可能是洞（只支持一个Shell的洞）
        private List<Polyline> ProtectAreas { get; set; }

        public Dictionary<Polyline, List<Polyline>> Results { get; private set; }

        public ThDivideRoomService(Entity roomOutline,List<Polyline> protectAreas)
        {
            RoomOutline = roomOutline;
            ProtectAreas = protectAreas;
            TesslateLength = 500;
            ExtendLength = 1.0;
            Results = new Dictionary<Polyline, List<Polyline>>();
        }
        public void Divide()
        {    
            var objs = Preprocess();
            var polygons = objs.Polygons().Cast<Polyline>().ToList();

            //找出属于房间的Polygons
            var roomDic = BufferPolygon(polygons); // key->内缩的房间内部Polygon,Value->原始的房间内部Polygon
            var belongedRooms = BelongedRoom(roomDic.Keys.ToList());

            //找出所属房间的区域在哪些保护区域里
            belongedRooms.ForEach(o => Results.Add(roomDic[o], BelongedProtectArea(o)));
        }
        private List<Polyline> BelongedProtectArea(Polyline roomInnerPolygon)
        {
            return ProtectAreas.Where(o => o.Contains(roomInnerPolygon)).ToList();
        }
        private List<Polyline> BelongedRoom(List<Polyline> polygons)
        {
            return polygons.Where(o => RoomOutline.IsContains(o)).ToList();
        }
        private Dictionary<Polyline,Polyline> BufferPolygon(List<Polyline> polygons)
        {
            var result = new Dictionary<Polyline, Polyline>();
            polygons.ForEach(o =>
            {
                var buffers = o.Buffer(-1.0).Cast<Polyline>().OrderByDescending(o=>o.Area);
                if(buffers.Count()>0)
                {
                    result.Add(buffers.First(),o);
                }
                else
                {
                    result.Add(o.Clone() as Polyline,o);
                }
            });
            return result;
        }
        private DBObjectCollection Preprocess()
        {           
            // 转成多段线
            var polys = new List<Polyline>();
            polys.AddRange(ToPolylines(RoomOutline));
            ProtectAreas.ForEach(o => polys.AddRange(ToPolylines(o)));

            polys = MakeValid(polys);

            // 去重
            var lines = polys.SelectMany(o => o.ToLines(TesslateLength)).ToCollection();
            lines = ClearZeroLines(lines);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            lines = spatialIndex.Geometries.Values.ToCollection();

            // 合并
            lines = Merge(lines);

            // 延长
            if (ExtendLength <= 0)
            {
                ExtendLength = 1.0;
            }
            lines = lines.Cast<Line>().Select(o => o.ExtendLine(ExtendLength)).ToCollection();
            return lines;
        }
        private DBObjectCollection Merge(DBObjectCollection lines)
        {
            lines = ClearZeroLines(lines);
            var old_extend_distance = ThLaneLineEngine.extend_distance;
            var old_collinear_gap_distance = ThLaneLineEngine.collinear_gap_distance;
            ThLaneLineEngine.extend_distance = 2.0;
            ThLaneLineEngine.collinear_gap_distance = 1.0;
            var results = new DBObjectCollection();
            try
            {                
                results = ThLaneLineMergeExtension.Merge(lines);
                results = results.Cast<Line>().Where(o => o.Length > ThLaneLineEngine.extend_distance).ToCollection();
            }
            finally
            {
                ThLaneLineEngine.extend_distance = old_extend_distance;
                ThLaneLineEngine.collinear_gap_distance = old_collinear_gap_distance;
            }
            return ClearZeroLines(results);
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
        private List<Polyline> MakeValid(List<Polyline> polys)
        {
            var results = new List<Polyline>();
            polys.ForEach(o =>
            {
                var subResults = o.MakeValid().Cast<Polyline>();
                if (subResults.Any())
                {
                    results.Add(subResults.OrderByDescending(p => p.Area).First());
                }
            });
            return results;
        }
        private DBObjectCollection ClearZeroLines(DBObjectCollection lines,double baseLength =0.0)
        {
            return lines.Cast<Line>().Where(o => o.Length > baseLength).ToCollection();
        }
        public void Print(Database db)
        {
            using (var acadDb= AcadDatabase.Use(db))
            {
                int colorIndex = 1;
                Results.ForEach(o =>
                {
                    var groups = new List<Polyline> { o.Key.Clone() as Polyline };
                    o.Value.ForEach(e => groups.Add(e.Clone() as Polyline));
                    groups.ForEach(e =>
                    {
                        acadDb.ModelSpace.Add(e);
                        e.ColorIndex = colorIndex;
                        e.SetDatabaseDefaults();
                    });
                    GroupTools.CreateGroup(db, Guid.NewGuid().ToString(), groups.Select(o=>o.ObjectId).ToObjectIdCollection());
                    colorIndex++;
                });
            }
        }
    }
}
