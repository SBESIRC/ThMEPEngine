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
                if(sprayIn.PtTypeDic.ContainsKey(pt))
                {
                    if(sprayIn.PtTypeDic[pt].Contains("BranchLoop"))
                    {
                        continue;
                    }
                }
                if(sprayIn.ThroughPt.Contains(pt))
                {
                    continue;
                }
                var ptFloor = Convert.ToInt32(pt._pt.GetFloor(sprayIn.FloorRectDic).Last().ToString());//获取当前点的楼层号


                //if (ptFloor == currentFloor)
                //{
                //    double fireNums = 0;

                //    if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
                //    {
                //        fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
                //    }
                //    if (!spraySystem.BranchPtDic.ContainsKey(pt))//支管图纸绘制起始点不存在
                //    {
                //        continue;//跳过这个点
                //    }
                //    var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                //    var stPt4 = spraySystem.BranchPtDic[pt];//图纸绘制支路4起始点
                //    if (!spraySystem.BranchDic.ContainsKey(pt))//支路列表没有这个点
                //    {
                //        continue;//跳过这个点
                //    }
                //    var tpts = spraySystem.BranchDic[pt];

                //    bool hasAutoValve = true;
                //    foreach (var tpt in tpts)
                //    {
                //        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                //        {
                //            continue;
                //        }
                //        var type = sprayIn.TermPtDic[tpt].Type;
                //        if (type == 1 || type == 2)//端点类型是防火分区或者其他楼层
                //        {
                //            continue;
                //        }
                //        hasAutoValve = false;//没有自动排气阀
                //        break;
                //    }

                //    if (hasAutoValve)
                //    {
                //        sprayOut.SprayBlocks.Add(new SprayBlock("自动排气阀系统1", stPt));
                //        if (!textRecord)
                //        {
                //            textRecord = true;
                //            var stPt1 = stPt.OffsetY(731.7);
                //            var stPt2 = stPt1.OffsetY(2000);
                //            var stPt3 = stPt2.OffsetX(1000);
                //            sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                //            sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                //            var text = new Text("DN25排气阀余同", stPt2);
                //            sprayOut.Texts.Add(text);
                //        }
                //    }

                //    bool signelBranch = true;//第一个 type 4 标志
                //    var cnt = tpts.Count;
                //    bool firstFireAlarmVisited = false;

                //    bool hasType1 = false;
                //    int type4Nums = 0;
                //    foreach (var tpt in tpts)// tpt 支路端点
                //    {
                //        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                //        {
                //            continue;
                //        }

                //        var termPt = sprayIn.TermPtDic[tpt];
                //        string DN = "DNXXX";
                //        foreach (var vpt in sprayIn.TermDnDic.Keys)
                //        {
                //            if (tpt._pt.DistanceTo(vpt._pt) < 100)//端点管径标注包含当前点
                //            {
                //                DN = sprayIn.TermDnDic[vpt];
                //                break;
                //            }
                //        }

                //        if (termPt.Type == 1)//防火分区
                //        {
                //            type4Nums = 0;
                //            hasType1 = true;

                //            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                //            {
                //                continue;
                //            }
                //            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                //            if (lastFirePt.DistanceTo(new Point3d()) > 10)//前一个点不为空
                //            {
                //                if (lastFirePt.DistanceTo(fireStpt) > 10)//起始点变了，说明支环变了
                //                {
                //                    branchNums = 0;
                //                    throughIndex = 0;
                //                    index = 0;
                //                }
                //            }

                //            var firePt = fireStpt.OffsetX((fireNums - branchNums - 1) * 5500);
                //            bool hasFlow = false;
                //            if (spraySystem.FlowDIc.Contains(tpt))
                //            {
                //                hasFlow = true;
                //            }
                //            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow);
                //            if (firstFireAlarmVisited)
                //            {
                //                sprayOut.Texts.Add(new Text("DN150", new Point3d(firePt.X - 2000, stPt.Y, 0)));
                //            }
                //            firstFireAlarmVisited = true;
                //            sprayOut.FireDistricts.Add(fireDistrict);
                //            sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                //            sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                //            branchNums++;
                //            lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
                //            if (firePt.X > spraySystem.MaxOffSetX)
                //            {
                //                spraySystem.MaxOffSetX = firePt.X;
                //            }
                //        }
                //        if (termPt.Type == 2)//其他楼层
                //        {
                //            if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))
                //            {
                //                continue;
                //            }
                //            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                //            if (!spraySystem.BranchThroughDic.ContainsKey(tpt))
                //            {
                //                continue;
                //            }
                //            foreach (var cpt in spraySystem.BranchThroughDic[tpt])
                //            {
                //                if (!sprayIn.TermPtDic.ContainsKey(cpt))
                //                {
                //                    continue;
                //                }
                //                var termPt1 = sprayIn.TermPtDic[cpt];
                //                var firePt = fireStpt.OffsetXY(-throughIndex * 5500 - sprayIn.PipeGap, -sprayIn.FloorHeight);
                //                var pt1 = new Point3d(fireStpt.X - 500 * (index + 1), stPt.Y, 0);
                //                var pt2 = new Point3d(pt1.X, firePt.Y + 600 * (index + 1), 0);
                //                var pt3 = new Point3d(firePt.X, pt2.Y, 0);
                //                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                //                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                //                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                //                sprayOut.PipeLine.Add(new Line(pt3, firePt));
                //                throughIndex++;

                //                var fireDistrict = new FireDistrictLeft(firePt, termPt1);
                //                sprayOut.FireDistrictLefts.Add(fireDistrict);
                //            }
                //            index++;
                //        }
                //        if (termPt.Type == 3)//水泵接合器
                //        {
                //            var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 1300, 0);
                //            sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
                //            sprayOut.WaterPumps.Add(new WaterPump(pumpPt, termPt.PipeNumber, DN));
                //        }
                //        if (termPt.Type == 4)
                //        {
                //            bool needEvade = true;//默认需要躲避
                //            double length = GetLength(termPt.PipeNumber) + 100;

                //            var fireStpt = spraySystem.FireAreaStPtDic[pt];
                //            var firePt = fireStpt.OffsetX((fireNums - branchWithFireNums - 1) * 5500);
                //            var pt1 = new Point3d(firePt.X + type4Nums * (length + 500) + 1000, stPt.Y, 0);
                //            var pt2 = pt1.OffsetX(650);
                //            var pt3 = pt2.OffsetY(1300);
                //            double stepSize = 450;
                //            int indx = 0;
                //            while (needEvade)
                //            {
                //                var textPt = pt3.OffsetXY(-length, 900 + stepSize * indx);
                //                var text2 = new Text(termPt.PipeNumber, textPt);
                //                var texts = new List<DBText>() { text2.DbText };
                //                var line34 = new Line(pt3, pt3.OffsetY(900 + stepSize * indx));
                //                var lines = new List<Line>() { line34 };
                //                if (spraySystem.BlockExtents.SelectAll().Count == 0)//外包框数目为0
                //                {
                //                    spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                //                    spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                //                    break;
                //                }
                //                else
                //                {
                //                    var rect = GetRect(text2.DbText);//提框
                //                    var dbObjs = spraySystem.BlockExtents.SelectCrossingPolygon(rect);
                //                    if (dbObjs.Count == 0)
                //                    {
                //                        spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                //                        spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                //                        break;
                //                    }
                //                }
                //                indx++;
                //            }
                //            var pt4 = pt3.OffsetY(900 + stepSize * indx);
                //            var pt5 = pt4.OffsetX(-length);
                //            if (signelBranch)
                //            {
                //                sprayOut.PipeLine.Add(new Line(stPt4, pt1));

                //            }
                //            sprayOut.PipeLine.Add(new Line(pt1, pt2));
                //            if (spraySystem.ValveDic.Contains(tpt))
                //            {
                //                sprayOut.PipeLine.Add(new Line(pt2, pt2.OffsetY(50)));
                //                sprayOut.PipeLine.Add(new Line(pt2.OffsetY(350), pt3));
                //                sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", pt2.OffsetY(50), Math.PI / 2));
                //            }
                //            else
                //            {
                //                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                //            }

                //            sprayOut.NoteLine.Add(new Line(pt3, pt4));
                //            sprayOut.NoteLine.Add(new Line(pt4, pt5));
                //            sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, Math.PI / 2));
                //            var text = new Text(termPt.PipeNumber, pt5);
                //            var dn = new Text(DN, pt5.OffsetXY(150, -400));
                //            sprayOut.Texts.Add(text);
                //            sprayOut.Texts.Add(dn);
                //            stPt4 = stPt4.OffsetX(600);
                //            signelBranch = false;
                //            type4Nums++;
                //        }
                //    }
                //    if (!hasType1) branchWithFireNums++;
                //}
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

                    bool hasAutoValve = true;
                    foreach (var tpt in tpts)
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        var type = sprayIn.TermPtDic[tpt].Type;
                        if (type == 1 || type == 2)//端点类型是防火分区或者其他楼层
                        {
                            continue;
                        }
                        hasAutoValve = false;//没有自动排气阀
                        break;
                    }

                    if (hasAutoValve)
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("自动排气阀系统1", stPt));
                        if (!textRecord)
                        {
                            textRecord = true;
                            var stPt1 = stPt.OffsetY(731.7);
                            var stPt2 = stPt1.OffsetY(2000);
                            var stPt3 = stPt2.OffsetX(1000);
                            sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                            sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                            var text = new Text("DN25排气阀余同", stPt2);
                            sprayOut.Texts.Add(text);
                        }
                    }

                    bool signelBranch = true;//第一个 type 4 标志
                    var cnt = tpts.Count;
                    if(cnt > 2)
                    {
                        ;
                    }
                    bool firstFireAlarmVisited = false;//访问过第一个防火分区
                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }

                        var termPt = sprayIn.TermPtDic[tpt];
                        string DN = "DNXXX";
                        foreach (var vpt in sprayIn.TermDnDic.Keys)
                        {
                            if (tpt._pt.DistanceTo(vpt._pt) < 100)//端点管径标注包含当前点
                            {
                                DN = sprayIn.TermDnDic[vpt];
                                break;
                            }
                        }

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
                            if (spraySystem.FlowDIc.Contains(tpt))
                            {
                                hasFlow = true;
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow);
                            if(firstFireAlarmVisited)
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

                                var fireDistrict = new FireDistrictLeft(firePt, termPt1);
                                sprayOut.FireDistrictLefts.Add(fireDistrict);
                            }
                            index++;
                        }
                        if (termPt.Type == 3)//水泵接合器
                        {
                            var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 1300, 0);
                            sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
                            sprayOut.WaterPumps.Add(new WaterPump(pumpPt, termPt.PipeNumber, DN));
                        }
                        if (termPt.Type == 4)
                        {
                            bool needEvade = true;//默认需要躲避
                            var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + floorHeight + 400, 0);
                            var pt2 = pt1.OffsetX(650);
                            var pt3 = pt2.OffsetY(1300);
                            double length = GetLength(termPt.PipeNumber) + 100;
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
                                    var rect = GetRect(text2.DbText);//提框
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

                    bool hasAutoValve = true;
                    foreach (var tpt in tpts)
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        var type = sprayIn.TermPtDic[tpt].Type;
                        if (type == 1 || type == 2)//端点类型是防火分区或者其他楼层
                        {
                            continue;
                        }
                        hasAutoValve = false;//没有自动排气阀
                        break;
                    }
                    bool hasFireArea = false;
                    bool hasRiser = false;
                    foreach(var tpt in tpts)
                    {
                        var type = sprayIn.TermPtDic[tpt].Type;
                        if (type==1)//防火分区
                        {
                            hasFireArea = true;
                        }
                        if(type==4)//立管
                        {
                            hasRiser = true;
                        }
                    }
                    if(sprayIn.TermPtDic[tpts[0]].Type == 1)
                    {
                        tpts.Reverse();
                    }
                    var needNewDrawings = hasFireArea && hasRiser;//同时存在采用新的画法

                    if (hasAutoValve)
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("自动排气阀系统1", stPt));
                        if (!textRecord)
                        {
                            textRecord = true;
                            var stPt1 = stPt.OffsetY(731.7);
                            var stPt2 = stPt1.OffsetY(2000);
                            var stPt3 = stPt2.OffsetX(1000);
                            sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                            sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                            var text = new Text("DN25排气阀余同", stPt2);
                            sprayOut.Texts.Add(text);
                        }
                    }

                    bool signelBranch = true;//第一个 type 4 标志
                    var cnt = tpts.Count;
                    if (cnt > 2)
                    {
                        ;
                    }
                    bool firstFireAlarmVisited = false;//访问过第一个防火分区
                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }

                        var termPt = sprayIn.TermPtDic[tpt];
                        string DN = "DNXXX";
                        foreach (var vpt in sprayIn.TermDnDic.Keys)
                        {
                            if (tpt._pt.DistanceTo(vpt._pt) < 100)//端点管径标注包含当前点
                            {
                                DN = sprayIn.TermDnDic[vpt];
                                break;
                            }
                        }

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
                            if (spraySystem.FlowDIc.Contains(tpt))
                            {
                                hasFlow = true;
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow);
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
                                if(crossNum > 0)//向上穿
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

                                    var fireDistrict = new FireDistrictRight(firePt, termPt1, "", true);
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

                                    var fireDistrict = new FireDistrictLeft(firePt, termPt1);
                                    sprayOut.FireDistrictLefts.Add(fireDistrict);
                                }
                                
                            }
                            index++;
                        }
                        if (termPt.Type == 3)//水泵接合器
                        {
                            var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 1300, 0);
                            sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
                            sprayOut.WaterPumps.Add(new WaterPump(pumpPt, termPt.PipeNumber, DN));
                        }
                        if (termPt.Type == 4)
                        {
                            double length = GetLength(termPt.PipeNumber) + 100;

                            if (needNewDrawings)
                            {
                                ;
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
                                var pt1 = new Point3d(firePt.X-2000, stPt.Y, 0);
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
                            else
                            {
                                bool needEvade = true;//默认需要躲避
                                var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y - floorHeight + 400, 0);
                                var pt2 = pt1.OffsetX(650);
                                var pt3 = pt2.OffsetY(1300);
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
                                        var rect = GetRect(text2.DbText);//提框
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
                                var text = new Text(termPt.PipeNumber.Split('喷')[0], pt5);
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

        public static void GetInOtherFloor(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double branchNums = 0;
            int throughIndex = 0;
            int index = 0;
            var lastFirePt = new Point3d();
            bool textRecord = false; //记录是否标记排气阀
            int currentFloor = Convert.ToInt32(sprayOut.CurrentFloor.Last());
            double floorHeight = sprayIn.FloorHeight;//楼层高度
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

                    bool hasAutoValve = true;
                    foreach (var tpt in tpts)
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        var type = sprayIn.TermPtDic[tpt].Type;
                        if (type == 1 || type == 2)//端点类型是防火分区或者其他楼层
                        {
                            continue;
                        }
                        hasAutoValve = false;//没有自动排气阀
                        break;
                    }

                    if (hasAutoValve)
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("自动排气阀系统1", stPt));
                        if (!textRecord)
                        {
                            textRecord = true;
                            var stPt1 = stPt.OffsetY(731.7);
                            var stPt2 = stPt1.OffsetY(2000);
                            var stPt3 = stPt2.OffsetX(1000);
                            sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                            sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                            var text = new Text("DN25排气阀余同", stPt2);
                            sprayOut.Texts.Add(text);
                        }
                    }

                    bool signelBranch = true;//第一个 type 4 标志
                    var cnt = tpts.Count;
                    if (cnt > 2)
                    {
                        ;
                    }
                    bool firstFireAlarmVisited = false;
                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if (!sprayIn.TermPtDic.ContainsKey(tpt))
                        {
                            continue;
                        }

                        var termPt = sprayIn.TermPtDic[tpt];
                        string DN = "DNXXX";
                        foreach (var vpt in sprayIn.TermDnDic.Keys)
                        {
                            if (tpt._pt.DistanceTo(vpt._pt) < 100)//端点管径标注包含当前点
                            {
                                DN = sprayIn.TermDnDic[vpt];
                                break;
                            }
                        }

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
                            if (spraySystem.FlowDIc.Contains(tpt))
                            {
                                hasFlow = true;
                            }
                            var fireDistrict = new FireDistrictRight(firePt, termPt, DN, hasFlow);
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

                                var fireDistrict = new FireDistrictLeft(firePt, termPt1);
                                sprayOut.FireDistrictLefts.Add(fireDistrict);
                            }
                            index++;
                        }
                        if (termPt.Type == 3)//水泵接合器
                        {
                            var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 1300, 0);
                            sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
                            sprayOut.WaterPumps.Add(new WaterPump(pumpPt, termPt.PipeNumber, DN));
                        }
                        if (termPt.Type == 4)
                        {
                            bool needEvade = true;//默认需要躲避
                            var pt1 = new Point3d(stPt4.X, sprayOut.PipeInsertPoint.Y + 400, 0);
                            var pt2 = pt1.OffsetX(650);
                            var pt3 = pt2.OffsetY(1300);
                            double length = GetLength(termPt.PipeNumber) + 100;
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
                                    var rect = GetRect(text2.DbText);//提框
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

        private static Point3dCollection GetRect(DBText dBText)
        {
            double gap = 50;
            var maxPt = dBText.Position.OffsetXY(gap, gap);
            var minPt = dBText.Position.OffsetXY(-gap, -gap);
            if (!dBText.TextString.Equals(""))
            {
                maxPt = dBText.GeometricExtents.MaxPoint.OffsetXY(gap, gap);
                minPt = dBText.GeometricExtents.MinPoint.OffsetXY(-gap, -gap - 400);
            }


            var pts = new Point3d[5];
            pts[0] = new Point3d(minPt.X, maxPt.Y, 0);
            pts[1] = maxPt;
            pts[2] = new Point3d(maxPt.X, minPt.Y, 0);
            pts[3] = minPt;
            pts[4] = pts[0];

            var ptColl = new Point3dCollection(pts);
            return ptColl;
        }

        private static double GetLength(string text)
        {
            if (text.Equals(""))
            {
                return 0;
            }
            var dBText = new Text(text, new Point3d()).DbText;
            var maxPt = dBText.GeometricExtents.MaxPoint;
            var minPt = dBText.GeometricExtents.MinPoint;
            return Math.Abs(maxPt.X - minPt.X);
        }
    }
}
