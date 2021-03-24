using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThGemometryExportEngine:IDisposable
    {
        public List<ThIfcRoom> Rooms { get; set; }
        public ThGemometryExportEngine()
        {
            Rooms = new List<ThIfcRoom>();
        }
        public void Export(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var roomDbExtension = new ThRoomDbExtension(database))
            {
                roomDbExtension.BuildElementCurves();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    roomDbExtension.Rooms.ForEach(o => dbObjs.Add(o.Boundary));
                    var roomSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    var polylines = new List<Polyline>();
                    foreach (var filterObj in roomSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        polylines.Add(filterObj as Polyline);
                    }
                    Rooms = roomDbExtension.Rooms.Where(o=> polylines.Contains(o.Boundary)).ToList();
                }
                else
                {
                    Rooms = roomDbExtension.Rooms;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
