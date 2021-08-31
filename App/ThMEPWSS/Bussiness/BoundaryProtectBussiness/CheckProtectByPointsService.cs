using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
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
        int checkNum = 1;   //检验周围喷淋点位迭代次数

        public void CheckBoundarySprays(Polyline plFrame, List<Polyline> polylines, List<SprayLayoutData> sprays, double length, double minSpacing)
        {
            sprayMinSpcing = minSpacing;
            lineMaxSpacing = length / 2;
            sprayMaxSpacing = length;

            //获取边界的喷淋
            var bSprays = GetBoundarySpray(plFrame, polylines, sprays, sprayMaxSpacing);
            
            //调整边界喷淋
            AdjustSprayPosition(bSprays, sprays, polylines, 0);
        }

        /// <summary>
        /// 调整边界喷淋
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="maxSpacing"></param>
        public void AdjustSprayPosition(Dictionary<Line, List<SprayLayoutData>> sprays, List<SprayLayoutData> allSprays, List<Polyline> holes, int num)
        {
            foreach (var sprayLst in sprays)
            {
                foreach (var spray in sprayLst.Value)
                {
                    AdjustPosition(sprays, spray, sprayLst.Key, allSprays, holes, num);
                    #region 暂不用
                    //CalMoveSprayInfo(spray, sprayLst.Key, out Vector3d dir, out double length, out bool needMove);
                    //if (needMove)
                    //{
                    //    bool moveRes = true;
                    //    Point3d newPosition = spray.Position + dir * length;

                    //    CheckService checkService = new CheckService();
                    //    //检验是否落在了洞内
                    //    //if(!checkService.CheckSprayWithHoles(newPosition, holes))
                    //    //{
                    //    //    moveRes = false;
                    //    //    break;
                    //    //}

                    //    var aroundSprays = spray.GetAroundSprays(allSprays);
                    //    foreach (var aSpray in aroundSprays)
                    //    {
                    //        //校验喷淋间是否满足间距
                    //        if (!checkService.CheckSprayPtDistance(aSpray.Position, newPosition, sprayMaxSpacing, sprayMinSpcing))
                    //        {
                    //            Line tempLine = (spray.vLine.EndPoint - spray.vLine.StartPoint).IsParallelTo(dir, new Tolerance(0.1, 0.1)) ? spray.tLine : spray.vLine;
                    //            tempLine = new Line(tempLine.StartPoint + dir * length, tempLine.EndPoint + dir * length);
                    //            allSprays.Remove(spray);
                    //            var tempDic = new Dictionary<Line, List<SprayLayoutData>>();
                    //            tempDic.Add(tempLine, allSprays);
                    //            if (AdjustSprayPosition(tempDic, holes, num++))
                    //            {

                    //            }
                    //            moveRes = false;
                    //            break;
                    //        }
                    //    }

                    //    //校验喷淋与边界是否满足间距
                    //    var bLines = sprays.Where(x => x.Value.Contains(spray)).Select(x => x.Key).ToList();
                    //    if (!checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, lineMaxSpacing))
                    //    {
                    //        moveRes = false;
                    //        break;
                    //    }

                    //    if (moveRes)
                    //    {
                    //        spray.Position = newPosition;
                    //    }
                    //}
                    #endregion 
                }
            }
        }

        /// <summary>
        /// 调整点
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="spray"></param>
        /// <param name="boundaryLine"></param>
        /// <param name="allSprays"></param>
        /// <param name="holes"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool AdjustPosition(Dictionary<Line, List<SprayLayoutData>> sprays, SprayLayoutData spray, Line boundaryLine, List<SprayLayoutData> allSprays, List<Polyline> holes, int num)
        {
            if (num > checkNum)
            {
                return false;
            }
            CalMoveSprayInfo(spray, boundaryLine, out Vector3d dir, out double length, out bool needMove);
            if (needMove)
            {
                bool moveRes = true;
                Point3d newPosition = spray.Position + dir * length;
                Line checkLine = new Line(newPosition, spray.Position);
                if (holes.Any(x => x.Intersects(checkLine)))
                {
                    return false;
                }

                CheckService checkService = new CheckService();
                var aroundSprays = spray.GetAroundSprays(allSprays);
                foreach (var aSpray in aroundSprays)
                {
                    //校验喷淋间是否满足间距
                    if (!checkService.CheckSprayPtDistance(aSpray.Position, newPosition, sprayMaxSpacing, sprayMinSpcing))
                    {
                        Line tempLine = (spray.vLine.EndPoint - spray.vLine.StartPoint).IsParallelTo(dir, new Tolerance(0.1, 0.1)) ? spray.tLine : spray.vLine;
                        tempLine = new Line(tempLine.StartPoint + dir * (length - lineMaxSpacing), tempLine.EndPoint + dir * (length - lineMaxSpacing));
                        var otherSprays = allSprays.Except(new List<SprayLayoutData>() { spray }).ToList();
                        if (!AdjustPosition(sprays, aSpray, tempLine, otherSprays, holes, ++num))
                        {
                            moveRes = false;
                            break;
                        }
                    }
                }

                //校验喷淋与边界是否满足间距
                var bLines = sprays.Where(x => x.Value.Contains(spray)).Select(x => x.Key).ToList();
                if (!checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, lineMaxSpacing))
                {
                    moveRes = false;
                }

                if (moveRes)
                {
                    spray.Position = newPosition;
                }
                return moveRes;
            }
            return false;
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
