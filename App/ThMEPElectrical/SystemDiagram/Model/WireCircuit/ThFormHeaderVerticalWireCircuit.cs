﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    /// <summary>
    /// 系统图的左边框和表头
    /// </summary>
    public class ThFormHeaderVerticalWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            this.FloorIndex = AllFireDistrictData.Count;
            List<Entity> Result = new List<Entity>();
            //画三个标题
            {
                //the one
                Polyline OnePolyLine = new Polyline(4);
                OnePolyLine.Closed = true;
                OnePolyLine.AddVertexAt(0, new Point2d(-3000, OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                OnePolyLine.AddVertexAt(1, new Point2d(-3000, OuterFrameLength * FloorIndex), 0, 0, 0);
                OnePolyLine.AddVertexAt(2, new Point2d(0, OuterFrameLength * FloorIndex), 0, 0, 0);
                OnePolyLine.AddVertexAt(3, new Point2d(0, OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                Result.Add(OnePolyLine);
                DBText OneText = new DBText() { Height = 500, WidthFactor = 0.5, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                OneText.TextString = ThAutoFireAlarmSystemCommon.SystemDiagramChartHeader1;
                OneText.Position = new Point3d(-1500, OuterFrameLength * this.FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight / 2, 0);
                OneText.AlignmentPoint = OneText.Position;
                Result.Add(OneText);
                //the Two
                Polyline TwoPolyLine = new Polyline(4);
                TwoPolyLine.Closed = true;
                TwoPolyLine.AddVertexAt(0, new Point2d(0, OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                TwoPolyLine.AddVertexAt(1, new Point2d(0, OuterFrameLength * FloorIndex), 0, 0, 0);
                TwoPolyLine.AddVertexAt(2, new Point2d(OuterFrameLength * ThAutoFireAlarmSystemCommon.SystemColLeftNum, OuterFrameLength * FloorIndex), 0, 0, 0);
                TwoPolyLine.AddVertexAt(3, new Point2d(OuterFrameLength * ThAutoFireAlarmSystemCommon.SystemColLeftNum, OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                Result.Add(TwoPolyLine);
                DBText TwoText = new DBText() { Height = 500, WidthFactor = 0.5, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                TwoText.TextString = ThAutoFireAlarmSystemCommon.SystemDiagramChartHeader2;
                TwoText.Position = new Point3d(9000, OuterFrameLength * this.FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight / 2, 0);
                TwoText.AlignmentPoint = TwoText.Position;
                Result.Add(TwoText);
                //the Three
                Polyline ThreePolyLine = new Polyline(4);
                ThreePolyLine.Closed = true;
                ThreePolyLine.AddVertexAt(0, new Point2d(OuterFrameLength * ThAutoFireAlarmSystemCommon.SystemColLeftNum, OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                ThreePolyLine.AddVertexAt(1, new Point2d(OuterFrameLength * ThAutoFireAlarmSystemCommon.SystemColLeftNum, OuterFrameLength * FloorIndex), 0, 0, 0);
                ThreePolyLine.AddVertexAt(2, new Point2d(OuterFrameLength * (ThAutoFireAlarmSystemCommon.SystemColLeftNum + ThAutoFireAlarmSystemCommon.SystemColRightNum), OuterFrameLength * FloorIndex), 0, 0, 0);
                ThreePolyLine.AddVertexAt(3, new Point2d(OuterFrameLength * (ThAutoFireAlarmSystemCommon.SystemColLeftNum + ThAutoFireAlarmSystemCommon.SystemColRightNum), OuterFrameLength * FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight), 0, 0, 0);
                Result.Add(ThreePolyLine);
                DBText ThreeText = new DBText() { Height = 500, WidthFactor = 0.5, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                ThreeText.TextString = ThAutoFireAlarmSystemCommon.SystemDiagramChartHeader3;
                ThreeText.Position = new Point3d(OuterFrameLength * ThAutoFireAlarmSystemCommon.SystemColLeftNum + 10500, OuterFrameLength * this.FloorIndex + ThAutoFireAlarmSystemCommon.SystemDiagramChartHeight / 2, 0);
                ThreeText.AlignmentPoint = ThreeText.Position;
                Result.Add(ThreeText);
            }
            //设置线型
            Result.ForEach(o =>
            {
                if (o is DBText)
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                }
                if (o is Polyline)
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = ThAutoFireAlarmSystemCommon.OuterBorderBlockByLayer;
                    o.ColorIndex = ThAutoFireAlarmSystemCommon.OuterBorderBlockColorIndex;
                }
            });
            return Result;
        }

        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            Dictionary<int, List<Entity>> ResultDic = new Dictionary<int, List<Entity>>();
            //画楼层左侧的楼层名称
            for (int i = 0; i < this.FloorIndex; i++)
            {
                List<Entity> Result = new List<Entity>();
                var floorData = this.AllFireDistrictData[i];
                DBText Text = new DBText() { Height = 500, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                Text.TextString = floorData.FireDistrictName;
                Text.Position = new Point3d(-1500, OuterFrameLength * i + 1500, 0);
                Text.AlignmentPoint = Text.Position;
                Result.Add(Text);
                if (floorData.DrawCircuitName)
                {
                    DBText WireCircuitText = new DBText() { Height = 350, WidthFactor = 0.5, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                    WireCircuitText.TextString = floorData.WireCircuitName;
                    WireCircuitText.Position = new Point3d(13500, OuterFrameLength * i + 2200, 0);
                    WireCircuitText.AlignmentPoint = WireCircuitText.Position;
                    Result.Add(WireCircuitText);
                }
                //设置线型
                Result.ForEach(o =>
                {
                    o.Linetype = this.CircuitLinetype;
                    o.Layer = this.CircuitLayer;
                    o.ColorIndex = this.CircuitColorIndex;
                });
                ResultDic.Add(i + 1, Result);
            }
            return ResultDic;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-UNIV-NOTE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            //this.StartIndexBlock = 1;
            //this.EndIndexBlock = 21;
        }
    }
}
