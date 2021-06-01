using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameService
    {
        private static double OFFSET_DISTANCE = 30.0;
        public ThMEPFrameService(ThBeamConnectRecogitionEngine thBeamConnectRecogition)
        {

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
                .ForEach(o =>
                {
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
    }
}
