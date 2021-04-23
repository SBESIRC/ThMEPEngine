using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameService
    {
        private static double OFFSET_DISTANCE = 30.0;
        private ThBeamConnectRecogitionEngine BeamConnectEngine { get; set; }
        public ThMEPFrameService(ThBeamConnectRecogitionEngine thBeamConnectRecogition)
        {
            BeamConnectEngine = thBeamConnectRecogition;
        }

        public static Polyline Normalize(Polyline frame)
        {
            // 创建封闭多段线
            var clone = frame.WashClone() as Polyline;
            clone.Closed = true;

            // 剔除尖状物
            clone = RemoveSpikes(clone);

            // 处理各种“Invalid Polygon“的情况
            return clone.MakeValid().Cast<Polyline>().OrderByDescending(o => o.Area).First();
        }

        private static Polyline RemoveSpikes(Polyline poly)
        {
            var objs = new DBObjectCollection();
            poly.Buffer(-OFFSET_DISTANCE)
                .Cast<Polyline>()
                .ForEach(o => {
                    o.Buffer(OFFSET_DISTANCE)
                    .Cast<Polyline>()
                    .ForEach(e => objs.Add(e));
                });
            if (objs.Count > 0)
            {
                return objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
            }
            return poly;
        }

        public static Polyline NormalizeEx(Polyline frame)
        {
            if (IsClosed(frame))
            {
                return Normalize(frame);
            }
            // 返回"Dummy"框线
            // 暂时不支持分割线的情况
            return new Polyline();
        }

        public static Polyline Buffer(Polyline frame, double distance)
        {
            var results = frame.Buffer(distance);
            return results.Cast<Polyline>().FindByMax(o => o.Area);
        }

        public static bool IsClosed(Polyline frame)
        {
            // 支持真实闭合或视觉闭合
            return frame.Closed || (frame.StartPoint.DistanceTo(frame.EndPoint) <= ThMEPEngineCoreCommon.LOOSE_CLOSED_POLYLINE);
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
