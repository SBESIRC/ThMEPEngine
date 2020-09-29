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

namespace ThMEPWSS.Bussiness
{
    public class CheckProtectService
    {
        readonly double minSpacing = 400;
        readonly double moveLength = 100;

        public void CheckBoundarySprays(Polyline polyline, List<SprayLayoutData> sprays, double length)
        {
            var bSprays = GetBoundarySpray(polyline, sprays, length);
            AdjustSprayLine(bSprays, length / 2, sprays);
        }

        /// <summary>
        /// 调整喷淋排布线
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="maxSpacing"></param>
        public void AdjustSprayLine(Dictionary<Line, List<SprayLayoutData>> sprays, double maxSpacing, List<SprayLayoutData> allSprays)
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
                    var thisLine = firSpray.Value.First().GetOtherPolylineByDir(moveDir.Value);
                    var moveLine = thisLine.MovePolyline(moveLength, moveDir.Value);
                    var resSprays = allSprays.Where(x => x.tLine == thisLine || x.vLine == thisLine).ToList();
                    if (CheckMoveLineSpcing(resSprays, moveDir.Value, moveLine, maxSpacing * 2) &&
                        CheckLegalityWithBoundary(sprayLst.Select(x => x.Key).ToList(), moveLine, maxSpacing))
                    {
                        SprayDataOperateService.UpdateSpraysLine(allSprays, thisLine, moveLine);
                    }

                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        //Polyline tempLine = moveLine.Clone() as Polyline;
                        //tempLine.ColorIndex = 3;
                        //acdb.ModelSpace.Add(tempLine);
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
        private bool CheckMoveLineSpcing(List<SprayLayoutData> sprays, Vector3d moveDir, Line moveLine, double maxLength)
        {
            foreach (var spray in sprays)
            {
                var nextLine = spray.GetOtherNextPolylineByDir(moveDir);
                if (nextLine != null)
                {
                    if (moveLine.IndexedDistance(nextLine) > maxLength)
                    {
                        return false;
                    }
                }

                var prevLine = spray.GetOtherPrePolylineByDir(moveDir);
                if (prevLine != null)
                {
                    if (moveLine.IndexedDistance(prevLine) > maxLength)
                    {
                        return false;
                    }
                }
            }
            
            return true;
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

        /// <summary>
        /// 移动喷淋线
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="moveDir"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private Line MoveSprayLine(SprayLayoutData spray, Vector3d moveDir, double length)
        {
            return spray.GetOtherPolylineByDir(moveDir).MovePolyline(length, moveDir);
        }

        /// <summary>
        /// 获取边界喷淋
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="sprays"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private Dictionary<Line, List<SprayLayoutData>> GetBoundarySpray(Polyline polyline, List<SprayLayoutData> sprays, double length)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }

            Dictionary<Line, List<SprayLayoutData>> sprayDic = new Dictionary<Line, List<SprayLayoutData>>(); 
            foreach (var line in lines)
            {
                var linePoly = expandLine(line, length);
                var resSprays = GetSprays(line, linePoly, sprays);
                if (resSprays.Count > 0)
                {
                    sprayDic.Add(line, resSprays);
                }
            }

            return sprayDic;
        }

        /// <summary>
        /// 获取附近的喷淋点
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private List<SprayLayoutData> GetSprays(Line line, Polyline linePoly, List<SprayLayoutData> sprays)
        {
            var resPrays = sprays.Where(x => linePoly.IndexedContains(x.Position)).ToList();

            List<SprayLayoutData> closetPolys = new List<SprayLayoutData>();
            var nerstSpray = resPrays.OrderBy(x =>
            {
                var closetPt = line.GetClosestPointTo(x.Position, false);
                return closetPt.DistanceTo(x.Position);
            }).FirstOrDefault();
            if (nerstSpray != null)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var sprayDir = nerstSpray.mainDir;
                if (Math.Abs(lineDir.DotProduct(nerstSpray.mainDir)) < Math.Abs(lineDir.DotProduct(nerstSpray.otherDir)))
                {
                    sprayDir = nerstSpray.otherDir;
                }

                var sprayLine = nerstSpray.GetPolylineByDir(sprayDir);
                closetPolys.AddRange(resPrays.Where(x => x.vLine == sprayLine || x.tLine == sprayLine));
            }
            return closetPolys;
        }

        /// <summary>
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint + moveDir * distance;
            Point3d p2 = line.EndPoint + moveDir * distance;
            Point3d p3 = line.EndPoint - moveDir * distance;
            Point3d p4 = line.StartPoint - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
    
