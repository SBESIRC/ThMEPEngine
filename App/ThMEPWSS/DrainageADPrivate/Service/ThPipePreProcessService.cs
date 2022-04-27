using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThPipePreProcessService
    {
        public static void ConnectPipeToNearVerticalPipe(ThDrainageADPDataPass dataPass, out Dictionary<Point3d, List<Line>> ptDict, out Dictionary<Point3d, bool> ptCoolHotDict)
        {
            var coolPipe = new List<Line>();
            var hotPipe = new List<Line>();
            var verticalPipe = new List<Line>();

            coolPipe.AddRange(dataPass.CoolPipeTopView);
            hotPipe.AddRange(dataPass.HotPipeTopView);
            verticalPipe.AddRange(dataPass.VerticalPipe);

            ConnectSingleSystemToNearVerticalPipe(coolPipe, verticalPipe);
            ConnectSingleSystemToNearVerticalPipe(hotPipe, verticalPipe);

            DrawUtils.ShowGeometry(coolPipe, "l0adjustCool", 142);
            DrawUtils.ShowGeometry(hotPipe , "l0adjustHot", 22);

            var ptDictCool = ThDrainageADTreeService.GetPtDict(coolPipe);
            var ptDictHot = ThDrainageADTreeService.GetPtDict(hotPipe);

            ptDict = new Dictionary<Point3d, List<Line>>();
            ptCoolHotDict = new Dictionary<Point3d, bool>();

            foreach (var item in ptDictCool)
            {
                ptDict.Add(item.Key, item.Value);
                ptCoolHotDict.Add(item.Key, true);
            }
            foreach (var item in ptDictHot)
            {
                ptDict.Add(item.Key, item.Value);
                ptCoolHotDict.Add(item.Key, false);
            }
        }

        private static void ConnectSingleSystemToNearVerticalPipe(List<Line> pipe, List<Line> verticalPipe)
        {
            var needAdd = new List<Line>();
            foreach (var vpipe in verticalPipe)
            {
                var vst = vpipe.StartPoint;
                var bNeedAddToPipeS = AdjustLineNearPt(vst, pipe);
                var ved = vpipe.EndPoint;
                var bNeedAddToPipeE = AdjustLineNearPt(ved, pipe);

                if (bNeedAddToPipeS || bNeedAddToPipeE)
                {
                    needAdd.Add(vpipe);
                }
            }

            pipe.AddRange(needAdd);
            verticalPipe.RemoveAll(x => needAdd.Contains(x));

        }

        private static bool AdjustLineNearPt(Point3d pt, List<Line> pipes)
        {
            var minDistTol = 100;
            var bIfAddVertical = false;
            var nearpipe = pipes.Where(x => x.StartPoint.DistanceTo(pt) < minDistTol ||
                                            x.EndPoint.DistanceTo(pt) < minDistTol).ToList();

            var removePipe = new List<Line>();

            foreach (var nPipe in nearpipe)
            {
                var lineNearPt = nPipe.StartPoint;
                var lineOtherPt = nPipe.EndPoint;
                if (lineNearPt.DistanceTo(pt) > lineOtherPt.DistanceTo(pt))
                {
                    lineNearPt = nPipe.EndPoint;
                    lineOtherPt = nPipe.StartPoint;
                }

                if (lineNearPt.DistanceTo(pt) <= 1)
                {
                    bIfAddVertical = true;
                    continue;
                }

                var addDir = lineNearPt - pt;
                var lineDir = nPipe.EndPoint - nPipe.StartPoint;
                var angle = addDir.GetAngleTo(lineDir);
                if (Math.Abs(Math.Cos(angle)) > Math.Cos(1 * Math.PI / 180))
                {
                    //移动点的延长方向和原线必须同向。防止误改附近的管线（比如附近热水的立管）
                    var newLine = new Line(pt, lineOtherPt);
                    removePipe.Add(nPipe);
                    pipes.Add(newLine);
                    bIfAddVertical = true;
                }
            }
            pipes.RemoveAll(x => removePipe.Contains(x));
            return bIfAddVertical;
        }



    }
}
