using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Service.BranchFunc;


namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class BranchAcrossFloor
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var branchWithFireNums = 0;//
            double branchNums = 0;
            int throughIndex = 0;
            int index = 0;
            var lastFirePt = new Point3d();
            bool textRecord = false; //记录是否标记排气阀
            int currentFloor = Convert.ToInt32(sprayOut.CurrentFloor.Last().ToString());
            double floorHeight = sprayIn.FloorHeight;//楼层高度
            foreach (var pt in spraySystem.BranchDic.Keys)//pt 支路起始点
            {
                if (sprayIn.PtTypeDic.ContainsKey(pt))
                {
                    if (sprayIn.PtTypeDic[pt].Contains("BranchLoop"))
                    {
                        continue;
                    }
                }
                if (sprayIn.ThroughPt.Contains(pt))
                {
                    continue;
                }
                var ptFloor = Convert.ToInt32(pt._pt.GetFloor(sprayIn.FloorRectDic).Last().ToString());//获取当前点的楼层号

                if (ptFloor == currentFloor)
                {
                    Branch.BranchPtDraw(pt, ref textRecord, ref lastFirePt, ref branchWithFireNums, ref throughIndex, ref index, sprayOut, spraySystem, sprayIn);
                }
                if (ptFloor < currentFloor)//上一层（不带-号）
                {
                    double fireNums = 0;

                    if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
                    {
                        fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
                    }
                    if (!spraySystem.BranchPtDic.ContainsKey(pt))//支管图纸绘制起始点不存在
                    {
                        continue;//跳过这个点
                    }
                    var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                    var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点
                    if (!spraySystem.BranchDic.ContainsKey(pt))//支路列表没有这个点
                    {
                        continue;//跳过这个点
                    }
                    var tpts = spraySystem.BranchDic[pt];

                    if (Tool.HasAutoValve(pt, tpts, spraySystem, sprayIn))
                    {
                        Tool.DrawAutoValve(stPt, ref textRecord, sprayOut);
                    }

                    bool signelBranch = true;//第一个 type 4 标志

                    bool firstFireAlarmVisited = false;//访问过第一个防火分区
                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }

                        var termPt = sprayIn.TermPtDic[tpt];
                        string DN = Tool.GetDN(tpt, sprayIn);

                        if (termPt.Type == 1)//防火分区
                        {
                            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                            if (lastFirePt.DistanceTo(new Point3d()) > 10)//前一个点不为空
                            {
                                if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                                {
                                    branchNums = 0;
                                    throughIndex = 0;
                                    index = 0;
                                }
                            }

                            var firePt = fireStpt.OffsetX((fireNums - branchNums - 1) * 5500);
                            bool hasFlow = false;
                            string flowType = "";
                            if (spraySystem.FlowDIc.ContainsKey(tpt))
                            {
                                hasFlow = true;
                                flowType = spraySystem.FlowDIc[tpt];
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow, flowType);
                            if (firstFireAlarmVisited)
                            {
                                sprayOut.Texts.Add(new Text("DN150", new Point3d(firePt.X - 2000, stPt.Y, 0)));
                            }
                            firstFireAlarmVisited = true;
                            sprayOut.FireDistricts.Add(fireDistrict);
                            sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                            sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                            branchNums++;
                            lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
                            if (firePt.X > spraySystem.MaxOffSetX)
                            {
                                spraySystem.MaxOffSetX = firePt.X;
                            }
                        }
                        if (termPt.Type == 2)//其他楼层
                        {
                            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                            if (!spraySystem.BranchThroughDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            foreach (var cpt in spraySystem.BranchThroughDic[tpt])
                            {
                                if (!sprayIn.TermPtDic.ContainsKey(cpt))
                                {
                                    continue;
                                }
                                var termPt1 = sprayIn.TermPtDic[cpt];
                                var firePt = fireStpt.OffsetXY(-throughIndex * 5500 - sprayIn.PipeGap, -sprayIn.FloorHeight);
                                var pt1 = new Point3d(fireStpt.X - 500 * (index + 1), stPt.Y, 0);
                                var pt2 = new Point3d(pt1.X, firePt.Y + 600 * (index + 1), 0);
                                var pt3 = new Point3d(firePt.X, pt2.Y, 0);
                                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                                sprayOut.PipeLine.Add(new Line(pt3, firePt));
                                throughIndex++;

                                bool hasFlow = false;
                                string flowType = "";
                                if (spraySystem.FlowDIc.ContainsKey(tpt))
                                {
                                    hasFlow = true;
                                    flowType = spraySystem.FlowDIc[tpt];
                                }

                                var fireDistrict = new FireDistrictRight(firePt, termPt1,DN,hasFlow,flowType);
                                sprayOut.FireDistricts.Add(fireDistrict);
                            }
                            index++;
                        }
                        if (termPt.Type == 3)//水泵接合器
                        {
                            Type3.Get(stPt, termPt, DN, sprayOut);
                        }
                        if (termPt.Type == 4)
                        {
                            var pipeNumber = termPt.PipeNumber;
                            var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + floorHeight + 400, 0);
                            var pt2 = pt1.OffsetX(650);
                            var pt3 = pt2.OffsetY(1300);
                            double length = Tool.GetLength(pipeNumber) + 100;
                            var pt4 = Tool.GetEvadeStep(length, pt3, pipeNumber, spraySystem);

                            var pt5 = pt4.OffsetX(-length);
                            if (signelBranch)
                            {
                                sprayOut.PipeLine.Add(new Line(stPt4, pt1));
                            }
                            sprayOut.PipeLine.Add(new Line(pt1, pt2));
                            if (spraySystem.ValveDic.Contains(tpt))
                            {
                                sprayOut.PipeLine.Add(new Line(pt2, pt2.OffsetY(50)));
                                sprayOut.PipeLine.Add(new Line(pt2.OffsetY(350), pt3));
                                sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", pt2.OffsetY(50), Math.PI / 2));
                            }
                            else
                            {
                                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                            }

                            sprayOut.NoteLine.Add(new Line(pt3, pt4));
                            sprayOut.NoteLine.Add(new Line(pt4, pt5));
                            sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, Math.PI / 2));
                            var text = new Text(termPt.PipeNumber.Split('喷')[0], pt5);
                            var dn = new Text(DN, pt5.OffsetXY(150, -400));
                            sprayOut.Texts.Add(text);
                            sprayOut.Texts.Add(dn);
                            stPt4 = stPt4.OffsetX(600);
                            signelBranch = false;
                        }
                    }
                }
                if (ptFloor > currentFloor)//下一层
                {
                    if (!Tool.CheckValid(pt, spraySystem))
                    {
                        return;
                    }
                    double fireNums = 0;

                    if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
                    {
                        fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
                    }

                    var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                    var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点

                    var tpts = spraySystem.BranchDic[pt];
                    tpts.Reverse();
                    var hasAutoValve = Tool.HasAutoValve(pt, tpts, spraySystem, sprayIn);

                    if (hasAutoValve)
                    {
                        Tool.DrawAutoValve(stPt, ref textRecord, sprayOut);
                    }

                    bool firstFireAlarmVisited = false;//访问过第一个防火分区
                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }

                        var termPt = sprayIn.TermPtDic[tpt];
                        var DN = Tool.GetDN(tpt, sprayIn);

                        if (termPt.Type == 1)//防火分区
                        {
                            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt)) continue;
                            
                            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                            
                            if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                            {
                                branchNums = 0;
                                throughIndex = 0;
                                index = 0;
                            }
                            

                            var firePt = fireStpt.OffsetX((fireNums - branchNums - 1) * 5500);
                            bool hasFlow = false;
                            string flowType = "";
                            if (spraySystem.FlowDIc.ContainsKey(tpt))
                            {
                                hasFlow = true;
                                flowType = spraySystem.FlowDIc[tpt];
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow, flowType);
                            if (firstFireAlarmVisited)
                            {
                                sprayOut.Texts.Add(new Text("DN150", new Point3d(firePt.X - 2000, stPt.Y, 0)));
                            }
                            firstFireAlarmVisited = true;
                            sprayOut.FireDistricts.Add(fireDistrict);
                            sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                            sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                            branchNums++;
                            lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
                            if (firePt.X > spraySystem.MaxOffSetX)
                            {
                                spraySystem.MaxOffSetX = firePt.X;
                            }
                        }
                        if (termPt.Type == 2)//其他楼层
                        {
                            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                            if (!spraySystem.BranchThroughDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            foreach (var cpt in spraySystem.BranchThroughDic[tpt])
                            {
                                if (!sprayIn.TermPtDic.ContainsKey(cpt))
                                {
                                    continue;
                                }
                                var termPt1 = sprayIn.TermPtDic[cpt];
                                var cptFloor = Convert.ToInt32(cpt._pt.GetFloor(sprayIn.FloorRectDic).Last().ToString());//获取末端点的楼层号

                                var crossNum = ptFloor - cptFloor;
                                ;
                                if (crossNum > 0)//向上穿
                                {
                                    var firePt = fireStpt.OffsetXY(sprayIn.PipeGap + throughIndex * 5500, crossNum * sprayIn.FloorHeight);
                                    var pt1 = new Point3d(fireStpt.X + 500 * (index - 1), stPt.Y, 0);
                                    var pt2 = new Point3d(pt1.X, firePt.Y + 600 * (index + 1), 0);
                                    var pt3 = new Point3d(firePt.X, pt2.Y, 0);
                                    sprayOut.PipeLine.Add(new Line(stPt, pt1));
                                    sprayOut.PipeLine.Add(new Line(pt1, pt2));
                                    sprayOut.PipeLine.Add(new Line(pt2, pt3));
                                    sprayOut.PipeLine.Add(new Line(pt3, firePt));
                                    throughIndex++;
                                    bool hasFlow = false;
                                    string flowType = "";
                                    if (spraySystem.FlowDIc.ContainsKey(tpt))
                                    {
                                        hasFlow = true;
                                        flowType = spraySystem.FlowDIc[tpt];
                                    }
                                    var fireDistrict = new FireDistrictRight(firePt, termPt1, "", hasFlow,flowType);
                                    sprayOut.FireDistricts.Add(fireDistrict);
                                }
                                else//向下穿
                                {
                                    var firePt = fireStpt.OffsetXY(-throughIndex * 5500 - sprayIn.PipeGap, -sprayIn.FloorHeight);
                                    var pt1 = new Point3d(fireStpt.X - 500 * (index + 1), stPt.Y, 0);
                                    var pt2 = new Point3d(pt1.X, firePt.Y + 600 * (index + 1), 0);
                                    var pt3 = new Point3d(firePt.X, pt2.Y, 0);
                                    sprayOut.PipeLine.Add(new Line(stPt, pt1));
                                    sprayOut.PipeLine.Add(new Line(pt1, pt2));
                                    sprayOut.PipeLine.Add(new Line(pt2, pt3));
                                    sprayOut.PipeLine.Add(new Line(pt3, firePt));
                                    throughIndex++;
                                    bool hasFlow = false;
                                    string flowType = "";
                                    if (spraySystem.FlowDIc.ContainsKey(tpt))
                                    {
                                        hasFlow = true;
                                        flowType = spraySystem.FlowDIc[tpt];
                                    }
                                    var fireDistrict = new FireDistrictRight(firePt, termPt1, "", hasFlow, flowType);
                                    sprayOut.FireDistricts.Add(fireDistrict);
                                }

                            }
                            index++;
                        }
                        if (termPt.Type == 3)//水泵接合器
                        {
                            Type3.Get(stPt, termPt, DN, sprayOut);
                        }
                        if (termPt.Type == 4)
                        {
                            double length = Tool.GetLength(termPt.PipeNumber) + 100;

                            {
                                var fireStpt = spraySystem.FireAreaStPtDic[pt];
                                if (lastFirePt.DistanceTo(new Point3d()) > 10)//前一个点不为空
                                {
                                    if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                                    {
                                        branchNums = 0;
                                        throughIndex = 0;
                                        index = 0;
                                    }
                                }

                                var firePt = fireStpt.OffsetX((fireNums - branchNums - 1) * 5500);
                                var pt1 = new Point3d(firePt.X - 2000, stPt.Y, 0);
                                var pt2 = pt1.OffsetY(-600);
                                var pt3 = pt2.OffsetX(1200);
                                var pt4 = pt3.OffsetY(-1200);
                                var pt5 = pt4.OffsetX(-length);
                                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                                sprayOut.NoteLine.Add(new Line(pt3, pt4));
                                sprayOut.NoteLine.Add(new Line(pt4, pt5));
                                sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, 0));
                                var text = new Text(termPt.PipeNumber, pt5);
                                sprayOut.Texts.Add(text);
                            }

                        }
                    }
                }
            }
        }

        public static void GetInOtherFloor(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double branchNums = 0;
            int throughIndex = 0;
            int index = 0;
            var lastFirePt = new Point3d();
            bool textRecord = false; //记录是否标记排气阀
            foreach (var pt in spraySystem.BranchDic.Keys)//pt 支路起始点
            {
                if (sprayIn.ThroughPt.Contains(pt))
                {
                    continue;
                }

                double fireNums = 0;

                if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
                {
                    fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
                }
                if (!spraySystem.BranchPtDic.ContainsKey(pt))//支管图纸绘制起始点不存在
                {
                    continue;//跳过这个点
                }
                var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点
                if (!spraySystem.BranchDic.ContainsKey(pt))//支路列表没有这个点
                {
                    continue;//跳过这个点
                }
                var tpts = spraySystem.BranchDic[pt];

                if (Tool.HasAutoValve(pt, tpts, spraySystem, sprayIn))
                {
                    Tool.DrawAutoValve(stPt, ref textRecord, sprayOut);
                }

                bool signelBranch = true;//第一个 type 4 标志

                bool firstFireAlarmVisited = false;
                foreach (var tpt in tpts)// tpt 支路端点
                {
                    if (!sprayIn.TermPtDic.ContainsKey(tpt))
                    {
                        continue;
                    }

                    var termPt = sprayIn.TermPtDic[tpt];
                    var DN = Tool.GetDN(tpt, sprayIn);

                    if (termPt.Type == 1)//防火分区
                    {
                        if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        var fireStpt = spraySystem.FireAreaStPtDic[pt];
                        if (lastFirePt.DistanceTo(new Point3d()) > 10)//前一个点不为空
                        {
                            if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                            {
                                branchNums = 0;
                                throughIndex = 0;
                                index = 0;
                            }
                        }

                        var firePt = fireStpt.OffsetX((fireNums - branchNums - 1) * 5500);
                        bool hasFlow = false;
                        string flowType = "";
                        if (spraySystem.FlowDIc.ContainsKey(tpt))
                        {
                            hasFlow = true;
                            flowType = spraySystem.FlowDIc[tpt];
                        }
                        var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow, flowType);
                        if (firstFireAlarmVisited)
                        {
                            sprayOut.Texts.Add(new Text("DN150", new Point3d(firePt.X - 2000, stPt.Y, 0)));
                        }
                        firstFireAlarmVisited = true;
                        sprayOut.FireDistricts.Add(fireDistrict);
                        sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                        sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                        branchNums++;
                        lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
                        if (firePt.X > spraySystem.MaxOffSetX)
                        {
                            spraySystem.MaxOffSetX = firePt.X;
                        }
                    }
                    if (termPt.Type == 2)//其他楼层
                    {
                        if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        var fireStpt = spraySystem.FireAreaStPtDic[pt];
                        if (!spraySystem.BranchThroughDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        foreach (var cpt in spraySystem.BranchThroughDic[tpt])
                        {
                            if (!sprayIn.TermPtDic.ContainsKey(cpt))
                            {
                                continue;
                            }
                            var termPt1 = sprayIn.TermPtDic[cpt];
                            var firePt = fireStpt.OffsetXY(-throughIndex * 5500 - sprayIn.PipeGap, -sprayIn.FloorHeight);
                            var pt1 = new Point3d(fireStpt.X - 500 * (index + 1), stPt.Y, 0);
                            var pt2 = new Point3d(pt1.X, firePt.Y + 600 * (index + 1), 0);
                            var pt3 = new Point3d(firePt.X, pt2.Y, 0);
                            sprayOut.PipeLine.Add(new Line(stPt, pt1));
                            sprayOut.PipeLine.Add(new Line(pt1, pt2));
                            sprayOut.PipeLine.Add(new Line(pt2, pt3));
                            sprayOut.PipeLine.Add(new Line(pt3, firePt));
                            throughIndex++;
                            bool hasFlow = false;
                            string flowType = "";
                            if (spraySystem.FlowDIc.ContainsKey(tpt))
                            {
                                hasFlow = true;
                                flowType = spraySystem.FlowDIc[tpt];
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt1,"",hasFlow,flowType);
                            sprayOut.FireDistricts.Add(fireDistrict);
                        }
                        index++;
                    }
                    if (termPt.Type == 3)//水泵接合器
                    {
                        Type3.Get(stPt, termPt, DN, sprayOut);
                    }
                    if (termPt.Type == 4)
                    {
                        bool needEvade = true;//默认需要躲避
                        var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + 400, 0);
                        var pt2 = pt1.OffsetX(650);
                        var pt3 = pt2.OffsetY(1300);
                        double length = Tool.GetLength(termPt.PipeNumber) + 100;
                        double stepSize = 450;
                        int indx = 0;
                        while (needEvade)
                        {
                            var textPt = pt3.OffsetXY(-length, 900 + stepSize * indx);
                            var text2 = new Text(termPt.PipeNumber, textPt);
                            var texts = new List<DBText>() { text2.DbText };
                            var line34 = new Line(pt3, pt3.OffsetY(900 + stepSize * indx));
                            var lines = new List<Line>() { line34 };
                            if (spraySystem.BlockExtents.SelectAll().Count == 0)//外包框数目为0
                            {
                                spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                                spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                                break;
                            }
                            else
                            {
                                var rect = Tool.GetRect(text2.DbText);//提框
                                var dbObjs = spraySystem.BlockExtents.SelectCrossingPolygon(rect);
                                if (dbObjs.Count == 0)
                                {
                                    spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                                    spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                                    break;
                                }
                            }
                            indx++;
                        }
                        var pt4 = pt3.OffsetY(900 + stepSize * indx);
                        var pt5 = pt4.OffsetX(-length);
                        if (signelBranch)
                        {
                            sprayOut.PipeLine.Add(new Line(stPt4, pt1));

                        }
                        sprayOut.PipeLine.Add(new Line(pt1, pt2));
                        if (spraySystem.ValveDic.Contains(tpt))
                        {
                            sprayOut.PipeLine.Add(new Line(pt2, pt2.OffsetY(50)));
                            sprayOut.PipeLine.Add(new Line(pt2.OffsetY(350), pt3));
                            sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", pt2.OffsetY(50), Math.PI / 2));
                        }
                        else
                        {
                            sprayOut.PipeLine.Add(new Line(pt2, pt3));
                        }

                        sprayOut.NoteLine.Add(new Line(pt3, pt4));
                        sprayOut.NoteLine.Add(new Line(pt4, pt5));
                        sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, Math.PI / 2));
                        var text = new Text(termPt.PipeNumber, pt5);
                        var dn = new Text(DN, pt5.OffsetXY(150, -400));
                        sprayOut.Texts.Add(text);
                        sprayOut.Texts.Add(dn);
                        stPt4 = stPt4.OffsetX(600);
                        signelBranch = false;
                    }
                }
            }
        }
    }
}
