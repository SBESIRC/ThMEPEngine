﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Common;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Utils;
using ThMEPWSS.Uitl;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class PressureDrainageDataCollectionService
    {
        public List<Entity> Entities;
        public PressureDrainageGeoData CollectedData = new PressureDrainageGeoData();
        /// <summary>
        /// 提取数据主函数
        /// </summary>
        /// <param name="viewmodel"></param>
        public void CollectData(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            this.CollectEntities();
            this.CollectLabelLines();
            this.CollectLabelData();
            this.CollectVerticalPipes();
            this.CollectWaterWells();
            this.CollectWrappingPipes();
            this.CollectSubmergedPumps();
            this.CollectHorizontalPipes();
            this.CollectStoryFrame();
            GenerateNecessaryVerticalPipe();
            if (viewmodel.HasInfoTablesRoRead)
            {
                this.CollectInfoTables(viewmodel);
            }

            this.CollectWalls(viewmodel);
        }

        /// <summary>
        /// 提取图纸数据
        /// </summary>
        public void CollectEntities()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br)
                        {
                            if (br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 20000 && r.Width < 80000 && r.Height > 5000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else if (br.Layer == "块")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                }
                var entities = GetEntities().ToList();
                this.Entities = entities;
            }
        }
        /// <summary>
        /// 提取引线
        /// </summary>
        public void CollectLabelLines()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.LabelLines = new List<Line>();
                foreach (var e in Entities.OfType<Line>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-FRPT-NOTE") && e.Length > 0))
                {
                    Line line = new Line(e.StartPoint.ToPoint2d().ToPoint3d(), e.EndPoint.ToPoint2d().ToPoint3d());
                    this.CollectedData.LabelLines.Add(line);
                }
                foreach (var entity in adb.ModelSpace.OfType<Entity>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-FRPT-NOTE") && IsTianZhengElement(e)))
                {
                    foreach (var e in entity.ExplodeToDBObjectCollection().OfType<Line>())
                    {
                        Line line = new Line(e.StartPoint.ToPoint2d().ToPoint3d(), e.EndPoint.ToPoint2d().ToPoint3d());
                        this.CollectedData.LabelLines.Add(line);
                    }
                }
            }
        }
        /// <summary>
        /// 提取文字标注
        /// </summary>
        public void CollectLabelData()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.Labels = new List<DBText>();
                foreach (var e in Entities.OfType<DBText>().Where(e => e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS"))
                {
                    if (e.Bounds is Extents3d)
                    {
                        DBText dBText = new DBText();
                        dBText.TextString = e.TextString;
                        dBText.Position = e.Position;
                        dBText.Height = e.Height;
                        dBText.HorizontalMode = e.HorizontalMode;
                        dBText.VerticalMode = e.VerticalMode;
                        dBText.TextStyleId = e.TextStyleId;
                        this.CollectedData.Labels.Add(dBText);
                    }
                }
                foreach (var entity in adb.ModelSpace.OfType<Entity>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS") && IsTianZhengElement(e)))
                {
                    foreach (var e in entity.ExplodeToDBObjectCollection().OfType<DBText>())
                    {
                        if (e.Bounds is Extents3d)
                        {
                            DBText dBText = new DBText();
                            dBText.TextString = e.TextString;
                            dBText.Position = e.Position;
                            dBText.Height = e.Height;
                            dBText.HorizontalMode = e.HorizontalMode;
                            dBText.VerticalMode = e.VerticalMode;
                            dBText.TextStyleId = e.TextStyleId;
                            this.CollectedData.Labels.Add(dBText);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 提取排水立管
        /// </summary>
        public void CollectVerticalPipes()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.VerticalPipes = new List<Circle>();
                int distinguishDiameter = 35;
                {
                    string str1 = "带定位立管";
                    string str2 = "带定位立管150";
                    string layerName1 = "W-DRAI-EQPM";
                    string layerName2 = "W-RAIN-EQPM";
                    static Circle GetCorrespondindGeometry(Entity ent)
                    {
                        if (ent.Bounds is Extents3d)
                        {
                            var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                            var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                            if (et != null)
                            {
                                return new Circle(et.Center.ToPoint2d().ToPoint3d(), Vector3d.ZAxis, et.Radius);
                            }
                            else
                            {
                                Circle circle = ents[0];
                                foreach (var e in ents)
                                {
                                    circle = circle.Area > e.Area ? circle : e;
                                }
                                return new Circle(circle.Center.ToPoint2d().ToPoint3d(), Vector3d.ZAxis, circle.Radius);
                            }
                        }
                        else { return default; }
                    }
                    foreach (var e in Entities.OfType<Entity>().Where(e => e.Layer == layerName1 || e.Layer == layerName2).Where(e => e.ObjectId.IsValid)
                    .Where(e => e is BlockReference && (e.ToDataItem().EffectiveName == str1 || e.ToDataItem().EffectiveName == str2)))
                    {
                        this.CollectedData.VerticalPipes.Add(GetCorrespondindGeometry(e));
                    }
                    foreach (var e in Entities.OfType<Circle>().Where(e => e.Layer == layerName1 || e.Layer == layerName2)
                    .Where(e => e.Radius >= distinguishDiameter && e.Radius <= 300))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            Circle circle = new Circle(e.Center.ToPoint2d().ToPoint3d(), Vector3d.ZAxis, e.Radius);
                            this.CollectedData.VerticalPipes.Add(circle);
                        }
                    }
                    foreach (var e in Entities.OfType<Entity>().Where(e => (e.Layer == layerName1 || e.Layer == layerName2)
                     && PressureDrainageUtils.IsTianZhengElement(e)).Where(e => e.ExplodeToDBObjectCollection().OfType<Circle>().Any()))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            Circle circle = new Circle(extent3d.GetCenter().ToPoint2d().ToPoint3d(), Vector3d.ZAxis, 50);
                            this.CollectedData.VerticalPipes.Add(circle);
                        }
                    }
                    foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid
                     ? (e.Layer == layerName1 || e.Layer == layerName2) && e.ToDataItem().EffectiveName == "$LIGUAN"
                     : (e.Layer == layerName1 || e.Layer == layerName2)))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            this.CollectedData.VerticalPipes.Add(new Circle(extent3d.GetCenter().ToPoint2d().ToPoint3d(), Vector3d.ZAxis, 1155));
                        }
                    }
                    foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid)
                    .Where(e => (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE") && !e.ToDataItem().EffectiveName.Contains("井")))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            this.CollectedData.VerticalPipes.Add(new Circle(extent3d.GetCenter().ToPoint2d().ToPoint3d(), Vector3d.ZAxis, 1155));
                        }
                    }
                    PressureDrainageUtils.CollectTianzhengVerticalPipes(this.CollectedData.LabelLines, this.CollectedData.Labels, Entities);
                }
            }
        }
        /// <summary>
        /// 提取雨水井及编号
        /// </summary>
        public void CollectWaterWells()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.DrainWells = new List<DrainWellClass>();
                foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName().Contains("雨水井编号")))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d());
                        drainWell.Label = e.GetAttributesStrValue("-") ?? "";
                        drainWell.WellTypeName = e.Name;
                        this.CollectedData.DrainWells.Add(drainWell);
                    }
                }
                foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName().Contains("污废合流井编号")))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d());
                        drainWell.Label = e.GetAttributesStrValue("-") ?? "";
                        drainWell.WellTypeName = e.Name;
                        this.CollectedData.DrainWells.Add(drainWell);
                    }
                }
                foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName().Contains("污水井编号")))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d());
                        drainWell.Label = e.GetAttributesStrValue("-") ?? "";
                        drainWell.WellTypeName = e.Name;
                        this.CollectedData.DrainWells.Add(drainWell);
                    }
                }
            }
        }
        /// <summary>
        /// 提取套管
        /// </summary>
        public void CollectWrappingPipes()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.WrappingPipes = new List<Extents3d>();
                foreach (var e in Entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        this.CollectedData.WrappingPipes.Add(extent);
                    }
                }
            }
        }
        /// <summary>
        /// 提取潜水泵
        /// </summary>
        public void CollectSubmergedPumps()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.SubmergedPumps = new List<SubmergedPumpClass>();
                ObjectIdCollection objs = new ObjectIdCollection();
                var validRefs = Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid);
                var pumps = validRefs.Where(e => e.GetEffectiveName().Contains("潜水泵"));
                foreach (var e in pumps)
                {
                    if (e.Bounds is Extents3d)
                    {
                        var dbObjs = new DBObjectCollection();
                        e.Explode(dbObjs);
                        var visibleEnts = dbObjs.Cast<Entity>().Where(e => e.Visible).ToList();
                        List<Point3d> pts = new List<Point3d>();
                        foreach (var j in visibleEnts)
                        {
                            if (j.Bounds != null)
                            {
                                Extents3d extent = (Extents3d)j.Bounds;
                                foreach (Point3d k in extent.ToRectangle().Vertices())
                                {
                                    pts.Add(k);
                                }
                            }
                        }
                        List<Point3d> ptsboundary = ThGeometryTool.CalBoundingBox(pts);
                        Extents3d boundExtent = new Extents3d(ptsboundary[0], ptsboundary[1]);
                        SubmergedPumpClass submergedPump = new SubmergedPumpClass();
                        submergedPump.Extents = boundExtent;
                        submergedPump.Visibility = e.ObjectId.GetDynBlockValue("可见性");
                        submergedPump.Serial = e.GetAttributesStrValue("编号");
                        if (submergedPump.Visibility == "单台")
                        {
                            submergedPump.PumpCount = 1;
                        }
                        else if (submergedPump.Visibility == "两台")
                        {
                            submergedPump.PumpCount = 2;
                        }
                        else if (submergedPump.Visibility == "三台")
                        {
                            submergedPump.PumpCount = 3;
                        }
                        else if (submergedPump.Visibility == "四台")
                        {
                            submergedPump.PumpCount = 4;
                        }
                        submergedPump.Serial = submergedPump.Serial == null ? "" : submergedPump.Serial;
                        submergedPump.Visibility = submergedPump.Visibility == null ? "" : submergedPump.Visibility;
                        submergedPump.Location = submergedPump.Location == null ? "" : submergedPump.Location;
                        submergedPump.Allocation = submergedPump.Allocation == null ? "" : submergedPump.Allocation;
                        this.CollectedData.SubmergedPumps.Add(submergedPump);
                    }
                }
            }
        }
        /// <summary>
        /// 提取排水横管
        /// </summary>
        public void CollectHorizontalPipes()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.HorizontalPipes = new List<Line>();
                foreach (var e in Entities.OfType<Entity>().Where(e => (e.Layer == "W-FRPT-DRAI-PIPE") || (e.Layer == "W-RAIN-PIPE") || (e.Layer.Contains("W") && e.Layer.Contains("DRAI") && e.Layer.Contains("PIPE"))))
                {
                    var layer = e.Layer;
                    if (e is Line line && line.Length > 0)
                    {
                        var li = new Line(line.StartPoint.ToPoint2d().ToPoint3d(), line.EndPoint.ToPoint2d().ToPoint3d());
                        li.Layer = layer;
                        this.CollectedData.HorizontalPipes.Add(li);
                    }
                    else if (PressureDrainageUtils.IsTianZhengElement(e) && PressureDrainageUtils.TryConvertToLineSegment(e, out Line convertedLine) && convertedLine.Length > 0)
                    {
                        var explodedLine = e.ExplodeToDBObjectCollection().OfType<Line>().Where(k => k.Length > 0).ToList();
                        if (explodedLine.Count == 1)
                        {
                            var li = (new Line(explodedLine[0].StartPoint.ToPoint2d().ToPoint3d(), explodedLine[0].EndPoint.ToPoint2d().ToPoint3d()));
                            li.Layer = layer;
                            this.CollectedData.HorizontalPipes.Add(li);
                        }
                        else if (explodedLine.Count > 1)
                        {
                            var li = new Line(explodedLine[0].StartPoint.ToPoint2d().ToPoint3d(), explodedLine[0].EndPoint.ToPoint2d().ToPoint3d());
                            li.Layer = layer;
                            this.CollectedData.HorizontalPipes.Add(li);
                            Point3d pt1 = default, pt2 = default;
                            var tmpLineList = new List<Line>();
                            for (int i = 1; i < explodedLine.Count; i++)
                            {
                                var p = new Line(explodedLine[i].StartPoint.ToPoint2d().ToPoint3d(), explodedLine[i].EndPoint.ToPoint2d().ToPoint3d());
                                p.Layer = layer;
                                this.CollectedData.HorizontalPipes.Add(p);
                                pt1 = explodedLine[i - 1].EndPoint.ToPoint2d().ToPoint3d();
                                pt2 = explodedLine[i].StartPoint.ToPoint2d().ToPoint3d();
                                var tmpLine = new Line(pt1, pt2);
                                if (tmpLine.Length > 0)
                                {
                                    tmpLine.Layer = layer;
                                    tmpLineList.Add(tmpLine);
                                }
                            }
                            this.CollectedData.HorizontalPipes.AddRange(tmpLineList);
                        }
                    }
                }
                foreach (var e in Entities.OfType<Entity>().Where(e => ((e.Layer == "W-FRPT-DRAI-PIPE") || (e.Layer == "W-RAIN-PIPE") || (e.Layer.Contains("W") && e.Layer.Contains("DRAI") && e.Layer.Contains("PIPE")))
                     && PressureDrainageUtils.IsTianZhengElement(e)).Where(e => e.ExplodeToDBObjectCollection().OfType<Polyline>().Any()))
                {
                    var layer = e.Layer;
                    var plys = e.ExplodeToDBObjectCollection().OfType<Polyline>();
                    foreach (var ply in plys)
                    {
                        var vertices = ply.Vertices().Cast<Point3d>().ToList();
                        for (int i = 1; i < vertices.Count; i++)
                        {
                            Line lin = new Line(vertices[i - 1], vertices[i]);
                            lin.Layer = layer;
                            this.CollectedData.HorizontalPipes.Add(lin);
                        }
                    }
                }
                List<Line> lines = new List<Line>();
                this.CollectedData.HorizontalPipes.ForEach(o => lines.Add(o));
                List<Point3d> pts = new List<Point3d>();
                this.CollectedData.VerticalPipes.ForEach(o => pts.Add(o.Center));
                this.CollectedData.SubmergedPumps.ForEach(o => pts.Add(o.Extents.CenterPoint()));
                List<Line> mergedLines = new();
                lines.ForEach(o => mergedLines.Add(o));
                ConnectBrokenLine(lines, pts).Where(o => o.Length > 0).ForEach(o => mergedLines.Add(o));
                var objs = new DBObjectCollection();
                mergedLines.ForEach(o => objs.Add(o));
                var processedLines = ThLaneLineMergeExtension.Merge(objs).Cast<Line>().ToList();
                this.CollectedData.HorizontalPipes.Clear();
                processedLines.ForEach(o => this.CollectedData.HorizontalPipes.Add(o));
            }
        }
        /// <summary>
        /// 如果潜水泵旁漏画立管，给它补上
        /// </summary>
        public void GenerateNecessaryVerticalPipe()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                foreach (var j in this.CollectedData.SubmergedPumps)
                {
                    int dd = 0;
                    foreach (var k in this.CollectedData.VerticalPipes)
                    {
                        if (j.Extents.ToRectangle().GetClosePoint(k.Center).DistanceTo(k.Center) < 300 || j.Extents.IsPointIn(k.Center))
                        {
                            dd = 1;
                            break;
                        }
                    }
                    if (dd == 0)
                    {
                        double toldis = 50;
                        Point3d ptlocPipe = j.Extents.CenterPoint();
                        foreach (var lin in this.CollectedData.HorizontalPipes)
                        {
                            if (j.Extents.IsPointIn(lin.StartPoint) || j.Extents.ToRectangle().GetClosePoint(lin.StartPoint).DistanceTo(lin.StartPoint) < toldis)
                            {
                                ptlocPipe = lin.StartPoint;
                                break;
                            }
                            else if (j.Extents.IsPointIn(lin.EndPoint) || j.Extents.ToRectangle().GetClosePoint(lin.EndPoint).DistanceTo(lin.EndPoint) < toldis)
                            {
                                ptlocPipe = lin.EndPoint;
                                break;
                            }
                        }
                        Circle ci = new Circle(ptlocPipe, Vector3d.ZAxis, 50);
                        ci.Layer = "W-DRAI-EQPM";

                        double mindis = double.PositiveInfinity;
                        int index = -1;
                        for (int i = 0; i < this.CollectedData.HorizontalPipes.Count; i++)
                        {
                            double curdis = this.CollectedData.HorizontalPipes[i].GetClosestPointTo(ci.Center, false).DistanceTo(ci.Center);
                            if (curdis < mindis)
                            {
                                mindis = curdis;
                                index = i;
                            }
                        }
                        Line line = new Line(this.CollectedData.HorizontalPipes[index].GetClosestPointTo(ci.Center, false), ci.Center);
                        if (line.Length > 0)
                        {
                            this.CollectedData.HorizontalPipes.Add(line);
                        }
                        adb.Database.CreateAILayer("AdditonPipe",(short)0);
                        ci.Layer = "AdditonPipe";
                        this.CollectedData.VerticalPipes.Add(ci);
                    }
                }
            }
           
        }
        /// <summary>
        /// 收集提资表数据
        /// </summary>
        public void CollectInfoTables(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                foreach (var br in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName().Contains("集水井提资表表身")))
                {
                    if (viewmodel.InfoRegion.IsPointIn(br.Bounds.Value.CenterPoint()))
                    {
                        foreach (var pump in this.CollectedData.SubmergedPumps)
                        {
                            if (pump.Serial == br.GetAttributesStrValue("集水井编号"))
                            {
                                pump.Allocation = br.Id.GetDynBlockValue("水泵配置");
                                pump.Location = br.GetAttributesStrValue("位置");
                                double.TryParse(br.GetAttributesStrValue("流量"), out pump.paraQ);
                                double.TryParse(br.GetAttributesStrValue("扬程"), out pump.paraH);
                                double.TryParse(br.GetAttributesStrValue("电量"), out pump.paraN);
                                double.TryParse(br.GetAttributesStrValue("井深"), out pump.Depth);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 提取楼层框定定位点
        /// </summary>
        public void CollectStoryFrame()
        {
            var frames = FramedReadUtil.ReadAllFloorFramed();
            this.CollectedData.StoryFrameBasePt = new();
            foreach (var frame in frames)
            {
                this.CollectedData.StoryFrameBasePt.Add(new Point3d(frame.datumPoint.X, frame.datumPoint.Y, 0));
            }
        }

        /// <summary>
        /// 收集墙线
        /// </summary>
        public void CollectWalls(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            using (var Doclock = Active.Document.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                List<Polyline> frames = new List<Polyline>();

                Point3d ptmin = viewmodel.SelectedArea[0].X < viewmodel.SelectedArea[2].X ? viewmodel.SelectedArea[0] : viewmodel.SelectedArea[2];
                Point3d ptmax = viewmodel.SelectedArea[0].X < viewmodel.SelectedArea[2].X ? viewmodel.SelectedArea[2] : viewmodel.SelectedArea[0];
                Extents3d ex = new Extents3d(ptmin, ptmax);
                frames.Add(ex.ToRectangle());

                var pt = frames.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frames = frames.Select(x =>{return ThMEPFrameService.Normalize(x);}).ToList();
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                List<Polyline> res = new();
                foreach (var outFrame in frames)
                {
                    getPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);
                    walls.ForEach(e => res.Add(e));
                }
                this.CollectedData.WallPolyLines = new List<Polyline>();
                this.CollectedData.WallPolyLines.AddRange(res);
            }
        }
    }
}