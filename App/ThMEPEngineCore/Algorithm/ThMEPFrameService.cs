using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameService
    {
        private static double OFFSET_DISTANCE = 30.0;

        public static Polyline Normalize(Polyline frame)
        {
            // 创建封闭多段线
            var obj = frame.WashClone();
            var clone = obj as Polyline;
            if (clone == null)
            {
                return new Polyline();
            }
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

        public static Polyline NormalizeEx(Polyline frame, double tolerance = ThMEPEngineCoreCommon.LOOSE_CLOSED_POLYLINE)
        {
            if (IsClosed(frame, tolerance))
            {
                return Normalize(frame);
            }
            // 返回"Dummy"框线
            // 暂时不支持分割线的情况
            return new Polyline();
        }

        public static Polyline Rebuild(Polyline frame, double extendLength = 1.0, double tesslateLength = 100.0)
        {
            // 经过前面的处理：
            //  1. RemoveSpikes -> 去除非常细的尖角
            //  2. MakeValid -> 去除自交部分
            // 若还存在问题，例如：
            //  1. 起点和终点不一致，但是“看起来”是“闭合”的。
            // 这样的情况下，就需要打散后再重新生成闭合区域
            var objs = new DBObjectCollection() { frame };
            var roomOutlineBuilder = new ThRoomOutlineBuilderEngine()
            {
                ArcTessellateLength = tesslateLength,
                LineExtendDistance = extendLength,
            };
            roomOutlineBuilder.Build(objs);
            if (roomOutlineBuilder.Areas.Count > 0)
            {
                return roomOutlineBuilder.Areas.OfType<Polyline>().OrderByDescending(o => o.Area).First();
            }
            else
            {
                return frame;
            }
        }

        public static Polyline Buffer(Polyline frame, double distance)
        {
            var results = frame.Buffer(distance);
            return results.Cast<Polyline>().FindByMax(o => o.Area);
        }

        public static Polyline Simplify(Polyline frame, double distance)
        {
            return frame.TPSimplify(distance);
        }

        public static bool IsClosed(Polyline frame, double tolerance)
        {
            // 支持真实闭合或视觉闭合
            return frame.Closed || (frame.StartPoint.DistanceTo(frame.EndPoint) <= tolerance);
        }
        /// <summary>
        /// 处理NTS difference以后造成的闭合polyline有重复点修复。
        /// 有可能将一个闭合polyline破成多个polyline。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns> 
        public static List<Polyline> FixDuplicatePointPolyline(Polyline frame)
        {
            Polyline framePolyline = new Polyline();
            var objs = new DBObjectCollection();
            objs.Add(frame);
            objs = objs.BufferPolygons(-1);
            objs = objs.BufferPolygons(1);

            var plList = objs.OfType<Polyline>().ToList();
            plList = plList.Select(x => ThMEPFrameService.Normalize(x)).ToList();

            return plList;
        }
    }
}
