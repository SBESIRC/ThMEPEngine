using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class SpraySystem
    {
        public List<Point3dEx> MainLoop { get; set; }//主环路
        public List<List<Point3dEx>> MainLoops { get; set; }//无嵌套环路
        public List<List<Point3dEx>> SubLoops { get; set; }//次环路
        public List<List<Point3dEx>> BranchLoops { get; set; }//报警阀环路
        public Dictionary<Point3dEx, List<Point3dEx>> BranchDic { get; set; }//支路
        public Dictionary<Point3dEx, List<Point3dEx>> BranchThroughDic { get; set; }//穿越点支路
        public Dictionary<Point3dEx, bool> ValveDic { get; set; }//端点是否存在阀门
        public Dictionary<Point3dEx, List<int>> SubLoopAlarmsDic { get; set; }//支路的报警阀数目
        public Dictionary<Point3dEx, List<int>> SubLoopFireAreasDic { get; set; }//支路的防火分区数目
        public Dictionary<Point3dEx, int> SubLoopBranchDic { get; set; }//次环支路数目
        public Dictionary<Point3dEx, List<Point3dEx>> SubLoopBranchPtDic { get; set; }//次环上的支路点集
        public Dictionary<Point3dEx, Point3d> SubLoopPtDic { get; set; } //次环起始点
        public Dictionary<Point3dEx, Point3d> BranchLoopPtDic { get; set; } //支环起始点
        public Dictionary<Point3dEx, Point3d> BranchPtDic { get; set; } //支路起始点
        public Dictionary<Point3dEx, Point3d> FireAreaStPtDic { get; set; } //防火分区起始点
        public Point3d TempSubLoopStartPt { get; set; }//存放次环的起始绘制点
        public ThCADCoreNTSSpatialIndex BlockExtents { get; set; }//存放文字和模块的外包框

        public double MaxOffSetX { get; set; }//存放最远楼板线边界

        public SpraySystem()
        {
            MainLoop = new List<Point3dEx>();
            MainLoops = new List<List<Point3dEx>>();
            SubLoops = new List<List<Point3dEx>>();
            BranchLoops = new List<List<Point3dEx>>();
            BranchDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            BranchThroughDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            ValveDic = new Dictionary<Point3dEx, bool>();
            SubLoopAlarmsDic = new Dictionary<Point3dEx, List<int>>();
            SubLoopFireAreasDic = new Dictionary<Point3dEx, List<int>>();
            SubLoopBranchDic = new Dictionary<Point3dEx, int>();
            SubLoopBranchPtDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            SubLoopPtDic = new Dictionary<Point3dEx, Point3d>();
            BranchLoopPtDic = new Dictionary<Point3dEx, Point3d>();
            BranchPtDic = new Dictionary<Point3dEx, Point3d>();
            FireAreaStPtDic = new Dictionary<Point3dEx, Point3d>();
            TempSubLoopStartPt = new Point3d();
            BlockExtents = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
            MaxOffSetX = 20000;
        }
    }
}
