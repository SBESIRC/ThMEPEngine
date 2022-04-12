using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class CheckService
    {
        /// <summary>
        /// 判断是否和外框线相交
        /// </summary>
        /// <param name="line"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithFrame(Curve line, Polyline frame)
        {
            return frame.IsIntersects(line);
        }

        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> polylines, Polyline line, double bufferWidth)
        {
            DBObjectCollection dBObject = new DBObjectCollection() { line };
            foreach (Polyline polyline in dBObject.Buffer(bufferWidth))
            {
                var objs = polylines.ToCollection();
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var resPolys = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
                if (resPolys.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断框线是否和洞口相交
        /// </summary>
        /// <param name="line"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithHoles(Curve line, List<Polyline> holes)
        {
            return holes.Any(x => x.IsIntersects(line));
        }

        /// <summary>
        /// 判断立管类型是否相等
        /// </summary>
        /// <param name="type"></param>
        /// <param name="otherType"></param>
        /// <returns></returns>
        public static bool CheckVerticalType(VerticalPipeType type, VerticalPipeType otherType)
        {
            if (type == otherType)
            {
                return true;
            }
            else if (type != VerticalPipeType.CondensatePipe && type != VerticalPipeType.rainPipe && otherType != VerticalPipeType.CondensatePipe && otherType != VerticalPipeType.rainPipe)
            {
                return true;
            }
            else if ((type == VerticalPipeType.CondensatePipe || type == VerticalPipeType.rainPipe) && (otherType == VerticalPipeType.CondensatePipe || otherType == VerticalPipeType.rainPipe))
            {
                return true;
            }

            return false;
        }
    }
}
