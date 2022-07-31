using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        private double GenerateLaneBetweenTwoBuilds(ref GenerateLaneParas paras)
        {
            double generate_lane_length = -1;
            if (BuildingBoxes.Count <= 1) return generate_lane_length;
            #region 生成判断函数
            for (int i = 0; i < BuildingBoxes.Count - 1; i++)
            {
                for (int j = i + 1; j < BuildingBoxes.Count; j++)
                {
                    //计算出生成车道线
                    var pcenter_i = BuildingBoxes[i].Centroid.Coordinate;
                    var pcenter_j = BuildingBoxes[j].Centroid.Coordinate;
                    var line_ij = new LineSegment(pcenter_i, pcenter_j);
                    var degree = Math.Abs(line_ij.Angle) / Math.PI * 180;
                    degree = Math.Min(degree, Math.Abs(90 - degree));
                    if (degree > 10) continue;
                    var lines = SplitLine(line_ij, BuildingBoxes).Where(e => !IsInAnyBoxes(e.MidPoint, BuildingBoxes));
                    line_ij = ChangeLineToBeOrthogonal(line_ij);
                    if (line_ij.Length < 1) continue;
                    if (BuildingBoxes.Count > 2)
                    {
                        bool quitcycle = false;
                        for (int k = 0; k < BuildingBoxes.Count; k++)
                        {
                            if (k != i && k != j)
                            {
                                var p = line_ij.MidPoint;
                                var lt = new LineSegment(p.Translation(Vector(line_ij).Normalize().GetPerpendicularVector() * MaxLength),
                                    p.Translation(-Vector(line_ij).Normalize().GetPerpendicularVector() * MaxLength));
                                var bf = lt.Buffer(line_ij.Length / 2);
                                if (bf.IntersectPoint(BuildingBoxes[k]).Count() > 0 || bf.Contains(BuildingBoxes[k].Envelope.Centroid.Coordinate))
                                {
                                    quitcycle = true;
                                    break;
                                }
                            }
                        }
                        if (quitcycle) continue;
                    }
                    if (lines.Count() == 0) continue;
                    var line = lines.First();
                    line = ChangeLineToBeOrthogonal(line);
                    if (line.Length < DisCarAndHalfLane) continue;

                    Coordinate ps = new Coordinate();
                    if (Math.Abs(line.P0.X - line.P1.X) > 1)
                    {
                        if (line.P0.X < line.P1.X) line = new LineSegment(line.P1, line.P0);
                        ps = line.P0.Translation(Vector(line).Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
                    }
                    else if (Math.Abs(line.P0.Y - line.P1.Y) > 1)
                    {
                        if (line.P0.Y < line.P1.Y) line = new LineSegment(line.P1, line.P0);
                        ps = line.P0.Translation(Vector(line).Normalize() * DisLaneWidth / 2);
                    }

                    var vec = Vector(line).GetPerpendicularVector().Normalize();
                    var gline = new LineSegment(ps.Translation(vec * MaxLength), ps.Translation(-vec * MaxLength));
                    var glines = SplitLine(gline, Boundary).Where(e => Boundary.Contains(e.MidPoint))
                        .Where(e => e.Length > 1)
                        .OrderBy(e => e.ClosestPoint(ps).Distance(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    glines = SplitLine(gline, CarBoxes)
                        .Where(e => e.Length > 1)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes))
                        .OrderBy(e => e.ClosestPoint(ps).Distance(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    if (ClosestPointInCurves(gline.MidPoint, IniLanes.Select(e => e.Line.ToLineString()).ToList()) < 1)
                        continue;
                    if (ClosestPointInCurves(gline.P0, IniLanes.Select(e => e.Line.ToLineString()).ToList()) > 1)
                        gline = new LineSegment(gline.P1, gline.P0);
                    if (gline.Length < LengthCanGAdjLaneConnectSingle) continue;
                    if (!IsConnectedToLane(gline)) continue;
                    double dis_connected_double = 0;
                    if (IsConnectedToLaneDouble(gline)) dis_connected_double = DisCarAndHalfLane;
                    bool quit = false;
                    foreach (var box in BuildingBoxes)
                    {
                        if (gline.IntersectPoint(box).Count() > 0)
                        {
                            quit = true;
                            break;
                        }
                    }
                    if (quit) continue;
                    //与障碍物相交处理
                    var gline_buffer = gline.Buffer(DisLaneWidth / 2);
                    gline_buffer = gline_buffer.Scale(ScareFactorForCollisionCheck);
                    var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(gline_buffer).Cast<Polygon>().ToList();
                    gline_buffer = gline.Buffer(DisLaneWidth / 2);
                    var points = new List<Coordinate>();
                    foreach (var cross in obscrossed)
                    {
                        points.AddRange(cross.Coordinates);
                        points.AddRange(cross.IntersectPoint(gline_buffer));
                    }
                    points = points.Where(p => gline_buffer.Contains(p)).Select(p => gline.ClosestPoint(p)).Where(p => p.Distance(gline.P0) > 1).ToList();
                    var gline_splits = SplitLine(gline, points);
                    if (gline_splits.Count > 0) gline = gline_splits[0];
                    if (gline.Length < DisCarAndHalfLane) continue;
                    //生成参数赋值
                    if (IsConnectedToLane(gline) || IsConnectedToLane(gline, false))
                    {
                        paras.LanesToAdd.Add(new Lane(gline, Vector(line).Normalize()));
                        paras.LanesToAdd.Add(new Lane(gline, -Vector(line).Normalize()));
                        paras.CarBoxesToAdd.Add(PolyFromLine(gline));
                        generate_lane_length = gline.Length;
                        if (generate_lane_length - dis_connected_double > 0)
                            generate_lane_length -= dis_connected_double;
                    }
                }
            }
            #endregion
            if (paras.LanesToAdd.Count > 0)
            {
                switch (LayoutMode)
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            if (generate_lane_length > 0 && !IsHorizontalLine(paras.LanesToAdd[0].Line))
                            {
                                generate_lane_length *= LayoutScareFactor_betweenBuilds;
                            }
                            break;
                        }
                    case 2:
                        {
                            if (generate_lane_length > 0 && !IsVerticalLine(paras.LanesToAdd[0].Line))
                            {
                                generate_lane_length *= LayoutScareFactor_betweenBuilds;
                            }
                            break;
                        }
                }
            }
            return generate_lane_length;
        }
    }
}
