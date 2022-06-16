﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
namespace ThMEPWSS.PressureDrainageSystem.Model
{
    public class PressureDrainageModelData
    {
        public double FloorLineSpace { get; set; }//楼层线间距
        public List<string> FloorListDatas { get; set; }//楼层表
        public List<List<Point3dCollection>> FloorAreaList { get; set; }//楼层域点集
        public Dictionary<string, PressureDrainageSystemDiagramStorey> FloorDict { get; set; }//楼层字典
        public List<List<int>> FloorNumList { get; set; }
        public List<Point3d> FloorLocPoints { get; set; }//楼层定位点
        public string InitialLayer { get; set; }
        public List<Polyline> WallLines { get; set; }
        public List<Polyline> Boundaries { get; set; }
        public PressureDrainageModelData()
        {
            FloorLineSpace = new double();
            FloorListDatas = new List<string>();
            FloorAreaList = new List<List<Point3dCollection>>();
            FloorDict = new Dictionary<string, PressureDrainageSystemDiagramStorey>();
        }
    }
    public class PressureDrainageSystemDiagramStorey//楼层类
    {
        public List<VerticalPipeClass> VerticalPipes;//立管
        public List<Horizontal> HorizontalPipe;//横管
        public List<SubmergedPumpClass> SubmergedPumps;//潜水泵
        public List<DrainWellClass> DrainWells;//排水井
        public List<Extents3d> Wrappipes;//套管
    }
    public class PressureDrainageGeoData
    {
        public List<Extents3d> Storeys;//楼层框
        public List<Line> LabelLines;//标注线引线
        public List<Horizontal> HorizontalPipes;
        public List<DBText> Labels;
        public List<Circle> VerticalPipes;
        public List<DrainWellClass> DrainWells;
        public List<Extents3d> WrappingPipes;//套管
        public List<SubmergedPumpClass> SubmergedPumps;
        public List<Point3d> StoryFrameBasePt;
        public string InitialLayer;
        public List<Polyline> WallPolyLines;
        public List<Polyline> ColumnsPolyLines;
        public List<WellInfo> Wells;
    }
    public class DrainWellClass
    {
        public Extents3d Extents;
        public string Label;
        public string WellTypeName;
    }
    public class LabelClass
    {
        public DBText DBText;
    }
    public class Horizontal
    {
        public Horizontal(Line line, bool isInitialLine = true)
        {
            Line = line;
            IsInitialLine = isInitialLine;
        }
        public Horizontal()
        {

        }
        public Line Line = new Line();
        public bool IsInitialLine = true;
    }

    public class WellInfo
    {
        public Point3d Location;
        public double Length;
        public double Width;
    }
    public class VerticalPipeClass
    {
        public Circle Circle;//立管图形
        public string Identifier;
        public string Label;
        public bool IsPumpVerticalPipe;
        public SubmergedPumpClass AppendedSubmergedPump;
        public bool IsNexttoDainWell;
        public DrainWellClass AppendedDrainWell;
        public int Id;
        public bool isUnitStart;
        public bool HasChildPipe = false;
        public List<string> SameTypeIdentifiers;
        public int Diameter = 0;//立管管径
        public double totalQ = 0;//总流量
        public int AppendusedpumpCount = 0;
        public bool IsInitialDrainWell = false;
        public bool IsAdditionPipe = false;
        public int IsBridgePipe = 0;//用于绘图，判断是否为连接立管
        public bool CanUsedToJudgeCrossLayer = true;
    }
    public class HorizontalLabelClass
    {
        public Line HorizontalLabelLine;
        public List<DBText> DBTexts;
    }
    public class SubmergedPumpClass
    {
        public Extents3d Extents;
        public string Serial = "";
        public string Visibility = "";
        public string Location = "普通车库";
        public double paraQ = 0; //流量
        public double paraH = 0;
        public double paraN = 0;
        public string Allocation = "";
        public double Depth=0;
        public int PumpCount = 1;
        public string Length = "x";
        public string Width = "x";
    }
    public class PipeLineSystemUnitClass
    {
        public PipeLineSystemUnitClass()
        {
            if(this.PipeLineUnits==null)
            {
                this.PipeLineUnits = new List<PipeLineUnit>();
            }
            if(this.CrossLayerConnectedArrs==null)
            {
                this.CrossLayerConnectedArrs = new List<int[,]>();
            }
        }
        public List<Point3d> FloorLocPoints { get; set; }
        public int LayerNumbers { get; set; }
        public List<PipeLineUnit> PipeLineUnits { get; set; }
        public List<int[,]> CrossLayerConnectedArrs { get; set; }
        public int DrainageMode = 0;
        public DrainWellClass DrainWell;
        public int DrainWellPipeIndex { get; set; }
        public List<int> verticalPipeId { get; set; }
        public List<Point3d> SameUnitsStartPt { get; set; }
        public string InitialLayer { get; set; }
    }
    public class PipeLineUnit
    {
        public PipeLineUnit()
        {
            if(this.VerticalPipes==null)
            {
                this.VerticalPipes = new List<VerticalPipeClass>();
            }
            if(this.HorizontalPipes==null)
            {
                this.HorizontalPipes = new List<Horizontal>();
            }
            if (this.HorizontalPipes == null)
            {
                this.OriginalHorizontalPipes = new List<Horizontal>();
            }
            if (this.WrapPipes == null)
            {
                this.WrapPipes = new List<Polyline>();
            }
        }
        public List<VerticalPipeClass> VerticalPipes { get; set; }
        public List<Horizontal> HorizontalPipes { get; set; }
        public List<Horizontal> OriginalHorizontalPipes { get; set; }
        public List<Polyline> WrapPipes { get; set; }
        public int[,] VertPipeConnectedArr { get; set; }
        public int DrainWellPipeIndex { get; set; }
        public int DrainMode { get; set; }
        public enum UnitDrainMode : int
        {
            CROSSROOFAndINTOWELL = 1,//穿顶板进水井-丢弃
            CROSSROOF = 2,//穿顶板
            CROSSOUTDOOR = 3,//穿外墙
            CROSSINDOOR = 4//穿侧墙
        }
    }
}
