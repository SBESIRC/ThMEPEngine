using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThCADExtension;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.Utils;
using ThWSS.Bussiness;

namespace ThMEPWSS.Bussiness
{
    public class AvoidBeamService
    {
        readonly double dis = 300;
        readonly double moveLength = 100;
        CheckService checkService = new CheckService();

        public void AvoidBeam(Polyline polyline, List<SprayLayoutData> sprays)
        {
            //获得所有梁
            var allBeams = GetBeam(polyline);
            checkService.allBeams = allBeams;

            Dictionary<SprayLayoutData, List<ThIfcBeam>> matterSprays = new Dictionary<SprayLayoutData, List<ThIfcBeam>>();
            foreach (var spray in sprays)
            {
                foreach (var beam in allBeams)
                {
                    var closet = (beam.Outline as Polyline).GetClosestPointTo(spray.Position, false);
                    if (closet.DistanceTo(spray.Position) < dis)
                    {
                        if (matterSprays.Keys.Contains(spray))
                        {
                            matterSprays[spray].Add(beam);
                        }
                        else
                        {
                            matterSprays.Add(spray, new List<ThIfcBeam>() { beam });
                        }
                    }
                }
            }

            MoveSprayLine(matterSprays, sprays);
            InsertSprayService.InsertSprayBlock(matterSprays.Keys.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);
        }

        /// <summary>
        /// 避梁
        /// </summary>
        /// <param name="matterSprays"></param>
        /// <param name="allSprays"></param>
        private void MoveSprayLine(Dictionary<SprayLayoutData, List<ThIfcBeam>> matterSprays, List<SprayLayoutData> allSprays)
        {
            while (matterSprays.Count > 0)
            {
                KeyValuePair<SprayLayoutData, List<ThIfcBeam>> firSpray = matterSprays.First();
                matterSprays.Remove(firSpray.Key);
                if (firSpray.Value.Count > 1)
                {
                    continue;
                }

                var beam = firSpray.Value.First();
                var beamDir = (beam as ThIfcLineBeam).Direction.GetNormal();
                var moveDir = firSpray.Key.mainDir;
                var otherDir = firSpray.Key.otherDir;
                var beamPoly = beam.Outline as Polyline;
                if (Math.Abs(firSpray.Key.mainDir.DotProduct(beamDir)) > Math.Abs(firSpray.Key.otherDir.DotProduct(beamDir)))
                {
                    moveDir = firSpray.Key.otherDir;
                    otherDir = firSpray.Key.mainDir;
                }

                bool needMove = true;
                if (beamPoly.IsInPolyline(firSpray.Key))
                {
                    if (needMove)
                    {
                        needMove = !TryMoveTest(beamPoly, firSpray, beamDir, moveDir, otherDir, allSprays, true);
                    }

                    if (needMove)
                    {
                        TryMoveTest(beamPoly, firSpray, beamDir, -moveDir, otherDir, allSprays, true);
                    }
                }
                else
                {
                    var closePt = beamPoly.GetClosestPointTo(firSpray.Key.Position, true);
                    if ((firSpray.Key.Position - closePt).GetNormal().DotProduct(moveDir) < 0)
                    {
                        moveDir = -moveDir;
                    }
                    TryMoveTest(beamPoly, firSpray, beamDir, moveDir, otherDir, allSprays, false);
                }
            }
        }

        /// <summary>
        /// 尝试移动策略
        /// </summary>
        /// <param name="beamPoly"></param>
        /// <param name="spray"></param>
        /// <param name="beamDir"></param>
        /// <param name="moveDir"></param>
        /// <param name="otherDir"></param>
        /// <param name="allSprays"></param>
        private bool TryMoveTest(Polyline beamPoly, KeyValuePair<SprayLayoutData, List<ThIfcBeam>> spray, Vector3d beamDir, Vector3d moveDir, Vector3d otherDir, List<SprayLayoutData> allSprays, bool isInBeam)
        {
            var length = CalMovelength(beamPoly, spray.Key.Position, beamDir, moveDir, isInBeam);
            for (int i = 0; i < 3; i++)
            {
                var newPoly = spray.Key.GetPolylineByDir(otherDir).MovePolyline(length, moveDir);
                if (checkService.CheckSprayData(newPoly, allSprays, moveDir, dis))
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        newPoly.ColorIndex = 2;
                        acdb.ModelSpace.Add(newPoly);
                    }
                    return true;
                }
                else
                {
                    length += moveLength;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算移动距离
        /// </summary>
        /// <param name="beamPoly"></param>
        /// <param name="pt"></param>
        /// <param name="beamDir"></param>
        /// <param name="moveDir"></param>
        /// <returns></returns>
        private double CalMovelength(Polyline beamPoly, Point3d pt, Vector3d beamDir, Vector3d moveDir, bool isInBeam = true)
        {
            if(beamDir.DotProduct(moveDir) < 0)
            {
                beamDir = -beamDir;
            }

            int index = -1;
            if (isInBeam)
            {
                index = 1;
            }
            var closetPt = beamPoly.GetClosestPointTo(pt, false);
            double angle = Math.PI / 2 - beamDir.GetAngleTo(moveDir);
            double length = dis / Math.Cos(angle) + closetPt.DistanceTo(pt) * index;
            return Math.Ceiling(length / 100) * 100;
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

        /// <summary>
        /// 获得所有次梁
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<ThIfcBeam> GetSecondBeam(Polyline polyline)
        {
            List<ThIfcBeam> beams = new List<ThIfcBeam>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThBeamConnectRecogitionEngine beamEngine = new ThBeamConnectRecogitionEngine())
            {
                beamEngine.Recognize(Active.Database, polyline.Vertices());

                foreach (var beam in beamEngine.SecondaryBeamLinks)
                {
                    foreach (var secondBeam in beam.Beams)
                    {
                        if (secondBeam is ThIfcBeam thBeam)
                        {
                            beams.Add(thBeam);
                        }
                    }
                }
            }

            return beams;
        }
    }
}
