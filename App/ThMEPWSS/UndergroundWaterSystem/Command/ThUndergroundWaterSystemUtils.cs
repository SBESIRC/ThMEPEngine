using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Command
{
    public static class ThUndergroundWaterSystemUtils
    {
        /// <summary>
        /// 判断是否是天正元素
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }
        /// <summary>
        /// 查找startPt所在线
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Line FindStartLine(Point3d startPt, List<Line> lines)
        {
            foreach (var l in lines)
            {
                if (l.StartPoint.DistanceTo(startPt) < 10)
                {
                    return l;
                }
                else if (l.EndPoint.DistanceTo(startPt) < 10)
                {
                    var tmpPt = l.StartPoint;
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPt;
                    return l;
                }
            }
            //扩大搜索范围
            double tol = 100;
            foreach (var l in lines)
            {
                if (l.StartPoint.DistanceTo(startPt) <= tol)
                {
                    return l;
                }
                else if (l.EndPoint.DistanceTo(startPt) <= tol)
                {
                    var tmpPt = l.StartPoint;
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPt;
                    return l;
                }
            }
            return null;
        }
        public static Point3d SelectPoint(string tips)
        {
            var point1 = Active.Editor.GetPoint(tips);
            if (point1.Status != PromptStatus.OK)
            {
                return new Point3d();
            }
            return point1.Value.TransformBy(Active.Editor.UCS2WCS());
        }
        public static Point3dCollection SelectArea()
        {
            var input = ThMEPWSS.Common.Utils.SelectPoints();
            var range = new Point3dCollection();
            range.Add(input.Item1);
            range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
            range.Add(input.Item2);
            range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
            return range;
        }
    }
}
