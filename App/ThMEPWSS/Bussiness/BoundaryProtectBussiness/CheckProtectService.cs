using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.Utils;
using ThWSS.Bussiness;

namespace ThMEPWSS.Bussiness.BoundaryProtectBussiness
{
    public class CheckProtectService : BoundaryProtestService
    {
        readonly double minSpacing = 400;
        readonly double moveLength = 100;
        readonly double maxMoveLength = 200;

        public void CheckBoundarySprays(Polyline plFrame, List<Polyline> polylines, List<SprayLayoutData> sprays, double length, double minLength)
        {
            var bSprays = GetBoundarySpray(plFrame, polylines, sprays, length);
            AdjustSprayLine(bSprays, sprays, length / 2, minLength);
        }

        /// <summary>
        /// 调整喷淋排布线
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="maxSpacing"></param>
        public void AdjustSprayLine(Dictionary<Line, List<SprayLayoutData>> sprays, List<SprayLayoutData> allSprays, double maxSpacing, double minLength)
        {
            while (sprays.Count > 0)
            {
                var firSpray = sprays.First();
                sprays.Remove(firSpray.Key);

                var sprayLine = firSpray.Value.First().GetPolylineByDir((firSpray.Key.EndPoint - firSpray.Key.StartPoint).GetNormal());
                var sprayLst = sprays.Where(x =>
                {
                    var lineDir = (x.Key.EndPoint - x.Key.StartPoint).GetNormal();
                    return x.Value.First().GetPolylineByDir(lineDir) == sprayLine;
                }).ToList();
                foreach (var removeSpray in sprayLst)
                {
                    sprays.Remove(removeSpray.Key);
                }
                sprayLst.Add(firSpray);
                
                Vector3d? moveDir = null;
                double moveLength = 0;
                bool needMove = false;
                foreach (var spray in sprayLst)
                {
                    CalMoveSprayInfo(spray, maxSpacing, out Vector3d dir, out double length, out bool isIn, out bool moveRes);
                    if (moveRes)
                    {
                        needMove = true;
                        if (moveDir == null)
                        {
                            moveDir = dir;
                            moveLength = length;
                        }
                        else
                        {
                            if (moveDir != dir)   //喷淋移动方向不一样就不移动
                            {
                                needMove = false;
                                break;
                            }

                            if (isIn)
                            {
                                if (moveLength > length)
                                {
                                    moveLength = length;     //保持向内移动最小距离
                                }
                            }
                            else
                            {
                                if (moveLength < length)
                                {
                                    moveLength = length;     //保持向外移动最大距离
                                }
                            }
                        }
                    }
                }

                if (needMove)
                {
                    if (moveLength > maxMoveLength)
                    {
                        continue;
                    }
                    var thisLine = firSpray.Value.First().GetOtherPolylineByDir(moveDir.Value);
                    var moveLine = thisLine.MovePolyline(moveLength, moveDir.Value);
                    var resSprays = allSprays.Where(x => x.tLine == thisLine || x.vLine == thisLine).ToList();
                    if (CheckLegalityWithBoundary(sprayLst.Select(x => x.Key).ToList(), moveLine, maxSpacing))
                    {
                        if (CheckMoveLineSpcing(allSprays, resSprays, moveDir.Value, moveLine, maxSpacing * 2, minLength, true))
                        {
                            SprayDataOperateService.UpdateSpraysLine(allSprays, thisLine, moveLine);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查移动线之后前后间距是否正常
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveLine"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private bool CheckMoveLineSpcing(List<SprayLayoutData> allSprays, List<SprayLayoutData> sprays, Vector3d moveDir, Line moveLine, double maxLength, double minLength, bool isAdjust)
        {
            Dictionary<Line, Line> matterLines = new Dictionary<Line, Line>();
            foreach (var spray in sprays)
            {
                var nextLine = spray.GetOtherNextPolylineByDir(moveDir);
                if (nextLine != null)
                {
                    double distance = moveLine.IndexedDistance(nextLine);
                    if (distance > maxLength || distance < minLength)
                    {
                        var dir = (nextLine.StartPoint - moveLine.StartPoint).GetNormal().DotProduct(moveDir) > 0 ? -moveDir : moveDir;
                        dir = distance < minLength ? -dir : dir;
                        if (isAdjust && MoveAdjacentPolyline(allSprays, nextLine, dir, distance, maxLength, minLength, out Line mLine))
                        {
                            if (!matterLines.Keys.Contains(nextLine))
                            {
                                matterLines.Add(nextLine, mLine);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                var prevLine = spray.GetOtherPrePolylineByDir(moveDir);
                if (prevLine != null)
                {
                    double distance = moveLine.IndexedDistance(prevLine);
                    if (distance > maxLength || distance < minLength)
                    {
                        var dir = (prevLine.StartPoint - moveLine.StartPoint).GetNormal().DotProduct(moveDir) > 0 ? -moveDir : moveDir;
                        dir = distance < minLength ? -dir : dir;
                        if (isAdjust && MoveAdjacentPolyline(allSprays, prevLine, dir, distance, maxLength, minLength, out Line mLine))
                        {
                            if (!matterLines.Keys.Contains(prevLine))
                            {
                                matterLines.Add(prevLine, mLine);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (var lineDic in matterLines)
            {
                SprayDataOperateService.UpdateSpraysLine(allSprays, lineDic.Key, lineDic.Value);
            }
            return true;
        }

        /// <summary>
        /// 移动相邻喷淋线
        /// </summary>
        /// <param name="allSprays"></param>
        /// <param name="thisLine"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveLength"></param>
        /// <param name="maxLength"></param>
        /// <param name="mLine"></param>
        /// <returns></returns>
        private bool MoveAdjacentPolyline(List<SprayLayoutData> allSprays, Line thisLine, Vector3d moveDir, double moveLength, double maxLength, double minLength, out Line mLine)
        {
            mLine = null;
            var resSprays = allSprays.Where(x => x.tLine == thisLine || x.vLine == thisLine).ToList();
            var moveLine = thisLine.MovePolyline(moveLength - maxLength, moveDir);
            if(CheckMoveLineSpcing(allSprays, resSprays, moveDir, moveLine, maxLength, minLength, false))
            {
                mLine = moveLine;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 校核移动线之后距离边界是否正常
        /// </summary>
        /// <param name="sprayLines"></param>
        /// <param name="moveLine"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private bool CheckLegalityWithBoundary(List<Line> sprayLines, Line moveLine, double maxLength)
        {
            bool checkRes = true;
            foreach (var line in sprayLines)
            {
                double distance = moveLine.IndexedDistance(line);
                if (distance < minSpacing || distance > maxLength)
                {
                    return false;
                }
            }
            return checkRes;
        }

        /// <summary>
        /// 获取喷淋线的移动信息
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="maxSpacing"></param>
        /// <param name="moveDir"></param>
        /// <param name="length"></param>
        private void CalMoveSprayInfo(KeyValuePair<Line, List<SprayLayoutData>> spray, double maxSpacing, out Vector3d moveDir, out double length, out bool isIn, out bool needMove)
        {
            var sortValue = spray.Value.Select(x =>
            {
                var closePt = spray.Key.GetClosestPointTo(x.Position, true);
                return closePt.DistanceTo(x.Position);
            }).OrderBy(x => x).ToList();

            SprayLayoutData firSpray = spray.Value.First();
            Vector3d lineDir = (spray.Key.StartPoint - spray.Key.EndPoint).GetNormal();
            Vector3d dir = (spray.Key.GetClosestPointTo(firSpray.Position, true) - firSpray.Position).GetNormal();
            moveDir = Math.Abs(firSpray.mainDir.DotProduct(lineDir)) > Math.Abs(firSpray.otherDir.DotProduct(lineDir)) ? firSpray.otherDir : firSpray.mainDir;
            length = 0;
            isIn = true;
            needMove = false;
            if (sortValue.First() < minSpacing)
            {
                moveDir = moveDir.DotProduct(dir) > 0 ? -moveDir : moveDir;
                needMove = true;
                length = Math.Ceiling((minSpacing - sortValue.First()) / moveLength) * moveLength;
            }
            if (sortValue.Last() > maxSpacing)
            {
                needMove = true;
                isIn = false;
                moveDir = moveDir.DotProduct(dir) > 0 ? moveDir : -moveDir;
                length = Math.Ceiling((sortValue.Last() - maxSpacing) / moveLength) * moveLength;
            }
        }
    }
}
    
