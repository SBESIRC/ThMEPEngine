using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Service.BranchFunc;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class Branch
    {
        public static void AlarmValveGet(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double branchNums = 0;
            int throughIndex = 0;
            int index = 0;
            foreach (var pt in spraySystem.BranchDic.Keys)//pt 支路起始点
            {
                int fireAreaIndex = 0;
                var fireStpt = spraySystem.BranchPtDic[pt];//图纸绘制起始点;
                if (sprayIn.TermPtDic.ContainsKey(pt))
                {
                    var str = sprayIn.TermPtDic[pt].PipeNumber;
                    var stPt1 = fireStpt;
                    var stPt2 = stPt1.OffsetY(2000);
                    var stPt3 = stPt2.OffsetX(1000);
                    sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                    sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                    sprayOut.Texts.Add(new Text(str, stPt2));
                }

                if (!Tool.CheckValid(pt, spraySystem))
                {
                    return;
                }
                var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点

                var tpts = spraySystem.BranchDic[pt];

                tpts.Reverse();

                var needNewDrawings = Tool.NeedNewDrawing(tpts, sprayIn);//同时存在采用新的画法

                int type4Nums = 0;//类型为4的数目
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
                        type4Nums = 0;

                        var firePt = fireStpt.OffsetX(fireAreaIndex * 5500);
                        fireAreaIndex++;
                        bool hasFlow = false;
                        string flowType = "";
                        if (spraySystem.FlowDIc.ContainsKey(tpt))
                        {
                            hasFlow = true;
                            flowType = spraySystem.FlowDIc[tpt];
                        }
                        var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow, flowType);
                        sprayOut.FireDistricts.Add(fireDistrict);
                        sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                        sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                        branchNums++;
                        if (firePt.X > spraySystem.MaxOffSetX)
                        {
                            spraySystem.MaxOffSetX = firePt.X;
                        }
                    }
                    if (termPt.Type == 2)//其他楼层
                    {
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
                            var fireDistrict = new FireDistrictLeft(firePt, termPt1,"",hasFlow,flowType);
                            sprayOut.FireDistrictLefts.Add(fireDistrict);
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
                        var firePt = fireStpt;
                        var pt1 = new Point3d(firePt.X + type4Nums * (length + 500) + 1000, stPt.Y, 0);
                        type4Nums++;
                        var pt2 = pt1.OffsetX(1200);
                        var pt3 = pt2.OffsetY(-1200);
                        var pt4 = pt3.OffsetX(-length);
                        sprayOut.PipeLine.Add(new Line(stPt, pt1));
                        sprayOut.PipeLine.Add(new Line(pt1, pt2));
                        sprayOut.NoteLine.Add(new Line(pt2, pt3));
                        sprayOut.NoteLine.Add(new Line(pt3, pt4));
                        sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt2, 0));
                        var text = new Text(termPt.PipeNumber, pt4);
                        sprayOut.Texts.Add(text);
                    }
                }
            }
        }

        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var branchWithFireNums = 0;//
            int throughIndex = 0;
            int index = 0;
            var lastFirePt = new Point3d();
            bool textRecord = false; //记录是否标记排气阀
            foreach (var pt in spraySystem.BranchDic.Keys)//pt 支路起始点
            {
                BranchPtDraw(pt, ref textRecord, ref lastFirePt, ref branchWithFireNums, ref throughIndex, ref index, sprayOut, spraySystem, sprayIn);
            }
        }

        
        public static void BranchPtDraw(Point3dEx pt, ref bool textRecord, ref Point3d lastFirePt, ref int branchWithFireNums, ref int throughIndex, ref int index,
            SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            if(!Tool.CheckValid(pt,spraySystem))
            {
                return;
            }

            double fireNums = 0;//该点所在报警阀间防火分区总数（无防火分区的报警阀为 1，环管上的点为 0）

            if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
            {
                fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
            }
            
            var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
            var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点

            var tpts = spraySystem.BranchDic[pt];
            tpts.Reverse();
            var hasAutoValve = Tool.HasAutoValve(pt, tpts, spraySystem, sprayIn);
            var needNewDrawings = Tool.NeedNewDrawing(tpts, sprayIn);//同时存在采用新的画法

            if (hasAutoValve)
            {
                Tool.DrawAutoValve(stPt, ref textRecord, sprayOut);
            }

            bool signelBranch = true;//第一个 type 4 标志

            int type4Nums = 0;//类型为4的数目
            bool hasType1 = false;
            foreach (var tpt in tpts)// tpt 支路端点
            {
                if (!sprayIn.TermPtDic.ContainsKey(tpt)) continue;
                
                var DN = Tool.GetDN(tpt, sprayIn);
                var termPt = sprayIn.TermPtDic[tpt];

                if (termPt.Type == 1)//防火分区
                {
                    hasType1 = true;
                    type4Nums = 0;
                    if (!spraySystem.FireAreaStPtDic.ContainsKey(pt)) continue;
                    
                    var fireStpt = spraySystem.FireAreaStPtDic[pt];

                    if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                    {
                        branchWithFireNums = 0;
                        throughIndex = 0;
                        index = 0;
                    }


                    var firePt = fireStpt.OffsetX((fireNums - branchWithFireNums - 1) * 5500);
                    bool hasFlow = false;
                    string flowType = "";
                    if (spraySystem.FlowDIc.ContainsKey(tpt))
                    {
                        hasFlow = true;
                        flowType = spraySystem.FlowDIc[tpt];
                    }
                    var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow,flowType);
                    sprayOut.FireDistricts.Add(fireDistrict);
                    sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                    sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                    branchWithFireNums++;
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
                        var fireDistrict = new FireDistrictLeft(firePt, termPt1,"",hasFlow,flowType);
                        sprayOut.FireDistrictLefts.Add(fireDistrict);
                    }
                    index++;
                }
                if (termPt.Type == 3)//水泵接合器
                {
                    Type3.Get(stPt, termPt, DN, sprayOut);
                }
                if (termPt.Type == 4 || termPt.Type == 5)
                {
                    if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                    {
                        var pipeNumber = termPt.PipeNumber;
                        Type4NeedEvade(ref stPt4, ref signelBranch, tpt, pipeNumber, DN, sprayOut, spraySystem);
                        continue;
                    }

                    var fireStpt = spraySystem.FireAreaStPtDic[pt];



                    double length = Tool.GetLength(termPt.PipeNumber) + 100;
                    if (termPt.PipeNumber.Equals(""))
                    {
                        length = Tool.GetLength("DNXXX") + 100;
                    }
                    if (needNewDrawings)
                    {
                        //if (lastFirePt.DistanceTo(new Point3d()) > 10)//前一个点不为空
                        {
                            if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                            {
                                branchWithFireNums = 0;
                                throughIndex = 0;
                                index = 0;
                            }
                        }
                        lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);

                        var firePt = fireStpt.OffsetX((fireNums - branchWithFireNums - 1) * 5500);

                        //var firePt = fireStpt.OffsetX((fireNums - branchWithFireNums - 1) * 5500);
                        var pt1 = new Point3d(firePt.X - type4Nums * (length + 500) - 1000 + 5000, stPt.Y, 0);
                        var pt2 = pt1.OffsetY(-600);
                        var pt3 = pt2.OffsetX(1200);
                        var pt4 = pt3.OffsetY(-1200);
                        var pt5 = pt4.OffsetX(-length);
                        sprayOut.PipeLine.Add(new Line(stPt, pt1));
                        sprayOut.PipeLine.Add(new Line(pt1, pt2));
                        sprayOut.PipeLine.Add(new Line(pt2, pt3));
                        sprayOut.NoteLine.Add(new Line(pt3, pt4));
                        sprayOut.NoteLine.Add(new Line(pt4, pt5));
                        sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, 0));
                        var text = new Text(termPt.PipeNumber, pt5);
                        sprayOut.Texts.Add(text);

                    }
                    else
                    {
                        var firePt = fireStpt.OffsetX((fireNums - branchWithFireNums - 1) * 5500);
                        if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                        {
                            branchWithFireNums = 0;
                            throughIndex = 0;
                            index = 0;
                        }
                        lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
                        var pipeNumber = termPt.PipeNumber;
                        var pt1 = new Point3d(firePt.X + type4Nums * (length + 500) + 1000, stPt.Y, 0);
                        var pt2 = new Point3d(pt1.X, sprayOut.PipeInsertPoint.Y + 400, 0);
                        var pt3 = pt2.OffsetY(1300);
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
                        var dn = new Text(DN, pt5.OffsetXY(50, -400));
                        sprayOut.Texts.Add(text);
                        sprayOut.Texts.Add(dn);
                        stPt4 = stPt4.OffsetX(600);
                        signelBranch = false;
                    }
                    type4Nums++;
                }
            }
            if (!hasType1) branchWithFireNums++;
        }

        public static void Type4NeedEvade(ref Point3d stPt4, ref bool signelBranch, Point3dEx tpt, string pipeNumber,
            string DN, SprayOut sprayOut, SpraySystem spraySystem)
        {
            bool needEvade = true;//默认需要躲避
            var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + 400, 0);
            var pt2 = pt1.OffsetX(650);
            var pt3 = pt2.OffsetY(1300);
            double length = Tool.GetLength(pipeNumber) + 100;
            if (pipeNumber.Equals(""))
            {
                length = Tool.GetLength("DNXXX") + 100;
            }
            double stepSize = 450;
            int indx = 0;
            while (needEvade)
            {
                var textPt = pt3.OffsetXY(-length, 900 + stepSize * indx);
                var text2 = new Text(pipeNumber, textPt);
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
            var text = new Text(pipeNumber, pt5);
            var dn = new Text(DN, pt5.OffsetXY(50, -400));
            sprayOut.Texts.Add(text);
            sprayOut.Texts.Add(dn);
            stPt4 = stPt4.OffsetX(600);
            signelBranch = false;
        }

        public static void Type5NeedEvade(ref Point3d stPt4, ref bool signelBranch, Point3dEx tpt, string pipeNumber,
            string DN, SprayOut sprayOut, SpraySystem spraySystem)
        {
            bool needEvade = true;//默认需要躲避
            var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + 400, 0);
            var pt2 = pt1.OffsetX(650);
            var pt3 = pt2.OffsetY(1300);
            double length = Tool.GetLength(pipeNumber) + 100;
            if (pipeNumber.Equals(""))
            {
                length = Tool.GetLength("DNXXX") + 100;
            }
            double stepSize = 450;
            int indx = 0;
            while (needEvade)
            {
                var textPt = pt3.OffsetXY(-length, 900 + stepSize * indx);
                var text2 = new Text(pipeNumber, textPt);
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
            var text = new Text(pipeNumber, pt5);
            var dn = new Text(DN, pt5.OffsetXY(50, -400));
            sprayOut.Texts.Add(text);
            sprayOut.Texts.Add(dn);
            stPt4 = stPt4.OffsetX(600);
            signelBranch = false;
        }

        
       
    }
}

