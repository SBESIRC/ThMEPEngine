using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Utils;
using ThMEPWSS.Uitl;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;
using static ThMEPWSS.PressureDrainageSystem.DebugTools;
using System.Diagnostics;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class PressureDrainageDataCollectionService
    {
        public PressureDrainageDataCollectionService(PressureDrainageSystemDiagramVieModel _viewmodel)
        {
            viewmodel = _viewmodel;
            SelectedBound = viewmodel.SelectedBound;
        }
        public List<Entity> Entities;
        public PressureDrainageGeoData CollectedData = new PressureDrainageGeoData();
        public Polyline SelectedBound { get; set; }
        private PressureDrainageSystemDiagramVieModel viewmodel { get; set; }
        public List<Line> Lines = new List<Line>();
        public List<Polyline> Polylines = new List<Polyline>();
        public List<BlockReference> BlockReferences = new List<BlockReference>();
        public List<Entity> TangentElements = new List<Entity>();

        public void InitData()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                IEnumerable<Entity> GetEntities()
                {
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        yield return ent;
                    }
                }
                Entities = GetEntities().ToList();
                Lines = Entities.OfType<Line>().ToList();
                Polylines = Entities.OfType<Polyline>().ToList();
                BlockReferences = Entities.OfType<BlockReference>().ToList();
                //Entities = Entities.Except(Lines).Except(Polylines).Except(BlockReferences).ToList();
                TangentElements = Entities.Where(e => IsTianZhengElement(e)).ToList();
                //Entities = Entities.Except(TangentElements).ToList();
            }
        }
        /// <summary>
        /// 提取数据主函数
        /// </summary>
        /// <param name="viewmodel"></param>
        public async void CollectData()
        {
            //double minX = Math.Min(viewmodel.SelectedArea[0].X, viewmodel.SelectedArea[2].X);
            //double maxX = Math.Max(viewmodel.SelectedArea[0].X, viewmodel.SelectedArea[2].X);
            //double minY = Math.Min(viewmodel.SelectedArea[0].Y, viewmodel.SelectedArea[2].Y);
            //double maxY = Math.Max(viewmodel.SelectedArea[0].Y, viewmodel.SelectedArea[2].Y);
            //Point3d ptmin = new Point3d(minX, minY, 0);
            //Point3d ptmax = new Point3d(maxX, maxY, 0);
            //var frame = new Extents3d(ptmin, ptmax).ToRectangle();
            //var task = new Task<List<List<Polyline>>>(() => CollectWallsAndColumns(frame));
            //task.Start();

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();

            this.CollectLabelLines();
            this.CollectLabelData();
            this.CollectVerticalPipes();
            this.CollectWaterWells();
            this.CollectWrappingPipes();
            this.CollectpumpWells();
            this.CollectSubmergedPumps();
            this.CollectHorizontalPipes();
            this.CollectStoryFrame();
            GenerateNecessaryVerticalPipe();
            if (viewmodel.HasInfoTablesRoRead)
            {
                this.CollectInfoTables(viewmodel);
            }
            this.CollectedData.WallPolyLines = new List<Polyline>();
            this.CollectedData.ColumnsPolyLines = new List<Polyline>();

            //sw1.Stop();
            //Stopwatch sw2 = new Stopwatch();
            //sw2.Start();

            //task.Wait();

            //sw2.Stop();
            //MessageBox.Show(sw1.ElapsedMilliseconds.ToString());
            //MessageBox.Show(sw2.ElapsedMilliseconds.ToString());

            //var wallInfos = task.Result;
            //CollectedData.WallPolyLines = wallInfos[0];
            //CollectedData.ColumnsPolyLines= wallInfos[1];

            this.CollectWallsAndColumns(viewmodel);
        }

        /// <summary>
        /// 提取引线
        /// </summary>
        public void CollectLabelLines()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.LabelLines = new List<Line>();
                foreach (var e in Entities.OfType<Line>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-FRPT-NOTE" || e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                {
                    Line line = new Line(e.StartPoint.ToPoint2d().ToPoint3d(), e.EndPoint.ToPoint2d().ToPoint3d());
                    this.CollectedData.LabelLines.Add(line);
                }
                foreach (var entity in adb.ModelSpace.OfType<Entity>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-FRPT-NOTE" || e.Layer == "W-RAIN-DIMS") && IsTianZhengElement(e)))
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
                foreach (var e in Entities.OfType<DBText>().Where(e => e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-RAIN-DIMS"))
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
                foreach (var entity in adb.ModelSpace.OfType<Entity>().Where(e => (e.Layer == "W-FRPT-HYDT-DIMS" || e.Layer == "W-DRAI-DIMS" || e.Layer == "W-RAIN-DIMS") && IsTianZhengElement(e)))
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



                    foreach (var e in Entities.OfType<Entity>().Where(e => IsVertPipeLayer(e.Layer)).Where(e => e.ObjectId.IsValid)
                    .Where(e => e is BlockReference && (e.ToDataItem().EffectiveName == str1 || e.ToDataItem().EffectiveName == str2)))
                    {
                        this.CollectedData.VerticalPipes.Add(GetCorrespondindGeometry(e));
                    }
                    foreach (var e in Entities.OfType<Circle>().Where(e => IsVertPipeLayer(e.Layer))
                    .Where(e => e.Radius >= distinguishDiameter && e.Radius <= 300))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            Circle circle = new Circle(e.Center.ToPoint2d().ToPoint3d(), Vector3d.ZAxis, e.Radius);
                            this.CollectedData.VerticalPipes.Add(circle);
                        }
                    }
                    foreach (var e in Entities.OfType<Entity>().Where(e => IsVertPipeLayer(e.Layer)
                     && PressureDrainageUtils.IsTianZhengElement(e)).Where(e => e.ExplodeToDBObjectCollection().OfType<Circle>().Any()))
                    {
                        if (e.Bounds is Extents3d extent3d)
                        {
                            Circle circle = new Circle(extent3d.GetCenter().ToPoint2d().ToPoint3d(), Vector3d.ZAxis, 50);
                            this.CollectedData.VerticalPipes.Add(circle);
                        }
                    }
                    foreach (var e in Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid
                     ? (IsVertPipeLayer(e.Layer)) && e.ToDataItem().EffectiveName == "$LIGUAN"
                     : (IsVertPipeLayer(e.Layer))))
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

        bool IsVertPipeLayer(string layer)
        {
            string layerName1 = "DRAI";
            string layerName2 = "RAIN";
            return layer.Contains(layerName1) || layer.Contains(layerName2);
        }
        
        /// <summary>
        /// 提取雨水井及编号
        /// </summary>
        public void CollectWaterWells()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                this.CollectedData.DrainWells = new List<DrainWellClass>();
                foreach (var e in Entities.OfType<BlockReference>().Where(e =>
                {
                    try
                    {
                        return e.Database != null && e.GetEffectiveName().Contains("雨水井编号");
                    }
                    catch
                    {
                        //使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块
                        return false;
                    }
                }))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d()).ToRectangle();
                        drainWell.Label = e.GetAttributesStrValue("-") ?? "";
                        drainWell.WellTypeName = e.Name;
                        this.CollectedData.DrainWells.Add(drainWell);
                    }
                }
                foreach (var e in Entities.OfType<BlockReference>().Where(e =>
                {
                    try
                    {
                        return e.Database != null && e.GetEffectiveName().Contains("污废合流井编号");
                    }
                    catch
                    {
                        //使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块
                        return false;
                    }
                }))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d()).ToRectangle();
                        drainWell.Label = e.GetAttributesStrValue("-") ?? "";
                        drainWell.WellTypeName = e.Name;
                        this.CollectedData.DrainWells.Add(drainWell);
                    }
                }
                foreach (var e in Entities.OfType<BlockReference>().Where(e =>
                {
                    try
                    {
                        return e.Database != null && (e.GetEffectiveName().Contains("污水井编号") || e.GetEffectiveName().Contains("废水井编号"));
                    }
                    catch
                    {
                        //使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块
                        return false;
                    }
                }))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        DrainWellClass drainWell = new DrainWellClass();
                        drainWell.Extents = new Extents3d(extent.MinPoint.ToPoint2d().ToPoint3d(), extent.MaxPoint.ToPoint2d().ToPoint3d()).ToRectangle();
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
                foreach (var e in Entities.OfType<BlockReference>().Where(x =>
                {
                    var cond = false;
                    try
                    {
                        cond =x.Database != null && (x.GetEffectiveName().Equals("套管")|| x.GetEffectiveName().Equals("人防套管"));
                    }
                    catch{/*使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块*/}
                    return x.ObjectId.IsValid ? x.Layer == "W-BUSH" && cond : x.Layer == "W-BUSH";
                }))
                {
                    if (e.Bounds is Extents3d extent)
                    {
                        this.CollectedData.WrappingPipes.Add(extent);
                    }
                }
                var names = new string[] { "00000093", "00000094", "00000095", "00000096", "00000097" };
                foreach (var e in Entities.OfType<Entity>().Where(e => e.Layer == "W-BUSH").Where(e => e.ObjectId.IsValid)
                    .Where(e => IsTianZhengElement(e)))
                {
                    var exploded_ents = GetAllEntitiesByExplodingTianZhengElementThoroughly(e);
                    foreach (var exploded_ent in exploded_ents)
                    {
                        if (exploded_ent is BlockReference br)
                        {
                            foreach (var name in names)
                            {
                                if (br.Name.Contains("public") && br.Name.Contains(name) && br.Bounds is Extents3d extent)
                                {
                                    this.CollectedData.WrappingPipes.Add(extent);
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 识别并提取集水井
        /// </summary>
        public void CollectpumpWells()
        {
            List<ThWWaterWell> waterWellList = new List<ThWWaterWell>();
            WaterWellIdentifyConfigInfo info = new WaterWellIdentifyConfigInfo();
            info.WhiteList.Clear();
            viewmodel.WellBlockKeyNames.ForEach(e => info.WhiteList.Add(e));
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(info))
            {
                waterwellEngine.Recognize(database.Database, viewmodel.SelectedArea);
                waterwellEngine.RecognizeMS(database.Database, viewmodel.SelectedArea);
                var objIds = new ObjectIdCollection(); // Print
                foreach (var element in waterwellEngine.Datas)
                {
                    ThWWaterWell waterWell = ThWWaterWell.Create(element);
                    waterWell.Init();
                    waterWellList.Add(waterWell);
                }
            }
            this.CollectedData.Wells = new List<WellInfo>();
            foreach (var e in waterWellList)
            {
                WellInfo well = new WellInfo();
                well.Location = e.OBB.GetCenter();
                if (e.Length < e.Width)
                {
                    double tmp = e.Length;
                    e.Length = e.Width;
                    e.Width = tmp;
                }
                well.Length = ((int)(e.Length / 100)) * 100;
                well.Width = ((int)(e.Width / 100)) * 100;
                this.CollectedData.Wells.Add(well);
            }
        }
        /// <summary>
        /// 识别提取潜水井
        /// </summary>
        public void CollectPumpWells()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var wells = Entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid).
                    Where(e =>
                    {
                        try
                        {
                            return e.Database != null && e.GetEffectiveName().Contains("Well");
                        }
                        catch
                        {
                            //使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块
                            return false;
                        }
                    });
                foreach (var br in wells)
                {
                    int a = 1;
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
                var pumps = validRefs.Where(e =>
                {
                    try
                    {
                        return e.Database != null && e.GetEffectiveName().Contains("潜水泵");
                    }
                    catch
                    {
                        //使用GetEffectiveName()方法和最新ExtractBlock()方法均有问题，使用trycatch筛除掉非目标块
                        return false;
                    }
                });
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
                        submergedPump.Extents = boundExtent.ToRectangle();
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
                        SortWellsBasedSpecailPumps(this.CollectedData.Wells, submergedPump.Extents.GetCenter());
                        if (this.CollectedData.Wells.Count > 0)
                        {
                            if (this.CollectedData.Wells[0].Location.DistanceTo(submergedPump.Extents.GetCenter()) < 5000)
                            {
                                submergedPump.Length = this.CollectedData.Wells[0].Length.ToString();
                                submergedPump.Width = this.CollectedData.Wells[0].Width.ToString();
                                this.CollectedData.Wells.RemoveAt(0);
                            }
                        }
                        submergedPump.Block = e;
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
            HorizontalPipeCollection horizontalPipeCollection = new HorizontalPipeCollection(this);
            horizontalPipeCollection.Execute();
            CollectedData = horizontalPipeCollection.CollectedData;        
        }
        /// <summary>
        /// 如果潜水泵旁漏画立管，给它补上
        /// </summary>
        public void GenerateNecessaryVerticalPipe()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                double tol = 600;
                foreach (var j in this.CollectedData.SubmergedPumps)
                {
                    var vertPoints = CollectedData.VerticalPipes.Select(e => e.Center);
                    if (vertPoints.Any())
                    {
                        var submergePoint = j.Extents.GeometricExtents.CenterPoint();
                        var vertPoint = vertPoints.OrderBy(e => e.DistanceTo(submergePoint)).First();
                        if (j.Extents.GetClosePoint(vertPoint).DistanceTo(vertPoint) < tol || j.Extents.IsPointIn(vertPoint))
                        {
                            continue;
                        }
                        else
                        {
                            double toldis = 50;
                            Point3d ptlocPipe = j.Extents.Centroid();
                            if (CollectedData.HorizontalPipes.Count == 0)
                                return;
                            var lins_a = CollectedData.HorizontalPipes.Select(e => e.Line).OrderBy(e => j.Extents.GeometricExtents.CenterPoint().DistanceTo(e.StartPoint)).First();
                            var lins_b = CollectedData.HorizontalPipes.Select(e => e.Line).OrderBy(e => j.Extents.GeometricExtents.CenterPoint().DistanceTo(e.EndPoint)).First();
                            if (j.Extents.IsPointIn(lins_a.StartPoint) || j.Extents.GetClosePoint(lins_a.StartPoint).DistanceTo(lins_a.StartPoint) < toldis)
                                ptlocPipe = lins_a.StartPoint;
                            else if (j.Extents.IsPointIn(lins_b.EndPoint) || j.Extents.GetClosePoint(lins_b.EndPoint).DistanceTo(lins_b.EndPoint) < toldis)
                                ptlocPipe = lins_b.EndPoint;
                            Circle ci = new Circle(ptlocPipe, Vector3d.ZAxis, 50);
                            ci.Layer = "W-DRAI-EQPM";
                            double mindis = 2000;
                            int index = -1;
                            for (int i = 0; i < this.CollectedData.HorizontalPipes.Count; i++)
                            {
                                double curdis = this.CollectedData.HorizontalPipes[i].Line.GetClosestPointTo(ci.Center, false).DistanceTo(ci.Center);
                                if (curdis < mindis)
                                {
                                    mindis = curdis;
                                    index = i;
                                }
                            }
                            if (index != -1)
                            {
                                Line line = new Line(this.CollectedData.HorizontalPipes[index].Line.GetClosestPointTo(ci.Center, false), ci.Center);
                                if (line.Length > 0)
                                {
                                    this.CollectedData.HorizontalPipes.Add(new Horizontal(line, false));
                                }
                            }
                            if (!adb.Layers.Contains("AdditonPipe"))
                                adb.Database.CreateAILayer("AdditonPipe", (short)0);
                            ci.Layer = "AdditonPipe";
                            this.CollectedData.VerticalPipes.Add(ci);
                        }
                    }
                    else
                        return;
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
                foreach (var br in Entities.OfType<BlockReference>().Where(e =>
                {
                    try
                    {
                        return e.ObjectId.IsValid && e.GetEffectiveName().Contains("集水井提资表表身");
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }))
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
                                double length = 0;
                                double width = 0;
                                double depth = 0;
                                double.TryParse(br.GetAttributesStrValue("长"), out length);
                                double.TryParse(br.GetAttributesStrValue("宽"), out width);
                                double.TryParse(br.GetAttributesStrValue("深"), out depth);
                                pump.Length = length.ToString();
                                pump.Width = width.ToString();
                                if (depth > 0) pump.Depth = depth / 1000;
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

        List<List<Polyline>> CollectWallsAndColumns(Polyline frame)
        {
            var res=new List<List<Polyline>>();
            using (var Doclock = Active.Document.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                List<Polyline> frames = new List<Polyline>();
                frames.Add(frame);
                var pt = frames.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frames = frames.Select(x => { return ThMEPFrameService.Normalize(x); }).ToList();
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                List<Polyline> resWalls = new();
                List<Polyline> resColumns = new();
                foreach (var outFrame in frames)
                {
                    getPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);
                    walls.ForEach(e => resWalls.Add(e));
                    columns.ForEach(e => resColumns.Add(e));
                }
                res.Add(resWalls);
                res.Add(resColumns);
            }
            return res;
        }

        /// <summary>
        /// 收集墙柱
        /// </summary>
        public void CollectWallsAndColumns(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            using (var Doclock = Active.Document.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                List<Polyline> frames = new List<Polyline>();

                double minX = Math.Min(viewmodel.SelectedArea[0].X, viewmodel.SelectedArea[2].X);
                double maxX = Math.Max(viewmodel.SelectedArea[0].X, viewmodel.SelectedArea[2].X);
                double minY = Math.Min(viewmodel.SelectedArea[0].Y, viewmodel.SelectedArea[2].Y);
                double maxY = Math.Max(viewmodel.SelectedArea[0].Y, viewmodel.SelectedArea[2].Y);
                Point3d ptmin = new Point3d(minX, minY, 0);
                Point3d ptmax = new Point3d(maxX, maxY, 0);
                Extents3d ex = new Extents3d(ptmin, ptmax);
                frames.Add(ex.ToRectangle());
                var pt = frames.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frames = frames.Select(x =>{return ThMEPFrameService.Normalize(x);}).ToList();
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                List<Polyline> resWalls = new();
                List<Polyline> resColumns = new();
                foreach (var outFrame in frames)
                {
                    getPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);
                    walls.ForEach(e => resWalls.Add(e));
                    columns.ForEach(e => resColumns.Add(e));
                }
                this.CollectedData.WallPolyLines = new List<Polyline>();
                this.CollectedData.ColumnsPolyLines = new List<Polyline>();
                this.CollectedData.WallPolyLines.AddRange(resWalls);
                this.CollectedData.ColumnsPolyLines.AddRange(resColumns);
            }
        }
    }
}