﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHWall : ThIfcWall
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public double Length { get; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        public Vector3d ExtrudedDirection { get; private set; }
        /// <summary>
        /// 中线方向
        /// </summary>
        public Vector3d XVector { get; }
        /// <summary>
        /// 中线中点
        /// </summary>
        public Point3d Origin { get; }
        /// <summary>
        /// 门
        /// </summary>
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        public List<ThTCHOpening> Openings { get; private set; }
        public ThTCHWall(Point3d startPt,Point3d endPt,double width,double height) 
        {
            Init();
            Width = width;
            Height = height;
            Length = startPt.DistanceTo(endPt);
            XVector = (endPt - startPt).GetNormal();
            Origin = startPt + XVector.MultiplyBy(Length / 2);
        }
        public ThTCHWall(Polyline outPline, double height) 
        {
            Init();
            XVector = Vector3d.XAxis;
            Outline = outPline;
            Height = height;
        }
        void Init()
        {
            Doors = new List<ThTCHDoor>();
            Windows = new List<ThTCHWindow>();
            Openings = new List<ThTCHOpening>();
            ExtrudedDirection = Vector3d.ZAxis;
        }
    }
}
