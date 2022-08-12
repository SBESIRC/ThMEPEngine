using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Hydrant.Engine;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class GetFireHydrantPipe
    {
        private static Point3d GetStpt(bool across, List<Point3dEx> rstPath, FireHydrantSystemOut fireHydrantSysOut, 
            FireHydrantSystemIn fireHydrantSysIn)
        {
            if(across)
            {
                var startFloor = fireHydrantSysIn.StartEndPts[0]._pt.GetFloorInt(fireHydrantSysIn.FloorRect);//起始环管所在层
                var curFloor = rstPath[0]._pt.GetFloorInt(fireHydrantSysIn.FloorRect);//当前主环所在层
                var offsetY = -fireHydrantSysIn.FloorHeight * (curFloor - startFloor -1);
                return new Point3d(fireHydrantSysOut.InsertPoint.X, fireHydrantSysOut.InsertPoint.Y + offsetY, 0);
            }
            else
            {
                return new Point3d(fireHydrantSysOut.InsertPoint.X, fireHydrantSysOut.InsertPoint.Y, 0);
            }
        }
        public static double GetMainLoop(FireHydrantSystemOut fireHydrantSysOut, List<Point3dEx> rstPath,
            FireHydrantSystemIn fireHydrantSysIn, Dictionary<Point3dEx, List<Point3dEx>> branchDic,bool across = false,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic = null)
        {
            var stPt = GetStpt(across, rstPath, fireHydrantSysOut, fireHydrantSysIn);
            var ptStart = new Point3d(stPt.X, stPt.Y, 0);
            double pipeGap = 400;
            double valveWidth = 240;

            for (int i = 0; i < rstPath.Count; i++)
            {
                try
                {
                    var pt = rstPath[i];
                    if(across)
                    {
                        if (i == 0 || i == rstPath.Count - 1)
                        {
                            fireHydrantSysOut.LoopLine.Add(new Line(stPt, stPt.OffsetY(pipeGap)));
                            fireHydrantSysIn.CrossMainPtDic.Add(pt, stPt.OffsetY(pipeGap));
                        }
                    }

                    double pipeLength = 1300;//fireHydrantSysIn.PipeWidth;
                    double upperLen = 0;//走上面的管长，消火栓和无立管末端
                    double lowerLen = 0;//走下面的管长，立管和水泵接合器
                    if (branchDic.ContainsKey(pt))
                    {
                        var tpts = branchDic[pt];
                        for(int j = tpts.Count - 1; j >0; j--)
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[j]))
                            {
                                branchDic[pt]?.Remove(tpts[j]);
                            }
                        }
                        foreach(var tpt in tpts)
                        {
                            if(!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            var term = fireHydrantSysIn.TermPointDic[tpt];
                            var type = term.Type;
                            if (type == 1 || type == 3)
                            {
                                lowerLen += term.PipeWidth;
                            }
                            else
                            {
                                upperLen += term.PipeWidth;
                            }
                        }
                        pipeLength = Math.Max(Math.Max(upperLen, lowerLen), 1600);
                        //if (tpts.Count == 2)
                        //{
                        //    var str1 = "";
                        //    if(fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
                        //    {
                        //        str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                        //    }
                        //    var str2 = "";
                        //    if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[1]))
                        //    {
                        //        str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                        //    }
                        //    if (!str1.IsCurrentFloor() && !str2.IsCurrentFloor())
                        //    {
                        //        pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                        //    }
                        //}
                        //if (tpts.Count == 3)
                        //{
                        //    var str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                        //    var str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                        //    var str3 = fireHydrantSysIn.TermPointDic[tpts[2]].PipeNumber;
                        //    if (!str1.IsCurrentFloor() && !str2.IsCurrentFloor() && !str3.IsCurrentFloor())
                        //    {
                        //        pipeLength = 3 * fireHydrantSysIn.PipeWidth;
                        //    }
                        //    else
                        //    {
                        //        pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                        //    }
                        //}
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = false;
                        stPt = GetPipePart.GetSubLoopPoint(fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        if (ValveDic is not null)
                        {
                            if (ValveDic.ContainsKey(pt))
                            {
                                if (ValveDic[pt].Count >= 3)//
                                {
                                    pipeLength += 200;
                                }
                            }
                            if (branchDic.ContainsKey(pt))
                            {
                                if (branchDic[pt].Count >= 2)
                                {
                                    pipeLength += 200;
                                }
                            }
                        }
                        stPt = GetPipePart.GetBranchPoint(fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("DieValve"))
                    {
                        stPt = GetPipePart.GetDieValvePoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("GateValve"))
                    {
                        stPt = GetPipePart.GetGateValvePoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Casing"))
                    {
                        stPt = GetPipePart.GetCasingPoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                }
                catch(Exception ex)
                {
                    ;
                }
            }
            
            if(!across)
            {
                GetPipePart.GetMainLoopDetial(fireHydrantSysOut, stPt, ptStart);
                fireHydrantSysOut.InsertPoint = ptStart.OffsetY(-fireHydrantSysIn.FloorHeight);
            }

            return stPt.X - fireHydrantSysOut.InsertPoint.X;
        }

        private static Point3d GetStPt(bool across, int index, FireHydrantSystemOut fireHydrantSysOut, 
            FireHydrantSystemIn fireHydrantSysIn, int subPathLsCnt)
        {
            var stPt1 = fireHydrantSysOut.InsertPoint;
            if (across)
            {
                return new Point3d(stPt1.X, stPt1.Y - (fireHydrantSysIn.FloorHeight + 3000) * (index + subPathLsCnt+1) - 3000, 0);
            }
            else
            {
                return new Point3d(stPt1.X, stPt1.Y - (fireHydrantSysIn.FloorHeight + 3000) * index - 3000, 0);
            }
        }
        public static void GetSubLoop(FireHydrantSystemOut fireHydrantSysOut, List<List<Point3dEx>> subPathList, FireHydrantSystemIn fireHydrantSysIn,
            Dictionary<Point3dEx, List<Point3dEx>> branchDic, bool across = false, int subPathLsCnt=0, Dictionary<Point3dEx, List<ValveCasing>> ValveDic = null)
        {
            var index = 0;
            foreach (var rstPath in subPathList)
            {
                var stPt = GetStPt(across, index, fireHydrantSysOut, fireHydrantSysIn, subPathLsCnt);
                index += 1;
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                var pipeGap = 400;
                var valveWidth = 240;

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    if(!fireHydrantSysIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }

                    double pipeLength = 1300;//fireHydrantSysIn.PipeWidth;
                    double upperLen = 0;//走上面的管长，消火栓和无立管末端
                    double lowerLen = 0;//走下面的管长，立管和水泵接合器
                    if (branchDic.ContainsKey(pt))
                    {
                        var tpts = branchDic[pt];
                        for (int j = tpts.Count - 1; j > 0; j--)
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[j]))
                            {
                                branchDic[pt]?.Remove(tpts[j]);
                            }
                        }
                        foreach (var tpt in tpts)
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            var term = fireHydrantSysIn.TermPointDic[tpt];
                            var type = term.Type;
                            if (type == 1 || type == 3)
                            {
                                lowerLen += term.PipeWidth;
                            }
                            else
                            {
                                upperLen += term.PipeWidth;
                            }
                        }
                        pipeLength = Math.Max(Math.Max(upperLen, lowerLen), 1600);
                    }

                    if (branchDic.ContainsKey(pt))
                    {
                        if (branchDic[pt].Count == 3)
                        {
                            pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                        }
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = true;
                        stPt = GetPipePart.GetSubLoopPoint(fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        if (ValveDic is not null)
                        {
                            if (ValveDic.ContainsKey(pt))
                            {
                                if (ValveDic[pt].Count >= 3)//
                                {
                                    pipeLength += 200;
                                }
                            }
                            if (branchDic.ContainsKey(pt))
                            {
                                if (branchDic[pt].Count >= 2)
                                {
                                    pipeLength += 200;
                                }
                            }
                        }
                        stPt = GetPipePart.GetBranchPoint(fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("DieValve"))
                    {
                        stPt = GetPipePart.GetDieValvePoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("GateValve"))
                    {
                        stPt = GetPipePart.GetGateValvePoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Casing"))
                    {
                        stPt = GetPipePart.GetCasingPoint(fireHydrantSysOut, stPt);
                        continue;
                    }
                }
                GetPipePart.GetSubLoopDetial(ref fireHydrantSysOut, stPt, ptStart, rstPath, fireHydrantSysIn.MarkList);
            }
        }

        public static void GetBranch(FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<Point3dEx>> branchDic,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var list = new List<Point3d>();
            foreach (var pt in fireHydrantSysIn.TermPointDic.Keys)
            {
                list.Add(pt._pt);
            }
            foreach (var pt in branchDic.Keys)//对于支路的每一个起始点
            {
                try
                {
                    int pipeLength = 800;
                    if (ValveDic is not null)
                    {
                        if (ValveDic.ContainsKey(pt))
                        {
                            if (ValveDic[pt].Count >= 3)//
                            {
                                pipeLength += 200;
                            }
                        }
                        if (branchDic.ContainsKey(pt))
                        {
                            if (branchDic[pt].Count >= 2)
                            {
                                pipeLength += 200;
                            }
                        }
                    }
                    if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                    {
                        continue;
                    }

                    if (branchDic[pt].Count == 1)//单支路
                    {
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                        var x = branchDic[pt];

                        if (fireHydrantSysIn.TermPointDic.ContainsKey(branchDic[pt][0]))
                        {
                            var type = fireHydrantSysIn.TermPointDic[branchDic[pt].First()].Type;
                            if (type.Equals(1))//终点是类型1，消火栓
                            {
                                string pipeNumber = fireHydrantSysIn.TermPointDic[branchDic[pt][0]].PipeNumber;//立管标号
                                if (pipeNumber.IsCurrentFloor())//消火栓
                                {
                                    GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn, pipeLength);
                                }
                            }
                            if (type.Equals(2))//终点是类型2，立管
                            {
                                GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn, pipeLength);
                            }
                            if (type.Equals(3))//终点是类型3，无立管
                            {
                                GetBranchType6(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn, pipeLength);
                            }
                            if (type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                            {
                                GetBranchType5(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn, pipeLength);
                            }
                            if(type.Equals(5))//终点是类型5，跨层主环点
                            {
                                GetBranchAcross(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                        }
                    }
                    if (branchDic[pt].Count == 2)//两个支路
                    {
                        int type1Nums = 0;
                        foreach (var tpt in branchDic[pt])
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                            {
                                type1Nums += 1;
                            }
                        }
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                        if (type1Nums == 1)
                        {
                            if (branchDic[pt].Count != 0)
                            {
                                GetBranchType3(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn, pipeLength);
                            }
                        }
                        else if(type1Nums==2)
                        {
                            GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn, pipeLength);
                            GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][1], ValveDic, fireHydrantSysIn, pipeLength+1250);

                        }
                        else
                        {
                            GetBranchType02(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn, pipeLength);
                        }
                    }
                    if (branchDic[pt].Count == 3)//三个支路
                    {
                        if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];

                        int type1Nums = 0;
                        foreach (var tpt in branchDic[pt])
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                            {
                                type1Nums += 1;
                            }
                        }
                        if (type1Nums == 0)
                        {
                            GetBranchType03(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                        }
                        if (type1Nums == 1)
                        {
                            GetBranchType12(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn, pipeLength);
                        }
                        if (type1Nums == 2)
                        {
                            GetBranchType21(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn, pipeLength);
                        }
                        if (type1Nums == 3)
                        {
                            continue;
                        }
                    }
                }
                catch(Exception ex)
                {
                    ;
                }

            }
        }

        public static void GetBranch(ref FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<Point3dEx>> branchDic,
               Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            int pipeLength = 800;
            var valveDic = new Dictionary<Point3dEx, List<ValveCasing>>();
            var list = new List<Point3d>();
            foreach (var pt in fireHydrantSysIn.TermPointDic.Keys)
            {
                list.Add(pt._pt);
            }
            foreach (var pt in branchDic.Keys)//对于支路的每一个起始点
            {
                try
                {
                    if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                    {
                        continue;
                    }

                    if (branchDic[pt].Count == 1)//单支路
                    {
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                        var x = branchDic[pt];

                        if (fireHydrantSysIn.TermPointDic.ContainsKey(branchDic[pt][0]))
                        {
                            var type = fireHydrantSysIn.TermPointDic[branchDic[pt].First()].Type;
                            if (type.Equals(1))//终点是类型1，消火栓
                            {
                                string pipeNumber = fireHydrantSysIn.TermPointDic[branchDic[pt][0]].PipeNumber;//立管标号
                                if (pipeNumber.IsCurrentFloor())//消火栓
                                {
                                    GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn,pipeLength);
                                }
                            }
                            if (type.Equals(2))//终点是类型2，其他区域
                            {
                                GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);
                            }
                            if (type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                            {
                                GetBranchType4(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);
                            }
                            if (type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                            {
                                GetBranchType5(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);
                            }
                            if (type.Equals(5))//终点是类型5，跨层主环点
                            {
                                GetBranchAcross(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn);
                            }
                            if (type.Equals(6))
                            {
                                GetBranchType6(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);
                            }
                        }
                        else
                        {
                            var vpt = new Point3dEx();
                            foreach (var tpt in fireHydrantSysIn.VerticalPosition)//每个圈圈的中心点
                            {
                                if (tpt._pt.DistanceTo(branchDic[pt][0]._pt) < 150)
                                {
                                    vpt = tpt;
                                    break;
                                }
                            }
                            if (fireHydrantSysIn.TermPointDic.ContainsKey(vpt))
                            {
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(1))//终点是类型1，消火栓
                                {
                                    string pipeNumber = fireHydrantSysIn.TermPointDic[vpt].PipeNumber;//立管标号
                                    if (pipeNumber.IsCurrentFloor())//消火栓
                                    {
                                        GetBranchType1(pt, ref fireHydrantSysOut, stPt, vpt, valveDic, fireHydrantSysIn, pipeLength);
                                    }
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(2))//终点是类型2，其他区域
                                {
                                    GetBranchType2(pt, ref fireHydrantSysOut, stPt, vpt, valveDic, fireHydrantSysIn, pipeLength);
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                                {
                                    GetBranchType4(pt, ref fireHydrantSysOut, stPt, vpt, valveDic, fireHydrantSysIn, pipeLength);
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                                {
                                    GetBranchType5(pt, ref fireHydrantSysOut, stPt, vpt, valveDic, fireHydrantSysIn, pipeLength);
                                }
                            }
                            else
                            {
                                var verticalHasHydrant = fireHydrantSysIn.VerticalHasHydrant.Contains(branchDic[pt][0]);
                                if (verticalHasHydrant)
                                {
                                    GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);

                                }
                                else
                                {
                                    GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], valveDic, fireHydrantSysIn, pipeLength);
                                }
                            }

                        }
                    }
                    if (branchDic[pt].Count == 2)//两个支路
                    {
                        int type1Nums = 0;
                        foreach (var tpt in branchDic[pt])
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                            {
                                type1Nums += 1;
                            }
                        }
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                        if (type1Nums == 1)
                        {
                            if (branchDic[pt].Count != 0)
                            {
                                GetBranchType3(pt, ref fireHydrantSysOut, stPt, branchDic[pt], valveDic, fireHydrantSysIn, pipeLength);
                            }
                        }
                        else
                        {
                            GetBranchType02(pt, ref fireHydrantSysOut, stPt, branchDic[pt], valveDic, fireHydrantSysIn, pipeLength);
                        }
                    }
                    if (branchDic[pt].Count == 3)//三个支路
                    {
                        if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        var stPt = fireHydrantSysOut.BranchDrawDic[pt];

                        int type1Nums = 0;
                        foreach (var tpt in branchDic[pt])
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                            {
                                type1Nums += 1;
                            }
                        }
                        if (type1Nums == 0)
                        {
                            GetBranchType03(pt, ref fireHydrantSysOut, stPt, branchDic[pt], valveDic, fireHydrantSysIn);
                        }
                        if (type1Nums == 1)
                        {
                            GetBranchType12(pt, ref fireHydrantSysOut, stPt, branchDic[pt], valveDic, fireHydrantSysIn, pipeLength);
                        }
                        if (type1Nums == 2)
                        {
                            GetBranchType21(pt, ref fireHydrantSysOut, stPt, branchDic[pt], valveDic, fireHydrantSysIn, pipeLength);
                        }
                        if (type1Nums == 3)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ;
                }

            }
        }


        /// <summary>
        /// 绘制向下的单分支
        /// </summary>
        private static void GetBranchType1(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt,
            Point3dEx tpt, Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn,int pipeLength, bool flag3 = false)
        {
            var pt1 = stpt._pt;
            var pt4 = pt1.OffsetX(pipeLength);
            double floorHeight = fireHydrantSysIn.FloorHeight;
            var pt5 = pt4.OffsetY(-floorHeight * 0.58);
            var pt6 = pt5.OffsetX(-800);
            var lineList = new List<Line>
            {
                new Line(pt4, pt5),
                new Line(pt5, pt6)
            };
            TextGet.GetText(tpt, fireHydrantSysIn, ref fireHydrantSysOut, pt4, pt6);
            fireHydrantSysOut.AidLines.Add(new Line(pt6, tpt._pt));
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, lineList, fireHydrantSysOut, pt1, pt4, flag3);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
        }

        /// <summary>
        /// 绘制向上的单分支
        /// </summary>
        private static void GetBranchType2(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength, double type = 2)
        {
            double floorHeight = fireHydrantSysIn.FloorHeight;

            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber2;//立管标号
                textWidth = fireHydrantSysIn.TermPointDic[tpt].TextWidth;
            }

            var pt1 = stpt._pt;
            double pipeWidth = pipeLength;
            if (type == 3)
            {
                pipeWidth = pipeLength -200;
            }
            var pt4 = pt1.OffsetX(pipeWidth);
            var pt5 = pt1.OffsetXY(pipeWidth, floorHeight * 0.4);
            var pt6 = pt1.OffsetXY(pipeWidth, floorHeight * 0.5);

            fireHydrantSysOut.AidLines.Add(new Line(pt5, tpt._pt));
            LoopLine.Split(fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            if (type == 2)
            {
                ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn,  lineList,  fireHydrantSysOut, pt1, pt4);
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI * 3 / 2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(textWidth - 50));
            var p2Flag = false;

            if (pipeNumber1 is not null)
            {
                var text = ThTextSet.ThText(pt6, pipeNumber1.Trim());
                if (!pipeNumber1.Trim().Equals(""))
                {
                    fireHydrantSysOut.TextLine.Add(textLine1);
                    fireHydrantSysOut.TextList.Add(text);
                    if (!pipeNumber12?.Equals("") == true)
                    {
                        text = ThTextSet.ThText(new Point3d(pt6.X, pt6.Y - 400, 0), pipeNumber12.Trim());
                        double textLength = text.GeometricExtents.MaxPoint.X - text.GeometricExtents.MinPoint.X;
                        if (textLength > textWidth)
                        {
                            var textLine3 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(textLength));
                            fireHydrantSysOut.TextLine.Add(textLine3);
                            p2Flag = true;

                        }

                        fireHydrantSysOut.TextList.Add(text);
                    }
                    if (!p2Flag)
                    {
                        fireHydrantSysOut.TextLine.Add(textLine2);
                    }
                }
            }

            var strDN = "DN100";
            if (fireHydrantSysIn.TermDnDic.ContainsKey(tpt))
            {
                strDN = fireHydrantSysIn.TermDnDic[tpt];
            }
            var DN1 = ThTextSet.ThText(new Point3d(pt5.X + 350, pt4.Y + floorHeight * 0.2, 0), Math.PI / 2, strDN);
            fireHydrantSysOut.DNList.Add(DN1);
        }

        /// <summary>
        /// 绘制双分支
        /// </summary>
        private static void GetBranchType3(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;//立管标号 1
            }
            if (pipeNumber1?.Equals("") == true)
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, pipeLength, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, pipeLength, 3);
                return;
            }
            if (pipeNumber1.IsCurrentFloor())
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, pipeLength, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, pipeLength, 3);
            }
            else
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, pipeLength, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, pipeLength, 3);
            }
        }

        /// <summary>
        /// 绘制共用支管的双分支
        /// </summary>
        private static void GetBranchType4(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn, pipeLength);
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn, pipeLength,4);
        }

        /// <summary>
        /// 绘制水泵接合器
        /// </summary>
        private static void GetBranchType5(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            double XGap = pipeLength;
            var floorHeight = fireHydrantSysIn.FloorHeight;
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
            }
            var pt1 = stpt._pt;
            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y + floorHeight * 0.25, 0);
            var pt51 = new Point3d(pt5.X + 900, pt5.Y, 0);
            var pt6 = new Point3d(pt51.X, pt4.Y + floorHeight * 0.5 + 800, 0);
            var pt7 = new Point3d(pt6.X + floorHeight * 0.3500, pt6.Y, 0);
            fireHydrantSysOut.AidLines.Add(new Line(pt51, tpt._pt));
            LoopLine.Split(fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt51));
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, lineList, fireHydrantSysOut, pt1, pt4);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt51, 0);
            var textLine1 = ThTextSet.ThTextLine(pt51, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt7);


            var text = ThTextSet.ThText(pt6, pipeNumber1);
            var text1 = ThTextSet.ThText(new Point3d(pt6.X, pt6.Y - 500, 0), "详见总图");
            if (!pipeNumber1.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber1.Contains("详见总图"))
                {
                    fireHydrantSysOut.TextList.Add(text1);
                }
            }
            var DN1 = ThTextSet.ThText(new Point3d(pt1.X, pt1.Y - 500, 0), 0, "DN100");
            fireHydrantSysOut.DNList.Add(DN1);
        }

        /// <summary>
        /// 绘制向下的单分支
        /// </summary>
        private static void GetBranchType6(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength, double type = 2)
        {
            double floorHeight = fireHydrantSysIn.FloorHeight;

            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                textWidth = fireHydrantSysIn.TermPointDic[tpt].TextWidth;
            }

            var pt1 = stpt._pt;
            double pipeWidth = pipeLength;
            var pt4 = pt1.OffsetX(pipeWidth);
            var pt5 = pt1.OffsetXY(pipeWidth, -floorHeight * 0.1);
            var pt6 = pt1.OffsetXY(pipeWidth, -floorHeight * 0.2);
            fireHydrantSysOut.AidLines.Add(new Line(pt5, tpt._pt));
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            if (type == 2)
            {
                ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, lineList, fireHydrantSysOut, pt1, pt4);
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI/2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(-textWidth - 50));

            if (pipeNumber1 is not null)
            {
                var text = ThTextSet.ThText(pt6.OffsetX(-textWidth), pipeNumber1.Trim());
                if (!pipeNumber1.Trim().Equals(""))
                {
                    fireHydrantSysOut.TextLine.Add(textLine1);
                    fireHydrantSysOut.TextLine.Add(textLine2);
                    fireHydrantSysOut.TextList.Add(text);
                }
            }
        }

        /// <summary>
        /// 绘制跨层主环
        /// </summary>
        private static void GetBranchAcross(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt,
            Point3dEx tpt, Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, bool flag3 = false)
        {
            var cpt = new Point3dEx();//跨层主环起始终止点
            foreach(var pt in fireHydrantSysIn.PtDic[branchPt])
            {
                if(fireHydrantSysIn.ThroughPt.Contains(pt))
                {
                    cpt = new Point3dEx(pt._pt);
                }
            }
            var drawingPt = fireHydrantSysIn.CrossMainPtDic[cpt];//跨层主环绘制点
            var pt1 = stpt._pt;//上环点

            var pt2 = new Point3d(pt1.X, drawingPt.Y, 0);

            var lineList = new List<Line>
            {
                new Line(pt1, pt2),
                new Line(pt2, drawingPt)
            };
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
        }

        /// <summary>
        /// 绘制2根立管
        /// </summary>
        private static void GetBranchType02(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            var pt1 = tpts[0];
            var pt2 = tpts[1];
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber2;//立管标号
            }
            if (pipeNumber12?.Equals("") == false)
            {
                pt1 = tpts[1];
                pt2 = tpts[0];
            }
            var stpt1 = new Point3dEx();
            if (fireHydrantSysIn.TermPointDic.ContainsKey(pt1))
            {
                {
                    double XGap = 1600;
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);
                    var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
                    stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
                    fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
                }
            }
            else
            {
                stpt1 = stpt;
            }
            if (fireHydrantSysIn.TermPointDic.ContainsKey(pt2))
            {
                {
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, pt2, new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);

                }
            }
        }

        /// <summary>
        /// 绘制两根供水管，一根立管
        /// </summary>
        private static void GetBranchType21(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            var newPt = new Point3dEx(0, 0, 0);
            foreach (var pt in tpts)
            {
                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt))
                {
                    continue;
                }
                if (fireHydrantSysIn.TermPointDic[pt].Type.Equals(1))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
            }
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn,  pipeLength);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);
        }

        /// <summary>
        /// 绘制三根立管
        /// </summary>
        private static void GetBranchType03(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            int pipeLength = 800;
            var ptls = new List<Point3dEx>(tpts);
            var pt1 = new Point3dEx(0, 0, 0);
            foreach (var pt in tpts)
            {
                if (fireHydrantSysIn.TermPointDic[pt].PipeNumber?.Contains("水泵接合器") == true)
                {
                    pt1 = new Point3dEx(pt._pt);
                    ptls.Remove(pt1);
                }
            }
            GetBranchType5(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[0], new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);
            pt4 = new Point3dEx(stpt1._pt.X + 800, stpt1._pt.Y, 0);
            stpt1 = new Point3dEx(stpt1._pt.X + XGap, stpt1._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[1], new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn,  pipeLength);
        }


        /// <summary>
        /// 绘制一根供水管，两根立管
        /// </summary>
        private static void GetBranchType12(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, int pipeLength)
        {
            var newPt = new Point3dEx(0, 0, 0);
            foreach (var pt in tpts)
            {
                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
                if (fireHydrantSysIn.TermPointDic[pt].Type.Equals(2))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
            }
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn, pipeLength);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<ValveCasing>>(), fireHydrantSysIn, pipeLength);
        }
    }
}
