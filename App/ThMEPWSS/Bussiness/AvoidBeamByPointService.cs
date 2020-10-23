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
using ThMEPEngineCore.Engine;
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
            var allBeams = GetBeam(polyline);

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
                var lAreas = GetSprayLayoutArea(spray, layoutAreas);
                lAreas = lAreas.OrderBy(x => x.Distance(spray.Position)).Take(2).ToList();
                if(!GetMoveInfo(lAreas, allSprays, spray))
                {
                    var sprayCircle = new Circle(spray.Position, Vector3d.ZAxis, sprayWidth);
                    using (AcadDatabase db = AcadDatabase.Active())
                    {
                        sprayCircle.ColorIndex = 1;
                        db.ModelSpace.Add(sprayCircle);
                    }
                }

                //if (spray.Position.DistanceTo(new Point3d(-3738593.8598, 1112654.5059, 0)) < 10)
                //{
                //    var s = 1;
                //}
            }
        }

        /// <summary>
        /// 计算喷淋移动信息
        /// </summary>
        /// <param name="areas"></param>
        /// <param name="allSprays"></param>
        /// <param name="spray"></param>
        /// <returns></returns>
        private bool GetMoveInfo(List<Polyline> areas, List<SprayLayoutData> allSprays, SprayLayoutData spray)
        {
            //检测主要方向上是否能移动
            foreach (var area in areas)
            {
                List<Point3d> intersectPTs = new List<Point3d>();
                Line tempLine = new Line(spray.Position + spray.mainDir * spcing, spray.Position - spray.mainDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                tempLine = new Line(spray.Position + spray.otherDir * spcing, spray.Position - spray.otherDir * spcing);
                intersectPTs.AddRange(area.Intersect(tempLine, Intersect.ExtendArgument));

                if (intersectPTs.Count > 0)
                {
                    var closePt = intersectPTs.OrderBy(x => x.DistanceTo(spray.Position)).First();
                    double moveLength = Math.Ceiling(closePt.DistanceTo(spray.Position) / 100) * 100;
                    Vector3d moveDir = (closePt - spray.Position).GetNormal();
                    if (CheckMoveResult(allSprays, spray, area, moveDir, moveLength))
                    {
                        return true;
                    }
                    else
                    {
                        //再用50的倍数尝试
                        moveLength = Math.Ceiling(closePt.DistanceTo(spray.Position) / 50) * 50;
                        if (CheckMoveResult(allSprays, spray, area, moveDir, moveLength))
                        {
                            return true;
                        }
                    }
                }
            }

            //主要方向无法移动就“斜”移
            foreach (var area in areas)
            {
                List<Point3d> resPTs = new List<Point3d>();
                resPTs.Add(area.GetClosestPointTo(spray.Position, true));

                var closePt = resPTs.OrderBy(x => x.DistanceTo(spray.Position)).First();
                Vector3d moveDir = (closePt - spray.Position).GetNormal();
                double distance = closePt.DistanceTo(spray.Position);
                double moveLength = Math.Sqrt(Math.Pow(Math.Ceiling(Math.Abs(distance * moveDir.X) / 100) * 100, 2) 
                    + Math.Pow(Math.Ceiling(Math.Abs(distance * moveDir.Y) / 100) * 100, 2));

                if (CheckMoveResult(allSprays, spray, area, moveDir, moveLength))
                {
                    return true;
                }
                else
                {
                    //50的倍数尝试
                    moveLength = Math.Sqrt(Math.Pow(Math.Ceiling(Math.Abs(distance * moveDir.X) / 50) * 50, 2)
                    + Math.Pow(Math.Ceiling(Math.Abs(distance * moveDir.Y) / 50) * 50, 2));
                    if (CheckMoveResult(allSprays, spray, area, moveDir, moveLength))
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
        private bool CheckMoveResult(List<SprayLayoutData> allSprays, SprayLayoutData spray, Polyline area, Vector3d moveDir, double moveLength)
        {
            var newPosition = spray.Position + moveDir * moveLength;
            if (!area.Contains(newPosition))
            {
                return false;
            }

            var aroundSprays = spray.GetAroundSprays(allSprays);
            foreach (var aSpray in aroundSprays)
            {
                double distance = newPosition.DistanceTo(aSpray.Position);
                Vector3d compareDir = (aSpray.Position - newPosition).GetNormal();
                double compareX = Math.Abs(compareDir.X) > Math.Abs(compareDir.Y) ? Math.Abs(compareDir.X) : Math.Abs(compareDir.Y);
                double compareValue = distance * compareX;
                if (compareValue < minSpacing || compareValue > maxSpacing)
                {
                    return false;
                }
            }

            spray.Position = newPosition;
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
        private List<Polyline> GetLayoutArea(Polyline polyline, List<ThIfcBeam> allBeams, List<Polyline> columnPoly)
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
            using (ThBeamRecognitionEngine beamEngine = new ThBeamRecognitionEngine())
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
