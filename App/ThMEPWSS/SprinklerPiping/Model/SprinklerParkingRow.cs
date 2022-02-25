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
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerPiping.Service;
using ThMEPWSS.DrainageSystemDiagram;
//using ThMEPWSS.Assistant;
using DrawUtils = ThMEPEngineCore.Diagnostics.DrawUtils;

namespace ThMEPWSS.SprinklerPiping.Model
{
    public class SprinklerParkingRow
    {
        public Polyline parkingRow;
        public Polyline inParkingRow;
        public Polyline interPoly;
        public bool isX; //1:X, 0:Y
        public bool isDoubleRow; //0:single, 1:double
        public List<SprinklerPoint> ptColumn;
        public bool againstWall = false; //1:靠墙
        public int wallDir = -1; //靠墙边 -1:不靠墙 0:0-1边靠墙 1:2-3边靠墙
        //public int ptCnt = 0;
        public List<Line> choices = new List<Line>();
        public List<HashSet<Point3d>> ptAssignedLists = new List<HashSet<Point3d>>();
        public List<int> randomWeight = new List<int>();
        public double width;
        public int[] vote = new int[] { 0, 0 }; // up down

        //TODO: 平行
        public SprinklerParkingRow(Polyline parkingRow, SprinklerPipingParameter parameter)
        {
            this.parkingRow = parkingRow;
            List<Point3d> pts = parameter.pts;
            double parkingLength = parameter.parkingLength;
            List<SprinklerPoint> sprinklerPoints = parameter.sprinklerPoints;
            double minSpace = parameter.minSpace;
            Polyline frame = parameter.frame;
            Dictionary<Point3d, SprinklerPoint> ptDic = parameter.ptDic;

            interPoly = ThCADCoreNTSEntityExtension.Intersection(parameter.frame, parkingRow, false).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            //interPoly.Closed = true;
            //int a = interPoly.NumberOfVertices;
            interPoly = SprinklerPipingAssist.NormalizeInterPoly(interPoly);
            //DrawUtils.ShowGeometry(interPoly, "l00interparkingrows");

            //for (int i = 0; i < interPoly.NumberOfVertices; i++)
            //{
            //    Point3d curPt = interPoly.GetPoint3dAt(i);
            //}



            //TODO:验证一下曲折相交时候
            List<List<Point3d>> totalIntersectPts = SprinklerPipingAssist.GetIntersectPts(parkingRow, frame);
            
            if (interPoly.NumberOfVertices == 4)
            {
                if (totalIntersectPts[0].Count != 0 && totalIntersectPts[2].Count != 0)
                {
                    //totalIntersectPts[0].Count == totalIntersectPts[2].Count 一定？
                    //纵切
                    List<Point3d> intersectPts = new List<Point3d>(totalIntersectPts[0]);
                    Line oppositeLine = new Line(parkingRow.GetPoint3dAt(2), parkingRow.GetPoint3dAt(3));
                    if (totalIntersectPts[0].Count == 1)
                    {
                        intersectPts = totalIntersectPts[0];
                        if (frame.Contains(parkingRow.GetPoint3dAt(0)))
                        {
                            intersectPts.Insert(0, parkingRow.GetPoint3dAt(0));
                            intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[1], false));
                            intersectPts.Add(parkingRow.GetPoint3dAt(3));
                        }
                        else
                        {
                            intersectPts.Add(parkingRow.GetPoint3dAt(1));
                            intersectPts.Add(parkingRow.GetPoint3dAt(2));
                            intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[0], false));
                        }
                    }
                    else
                    {
                        // == 2 双线纵切
                        //TODO: 如果有一条线有弯折
                        if (parkingRow.GetPoint3dAt(0).DistanceTo(intersectPts[0]) > parkingRow.GetPoint3dAt(0).DistanceTo(intersectPts[1]))
                        {
                            Point3d temp = intersectPts[0];
                            intersectPts[0] = intersectPts[1];
                            intersectPts[1] = temp;
                        }
                        intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[1], false));
                        intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[0], false));
                    }
                    inParkingRow = FormatParkingRow(intersectPts);
                }
                else if (totalIntersectPts[0].Count == 2 && totalIntersectPts[2].Count == 0)
                {
                    List<Point3d> intersectPts = new List<Point3d>();
                    if (parkingRow.GetPoint3dAt(0).DistanceTo(totalIntersectPts[0][0]) > parkingRow.GetPoint3dAt(0).DistanceTo(totalIntersectPts[0][1]))
                    {
                        Point3d temp = totalIntersectPts[0][0];
                        totalIntersectPts[0][0] = totalIntersectPts[0][1];
                        totalIntersectPts[0][1] = temp;
                    }
                    Line oppoLine = new Line();
                    for(int i=0; i<interPoly.NumberOfVertices; i++)
                    {
                        int nexti = i < interPoly.NumberOfVertices - 1 ? i + 1 : 0;
                        Line curLine = new Line(interPoly.GetPoint3dAt(i), interPoly.GetPoint3dAt(nexti));
                        if (curLine.DistanceTo(totalIntersectPts[0][0], false) != 0 && curLine.DistanceTo(totalIntersectPts[0][1], false) != 0)
                            oppoLine = curLine;
                            break;
                    }
                    intersectPts.Add(totalIntersectPts[0][0]);
                    intersectPts.Add(totalIntersectPts[0][1]);
                    intersectPts.Add(oppoLine.GetClosestPointTo(totalIntersectPts[0][1], false));
                    intersectPts.Add(oppoLine.GetClosestPointTo(totalIntersectPts[0][0], false));
                    inParkingRow = FormatParkingRow(intersectPts);

                }
                else if(totalIntersectPts[0].Count == 0 && totalIntersectPts[2].Count == 2)
                {
                    List<Point3d> intersectPts = new List<Point3d>();
                    if (parkingRow.GetPoint3dAt(3).DistanceTo(totalIntersectPts[2][0]) > parkingRow.GetPoint3dAt(3).DistanceTo(totalIntersectPts[2][1]))
                    {
                        Point3d temp = totalIntersectPts[2][0];
                        totalIntersectPts[2][0] = totalIntersectPts[2][1];
                        totalIntersectPts[2][1] = temp;
                    }
                    Line oppoLine = new Line();
                    for (int i = 0; i < interPoly.NumberOfVertices; i++)
                    {
                        int nexti = i < interPoly.NumberOfVertices - 1 ? i + 1 : 0;
                        Line curLine = new Line(interPoly.GetPoint3dAt(i), interPoly.GetPoint3dAt(nexti));
                        if (curLine.DistanceTo(totalIntersectPts[2][0], false) != 0 && curLine.DistanceTo(totalIntersectPts[2][1], false) != 0)
                            oppoLine = curLine;
                            break;
                    }
                    intersectPts.Add(oppoLine.GetClosestPointTo(totalIntersectPts[2][0], false));
                    intersectPts.Add(oppoLine.GetClosestPointTo(totalIntersectPts[2][1], false));
                    intersectPts.Add(totalIntersectPts[2][1]);
                    intersectPts.Add(totalIntersectPts[2][0]);
                    inParkingRow = FormatParkingRow(intersectPts);
                }
                else if (totalIntersectPts[1].Count != 0 && totalIntersectPts[3].Count != 0)
                {
                    //横切
                    //againstWall = true;
                    List<Point3d> intersectPts = new List<Point3d>();
                    if (frame.Contains(parkingRow.GetPoint3dAt(0)))
                    {
                        intersectPts.Add(parkingRow.GetPoint3dAt(0));
                        intersectPts.Add(parkingRow.GetPoint3dAt(1));
                        intersectPts.Add(totalIntersectPts[1][0]);
                        intersectPts.Add(totalIntersectPts[3][0]);
                    }
                    else
                    {
                        intersectPts.Add(parkingRow.GetPoint3dAt(3));
                        intersectPts.Add(parkingRow.GetPoint3dAt(2));
                        intersectPts.Add(totalIntersectPts[1][0]);
                        intersectPts.Add(totalIntersectPts[3][0]);
                    }
                    inParkingRow = FormatParkingRow(intersectPts);
                }
                else if (totalIntersectPts[0].Count + totalIntersectPts[2].Count == 1 && totalIntersectPts[1].Count + totalIntersectPts[3].Count == 1)
                {
                    //切角
                    //TODO:留的不是角
                    List<Point3d> intersectPts = new List<Point3d>();
                    if (totalIntersectPts[0].Count == 1 && totalIntersectPts[1].Count == 1 && frame.Contains(parkingRow.GetPoint3dAt(1)))
                    {
                        intersectPts.Add(totalIntersectPts[0][0]);
                        intersectPts.Add(parkingRow.GetPoint3dAt(1));
                        intersectPts.Add(totalIntersectPts[1][0]);
                        intersectPts.Add(intersectPts[2]+(intersectPts[0] - intersectPts[1]));
                    }
                    else if (totalIntersectPts[2].Count == 1 && totalIntersectPts[1].Count == 1 && frame.Contains(parkingRow.GetPoint3dAt(2)))
                    {
                        intersectPts.Add(totalIntersectPts[1][0]);
                        intersectPts.Add(parkingRow.GetPoint3dAt(2));
                        intersectPts.Add(totalIntersectPts[2][0]);
                        intersectPts.Insert(0, intersectPts[0]+(intersectPts[2] - intersectPts[1]));
                    }
                    else if (totalIntersectPts[0].Count == 1 && totalIntersectPts[3].Count == 1 && frame.Contains(parkingRow.GetPoint3dAt(0)))
                    {
                        intersectPts.Add(parkingRow.GetPoint3dAt(0));
                        intersectPts.Add(totalIntersectPts[0][0]);
                        intersectPts.Add(totalIntersectPts[3][0]);
                        intersectPts.Insert(2, intersectPts[2]+(intersectPts[1] - intersectPts[0]));
                    }
                    else if (totalIntersectPts[2].Count == 1 && totalIntersectPts[3].Count == 1 && frame.Contains(parkingRow.GetPoint3dAt(3)))
                    {
                        intersectPts.Add(totalIntersectPts[3][0]);
                        intersectPts.Add(totalIntersectPts[2][0]);
                        intersectPts.Add(parkingRow.GetPoint3dAt(3));
                        intersectPts.Insert(1, intersectPts[0]+(intersectPts[1] - intersectPts[2]));
                    }
                    inParkingRow = FormatParkingRow(intersectPts);
                }
                else
                {
                    //完整
                    inParkingRow = FormatParkingRow(parkingRow);
                }
                //inParkingRow = intersectPts.Count == 0 ? FormatParkingRow(parkingRow) : FormatParkingRow(intersectPts);
            }
            else if (interPoly.NumberOfVertices == 6)
            {
                //if (interPoly.NumberOfVertices != 6)
                //    return;
                //曲折相交 六边形？/ 八边形
                //短的<2/3  以长的车位排当成单排车位处理 否则当双排
                //TODO：如果是变成双排要延长
                //or <2/3 vote长边 否则 去除短边的choice 
                
                List<Point3d> intersectPts = new List<Point3d>();
                double angle = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(1)).Angle;
                Line lane = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0)); //平行车道的最长边
                Line minlane = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0)); //平行车道的最短边
                Line vlane = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0)); //垂直车道的最长边
                Line minvlane = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0)); //垂直车道的最短边
                for (int i=0; i<interPoly.NumberOfVertices; i++)
                {
                    int nexti = i < interPoly.NumberOfVertices - 1 ? i + 1 : 0;
                    Line curLine = new Line(interPoly.GetPoint3dAt(i), interPoly.GetPoint3dAt(nexti));
                    double angledis = Math.Abs(curLine.Angle - angle) % Math.PI;
                    //TODO: 可能精度还会出问题
                    if (angledis < 0.2 || Math.Abs(angledis - Math.PI) < 0.2) 
                    {
                        lane = curLine.Length > lane.Length ? curLine : lane;
                        if(minlane.Length == 0)
                        {
                            minlane = lane;
                        }
                        else
                        {
                            minlane = curLine.Length < minlane.Length ? curLine : minlane;
                        }
                    }
                    else
                    {
                        vlane = curLine.Length > vlane.Length ? curLine : vlane;
                        if (minvlane.Length == 0)
                        {
                            minvlane = vlane;
                        }
                        else
                        {
                            minvlane = curLine.Length < minvlane.Length ? curLine : minvlane;
                        }
                    }
                }

                //TODO:可能需要debug（理论上一定相交 且交于端点）
                //if()
                List<Point3d> temp = lane.ExtendLine(1).Intersect(vlane.ExtendLine(1), 0);
                //DrawUtils.ShowGeometry(lane, "l00lane");
                //DrawUtils.ShowGeometry(vlane, "l00lane");
                if(temp.Count == 0)
                {
                    inParkingRow = FormatParkingRow(parkingRow);
                }
                else
                {
                    Point3d interPt = lane.ExtendLine(1).Intersect(vlane.ExtendLine(1), 0)[0];
                    if (lane.EndPoint.IsEqualTo(interPt))
                    {
                        lane = new Line(lane.EndPoint, lane.StartPoint);
                    }
                    if (vlane.EndPoint.IsEqualTo(interPt))
                    {
                        vlane = new Line(vlane.EndPoint, vlane.StartPoint);
                    }
                    intersectPts.Add(interPt);
                    intersectPts.Add(lane.EndPoint);
                    intersectPts.Add(lane.EndPoint + (vlane.EndPoint - vlane.StartPoint));
                    intersectPts.Add(vlane.EndPoint);

                    inParkingRow = FormatParkingRow(intersectPts);

                    if (lane.GetClosestPointTo(inParkingRow.GetPoint3dAt(0), false).IsEqualTo(inParkingRow.GetPoint3dAt(0), new Tolerance(1, 1))
                        && lane.GetClosestPointTo(inParkingRow.GetPoint3dAt(1), false).IsEqualTo(inParkingRow.GetPoint3dAt(1), new Tolerance(1, 1)))
                    {
                        //0-1是最长边（即最长边是车位排下边缘）
                        vote[0] = 1;
                    }
                    else
                    {
                        vote[1] = 1;
                    }
                }
            }
            else if (interPoly.NumberOfVertices == 8)
            {
                //八边形
                List<Point3d> intersectPts = new List<Point3d>();
                Line laneLine = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(1));
                Line oppositeLine = new Line(parkingRow.GetPoint3dAt(2), parkingRow.GetPoint3dAt(3));

                if (parkingRow.GetPoint3dAt(0).DistanceTo(totalIntersectPts[0][0]) > parkingRow.GetPoint3dAt(0).DistanceTo(totalIntersectPts[0][1]))
                {
                    Point3d temp = totalIntersectPts[0][0];
                    totalIntersectPts[0][0] = totalIntersectPts[0][1];
                    totalIntersectPts[0][1] = temp;
                }
                if (parkingRow.GetPoint3dAt(3).DistanceTo(totalIntersectPts[2][0]) > parkingRow.GetPoint3dAt(3).DistanceTo(totalIntersectPts[2][1]))
                {
                    Point3d temp = totalIntersectPts[2][0];
                    totalIntersectPts[2][0] = totalIntersectPts[2][1];
                    totalIntersectPts[2][1] = temp;
                }

                if (parkingRow.GetPoint3dAt(1).DistanceTo(totalIntersectPts[0][1]) >= parkingRow.GetPoint3dAt(2).DistanceTo(totalIntersectPts[2][1]))
                {
                    intersectPts.Add(laneLine.GetClosestPointTo(totalIntersectPts[2][1], false));
                    intersectPts.Add(totalIntersectPts[2][1]);
                }
                else
                {
                    intersectPts.Add(totalIntersectPts[0][1]);
                    intersectPts.Add(oppositeLine.GetClosestPointTo(totalIntersectPts[0][1], false));
                }

                if (parkingRow.GetPoint3dAt(0).DistanceTo(totalIntersectPts[0][0]) >= parkingRow.GetPoint3dAt(3).DistanceTo(totalIntersectPts[2][0]))
                {
                    intersectPts.Insert(0, laneLine.GetClosestPointTo(totalIntersectPts[2][0], false));
                    intersectPts.Add(totalIntersectPts[2][0]);
                }
                else
                {
                    intersectPts.Insert(0, totalIntersectPts[0][0]);
                    intersectPts.Add(oppositeLine.GetClosestPointTo(totalIntersectPts[0][0], false));
                }

                inParkingRow = FormatParkingRow(intersectPts);
            }
            else
            {
                DrawUtils.ShowGeometry(interPoly, "l00interPoly");
                return;
            }

            //DrawUtils.ShowGeometry(inParkingRow, "l00inparkingrows");


            //List<Point3d> vintersectPts = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(3)).Intersect(frame, 0);
            ////纵切
            //List<Point3d> intersectPts = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(1)).Intersect(frame, 0);
            //if (intersectPts.Count != 0){
            //    //List<Point3d> intersectPts = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(1)).Intersect(frame, 0);
            //    //List<Point3d> intersectPts23 = new Line(parkingRow.GetPoint3dAt(2), parkingRow.GetPoint3dAt(3)).Intersect(frame, 0);
            //    if(intersectPts.Count == 2 && parkingRow.GetPoint3dAt(0).DistanceTo(intersectPts[0]) > parkingRow.GetPoint3dAt(0).DistanceTo(intersectPts[1]))
            //    {
            //        Point3d temp = intersectPts[0];
            //        intersectPts[0] = intersectPts[1];
            //        intersectPts[1] = temp;
            //    }
            //    Line oppositeLine = new Line(parkingRow.GetPoint3dAt(2), parkingRow.GetPoint3dAt(3));
            //    if(intersectPts.Count == 1)
            //    {
            //        if (frame.Contains(parkingRow.GetPoint3dAt(0)))
            //        {
            //            intersectPts.Insert(0, parkingRow.GetPoint3dAt(0));
            //            intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[1], false));
            //            intersectPts.Add(parkingRow.GetPoint3dAt(3));
            //        }
            //        else
            //        {
            //            intersectPts.Add(parkingRow.GetPoint3dAt(1));
            //            intersectPts.Add(parkingRow.GetPoint3dAt(2));
            //            intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[0], false));
            //        }
            //    }
            //    else
            //    {
            //        intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[1], false));
            //        intersectPts.Add(oppositeLine.GetClosestPointTo(intersectPts[0], false));
            //    }
            //    //0-3 down-up
            //    inParkingRow = FormatParkingRow(intersectPts);
            //}
            //else
            //{
            //    inParkingRow = FormatParkingRow(parkingRow);
            //}

            Line verticalLine = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3));

            //using(AcadDatabase acadDatabase = AcadDatabase.Active())
            //{
            //    acadDatabase.ModelSpace.Add(verticalLine);
            //}
            width = verticalLine.Length;
            if (width > parkingLength) 
                isDoubleRow = true;

            var objs = new DBObjectCollection();
            pts.ForEach(x => objs.Add(new DBPoint(x)));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var parkingPts = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(inParkingRow).Cast<DBPoint>().Select(x => x.Position).ToList();

            //ptCnt = 1;
            ptColumn = new List<SprinklerPoint>(); //left->right or down->up
            int cnt = 0;

            //double k = Math.Round(verticalLine.Angle % Math.PI / Math.PI * 180, 2);
            //bool a = Math.Round(verticalLine.Angle % Math.PI / Math.PI * 180, 2) >= 90;
            //double kk = verticalLine.Angle;
            if (Math.Round(verticalLine.Angle % Math.PI / Math.PI * 180, 2) >= 90)
            {
                isX = true;
                //ptColumn down->up
                //foreach(var pt in parkingPts)
                //{
                //    if (ptDic.ContainsKey(pt))
                //    {
                //        SprinklerPoint aa = ptDic[pt];
                //        DrawUtils.ShowGeometry(aa.pos, String.Format("l00pts-{0}-{1}-{2}", aa.groupIdx, aa.graphIdx, aa.nodeIdx));
                //    }
                //}
                foreach (var pt in parkingPts)
                {
                    List<SprinklerPoint> curPtColumn = new List<SprinklerPoint>();
                    //SprinklerPoint sprinklerPt = sprinklerPoints.Find(x => x.pos.IsEqualTo(parkingPts[0], new Tolerance(1, 1)));
                    SprinklerPoint sprinklerPt;
                    if (ptDic.ContainsKey(pt))
                    {
                        sprinklerPt = ptDic[pt];
                    }
                    else
                        continue;

                    if (!frame.Contains(pt)||(sprinklerPt.upNeighbor == null && sprinklerPt.downNeighbor == null)) 
                        continue;
                    SprinklerPoint curPt = new SprinklerPoint(sprinklerPt);
                    //List<SprinklerPoint> aa = parameter.sprinklerPoints;
                    //DrawUtils.ShowGeometry(curPt.pos, "l00pts");
                    curPtColumn.Add(curPt);
                    while (curPt.upNeighbor != null && inParkingRow.Contains(curPt.upNeighbor.pos))
                    {
                        //ptCnt++;
                        curPt = curPt.upNeighbor;
                        curPtColumn.Add(curPt);
                    }
                    curPt = new SprinklerPoint(sprinklerPt);
                    while (curPt.downNeighbor != null && inParkingRow.Contains(curPt.downNeighbor.pos))
                    {
                        //ptCnt++;
                        curPt = curPt.downNeighbor;
                        curPtColumn.Insert(0, curPt);
                    }

                    //if(curPtColumn.Count > ptColumn.Count)
                    if(columnCnt(curPtColumn) > columnCnt(ptColumn))
                    {
                        ptColumn = curPtColumn;
                    }
                    if (ptColumn.Count > 0 && cnt++ > 10) break;
                    //break;
                }

                //靠墙
                if (isAgainstWall(frame, inParkingRow.GetPoint3dAt(2), inParkingRow.GetPoint3dAt(3)))
                {
                    againstWall = true;
                    wallDir = 1;
                    vote[1] = 1;
                }
                if (isAgainstWall(frame, inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(1)))
                {
                    againstWall = true;
                    wallDir = 0;
                    vote[0] = 1;
                }
            }
            else
            {
                isX = false;

                //left->right
                foreach (var pt in parkingPts)
                {
                    List<SprinklerPoint> curPtColumn = new List<SprinklerPoint>();
                    //SprinklerPoint sprinklerPt = sprinklerPoints.Find(x => x.pos.IsEqualTo(pt, new Tolerance(1, 1)));
                    SprinklerPoint sprinklerPt;
                    if (ptDic.ContainsKey(pt))
                    {
                        sprinklerPt = ptDic[pt];
                    }
                    else
                        continue;

                    if (!frame.Contains(pt) || sprinklerPt.rightNeighbor == null && sprinklerPt.leftNeighbor == null) 
                        continue;
                    SprinklerPoint curPt = new SprinklerPoint(sprinklerPt);
                    curPtColumn.Add(curPt);
                    while (curPt.rightNeighbor != null && inParkingRow.Contains(curPt.rightNeighbor.pos))
                    {
                        //ptCnt++;
                        curPt = curPt.rightNeighbor;
                        curPtColumn.Add(curPt);
                    }
                    curPt = new SprinklerPoint(sprinklerPt);
                    while (curPt.leftNeighbor != null && inParkingRow.Contains(curPt.leftNeighbor.pos))
                    {
                        //ptCnt++;
                        curPt = curPt.leftNeighbor;
                        curPtColumn.Insert(0, curPt);
                    }
                    //break;
                    //if (curPtColumn.Count > ptColumn.Count)
                    if (columnCnt(curPtColumn) > columnCnt(ptColumn))
                    {
                        ptColumn = curPtColumn;
                    }
                    if (ptColumn.Count > 0 && cnt++ > 10) break;
                }

                if (isAgainstWall(frame, inParkingRow.GetPoint3dAt(2), inParkingRow.GetPoint3dAt(3)))
                {
                    againstWall = true;
                    wallDir = 1;
                    vote[1] = 1;
                }
                if (isAgainstWall(frame, inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(1)))
                {
                    againstWall = true;
                    wallDir = 0;
                    vote[0] = 1;
                }
            }
            //ptCnt = ptColumn.Count;
            getChoices(parameter);
            //if(ptColumn.Count > 0)
            //{

            //    Line l = new Line(ptColumn[0].pos, ptColumn[ptColumn.Count - 1].pos);

            //    DrawUtils.ShowGeometry(l, "l00new-pipes", lineWeightNum: 100);
            //}
        }

        public void getChoices(SprinklerPipingParameter parameter)
        {
            //get choices TODO:有障碍物，不选中线
            //TODO: 保证在frame内（截断）
            //ptColumn
            double minSpace = parameter.minSpace;
            
            List<Point3d> choiceColumn = new List<Point3d>();
            int lowerBound = 0;
            int upperBound = ptColumn.Count - 1;
            int cntFlagl = 0;
            int cntFlagu = 0;
            //for (int i = 0; i < ptCnt-1; i++)
            //{
            //    DrawUtils.ShowGeometry(new Line(ptColumn[i].pos, ptColumn[i+1].pos), "l00ptcolumn", lineWeightNum: 100);
            //}
            if (againstWall)
            {
                if (inParkingRow.GetPoint3dAt(3).Y >= inParkingRow.GetPoint3dAt(0).Y ^ wallDir == 0)
                    upperBound = (ptColumn.Count - 1) / 2;
                else
                    lowerBound = ptColumn.Count / 2;
            }

            for(int i=lowerBound; i<=upperBound; i++)
            {
                choiceColumn.Add(ptColumn[i].pos);
            }
            if(lowerBound == 0)
            {
                Point3d ptStart = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(1)).GetClosestPointTo(ptColumn[0].pos, false);
                choiceColumn.Insert(0, ptStart);
                cntFlagl = 1;
            }
            if(upperBound == ptColumn.Count - 1)
            {
                Point3d ptEnd = new Line(inParkingRow.GetPoint3dAt(2), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptColumn[ptColumn.Count - 1].pos, false);
                choiceColumn.Add(ptEnd);
                cntFlagu = 1;
            }

            for (int i = 0; i < choiceColumn.Count-1; i++)
            {
                if (choiceColumn[i].DistanceTo(choiceColumn[i + 1]) < minSpace * 2)
                    continue;
                Point3d ptCenter = new Point3d((choiceColumn[i].X + choiceColumn[i + 1].X) / 2, (choiceColumn[i].Y + choiceColumn[i + 1].Y) / 2, 0);
                int cntThreshold = (cntFlagl == 1 && i == 0) || (cntFlagu == 1 && i == choiceColumn.Count-1) ? 7 : 8;
                int idx = i - cntFlagl + lowerBound;
                if (CheckSprinklerNum(idx, idx < ptColumn.Count / 2 ? 1 : -1) > cntThreshold) //up:1 down:-1 
                    continue;
                Point3d startPt = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptCenter, false);
                Point3d endPt = new Line(inParkingRow.GetPoint3dAt(1), inParkingRow.GetPoint3dAt(2)).GetClosestPointTo(ptCenter, false);
                double dist = new Line(startPt, inParkingRow.GetPoint3dAt(0)).Length;
                int cnt = 0;
                if (isDoubleRow)
                {
                    while (((dist > width / 6 && dist <= width / 4) || (dist > width / 3 * 2 && dist <= width / 4 * 3)) && cnt < 2)
                    {
                        cnt++;
                        Point3d curCenter = new Point3d((choiceColumn[i].X + ptCenter.X) / 2, (choiceColumn[i].Y + ptCenter.Y) / 2, 0);
                        if (curCenter.DistanceTo(choiceColumn[i]) < minSpace) 
                            break;
                        ptCenter = curCenter;
                        startPt = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptCenter, false);
                        dist = new Line(startPt, inParkingRow.GetPoint3dAt(0)).Length;
                    }
                    while (((dist > width / 4 && dist <= width / 3 * 1) || (dist > width / 4 * 3 && dist <= width / 6 * 5)) && cnt < 2)
                    {
                        cnt++;
                        Point3d curCenter = new Point3d((choiceColumn[i + 1].X + ptCenter.X) / 2, (choiceColumn[i + 1].Y + ptCenter.Y) / 2, 0);
                        if (curCenter.DistanceTo(choiceColumn[i+1]) < minSpace)
                            break;
                        ptCenter = curCenter;
                        startPt = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptCenter, false);
                        dist = new Line(startPt, inParkingRow.GetPoint3dAt(0)).Length;
                    }
                    if ((dist > width / 6 && dist < width / 3 * 1) || (dist > width / 3 * 2 && dist < width / 6 * 5))
                        continue;
                }
                else
                {
                    while (dist > width / 3 && dist <= width / 2 && cnt < 2)
                    {
                        cnt++;
                        Point3d curCenter = new Point3d((choiceColumn[i].X + ptCenter.X) / 2, (choiceColumn[i].Y + ptCenter.Y) / 2, 0);
                        if (curCenter.DistanceTo(choiceColumn[i]) < minSpace)
                            break;
                        ptCenter = curCenter;
                        startPt = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptCenter, false);
                        dist = new Line(startPt, inParkingRow.GetPoint3dAt(0)).Length;
                    }
                    while (dist > width / 2 && dist < width / 3 * 2 && cnt < 2)
                    {
                        cnt++;
                        Point3d curCenter = new Point3d((choiceColumn[i + 1].X + ptCenter.X) / 2, (choiceColumn[i + 1].Y + ptCenter.Y) / 2, 0);
                        if (curCenter.DistanceTo(choiceColumn[i + 1]) < minSpace)
                            break;
                        ptCenter = curCenter;
                        startPt = new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(3)).GetClosestPointTo(ptCenter, false);
                        dist = new Line(startPt, inParkingRow.GetPoint3dAt(0)).Length;
                    }
                    if (dist > width / 3 && dist < width / 3 * 2)
                        continue;
                }
                if (cnt > 0)
                    endPt = new Line(inParkingRow.GetPoint3dAt(1), inParkingRow.GetPoint3dAt(2)).GetClosestPointTo(ptCenter, false);
                Line choiceLine = new Line(startPt, endPt);
                List<Point3d> interPts = choiceLine.Intersect(parameter.frame, 0);
                if (interPts.Count == 2)
                {
                    choiceLine = new Line(interPts[0], interPts[1]);
                }
                else if (interPts.Count == 1 && !interPts[0].IsEqualTo(choiceLine.EndPoint, new Tolerance(1, 1)) && !interPts[0].IsEqualTo(choiceLine.StartPoint, new Tolerance(1, 1)))
                {
                    if (parameter.frame.Contains(choiceLine.StartPoint))
                    {
                        choiceLine.EndPoint = interPts[0];
                    }
                    else
                    {
                        choiceLine.StartPoint = interPts[0];
                    }
                }
                if (choiceLine.Length < new Line(inParkingRow.GetPoint3dAt(0), inParkingRow.GetPoint3dAt(1)).Length / 3)
                    continue;

                //TODO: 交点数量应该没有别的情况了吧
                choices.Add(choiceLine);
                ptAssignedLists.Add(getAssignedIdx(idx));
            }
        } 

        public Line Select(int position, out HashSet<Point3d> ptAssignedList) //up:1 down:-1 arbitrary:0
        {
            randomWeight = new List<int>();
            int maxidx = choices.Count - 1;
            //TODO: 线间如果多于16个点位
            if (choices.Count == 1)
            {
                ptAssignedList = ptAssignedLists[0];
                return choices[0];
            }
            if (againstWall)
            {
                if (inParkingRow.GetPoint3dAt(3).Y >= inParkingRow.GetPoint3dAt(0).Y)
                {
                    for(int i=0; i<choices.Count; i++)
                        randomWeight.Add((int)Math.Pow(5, choices.Count - 1 - i));
                }
                else
                {
                    for (int i = 0; i < choices.Count; i++)
                        randomWeight.Add((int)Math.Pow(5, i));
                }
            }
            else
            {
                if(position == 0)
                {
                    for (int i = 0; i < choices.Count; i++)
                    {
                        randomWeight.Add((int)Math.Pow(5, (int)Math.Abs(maxidx / 2 - i)));
                    }
                }
                else
                {
                    for (int i = 0; i < choices.Count; i++)
                    {
                        int p = (int)(maxidx / 2.0 - position * maxidx / 2.0) + position * i;
                        randomWeight.Add((int)Math.Pow(5, p));
                    }
                    
                }
            }

            int totalWeight = randomWeight.Sum();
            int cursor = 0;
            Random rand = new Random();
            int randInt = rand.Next(totalWeight);
            for (int i = 0; i < randomWeight.Count; i++)
            {
                cursor += randomWeight[i];
                if(cursor > randInt)
                {
                    ptAssignedList = ptAssignedLists[i];
                    return choices[i];
                }
            }
            ptAssignedList = (randomWeight[0] > randomWeight[maxidx]) ? ptAssignedLists[0] : ptAssignedLists[maxidx];
            return (randomWeight[0] > randomWeight[maxidx]) ? choices[0] : choices[maxidx];
        }

        public HashSet<Point3d> getAssignedIdx(int idx)
        {

            //TODO
            HashSet<Point3d> ptList = new HashSet<Point3d>();
            SprinklerPoint curPt, p;
            int cnt;
            int idxFlag = 0;
            if (isX)
            {
                if(idx < 0)
                {
                    p = new SprinklerPoint(ptColumn[0]);
                    idxFlag = 1;
                }
                else
                {
                    p = new SprinklerPoint(ptColumn[idx]);
                }

                //right
                while (p != null && inParkingRow.Contains(p.pos))
                {
                    //up
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 9 - idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.upNeighbor;
                    }
                    //down
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 8 + idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.downNeighbor;
                    }
                    p = p.rightNeighbor;
                }
                //left
                //p = new SprinklerPoint(ptColumn[idx]);
                p = idx < 0 ? new SprinklerPoint(ptColumn[0]) : new SprinklerPoint(ptColumn[idx]);
                while (p != null && inParkingRow.Contains(p.pos))
                {
                    //up
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 9 - idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.upNeighbor;
                    }
                    //down
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 8 + idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.downNeighbor;
                    }
                    p = p.leftNeighbor;
                }
            }
            else
            {
                if (idx < 0)
                {
                    p = new SprinklerPoint(ptColumn[0]);
                    idxFlag = 1;
                }
                else
                {
                    p = new SprinklerPoint(ptColumn[idx]);
                }

                //up
                //p = new SprinklerPoint(ptColumn[idx]);
                while (p != null && inParkingRow.Contains(p.pos))
                {
                    //right
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 9 - idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.rightNeighbor;
                    }
                    //left
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 8 + idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.leftNeighbor;
                    }
                    p = p.upNeighbor;
                }
                //down
                //p = new SprinklerPoint(ptColumn[idx]);
                p = idx < 0 ? new SprinklerPoint(ptColumn[0]) : new SprinklerPoint(ptColumn[idx]);
                while (p != null && inParkingRow.Contains(p.pos))
                {
                    //right
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 9 - idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.rightNeighbor;
                    }
                    //left
                    curPt = new SprinklerPoint(p);
                    cnt = 0;
                    while (curPt != null && cnt < 8 + idxFlag)
                    {
                        ptList.Add(curPt.pos);
                        cnt++;
                        curPt = curPt.leftNeighbor;
                    }
                    p = p.downNeighbor;
                }
            }
            return ptList;
        }

        public int columnCnt(List<SprinklerPoint> column)
        {
            int num = column.Count;
            if(num == 0)
            {
                return num;
            }
            if (isX)
            {
                //down
                SprinklerPoint pt = column[0];
                while(pt.downNeighbor != null && pt.downNeighbor.scene == Scenes.Others)
                {
                    pt = pt.downNeighbor;
                    num++;
                }
                //up
                pt = column[column.Count-1];
                while (pt.upNeighbor != null && pt.upNeighbor.scene == Scenes.Others)
                {
                    pt = pt.upNeighbor;
                    num++;
                }
            }
            else
            {
                //left
                SprinklerPoint pt = column[0];
                while (pt.leftNeighbor != null && pt.leftNeighbor.scene == Scenes.Others)
                {
                    pt = pt.leftNeighbor;
                    num++;
                }
                //right
                pt = column[column.Count - 1];
                while (pt.rightNeighbor != null && pt.rightNeighbor.scene == Scenes.Others)
                {
                    pt = pt.rightNeighbor;
                    num++;
                }
            }
            return num;
        }

        public int CheckSprinklerNum(int idx, int dir) //up:1 down:-1
        {
            int num = 0;
            SprinklerPoint curPt;
            if (dir > 0)
            {
                num = ptColumn.Count - idx - 1;
                curPt = ptColumn[ptColumn.Count - 1];
            }
            else
            {
                num = idx + 1;
                curPt = ptColumn[0];
            }
            while(true)
            {
                if(isX && dir > 0)
                {
                    //up
                    curPt = curPt.upNeighbor;
                }
                else if(isX && dir < 0)
                {
                    //down
                    curPt = curPt.downNeighbor;
                }
                else if(!isX && dir > 0)
                {
                    //right
                    curPt = curPt.rightNeighbor;
                }
                else
                {
                    //left
                    curPt = curPt.leftNeighbor;
                }

                //车道
                if (curPt == null)
                    break;
                if (curPt.scene == Scenes.Parking)
                    //break;
                    return 8;
                //TODO：需要核对
                if (curPt.scene != Scenes.Others)
                    break;
                num++;
            }
            return num;
        }

        public Polyline FormatParkingRow(Polyline inrow)
        {
            List<Point3d> pts = new List<Point3d>();
            if(inrow.GetPoint3dAt(3).Y >= inrow.GetPoint3dAt(0).Y)
            {
                return inrow;
            }
            pts.Add(inrow.GetPoint3dAt(3));
            pts.Add(inrow.GetPoint3dAt(2));
            pts.Add(inrow.GetPoint3dAt(1));
            pts.Add(inrow.GetPoint3dAt(0));
            return SprinklerPipingAssist.MakePolyline(pts);
        }

        public Polyline FormatParkingRow(List<Point3d> inpts)
        {
            List<Point3d> pts = new List<Point3d>();
            if (inpts[3].Y >= inpts[0].Y && inpts[1].X >= inpts[0].X)
            {
                return SprinklerPipingAssist.MakePolyline(inpts);
            }
            else if (inpts[3].Y < inpts[0].Y && inpts[1].X >= inpts[0].X)
            {
                pts.Add(inpts[3]);
                pts.Add(inpts[2]);
                pts.Add(inpts[1]);
                pts.Add(inpts[0]);
            }
            else if (inpts[3].Y >= inpts[0].Y && inpts[1].X < inpts[0].X)
            {
                pts.Add(inpts[1]);
                pts.Add(inpts[0]);
                pts.Add(inpts[3]);
                pts.Add(inpts[2]);
            }
            else
            {
                pts.Add(inpts[2]);
                pts.Add(inpts[3]);
                pts.Add(inpts[0]);
                pts.Add(inpts[1]);
            }
               
            return SprinklerPipingAssist.MakePolyline(pts);
        }

        public bool isAgainstWall(Polyline frame, Point3d pt1, Point3d pt2)
        {
            Point3d centerPt = SprinklerPipingAssist.GetCenterPt(pt1, pt2);
            if (frame.GetClosestPointTo(pt1, false).DistanceTo(pt1) < 600 && frame.GetClosestPointTo(pt2, false).DistanceTo(pt2) < 600)
            {
                return frame.GetClosestPointTo(centerPt, false).DistanceTo(centerPt) < 600;
            }
            if (frame.GetClosestPointTo(centerPt, false).DistanceTo(centerPt) < 600)
            {
                return frame.GetClosestPointTo(pt1, false).DistanceTo(pt1) < 600 || frame.GetClosestPointTo(pt2, false).DistanceTo(pt2) < 600;
            }

            return false;
        }
    }
}
