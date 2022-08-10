using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Service;
using ThMEPWSS.WaterSupplyPipeSystem.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPEngineCore;
using System.Linq;

namespace ThMEPWSS.WaterSupplyPipeSystem.Data
{
    public class SysOut
    {
        public List<Line> FloorLines { get; set; }//楼板线
        public List<Line> PipeLines { get; set; }//管线
        public List<Line> TextLines { get; set; }//文字引线
        public List<DBText> Texts { get; set; }//文字
        public List<Point3d> InDoorWaterMeters { get; set; }//进户水表
        public List<Point3d> MetersWithPrValve { get; set; }//水表带减压阀
        public List<Point3d> GateValves { get; set; }//闸阀
        public List<Point3d> DieValves { get; set; }//蝶阀
        public List<Point3d> PrValves { get; set; }//减压阀
        public List<Point3d> AutoValves { get; set; }//自动排气阀
        public List<Point3d> PrValveGroups { get; set; }//减压阀组
        public List<Point3d> PrValveDetail { get; set; }//减压阀组详图
        public List<Point3d> InTankDetail { get; set; }//水箱详图
        public List<Point3d> OutTankDetail { get; set; }//水箱详图
        public Dictionary<Point3d,string> ElevationDic { get; set; }//标高

        public SysOut()
        {
            FloorLines = new List<Line>();
            PipeLines = new List<Line>();
            TextLines = new List<Line>();
            Texts = new List<DBText>();
            InDoorWaterMeters = new List<Point3d>();
            MetersWithPrValve = new List<Point3d>();
            GateValves = new List<Point3d>();
            DieValves = new List<Point3d>();
            PrValves = new List<Point3d>();
            AutoValves = new List<Point3d>();
            PrValveGroups = new List<Point3d>();
            PrValveDetail = new List<Point3d>();
            InTankDetail = new List<Point3d>();
            OutTankDetail = new List<Point3d>();
            ElevationDic = new Dictionary<Point3d, string>();
        }

        public void Draw(RoofTankVM tankVM,double qg)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                BlocksImport.ImportElementsFromStdDwg();

