using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThMEPElectrical.AlarmSensorLayout.Command;
using ThMEPElectrical.AlarmSensorLayout.Sensorlayout;
using DotNetARX;

namespace ThMEPElectrical.AlarmSensorLayout.Test
{
    class TestCmd
    {
        //public BeamSensorLayout.BeamSensorLayout sensorLayout;

        public BeamSensorOpt sensorOpt;
        //public BeamChromosome chromosome;
        public List<ObjectId> lineId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> pointId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> UCS_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> blind_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> detect_List { get; set; } = new List<ObjectId>();
        public Point3d center { get; set; }
        public double angle { get; set; }

        //[CommandMethod("TIANHUACAD", "THASLT", CommandFlags.Modal)]
        //public void THASLT()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        foreach (var id1 in lineId_list)
        //        {
        //            id1.Erase();
        //        }
        //        lineId_list.Clear();
        //        foreach (var id1 in pointId_list)
        //        {
        //            id1.Erase();
        //        }
        //        pointId_list.Clear();
        //        // 获取框线
        //        PromptSelectionOptions options = new PromptSelectionOptions()
        //        {
        //            AllowDuplicates = false,
        //            MessageForAdding = "请选择布置区域框线",
        //            RejectObjectsOnLockedLayers = true,
        //        };
        //        var dxfNames = new string[]
        //        {
        //            RXClass.GetClass(typeof(Polyline)).DxfName,
        //        };
        //        var filter = ThSelectionFilterTool.Build(dxfNames);
        //        var result = Active.Editor.GetSelection(options, filter);
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        var ptOri = new Point3d();
        //        var transformer = new ThMEPOriginTransformer(ptOri);
        //        var frameList = new List<Polyline>();

        //        foreach (ObjectId obj in result.Value.GetObjectIds())
        //        {
        //            //获取外包框
        //            var frameTemp = acadDatabase.Element<Polyline>(obj);
        //            var nFrame = processFrame(frameTemp, transformer);
        //            if (nFrame.Area < 1)
        //            {
        //                continue;
        //            }

        //            frameList.Add(nFrame);
        //        }

        //        var frame = frameList.OrderByDescending(x => x.Area).First();

        //        var holeList = getPoly(frame, "AI-房间框线", transformer, true);

        //        var layoutList = getPoly(frame, "AI-可布区域", transformer, false);

        //        var wallList = getPoly(frame, "AI-墙", transformer, false);

        //        //计算房间方向
        //        var minRect = frame.OBB();
        //        center = frame.GetCentroidPoint();
        //        var vector = minRect.GetPoint3dAt(1) - minRect.GetPoint3dAt(0);
        //        if (frame.Area / minRect.Area > 0.9)
        //        {
        //            angle = vector.GetAngleTo(Vector3d.XAxis);
        //            if (angle > Math.PI / 4)
        //                angle = Math.PI / 2 - angle;
        //            else if (angle < -Math.PI / 4)
        //                angle = -Math.PI / 2 - angle;
        //        }
        //        else angle = 0;
        //        //angle = 0;

        //        //获取旋转后的房间
        //        frame.Rotate(center, angle);
        //        foreach (var hole in holeList)
        //            hole.Rotate(center, angle);
        //        foreach (var wall in wallList)
        //            wall.Rotate(center, angle);
        //        foreach (var layout in layoutList)
        //            layout.Rotate(center, angle);

        //        Polygon area = frame.ToNTSPolygon();
        //        foreach (var wall in wallList)
        //            area = area.Difference(wall.ToNTSPolygon()) as Polygon;
        //        foreach (var hole in holeList)
        //            area = area.Difference(hole.ToNTSPolygon()) as Polygon;

        //        List<Polygon> layouts = new List<Polygon>();
        //        foreach (var layout in layoutList)
        //        {
        //            var layoutNTS = layout.ToNTSPolygon();
        //            if(area.Contains(layoutNTS))
        //                layouts.Add(layoutNTS);
        //        }

