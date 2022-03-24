using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    class ThDuctPortsShapeService
    {
        public static double GetCrossRotateAngle(Vector3d inVec)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            var judger = -Vector3d.YAxis;
            if (inVec.IsEqualTo(judger, tor))
                return 0;
            else if (inVec.IsEqualTo(-judger, tor))
                return Math.PI;
            else
            {
                var z = judger.CrossProduct(inVec).Z;
                if (z < 0)
                {
                    return inVec.GetAngleTo(judger);
                }
                else
                {
                    return Math.PI * 2 - inVec.GetAngleTo(judger);
                }
            }
        }
        public static double GetTeeRotateAngle(Vector3d inVec)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            var judger = -Vector3d.YAxis;
            if (inVec.IsEqualTo(judger, tor))
                return 0;
            else if (inVec.IsEqualTo(-judger, tor))
                return Math.PI;
            else
            {
                var z = judger.CrossProduct(inVec).Z;
                if (z < 0)
                {
                    return inVec.GetAngleTo(judger);
                }
                else
                {
                    return Math.PI * 2 - inVec.GetAngleTo(judger);
                }
            }
        }
        public static double GetElbowRotateAngle(Vector3d inVec)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            var judger = -Vector3d.YAxis;
            if (inVec.IsEqualTo(judger, tor))
                return 0;
            else if (inVec.IsEqualTo(-judger, tor))
                return Math.PI;
            else
            {
                var z = judger.CrossProduct(inVec).Z;
                if (z < 0)
                {
                    return inVec.GetAngleTo(judger);
                }
                else
                {
                    return Math.PI * 2 - inVec.GetAngleTo(judger);
                }
            }
        }
        public static TeeType GetTeeType(Line branch1, Line branch2)
        {
            var v1 = ThMEPHVACService.GetEdgeDirection(branch1);
            var v2 = ThMEPHVACService.GetEdgeDirection(branch2);
            if (ThMEPHVACService.IsVertical(v1, v2))
                return TeeType.BRANCH_VERTICAL_WITH_OTTER;
            else
                return TeeType.BRANCH_COLLINEAR_WITH_OTTER;
        }
        public static TeeType GetTeeType(Point3d cp, Point3d p1, Point3d p2)
        {
            var v1 = (p1 - cp).GetNormal();
            var v2 = (p2 - cp).GetNormal();
            if (ThMEPHVACService.IsVertical(v1, v2))
                return TeeType.BRANCH_VERTICAL_WITH_OTTER;
            else
                return TeeType.BRANCH_COLLINEAR_WITH_OTTER;
        }
        public static double GetElbowOpenAngle(Point3d centerP, Line l1, Line l2)
        {
            var tor = new Tolerance(1.5, 1.5);
            var otherP1 = ThMEPHVACService.GetOtherPoint(l1, centerP, tor);
            var otherP2 = ThMEPHVACService.GetOtherPoint(l2, centerP, tor);
            var dirVec1 = (otherP1 - centerP).GetNormal();
            var dirVec2 = (otherP2 - centerP).GetNormal();
            return dirVec1.GetAngleTo(dirVec2);
        }
        public static double GetElbowShrink(double openAngle, double width)
        {
            const double K = 0.7;
            const double PI = 3.15;//Math.PI + 0.01
            const double HALFPI = 1.5;//0.5 * Math.PI - 0.01 
            if (openAngle > 0 && openAngle <= HALFPI) //不处理锐角
                throw new NotImplementedException("不支持弯头带锐角！！！");
            else if (openAngle > PI)
                throw new NotImplementedException("不支持弯头大于平角！！！");
            else
                return Math.Abs(K * width * Math.Tan(0.5 * (Math.PI - openAngle))) + 50;
        }
        public static Dictionary<int, double> GetCrossShrink(Line curLine,
                                                             Tuple<double, string> curParam,
                                                             List<Line> otherLines,
                                                             List<Tuple<double, string>> otherParams)
        {
            var otherLine1 = otherLines[0];
            var otherLine2 = otherLines[1];
            var otherLine3 = otherLines[2];
            var param1 = otherParams[0];
            var param2 = otherParams[1];
            var param3 = otherParams[2];
            return GetCrossShrink(curLine, otherLine1, otherLine2, otherLine3, curParam, param1, param2, param3);
        }
        public static Dictionary<int, double> GetCrossShrink(Line curLine,
                                                             Line otherLine1,
                                                             Line otherLine2,
                                                             Line otherLine3,
                                                             Tuple<double, string> curParam,
                                                             Tuple<double, string> param1,
                                                             Tuple<double, string> param2,
                                                             Tuple<double, string> param3)
        {
            var curW = ThMEPHVACService.GetWidth(curParam.Item2);
            var w1 = ThMEPHVACService.GetWidth(param1.Item2);
            var w2 = ThMEPHVACService.GetWidth(param2.Item2);
            var w3 = ThMEPHVACService.GetWidth(param3.Item2);
            var curAirVolume = curParam.Item1;
            var airVolume1 = param1.Item1;
            var airVolume2 = param2.Item1;
            var airVolume3 = param3.Item1;
            if (curAirVolume >= airVolume1 && curAirVolume >= airVolume2 && curAirVolume >= airVolume3)
                return GetCrossShrink(curLine, otherLine1, otherLine2, otherLine3, curW, w1, w2, w3);// l是进风口
            if (airVolume1 > curAirVolume && airVolume1 > airVolume2 && airVolume1 > airVolume3)
                return GetCrossShrink(otherLine1, curLine, otherLine2, otherLine3, w1, curW, w2, w3);// otherLine1是进风口
            if (airVolume2 > curAirVolume && airVolume2 > airVolume1 && airVolume2 > airVolume3)
                return GetCrossShrink(otherLine2, curLine, otherLine1, otherLine3, w2, curW, w1, w3);// otherLine2是进风口
            if (airVolume3 > curAirVolume && airVolume3 > airVolume1 && airVolume3 > airVolume2)
                return GetCrossShrink(otherLine3, curLine, otherLine1, otherLine2, w3, curW, w1, w2);// otherLine3是进风口
            throw new NotImplementedException("不可能跑这, 四通风量一定有一个最大");
        }
        private static Dictionary<int, double> GetCrossShrink(Line inLine, Line branch1, Line branch2, Line collinearLine,
                                                              double inW, double branchW1, double branchW2, Vector3d inVec, Vector3d branch1Vec)
        {
            var dic = new Dictionary<int, double>();
            GetCrossShrink(inW, branchW1, branchW2, out double inShrink, out double oInnerShrink, out double oOutterShrink, out double oCollinearShrink);
            dic.Add(inLine.GetHashCode(), inShrink);
            dic.Add(collinearLine.GetHashCode(), oCollinearShrink);
            if (ThMEPHVACService.IsOutter(inVec, branch1Vec))
            {
                dic.Add(branch1.GetHashCode(), oOutterShrink);
                dic.Add(branch2.GetHashCode(), oInnerShrink);
            }
            else
            {
                dic.Add(branch1.GetHashCode(), oInnerShrink);
                dic.Add(branch2.GetHashCode(), oOutterShrink);
            }
            return dic;
        }
        private static Dictionary<int, double> GetCrossShrink(Line inLine, Line otherLine1, Line otherLine2, Line otherLine3, double inW, double w1, double w2, double w3)
        {
            // 根据Tee类型设置不同的管段缩的长度
            var directions = GetConnectorDirection(inLine, new DBObjectCollection() { otherLine1, otherLine2, otherLine3 });
            var inVec = directions[0];
            var vec1 = directions[1];
            var vec2 = directions[2];
            var vec3 = directions[3];
            if (ThMEPHVACService.IsCollinear(inVec, vec1))
            {
                return GetCrossShrink(inLine, otherLine2, otherLine3, otherLine1, inW, w2, w3, inVec, vec2);
            }
            if (ThMEPHVACService.IsCollinear(inVec, vec2))
            {
                return GetCrossShrink(inLine, otherLine1, otherLine3, otherLine2, inW, w1, w3, inVec, vec1);
            }
            if (ThMEPHVACService.IsCollinear(inVec, vec3))
            {
                return GetCrossShrink(inLine, otherLine1, otherLine2, otherLine3, inW, w1, w2, inVec, vec1);
            }
            throw new NotImplementedException("不可能跑这，四通必定有一个出口和入风口共线");
        }
        public static void GetCrossShrink(double inW, double innerW, double outterW,
                                          out double inShrink, out double oInnerShrink, out double oOutterShrink, out double oCollinearShrink)
        {
            double maxW = Math.Max(innerW, outterW);
            double minW = Math.Min(innerW, outterW);
            inShrink = maxW + 50;
            oCollinearShrink = maxW * 0.5 + 100;
            // ThDuctPortsFactory.cs Line:138
            oInnerShrink = (inW + minW) * 0.5 + 50;//和主路叉积z值<0共线的支路，用大的值
            oOutterShrink = (inW + maxW) * 0.5 + 50;//和主路叉积z值>0共线的支路，用小的值
        }
        public static Dictionary<int, double> GetTeeShrink(Line curLine, Line otherLine1, Line otherLine2, 
                                                           FanParam curParam, FanParam param1, FanParam param2)
        {
            var curPara = new Tuple<double, string>(curParam.airVolume, curParam.notRoomDuctSize);
            var para1 = new Tuple<double, string>(param1.airVolume, param1.notRoomDuctSize);
            var para2 = new Tuple<double, string>(param2.airVolume, param2.notRoomDuctSize);
            return GetTeeShrink(curLine, otherLine1, otherLine2, curPara, para1, para2);
        }

        public static Dictionary<int, double> GetTeeShrink(Line curLine,
                                                           Tuple<double, string> curParam,
                                                           List<Line> otherLines,
                                                           List<Tuple<double, string>> otherParams)
        {
            var otherLine1 = otherLines[0];
            var otherLine2 = otherLines[1];
            var param1 = otherParams[0];
            var param2 = otherParams[1];
            return GetTeeShrink(curLine, otherLine1, otherLine2, curParam, param1, param2);
        }
        public static Dictionary<int, double> GetTeeShrink(Line curLine, 
                                                           Line otherLine1, 
                                                           Line otherLine2, 
                                                           Tuple<double, string> curParam, 
                                                           Tuple<double, string> param1,
                                                           Tuple<double, string> param2)
        {
            var curW = ThMEPHVACService.GetWidth(curParam.Item2);
            var w1 = ThMEPHVACService.GetWidth(param1.Item2);
            var w2 = ThMEPHVACService.GetWidth(param2.Item2);
            var curAirVolume = curParam.Item1;
            var airVolume1 = param1.Item1;
            var airVolume2 = param2.Item1;
            // 保证主管段风量一定大于直管段风量
            // ThDuctPortsAnalysis.cs Line:364
            // ThSepereateFansDuct.cs Line:219
            if (curAirVolume >= airVolume1 && curAirVolume >= airVolume2)
                return GetTeeShrink(curLine, otherLine1, otherLine2, curW, w1, w2);// l是进风口
            if (airVolume1 >= curAirVolume && airVolume1 >= airVolume2)
                return GetTeeShrink(otherLine1, otherLine2, curLine, w1, w2, curW);// otherLine1是进风口
            if (airVolume2 >= curAirVolume && airVolume2 >= airVolume1)
                return GetTeeShrink(otherLine2, otherLine1, curLine, w2, w1, curW);// otherLine2是进风口
            throw new NotImplementedException("不可能跑这，三通风量一定有一个最大");
        }
        public static Dictionary<int, double> GetTeeShrink(Line inLine, Line otherLine, Line curLine, double inW, double otherW, double curW)
        {
            // 根据Tee类型设置不同的管段缩的长度
            double inShrink;
            double branchShrink;
            double otherShrink;
            var tor = new Tolerance(1.5, 1.5);
            var type = GetTeeType(otherLine, curLine);
            var centerP = ThMEPHVACService.FindSamePoint(inLine, curLine);
            var inOtherP = ThMEPHVACService.GetOtherPoint(inLine, centerP, tor);
            var inCurP = ThMEPHVACService.GetOtherPoint(curLine, centerP, tor);
            var inVec = (inOtherP - centerP).GetNormal();
            var curVec = (inCurP - centerP).GetNormal();
            var dic = new Dictionary<int, double>();
            if (type == TeeType.BRANCH_VERTICAL_WITH_OTTER)
            {
                if (ThMEPHVACService.IsCollinear(inVec, curVec))
                {
                    GetTeeShrink(inW, otherW, curW, type, out inShrink, out branchShrink, out otherShrink);
                    dic.Add(inLine.GetHashCode(), inShrink);
                    dic.Add(curLine.GetHashCode(), otherShrink);
                    dic.Add(otherLine.GetHashCode(), branchShrink);
                }
                else
                {
                    GetTeeShrink(inW, curW, otherW, type, out inShrink, out branchShrink, out otherShrink);
                    dic.Add(inLine.GetHashCode(), inShrink);
                    dic.Add(curLine.GetHashCode(), branchShrink);
                    dic.Add(otherLine.GetHashCode(), otherShrink);
                }
            }
            else
            {
                if (ThMEPHVACService.IsOutter(inVec, curVec))
                {
                    GetTeeShrink(inW, curW, otherW, type, out inShrink, out branchShrink, out otherShrink);
                    dic.Add(inLine.GetHashCode(), inShrink);
                    dic.Add(curLine.GetHashCode(), branchShrink);
                    dic.Add(otherLine.GetHashCode(), otherShrink);
                }
                else
                {
                    GetTeeShrink(inW, otherW, curW, type, out inShrink, out branchShrink, out otherShrink);
                    dic.Add(inLine.GetHashCode(), inShrink);
                    dic.Add(curLine.GetHashCode(), otherShrink);
                    dic.Add(otherLine.GetHashCode(), branchShrink);
                }
            }
            return dic;
        }
        public static void GetTeeShrink(double inW, double branchW, double otherW, TeeType type,
                                        out double inShrink, out double branchShrink, out double otherShrink)
        {
            if (type == TeeType.BRANCH_VERTICAL_WITH_OTTER)
            {
                inShrink = branchW + 50;
                otherShrink = branchW * 0.5 + 100;//和主路共线的支路
                branchShrink = (inW + branchW) * 0.5 + 50;//和主路垂直的支路
            }
            else
            {
                double maxBranch = Math.Max(branchW, otherW);
                inShrink = maxBranch + 50;
                otherShrink = (inW + otherW) * 0.5 + 50;//和主路叉积z值<0共线的支路
                branchShrink = (inW + branchW) * 0.5 + 50;//和主路叉积z值>0共线的支路
            }
        }
        private static List<Vector3d> GetConnectorDirection(Line inLine, DBObjectCollection otherLines)
        {
            // 连接件的每一个出口的指向(In 放在第一个)
            var tor = new Tolerance(1.5, 1.5);
            var directions = new List<Vector3d>();
            var testLine = otherLines[0] as Line;
            var centerP = ThMEPHVACService.FindSamePoint(inLine, testLine);
            var inOtherP = ThMEPHVACService.GetOtherPoint(inLine, centerP, tor);
            var inVec = (inOtherP - centerP).GetNormal();
            directions.Add(inVec);
            foreach (Line l in otherLines)
            {
                var otherP = ThMEPHVACService.GetOtherPoint(l, centerP, tor);
                var dirVec = (otherP - centerP).GetNormal();
                directions.Add(dirVec);
            }
            return directions;
        }
        public static double GetReducingLen(double big, double small)
        {
            double reducinglength = 0.5 * Math.Abs(big - small) / Math.Tan(20 * Math.PI / 180);
            var a = reducinglength < 200 ? 200 : reducinglength > 600 ? 600 : reducinglength;
            return ThMEPHVACService.RoundNum(a, 10);
        }
    }
}