using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness
{
    public class AvoidBeamByPointService
    {
        double spcing = 300;
        double maxSpacing = 3400;
        double minSpacing = 100;
        double sprayWidth = 200;

        public void AvoidBeam(Polyline polyline, List<SprayLayoutData> sprays, List<Polyline> columnPolys,  double maxValue, double minValue)
        {
            maxSpacing = maxValue;
            minSpacing = minValue;

            //获得所有梁
            var allBeams = GetBeam(polyline).Cast<ThIfcLineBeam>().ToList();
            allBeams.ForEach(x => x.ExtendBoth(20, 20));

            //计算可布置区域
            var layoutAreas = GetLayoutArea(polyline, allBeams, columnPolys);
#if DEBUG
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in layoutAreas)
                {
                    acdb.ModelSpace.Add(item);
                }
            }
#endif
            //计算出不合法的喷淋点位
            var moveSprays = CalIllegalSpary(sprays, layoutAreas);

            //移动并校核喷淋
            MoveSpray(moveSprays, layoutAreas, sprays);
        }

        /// <summary>
        /// 尝试移动喷淋
        /// </summary>
        /// <param name="moveSprays"></param>
        /// <param name="layoutAreas"></param>
        /// <param name="allSprays"></param>
        private void MoveSpray(List<SprayLayoutData> moveSprays, List<Polyline> layoutAreas, List<SprayLayoutData> allSprays)
        {
            foreach (var spray in moveSprays)
            {
                if(!GetMoveInfo(layoutAreas, allSprays, spray))
                {
                    var sprayCircle = new Circle(spray.Position, Vector3d.ZAxis, sprayWidth);
                    using (AcadDatabase db = AcadDatabase.Active())
                    {
                        sprayCircle.ColorIndex = 1;
                        db.ModelSpace.Add(sprayCircle);
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
        private bool GetMoveInfo(List<Polyline> layoutAreas, List<SprayLayoutData> allSprays, SprayLayoutData spray)
        {
            //计算可布置区域
            var lAreas = GetSprayLayoutArea(spray, layoutAreas).OrderBy(x => x.Distance(spray.Position)).Take(2).ToList();

            //检测主要方向上是否能移动
            foreach (var area in lAreas)
            {
                List<Point3d> intersectPTs = new List<Point3d>();
                Line tempLine = new Line(spray.Position + spray.mainDir * spcing, spray.Position - spray.mainDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                tempLine = new Line(spray.Position + spray.otherDir * spcing, spray.Position - spray.otherDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                if (intersectPTs.Count > 0)
                {
                    var closePt = intersectPTs.OrderBy(x => x.DistanceTo(spray.Position)).First();
                    double moveLength = closePt.DistanceTo(spray.Position);
                    Vector3d moveDir = (closePt - spray.Position).GetNormal();
                    if (CheckMoveResult(allSprays, layoutAreas, spray, area, moveDir, moveLength, 100))
                    {
                        return true;
                    }
                    else
                    {
                        //再用50的倍数尝试
                        moveLength = Math.Ceiling(closePt.DistanceTo(spray.Position) / 50) * 50;
                        if (CheckMoveResult(allSprays, layoutAreas, spray, area, moveDir, moveLength, 50))
                        {
                            return true;
                        }
                    }
                }
            }

            //主要方向无法移动就“斜”移
            foreach (var area in lAreas)
            {
                List<Point3d> resPTs = new List<Point3d>();
                resPTs.Add(area.GetClosestPointTo(spray.Position, true));

                var closePt = resPTs.OrderBy(x => x.DistanceTo(spray.Position)).First();
                Vector3d moveDir = (closePt - spray.Position).GetNormal();
                double distance = closePt.DistanceTo(spray.Position);
                if (CheckMoveResult(allSprays, layoutAreas, spray, area, moveDir, distance, 100))
                {
                    return true;
                }
                else
                {
                    //50的倍数尝试
                    if (CheckMoveResult(allSprays, layoutAreas, spray, area, moveDir, distance, 50))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检测移动后的喷淋是否符合规范并且更新喷淋点位
        /// </summary>
        /// <param name="allSprays"></param>
        /// <param name="spray"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveLength"></param>
        /// <returns></returns>
        private bool CheckMoveResult(List<SprayLayoutData> allSprays, List<Polyline> allAreas, SprayLayoutData spray, Polyline area, Vector3d moveDir, double moveLength, double index)
        {
            double xValue = Math.Ceiling(Math.Abs(moveDir.X) * moveLength / index) * index;
            double yValue = Math.Ceiling(Math.Abs(moveDir.Y) * moveLength / index) * index;
            Vector3d xDir = moveDir.X > 0 ? Vector3d.XAxis : -Vector3d.XAxis;
            Vector3d yDir = moveDir.Y > 0 ? Vector3d.YAxis : -Vector3d.YAxis;

            var newPosition = spray.Position + xValue * xDir + yValue * yDir;
            if (!area.Contains(newPosition))
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
            if (AdjustAroundSprays(allSprays, checkSprays, newPosition, allAreas))
            {
                spray.Position = newPosition;
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
        private bool AdjustAroundSprays(List<SprayLayoutData> allSprays, List<SprayLayoutData> checkSprays, Point3d newPosition, List<Polyline> allAreas)
        {
            Dictionary<SprayLayoutData, Point3d> sprayDic = new Dictionary<SprayLayoutData, Point3d>();
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
                if (allAreas.Where(x=>x.Contains(movePosition)).Count() <= 0)
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

                sprayDic.Add(cSpray, movePosition);
            }

            foreach (var sDic in sprayDic)
            {
                sDic.Key.Position = sDic.Value;
            }
            return true;
        }

        /// <summary>
        /// 计算需挪动喷淋可移动区域
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="layoutAreas"></param>
        /// <returns></returns>
        private List<Polyline> GetSprayLayoutArea(SprayLayoutData spray, List<Polyline> layoutAreas)
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
        private List<SprayLayoutData> CalIllegalSpary(List<SprayLayoutData> sprays, List<Polyline> layoutAreas)
        {
            return sprays.Where(x => layoutAreas.Where(y => y.Contains(x.Position)).Count() <= 0).ToList();
        }

        /// <summary>
        /// 计算可布置区域
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="allBeams"></param>
        /// <returns></returns>
        private List<Polyline> GetLayoutArea(Polyline polyline, List<ThIfcLineBeam> allBeams, List<Polyline> columnPoly)
        {
            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (var beam in allBeams)
            {
                dBObjects.Add(beam.Outline as Polyline);
            }

            foreach (var cPoly in columnPoly)
            {
                dBObjects.Add(cPoly);
            }
            var layoutAreas = polyline.Difference(dBObjects).Cast<Polyline>().SelectMany(x => x.Buffer(-spcing).Cast<Polyline>()).Where(x => x.Area > 0).ToList();
            return layoutAreas;
        }

        /// <summary>
        /// 获得所有梁
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<ThIfcBeam> GetBeam(Polyline polyline)
        {
            List<ThIfcBeam> beams = new List<ThIfcBeam>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamEngine = ThMEPEngineCoreService.Instance.CreateBeamEngine())
            {
                beamEngine.Recognize(Active.Database, polyline.Vertices());

                foreach (var beam in beamEngine.Elements)
                {
                    if (beam is ThIfcBeam thBeam)
                    {
                        beams.Add(thBeam);
                    }
                }
            }

            return beams;
        }
    }
}
