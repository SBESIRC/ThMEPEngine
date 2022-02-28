using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Algorithm
{
    public class ThMEPHAVCBounds
    {
        // 进来的线应该移动到原点附近
        private static double sidePortFeetLens = 100;
        public static Polyline GetConnectorBounds(DBObjectCollection centerLines, double offset)
        {
            var outlines = centerLines.Buffer(offset);
            var lines = ThMEPHVACLineProc.Explode(outlines); // 打散的边不能相互连接
            var points = new Point3dCollection();
            foreach (Line l in lines)
            {
                points.Add(l.StartPoint);
                points.Add(l.EndPoint);
            }
            var pl = new Polyline();
            pl.CreatePolyline(points);
            return pl;
        }
        public static Polyline GetValveBounds(BlockReference valve, ValveModifyParam param)
        {
            var dirVec = ThMEPHVACService.GetDirVecByAngle3(valve.Rotation);
            var rightDirVec = ThMEPHVACService.GetRightVerticalVec(dirVec);
            var points = new Point3dCollection();
            var basePos = valve.Position;
            var v1 = dirVec * param.width;
            var v2 = rightDirVec * param.height;
            points.Add(basePos);
            points.Add(basePos + v1);
            points.Add(basePos + v1 + v2);// Diagonal
            points.Add(basePos + v2);
            var pl = new Polyline();
            pl.CreatePolyline(points);
            return pl;
        }
        public static Polyline GetHoseBounds(BlockReference hose, double len, double width)
        {
            var dirVec = ThMEPHVACService.GetDirVecByAngle3(hose.Rotation);
            var rightDirVec = ThMEPHVACService.GetRightVerticalVec(dirVec);
            var points = new Point3dCollection();
            var basePos = hose.Position;
            var v1 = dirVec * width;
            var v2 = rightDirVec * len;
            points.Add(basePos);
            points.Add(basePos + v1);
            points.Add(basePos + v1 + v2);// Diagonal
            points.Add(basePos + v2);
            var pl = new Polyline();
            pl.CreatePolyline(points);
            return pl;
        }
        public static Point3d GetSidePortCenterPoint(BlockReference port, Point3d srtP, string portSize, double ductWidth)
        {
            // 计算侧回中心点需要用到相交管径信息
            // dicDuctInfo -> 外包框hashcode到管宽的映射
            var portWidth = ThMEPHVACService.GetWidth(portSize);
            return GetSidePortCenterPoint(port, srtP, portWidth, ductWidth);
        }
        public static Point3d GetSidePortCenterPoint(BlockReference port, Point3d srtP, double portWidth, double ductWidth)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            var rotation = port.Rotation;
            var position = port.Position.TransformBy(mat);
            var dirVec = ThMEPHVACService.GetDirVecByAngle(rotation);
            var lVec = ThMEPHVACService.GetLeftVerticalVec(dirVec);
            var disVec = dirVec * 0.5 * portWidth + lVec * (0.5 * ductWidth + sidePortFeetLens);// 100是侧送风口脚长
            var p = position.ToPoint2D() + disVec;
            return new Point3d(Math.Round(p.X, 6), Math.Round(p.Y, 6), 0);
        }
        public static Point3d GetDownPortCenterPoint(BlockReference port, PortParam portParam)
        {
            // 下回风口获取中心点， 侧回风口计算对应中心点
            var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            var p = port.Position.TransformBy(mat).ToPoint2D();
            p = ThMEPHVACService.RoundPoint(p, 6);
            return new Point3d(p.X, p.Y, 0);
        }
    }
}