        //        //输入区域
        //        var input_Area = new InputArea(area, layouts);
        //        //输入参数
        //        var equipmentParameter = new EquipmentParameter();
        //        //初始化布点引擎
        //        List<BeamSensorOpt> optlist = new List<BeamSensorOpt>();
        //        sensorOpt = new BeamSensorOpt(input_Area, equipmentParameter, 5400, 7500, 8000);
        //        sensorOpt.CalculatePlace();
        //        Debug.WriteLine("{0}", sensorOpt.minGap);
        //        ShowPoints();
        //        ShowLines();
        //        ShowBlind();

        //        frame.Rotate(center, -angle);
        //        foreach (var hole in holeList)
        //            hole.Rotate(center, -angle);
        //        foreach (var wall in wallList)
        //            wall.Rotate(center, -angle);
        //        foreach (var layout in layoutList)
        //            layout.Rotate(center, -angle);
        //    }
        //}


        //[CommandMethod("TIANHUACAD", "THASLS", CommandFlags.Modal)]
        //public void THASLS()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        ObjectId id;
        //        //选择点
        //        var per = Active.Editor.GetEntity("请选择点");
        //        if (per.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        var frame = acadDatabase.Element<Circle>(per.ObjectId);
        //        frame.Rotate(center, angle);
        //        var point = frame.Center.ToNTSCoordinate();

        //        int h_index = 0, v_index = 0;
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //            for (int j = 0; j < sensorOpt.vLines.Count; j++)
        //                if (sensorOpt.Positions[i][j].Equals(point))
        //                {
        //                    h_index = i;
        //                    v_index = j;
        //                }

        //        //如果当前横线与上一条横线距离过大或者过小，先调整距离
        //        var hgap = sensorOpt.GetTopPoint(h_index, v_index).Y - sensorOpt.Positions[h_index][v_index].Y;
        //        if (sensorOpt.HasTop(h_index, v_index) && (hgap > sensorOpt.maxGap || hgap < sensorOpt.minGap))
        //            sensorOpt.CutHLine(h_index, v_index, sensorOpt.GetTopPoint(h_index, v_index).Y - sensorOpt.AdjustGap);
        //        else if (!sensorOpt.HasTop(h_index, v_index) && (hgap > sensorOpt.AdjustGap / 2))
        //            sensorOpt.CutHLine(h_index, v_index, sensorOpt.GetTopPoint(h_index, v_index).Y - sensorOpt.AdjustGap / 2);
        //        var vgap = sensorOpt.Positions[h_index][v_index].X - sensorOpt.GetLeftPoint(h_index, v_index).X;
        //        //如果当前竖线与上一条竖线距离过大或者过小，先调整距离
        //        if (sensorOpt.HasLeft(h_index, v_index) && (vgap > sensorOpt.maxGap || vgap < sensorOpt.minGap))
        //            sensorOpt.CutVLine(h_index, v_index, sensorOpt.GetLeftPoint(h_index, v_index).X + sensorOpt.AdjustGap);
        //        else if (!sensorOpt.HasLeft(h_index, v_index) && (vgap > sensorOpt.AdjustGap / 2))
        //            sensorOpt.CutVLine(h_index, v_index, sensorOpt.GetLeftPoint(h_index, v_index).X + sensorOpt.AdjustGap / 2);
        //        //当前点不在房间内
        //        if (!sensorOpt.validPoints[h_index][v_index])
        //            return;

        //        var nearlayouts = sensorOpt.GetNearLayouts(h_index, v_index);
        //        var old_Line = new List<Coordinate>();
        //        var old_valid = new List<bool>();
        //        for (int t = 0; t < sensorOpt.hLines.Count; t++)
        //        {
        //            old_Line.Add(sensorOpt.Positions[t][v_index].Copy());
        //            old_valid.Add(sensorOpt.validPoints[t][v_index]);
        //        }
        //        ////先试试能否直接移动到可布置区域中心点
        //        //var target_center = sensorOpt.FindNearestPoint(h_index, v_index, nearlayouts);
        //        //if (sensorOpt.AdjustVLine(h_index, v_index, target_center.X))
        //        //{
        //        //    if (sensorOpt.AdjustHLine(h_index, v_index, target_center.Y))
        //        //    {
        //        //        ShowPoints();
        //        //        return;
        //        //    }
        //        //    else
        //        //    {
        //        //        for (int t = 0; t < sensorOpt.hLines.Count; t++)
        //        //        {
        //        //            sensorOpt.Positions[t][v_index] = old_Line[t];
        //        //            sensorOpt.validPoints[t][v_index] = old_valid[t];
        //        //        }
        //        //    }
        //        //}

