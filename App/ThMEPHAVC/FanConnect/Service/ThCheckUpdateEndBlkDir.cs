using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    internal class ThCheckUpdateEndBlkDir
    {
        public static void UpdateEndBlkDir(ThFanTreeNode<ThFanPipeModel> tree)
        {
            var childs = tree.GetDecendent();

            foreach (var c in childs)
            {
                if (c.Item.ConnectFan != null && c.Item.ConnectFan.FanType == "AI-水管断线")
                {
                    var fanBlk = c.Item.ConnectFan.FanData;
                    if (NeedRotateBlk(fanBlk, c.Item.PLine.StartPoint, c.Item.PLine.EndPoint, out var dirLine))
                    {
                        TrunBlk(fanBlk, dirLine);
                    }
                }
            }
        }

        public static void UpdateEndBlkDir(List<Line> rightLine, List<ThFanCUModel> fan)
        {
            foreach (var l in rightLine)
            {
                var closeFan = fan.Where(x => x.FanType == "AI-水管断线" &&
                                        (x.FanPoint.DistanceTo(l.EndPoint) < 400.0 || x.FanPoint.DistanceTo(l.StartPoint) < 400.0));
                if (closeFan.Any())
                {
                    var fanBlk = closeFan.First().FanData;
                    if (NeedRotateBlk(fanBlk, l.StartPoint, l.EndPoint, out var dirLine))
                    {
                        TrunBlk(fanBlk, dirLine);
                    }
                }
            }
        }

        private static bool NeedRotateBlk(BlockReference endBlk, Point3d connectPt1, Point3d connectPt2, out Vector3d dirLine)
        {
            var need = false;
            var dirBlk = Vector3d.XAxis.RotateBy(endBlk.Rotation, Vector3d.ZAxis).GetNormal();

            dirLine = new Vector3d();
            if (endBlk.Position.DistanceTo(connectPt1) <= endBlk.Position.DistanceTo(connectPt2))
            {
                dirLine = (connectPt2 - connectPt1).GetNormal();
            }
            else
            {
                dirLine = (connectPt1 - connectPt2).GetNormal();
            }

            var angle = dirLine.GetAngleTo(dirBlk, Vector3d.ZAxis) * 180 / Math.PI;
            if ((89 <= angle && angle <= 91) == false)
            {
                need = true;
            }

            return need;
        }

        private static void TrunBlk(BlockReference endBlk, Vector3d connectLineDir)
        {
            var newDir = connectLineDir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);
            var newAngle = 2 * Math.PI - endBlk.Rotation + Vector3d.XAxis.GetAngleTo(newDir, Vector3d.ZAxis);
            var rotaM = Matrix3d.Rotation(newAngle, Vector3d.ZAxis, endBlk.Position);

            endBlk.UpgradeOpen();
            endBlk.TransformBy(rotaM);
            endBlk.DowngradeOpen();
        }

    }
}
