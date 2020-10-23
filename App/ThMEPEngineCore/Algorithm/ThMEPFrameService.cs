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
            return ThLineBeamOutliner.ExtendBoth(lineBeam, extendDis, extendDis);
        }
    }
}
