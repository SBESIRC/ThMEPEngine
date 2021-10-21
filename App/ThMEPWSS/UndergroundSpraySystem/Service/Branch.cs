using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class Branch
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double branchNums = 0;
            int throughIndex = 0;
            int index = 0;
            var lastFirePt = new Point3d();
            foreach (var pt in spraySystem.BranchDic.Keys)//pt 支路起始点
            {
                try
                {
                    double fireNums = 0;
                    
                    if (spraySystem.SubLoopFireAreasDic.ContainsKey(pt))
                    {
                        fireNums = spraySystem.SubLoopFireAreasDic[pt][0];
                    }
                    
                    if(!spraySystem.BranchPtDic.ContainsKey(pt))//支管图纸绘制起始点不存在
                    {
                        continue;//跳过这个点
                    }
                    var stPt = spraySystem.BranchPtDic[pt];//图纸绘制起始点
                    if(!spraySystem.BranchDic.ContainsKey(pt))//支路列表没有这个点
                    {
                        continue;//跳过这个点
                    }
                    var tpts = spraySystem.BranchDic[pt];
                    bool hasAutoValve = true;
                    foreach(var tpt in tpts)
                    {
                        if(!sprayIn.TermPtDic.ContainsKey(tpt))
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
                    }

                    foreach (var tpt in tpts)// tpt 支路端点
                    {
                        if(tpt._pt.DistanceTo(new Point3d(18233832.6440811, 21207404.319614, 0)) < 10)
                        {
                            ;
                        }
                        try
                        {
                            if (!sprayIn.TermPtDic.ContainsKey(tpt))
                            {
                                continue;
                            }

                            var termPt = sprayIn.TermPtDic[tpt];
                            
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
                                var fireDistrict = new FireDistrictRight(firePt, termPt);
                                sprayOut.FireDistricts.Add(fireDistrict);
                                sprayOut.PipeLine.Add(new Line(stPt, new Point3d(firePt.X, stPt.Y, 0)));
                                sprayOut.PipeLine.Add(new Line(firePt, new Point3d(firePt.X, stPt.Y, 0)));
                                branchNums++;
                                lastFirePt = new Point3d(fireStpt.X, fireStpt.Y, 0);
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
                                    var firePt = fireStpt.OffsetXY(-throughIndex * 5500 - 1500, -sprayIn.FloorHeight);
                                    var pt1 = new Point3d(fireStpt.X - 500 * index, stPt.Y, 0);
                                    var pt2 = new Point3d(pt1.X, firePt.Y + 0.06 * sprayIn.FloorHeight * (index + 1), 0);
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
                                var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 0.13 * sprayIn.FloorHeight, 0);
                                sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
                                sprayOut.WaterPumps.Add(new WaterPump(pumpPt));
                            }
                            if (termPt.Type == 4)
                            {
                                bool needEvade = true;//默认需要躲避
                                var pt1 = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 0.06 * sprayIn.FloorHeight, 0);
                                var pt2 = pt1.OffsetX(650);
                                var pt3 = pt2.OffsetY(0.13 * sprayIn.FloorHeight);
                                double length = GetLength(termPt.PipeNumber) + 100;
                                double stepSize = 450;
                                int indx = 0;
                                while (needEvade)
                                {
                                    var textPt = pt3.OffsetXY(-length, 0.09 * sprayIn.FloorHeight + stepSize * indx);
                                    var text2 = new Text(termPt.PipeNumber, textPt);
                                    var texts = new List<DBText>() { text2.DbText };

                                    if (spraySystem.BlockExtents.SelectAll().Count == 0)//外包框数目为0
                                    {
                                        spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                                        break;
                                    }
                                    else
                                    {
                                        var rect = GetRect(text2.DbText);//提框
                                        var dbObjs = spraySystem.BlockExtents.SelectCrossingPolygon(rect);
                                        if (dbObjs.Count == 0)
                                        {
                                            spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                                            break;
                                        }
                                    }
                                    indx++;
                                }
                                var pt4 = pt3.OffsetY(0.09 * sprayIn.FloorHeight + stepSize * indx);
                                var pt5 = pt4.OffsetX(-length);
                                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                                sprayOut.PipeLine.Add(new Line(pt2, pt3));
                                sprayOut.NoteLine.Add(new Line(pt3, pt4));
                                sprayOut.NoteLine.Add(new Line(pt4, pt5));
                                sprayOut.SprayBlocks.Add(new SprayBlock("水管中断", pt3, Math.PI / 2));
                                var text = new Text(termPt.PipeNumber, pt5);
                                sprayOut.Texts.Add(text);
                            }
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                        
                    }
                }

                catch(Exception ex)
                {
                    ;
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
                minPt = dBText.GeometricExtents.MinPoint.OffsetXY(-gap, -gap);
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
            if(text.Equals(""))
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

