using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThCADCore.NTS;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Alg
{
    public class ThMEPHAVCBounds
    {
        // 进来的线应该移动到原点附近
        public static Polyline getConnectorBounds(DBObjectCollection centerLines, double offset)
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
        public static Polyline getValveBounds(BlockReference valve, Valve_modify_param param)
        {
            var dirVec = ThMEPHVACService.Get_dir_vec_by_angle_3(valve.Rotation);
            var rightDirVec = ThMEPHVACService.Get_right_vertical_vec(dirVec);
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
        public static Polyline getHoseBounds(BlockReference hose, double len, double width)
        {
            var dirVec = ThMEPHVACService.Get_dir_vec_by_angle_3(hose.Rotation);
            var rightDirVec = ThMEPHVACService.Get_right_vertical_vec(dirVec);
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
    }
}
