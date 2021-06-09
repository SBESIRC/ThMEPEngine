﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using DotNetARX;
using System;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using AcHelper;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Diagram.ViewModel;

namespace ThMEPWSS
{


    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "TestExractCleanTools", CommandFlags.Modal)]
        public void TestExractCleanTools()
        {
            using (var db = AcadDatabase.Active())
            using (var engine = new ThWCleanToolsRecongnitionEngine())//创建卫生洁具识别引擎
            {
                var per = Active.Editor.GetEntity("\n选择一个框");//交互界面
                if (per.Status == PromptStatus.OK)//框选择成功
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                    storeysRecEngine.Recognize(db.Database, db.Element<Polyline>(per.ObjectId).Vertices());
                    var floorNum = storeysRecEngine.Elements.Select(floor => (floor as ThMEPEngineCore.Model.Common.ThStoreys).StoreyNumber).ToList();
                    var floorList = new int[floorNum.Count, 2];
                    for (int i = 0; i < floorNum.Count; i++)
                    {
                        var fNumi = floorNum[i].Split('-');
                        floorList[i, 0] = int.Parse(fNumi[0]);
                        floorList[i, 1] = int.Parse(fNumi[fNumi.Length - 1]);
                    }
                    var floorAreaList = new List<List<Point3dCollection>>();
                    foreach (var obj in storeysRecEngine.Elements)
                    {
                        if (obj is ThStoreys)
                        {
                            var sobj = obj as ThStoreys;
                            var br = db.Element<BlockReference>(sobj.ObjectId);
                            if (!br.IsDynamicBlock)
                            {
                                continue;
                            }

                            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
                            var eptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("宽度"));
                            var eptY = spt.Y - Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("高度"));
                            var LineXList = new List<double>();
                            var index = 1;
                            foreach (var p in sobj.ObjectId.GetDynProperties())
                            {
                                if (p.ToString().Contains("分割" + Convert.ToString(index) + " X"))
                                {
                                    LineXList.Add(spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割" + Convert.ToString(index) + " X")));
                                    index += 1;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            //创建楼层分区类
                            var floorZone = new FloorZone(spt, new Point3d(eptX, eptY, 0), LineXList);
                            var rectList = floorZone.CreateRectList();//创建楼层分区的多段线

                            floorAreaList.Add(rectList);
                        }
                    }

                    var CleanToolList = new List<List<CleaningToolsSystem>>();
                    var households = new int[floorAreaList.Count, floorAreaList[0].Count];
                    for (int i = 0; i < floorAreaList.Count; i++)
                    {
                        for (int j = 0; j < floorAreaList[0].Count; j++)
                        {
                            households[i, j] = 0;
                        }
                    }

                    for (int j = 0; j < floorAreaList.Count; j++)//遍历每个楼层
                    {
                        var CleanTools = new List<CleaningToolsSystem>();
                        for (int i = 0; i < floorAreaList[j].Count; i++)//遍历楼层的每个区域
                        {
                            var engineKitchen = new ThMEPEngineCore.Engine.ThRoomMarkRecognitionEngine();//创建厨房识别引擎
                            engineKitchen.Recognize(db.Database, floorAreaList[j][i]);
                            var ele = engineKitchen.Elements;
                            var roomMark = ele.Select(e => (e as ThMEPEngineCore.Model.ThIfcTextNote).Text);
                            foreach (var mark in roomMark)
                            {
                                if (mark == "厨房")
                                {
                                    households[j, i] += 1;
                                }
                            }

                            engine.Recognize(db.Database, floorAreaList[j][i]);
                            var allBlockNames = engine.Datas.Select(ct => ct.Data as string);
                            var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                            if (allBlockNames.ToList().Count > 0)
                            {
                                foreach (var tool in allBlockNames.ToList())
                                {
                                    cleanTools[ThCleanToolsManager.CleanToolIndex(tool)] += 1;
                                }
                                var CleanTool = new CleaningToolsSystem(j, i, households[j, i], cleanTools);

                                CleanTools.Add(CleanTool);
                            }
                        }
                        CleanToolList.Add(CleanTools);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "TestExractKitchens", CommandFlags.Modal)]
        public void TestExractKitchens()
        {
            using (var db = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个框");//交互界面
                var engine = new ThRoomMarkRecognitionEngine();//创建厨房识别引擎
                if (per.Status == PromptStatus.OK)//框选择成功
                {
                    var households = 0;
                    engine.Recognize(db.Database, db.Element<Polyline>(per.ObjectId).Vertices());
                    var ele = engine.Elements;
                    var roomMark = ele.Select(e => (e as ThMEPEngineCore.Model.ThIfcTextNote).Text);
                    foreach (var mark in roomMark)
                    {
                        if (mark == "厨房")
                        {
                            households += 1;
                        }
                    }
                }
            }
        }
    } 
}
