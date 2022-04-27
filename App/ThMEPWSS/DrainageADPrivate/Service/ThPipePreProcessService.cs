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
            var pipeCool = new List<Line>();
            pipeCool.AddRange(dataPass.CoolPipeTopView);
            pipeCool.AddRange(dataPass.VerticalPipe);
            pipeCool.RemoveAll(x => x.Length <= 1);
            var ptDictCool = ConnectSingleSystemToNearVerticalPipe(pipeCool, dataPass.VerticalPipe);
            var ptCoolDict = RemoveSingleVerticalSetIsCool(ptDictCool, dataPass.VerticalPipe, true);

            var pipeHot = new List<Line>();
            pipeHot.AddRange(dataPass.HotPipeTopView);
            pipeHot.AddRange(dataPass.VerticalPipe);
            pipeHot.RemoveAll(x => x.Length <= 1);
            var ptDictHot = ConnectSingleSystemToNearVerticalPipe(pipeHot, dataPass.VerticalPipe);
            var ptHotDict = RemoveSingleVerticalSetIsCool(ptDictHot, dataPass.VerticalPipe, false);


            ptDict = new Dictionary<Point3d, List<Line>>();
            ptCoolHotDict = new Dictionary<Point3d, bool>();

            foreach (var item in ptDictCool)
            {
                ptDict.Add(item.Key, item.Value);
            }
            foreach (var item in ptDictHot)
            {
                ptDict.Add(item.Key, item.Value);
            }
            foreach (var item in ptCoolDict)
            {
                ptCoolHotDict.Add(item.Key, item.Value);
            }
            foreach (var item in ptHotDict)
            {
                ptCoolHotDict.Add(item.Key, item.Value);
            }

        }
        private static Dictionary<Point3d, List<Line>> ConnectSingleSystemToNearVerticalPipe(List<Line> pipes, List<Line> verticalPipe)
        {
            var minDistTol = 26;
            var tol = new Tolerance(1, 1);
            var ptIsCool = new Dictionary<Point3d, bool>();
            var ptDict = ThDrainageADTreeService.GetPtDict(pipes);

            for (int i = 0; i < ptDict.Count(); i++)
            {
                //立管上的点
                if (ptDict.ElementAt(i).Value.Where(x => verticalPipe.Contains(x)).Any())
                {
                    var ptVertical = ptDict.ElementAt(i).Key;
                    var NearPtDict = ptDict.Where(x => x.Key != ptVertical && x.Key.DistanceTo(ptVertical) <= minDistTol);
                    if (NearPtDict.Count() > 0)
                    {
                        foreach (var nearPt in NearPtDict)
                        {
                            for (int j = 0; j < nearPt.Value.Count; j++)
                            {
                                var line = nearPt.Value[j];
                                var lineNearPt = line.StartPoint;
                                var lineOtherPt = line.EndPoint;
                                if (line.StartPoint.DistanceTo(ptVertical) > line.EndPoint.DistanceTo(ptVertical))
                                {
                                    lineNearPt = line.EndPoint;
                                    lineOtherPt = line.StartPoint;
                                }

                                var addDir = lineNearPt - ptVertical;
                                var lineDir = line.EndPoint - line.StartPoint;
                                var angle = addDir.GetAngleTo(lineDir);
                                if (Math.Abs(Math.Cos(angle)) > Math.Cos(1 * Math.PI / 180))
                                {
                                    //移动点的延长方向和原线必须同向。防止误改附近的管线（比如附近热水的立管）
                                    var newLine = new Line(ptVertical, lineOtherPt);
                                    ptDict.ElementAt(i).Value.Add(newLine);
                                    nearPt.Value.Remove(line);

                                    //更新另一端里面的点线
                                    var otherSidePt = ptDict.Where(x => x.Value.Contains(line) && x.Key != nearPt.Key).First();
                                    otherSidePt.Value.Remove(line);
                                    otherSidePt.Value.Add(newLine);


                                }
                            }
                        }
                    }
                }
            }

            var ptRemove = ptDict.Where(x => x.Value.Count() == 0).Select(x => x.Key).ToList();
            ptRemove.ForEach(x => ptDict.Remove(x));

            return ptDict;
        }
        private static Dictionary<Point3d, bool> RemoveSingleVerticalSetIsCool(Dictionary<Point3d, List<Line>> ptDict, List<Line> verticalPipe, bool isCool)
        {
            var ptCoolDict = new Dictionary<Point3d, bool>();
            var tol = new Tolerance(1, 1);
            var removeList = new List<Point3d>();
            for (int i = 0; i < ptDict.Count; i++)
            {
                var item = ptDict.ElementAt(i);

                if (item.Value.Where(x => verticalPipe.Contains(x) == false).Any())
                {
                    ptCoolDict.Add(item.Key, isCool);
                }
                else
                {
                    //末端，只有立管
                    var ptOther = item.Value[0].EndPoint;
                    if (item.Key.IsEqualTo(ptOther, tol))
                    {
                        ptOther = item.Value[0].StartPoint;
                    }
                    var ptOtherEndKey = ThDrainageADTreeService.IsInDict(ptOther, ptDict);
                    if (ptOtherEndKey != Point3d.Origin)
                    {
                        if (ptDict[ptOtherEndKey].Count > 1)
                        {
                            //立管另一端有别的线   
                            ptCoolDict.Add(item.Key, isCool);
                        }
                    }
                }
                if (ptCoolDict.ContainsKey(item.Key) == false)
                {
                    removeList.Add(item.Key);
                }
            }

            removeList.ForEach(x => ptDict.Remove(x));

            return ptCoolDict;
        }
    }
}