                foreach (var line in FloorLines)
                {
                    var layer = "W-ZP-DIM";
                    if (!acadDatabase.Layers.Contains(layer))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layer, 30);
                    }
                    line.Layer = layer;
                    acadDatabase.CurrentSpace.Add(line);
                }

                var splitLines = Split(PipeLines);
                foreach (var line in splitLines)
                {
                    var layer = "W-WSUP-COOL-PIPE";
                    if (!acadDatabase.Layers.Contains(layer))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layer, 30);
                    }
                    line.Layer = layer;
                    acadDatabase.CurrentSpace.Add(line);
                }

                foreach(var line in TextLines)
                {
                    var layer = "W-WSUP-DIMS";
                    if (!acadDatabase.Layers.Contains(layer))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layer, 30);
                    }
                    line.Layer = layer;
                    acadDatabase.CurrentSpace.Add(line);
                }
                foreach(var text in Texts)
                {
                    acadDatabase.CurrentSpace.Add(text);
                }
                var blkLayer = "W-WSUP-EQPM";
                if (!acadDatabase.Layers.Contains(blkLayer))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, blkLayer, 30);
                }
                foreach (var pt in InDoorWaterMeters)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "室内水表详图", pt, new Scale3d(0.7, 0.7, 0.7), 0);
                }
                bool hasDrawDetail = false;
                foreach (var pt in MetersWithPrValve)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "室内水表带减压阀", pt, new Scale3d(0.7, 0.7, 0.7), 0);
                    if(!hasDrawDetail)
                    {
                        var line1 = new Line(pt.OffsetX(370), pt.OffsetXY(370, 850));
                        var line2 = new Line(pt.OffsetXY(4800, 850), pt.OffsetXY(370, 850));
                        line1.Layer = "W-WSUP-DIMS";
                        line2.Layer = "W-WSUP-DIMS";
                        acadDatabase.CurrentSpace.Add(line1);
                        acadDatabase.CurrentSpace.Add(line2);
                        acadDatabase.CurrentSpace.Add(ThText.DbText(pt.OffsetXY(2000, 850), "阀后压力约为0.15MPa", "W-WSUP-DIMS"));
                        hasDrawDetail = true;
                    }
                }
                foreach (var pt in PrValves)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "减压阀", pt, new Scale3d(0.7, 0.7, 0.7), 0);
                    if (!hasDrawDetail)
                    {
                        var line1 = new Line(pt.OffsetX(105), pt.OffsetXY(105, -800));
                        var line2 = new Line(pt.OffsetXY(-4000, -800), pt.OffsetXY(105, -800));
                        line1.Layer = "W-WSUP-DIMS";
                        line2.Layer = "W-WSUP-DIMS";
                        acadDatabase.CurrentSpace.Add(line1);
                        acadDatabase.CurrentSpace.Add(line2);
                        acadDatabase.CurrentSpace.Add(ThText.DbText(pt.OffsetXY(-4000, -800), "阀后压力约为0.15MPa", "W-WSUP-DIMS"));
                        hasDrawDetail = true;
                    }
                }
                foreach (var pt in AutoValves)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "自动排气阀系统1", pt, new Scale3d(0.6, 0.6, 0.6), 0);
                }

                foreach (var pt in PrValveGroups)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "减压阀组", pt, new Scale3d(1,1,1), 0);
                }
                foreach(var pt in PrValveDetail)
                {
                    var dic2 = new Dictionary<string, string>();
                    dic2.Add("可调式", "可调式");
                    dic2.Add("YL", "0.12");
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "系统减压阀组详图", 
                        pt, new Scale3d(1, 1, 1), 0,dic2);
                    objID.SetDynBlockValue("可见性", "垂直闸阀1");

                }

                var dic = CreateTankDetailDic(tankVM,qg);
                foreach (var pt in InTankDetail)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "高位生活水箱（内置消毒）", 
                        pt, new Scale3d(1, 1, 1), 0, dic);
                    AddTankText(acadDatabase, pt.OffsetXY(-3379,15000), tankVM);
                }
                foreach (var pt in OutTankDetail)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "高位生活水箱（外置消毒）", 
                        pt, new Scale3d(1, 1, 1), 0, dic);
                    AddTankText(acadDatabase, pt.OffsetXY(-3379, 15000), tankVM);
                }
                foreach (var pair in ElevationDic)
                {
                    var scaled = new Scale3d(1, 1, 1);
                    if (pair.Value.Equals(""))
                    {
                        scaled = new Scale3d(-1, 1, 1);
                    }
                    var attNameValues = new Dictionary<string, string>() { { "标高", pair.Value } };
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(blkLayer, "标高", 
                        pair.Key, scaled, 0, attNameValues);
                }

            }
        }

        private Dictionary<string,string> CreateTankDetailDic(RoofTankVM tankVM,double qg)
        {
            var dic = new Dictionary<string, string>();

            if(qg<20) dic.Add("1.5", "1.50");//KW
            else dic.Add("1.5", "2.20");//KW
            double Qg = Math.Ceiling(qg);
            dic.Add("10.0", Qg.ToString("0.00"));//流量
            dic.Add("H+1.0", "h+1.00");
            dic.Add("H+2.3", "h+"+(tankVM.TankHeight - 0.2).ToString("0.00"));
            dic.Add("H+2.5", "h+" + (tankVM.TankHeight).ToString("0.00"));
            dic.Add("H+2.45", "h+" + (tankVM.TankHeight - 0.05).ToString("0.00"));
            dic.Add("H(39.70)", (tankVM.Elevation).ToString("0.00"));
            dic.Add("11.87", (tankVM.TankLength* tankVM.TankWidth* (tankVM.TankHeight-0.6)).ToString("0.00"));
            dic.Add("长", Convert.ToString(tankVM.TankLength));
            dic.Add("宽", Convert.ToString(tankVM.TankWidth));
            dic.Add("高", Convert.ToString(tankVM.TankHeight));

            return dic;
        }

        private void AddTankText(AcadDatabase acadDatabase, Point3d spt, RoofTankVM tankVM)
        {
            var lines = new List<Line>();
            lines.Add(new Line(spt, spt.OffsetX(8940)));
            lines.Add(new Line(spt, spt.OffsetY(-5815)));
            lines.Add(new Line(spt.OffsetX(8940), spt.OffsetXY(8940, -5815)));
            lines.Add(new Line(spt.OffsetY(-5815), spt.OffsetXY(8940, -5815)));
            foreach(var line in lines)
            {
                line.Layer = "W-WSUP-DIMS";
                line.Linetype ="DASHED";
                acadDatabase.CurrentSpace.Add(line);
            }
            var strs = new List<string>();
            strs.Add("有效水深1.90m，水箱高2.5m。");
            strs.Add("地下生活加压泵供给的水箱进水管上电动阀开启，生活加压泵起泵，");
            strs.Add("全部电动阀关闭，生活加压泵停泵。");
            strs.Add("高位水箱水位控制原理：");

            strs.Add("1.溢流水位：h+" + (tankVM.TankHeight - 0.2).ToString("0.00") + "m； ");
            strs.Add("2.高报警水位：h+" + (tankVM.TankHeight - 0.25).ToString("0.00") + "m； ");
            strs.Add("3.电动阀关：h+"+ (tankVM.TankHeight - 0.3).ToString("0.00") + "m，水箱达此水位进水管电动阀关闭；");
            strs.Add("4.电动阀开：h+0.90m，水箱低于此水位进水管电动阀开启；");
            strs.Add("5.低报警水位：h+0.30m，且变频泵启动停泵保护；");
            strs.Add("6.水池底：h="+ tankVM.Elevation.ToString("0.00")+"m；");
            for(int i =0; i < strs.Count; i++)
            {
                var s = strs[i];
                var pt = spt.OffsetXY(230,-700-467*i);
                if (i > 3) pt = pt.OffsetY(-467);
                var text = ThText.DbText(pt,s,"W-WSUP-DIMS");
                acadDatabase.CurrentSpace.Add(text);
            }
        }


        private List<Line> Split(List<Line> lines)
        {
            

                double tolerance = 1;
                double gap = 100;
                var horizontalPipe = new List<Line>();//横管
                var verticalPipe = new List<Line>();//竖管
                foreach (var line in lines)
                {
                    var spt = line.StartPoint;
                    var ept = line.EndPoint;
                    if (Math.Abs(spt.X - ept.X) < tolerance)//若是竖管
                    {
                        verticalPipe.Add(line);
                        continue;
                    }
                    if (Math.Abs(spt.Y - ept.Y) < tolerance)//若是横管
                    {
                        horizontalPipe.Add(line);
                        continue;
                    }
                }
                foreach (var line in horizontalPipe.ToList())//遍历横管
                {
                    var spt = line.StartPoint;
                    var ept = line.EndPoint;
                    var vertical = new List<Line>();
                    var xs = new List<double>();
                    foreach (var line2 in verticalPipe)//遍历竖管
                    {
                        var spt2 = line2.StartPoint;
                        var ept2 = line2.EndPoint;
                        if ((spt.X - spt2.X) * (ept.X - spt2.X) < 0 &&
                            (spt.Y - spt2.Y) * (spt.Y - ept2.Y) < 0)
                        {
                            vertical.Add(line2);
                        }
                    }
                    if (vertical.Count == 0)
                    {
                        continue;
                    }
                    vertical = vertical.OrderBy(e => e.StartPoint.X).ToList();

                    var leftPt = new Point3d(Math.Min(spt.X, ept.X), spt.Y, 0);
                    var rightPt = new Point3d(Math.Max(spt.X, ept.X), spt.Y, 0);
                    xs.Add(Math.Min(spt.X, ept.X));
                    vertical.ForEach(e => xs.Add(e.StartPoint.X));
                    xs.Add(Math.Max(spt.X, ept.X));
                    for (int i = 0; i < xs.Count - 1; i++)
                    {
                        Line l;
                        if (i == 0)
                        {
                            l = new Line(new Point3d(xs[i], spt.Y, 0), new Point3d(xs[i + 1] - gap, spt.Y, 0));
                        }
                        else if (i == xs.Count - 2)
                        {
                            l = new Line(new Point3d(xs[i] + gap, spt.Y, 0), new Point3d(xs[i + 1], spt.Y, 0));
                        }
                        else
                        {
                            l = new Line(new Point3d(xs[i] + gap, spt.Y, 0), new Point3d(xs[i + 1] - gap, spt.Y, 0));
                        }
                        horizontalPipe.Add(l);
                    }
                    horizontalPipe.Remove(line);
                }
            var newLines = new List<Line>();
            newLines.AddRange(horizontalPipe);
            newLines.AddRange(verticalPipe);

            return newLines;

            }
        
    }
}
