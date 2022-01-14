using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Service
{
    /// <summary>
    /// 负荷计算Service
    /// </summary>
    public static class ThLoadCalculationService
    {
        /// <summary>
        /// 根据房间框线查找负荷计算表
        /// </summary>
        /// <param name="RoomBoundary"></param>
        /// <returns></returns>
        public static List<Tuple<ThIfcRoom, Table>> GetLoadCalculationTables(this List<ThIfcRoom> rooms, List<Curve> curves, List<Table> loadCalculationtables)
        {
            List<Tuple<ThIfcRoom, Table>> RoomMapping = new List<Tuple<ThIfcRoom, Table>>();
            var roomDic = rooms.ToDictionary(x => x.Boundary, y => y);
            ThCADCoreNTSSpatialIndex curveSpatialIndex = new ThCADCoreNTSSpatialIndex(curves.ToCollection());
            Dictionary<Polyline, Table> tableDic = loadCalculationtables.ToDictionary(key => key.GeometricExtents.ToRectangle(), value => value);
            ThCADCoreNTSSpatialIndex tableSpatialIndex = new ThCADCoreNTSSpatialIndex(tableDic.Keys.ToCollection());
            foreach (Entity roomBoundary in roomDic.Keys)
            {
                var loadCalculationtableobjs = tableSpatialIndex.SelectCrossingPolygon(roomBoundary);
                if (loadCalculationtableobjs.Count >0)
                {
                    foreach (Entity item in loadCalculationtableobjs)
                    {
                        //本身含有表格
                        Polyline tableBoundary = item as Polyline;
                        var existedTable = tableDic[tableBoundary];
                        if (roomBoundary.EntityContains(existedTable.Position))
                        {
                            //找到表格
                            RoomMapping.Add((roomDic[roomBoundary], existedTable).ToTuple());
                            break;
                        }
                    }
                }
                else
                {
                    var curveobjs = curveSpatialIndex.SelectFence(roomBoundary);
                    if (curveobjs.Count > 0)
                    {
                        foreach (Curve curve in curveobjs)
                        {
                            var pts = new Point3dCollection();
                            roomBoundary.IntersectWith(curve, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count % 2 == 1)
                            {
                                var tableobjs = tableSpatialIndex.SelectFence(curve);
                                if (tableobjs.Count == 1)
                                {
                                    //本身含有表格
                                    Polyline tableBoundary = tableobjs[0] as Polyline;
                                    RoomMapping.Add((roomDic[roomBoundary], tableDic[tableBoundary]).ToTuple());
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return RoomMapping;
        }


        /// <summary>
        /// 根据房间框线查找负荷计算表
        /// </summary>
        /// <param name="RoomBoundary"></param>
        /// <returns></returns>
        public static Table GetLoadCalculationTable(this ThIfcRoom ifcroom, List<Curve> curves, List<Table> loadCalculationtables)
        {
            var room = ifcroom.Boundary;
            ThCADCoreNTSSpatialIndex curveSpatialIndex = new ThCADCoreNTSSpatialIndex(curves.ToCollection());
            Dictionary<Polyline, Table> tableDic = loadCalculationtables.ToDictionary(key => key.GeometricExtents.ToRectangle(), value => value);
            ThCADCoreNTSSpatialIndex tableSpatialIndex = new ThCADCoreNTSSpatialIndex(tableDic.Keys.ToCollection());
            var loadCalculationtableobjs = tableSpatialIndex.SelectCrossingPolygon(room);
            if (loadCalculationtableobjs.Count >0)
            {
                foreach (Entity item in loadCalculationtableobjs)
                {
                    //本身含有表格
                    Polyline tableBoundary = item as Polyline;
                    Table existedTable = tableDic[tableBoundary];
                    if (room.EntityContains(existedTable.Position))
                    {
                        return existedTable;
                    }
                }
            }
            else
            {
                var curveobjs = curveSpatialIndex.SelectFence(room);
                if (curveobjs.Count > 0)
                {
                    foreach (Curve curve in curveobjs)
                    {
                        var pts = new Point3dCollection();
                        room.IntersectWith(curve, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count % 2 == 1)
                        {
                            var tableobjs = tableSpatialIndex.SelectFence(curve);
                            if (tableobjs.Count == 1)
                            {
                                //本身含有表格
                                Polyline tableBoundary = tableobjs[0] as Polyline;
                                Table existedTable = tableDic[tableBoundary];
                                return existedTable;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
