using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness.BoundaryProtectBussiness
{
    public class CheckProtectByPointsService : BoundaryProtestService
    {
        double sprayMinSpcing = 1800;
        double lineMaxSpacing = 1700;
        double sprayMaxSpacing = 3400;
        readonly double lineMinSpacing = 400;
        readonly double moveLength = 100;

        public void CheckBoundarySprays(Polyline polyline, List<SprayLayoutData> sprays, double length, double minSpacing)
        {
            sprayMinSpcing = minSpacing;
            lineMaxSpacing = length / 2;
            sprayMaxSpacing = length;

            //获取边界的喷淋
            var bSprays = GetBoundarySpray(polyline, sprays, sprayMaxSpacing);
            
            //调整边界喷淋
            AdjustSprayPosition(bSprays, sprays);
        }

        /// <summary>
        /// 调整边界喷淋
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="maxSpacing"></param>
        public void AdjustSprayPosition(Dictionary<Line, List<SprayLayoutData>> sprays, List<SprayLayoutData> allSprays)
        {
            foreach (var sprayLst in sprays)
            {
                foreach (var spray in sprayLst.Value)
                {
                    CalMoveSprayInfo(spray, sprayLst.Key, out Vector3d dir, out double length, out bool needMove);
                    if (needMove)
                    {
                        bool moveRes = true;
                        Point3d newPosition = spray.Position + dir * length;

                        CheckService checkService = new CheckService();
                        var aroundSprays = spray.GetAroundSprays(allSprays);
                        foreach (var aSpray in aroundSprays)
                        {
                            //校验喷淋间是否满足间距
                            if (!checkService.CheckSprayPtDistance(aSpray.Position, newPosition, sprayMaxSpacing, sprayMinSpcing))
                            {
                                moveRes = false;
                                break;
                            }
                        }

                        //校验喷淋与边界是否满足间距
                        var bLines = sprays.Where(x => x.Value.Contains(spray)).Select(x => x.Key).ToList();
                        if (!checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, lineMaxSpacing))
                        {
                            moveRes = false;
                            break;
                        }

                        if (moveRes)
                        {
                            spray.Position = newPosition;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取喷淋线的移动信息
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="maxSpacing"></param>
        /// <param name="moveDir"></param>
        /// <param name="length"></param>
        private void CalMoveSprayInfo(SprayLayoutData spray, Line boundaryLine, out Vector3d moveDir, out double length, out bool needMove)
        {
            var closePt = boundaryLine.GetClosestPointTo(spray.Position, true);
            var distance = closePt.DistanceTo(spray.Position);
            Vector3d lineDir = (boundaryLine.StartPoint - boundaryLine.EndPoint).GetNormal();
            Vector3d dir = (boundaryLine.GetClosestPointTo(spray.Position, true) - spray.Position).GetNormal();
            moveDir = Math.Abs(spray.mainDir.DotProduct(lineDir)) > Math.Abs(spray.otherDir.DotProduct(lineDir)) ? spray.otherDir : spray.mainDir;
            length = 0;
            needMove = false;
            if (distance < lineMinSpacing)
            {
                moveDir = moveDir.DotProduct(dir) > 0 ? -moveDir : moveDir;
                needMove = true;
                length = Math.Ceiling((lineMinSpacing - distance) / moveLength) * moveLength;
            }
            if (distance > lineMaxSpacing)
            {
                needMove = true;
                moveDir = moveDir.DotProduct(dir) > 0 ? moveDir : -moveDir;
                length = Math.Ceiling((distance - lineMaxSpacing) / moveLength) * moveLength;
            }
        }
    }
}
