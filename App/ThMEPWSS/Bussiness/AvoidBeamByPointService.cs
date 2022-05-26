using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Bussiness.BoundaryProtectBussiness;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness
{
    public class AvoidBeamByPointService
    {
        double spcing = 300;
        double maxSpacing = 3400;
        double minSpacing = 100;

        public void AvoidBeam(Polyline polyline, List<SprayLayoutData> sprays, List<Polyline> columnPolys, List<Entity> beamPolys, List<Polyline> wallPolys, 
            double maxValue, double minValue, Matrix3d matrix)
        {
            maxSpacing = maxValue;
            minSpacing = minValue;

            //计算可布置区域
            var layoutAreas = CreateLayoutAreaService.GetLayoutArea(polyline, beamPolys, columnPolys, wallPolys, spcing);

            //计算出不合法的喷淋点位
            var moveSprays = CalIllegalSpary(sprays, layoutAreas);

            //计算出边界喷淋
            BoundaryProtestService protestService = new BoundaryProtestService();
            var bSprays = protestService.GetBoundarySpray(polyline, new List<Polyline>() { polyline }, sprays, maxSpacing);

            //移动并校核喷淋
            MoveSpray(moveSprays, layoutAreas, sprays, bSprays);

            //计算出校核之后任然不合法的喷淋点位
            var errorSprays = CalIllegalSpary(sprays, layoutAreas);

            //打印可布置区域
            MarkService.PrintLayoutArea(layoutAreas, matrix);

            //打印错误喷淋点位
            MarkService.PrintErrorSpray(errorSprays, matrix);
        }

        /// <summary>
        /// 尝试移动喷淋
        /// </summary>
        /// <param name="moveSprays"></param>
        /// <param name="layoutAreas"></param>
        /// <param name="allSprays"></param>
        private void MoveSpray(List<SprayLayoutData> moveSprays, List<MPolygon> layoutAreas, List<SprayLayoutData> allSprays, Dictionary<Line, List<SprayLayoutData>> bSprays)
        {
            foreach (var spray in moveSprays)
            {
                if (GetMoveInfo(layoutAreas, allSprays, spray, bSprays, out List<KeyValuePair<SprayLayoutData, Point3d>> moveInfo))
                {
                    //更新喷淋点位
                    foreach (var mInfo in moveInfo)
                    {
                        mInfo.Key.Position = mInfo.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 计算喷淋移动信息
        /// </summary>
        /// <param name="areas"></param>
        /// <param name="allSprays"></param>
        /// <param name="spray"></param>
        /// <returns></returns>
        private bool GetMoveInfo(List<MPolygon> layoutAreas, List<SprayLayoutData> allSprays, SprayLayoutData spray, Dictionary<Line, List<SprayLayoutData>> bSprays, 
            out List<KeyValuePair<SprayLayoutData, Point3d>> moveInfo)
        {
            moveInfo = null;
            var bLines = bSprays.Where(x => x.Value.Contains(spray)).Select(x => x.Key).ToList();
            bool isBoundary = false;
            if (bLines.Count > 0)
            {
                isBoundary = true;
            }

            //计算可布置区域
            var lAreas = GetSprayLayoutArea(spray, layoutAreas).OrderBy(x => x.Outline().Distance(spray.Position)).ToList();

            CheckService checkService = new CheckService();
            //检测主要方向上是否能移动
            foreach (var area in lAreas)
            {
                List<Point3d> intersectPTs = new List<Point3d>();
                Line tempLine = new Line(spray.Position + spray.mainDir * spcing, spray.Position - spray.mainDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                tempLine = new Line(spray.Position + spray.otherDir * spcing, spray.Position - spray.otherDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                intersectPTs = intersectPTs.OrderBy(x => x.DistanceTo(spray.Position)).ToList();
                foreach (var interPt in intersectPTs)
                {
                    var newPosition = MoveSpray(spray, interPt, 100);
                    if (CheckMoveResult(allSprays, layoutAreas, spray, area, newPosition, out List<KeyValuePair<SprayLayoutData, Point3d>> movePtInfo))
                    {
                        if (isBoundary)
                        {
                            if (checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, maxSpacing / 2))
                            {
                                moveInfo = movePtInfo;
                                return true;
                            }
                            else
                            {
                                if (moveInfo == null || moveInfo.Count <= 0)
                                {
                                    moveInfo = movePtInfo;
                                }
                            }
                        }
                        else
                        {
                            moveInfo = movePtInfo;
                            return true;
                        }
                    }
                    else
                    {
                        //再用50的倍数尝试
                        newPosition = MoveSpray(spray, interPt, 50);
                        if (CheckMoveResult(allSprays, layoutAreas, spray, area, newPosition, out movePtInfo))
                        {
                            if (isBoundary)
                            {
                                if (checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, maxSpacing / 2))
                                {
                                    moveInfo = movePtInfo;
                                    return true;
                                }
                                else
                                {
                                    if (moveInfo == null || moveInfo.Count <= 0)
                                    {
                                        moveInfo = movePtInfo;
                                    }
                                }
                            }
                            else
                            {
                                moveInfo = movePtInfo;
                                return true;
                            }
                        }
                    }
                }
            }

            //主要方向无法移动就“斜”移
            foreach (var area in lAreas)
            {
                List<Point3d> resPTs = new List<Point3d>();
                resPTs.Add(area.Outline().GetClosestPointTo(spray.Position, true));

                var closePt = resPTs.OrderBy(x => x.DistanceTo(spray.Position)).First();
                var newPosition = MoveSpray(spray, closePt, 100);
                if (CheckMoveResult(allSprays, layoutAreas, spray, area, newPosition, out List<KeyValuePair<SprayLayoutData, Point3d>> movePtInfo))
                {
                    if (isBoundary)
                    {
                        if (checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, maxSpacing / 2))
                        {
                            moveInfo = movePtInfo;
                            return true;
                        }
                        else
                        {
                            if (moveInfo == null || moveInfo.Count <= 0)
                            {
                                moveInfo = movePtInfo;
                            }
                        }
                    }
                    else
                    {
                        moveInfo = movePtInfo;
                        return true;
                    }
                }
                else
                {
                    //50的倍数尝试
                    newPosition = MoveSpray(spray, closePt, 50);
                    if (CheckMoveResult(allSprays, layoutAreas, spray, area, newPosition, out movePtInfo))
                    {
                        if (isBoundary)
                        {
                            if (checkService.CheckBoundaryLines(bLines, spray.Position, newPosition, maxSpacing / 2))
                            {
                                moveInfo = movePtInfo;
                                return true;
                            }
                            else
                            {
                                if (moveInfo == null || moveInfo.Count <= 0)
                                {
                                    moveInfo = movePtInfo;
                                }
                            }
                        }
                        else
                        {
                            moveInfo = movePtInfo;
                            return true;
                        }
                    }
                }
            }

            if (moveInfo != null && moveInfo.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 计算喷淋移动点位
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="pt"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private Point3d MoveSpray(SprayLayoutData spray, Point3d pt, double index)
        {
            Vector3d moveDir = (pt - spray.Position).GetNormal();
            double distance = pt.DistanceTo(spray.Position);

            double xValue = Math.Ceiling(Math.Abs(moveDir.X) * distance / index) * index;
            double yValue = Math.Ceiling(Math.Abs(moveDir.Y) * distance / index) * index;
            Vector3d xDir = moveDir.X > 0 ? Vector3d.XAxis : -Vector3d.XAxis;
            Vector3d yDir = moveDir.Y > 0 ? Vector3d.YAxis : -Vector3d.YAxis;

            var newPosition = spray.Position + xValue * xDir + yValue * yDir;
            return newPosition;
        }

        /// <summary>
        /// 检测移动后的喷淋是否符合规范并且更新喷淋点位
        /// </summary>
        /// <param name="allSprays"></param>
        /// <param name="spray"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveLength"></param>
        /// <returns></returns>
        private bool CheckMoveResult(List<SprayLayoutData> allSprays, List<MPolygon> allAreas, SprayLayoutData spray, MPolygon area, Point3d newPosition,
            out List<KeyValuePair<SprayLayoutData, Point3d>> movePtInfo)
        {
            movePtInfo = new List<KeyValuePair<SprayLayoutData, Point3d>>();
            if (!area.Intersects(new DBPoint(newPosition)))
            {
                return false;
            }

            List<SprayLayoutData> checkSprays = new List<SprayLayoutData>();
            var aroundSprays = spray.GetAroundSprays(allSprays);
            foreach (var aSpray in aroundSprays)
            {
                //校验喷淋是否满足间距
                CheckService checkService = new CheckService();
                if (!checkService.CheckSprayPtDistance(aSpray.Position, newPosition, maxSpacing, minSpacing))
                {
                    checkSprays.Add(aSpray);
                }
            }

            //尝试调整周围有问题的点
            if (AdjustAroundSprays(allSprays, checkSprays, newPosition, allAreas, movePtInfo))
            {
                movePtInfo.Add(new KeyValuePair<SprayLayoutData, Point3d>(spray, newPosition));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 联动适应周围点
        /// </summary>
        /// <param name="allSprays"></param>
        /// <param name="checkSprays"></param>
        /// <param name="newPosition"></param>
        /// <param name="allAreas"></param>
        /// <returns></returns>
        private bool AdjustAroundSprays(List<SprayLayoutData> allSprays, List<SprayLayoutData> checkSprays, Point3d newPosition, List<MPolygon> allAreas, 
            List<KeyValuePair<SprayLayoutData, Point3d>> movePtInfo)
        {
            foreach (var cSpray in checkSprays)
            {
                var compareDir = (cSpray.Position - newPosition).GetNormal();
                var compareValue = cSpray.Position.DistanceTo(newPosition);
                double moveLength = 0;
                Vector3d moveDir = Vector3d.XAxis;
                if (Math.Abs(compareDir.X) > Math.Abs(compareDir.Y))
                {
                    double maxLength = (maxSpacing - compareValue * Math.Abs(compareDir.X)) / 100 * 100;
                    double minLength = (minSpacing - compareValue * Math.Abs(compareDir.X)) / 100 * 100;
                    moveLength = Math.Abs(maxLength) < Math.Abs(minLength) ? maxLength : minLength;
                    moveDir = Vector3d.XAxis.DotProduct(compareDir) > 0 ? Vector3d.XAxis : -Vector3d.XAxis;
                }
                else
                {
                    double maxLength = (maxSpacing - compareValue * Math.Abs(compareDir.Y)) / 100 * 100;
                    double minLength = (minSpacing - compareValue * Math.Abs(compareDir.Y)) / 100 * 100;
                    moveLength = Math.Abs(maxLength) < Math.Abs(minLength) ? maxLength : minLength;
                    moveDir = Vector3d.YAxis.DotProduct(compareDir) > 0 ? Vector3d.YAxis : -Vector3d.YAxis;
                }

                Point3d movePosition = cSpray.Position + moveLength * moveDir;
                if (allAreas.Where(x => x.Intersects(new DBPoint(movePosition))).Count() <= 0)
                {
                    return false;
                }

                var aroundSprays = cSpray.GetAroundSprays(allSprays);
                foreach (var aSpray in aroundSprays)
                {
                    //校验喷淋是否满足间距
                    CheckService checkService = new CheckService();
                    if (!checkService.CheckSprayPtDistance(aSpray.Position, movePosition, maxSpacing, minSpacing))
                    {
                        return false;
                    }
                }

                movePtInfo.Add(new KeyValuePair<SprayLayoutData, Point3d>(cSpray, movePosition));
            }
            return true;
        }

        /// <summary>
        /// 计算需挪动喷淋可移动区域
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="layoutAreas"></param>
        /// <returns></returns>
        private List<MPolygon> GetSprayLayoutArea(SprayLayoutData spray, List<MPolygon> layoutAreas)
        {
            Polyline polyline = new Polyline() { Closed = true };
            double sprayRange = 400 + spcing * 2;
            polyline.AddVertexAt(0, (spray.Position + spray.mainDir * sprayRange + spray.otherDir * sprayRange).ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, (spray.Position - spray.mainDir * sprayRange + spray.otherDir * sprayRange).ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, (spray.Position - spray.mainDir * sprayRange - spray.otherDir * sprayRange).ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, (spray.Position + spray.mainDir * sprayRange - spray.otherDir * sprayRange).ToPoint2D(), 0, 0, 0);

            return layoutAreas.Where(x => x.Intersects(polyline)).ToList();
        }

        /// <summary>
        /// 计算出不合法的喷淋
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="layoutAreas"></param>
        /// <returns></returns>
        private List<SprayLayoutData> CalIllegalSpary(List<SprayLayoutData> sprays, List<MPolygon> layoutAreas)
        {
            var IllegaleSprays = new List<SprayLayoutData>(sprays);
            var sprayDic = IllegaleSprays.ToDictionary(x => new DBPoint(x.Position), y => y);
            var sprayPts = sprayDic.Keys.ToCollection();
            ThCADCoreNTSSpatialIndex cADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(sprayPts);
            foreach (var area in layoutAreas)
            {
                var legalSprays = cADCoreNTSSpatialIndex.SelectCrossingPolygon(area);
                foreach (DBPoint sp in legalSprays)
                {
                    IllegaleSprays.Remove(sprayDic[sp]);
                }
            }
            return IllegaleSprays;
        }
    }
}
