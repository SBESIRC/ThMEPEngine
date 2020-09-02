using System;
using ThCADCore.NTS;
using GeoAPI.Geometries;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPFrameService instance = new ThMEPFrameService() {  };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPFrameService() { }
        internal ThMEPFrameService() { }
        public static ThMEPFrameService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public void InitializeWithDb(Database database)
        {
            ThMEPModelManager.Instance.Initialize();
            ThMEPModelManager.Instance.LoadFromDatabase(database);
            ThMEPModelManager.Instance.CreateSpatialIndex();
        }

        public DBObjectCollection RegionsFromFrame(Polyline frame)
        {
            // Select Crossing Polygon => 筛选出框线内所有梁柱墙（包括与框线不相交的梁柱墙）
            var objs_query = new DBObjectCollection();
            var fence_beam = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectCrossingPolygon(frame);
            var fence_column = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectCrossingPolygon(frame);
            var fence_wall = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectCrossingPolygon(frame);

            List<ThIfcColumn> queryColumnElements = new List<ThIfcColumn>();
            List<ThIfcBuildingElement> queryWallElements = new List<ThIfcBuildingElement>();
            List<Tuple<ThIfcBeam,Polyline>> queryBeamElements = new List <Tuple<ThIfcBeam, Polyline>>();
            foreach (Polyline polyline in fence_beam)
            {
                if (polyline.IsClosed())
                {
                    var beamElement = ThMEPModelManager.Instance.BeamEngine.FilterByOutline(polyline) as ThIfcBeam;
                    queryBeamElements.Add(Tuple.Create(beamElement, CreateExtendBeamOutline(beamElement as ThIfcLineBeam,100.0)));
                }
            }
            foreach (Polyline polyline in fence_column)
            {
                if (polyline.IsClosed())
                {
                    queryColumnElements.Add(ThMEPModelManager.Instance.ColumnEngine.FilterByOutline(polyline) as ThIfcColumn);
                }
            }
            foreach (Polyline polyline in fence_wall)
            {
                if (polyline.IsClosed())
                {
                    queryWallElements.Add(ThMEPModelManager.Instance.ShearWallEngine.FilterByOutline(polyline));
                }
            }
            var element_polyline = new DBObjectCollection();
            foreach (var element in queryBeamElements)
            {
                element_polyline.Add(element.Item2);
            }
            foreach (var element in queryColumnElements)
            {
                element_polyline.Add(element.Outline);
            }
            foreach (var element in queryWallElements)
            {
                element_polyline.Add(element.Outline);
            }
            var objs = new DBObjectCollection();
            foreach (Entity polyline in frame.Difference(element_polyline))
            {
                objs.Add(polyline);
            }
            return (objs);
        }

        private Polyline CreateExtendBeamOutline(ThIfcLineBeam lineBeam , double extendDis)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            Vector3d vec = lineBeam.StartPoint.GetVectorTo(lineBeam.EndPoint);
            Vector3d perpendVec = vec.GetPerpendicularVector();
            Point3d newSpt = lineBeam.StartPoint - vec.GetNormal().MultiplyBy(extendDis);
            Point3d newEpt = lineBeam.EndPoint + vec.GetNormal().MultiplyBy(extendDis);
            Point3d pt1 = newSpt - perpendVec.GetNormal().MultiplyBy(lineBeam.ActualWidth / 2.0);
            Point3d pt2 = newSpt + perpendVec.GetNormal().MultiplyBy(lineBeam.ActualWidth / 2.0);
            Point3d pt3 = newEpt + perpendVec.GetNormal().MultiplyBy(lineBeam.ActualWidth / 2.0);
            Point3d pt4 = newEpt - perpendVec.GetNormal().MultiplyBy(lineBeam.ActualWidth / 2.0);

            polyline.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(pt3.X, pt3.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(pt4.X, pt4.Y), 0.0, 0.0, 0.0);
            return polyline;
        }
    }
}
