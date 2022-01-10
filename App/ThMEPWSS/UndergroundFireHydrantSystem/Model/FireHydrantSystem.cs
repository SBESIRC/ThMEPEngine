﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class FireHydrantSystemOut
    {
        public Dictionary<Point3dEx, Point3dEx> BranchDrawDic { get; set; }
        public List<Line> LoopLine { get; set; }
        public List<Line> TextLine { get; set; }
        public List<DBText> TextList { get; set; }
        public Dictionary<Point3d, double> PipeInterrupted { get; set; }
        public List<Point3d> GateValve { get; set; }
        public List<Point3d> Valve { get; set; }
        public HashSet<Point3d> IsGateValve { get; set; }
        public List<Point3d> IsCasing { get; set; }
        public List<Point3d> FireHydrant { get; set; }
        public Point3d InsertPoint { get; set; }
        public List<DBText> DNList { get; set; }
        public Dictionary<Point3dEx, DBText> ExtraTextDic { get; set; }

        public FireHydrantSystemOut()
        {
            BranchDrawDic = new Dictionary<Point3dEx, Point3dEx>();
            LoopLine = new List<Line>();
            TextLine = new List<Line>();
            TextList = new List<DBText>();
            PipeInterrupted = new Dictionary<Point3d, double>();
            Valve = new List<Point3d>();
            IsGateValve = new HashSet<Point3d>();
            IsCasing = new List<Point3d>();
            GateValve = new List<Point3d>();
            FireHydrant = new List<Point3d>();
            DNList = new List<DBText>();
            ExtraTextDic = new Dictionary<Point3dEx, DBText>();
        }

        public void Draw()
        {
            using ( var acadDatabase = AcadDatabase.Active())
            {
                WaterSuplyUtils.ImportNecessaryBlocks();//导入需要的模块
                foreach (var line in LoopLine)
                {
                    acadDatabase.CurrentSpace.Add(line);
                    line.Layer = "W-FRPT-HYDT-PIPE";
                    line.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach(var text in TextList)
                {
                    acadDatabase.CurrentSpace.Add(text);
                    text.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach(var line in TextLine)
                {
                    acadDatabase.CurrentSpace.Add(line);
                    line.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach(var pipeInt in PipeInterrupted.Keys)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-EQPM", 
                        "水管中断",
                        pipeInt, 
                        new Scale3d(-0.8, 0.8, 0.8), 
                        PipeInterrupted[pipeInt]);
                }

                foreach(var valve in Valve)
                {
                    string valveName = "蝶阀";
                    if (IsGateValve.Contains(valve))
                    {
                        valveName = "闸阀";
                    }
                    
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-EQPM", 
                        valveName,
                        valve, 
                        new Scale3d(1, 1, 1), 
                        0);
                }

                foreach(var casing in IsCasing)
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-NOTE", 
                        "套管系统",
                        casing, 
                        new Scale3d(1, 1, 1), 
                        0);
                    objID.SetDynBlockValue("可见性", "放水套管水平");
                }

                foreach (var fh in FireHydrant)
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-PIPE", 
                        "室内消火栓系统1", 
                        fh, 
                        new Scale3d(1, 1, 1), 
                        0);
                    objID.SetDynBlockValue("可见性", "单栓");
                }

                foreach (var text in DNList)
                {
                    acadDatabase.CurrentSpace.Add(text);
                    text.ColorIndex = (int)ColorIndex.BYLAYER;
                }
            }
        }
    }

    public class FireHydrantSystemIn
    {
        public double FloorHeight { get; set; }
        public List<List<Line>> MarkLineList { get; set; }
        public List<List<Point3dEx>> NodeList { get; set; }
        public Dictionary<Point3dEx, double> AngleList { get; set; }
        public Dictionary<Point3dEx, string> MarkList { get; set; }
        public Dictionary<Point3dEx, string> PtTypeDic { get; set; }
        public List<Point3dEx> VerticalPosition { get; set; }
        public Dictionary<Point3dEx, TermPoint> TermPointDic { get; set; }
        public Dictionary<Point3dEx, List<Point3dEx>> PtDic { get; set; }
        public Dictionary<Line, List<Line>> LeadLineDic { get; set; }
        public Dictionary<LineSegEx, string> PtDNDic { get; set; }
        public Dictionary<Point3dEx, string> SlashDic { get; set; }
        public double TextWidth { get; set; }
        public double PipeWidth { get; set; }
        public bool ValveIsBkReference { get; set; }
        public List<Point3d> GateValves { get; set; }
        public Dictionary<Point3dEx, string> TermDnDic { get; set; }
        public List<Point3dEx> StartEndPts { get; set; }
        public HashSet<Point3dEx> VerticalHasHydrant { get; set; }
        public HashSet<Point3dEx> TermPtDic { get; set; }
        public FireHydrantSystemIn(double floorHeight = 5000)
        {
            FloorHeight = floorHeight;
            MarkLineList = new List<List<Line>>();//环管标记所在直线
            NodeList = new List<List<Point3dEx>>();//次环节点
            AngleList = new Dictionary<Point3dEx, double>();//次环节点角度
            MarkList = new Dictionary<Point3dEx, string>();//次环节点名称
            PtTypeDic = new Dictionary<Point3dEx, string>();//当前点的类型字典对
            VerticalPosition = new List<Point3dEx>(); //消火栓端点
            TermPointDic = new Dictionary<Point3dEx, TermPoint>();//端点字典对
            PtDic = new Dictionary<Point3dEx, List<Point3dEx>>();//当前点和邻接点字典对
            LeadLineDic = new Dictionary<Line, List<Line>>();//引线和邻接线字典对
            PtDNDic = new Dictionary<LineSegEx, string>();//当前点的DN字典对
            SlashDic = new Dictionary<Point3dEx, string>();//斜点的DN字典对
            GateValves = new List<Point3d>(); //闸阀中点位置
            TermDnDic = new Dictionary<Point3dEx, string>();//端点的标注
            StartEndPts = new List<Point3dEx>();//环管的起始终结点
            VerticalHasHydrant = new HashSet<Point3dEx>();//立管有消火栓设备
            TermPtDic = new HashSet<Point3dEx>();//是立管的管道末端
        }
    }
}