        //        //var target_centerH = sensorOpt.FindNearestPointOnHLine(h_index, v_index, nearlayouts);
        //        //var target_centerV = sensorOpt.FindNearestPointOnVLine(h_index, v_index, nearlayouts);
        //        //if (target_centerH == null && target_centerV == null)
        //        //    Debug.WriteLine("没有中心点能移动");
        //        //else if (target_centerV == null)
        //        //{
        //        //    if (sensorOpt.AdjustVLine(h_index, v_index, target_centerH.X))
        //        //    {
        //        //        ShowPoints();
        //        //        return;
        //        //    }
        //        //    else
        //        //        Debug.WriteLine("只有横线上有中心点，移动竖线失败！");
        //        //}
        //        //else if (target_centerH == null)
        //        //{
        //        //    if (sensorOpt.AdjustHLine(h_index, v_index, target_centerV.Y))
        //        //    {
        //        //        ShowPoints();
        //        //        return;
        //        //    }
        //        //    else
        //        //        Debug.WriteLine("只有竖线上有中心点，移动横线失败！");
        //        //}
        //        //else if (target_centerH.Distance(point) <= target_centerV.Distance(point))
        //        //{
        //        //    if (sensorOpt.AdjustVLine(h_index, v_index, target_centerH.X) || sensorOpt.AdjustHLine(h_index, v_index, target_centerV.Y))
        //        //    {
        //        //        ShowPoints();
        //        //        return;
        //        //    }
        //        //    Debug.WriteLine("优先移动竖线至中心点，移动失败！");
        //        //}
        //        //else
        //        //{
        //        //    if (sensorOpt.AdjustHLine(h_index, v_index, target_centerV.Y) || sensorOpt.AdjustVLine(h_index, v_index, target_centerH.X))
        //        //    {
        //        //        ShowPoints();
        //        //        return;
        //        //    }
        //        //    else
        //        //        Debug.WriteLine("优先移动横线至中心点，移动失败！");
        //        //}


        //        if (Methods.MultiPolygonContainPoint(sensorOpt.m_inputArea.layout_area, sensorOpt.Positions[h_index][v_index]))
        //        {
        //            Debug.WriteLine("已经在可布置区域内");

        //            ShowPoints();
        //            ShowLines();
        //            return;
        //        }

        //        var target_V = sensorOpt.FindNearestPointOnVLineWithBuffer(h_index, v_index, nearlayouts, 300);
        //        var target_H = sensorOpt.FindNearestPointOnHLineWithBuffer(h_index, v_index, nearlayouts, 300);
        //        var target = sensorOpt.FindNearestPointWithBuffer(h_index, v_index, nearlayouts, 300);
        //        if (target_V == null && target_H == null) 
        //        {
        //            Debug.WriteLine("找不到目标点");
        //        }
        //        else if(target_V==null)
        //        {
        //            if (sensorOpt.AdjustVLine(h_index, v_index, target_H.X))
        //            {
        //                ShowPoints();
        //                ShowLines();
        //                return;
        //            }
        //            else
        //                Debug.WriteLine("只能移动竖线，移动竖线失败！");
        //        }
        //        else if(target_H==null)
        //        {
        //            if (sensorOpt.AdjustHLine(h_index, v_index, target_V.Y))
        //            {
        //                ShowPoints();
        //                ShowLines();
        //                return;
        //            }
        //            else
        //                Debug.WriteLine("只能移动横线，移动横线失败！");
        //        }
        //        else if(target_H.Distance(point)<=target_V.Distance(point))
        //        {
        //            if (sensorOpt.AdjustVLine(h_index, v_index, target_H.X) || sensorOpt.AdjustHLine(h_index, v_index, target_V.Y))
        //            {
        //                ShowPoints();
        //                ShowLines();
        //                return;
        //            }
        //            else
        //                Debug.WriteLine("优先移动竖线，移动失败！");
        //        }
        //        else
        //        {
        //            if (sensorOpt.AdjustHLine(h_index, v_index, target_V.Y) || sensorOpt.AdjustVLine(h_index, v_index, target_H.X))
        //            {
        //                ShowPoints();
        //                ShowLines();
        //                return;
        //            }
        //            else
        //                Debug.WriteLine("优先移动横线，移动失败！");
        //        }


