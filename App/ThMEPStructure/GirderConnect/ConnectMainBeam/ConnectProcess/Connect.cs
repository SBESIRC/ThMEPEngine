using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess
{
    class Connect
    {
        /// <summary>
        /// Connect Steps
        /// </summary>
        /// <param name="clumnPts"></param>
        /// <param name="outlineWalls"></param>
        /// <returns></returns>
        public static HashSet<Tuple<Point3d, Point3d>> Calculate(Point3dCollection clumnPts, Dictionary<Polyline, List<Polyline>> outlineWalls, Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
        {
            //Steps:
            //1.1:Get near points of outlines
            Dictionary<Polyline, Point3dCollection> outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
            foreach (var pl in outlineWalls.Keys)
            {
                outlineNearPts.Add(pl, new Point3dCollection());
            }
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);

            //1.2:Get border points on/in outlines
            Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWalls, outlineClumns, outline2BorderNearPts);

            HashSet<Point3d> borderPts = new HashSet<Point3d>();
            Point3dCollection allPts = new Point3dCollection();
            foreach (Point3d clumnPt in clumnPts)
            {
                allPts.Add(clumnPt);
            }
            foreach (var pts in outlineClumns.Values)
            {
                foreach (Point3d pt in pts)
                {
                    allPts.Add(pt);
                    borderPts.Add(pt);
                }
            }

            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt in borderPt2NearPts.Keys)
                {
                    borderPts.Add(borderPt);
                    allPts.Add(borderPt);
                    //ShowInfo.ShowPointAsX(borderPt, 230, 300);
                }
            }

            //1.3:DT/VDconnect points with borderPoints and clumnPoints
            //HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.DelaunayTriangulationConnect(allPts);
            HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.VoronoiDiagramConnect(allPts);

            #region test
            /*
            //4:Delete borderPoints`s connects && add line of boredrPoint connect with nearPoint
            HashSet<Point3d> delPts = new HashSet<Point3d>();
            foreach (var pts in outlineNearPts.Values)
            {
                foreach (var pt in pts)
                {
                    if (pt is Point3d ptt && !delPts.Contains(ptt))
                    {
                        delPts.Add(ptt);
                    }
                }
            }
            foreach(var pt in borderPts)
            {
                if (!delPts.Contains(pt))
                {
                    delPts.Add(pt);
                }
            }

            HashSet<Point3d> nearPts = new HashSet<Point3d>();
            foreach (Point3dCollection pts in outlineNearPts.Values)
            {
                foreach (Point3d pt in pts)
                {
                    nearPts.Add(pt);
                }
            }
            */

            //disposed
            //删除和近点相连的内点，使只剩一根连线
            //StructureDealer.DeleteLineConnectToSingle(outlineNearPts, clumnPts, dicTuples);

            //LineDealer.DeleteSameClassLine(delPts, tuples); //注释掉是正确的
            //LineDealer.DeleteDiffClassLine(borderPts, clumnPts, tuples);//(borderPts, allPts, tuples)
            //LineDealer.DeleteSameClassLine(borderPts, tuples);
            //LineDealer.DeleteSameClassLine(nearPts, tuples); //注释掉是正确的
            //LineDealer.AddSpecialLine(borderPt2NearPts, tuples);
            #endregion

            Dictionary<Point3d, HashSet<Point3d>> dicTuples = LineDealer.TuplesStandardize(tuples, allPts);

            //1.4:find true border points with it`s near point of a outline
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var bordrPt in borderPt2NearPts.Keys)
                {
                    if (dicTuples.ContainsKey(bordrPt))
                    {
                        foreach (var nearPt in dicTuples[bordrPt])
                        {
                            if (!borderPt2NearPts[bordrPt].Contains(nearPt) && !borderPt2NearPts.ContainsKey(nearPt))
                            {
                                borderPt2NearPts[bordrPt].Add(nearPt);
                            }
                        }
                    }
                }
            }

            //2.0
            LineDealer.DeleteSameClassLine(borderPts, dicTuples);
            LineDealer.DeleteDiffClassLine(borderPts, clumnPts, dicTuples); //重要区分,删除与否要看情况
            LineDealer.AddSpecialLine(outline2BorderNearPts, dicTuples);
            //2.1、delete connect up to four
            StructureDealer.DeleteConnectUpToFour(dicTuples, outline2BorderNearPts);
            //2.2 close border
            Dictionary<Point3d, Point3d> closeBorderLines = StructureDealer.CloseBorder(outline2BorderNearPts);
            //2.3 show off
            foreach (var closeBorderLine in closeBorderLines)
            {
                ShowInfo.DrawLine(closeBorderLine.Key, closeBorderLine.Value, 90);
                ShowInfo.DrawLine(closeBorderLine.Value, closeBorderLine.Key, 90);
                //if (!dicTuples.ContainsKey(closeBorderLine.Key))
                //{
                //    dicTuples.Add(closeBorderLine.Key, new HashSet<Point3d>());
                //}
                //if (!dicTuples[closeBorderLine.Key].Contains(closeBorderLine.Value))
                //{
                //    dicTuples[closeBorderLine.Key].Add(closeBorderLine.Value);
                //}
            }
            foreach (var dicTuple in dicTuples)
            {
                foreach (Point3d pt in dicTuple.Value)
                {
                    ShowInfo.DrawLine(dicTuple.Key, pt, 130);
                }
            }
            foreach(var outline2BorderNearPt in outline2BorderNearPts)
            {
                foreach(var border2NearPts in outline2BorderNearPt.Value)
                {
                    foreach(var nearPt in border2NearPts.Value)
                    {
                        ShowInfo.DrawLine(border2NearPts.Key, nearPt, 210);
                        ShowInfo.DrawLine(nearPt, border2NearPts.Key, 210);
                    }
                }
            }

            //3.0
            Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLine = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, findPolylineFromLine);

            //3.1、add connect up tp four / splic polyline
            //StructureDealer.AddConnectUpToFour(dicTuples);
            //Split polyline && Merge polylines
            //postponed:May be "split polyline" will be replaced by "connrct nearPt to innerPt to utmost 4"
            //answer: can not batter than split to four

            return tuples;
        }
    }
}
