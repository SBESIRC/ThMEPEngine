using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.Common
{
    class ThRoomLoadTable
    {
        string loadLayerName = "AI-负荷通风标注";
        ThMEPOriginTransformer _originTransformer;
        Dictionary<Polyline, Table> _tablePolyLines;
        private ThCADCoreNTSSpatialIndex tableLoadIndex;
        public ThRoomLoadTable(ThMEPOriginTransformer originTransformer)
        {
            _originTransformer = originTransformer;
            _tablePolyLines = new Dictionary<Polyline, Table>();
        }
        public List<Table> GetAllRoomLoadTable()
        {
            var loadTables = new List<Table>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var tables = acdb.ModelSpace.OfType<Table>().ToList();
                foreach (var table in tables)
                {
                    if (table.Layer != loadLayerName)
                        continue;
                    var copy = (Table)table.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    loadTables.Add(copy);
                }
            }
            return loadTables;
        }
        public void CreateSpatialIndex(List<Table> targetLoadTables) 
        {
            _tablePolyLines = new Dictionary<Polyline, Table>();
            var objs = new DBObjectCollection();
            foreach (var item in targetLoadTables) 
            {
                var rect = GetTableRectangle(item);
                _tablePolyLines.Add(rect, item);
                objs.Add(rect);
            }
            tableLoadIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public List<Table> GetIndexTables(Polyline roomPLine) 
        {
            var resTab = new List<Table>();
            var inPLines = tableLoadIndex.SelectCrossingPolygon(roomPLine);
            foreach (var item in inPLines) 
            {
                var pl = item as Polyline;
                var table = _tablePolyLines[pl];
                resTab.Add(table);
            }
            return resTab;
        }
        public List<Curve> GetAllLeadLine()
        {
            var leadCurves = new List<Curve>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var curves = acdb.ModelSpace.OfType<Curve>().ToList();
                foreach (var curve in curves)
                {
                    if (curve.Layer != loadLayerName)
                        continue;
                    if (curve is Line || curve is Polyline)
                    {
                        var copy = (Curve)curve.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copy);
                        leadCurves.Add(copy);
                    }
                }
            }
            return leadCurves;
        }
        public List<Table> GetRoomInnerTables(Polyline roomPLine, List<Table> targetLoadTables)
        {
            var tables = new List<Table>();
            foreach (var item in targetLoadTables) 
            {
                var point = item.Position;
                point = new Point3d(point.X,point.Y,0);
                if(roomPLine.Contains(point))
                    tables.Add(item);
            }
            return tables;
        }
        public List<Table> GetRoomIntersectsTables(Polyline roomPLine, List<Table> targetLoadTables)
        {
            var tableDic = targetLoadTables.ToDictionary(key => GetTableRectangle(key), value => value);
            var tables = new List<Table>();
            foreach (var table in tableDic)
            {
                if (roomPLine.Intersects(table.Key))
                    tables.Add(table.Value);
            }
            return tables;
        }
        public List<Table> GetRoomLeadTables(Polyline roomPLine,  List<Table> targetLoadTables, List<Curve> targetLeadCurves) 
        {
            var tables = new List<Table>();
            var tableDic = targetLoadTables.ToDictionary(key => GetTableRectangle(key,100), value => value);
            foreach (var curve in targetLeadCurves) 
            {
                var sp = curve.StartPoint;
                var ep = curve.EndPoint;
                var checkPoint = sp;
                if (roomPLine.Contains(sp))
                    checkPoint = ep;
                else if (roomPLine.Contains(ep))
                    checkPoint = sp;
                else
                    continue;
                foreach (var table in tableDic) 
                {
                    if (table.Key.Contains(checkPoint))
                        tables.Add(table.Value);
                }
            }
            return tables;
        }
        private Polyline GetTableRectangle(Table table,double bufferDis=0) 
        {
            var tableClone = (Table)table.Clone();
            var ang = Vector3d.XAxis.GetAngleTo(tableClone.Direction, tableClone.Normal);
            Matrix3d clockwiseMat = Matrix3d.Rotation(-1.0 * ang, tableClone.Normal, tableClone.Position);
            var newTable = tableClone.GetTransformedCopy(clockwiseMat) as Table;
            var obb = newTable.GeometricExtents.ToRectangle();
            Matrix3d counterClockwiseMat = Matrix3d.Rotation(ang, tableClone.Normal, tableClone.Position);
            obb.TransformBy(counterClockwiseMat);
            if (bufferDis > 0) 
            {
                obb = obb.Buffer(bufferDis)[0] as Polyline;
            }
            return obb;
        }
    }
}