        //        if (sensorOpt.AdjustVLine(h_index, v_index, target.X))
        //        {
        //            if(sensorOpt.AdjustHLine(h_index, v_index, target.Y))
        //            {
        //                ShowPoints();
        //                ShowLines();
        //                return;
        //            }

        //        }
        //        for (int t = 0; t < sensorOpt.hLines.Count; t++)
        //        {
        //            sensorOpt.Positions[t][v_index] = old_Line[t];
        //            sensorOpt.validPoints[t][v_index] = old_valid[t];
        //        }
        //        Debug.WriteLine("同时移动横线和竖线，移动失败！");
        //        sensorOpt.CutHLine(h_index, v_index, target.Y);
        //        sensorOpt.CutVLine(h_index, v_index, target.X);

        //        ShowPoints();
        //        ShowLines();
        //    }
        //}
        [CommandMethod("TIANHUACAD", "THASLR", CommandFlags.Modal)]
        public void THASLR()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frame = frameList.OrderByDescending(x => x.Area).First();

                var holeList = getPoly(frame, "AI-房间框线", transformer, true);

                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);

                var wallList = getPoly(frame, "AI-墙", transformer, false);

                var rst = Active.Editor.GetDouble(new PromptDoubleOptions("Input protection radius:"));
                if (rst.Status != PromptStatus.OK)
                    return;
                var radius = rst.Value;

                var layoutCmd = new AlarmSensorLayoutCmd();
                layoutCmd.frame = frame;
                layoutCmd.holeList = holeList;
                layoutCmd.layoutList = layoutList;
                layoutCmd.wallList = wallList;
                layoutCmd.protectRadius = radius;
                layoutCmd.equipmentType = BlindType.VisibleArea;
                layoutCmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THASLM", CommandFlags.Modal)]
        public void THASLM()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frame = frameList.OrderByDescending(x => x.Area).First();

