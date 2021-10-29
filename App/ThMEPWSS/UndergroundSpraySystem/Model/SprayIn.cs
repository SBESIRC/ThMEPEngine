﻿using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.ViewModel;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class SprayIn
    {
        public double FloorLength { get; set; }//楼层线宽
        public double FloorHeight { get; set; }//楼层高
        public double PipeGap { get; set; }//管道通用间距
        public Point3dEx LoopStartPt { get; set; }//环管起点
        public Point3dEx LoopEndPt { get; set; }//环管终点
        public List<Line> PipeLines { get; set; }//管道
        public List<Point3dEx> Verticals { get; set; }//立管
        public List<Line> LeadLines { get; set; }//标注引线
        public List<DBText> PumpTexts { get; set; }
        public Dictionary<Point3dEx, List<Point3dEx>> PtDic { get; set; }//当前点及其邻接点
        public Dictionary<Point3dEx, string> PtTypeDic { get; set; }//当前点及其类型
        public Dictionary<Point3dEx, List<string>> PtTextDic { get; set; }//当前点及其标注
        public Dictionary<Line, List<Line>> LeadLineDic { get; set; }//引线及其邻接线
        public Dictionary<Point3dEx, int> TermPtTypeDic { get; set; }//端点类型
        public Dictionary<Point3dEx, TermPoint2> TermPtDic { get; set; }//端点及其属性
        public Dictionary<string, Polyline> FloorRectDic { get; set; }//楼板号及其框线
        public Dictionary<string, Point3d> FloorPtDic { get; set; }//楼板号及其标注点
        public List<Point3dEx> ThroughPt { get; set; }//穿越点
        public List<Point3dEx> CurThroughPt { get; set; }//当前层穿越点
        public Dictionary<Point3dEx, string> AlarmTextDic { get; set; }//报警阀文字
        public Dictionary<string, double> floorNumberYDic { get; set; }//楼层的Y

        public Dictionary<Point3dEx, string> TermDnDic { get; set; }//端点及其管径标注
        public Dictionary<Point3dEx, string> SlashDic { get; set; }//斜点的DN字典对
        public Dictionary<LineSegEx, string> PtDNDic { get; set; }//当前点的DN字典对

        public SprayIn(SprayVM _UiConfigs) 
        {
            FloorLength = 80000;
            PipeGap = 1900;
            FloorHeight = _UiConfigs.SetViewModel.FloorLineSpace;
            FloorRectDic = _UiConfigs.FloorRect;
            FloorPtDic = _UiConfigs.FloorPt;
            LoopStartPt = new Point3dEx();
            LoopEndPt = new Point3dEx();
            PipeLines = new List<Line>();
            Verticals = new List<Point3dEx>();
            PumpTexts = new List<DBText>();
            PtDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            PtTypeDic = new Dictionary<Point3dEx, string>();
            PtTextDic = new Dictionary<Point3dEx, List<string>>();
            LeadLineDic = new Dictionary<Line, List<Line>>();
            TermPtTypeDic = new Dictionary<Point3dEx, int>();
            TermPtDic = new Dictionary<Point3dEx, TermPoint2>();
            ThroughPt = new List<Point3dEx>();
            CurThroughPt = new List<Point3dEx>();
            AlarmTextDic = new Dictionary<Point3dEx, string>();
            floorNumberYDic = new Dictionary<string, double>();
            TermDnDic = new Dictionary<Point3dEx, string>();
            SlashDic = new Dictionary<Point3dEx, string>();
            PtDNDic = new Dictionary<LineSegEx, string>();
        }
    }
}
