using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHWall : ThIfcWall
    {
        public Point3d WallStartPoint { get; set; }
        public Point3d WallEndPoint { get; set; }
        public double WallWidth { get; set; }
        public double WallHeight { get; set; }
        public double WallLength { get; }
        public Vector3d ExtrudedDirection { get; private set; }
        public Vector3d XVector { get; }
        public Point3d IfcOrigin { get; }
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
            WallStartPoint = startPt;
            WallEndPoint = endPt;
            WallHeight = height;
            WallWidth = width;
            XVector = (endPt - startPt).GetNormal();
            WallLength = WallStartPoint.DistanceTo(WallEndPoint);
            IfcOrigin = startPt + XVector.MultiplyBy(WallLength / 2);
        }
        public ThTCHWall(Polyline outPline, double height) 
        {
            Init();
            Outline = outPline;
            WallHeight = height;
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
