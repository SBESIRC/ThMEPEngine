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
        public static double GetMainLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<Point3dEx> rstPath,
            FireHydrantSystemIn fireHydrantSysIn, Dictionary<Point3dEx, List<Point3dEx>> branchDic,bool across = false)
        {
            var stPt = GetStpt(across, rstPath, fireHydrantSysOut, fireHydrantSysIn);
            var ptStart = new Point3d(stPt.X, stPt.Y, 0);
            double pipeGap = 400;
            double valveWidth = 240;
            bool valveCheck = true;

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
         
                    double pipeLength = fireHydrantSysIn.PipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        var tpts = branchDic[pt];
                        if (tpts.Count == 2)
                        {
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
                            {
                                continue;
                            }
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[1]))
                            {
                                continue;
                            }
                            var str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                            var str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                            if (!str1.IsCurrentFloor() && !str2.IsCurrentFloor())
                            {
                                pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                            }
                        }
                        if (tpts.Count == 3)
                        {
                            var str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                            var str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                            var str3 = fireHydrantSysIn.TermPointDic[tpts[2]].PipeNumber;
                            if (!str1.IsCurrentFloor() && !str2.IsCurrentFloor() && !str3.IsCurrentFloor())
                            {
                                pipeLength = 3 * fireHydrantSysIn.PipeWidth;
                            }
                            else
                            {
                                pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                            }
                        }
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = false;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(pt, ref fireHydrantSysOut, stPt, ref valveCheck, fireHydrantSysIn);
                        continue;
                    }
                }
                catch
                {
                    ;
                }
            }

            
            if(!across)
            {
                GetPipePart.GetMainLoopDetial(ref fireHydrantSysOut, stPt, ptStart);
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
        public static void GetSubLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<List<Point3dEx>> subPathList, FireHydrantSystemIn fireHydrantSysIn,
            Dictionary<Point3dEx, List<Point3dEx>> branchDic, bool across = false, int subPathLsCnt=0)
        {
            var index = 0;
            foreach (var rstPath in subPathList)
            {
                var stPt = GetStPt(across, index, fireHydrantSysOut, fireHydrantSysIn, subPathLsCnt);
                index += 1;
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                var pipeGap = 400;
                var valveWidth = 240;
                var valveCheck = true;

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    if(!fireHydrantSysIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }
                    double pipeLength = fireHydrantSysIn.PipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        if (branchDic[pt].Count == 3)
                        {
                            pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                        }
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = true;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(pt, ref fireHydrantSysOut, stPt, ref valveCheck, fireHydrantSysIn);
                        continue;
                    }
                }
                GetPipePart.GetSubLoopDetial(ref fireHydrantSysOut, stPt, ptStart, rstPath, fireHydrantSysIn.MarkList);
            }
        }

        public static void GetBranch(ref FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<Point3dEx>> branchDic,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
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
                    if(pt.DistanceToEx(new Point3dEx(1495711.3, 896071, 0))<10)
                    {
                        ;
                    }
                    if (pt.DistanceToEx(new Point3dEx(1496511.5, 896071, 0)) < 10)
                    {
                        ;
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
                                    GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                                }
                            }
                            if (type.Equals(2))//终点是类型2，其他区域
                            {
                                GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                            if (type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                            {
                                GetBranchType4(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                            if (type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                            {
                                GetBranchType5(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                            if(type.Equals(5))//终点是类型5，跨层主环点
                            {
                                GetBranchAcross(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                        }
                        else
                        {
                            ;
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
                                        GetBranchType1(pt, ref fireHydrantSysOut, stPt, vpt, ValveDic, fireHydrantSysIn);
                                    }
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(2))//终点是类型2，其他区域
                                {
                                    GetBranchType2(pt, ref fireHydrantSysOut, stPt, vpt, ValveDic, fireHydrantSysIn);
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                                {
                                    GetBranchType4(pt, ref fireHydrantSysOut, stPt, vpt, ValveDic, fireHydrantSysIn);
                                }
                                if (fireHydrantSysIn.TermPointDic[vpt].Type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                                {
                                    GetBranchType5(pt, ref fireHydrantSysOut, stPt, vpt, ValveDic, fireHydrantSysIn);
                                }
                            }
                            else
                            {
                                var verticalHasHydrant = fireHydrantSysIn.VerticalHasHydrant.Contains(branchDic[pt][0]);
                                if (verticalHasHydrant)
                                {
                                    GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);

                                }
                                else
                                {
                                    GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);

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
                                GetBranchType3(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                            }
                        }
                        else
                        {
                            GetBranchType02(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
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
                            GetBranchType12(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                        }
                        if (type1Nums == 2)
                        {
                            GetBranchType21(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                        }
                        if (type1Nums == 3)
                        {
                            continue;
                        }
                    }
                }
                catch
                {
                    ;
                }

            }
        }


        public static void DrawPipeLabels(FireHydrantSystemIn fireHydrantSysIn, FireHydrantSystemOut fireHydrantSysOut)
        {
            var angleStep = 15;
            var radiusStep = 200;
            var radiusStart = 800;
            var radiusEnd = 5000;
            using (var adb = AcadDatabase.Active())
            {
                var results = adb.ModelSpace.OfType<Entity>().ToCollection();
                var thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(results);
                foreach (var pt in fireHydrantSysIn.TermPointDic.Values)
                {
                    var center = pt.PtEx._pt;
                    {
                        var radius = 800;
                        var angle = 30.0.AngleFromDegree();
                        var e = new MLeader { MText = new MText() { Contents = pt.PipeNumber, TextHeight = 100, ColorIndex = 40, } };
                        e.SetLastVertex(e.AddLeaderLine(center), center.OffsetXY(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                        adb.ModelSpace.Add(e);
                    }
                    var ok = false;
                    for (double angleDegree = 0; angleDegree < 360; angleDegree += angleStep)
                    {
                        var angle = angleDegree.AngleFromDegree();
                        for (double radius = radiusStart; radius <= radiusEnd; radius += radiusStep)
                        {
                            var areaObjs = thCADCoreNTSSpatialIndex.SelectCrossingWindow(center.OffsetXY(-radius, -radius), center.OffsetXY(radius, radius));
                            var areaObjsSpatialIndex = new ThCADCoreNTSSpatialIndex(areaObjs);
                            var e = new MLeader { MText = new MText() { Contents = pt.PipeNumber, TextHeight = 100, ColorIndex = 40, } };
                            e.SetLastVertex(e.AddLeaderLine(center), center.OffsetXY(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                            var rets = areaObjsSpatialIndex.SelectCrossingPolygon(e);
                            if (rets.Count == 0)//没撞到别的东西
                            {
                                adb.ModelSpace.Add(e);
                                ok = true;
                                break;
                            }
                        }
                        if (ok) break;
                    }
                    if (!ok)//躲不开了，就这样吧
                    {
                        var radius = 800;
                        var angle = 30.0.AngleFromDegree();
                        var e = new MLeader { MText = new MText() { Contents = pt.PipeNumber, TextHeight = 100, ColorIndex = 40, } };
                        e.SetLastVertex(e.AddLeaderLine(center), center.OffsetXY(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                        adb.ModelSpace.Add(e);
                    }
                }
            }
        }

        private static void GetFireHydrantPipeTextElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            RecogniseFireHydrantPipeLines(selectArea, adb);
            RecogniseFireHydrantPipeElements(selectArea, adb);
            GetOtherPipeLineList(selectArea, results);
        }

        private static void GetFireHydrantPipeMarkElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            RecogniseFireHydrantLabelTexts(selectArea, adb);
            ExtractFireHydrantMarks(adb, results);
            RecogniseFireHydrantMarks(selectArea, adb);
            ExtractFireEngines(adb, results);
            ExtractFireEnginePipes(adb, results);
        }

        private static void GetFireHydrantPipeLableElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            RecogniseFireHydrantButterValves(selectArea, adb);
            ExtractFireHydrantButterValves(adb, results);
            ExtractFires(adb, results);
            RecogniseLabelLines(selectArea, adb);
            ExtractFireLabelLines(adb, results);
            ExtractFireHydrantLabelTexts(adb, results);
        }

        private static void GetFireHydrantPipeLineElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            RecogniseFirePipes(selectArea, adb);
            ExtractFirePipes(adb, results);
            RecogniseFirePipeLines(selectArea, adb);
            ExtractFirePipeLines(adb, results);
        }

        private static void GetFireHydrantPipeElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            RecogniseFireHydrants(selectArea, adb);
            ExtractFireHydrants(adb, results);
            RecogniseFireHydrantValves(selectArea, adb);
            ExtractFireHydrantValves(adb, results);
        }

        private static void GetOtherPipeLineList(Point3dCollection selectArea, DBObjectCollection results)
        {
            //接黄工的管线识别
            new HydrantConnectPipe.Service.ThOtherPipeLineService().GetOtherPipeLineList(selectArea).ForEach(o => results.Add(o));
        }

        private static void RecogniseFireHydrantPipeElements(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantPipeRecognitionEngine = new ThFireHydrantPipeRecognitionEngine();
            fireHydrantPipeRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void RecogniseFireHydrantPipeLines(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantPipeLineRecognitionEngine = new ThFireHydrantPipeLineRecognitionEngine();
            fireHydrantPipeLineRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireEnginePipes(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantPipeLineEngine = new ThFireHydrantPipeLineEngine();
            fireHydrantPipeLineEngine.Extract(adb.Database);
            fireHydrantPipeLineEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void ExtractFireEngines(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantPipeEngine = new ThFireHydrantPipeEngine();
            fireHydrantPipeEngine.Extract(adb.Database);
            fireHydrantPipeEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseFireHydrantMarks(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantMarkRecognitionEngine = new ThFireHydrantMarkRecognitionEngine();
            fireHydrantMarkRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireHydrantMarks(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantMarkEngine = new ThFireHydrantMarkEngine();
            fireHydrantMarkEngine.Extract(adb.Database);
            fireHydrantMarkEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseFireHydrantLabelTexts(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantLabelTextRecognitionEngine = new ThFireHydrantLabelTextRecognitionEngine();
            fireHydrantLabelTextRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireHydrantLabelTexts(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantLabelTextEngine = new ThFireHydrantLabelTextEngine();
            fireHydrantLabelTextEngine.Extract(adb.Database);
            fireHydrantLabelTextEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseLabelLines(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantLabelLineRecognitionEngine = new ThFireHydrantLabelLineRecognitionEngine();
            fireHydrantLabelLineRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireLabelLines(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantLabelLineEngine = new ThFireHydrantLabelLineEngine();
            fireHydrantLabelLineEngine.Extract(adb.Database);
            fireHydrantLabelLineEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void ExtractFires(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantEngine = new ThFireHydrantEngine();
            fireHydrantEngine.Extract(adb.Database);
            fireHydrantEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseFireHydrantButterValves(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantButterValveRecognitionEngine = new ThFireHydrantButterValveRecognitionEngine();
            fireHydrantButterValveRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireHydrantButterValves(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantButterValveEngine = new ThFireHydrantButterValveEngine();
            fireHydrantButterValveEngine.Extract(adb.Database);
            fireHydrantButterValveEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseFirePipes(Point3dCollection selectArea, AcadDatabase adb)
        {
            var firePipeRecognitionEngine = new ThFirePipeRecognitionEngine();
            firePipeRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void RecogniseFirePipeLines(Point3dCollection selectArea, AcadDatabase adb)
        {
            var firePipeLineRecognitionEngine = new ThFirePipeLineRecognitionEngine();
            firePipeLineRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFirePipeLines(AcadDatabase adb, DBObjectCollection results)
        {
            var firePipeLineEngine = new ThFirePipeLineEngine();
            firePipeLineEngine.Extract(adb.Database);
            firePipeLineEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void ExtractFirePipes(AcadDatabase adb, DBObjectCollection results)
        {
            var firePipeEngine = new ThFirePipeEngine();
            firePipeEngine.Extract(adb.Database);
            firePipeEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void RecogniseFireHydrantValves(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantValveRecognitionEngine = new ThFireHydrantValveRecognitionEngine();
            fireHydrantValveRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void RecogniseFireHydrants(Point3dCollection selectArea, AcadDatabase adb)
        {
            var fireHydrantValveGroupRecognitionEngine = new ThFireHydrantValveGroupRecognitionEngine();
            fireHydrantValveGroupRecognitionEngine.Recognize(adb.Database, selectArea);
        }

        private static void ExtractFireHydrantValves(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantValveGroupEngine = new ThFireHydrantValveGroupEngine();
            fireHydrantValveGroupEngine.Extract(adb.Database);
            fireHydrantValveGroupEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        private static void ExtractFireHydrants(AcadDatabase adb, DBObjectCollection results)
        {
            var fireHydrantValveEngine = new ThFireHydrantValveEngine();
            fireHydrantValveEngine.Extract(adb.Database);
            fireHydrantValveEngine.Results.ForEach(o => results.Add(o.Geometry));
        }

        /// <summary>
        /// 绘制向下的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType1(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt,
            Point3dEx tpt, Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, bool flag3 = false)
        {
            var pt1 = stpt._pt;
            var pt4 = pt1.OffsetX(800);
            double floorHeight = fireHydrantSysIn.FloorHeight;
            var pt5 = pt4.OffsetY(-floorHeight * 0.58);
            var pt6 = pt1.OffsetY(-floorHeight * 0.58);
            var lineList = new List<Line>
            {
                new Line(pt4, pt5),
                new Line(pt5, pt6)
            };
            TextGet.GetText(tpt, fireHydrantSysIn, ref fireHydrantSysOut, pt4, pt6);
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4, flag3);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
        }

        /// <summary>
        /// 绘制向上的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType2(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, double type = 2)
        {
            double floorHeight = fireHydrantSysIn.FloorHeight;

            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber2;//立管标号
            }

            var pt1 = stpt._pt;
            double pipeWidth = 800;
            if (type == 3)
            {
                pipeWidth = 600;
            }
            var pt4 = pt1.OffsetX(pipeWidth);
            var pt5 = pt1.OffsetXY(pipeWidth, floorHeight * 0.4);
            var pt6 = pt1.OffsetXY(pipeWidth, floorHeight * 0.5);

            LoopLine.Split(ref fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            if (type == 2)
            {
                ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4);
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI * 3 / 2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(textWidth - 50));
            var p2Flag = false;
            if (pipeNumber1 is null)
            {
                ;
            }
            else
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
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType3(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;//立管标号 1
            }
            if (pipeNumber1?.Equals("") == true)
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, 3);
                return;
            }
            if (pipeNumber1.IsCurrentFloor())
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, 3);
            }
            else
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, 3);
            }
        }

        /// <summary>
        /// 绘制共用支管的双分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType4(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn);
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn, 4);
        }

        /// <summary>
        /// 绘制水泵接合器
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType5(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            double XGap = 800;
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
            LoopLine.Split(ref fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt51));
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4);
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
        /// 绘制跨层主环
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchAcross(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt,
            Point3dEx tpt, Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, bool flag3 = false)
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
            //ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4, flag3);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
        }


        /// <summary>
        /// 绘制2根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType02(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
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
                if (!fireHydrantSysIn.TermPointDic[pt1].PipeNumber?.Equals("") == true)
                {
                    double XGap = 1600;
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
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
                if (!fireHydrantSysIn.TermPointDic[pt2].PipeNumber?.Equals("") == true)
                {
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, pt2, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);

                }
            }
        }

        /// <summary>
        /// 绘制两根供水管，一根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType21(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
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
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }

        /// <summary>
        /// 绘制三根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType03(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
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
            GetBranchType5(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[0], new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
            pt4 = new Point3dEx(stpt1._pt.X + 800, stpt1._pt.Y, 0);
            stpt1 = new Point3dEx(stpt1._pt.X + XGap, stpt1._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[1], new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }

        private static void CollectFireHydrantElements(Point3dCollection selectArea, AcadDatabase adb, DBObjectCollection results)
        {
            GetFireHydrantPipeElements(selectArea, adb, results);
            GetFireHydrantPipeLineElements(selectArea, adb, results);
            GetFireHydrantPipeLableElements(selectArea, adb, results);
            GetFireHydrantPipeMarkElements(selectArea, adb, results);
            GetFireHydrantPipeTextElements(selectArea, adb, results);
        }

        /// <summary>
        /// 绘制一根供水管，两根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType12(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
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
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }
    }
}
