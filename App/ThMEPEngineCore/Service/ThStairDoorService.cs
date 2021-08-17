using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStairDoorService
    {
        public List<List<Point3d>> GetDoorList(BlockReference stair)
        {
            var points = GetClosePointToPline(stair);
            var doorList = new List<List<Point3d>>();
            for (int row = 0; row < points.Count; row++) 
            {
                var maxDistance = 0.0;
                var door = new List<Point3d>();
                for (int i = 0; i < points[row].Count; i++)
                {
                    for (int j = i + 1; j < points[row].Count; j++)
                    {
                        var distance = points[row][i].DistanceTo(points[row][j]);
                        if (distance > maxDistance)
                        {
                            if (door.Count < 2)
                            {
                                door.Add(points[row][i]);
                                door.Add(points[row][j]);
                                maxDistance = distance;
                            }
                            else
                            {
                                door[0] = points[row][i];
                                door[1] = points[row][j];
                            }
                        }
                    }
                }
                doorList.Add(door);
            }
            return doorList;
        }

        private List<List<Point3d>> GetClosePointToPline(BlockReference stair)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ToOBB(stair);
                var buffer = GetBuffer(frame);
                var closePointToPline = new List<List<Point3d>>();
                var doors = GetDoors(stair, buffer);
                var blocksSpatialIndex = new ThCADCoreNTSSpatialIndex(doors);
                foreach (Polyline filterObj in blocksSpatialIndex.SelectCrossingPolygon(buffer))
                {
                    var verticesMap = new List<Point3d>();
                    foreach (Point3d vertice in filterObj.Vertices())
                    {
                        verticesMap.Add(frame.GetClosePoint(vertice));
                    }
                    closePointToPline.Add(verticesMap);
                }
                return closePointToPline;
            }
        }

        private Polyline GetBuffer(Polyline frame)
        {
            var bufferCollection = frame.Buffer(100);
            var buffer = new Polyline();
            foreach (Polyline pline in bufferCollection)
            {
                buffer = pline;
            }
            return buffer;
        }

        private DBObjectCollection GetDoors(BlockReference stair, Polyline buffer)
        {
            var engine = new ThDB3DoorRecognitionEngine();
            engine.Recognize(stair.BlockTableRecord.Database, buffer.Vertices());
            return engine.Elements.Select(o => o.Outline).ToCollection();
        }

        private Polyline ToOBB(BlockReference br)
        {
            var extents = new Extents3d();
            var objs = new DBObjectCollection();
            br.Explode(objs);
            objs.OfType<Curve>().ForEach(e => extents.AddExtents(e.GeometricExtents));
            return extents.ToRectangle();
        }
    }
}
