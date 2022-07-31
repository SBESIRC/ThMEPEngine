using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class FireHydrantSystemOut
    {
        public Dictionary<Point3dEx, Point3dEx> BranchDrawDic { get; set; }
        public List<Line> LoopLine { get; set; }
        public List<Line> TextLine { get; set; }
        public List<Line> StoreyLine { get; set; }
        public List<DBText> TextList { get; set; }
        public Dictionary<Point3d, double> PipeInterrupted { get; set; }
        public List<Point3d> GateValve { get; set; }
        public List<Point3d> DieValve { get; set; }
        public List<Point3d> Casing { get; set; }
        public List<Point3d> FireHydrant { get; set; }
        public Point3d InsertPoint { get; set; }
        public List<DBText> DNList { get; set; }

        public HashSet<Point3d> VerticalHasReelHydrant { get; set; }
        public Dictionary<Point3dEx, DBText> ExtraTextDic { get; set; }
        public List<Line> AidLines { get; set; }

        public FireHydrantSystemOut()
        {
            BranchDrawDic = new Dictionary<Point3dEx, Point3dEx>();
            LoopLine = new List<Line>();
            TextLine = new List<Line>();
            StoreyLine = new List<Line>();
            TextList = new List<DBText>();
            PipeInterrupted = new Dictionary<Point3d, double>();
            DieValve = new List<Point3d>();
            GateValve = new List<Point3d>();
            Casing = new List<Point3d>();
            FireHydrant = new List<Point3d>();
            DNList = new List<DBText>();
            ExtraTextDic = new Dictionary<Point3dEx, DBText>();
            VerticalHasReelHydrant = new HashSet<Point3d>();
            AidLines = new List<Line>();
        }

        public void Draw(bool across)
        {
            var u2wMat = Active.Editor.UCS2WCS();
            using (var acadDatabase = AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-PIPE");

                WaterSuplyUtils.ImportNecessaryBlocks();//导入需要的模块
                foreach (var line in LoopLine)
                {
                    line.TransformBy(u2wMat);
                    acadDatabase.CurrentSpace.Add(line);
                    line.Layer = "W-FRPT-HYDT-PIPE";
                    line.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach (var text in TextList)
                {
                    text.TransformBy(u2wMat);
                    acadDatabase.CurrentSpace.Add(text);
                    text.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach (var line in TextLine)
                {
                    line.TransformBy(u2wMat);
                    acadDatabase.CurrentSpace.Add(line);
                    line.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach (var line in StoreyLine)
                {
                    line.TransformBy(u2wMat);
                    acadDatabase.CurrentSpace.Add(line);
                    line.LayerId = DbHelper.GetLayerId("W-NOTE");
                    line.ColorIndex = (int)ColorIndex.BYLAYER;
                }
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-EQPM");

                foreach (var pipeInt in PipeInterrupted.Keys)
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-EQPM",
                        "水管中断",
                        pipeInt,
                        new Scale3d(-0.8, 0.8, 0.8),
                        PipeInterrupted[pipeInt]);
                    var blk = acadDatabase.Element<BlockReference>(objID);
                    blk.TransformBy(u2wMat);
                }

                foreach (var valve in DieValve)
                {
                    string valveName = "蝶阀";
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-EQPM",valveName,valve,new Scale3d(1, 1, 1),0);
                    var blk = acadDatabase.Element<BlockReference>(objID);
                    blk.TransformBy(u2wMat);
                }
                foreach (var valve in GateValve)
                {
                    string valveName = "闸阀";
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-EQPM", valveName, valve, new Scale3d(1, 1, 1), 0);
                    var blk = acadDatabase.Element<BlockReference>(objID);
                    blk.TransformBy(u2wMat);
                }
                DbHelper.EnsureLayerOn("W-BUSH");

                foreach (var valve in Casing)
                {
                    string valveName = "套管系统";

                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-BUSH", valveName, valve, new Scale3d(1, 1, 1), 0);
                    var blk = acadDatabase.Element<BlockReference>(objID);
                    blk.TransformBy(u2wMat);
                }

                foreach (var fh in FireHydrant)
                {
                    
                    var HydrantWithReel = false;
                    if (VerticalHasReelHydrant.Contains(fh))
                    {
                        HydrantWithReel = true;
                    }
                    int scaleX = -2 * Convert.ToInt32(HydrantWithReel) + 1;
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        "W-FRPT-HYDT-PIPE",
                        "室内消火栓系统1",
                        fh,
                        new Scale3d(scaleX, 1, 1),
                        0);
                    if (HydrantWithReel)
                    {
                        objID.SetDynBlockValue("可见性", "单栓带卷盘");
                    }
                    else
                    {
                        objID.SetDynBlockValue("可见性", "单栓");
                    }
                    var blk = acadDatabase.Element<BlockReference>(objID);
                    blk.TransformBy(u2wMat);
                }

                foreach (var text in DNList)
                {
                    text.TransformBy(u2wMat);
                    acadDatabase.CurrentSpace.Add(text);
                    text.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                foreach(var line in AidLines)
                {
                    var newline = new Line(line.StartPoint.TransformBy(u2wMat), line.EndPoint);
                    acadDatabase.CurrentSpace.Add(newline);
                    newline.Layer = "W-辅助";
                    newline.ColorIndex = (int)ColorIndex.BYLAYER;
                }
            }
        }
    }

    public class FireHydrantSystemIn
    {
        public bool HasStoreyRect { get; set; }
        public Dictionary<string, Polyline> FloorRect = new();//楼层区域
        public Dictionary<string, Point3d> FloorPt = new();//楼层标准点 

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
        public List<Point3d> GateValves { get; set; }
        public Dictionary<Point3dEx, string> TermDnDic { get; set; }
        public List<Point3dEx> StartEndPts { get; set; }
        public HashSet<Point3dEx> VerticalHasHydrant { get; set; }
        public HashSet<Point3dEx> VerticalHasReelHydrant { get; set; }
        public HashSet<Point3dEx> TermPtDic { get; set; }
        public List<Point3dEx> ThroughPt { get; set; }

        public HashSet<BlockReference> HydrantWithReel { get; set; }

        public Dictionary<Point3dEx, Point3d> CrossMainPtDic { get; set; }//跨层主环的对应点位置
        public FireHydrantSystemIn(double floorHeight = 5000, StoreyRect storeyRect = null)
        {
            if(storeyRect is not null)
            {
                HasStoreyRect = storeyRect.HasStoreyRect;
                FloorRect = storeyRect.FloorRect;
                FloorPt = storeyRect.FloorPt;
            }

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
            VerticalHasReelHydrant = new HashSet<Point3dEx>(); //立管有带卷盘消火栓设备
            TermPtDic = new HashSet<Point3dEx>();//是立管的管道末端
            ThroughPt = new List<Point3dEx>();
            CrossMainPtDic = new Dictionary<Point3dEx, Point3d>();
            HydrantWithReel = new HashSet<BlockReference>();
        }
    }
}
