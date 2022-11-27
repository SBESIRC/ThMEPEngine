using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPTCH.Model;
using ThMEPTCH.TCHDrawServices;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG;
using ThMEPWSS.DrainageSystemAG.Bussiness;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.DrainageSystemAG.Services;
using ThMEPWSS.Engine;
using ThMEPWSS.Model;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public static class TangentPipeConvertion
    {
        public static void ConvertElemToTCHPipes(List<CreateBlockInfo> pipeElems, List<CreateBasicElement> createBasicElems, List<CreateDBTextElement> createTextElems,
            List<string> notCreateLineIds, List<string> notCreateTextIds, ref List<ThTCHVerticalPipe> verPipes)
        {
            foreach (var item in pipeElems)
            {
                var tchPipe = new ThTCHVerticalPipe();
                tchPipe.PipeBottomPoint = item.createPoint;
                tchPipe.PipeTopPoint = item.createPoint + Vector3d.ZAxis.MultiplyBy(3000);
                tchPipe.PipeDN = Convert.ToDouble(item.dymBlockAttr.First().Value.ToString().Replace("DN", ""));
                string pipeSystem = "废水";
                string pipeMaterial = "排水铸铁管";
                string pipeDNType = "DN";
                switch (item.tag)
                {
                    case "DL":
                        break;
                    case "FL":
                        break;
                    case "Y2L":
                        pipeSystem = "雨水";
                        pipeMaterial = "排水铸铁管";
                        break;
                    case "FyL":
                        pipeSystem = "废水";
                        pipeMaterial = "排水铸铁管";
                        break;
                    case "FcL":
                        pipeSystem = "废水";
                        pipeMaterial = "排水铸铁管";
                        break;
                    case "TL":
                        pipeSystem = "通气";
                        pipeMaterial = "排水铸铁管";
                        break;
                    case "PL":
                        pipeSystem = "排水";
                        pipeMaterial = "镀锌钢管";
                        break;
                    case "Y1L":
                    case "NL":
                        pipeSystem = "雨水";
                        pipeMaterial = "排水铸铁管";
                        break;
                    case "WL":
                        pipeSystem = "污水";
                        pipeMaterial = "排水铸铁管";
                        break;
                }
                tchPipe.PipeSystem = pipeSystem;
                tchPipe.PipeMaterial = pipeMaterial;
                tchPipe.DnType = pipeDNType;
                switch (SetServicesModel.Instance.drawingScale)
                {
                    case EnumDrawingScale.DrawingScale1_100:
                        tchPipe.DocScale = 100.0;
                        break;
                    case EnumDrawingScale.DrawingScale1_150:
                        tchPipe.DocScale = 150.0;
                        break;
                    case EnumDrawingScale.DrawingScale1_50:
                        tchPipe.DocScale = 50;
                        break;
                }
                var bId = string.IsNullOrEmpty(item.copyId) ? item.uid : item.copyId;
                var lines = createBasicElems.Where(c => (c.belongBlockId.Contains(bId) || c.belongBlockId.Contains(item.uid)) && c.floorId == item.floorId).ToList();
                var texts = createTextElems.Where(c => c.belongBlockId.Contains(bId) && c.floorUid == item.floorId).ToList();
                if ((null != lines && lines.Count > 0) && (texts != null && texts.Count > 0))
                {
                    //计算标注
                    var pipeCenter = item.createPoint;
                    Line nearLine = null;
                    double nearDis = double.MaxValue;
                    foreach (var line in lines)
                    {
                        var thisLine = line.baseCurce as Line;
                        var lineSp = thisLine.StartPoint;
                        var lineEp = thisLine.EndPoint;
                        var spDis = lineSp.DistanceTo(pipeCenter);
                        var epDis = lineEp.DistanceTo(pipeCenter);
                        var thisDis = Math.Min(spDis, epDis);
                        if (thisDis < nearDis)
                        {
                            nearLine = thisLine;
                        }
                    }
                    Line otherLine = null;
                    var dir = nearLine.LineDirection();
                    var allPoints = new List<Point3d>();
                    foreach (var line in lines)
                    {
                        var thisLine = line.baseCurce as Line;
                        var thisDir = thisLine.LineDirection();
                        if (Math.Abs(thisDir.DotProduct(dir)) < 0.9)
                        {
                            otherLine = thisLine;
                            break;
                        }
                    }
                    if (otherLine == null)
                        continue;
                    notCreateLineIds.AddRange(lines.Select(c => c.uid).ToList());
                    notCreateTextIds.AddRange(texts.Select(c => c.uid).ToList());
                    var pt1 = otherLine.StartPoint;
                    var pt2 = otherLine.EndPoint;
                    if (pt1.DistanceTo(pipeCenter) < pt2.DistanceTo(pipeCenter))
                    {
                        tchPipe.TurnPoint = pt1;
                        tchPipe.TextDirection = otherLine.LineDirection();
                    }
                    else
                    {
                        tchPipe.TurnPoint = pt2;
                        tchPipe.TextDirection = otherLine.LineDirection().Negate();
                    }
                    var textFirst = texts.First();
                    var spliteStr = textFirst.dbText.TextString.Split('-').ToList();
                    var numStr = spliteStr.Last();
                    var floorNum = spliteStr.First();
                    floorNum = floorNum.Replace(item.tag, "");
                    int.TryParse(numStr, out int intNum);
                    tchPipe.FloorNum = floorNum;
                    tchPipe.TextStyle = "_TWT_SERIAL";
                    tchPipe.DimType = 0;
                    tchPipe.FloorType = 4;
                    tchPipe.TextHeight = 3.5;
                    tchPipe.DimRadius = 4.0;
                    tchPipe.Spacing = 1.0;
                    tchPipe.DimTypeText = item.tag;
                    tchPipe.PipeNum = intNum.ToString();
                }
                verPipes.Add(tchPipe);
            }
        }
    }
}
