﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThFloorInfoExtractionService
    {
        public ThFloorInfo GetFloorInfo(ThFloorModel floor,int index)
        {
            //弃置文件
            var retInfo = new ThFloorInfo();
            var input = floor.FloorArea.Vertices();
            //提取横管
            var pipeExtractionService = new ThPipeExtractionService();
            retInfo.PipeLines = pipeExtractionService.GetPipeLines(input);
            //提取标记
            var markExtractionService = new ThMarkExtractionService();
            string startinfo = "";
            retInfo.MarkList = markExtractionService.GetMarkModelList(input, new Point3d(0, 0, 0), ref startinfo);
            //提取立管
            var riserExtractionService = new ThRiserExtracionService();
            retInfo.RiserList = riserExtractionService.GetRiserModelList(retInfo.PipeLines, input, index);
            //提取管径
            var dimExtractionService = new ThDimExtractionService();
            retInfo.DimList = dimExtractionService.GetDimModelList(input);
            //ToDo2:提取阀门
            var valveExtractionService = new ThOtherDataExtractionService();
            retInfo.ValveList = valveExtractionService.GetValveModelList(input);
            //ToDo1:提取皮带水嘴
            retInfo.FlushPointList = valveExtractionService.GetFlushPointList(input);
            return retInfo;
        }
        public void DrawText(string layer, string strText, Point3d position, double angle)
        {
            using (var database = AcadDatabase.Active())
            {
                var dbText = new DBText();
                dbText.Layer = layer;
                dbText.TextString = strText;
                dbText.Position = position;
                dbText.Rotation = angle;
                dbText.Height = 300.0;
                dbText.WidthFactor = 0.7;
                dbText.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                //dbText.HorizontalMode = TextHorizontalMode.TextCenter;
                //dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                //dbText.AlignmentPoint = position;
                database.ModelSpace.Add(dbText);
            }
        }
        public void DrawCircle(Point3d center, double radius, string layer, int colorIndex)
        {
            var circle = new Circle(center, new Vector3d(0.0, 0.0, 1.0), radius);
            circle.Layer = layer;
            circle.ColorIndex = colorIndex;
            Draw.AddToCurrentSpace(circle);
        }
    }
}