                var holeList = getPoly(frame, "AI-房间框线", transformer, true);

                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);

                var wallList = getPoly(frame, "AI-墙", transformer, false);

                SpaceDivider groupOpt = new SpaceDivider();
                groupOpt.Compute(frame, layoutList);

                foreach (var id in UCS_List)
                {
                    id.Erase();
                }
                UCS_List.Clear();

                //foreach (var layout in groupOpt.layouts)
                //{
                //    var dblayout = layout.ent;
                //    dblayout.ColorIndex = layout.GroupID;
                //    var id = acadDatabase.ModelSpace.Add(dblayout);
                //    circle_List.Add(id);

                //    var dbline = new Line(layout.ent.GetCentroidPoint(), layout.ent.GetCentroidPoint() + new Vector3d(300, 0, 0));
                //    dbline.Rotate(dbline.StartPoint, layout.angle / 180 * Math.PI);
                //    dbline.ColorIndex = layout.GroupID;
                //    id = acadDatabase.ModelSpace.Add(dbline);
                //    circle_List.Add(id);
                //}
                foreach (var group in groupOpt.UCSs)
                {
                    var dbucs = group.Key;
                    dbucs.ColorIndex = 5;

                    //var dbline = new Line(dbucs.GetCentroidPoint(), dbucs.GetCentroidPoint() + new Vector3d(3000, 0, 0));
                    //dbline.Rotate(dbline.StartPoint, group.Value / 180 * Math.PI);
                    //dbline.ColorIndex = 5;
                    //var id = acadDatabase.ModelSpace.Add(dbline);
                    //UCS_List.Add(id);

                    //dbucs.Rotate(dbucs.GetCentroidPoint(),-group.Value / 180 * Math.PI);
                    var id = acadDatabase.ModelSpace.Add(dbucs);
                    UCS_List.Add(id);
                }
            }
        }

        //private void ShowLines()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        foreach (var id1 in lineId_list)
        //        {
        //            id1.Erase();
        //        }
        //        lineId_list.Clear();
        //        Point3d p0, p1;
        //        //画上边线
        //        for (int i = 0; i < sensorOpt.vLines.Count; i++)
        //        {
        //            p0 = new Point3d(sensorOpt.Positions[0][i].X, sensorOpt.Positions[0][i].Y + 5800, 0);
        //            p1 = new Point3d(sensorOpt.Positions[0][i].X, sensorOpt.Positions[0][i].Y, 0);
        //            var line = new Line(p0, p1);
        //            line.ColorIndex = 1;
        //            var id = acadDatabase.ModelSpace.Add(line);
        //            id.Rotate(center, -angle);
        //            lineId_list.Add(id);
        //        }
        //        //画竖线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            for (int j = 0; j < sensorOpt.vLines.Count; j++)
        //            {
        //                p0 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y, 0);
        //                if (i != sensorOpt.hLines.Count - 1)
        //                    p1 = new Point3d(sensorOpt.Positions[i + 1][j].X, sensorOpt.Positions[i + 1][j].Y, 0);
        //                else
        //                    p1 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y - 5800, 0);
        //                var line = new Line(p0, p1);
        //                line.ColorIndex = 1;
        //                var id = acadDatabase.ModelSpace.Add(line);
        //                id.Rotate(center, -angle);
        //                lineId_list.Add(id);
        //            }
        //        }
        //        //画左边线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            p0 = new Point3d(sensorOpt.Positions[i][0].X-5800, sensorOpt.Positions[i][0].Y, 0);
        //            p1 = new Point3d(sensorOpt.Positions[i][0].X, sensorOpt.Positions[i][0].Y, 0);
        //            var line = new Line(p0, p1);
        //            line.ColorIndex = 1;
        //            var id = acadDatabase.ModelSpace.Add(line);
        //            id.Rotate(center, -angle);
        //            lineId_list.Add(id);
        //        }
        //        //画横线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            for (int j = 0; j < sensorOpt.vLines.Count; j++)
        //            {
        //                p0 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y, 0);
        //                if (j != sensorOpt.vLines.Count - 1)
        //                    p1 = new Point3d(sensorOpt.Positions[i][j + 1].X, sensorOpt.Positions[i][j + 1].Y, 0);
        //                else
        //                    p1 = new Point3d(sensorOpt.Positions[i][j].X+5800, sensorOpt.Positions[i][j].Y, 0);
        //                var line = new Line(p0, p1);
        //                line.ColorIndex = 1;
        //                var id = acadDatabase.ModelSpace.Add(line);
        //                id.Rotate(center, -angle);
        //                lineId_list.Add(id);
        //            }
        //        }
        //    }
        //}

        private static List<Polyline> getPoly(Polyline frame, string sLayer, ThMEPOriginTransformer transformer, bool onlyContains)
        {

            var layoutArea = ExtractPolyline(frame, sLayer, transformer, onlyContains);
            var layoutList = layoutArea.Select(x => x.Value).ToList();

            return layoutList;

        }
        private static Dictionary<Polyline, Polyline> ExtractPolyline(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer, bool onlyContain)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<Polyline>()
                      .Where(o => o.Layer == LayerName);

                List<Polyline> lineList = line.Select(x => x.WashClone() as Polyline).ToList();

                var plInFrame = new Dictionary<Polyline, Polyline>();

                foreach (Polyline pl in lineList)
                {
                    if (pl != null)
                    {
                        var plTrans = pl.Clone() as Polyline;

                        transformer.Transform(plTrans);
                        plInFrame.Add(pl, plTrans);
                    }
                }
                if (onlyContain == false)
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value) || bufferFrame.Intersects(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }


                return plInFrame;
            }
        }
        private static Polyline processFrame(Polyline frame, ThMEPOriginTransformer transformer)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            transformer.Transform(frameClone);
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;
        }
    }
}
