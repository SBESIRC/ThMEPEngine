using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameService
    {
        private ThBeamConnectRecogitionEngine BeamConnectEngine { get; set; }
        public ThMEPFrameService(ThBeamConnectRecogitionEngine thBeamConnectRecogition)
        {
            BeamConnectEngine = thBeamConnectRecogition;
        }

        public static Polyline Normalize(Polyline frame)
        {
            // 处理框线不闭合的情况
            var clone = frame.Clone() as Polyline;
            clone.Closed = true;

            // 处理共线和自交的情况
            var results = clone.PreprocessAsPolygon();
            return results.Cast<Polyline>().OrderByDescending(o => o.Area).First();
        }

        public DBObjectCollection RegionsFromFrame(Polyline frame)
        {
            var fence_beam = BeamConnectEngine.SpatialIndexManager.BeamSpatialIndex.SelectCrossingPolygon(frame);
            var fence_column = BeamConnectEngine.SpatialIndexManager.ColumnSpatialIndex.SelectCrossingPolygon(frame);
            var fence_wall = BeamConnectEngine.SpatialIndexManager.WallSpatialIndex.SelectCrossingPolygon(frame);

            List<ThIfcColumn> queryColumnElements = new List<ThIfcColumn>();
            List<ThIfcBuildingElement> queryWallElements = new List<ThIfcBuildingElement>();
            List<Tuple<ThIfcBeam,Polyline>> queryBeamElements = new List <Tuple<ThIfcBeam, Polyline>>();
            foreach (Polyline polyline in fence_beam)
            {
                if (polyline.IsClosed())
                {
                    var beamElement = BeamConnectEngine.BeamEngine.FilterByOutline(polyline) as ThIfcBeam;
                    queryBeamElements.Add(Tuple.Create(beamElement, CreateExtendBeamOutline(beamElement as ThIfcLineBeam,100.0)));
                }
            }
            foreach (Polyline polyline in fence_column)
            {
                if (polyline.IsClosed())
                {
                    queryColumnElements.Add(BeamConnectEngine.ColumnEngine.FilterByOutline(polyline) as ThIfcColumn);
                }
            }
            foreach (Polyline polyline in fence_wall)
            {
                if (polyline.IsClosed())
                {
                    queryWallElements.Add(BeamConnectEngine.ShearWallEngine.FilterByOutline(polyline));
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
            return frame.Difference(element_polyline);
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
