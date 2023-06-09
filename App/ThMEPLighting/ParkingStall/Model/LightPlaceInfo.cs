﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public enum Light_Place_Type
    {
        LONG_EDGE,
        SHORT_EDGE,
    }

    public enum ParkingSpace_Type
    {
        Invalid,
        Parallel_Parking,
        Reverse_stall_Parking,
    }

    // 长短边用于判断侧方停车或者是否有效
    public class BigGroupInformation
    {
        public Polyline BigGroupPoly;
        public Line BigGroupLongLine;
        public Line BigGroupShortLine;

        public BigGroupInformation(Polyline groupPoly, Line groupLongLine, Line groupShortLine)
        {
            BigGroupPoly = groupPoly;
            BigGroupLongLine = groupLongLine;
            BigGroupShortLine = groupShortLine;
        }
    }

    public class LightBlockConnectPointInfo
    {
        public Point3d MidPoint;
        public Point3d FirstEndPoint;
        public Point3d SecondEndPoint;

        public LightBlockConnectPointInfo(Point3d midPoint, Point3d firstEndPoint, Point3d secondEndPoint)
        {
            MidPoint = midPoint;
            FirstEndPoint = firstEndPoint;
            SecondEndPoint = secondEndPoint;
        }
    }

    public class LightPlaceInfo
    {
        public Point3d Position;
        public Point3d PtOnLanePoly;

        public double LengthFromStart = 0;

        public ObjectId InsertBlockId = ObjectId.Null;
        public List<Point3d> InsertLightPosisions = new List<Point3d>();
        public List<ObjectId> InsertLightBlockIds = new List<ObjectId>();
        public LightBlockConnectPointInfo LightBlockConnectInfo = null;

        public Line LongDirLength;
        public Line ShortDirLength;

        public double Angle;

        public BigGroupInformation BigGroupInfo; // 每组的大轮廓

        public bool IsUsed = false;

        public List<LightPlaceOneSide> lightsLst = new List<LightPlaceOneSide>();

        public ParkingSpace_Type ParkingSpace_TypeInfo = ParkingSpace_Type.Invalid;

        // 用于细分组的传导相关组计算
        public List<Line> SmallProfileLongLines; // 小轮廓的边界的长边集
        public List<Line> SmallProfileShortLines; // 小轮廓的边界短边集

        // 子分组灯点位修正
        public List<LightPlaceInfo> RelatedLightPlaceInfos = new List<LightPlaceInfo>();

        public LightPlaceInfo(Point3d position, Line longDirLength, Line shortDirLength, List<Line> smallProfileLongLines, List<Line> smallProfileShortLines)
        {
            Position = position;
            LongDirLength = longDirLength;
            ShortDirLength = shortDirLength;

            SmallProfileLongLines = smallProfileLongLines;
            SmallProfileShortLines = smallProfileShortLines;
        }
    }
}
