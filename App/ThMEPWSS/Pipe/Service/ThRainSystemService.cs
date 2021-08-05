namespace ThMEPWSS.ReleaseNs.RainSystemNs
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    using ThMEPWSS.ReleaseNs;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
    using static THRainService;
    public class RainGeoData
    {
        public List<Point2d> StoreyContraPoints;
        public List<GRect> Storeys;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> WLines;
        public List<GLineSegment> DLines;
        public List<GLineSegment> VLines;
        public List<GRect> VerticalPipes;
        public List<GRect> WrappingPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<string> WaterPortLabels;
        public List<GRect> CondensePipes;
        public List<GRect> WaterWells;
        public List<string> WaterWellLabels;
        public List<GRect> SideWaterBuckets;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> _87WaterBuckets;
        public List<GRect> RainPortSymbols;
        public List<Point2d> CleaningPorts;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            StoreyContraPoints ??= new List<Point2d>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            CondensePipes ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterWellLabels ??= new List<string>();
            CleaningPorts ??= new List<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
            PipeKillers ??= new List<GRect>();
            RainPortSymbols ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            GravityWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
            WrappingPipeRadius ??= new List<KeyValuePair<Point2d, string>>();
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.IsValid).Distinct().ToList();
            DLines = DLines.Where(x => x.IsValid).Distinct().ToList();
            VLines = VLines.Where(x => x.IsValid).Distinct().ToList();
            WLines = WLines.Where(x => x.IsValid).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterPorts = WaterPorts.Where(x => x.IsValid).Distinct().ToList();
            CondensePipes = CondensePipes.Where(x => x.IsValid).Distinct().ToList();
            {
                var d = new Dictionary<GRect, string>();
                for (int i = QUOTATIONSHAKES; i < WaterWells.Count; i++)
                {
                    var well = WaterWells[i];
                    var label = WaterWellLabels[i];
                    if (!string.IsNullOrWhiteSpace(label) || !d.ContainsKey(well))
                    {
                        d[well] = label;
                    }
                }
                WaterWells.Clear();
                WaterWellLabels.Clear();
                foreach (var kv in d)
                {
                    WaterWells.Add(kv.Key);
                    WaterWellLabels.Add(kv.Value);
                }
            }
            SideWaterBuckets = SideWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            GravityWaterBuckets = GravityWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            _87WaterBuckets = _87WaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            RainPortSymbols = RainPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            PipeKillers = PipeKillers.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipeRadius = WrappingPipeRadius.Distinct().ToList();
        }
        public RainGeoData Clone()
        {
            return (RainGeoData)MemberwiseClone();
        }
        public RainGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<RainGeoData>();
        }
    }
    public class RainCadData
    {
        public List<Geometry> Storeys;
        public List<Geometry> Labels;
        public List<Geometry> LabelLines;
        public List<Geometry> DLines;
        public List<Geometry> VLines;
        public List<Geometry> WLines;
        public List<Geometry> VerticalPipes;
        public List<Geometry> WrappingPipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterPorts;
        public List<Geometry> CondensePipes;
        public List<Geometry> CleaningPorts;
        public List<Geometry> SideFloorDrains;
        public List<Geometry> PipeKillers;
        public List<Geometry> WaterWells;
        public List<Geometry> RainPortSymbols;
        public List<Geometry> SideWaterBuckets;
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> _87WaterBuckets;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            DLines ??= new List<Geometry>();
            VLines ??= new List<Geometry>();
            WLines ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterPorts ??= new List<Geometry>();
            CondensePipes ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
            PipeKillers ??= new List<Geometry>();
            WaterWells ??= new List<Geometry>();
            RainPortSymbols ??= new List<Geometry>();
            SideWaterBuckets ??= new List<Geometry>();
            GravityWaterBuckets ??= new List<Geometry>();
            _87WaterBuckets ??= new List<Geometry>();
        }
        public static RainCadData Create(RainGeoData data)
        {
            var o = new RainCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WLines.AddRange(data.WLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.CondensePipes.AddRange(data.CondensePipes.Select(ConvertWashingMachinesF()));
            o.WaterWells.AddRange(data.WaterWells.Select(ConvertWashingMachinesF()));
            o.RainPortSymbols.AddRange(data.RainPortSymbols.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(ConvertVerticalPipesF()));
            o.SideWaterBuckets.AddRange(data.SideWaterBuckets.Select(ConvertVerticalPipesF()));
            o.GravityWaterBuckets.AddRange(data.GravityWaterBuckets.Select(ConvertVerticalPipesF()));
            o._87WaterBuckets.AddRange(data._87WaterBuckets.Select(ConvertVerticalPipesF()));
            return o;
        }
        private static Func<GRect, Polygon> ConvertWrappingPipesF()
        {
            return x => x.ToPolygon();
        }
        private static Func<GLineSegment, Geometry> ConvertLabelLinesF(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }
        public static Func<Point2d, Point> ConvertSideFloorDrains()
        {
            return x => x.ToNTSPoint();
        }
        public static Func<Point2d, Polygon> ConvertCleaningPortsF()
        {
            return x => new GCircle(x, INTENSIFICATION).ToCirclePolygon(INTERDIGITATING);
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(ADRENOCORTICOTROPHIC).ToCirclePolygon(QUOTATIONSPENSER);
        }
        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }
        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(INTERDIGITATING);
        }
        public static Func<GLineSegment, LineString> ConvertVLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GLineSegment, LineString> ConvertDLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GLineSegment, LineString> ConvertLabelLinesF()
        {
            return x => x.Extend(THESAURUSABANDON).ToLineString();
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(THESAURUSABANDONED);
            ret.AddRange(Storeys);
            ret.AddRange(Labels);
            ret.AddRange(LabelLines);
            ret.AddRange(DLines);
            ret.AddRange(VLines);
            ret.AddRange(WLines);
            ret.AddRange(VerticalPipes);
            ret.AddRange(WrappingPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(CondensePipes);
            ret.AddRange(CleaningPorts);
            ret.AddRange(SideFloorDrains);
            ret.AddRange(PipeKillers);
            ret.AddRange(WaterWells);
            ret.AddRange(RainPortSymbols);
            ret.AddRange(SideWaterBuckets);
            ret.AddRange(GravityWaterBuckets);
            ret.AddRange(_87WaterBuckets);
            return ret;
        }
        public List<RainCadData> SplitByStorey()
        {
            var lst = new List<RainCadData>(this.Storeys.Count);
            if (this.Storeys.Count == QUOTATIONSHAKES) return lst;
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new RainCadData();
                o.Init();
                o.Labels.AddRange(objs.Where(x => this.Labels.Contains(x)));
                o.LabelLines.AddRange(objs.Where(x => this.LabelLines.Contains(x)));
                o.DLines.AddRange(objs.Where(x => this.DLines.Contains(x)));
                o.VLines.AddRange(objs.Where(x => this.VLines.Contains(x)));
                o.WLines.AddRange(objs.Where(x => this.WLines.Contains(x)));
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterPorts.AddRange(objs.Where(x => this.WaterPorts.Contains(x)));
                o.CondensePipes.AddRange(objs.Where(x => this.CondensePipes.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                o.SideFloorDrains.AddRange(objs.Where(x => this.SideFloorDrains.Contains(x)));
                o.PipeKillers.AddRange(objs.Where(x => this.PipeKillers.Contains(x)));
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.RainPortSymbols.AddRange(objs.Where(x => this.RainPortSymbols.Contains(x)));
                o.SideWaterBuckets.AddRange(objs.Where(x => this.SideWaterBuckets.Contains(x)));
                o.GravityWaterBuckets.AddRange(objs.Where(x => this.GravityWaterBuckets.Contains(x)));
                o._87WaterBuckets.AddRange(objs.Where(x => this._87WaterBuckets.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public RainCadData Clone()
        {
            return (RainCadData)MemberwiseClone();
        }
    }
    public class ThRainSystemServiceGeoCollector3
    {
        public AcadDatabase adb;
        public RainGeoData geoData;
        bool isInXref;
        public void CollectEntities()
        {
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                if (entity is BlockReference br)
                {
                    if (!br.BlockTableRecord.IsValid) continue;
                    var btr = adb.Blocks.Element(br.BlockTableRecord);
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    Action f = null;
                    try
                    {
                        isInXref = btr.XrefStatus != XrefStatus.NotAnXref;
                        handleBlockReference(br, Matrix3d.Identity, _fs);
                    }
                    finally
                    {
                        isInXref = THESAURUSABDOMEN;
                    }
                    {
                        var info = br.XClipInfo();
                        if (info.IsValid)
                        {
                            info.TransformBy(br.BlockTransform);
                            var gf = info.PreparedPolygon;
                            foreach (var kv in _fs)
                            {
                                if (gf.Intersects(kv.Key))
                                {
                                    f += kv.Value;
                                }
                            }
                        }
                        else
                        {
                            foreach (var kv in _fs)
                            {
                                f += kv.Value;
                            }
                        }
                        f?.Invoke();
                    }
                }
                else
                {
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    handleEntity(entity, Matrix3d.Identity, _fs);
                    foreach (var kv in _fs)
                    {
                        kv.Value();
                    }
                }
            }
        }
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<GLineSegment> wLines => geoData.WLines;
        List<CText> cts => geoData.Labels;
        List<GRect> waterWells => geoData.WaterWells;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> condensePipes => geoData.CondensePipes;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.CondensePipes;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public void CollectStoreys(CommandContext ctx)
        {
            foreach (var id in ctx.StoreyContext.thStoreys.Select(x => x.ObjectId))
            {
                var br = adb.Element<BlockReference>(id);
                var bd = br.Bounds.ToGRect();
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        public void CollectStoreys(Point3dCollection range)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var br = adb.Element<BlockReference>(item.ObjectId);
                var bd = br.Bounds.ToGRect();
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        const int distinguishDiameter = THESAURUSABSTINENCE;
        private static bool IsWantedBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return THESAURUSABDOMEN;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return THESAURUSABDOMEN;
            }
            if (!blockTableRecord.Explodable)
            {
                return THESAURUSABDOMEN;
            }
            return THESAURUSABDOMINAL;
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<GLineSegment> lst)
        {
            reg(fs, seg, () => { lst.Add(seg); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<GRect> lst)
        {
            reg(fs, r, () => { lst.Add(r); });
        }
        private void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!IsLayerVisible(entity)) return;
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            if (isInXref)
            {
                return;
            }
            {
                if (entity.Layer is ELECTROMAGNETIC)
                {
                    if (entity is Spline)
                    {
                        var bd = entity.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, rainPortSymbols);
                        return;
                    }
                }
            }
            {
                if (entity.Layer is THESAURUSABSORB)
                {
                    if (entity is Line line && line.Length > QUOTATIONSHAKES)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, wLines);
                        return;
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, wLines);
                        }
                        return;
                    }
                }
            }
            {
                static bool f(string layer) => layer is THESAURUSABSENT or THESAURUSABORTIVE or THESAURUSABSORBENT or THESAURUSABRASIVE or QUOTATIONABSORBENT or THESAURUSABSENCE or ELECTROMAGNETIC or CHARACTERISTICALLY or THESAURUSABSORPTION or THESAURUSABUNDANT or THESAURUSABYSMAL;
                if (f(entity.Layer))
                {
                    if (entity is Line line && line.Length > QUOTATIONSHAKES)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, labelLines);
                        return;
                    }
                    else if (entity is DBText dbt && !string.IsNullOrWhiteSpace(dbt.TextString))
                    {
                        var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                        var ct = new CText()
                        {
                            Text = dbt.TextString,
                            Boundary = bd,
                        };
                        reg(fs, ct, cts);
                        return;
                    }
                    else if (entity is Circle c)
                    {
                        if (distinguishDiameter <= c.Radius && c.Radius <= INATTENTIVENESS)
                        {
                            if (f(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                reg(fs, r, pipes);
                                return;
                            }
                        }
                        else if (c.Layer is THESAURUSABSENCE && THESAURUSACCEDE < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                }
            }
            if (dxfName == THESAURUSACCELERATE)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSACCEPT + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>().Where(IsLayerVisible))
                {
                    if (e is Line line)
                    {
                        if (line.Length > QUOTATIONSHAKES)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, labelLines);
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSACCELERATION)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                        continue;
                    }
                }
                if (ts.Count > QUOTATIONSHAKES)
                {
                    GRect bd;
                    if (ts.Count == THESAURUSACCESSION) bd = ts[QUOTATIONSHAKES].Bounds.ToGRect();
                    else
                    {
                        bd = GeoFac.CreateGeometry(ts.Select(x => x.Bounds.ToGRect()).Where(x => x.IsValid).Select(x => x.ToPolygon())).EnvelopeInternal.ToGRect();
                    }
                    bd = bd.TransformBy(matrix);
                    var ct = new CText() { Text = text, Boundary = bd };
                    reg(fs, ct, cts);
                }
                return;
            }
            else if (dxfName == CONVENTIONALIZED)
            {
                if (entity.Layer is THESAURUSABSTRUSE or THESAURUSABSENCE or THESAURUSABSENT)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius > distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                    }
                }
                else if (entity.Layer is THESAURUSABSORB)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wLines);
                    return;
                }
            }
            else if (dxfName == THESAURUSALTHOUGH)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                reg(fs, r, rainPortSymbols);
            }
            else if (dxfName == THESAURUSACCELERATION)
            {
                dynamic o = entity.AcadObject;
                string text = o.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = text, Boundary = r };
                    reg(fs, ct, cts);
                    return;
                }
            }
            else if (dxfName == THESAURUSADULTERATE)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSACCELERATION or QUOTATIONALMAIN).Where(IsLayerVisible))
                    {
                        foreach (var dbText in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                        {
                            var bd = dbText.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbText.TextString, Boundary = bd };
                            reg(fs, ct, cts);
                        }
                    }
                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > QUOTATIONSHAKES).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                    {
                        reg(fs, seg, labelLines);
                    }
                }
                return;
            }
        }
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var name = br.GetEffectiveName();
                if (name.Contains(THESAURUSACCENTUATE) || name.Contains(THESAURUSACCEPTANCE) || name.Contains(ALLITERATIVENESS))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSACCEPT) ?? THESAURUSACCEPTABLE;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(lb);
                    });
                    return;
                }
                if (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name is THESAURUSADMIRATION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSADMONISH)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.Contains(THESAURUSADMIRE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, _87WaterBuckets);
                    return;
                }
                if (ThMEPEngineCore.Service.ThSideEntryWaterBucketLayerManager.IsSideEntryWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name.Contains(ACKNOWLEDGEMENT) || name is ADSIGNIFICATION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, floorDrains);
                    return;
                }
                if (name.Contains(THESAURUSABSTRACT))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < THESAURUSABSTRACTED && bd.Height < THESAURUSABSTRACTED)
                        {
                            reg(fs, bd, wrappingPipes);
                        }
                    }
                    return;
                }
                {
                    if (name is THESAURUSABSTRACTION or THESAURUSABUSIVE)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), INCOMPREHENSIBLE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(THESAURUSABSURD))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if(name is AMBIDEXTROUSNESS || name.Contains(AMBIDEXTROUSNESS))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                }
            }
            var btr = adb.Element<BlockTableRecord>(br.BlockTableRecord);
            if (!IsWantedBlock(btr)) return;
            var _fs = new List<KeyValuePair<Geometry, Action>>();
            foreach (var objId in btr)
            {
                var dbObj = adb.Element<Entity>(objId);
                if (dbObj is BlockReference b)
                {
                    handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                }
                else
                {
                    handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                }
            }
            {
                var lst = new List<KeyValuePair<Geometry, Action>>();
                var info = br.XClipInfo();
                if (info.IsValid)
                {
                    info.TransformBy(br.BlockTransform.PreMultiplyBy(matrix));
                    var gf = info.PreparedPolygon;
                    foreach (var kv in _fs)
                    {
                        if (gf.Intersects(kv.Key))
                        {
                            lst.Add(kv);
                        }
                    }
                }
                else
                {
                    foreach (var kv in _fs)
                    {
                        lst.Add(kv);
                    }
                }
                fs.AddRange(lst);
            }
        }
    }
    public class ThRainSystemServiceGeoCollector2
    {
        public AcadDatabase adb;
        public RainGeoData geoData;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<GLineSegment> wLines => geoData.WLines;
        List<CText> cts => geoData.Labels;
        List<GRect> waterWells => geoData.WaterWells;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> condensePipes => geoData.CondensePipes;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.CondensePipes;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public void CollectStoreys(Point3dCollection range)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var br = adb.Element<BlockReference>(item.ObjectId);
                var bd = br.Bounds.ToGRect();
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        void handleBlockReference(BlockReference br, Matrix3d matrix)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            {
            }
            {
                var name = br.GetEffectiveName();
                if (name.Contains(THESAURUSACCENTUATE) || name.Contains(THESAURUSACCEPTANCE) || name.Contains(ALLITERATIVENESS))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(br.GetAttributesStrValue(THESAURUSACCEPT) ?? THESAURUSACCEPTABLE);
                        return;
                    }
                }
                if (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is THESAURUSADMIRATION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is THESAURUSADMONISH)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(THESAURUSADMIRE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        _87WaterBuckets.Add(bd);
                        return;
                    }
                }
                if (ThMEPEngineCore.Service.ThSideEntryWaterBucketLayerManager.IsSideEntryWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(ACKNOWLEDGEMENT) || name is ADSIGNIFICATION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        floorDrains.Add(bd);
                    }
                    return;
                }
                if (name.Contains(THESAURUSABSTRACT))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < THESAURUSABSTRACTED && bd.Height < THESAURUSABSTRACTED)
                        {
                            wrappingPipes.Add(bd);
                        }
                    }
                    return;
                }
                {
                    if (name is THESAURUSABSTRACTION or THESAURUSABUSIVE)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), INCOMPREHENSIBLE);
                        pipes.Add(bd);
                        return;
                    }
                    if (name.Contains(THESAURUSABSURD))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (bd.IsValid) pipes.Add(bd);
                        return;
                    }
                }
            }
            var btr = adb.Element<BlockTableRecord>(br.BlockTableRecord);
            if (!IsBuildElementBlock(btr)) return;
            foreach (var objId in btr)
            {
                var dbObj = adb.Element<Entity>(objId);
                if (dbObj is BlockReference b)
                {
                    {
                        handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix));
                    }
                }
                else
                {
                    handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix));
                }
            }
        }
        const int distinguishDiameter = THESAURUSABSTINENCE;
        static bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return THESAURUSABDOMEN;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return THESAURUSABDOMEN;
            }
            if (!blockTableRecord.Explodable)
            {
                return THESAURUSABDOMEN;
            }
            return THESAURUSABDOMINAL;
        }
        void handleEntity(Entity entity, Matrix3d matrix)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            {
                if (entity.Layer is THESAURUSABSORB)
                {
                    if (entity is Line line && line.Length > QUOTATIONSHAKES)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        if (seg.IsValid)
                        {
                            wLines.Add(seg);
                        }
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            if (seg.IsValid)
                            {
                                wLines.Add(seg);
                            }
                        }
                    }
                }
                else if (entity.Layer is THESAURUSABSTAIN)
                {
                    if (entity is Line line && line.Length > QUOTATIONSHAKES)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        if (seg.IsValid)
                        {
                            vlines.Add(seg);
                        }
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            if (seg.IsValid)
                            {
                                vlines.Add(seg);
                            }
                        }
                    }
                }
            }
            {
                static bool f(string layer) => layer is THESAURUSABSENT or THESAURUSABORTIVE or THESAURUSABSORBENT or THESAURUSABRASIVE or QUOTATIONABSORBENT or THESAURUSABSENCE or ELECTROMAGNETIC or CHARACTERISTICALLY or THESAURUSABSORPTION or THESAURUSABUNDANT or THESAURUSABYSMAL;
                if (f(entity.Layer))
                {
                    if (entity is Line line && line.Length > QUOTATIONSHAKES)
                    {
                        labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                        return;
                    }
                    else if (entity is DBText dbt && !string.IsNullOrWhiteSpace(dbt.TextString))
                    {
                        var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                        if (bd.IsValid)
                        {
                            cts.Add(new CText()
                            {
                                Text = dbt.TextString,
                                Boundary = bd,
                            });
                            return;
                        }
                    }
                    else if (entity is Circle c)
                    {
                        if (distinguishDiameter <= c.Radius && c.Radius <= INATTENTIVENESS)
                        {
                            if (f(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                if (r.IsValid)
                                {
                                    pipes.Add(r);
                                    return;
                                }
                            }
                        }
                        else if (c.Layer is THESAURUSABSENCE && THESAURUSACCEDE < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            if (r.IsValid)
                            {
                                condensePipes.Add(r);
                                return;
                            }
                        }
                    }
                }
            }
            if (dxfName == THESAURUSACCELERATE)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSACCEPT + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>())
                {
                    if (e is Line line)
                    {
                        if (line.Length > QUOTATIONSHAKES)
                        {
                            labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSACCELERATION)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                        continue;
                    }
                }
                if (ts.Count > QUOTATIONSHAKES)
                {
                    GRect bd;
                    if (ts.Count == THESAURUSACCESSION) bd = ts[QUOTATIONSHAKES].Bounds.ToGRect();
                    else
                    {
                        bd = GeoFac.CreateGeometry(ts.Select(x => x.Bounds.ToGRect()).Where(x => x.IsValid).Select(x => x.ToPolygon())).EnvelopeInternal.ToGRect();
                    }
                    bd = bd.TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        cts.Add(new CText() { Text = text, Boundary = bd });
                    }
                }
                return;
            }
            if (dxfName == CONVENTIONALIZED)
            {
                if (entity.Layer is THESAURUSABSTRUSE or THESAURUSABSENCE)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>())
                    {
                        if (c.Radius > distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid) pipes.Add(bd);
                        }
                    }
                }
                else if (entity.Layer is THESAURUSABSORB)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    if (seg.IsValid)
                    {
                        wLines.Add(seg);
                    }
                }
                else if (entity.Layer is THESAURUSABSTAIN)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    if (seg.IsValid)
                    {
                        vlines.Add(seg);
                    }
                }
                return;
            }
            if (dxfName == THESAURUSALTHOUGH)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                if (r.IsValid)
                {
                    rainPortSymbols.Add(r);
                }
                return;
            }
            if (dxfName == THESAURUSACCELERATION)
            {
                dynamic o = entity.AcadObject;
                string text = o.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    if (r.IsValid)
                    {
                        cts.Add(new CText() { Text = text, Boundary = r });
                    }
                }
                return;
            }
            if (dxfName == THESAURUSADULTERATE)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var ee in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSACCELERATION or QUOTATIONALMAIN))
                    {
                        foreach (var dbText in ee.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)))
                        {
                            var bd = dbText.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid)
                            {
                                cts.Add(new CText() { Text = dbText.TextString, Boundary = bd });
                            }
                        }
                    }
                    labelLines.AddRange(colle.OfType<Line>().Where(x => x.Length > QUOTATIONSHAKES).Select(x => x.ToGLineSegment().TransformBy(matrix)));
                }
                return;
            }
        }
        public void CollectEntities()
        {
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                if (entity is BlockReference br)
                {
                    handleBlockReference(br, Matrix3d.Identity);
                }
                else
                {
                    handleEntity(entity, Matrix3d.Identity);
                }
            }
        }
    }
    public partial class RainDiagram
    {
        public class StoreysItem
        {
            public List<int> Ints;
            public List<string> Labels;
            public void Init()
            {
                Ints ??= new List<int>();
                Labels ??= new List<string>();
            }
        }
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
        }
        public static string GetLabelScore(string label)
        {
            if (label == null) return null;
            if (IsPL(label))
            {
                return THESAURUSACCESS + label;
            }
            if (IsFL(label))
            {
                return APPROACHABILITY + label;
            }
            return label;
        }
        public static int GetStoreyScore(string label)
        {
            if (label == null) return QUOTATIONSHAKES;
            switch (label)
            {
                case INTERCHANGEABLY: return ushort.MaxValue;
                case THESAURUSACCESSIBLE: return ushort.MaxValue + THESAURUSACCESSION;
                case THESAURUSACCESSORY: return ushort.MaxValue + THESAURUSACCIDENT;
                default:
                    {
                        int.TryParse(label.Replace(UNINTENTIONALLY, THESAURUSACCEPTABLE), out int ret);
                        return ret;
                    }
            }
        }
        public static bool CollectRainData(Point3dCollection range, AcadDatabase adb, out List<ThStoreysData> storeysItems, out List<RainDrawingData> drDatas, bool noWL = THESAURUSABDOMEN)
        {
            CollectRainGeoData(range, adb, out storeysItems, out RainGeoData geoData);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CollectRainData(AcadDatabase adb, out List<StoreysItem> storeysItems, out List<RainDrawingData> drDatas, CommandContext ctx, bool noWL = THESAURUSABDOMEN)
        {
            CollectRainGeoData(adb, out storeysItems, out RainGeoData geoData, ctx);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CreateRainDrawingData(AcadDatabase adb, out List<RainDrawingData> drDatas, bool noWL, RainGeoData geoData)
        {
            ThRainService.PreFixGeoData(geoData);
            drDatas = _CreateRainDrawingData(adb, geoData, THESAURUSABDOMINAL);
            return THESAURUSABDOMINAL;
        }
        public static List<RainDrawingData> CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.PreFixGeoData(geoData);
            return _CreateRainDrawingData(adb, geoData, noDraw);
        }
        private static List<RainDrawingData> _CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            GetCadDatas(geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas);
            var roomData = RainService.CollectRoomData(adb);
            RainService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out List<RainDrawingData> drDatas, roomData: roomData);
            if (noDraw) Dispose();
            return drDatas;
        }
        public static void GetCadDatas(RainGeoData geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas)
        {
            cadDataMain = RainCadData.Create(geoData);
            cadDatas = cadDataMain.SplitByStorey();
        }
        public static List<ThStoreysData> GetStoreys(AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.thStoreysDatas;
        }
        public static void CollectRainGeoData(AcadDatabase adb, out List<StoreysItem> storeysItems, out RainGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(adb, geoData, ctx);
        }
        public static List<ThStoreysData> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            foreach (var s in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var e = adb.Element<BlockReference>(s.ObjectId);
                var data = new ThStoreysData()
                {
                    Boundary = e.Bounds.ToGRect(),
                    Storeys = s.Storeys,
                    StoreyType = s.StoreyType,
                };
                Point2d pt = GetContraPoint(e);
                data.ContraPoint = pt;
                storeys.Add(data);
            }
            FixStoreys(storeys);
            return storeys;
        }
        public static void CollectRainGeoData(Point3dCollection range, AcadDatabase adb, out List<ThStoreysData> storeys, out RainGeoData geoData)
        {
            storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData);
        }
        public static List<StoreysItem> GetStoreysItem(List<ThStoreysData> thStoreys)
        {
            var storeysItems = new List<StoreysItem>();
            foreach (var s in thStoreys)
            {
                var item = new StoreysItem();
                storeysItems.Add(item);
                switch (s.StoreyType)
                {
                    case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                        {
                            item.Labels = new List<string>() { INTERCHANGEABLY };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Storeys.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + UNINTENTIONALLY).ToList();
                        }
                        break;
                    default:
                        break;
                }
            }
            return storeysItems;
        }
        public static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        public static void SwapBy2DSpace<T>(ref T v1, ref T v2, GRect bd1, GRect bd2)
        {
            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
            if (deltaY > deltaX)
            {
                if (bd2.MaxY < bd1.MaxY)
                {
                    Swap(ref v1, ref v2);
                }
            }
            else
            {
                if (bd2.MinX < bd1.MinX)
                {
                    Swap(ref v1, ref v2);
                }
            }
        }
        public class ThwSDStoreyItem
        {
            public string Storey;
            public GRect Boundary;
            public List<string> VerticalPipes;
        }
        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }
        static readonly Regex re = new Regex(THESAURUSACCIDENTAL);
        public static bool IsWaterPortLabel(string text)
        {
            return text.Contains(ENTHUSIASTICALLY) && text.Contains(FACESFAVOURABLE);
        }
        public static IEnumerable<KeyValuePair<string, string>> EnumerateSmallRoofVerticalPipes(List<ThwSDStoreyItem> wsdStoreys, string vpipe)
        {
            foreach (var s in wsdStoreys)
            {
                if (s.Storey == THESAURUSACCESSORY || s.Storey == THESAURUSACCESSIBLE)
                {
                    if (s.VerticalPipes.Contains(vpipe))
                    {
                        yield return new KeyValuePair<string, string>(s.Storey, vpipe);
                    }
                }
            }
        }
        public static List<ThwSDStoreyItem> CollectStoreys(List<ThStoreysData> thStoreys, List<RainDrawingData> drDatas)
        {
            var wsdStoreys = new List<ThwSDStoreyItem>();
            HashSet<string> GetVerticalPipeNotes(ThStoreysData storey)
            {
                var i = thStoreys.IndexOf(storey);
                if (i < QUOTATIONSHAKES) return new HashSet<string>();
                return new HashSet<string>(drDatas[i].VerticalPipeLabels.Where(IsRainLabel));
            }
            {
                var largeRoofVPTexts = new HashSet<string>();
                foreach (var storey in thStoreys)
                {
                    var bd = storey.Boundary;
                    switch (storey.StoreyType)
                    {
                        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                            {
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = INTERCHANGEABLY, Boundary = bd, VerticalPipes = GetVerticalPipeNotes(storey).ToList() });
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Storeys.ForEach(i => wsdStoreys.Add(new ThwSDStoreyItem() { Storey = i + UNINTENTIONALLY, Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = thStoreys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == THESAURUSACCESSION)
                    {
                        var storey = storeys[QUOTATIONSHAKES];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        {
                            var rf2vps = smallRoofVPTexts.Except(largeRoofVPTexts).ToList();
                            if (rf2vps.Count == QUOTATIONSHAKES)
                            {
                                var rf1Storey = new ThwSDStoreyItem() { Storey = THESAURUSACCESSIBLE, Boundary = bd, };
                                wsdStoreys.Add(rf1Storey);
                            }
                            else
                            {
                                var rf1vps = smallRoofVPTexts.Except(rf2vps).ToList();
                                var rf1Storey = new ThwSDStoreyItem() { Storey = THESAURUSACCESSIBLE, Boundary = bd, VerticalPipes = rf1vps };
                                wsdStoreys.Add(rf1Storey);
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSACCESSORY, Boundary = bd, VerticalPipes = rf2vps });
                            }
                        }
                    }
                    else if (storeys.Count == THESAURUSACCIDENT)
                    {
                        var s1 = storeys[QUOTATIONSHAKES];
                        var s2 = storeys[THESAURUSACCESSION];
                        var bd1 = s1.Boundary;
                        var bd2 = s2.Boundary;
                        var vpts1 = GetVerticalPipeNotes(s1);
                        var vpts2 = GetVerticalPipeNotes(s2);
                        var vps1 = vpts1.ToList();
                        var vps2 = vpts2.ToList();
                        {
                            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
                            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
                            if (deltaY > deltaX)
                            {
                                if (bd2.MaxY < bd1.MaxY)
                                {
                                    Swap(ref bd1, ref bd2);
                                }
                            }
                            else
                            {
                                if (bd2.MinX < bd1.MinX)
                                {
                                    Swap(ref bd1, ref bd2);
                                }
                            }
                        }
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSACCESSIBLE, Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSACCESSORY, Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
            return wsdStoreys;
        }
#pragma warning disable
        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
        }
        public class PipeCmpInfo : IEquatable<PipeCmpInfo>
        {
            public string label;
            public struct PipeRunCmpInfo
            {
                public int FloorDrainsCount;
                public bool HasLongTranslator;
                public bool HasShortTranslator;
                public bool HasCleaningPort;
                public bool HasWrappingPipe;
            }
            public List<PipeRunCmpInfo> PipeRuns;
            public bool IsWaterPortOutlet;
            public override int GetHashCode()
            {
                return QUOTATIONSHAKES;
            }
            public bool Equals(PipeCmpInfo other)
            {
                return this.IsWaterPortOutlet == other.IsWaterPortOutlet && PipeRuns.SequenceEqual(other.PipeRuns);
            }
        }
        public class WaterBucketInfo
        {
            public string WaterBucket;
            public string Pipe;
            public string Storey;
        }
        public static bool ShowWaterBucketHitting;
        public static List<RainGroupedPipeItem> GetRainGroupedPipeItems(List<RainDrawingData> drDatas, List<ThStoreysData> thStoreys, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            var thwSDStoreys = RainDiagram.CollectStoreys(thStoreys, drDatas);
            var storeysItems = new List<StoreysItem>(drDatas.Count);
            for (int i = QUOTATIONSHAKES; i < drDatas.Count; i++)
            {
                var bd = drDatas[i].Boundary;
                var item = new StoreysItem();
                item.Init();
                foreach (var sd in thwSDStoreys)
                {
                    if (sd.Boundary.EqualsTo(bd, THESAURUSACCLAIM))
                    {
                        item.Ints.Add(GetStoreyScore(sd.Storey));
                        item.Labels.Add(sd.Storey);
                    }
                }
                storeysItems.Add(item);
            }
            var allY1L = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsY1L(x)));
            var allY2L = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsY2L(x)));
            var allNL = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsNL(x)));
            var allYL = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsYL(x)));
            var storeys = thwSDStoreys.Select(x => x.Storey).ToList();
            storeys = storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var minS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Min();
            var maxS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Max();
            var countS = maxS - minS + THESAURUSACCESSION;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            var allNumStoreyLabels = allNumStoreys.Select(x => x + UNINTENTIONALLY).ToList();
            allRfStoreys = storeys.Where(x => !IsNumStorey(x)).OrderBy(GetStoreyScore).ToList();
            bool existStorey(string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return THESAURUSABDOMINAL;
                }
                return THESAURUSABDOMEN;
            }
            int getStoreyIndex(string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return i;
                }
                return -THESAURUSACCESSION;
            }
            var waterBucketsInfos = new List<WaterBucketInfo>();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).OrderBy(GetStoreyScore).ToList();
            string getLowerStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i >= THESAURUSACCESSION) return allStoreys[i - THESAURUSACCESSION];
                return null;
            }
            string getHigherStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i < QUOTATIONSHAKES) return null;
                if (i + THESAURUSACCESSION < allStoreys.Count) return allStoreys[i + THESAURUSACCESSION];
                return null;
            }
            {
                var toCmp = new List<int>();
                for (int i = QUOTATIONSHAKES; i < allStoreys.Count - THESAURUSACCESSION; i++)
                {
                    var s1 = allStoreys[i];
                    var s2 = allStoreys[i + THESAURUSACCESSION];
                    if ((GetStoreyScore(s2) - GetStoreyScore(s1) == THESAURUSACCESSION) || (GetStoreyScore(s1) == maxS && GetStoreyScore(s2) == GetStoreyScore(INTERCHANGEABLY)))
                    {
                        toCmp.Add(i);
                    }
                }
                foreach (var j in toCmp)
                {
                    var storey = allStoreys[j];
                    var i = getStoreyIndex(storey);
                    if (i < QUOTATIONSHAKES) continue;
                    var _drData = drDatas[i];
                    var item = storeysItems[i];
                    var higherStorey = getHigherStorey(storey);
                    if (higherStorey == null) continue;
                    var i1 = getStoreyIndex(higherStorey);
                    if (i1 < QUOTATIONSHAKES) continue;
                    var drData = drDatas[i1];
                    var v = drData.ContraPoint - _drData.ContraPoint;
                    var bkExpand = ACHONDROPLASTIC;
                    var gbks = drData.GravityWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var sbks = drData.SideWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var _87bks = drData._87WaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    if (ShowWaterBucketHitting)
                    {
                        foreach (var bk in gbks.Concat(sbks).Concat(_87bks))
                        {
                            DrawRectLazy(bk);
                            Dr.DrawSimpleLabel(bk.LeftTop, THESAURUSADMONITION);
                        }
                    }
                    var gbkgeos = gbks.Select(x => x.ToPolygon()).ToList();
                    var sbkgeos = sbks.Select(x => x.ToPolygon()).ToList();
                    var _87bkgeos = _87bks.Select(x => x.ToPolygon()).ToList();
                    var gbksf = GeoFac.CreateIntersectsSelector(gbkgeos);
                    var sbksf = GeoFac.CreateIntersectsSelector(sbkgeos);
                    var _87bksf = GeoFac.CreateIntersectsSelector(_87bkgeos);
                    for (int k = QUOTATIONSHAKES; k < _drData.Y1LVerticalPipeRectLabels.Count; k++)
                    {
                        var label = _drData.Y1LVerticalPipeRectLabels[k];
                        var vp = _drData.Y1LVerticalPipeRects[k];
                        {
                            var _gbks = gbksf(vp.ToPolygon());
                            if (_gbks.Count > QUOTATIONSHAKES)
                            {
                                var bk = drData.GravityWaterBucketLabels[gbkgeos.IndexOf(_gbks[QUOTATIONSHAKES])];
                                bk ??= CONGRATULATIONS;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _sbks = sbksf(vp.ToPolygon());
                            if (_sbks.Count > QUOTATIONSHAKES)
                            {
                                var bk = drData.SideWaterBucketLabels[sbkgeos.IndexOf(_sbks[QUOTATIONSHAKES])];
                                bk ??= THESAURUSACCLAMATION;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _8bks = _87bksf(vp.ToPolygon());
                            if (_8bks.Count > QUOTATIONSHAKES)
                            {
                                var bk = drData._87WaterBucketLabels[_87bkgeos.IndexOf(_8bks[QUOTATIONSHAKES])];
                                bk ??= THESAURUSADMISSIBLE;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                    }
                }
            }
            bool testExist(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.VerticalPipeLabels.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasLong(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.LongTranslatorLabels.Contains(label)) return THESAURUSABDOMINAL;
                        }
                    }
                }
                {
                    var _storey = getHigherStorey(storey);
                    if (_storey != null)
                    {
                        for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == _storey)
                                {
                                    var drData = drDatas[i];
                                    if (drData.ConnectedToGravityWaterBucket.Contains(label))
                                    {
                                        return THESAURUSABDOMINAL;
                                    }
                                    if (drData.ConnectedToSideWaterBucket.Contains(label))
                                    {
                                        return THESAURUSABDOMINAL;
                                    }
                                }
                            }
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasShort(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.ShortTranslatorLabels.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            string getWaterWellLabel(string label)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.WaterWellLabels.TryGetValue(label, out string value))
                            {
                                return value;
                            }
                        }
                    }
                }
                return null;
            }
            int getWaterWellId(string label)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.WaterWellIds.TryGetValue(label, out int value))
                            {
                                return value;
                            }
                        }
                    }
                }
                return -THESAURUSACCESSION;
            }
            int getRainPortId(string label)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.RainPortIds.TryGetValue(label, out int value))
                            {
                                return value;
                            }
                        }
                    }
                }
                return -THESAURUSACCESSION;
            }
            bool hasWaterWell(string label)
            {
                return getWaterWellLabel(label) != null;
            }
            bool hasRainPort(string label)
            {
                if (hasWaterWell(label)) return THESAURUSABDOMEN;
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            return drData.HasRainPortSymbols.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasSingleFloorDrainDrainageForRainPort(string label)
            {
                if (IsY1L(label)) return THESAURUSABDOMEN;
                var id = getRainPortId(label);
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForRainPort.Contains(id))
                            {
                                return THESAURUSABDOMINAL;
                            }
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasSingleFloorDrainDrainageForWaterWell(string label)
            {
                if (IsY1L(label)) return THESAURUSABDOMEN;
                var waterWellLabel = getWaterWellLabel(label);
                if (waterWellLabel == null) return THESAURUSABDOMEN;
                var id = getWaterWellId(label);
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterWell.Contains(id))
                            {
                                return THESAURUSABDOMINAL;
                            }
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForRainPort(string label)
            {
                if (!hasSingleFloorDrainDrainageForRainPort(label)) return THESAURUSABDOMEN;
                var id = getRainPortId(label);
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Contains(id))
                            {
                                return THESAURUSABDOMINAL;
                            }
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterWell(label)) return THESAURUSABDOMEN;
                var id = getWaterWellId(label);
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Contains(id))
                            {
                                return THESAURUSABDOMINAL;
                            }
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            int getFDCount(string label, string storey)
            {
                int _getFDCount()
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                drData.FloorDrains.TryGetValue(label, out int v);
                                return v;
                            }
                        }
                    }
                    return QUOTATIONSHAKES;
                }
                var ret = _getFDCount();
                return ret;
            }
            int getFDWrappingPipeCount(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            drData.FloorDrainWrappingPipes.TryGetValue(label, out int v);
                            return v;
                        }
                    }
                }
                return QUOTATIONSHAKES;
            }
            bool hasCondensePipe(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasCondensePipe.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return THESAURUSABDOMEN;
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasBrokenCondensePipes.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasNonBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return THESAURUSABDOMEN;
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasNonBrokenCondensePipes.Contains(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            bool hasWaterWellWrappingPipe(string label)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            return drData.OutletWrappingPipeDict.ContainsValue(label);
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            string getWaterWellWrappingPipeRadius(string label)
            {
                if (!hasWaterWellWrappingPipe(label)) return null;
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == ACCLIMATIZATION)
                        {
                            var drData = drDatas[i];
                            foreach (var kv in drData.OutletWrappingPipeDict)
                            {
                                if (kv.Value == label)
                                {
                                    var id = kv.Key;
                                    drData.WrappingPipeRadiusStringDict.TryGetValue(id, out string v);
                                    return v;
                                }
                            }
                        }
                    }
                }
                return null;
            }
            bool hasSolidPipe(string label, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                        }
                    }
                }
                return THESAURUSABDOMEN;
            }
            string getWaterBucketLabel(string pipe, string storey)
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.ConnectedToGravityWaterBucket.Contains(pipe)) return CONGRATULATIONS;
                            if (drData.ConnectedToSideWaterBucket.Contains(pipe)) return THESAURUSALTERNATIVE;
                        }
                    }
                }
                foreach (var drData in drDatas)
                {
                    foreach (var kv in drData.RoofWaterBuckets)
                    {
                        if (kv.Value == storey && kv.Key == pipe)
                        {
                            return CONGRATULATIONS;
                        }
                    }
                }
                return waterBucketsInfos.FirstOrDefault(x => x.Pipe == pipe && x.Storey == storey)?.WaterBucket;
            }
            WaterBucketItem getWaterBucketItem(string pipe, string storey)
            {
                var bkLabel = getWaterBucketLabel(pipe, storey);
                if (bkLabel == null) return null;
                return WaterBucketItem.TryParse(bkLabel);
            }
            var pipeInfoDict = new Dictionary<string, RainGroupingPipeItem>();
            var y1lGroupingItems = new List<RainGroupingPipeItem>();
            var y2lGroupingItems = new List<RainGroupingPipeItem>();
            var ylGroupingItems = new List<RainGroupingPipeItem>();
            var nlGroupingItems = new List<RainGroupingPipeItem>();
            foreach (var lb in allY1L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.Y1L;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        WaterBucket = getWaterBucketItem(lb, storey),
                    });
                    y1lGroupingItems.Add(item);
                    pipeInfoDict[lb] = item;
                }
            }
            foreach (var lb in allYL)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.YL;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                    });
                    ylGroupingItems.Add(item);
                    pipeInfoDict[lb] = item;
                }
            }
            foreach (var lb in allY2L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.Y2L;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        HasCondensePipe = hasCondensePipe(lb, storey),
                        HasBrokenCondensePipes = hasBrokenCondensePipes(lb, storey),
                        HasNonBrokenCondensePipes = hasNonBrokenCondensePipes(lb, storey),
                        FloorDrainWrappingPipesCount = getFDWrappingPipeCount(lb, storey),
                    });
                    item.CanHaveAringSymbol = item.Hangings.All(x => x.WaterBucket == null);
                    y2lGroupingItems.Add(item);
                    pipeInfoDict[lb] = item;
                }
            }
            foreach (var lb in allNL)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.NL;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        HasCondensePipe = hasCondensePipe(lb, storey),
                        HasBrokenCondensePipes = hasBrokenCondensePipes(lb, storey),
                        HasNonBrokenCondensePipes = hasNonBrokenCondensePipes(lb, storey),
                        FloorDrainWrappingPipesCount = getFDWrappingPipeCount(lb, storey),
                    });
                    item.CanHaveAringSymbol = item.Hangings.All(x => x.WaterBucket == null);
                    nlGroupingItems.Add(item);
                    pipeInfoDict[lb] = item;
                }
            }
            var iRF = allStoreys.IndexOf(INTERCHANGEABLY);
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    item.HasWaterWell = hasWaterWell(label);
                    item.WaterWellLabel = getWaterWellLabel(label);
                    item.HasWaterWellWrappingPipe = hasWaterWellWrappingPipe(label);
                    item.HasRainPort = hasRainPort(label);
                    item.HasSingleFloorDrainDrainageForWaterWell = hasSingleFloorDrainDrainageForWaterWell(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = isFloorDrainShareDrainageWithVerticalPipeForWaterWell(label);
                    item.HasSingleFloorDrainDrainageForRainPort = hasSingleFloorDrainDrainageForRainPort(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForRainPort = isFloorDrainShareDrainageWithVerticalPipeForRainPort(label);
                    item.WaterWellWrappingPipeRadius = getWaterWellWrappingPipeRadius(label);
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (item.Hangings.All(x => x.WaterBucket == null))
                    {
                        for (int i = item.Items.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; i--)
                        {
                            if (item.Items[i].Exist)
                            {
                                var _m = item.Items[i];
                                _m.Exist = THESAURUSABDOMEN;
                                if (Equals(_m, default(RainGroupingPipeItem.ValueItem)))
                                {
                                    if (i < iRF - THESAURUSACCESSION && i > QUOTATIONSHAKES)
                                    {
                                        item.Items[i] = default;
                                    }
                                    if (i < iRF - THESAURUSACCESSION)
                                    {
                                        item.CanHaveAringSymbol = THESAURUSABDOMEN;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = item.Items.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; i--)
                    {
                        if (item.Hangings[i].WaterBucket != null)
                        {
                            {
                                var j = i;
                                var _m = item.Items[j];
                                if (j == iRF)
                                {
                                    if (_m.Exist)
                                    {
                                        item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(QUOTATIONSHAKES, j).Select(k => item.Items[k]).Any(x => x.Exist);
                                    }
                                }
                                item.Items[j] = default;
                            }
                            {
                                for (int j = QUOTATIONSHAKES; j < i; j++)
                                {
                                    item.Hangings[j].WaterBucket = null;
                                }
                                for (int j = i + THESAURUSACCESSION; j < item.Items.Count; j++)
                                {
                                    item.Items[j] = default;
                                }
                            }
                            break;
                        }
                    }
                    if (item.Items.Count > QUOTATIONSHAKES)
                    {
                        var lst = Enumerable.Range(QUOTATIONSHAKES, item.Items.Count).Where(i => item.Items[i].Exist).ToList();
                        if (lst.Count > QUOTATIONSHAKES)
                        {
                            var maxi = lst.Max();
                            var mini = lst.Min();
                            var hasWaterBucket = item.Hangings.Any(x => x.WaterBucket != null);
                            if (hasWaterBucket && (maxi == iRF || maxi == iRF - THESAURUSACCESSION))
                            {
                                item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(QUOTATIONSHAKES, iRF).Select(k => item.Items[k]).Any(x => x.Exist);
                            }
                            else
                            {
                                var (ok, m) = item.Items.TryGetValue(iRF);
                                item.HasLineAtBuildingFinishedSurfice = ok && m.Exist && iRF > QUOTATIONSHAKES && item.Items[iRF - THESAURUSACCESSION].Exist;
                            }
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = QUOTATIONSHAKES; i < item.Hangings.Count; i++)
                    {
                        var storey = allStoreys.TryGet(i);
                        if (storey == ACCLIMATIZATION)
                        {
                            var hanging = item.Hangings[i];
                            if (item.HasWaterWell || item.HasRainPort)
                            {
                                hanging.HasCheckPoint = THESAURUSABDOMINAL;
                            }
                            break;
                        }
                    }
                    for (int i = QUOTATIONSHAKES; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasShort)
                        {
                            item.Hangings[i].HasCheckPoint = THESAURUSABDOMINAL;
                        }
                        if (m.HasLong)
                        {
                            var h = item.Hangings.TryGet(i + THESAURUSACCESSION);
                            if (h != null && (i + THESAURUSACCESSION) != iRF)
                            {
                                h.HasCheckPoint = THESAURUSABDOMINAL;
                            }
                        }
                    }
                }
            }
            var pipeGroupItems = new List<RainGroupedPipeItem>();
            var pipeGroupItems1 = new List<RainGroupedPipeItem>();
            var pipeGroupItems2 = new List<RainGroupedPipeItem>();
            var pipeGroupItems3 = new List<RainGroupedPipeItem>();
            var pipeGroupItems4 = new List<RainGroupedPipeItem>();
            foreach (var g in ylGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWaterWellWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasRainPort = g.Key.HasRainPort,
                    CanHaveAringSymbol = g.Key.CanHaveAringSymbol,
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    WaterWellWrappingPipeRadius = g.Key.WaterWellWrappingPipeRadius,
                };
                pipeGroupItems4.Add(item);
            }
            foreach (var g in y1lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWaterWellWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasRainPort = g.Key.HasRainPort,
                    CanHaveAringSymbol = g.Key.CanHaveAringSymbol,
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    WaterWellWrappingPipeRadius = g.Key.WaterWellWrappingPipeRadius,
                };
                pipeGroupItems1.Add(item);
            }
            foreach (var g in y2lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWaterWellWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasRainPort = g.Key.HasRainPort,
                    CanHaveAringSymbol = g.Key.CanHaveAringSymbol,
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    WaterWellWrappingPipeRadius = g.Key.WaterWellWrappingPipeRadius,
                };
                pipeGroupItems2.Add(item);
            }
            foreach (var g in nlGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWaterWellWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasRainPort = g.Key.HasRainPort,
                    CanHaveAringSymbol = g.Key.CanHaveAringSymbol,
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    WaterWellWrappingPipeRadius = g.Key.WaterWellWrappingPipeRadius,
                };
                pipeGroupItems3.Add(item);
            }
            pipeGroupItems1 = pipeGroupItems1.OrderBy(x => x.Labels.First()).ToList();
            pipeGroupItems2 = pipeGroupItems2.OrderBy(x => x.Labels.First()).ToList();
            pipeGroupItems3 = pipeGroupItems3.OrderBy(x => x.Labels.First()).ToList();
            pipeGroupItems4 = pipeGroupItems4.OrderBy(x => x.Labels.First()).ToList();
            pipeGroupItems.AddRange(pipeGroupItems4);
            pipeGroupItems.AddRange(pipeGroupItems1);
            pipeGroupItems.AddRange(pipeGroupItems2);
            pipeGroupItems.AddRange(pipeGroupItems3);
            return pipeGroupItems;
        }
        public static void DrawRainDiagram(RainSystemDiagramViewModel viewModel, bool focus)
        {
            if (focus) FocusMainWindow();
            if (ThRainService.commandContext == null) return;
            if (ThRainService.commandContext.StoreyContext == null) return;
            if (ThRainService.commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSABDOMINAL))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { THESAURUSACCOMMODATE, THESAURUSABSENCE, QUOTATIONABSORBENT, THESAURUSABSENT, ELECTROMAGNETIC, REPRESENTATIONAL, THESAURUSABSORB, THESAURUSABORTIVE });
                var storeys = ThRainService.commandContext.StoreyContext.thStoreysDatas;
                List<RainDrawingData> drDatas;
                var range = ThRainService.commandContext.range;
                List<ThStoreysData> storeysItems;
                if (range != null)
                {
                    if (!CollectRainData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSABDOMINAL)) return;
                }
                else
                {
                    if (!CollectRainData(adb, out _, out drDatas, ThRainService.commandContext, noWL: THESAURUSABDOMINAL)) return;
                    storeysItems = storeys;
                }
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + UNINTENTIONALLY).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - THESAURUSACCESSION;
                var end = QUOTATIONSHAKES;
                var OFFSET_X = ACCOMMODATINGLY;
                var SPAN_X = ACCOMMODATIVENESS;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSACCOMPANIMENT;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSACCOMPANIMENT;
                Dispose();
                DrawRainDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel);
                FlushDQ(adb);
            }
        }
        public static void DrawRainDiagram()
        {
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return;
            if (!TrySelectPoint(out Point3d point3D)) return;
            var basePoint = point3D.ToPoint2d();
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSABDOMINAL))
            {
                List<ThStoreysData> storeysItems;
                List<RainDrawingData> drDatas;
                if (!CollectRainData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSABDOMINAL)) return;
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                Dispose();
                DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                FlushDQ(adb);
            }
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof)
        {
            var offsetY = canPeopleBeOnRoof ? THESAURUSACCOMPANY : THESAURUSACCOMPLICE;
            DrawAiringSymbol(pt, offsetY);
            Dr.DrawDN_1(pt, QUOTATIONABSORBENT);
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: THESAURUSACCOMPLISH, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: THESAURUSABSORB, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(ACCOMPLISSEMENT, offsetY);
                br.ObjectId.SetDynBlockValue(THESAURUSACCOMPLISHMENT, ACCOMPLISHMENTS);
            });
        }
        private static void DrawDimLabelRight(Point3d basePt, double dy)
        {
            var pt1 = basePt;
            var pt2 = pt1.OffsetY(dy);
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1;
            dim.XLine2Point = pt2;
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(THESAURUSABSTRACTED);
            dim.DimensionText = THESAURUSACCORD;
            dim.Layer = ELECTROMAGNETIC;
            ByLayer(dim);
            DrawEntityLazy(dim);
        }
        public static double CHECKPOINT_OFFSET_Y = THESAURUSACCORDANCE;
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSABORTIVE;
                ByLayer(line);
            });
        }
        public static void DrawStoreyLine(string label, Point2d _basePt, double lineLen)
        {
            var basePt = _basePt.ToPoint3d();
            {
                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, QUOTATIONSHAKES));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: THESAURUSACCORDING, basePt: basePt.OffsetX(CORRESPONDINGLY), layer: THESAURUSACCOMMODATE, props: new Dictionary<string, string>() { { THESAURUSACCORDING, THESAURUSACCEPTABLE } });
            }
            if (label == INTERCHANGEABLY)
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, QUOTATIONSHAKES), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, QUOTATIONSHAKES));
                var dbt = DrawTextLazy(THESAURUSACCORDINGLY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, QUOTATIONSHAKES));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        public static void DrawRainDiagram(Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, RainSystemDiagramViewModel viewModel)
        {
            var _dy = THESAURUSACCOST;
            var __dy = THESAURUSACCOST;
            var dome_lines = new List<GLineSegment>(THESAURUSABANDONED);
            var dome_layer = THESAURUSABSORB;
            void drawDomePipe(GLineSegment seg)
            {
                if (seg.IsValid) dome_lines.Add(seg);
            }
            void drawDomePipes(IEnumerable<GLineSegment> segs)
            {
                dome_lines.AddRange(segs.Where(x => x.IsValid));
            }
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var db = adb.Database;
                static void DrawSegs(List<GLineSegment> segs) { for (int k = QUOTATIONSHAKES; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
                Func<List<Vector2d>> vecs0 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCOUNT - dy + _dy) };
                Func<List<Vector2d>> vecs1 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy + _dy) };
                Func<List<Vector2d>> vecs8 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy + __dy + _dy) };
                Func<List<Vector2d>> vecs11 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy - __dy + _dy) };
                Func<List<Vector2d>> vecs2 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATE - dy + _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
                Func<List<Vector2d>> vecs3 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy + _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
                Func<List<Vector2d>> vecs9 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy + __dy + _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
                Func<List<Vector2d>> vecs13 = () => new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy - __dy + _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
                Func<List<Vector2d>> vecs4 = () => vecs1().GetYAxisMirror();
                Func<List<Vector2d>> vecs5 = () => vecs2().GetYAxisMirror();
                Func<List<Vector2d>> vecs6 = () => vecs3().GetYAxisMirror();
                Func<List<Vector2d>> vecs10 = () => vecs9().GetYAxisMirror();
                Func<List<Vector2d>> vecs12 = () => vecs11().GetYAxisMirror();
                Func<List<Vector2d>> vecs14 = () => vecs13().GetYAxisMirror();
                var vec7 = new Vector2d(-ACCUMULATIVENESS, ACCUMULATIVENESS);
                {
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        DrawStoreyLine(storey, bsPt1, lineLen);
                    }
                }
                for (int j = QUOTATIONSHAKES; j < COUNT; j++)
                {
                    pipeGroupItems.Add(new RainGroupedPipeItem());
                }
                PipeRunLocationInfo[] getPipeRunLocationInfos(Point2d basePoint, ThwPipeLine thwPipeLine, int j)
                {
                    var arr = new PipeRunLocationInfo[allStoreys.Count];
                    for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                    {
                        arr[i] = new PipeRunLocationInfo() { Visible = THESAURUSABDOMINAL, Storey = allStoreys[i], };
                    }
                    {
                        var tdx = THESAURUSACCURACY;
                        for (int i = start; i >= end; i--)
                        {
                            var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + THESAURUSACCESSION) * SPAN_X) + new Vector2d(tdx, QUOTATIONSHAKES);
                            var run = thwPipeLine.PipeRuns.TryGet(i);
                            var storey = allStoreys[i];
                            if (storey == INTERCHANGEABLY)
                            {
                                _dy = ThWSDStorey.RF_OFFSET_Y;
                            }
                            else
                            {
                                _dy = THESAURUSACCOST;
                            }
                            PipeRunLocationInfo drawNormal()
                            {
                                {
                                    var vecs = vecs0();
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    arr[i].HangingEndPoint = arr[i].EndPoint;
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                    arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    segs[QUOTATIONSHAKES] = new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCESSION].StartPoint);
                                    arr[i].RightSegsLast = segs;
                                }
                                {
                                    var pt = arr[i].Segs.First().StartPoint.OffsetX(THESAURUSACCURATE);
                                    var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION))) };
                                    arr[i].RightSegsFirst = segs;
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                                }
                                return arr[i];
                            }
                            if (i == start)
                            {
                                drawNormal().Visible = THESAURUSABDOMEN;
                                continue;
                            }
                            if (run == null)
                            {
                                drawNormal().Visible = THESAURUSABDOMEN;
                                continue;
                            }
                            if (run.HasLongTranslator && run.HasShortTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    {
                                        var vecs = vecs3();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = vecs9().ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(THESAURUSACCOUNTABLE);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs.RemoveAt(THESAURUSACCUSE);
                                        segs.RemoveAt(THESAURUSACCUSTOM);
                                        segs.Add(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOMED].EndPoint.OffsetXY(-THESAURUSACCURATE, -THESAURUSACCURATE)));
                                        segs.Add(new GLineSegment(segs[THESAURUSACCIDENT].EndPoint, new Point2d(segs[THESAURUSACCUSE].EndPoint.X, segs[THESAURUSACCIDENT].EndPoint.Y)));
                                        segs.RemoveAt(THESAURUSACCUSE);
                                        segs.RemoveAt(THESAURUSACCUSTOMED);
                                        segs = new List<GLineSegment>() { segs[THESAURUSACCUSTOMED], new GLineSegment(segs[THESAURUSACCUSTOMED].StartPoint, segs[QUOTATIONSHAKES].StartPoint) };
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                        segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                else
                                {
                                    {
                                        var vecs = vecs6();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = vecs14().ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(-THESAURUSACCOUNTABLE);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))).Offset(ADRENOCORTICOTROPHIC, QUOTATIONSHAKES));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs.RemoveAt(THESAURUSACCUSE);
                                        segs.RemoveAt(THESAURUSACCUSTOM);
                                        segs.Add(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOM].StartPoint));
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                        segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                arr[i].HangingEndPoint = arr[i].Segs[THESAURUSACCUSTOM].EndPoint;
                            }
                            else if (run.HasLongTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    {
                                        var vecs = vecs1();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = vecs8().ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs = segs.Take(THESAURUSACCUSTOM).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOMED].EndPoint.OffsetXY(-THESAURUSACCURATE, -THESAURUSACCURATE))).ToList();
                                        segs.Add(new GLineSegment(segs[THESAURUSACCIDENT].EndPoint, new Point2d(segs[THESAURUSACCUSE].EndPoint.X, segs[THESAURUSACCIDENT].EndPoint.Y)));
                                        segs.RemoveAt(THESAURUSACCUSE);
                                        segs.RemoveAt(THESAURUSACCUSTOMED);
                                        segs = new List<GLineSegment>() { segs[THESAURUSACCUSTOMED], new GLineSegment(segs[THESAURUSACCUSTOMED].StartPoint, segs[QUOTATIONSHAKES].StartPoint) };
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                        segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                else
                                {
                                    {
                                        var vecs = vecs4();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = vecs12().ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))).Offset(ADRENOCORTICOTROPHIC, QUOTATIONSHAKES));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle;
                                        arr[i].RightSegsLast = segs.Take(THESAURUSACCUSTOM).YieldAfter(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSE].StartPoint)).YieldAfter(segs[THESAURUSACCUSE]).ToList();
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                        segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                arr[i].HangingEndPoint = arr[i].EndPoint;
                            }
                            else if (run.HasShortTranslator)
                            {
                                if (run.IsShortTranslatorToLeftOrRight)
                                {
                                    {
                                        var vecs = vecs2();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(THESAURUSACCOUNTABLE);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCIDENT].StartPoint), segs[THESAURUSACCIDENT] };
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var r = new GRect(segs[THESAURUSACCIDENT].StartPoint, segs[THESAURUSACCIDENT].EndPoint);
                                        segs[THESAURUSACCIDENT] = new GLineSegment(r.LeftTop, r.RightButtom);
                                        segs.RemoveAt(QUOTATIONSHAKES);
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, r.RightButtom));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                else
                                {
                                    {
                                        var vecs = vecs5();
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(-THESAURUSACCOUNTABLE);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCIDENT].StartPoint), segs[THESAURUSACCIDENT] };
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        var r = new GRect(segs[THESAURUSACCIDENT].StartPoint, segs[THESAURUSACCIDENT].EndPoint);
                                        segs[THESAURUSACCIDENT] = new GLineSegment(r.LeftTop, r.RightButtom);
                                        segs.RemoveAt(QUOTATIONSHAKES);
                                        segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, r.RightButtom));
                                        arr[i].RightSegsFirst = segs;
                                    }
                                }
                                arr[i].HangingEndPoint = arr[i].Segs[QUOTATIONSHAKES].EndPoint;
                            }
                            else
                            {
                                drawNormal();
                            }
                        }
                    }
                    for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = arr.TryGet(i);
                        if (info != null)
                        {
                            info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                        }
                    }
                    return arr;
                }
                var dx = QUOTATIONSHAKES;
                for (int j = QUOTATIONSHAKES; j < COUNT; j++)
                {
                    var gpItem = pipeGroupItems[j];
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                        } : null;
                        runs.Add(run);
                    }
                    {
                        {
                            bool? flag = null;
                            for (int i = runs.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasLongTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSABDOMINAL;
                                    }
                                    else
                                    {
                                        flag = !flag.Value;
                                    }
                                    r.IsLongTranslatorToLeftOrRight = flag.Value;
                                }
                            }
                        }
                        {
                            bool? flag = null;
                            for (int i = runs.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasShortTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSABDOMINAL;
                                    }
                                    else
                                    {
                                        flag = !flag.Value;
                                    }
                                    r.IsShortTranslatorToLeftOrRight = flag.Value;
                                }
                            }
                        }
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                    {
                        {
                            foreach (var info in arr)
                            {
                                if (info?.Storey == INTERCHANGEABLY)
                                {
                                    if (gpItem.CanHaveAringSymbol)
                                    {
                                        var pt = info.BasePoint;
                                        var seg = new GLineSegment(pt, pt.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                                        drawDomePipe(seg);
                                        DrawAiringSymbol(seg.EndPoint, viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSABDOMEN);
                                    }
                                }
                            }
                        }
                        void _DrawLabel(string text, Point2d basePt, bool leftOrRight, double height)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, height), new Vector2d(leftOrRight ? -DEHYDROGENATION : DEHYDROGENATION, QUOTATIONSHAKES) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForRainNote(lines.ToArray());
                            var t = DrawTextLazy(text, POLYOXYMETHYLENE, segs.Last().EndPoint.OffsetY(INCOMPREHENSIBLE));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        for (int i = end; i <= start; i++)
                        {
                            var storey = allStoreys.TryGet(i);
                            if (storey == null) continue;
                            var run = thwPipeLine.PipeRuns.TryGet(i);
                            if (run == null) continue;
                            var info = arr[i];
                            if (info == null) continue;
                            if (storey == ACCLIMATIZATION) break;
                            {
                                static void DrawLabel(Point3d basePt, string text, double lineYOffset)
                                {
                                    var height = POLYOXYMETHYLENE;
                                    var width = height * THESAURUSALLURE * text.Length;
                                    var yd = new YesDraw();
                                    yd.OffsetXY(QUOTATIONSHAKES, lineYOffset);
                                    yd.OffsetX(-width);
                                    var pts = yd.GetPoint3ds(basePt).ToList();
                                    var lines = DrawLinesLazy(pts);
                                    Dr.SetLabelStylesForRainNote(lines.ToArray());
                                    var t = DrawTextLazy(text, height, pts.Last().OffsetXY(INCOMPREHENSIBLE, INCOMPREHENSIBLE));
                                    Dr.SetLabelStylesForRainNote(t);
                                }
                                var basePt = info.EndPoint;
                                if (storey == INTERCHANGEABLY) basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                DrawLabel(basePt.OffsetY(THESAURUSACQUISITIVE).ToPoint3d(), NEUROTRANSMITTER + storey, storey == INTERCHANGEABLY ? -THESAURUSADDICTION - ThWSDStorey.RF_OFFSET_Y : -THESAURUSADDICTION);
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    var segs = info.DisplaySegs = info.Segs.ToList();
                                    segs[QUOTATIONSHAKES] = new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[QUOTATIONSHAKES].StartPoint.OffsetY(-(segs[QUOTATIONSHAKES].Length - THESAURUSACQUISITIVE)));
                                }
                                break;
                            }
                        }
                        for (int i = start; i >= end; i--)
                        {
                            var storey = allStoreys.TryGet(i);
                            if (storey == null) continue;
                            {
                                var info = arr[i];
                                if (info == null) continue;
                                {
                                    var bk = gpItem.Hangings[i].WaterBucket;
                                    if (bk != null)
                                    {
                                        var basePt = info.EndPoint;
                                        if (storey == INTERCHANGEABLY)
                                        {
                                            basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        switch (bk.WaterBucketType)
                                        {
                                            case WaterBucketEnum.Gravity:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(QUOTATIONSHAKES, THESAURUSACHIEVE).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            case WaterBucketEnum.Side:
                                                {
                                                    var relativeYOffsetToStorey = -COMPETITIVENESS;
                                                    var pt = basePt.OffsetY(relativeYOffsetToStorey);
                                                    Dr.DrawSideWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawSideWaterBucketLabel(pt.OffsetXY(-THESAURUSACHIEVEMENT, INVULNERABILITY).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            case WaterBucketEnum._87:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(QUOTATIONSHAKES, THESAURUSACHIEVE).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            default:
                                                throw new System.Exception();
                                        }
                                    }
                                }
                                {
                                    if (storey == ACCLIMATIZATION)
                                    {
                                        var basePt = info.EndPoint;
                                        if (gpItem.WaterWellWrappingPipeRadius != null)
                                        {
                                            var p1 = basePt + new Vector2d(-ACRIMONIOUSNESS, -THESAURUSADVERSITY);
                                            var p2 = p1.OffsetY(-THESAURUSADVERTISE);
                                            var p3 = p2.OffsetX(ACCOMMODATIONAL);
                                            var layer = THESAURUSABSENCE;
                                            DrawLine(layer, new GLineSegment(p1, p2));
                                            DrawLine(layer, new GLineSegment(p3, p2));
                                            DrawStoreyHeightSymbol(p3, THESAURUSABSENCE, gpItem.WaterWellWrappingPipeRadius);
                                        }
                                        if (gpItem.HasWaterWell)
                                        {
                                            void _DrawRainWaterWells(Point2d pt, List<string> values)
                                            {
                                                if (values == null) return;
                                                values = values.OrderBy(x =>
                                                {
                                                    long.TryParse(x, out long v);
                                                    return v;
                                                }).ThenBy(x => x).ToList();
                                                if (values.Count == THESAURUSACCESSION)
                                                {
                                                    DrawRainWaterWell(pt, values[QUOTATIONSHAKES]);
                                                }
                                                else if (values.Count >= THESAURUSACCIDENT)
                                                {
                                                    var pts = GetBasePoints(pt.OffsetX(-ACHLOROPHYLLOUS), THESAURUSACCIDENT, values.Count, ACHLOROPHYLLOUS, ACHLOROPHYLLOUS).ToList();
                                                    for (int i = QUOTATIONSHAKES; i < values.Count; i++)
                                                    {
                                                        DrawRainWaterWell(pts[i], values[i]);
                                                    }
                                                }
                                            }
                                            {
                                                var fixY = -ACCOMMODATIONAL;
                                                var v = new Vector2d(-ACHONDROPLASIAC - ACHONDROPLASTIC, -UNCHRONOLOGICAL + QUOTATIONCARLYLE + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                _DrawRainWaterWells(pt, values);
                                                var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, fixY), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-ACKNOWLEDGEABLE, QUOTATIONSHAKES), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    drawDomePipes(segs);
                                                    if (gpItem.HasWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSACKNOWLEDGE);
                                                        if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                                        {
                                                            if (!gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSADDICTION).ToPoint3d());
                                                            }
                                                            else
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSADDICTION).ToPoint3d());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DrawWrappingPipe(p.ToPoint3d());
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                            {
                                                var fixX = -ACQUISITIVENESS - ADVERTISEMENTAL;
                                                var fixY = ACHLOROPHYLLOUS;
                                                var fixV = new Vector2d(-THESAURUSADVANTAGE, -THESAURUSACQUAINTED);
                                                var p = basePt + new Vector2d(ADVANTAGEOUSNESS + fixX, -THESAURUSADVANTAGEOUS);
                                                DrawFloorDrain(p.ToPoint3d(), THESAURUSABDOMINAL);
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSADVENT + fixY), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-QUOTATIONSECOND, QUOTATIONSHAKES), new Vector2d(-THESAURUSADVENTITIOUS, THESAURUSADVENTURE) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSADVENT + fixY), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-ADVENTUROUSNESS - fixX, QUOTATIONSHAKES) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                }
                                            }
                                        }
                                        else if (gpItem.HasRainPort)
                                        {
                                            {
                                                var fixY = ACHLOROPHYLLOUS;
                                                var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSACKNOWLEDGEMENT + fixY), new Vector2d(-THESAURUSACOLYTE, -THESAURUSACOLYTE), new Vector2d(-THESAURUSACQUAINT, QUOTATIONSHAKES) };
                                                var segs = vecs.ToGLineSegments(basePt);
                                                drawDomePipes(segs);
                                                var pt = segs.Last().EndPoint.ToPoint3d();
                                                {
                                                    Dr.DrawRainPort(pt.OffsetX(ACHONDROPLASTIC));
                                                    Dr.DrawRainPortLabel(pt.OffsetX(-INCOMPREHENSIBLE));
                                                    Dr.DrawStarterPipeHeightLabel(pt.OffsetX(-INCOMPREHENSIBLE + ACQUAINTANCESHIP));
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort)
                                            {
                                                var fixX = -ACQUISITIVENESS - ADVERTISEMENTAL;
                                                var fixY = ACHLOROPHYLLOUS;
                                                var fixV = new Vector2d(-THESAURUSADVANTAGE, -THESAURUSACQUAINTED);
                                                var p = basePt + new Vector2d(ADVANTAGEOUSNESS + fixX, -THESAURUSADVANTAGEOUS);
                                                DrawFloorDrain(p.ToPoint3d(), THESAURUSABDOMINAL);
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSADVENT + fixY), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-QUOTATIONSECOND, QUOTATIONSHAKES), new Vector2d(-THESAURUSADVENTITIOUS, THESAURUSADVENTURE) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSADVENT + fixY), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-ADVENTUROUSNESS - fixX, QUOTATIONSHAKES) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var ditchDy = INATTENTIVENESS;
                                            var _run = runs.TryGet(i);
                                            if (_run != null)
                                            {
                                                if (!_run.HasLongTranslator && !_run.HasShortTranslator)
                                                {
                                                    _dy = INATTENTIVENESS;
                                                    var vecs = vecs0();
                                                    var p1 = info.StartPoint;
                                                    var p2 = p1 + new Vector2d(QUOTATIONSHAKES, -THESAURUSACCOUNT - dy + _dy);
                                                    var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                    info.DisplaySegs = segs;
                                                    var p = basePt.OffsetY(INATTENTIVENESS);
                                                    drawLabel(p, THESAURUSACQUAINTANCE, null, THESAURUSABDOMEN);
                                                    static void DrawDimLabelRight(Point3d basePt, double dy)
                                                    {
                                                        var pt1 = basePt;
                                                        var pt2 = pt1.OffsetY(dy);
                                                        var dim = new AlignedDimension();
                                                        dim.XLine1Point = pt1;
                                                        dim.XLine2Point = pt2;
                                                        dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-ACHONDROPLASTIC);
                                                        dim.DimensionText = THESAURUSALLOWANCE;
                                                        dim.Layer = ELECTROMAGNETIC;
                                                        ByLayer(dim);
                                                        DrawEntityLazy(dim);
                                                    }
                                                    DrawDimLabelRight(p.ToPoint3d(), -INATTENTIVENESS);
                                                }
                                                else
                                                {
                                                    Dr.DrawLabel(basePt.ToPoint3d(), THESAURUSACQUAINTANCE);
                                                }
                                            }
                                        }
                                    }
                                }
                                void _DrawFloorDrain(Point2d basePt, bool leftOrRight)
                                {
                                    if (leftOrRight)
                                    {
                                        DrawFloorDrain(basePt.OffsetXY(THESAURUSACQUAINTED + THESAURUSACCEDE, THESAURUSACQUAINTED).ToPoint3d(), leftOrRight);
                                    }
                                    else
                                    {
                                        DrawFloorDrain(basePt.OffsetXY(THESAURUSACQUAINTED + THESAURUSACCEDE - THESAURUSACQUIESCE, THESAURUSACQUAINTED).ToPoint3d(), leftOrRight);
                                    }
                                    return;
                                }
                                {
                                    void drawDN(string dn, Point2d pt)
                                    {
                                        var t = DrawTextLazy(dn, POLYOXYMETHYLENE, pt);
                                        Dr.SetLabelStylesForRainDims(t);
                                    }
                                    var hanging = gpItem.Hangings[i];
                                    var fixW = UNCHRONOLOGICAL;
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSACQUIESCENCE, THESAURUSACQUIESCENCE), new Vector2d(-THESAURUSACQUIESCENT - fixW, QUOTATIONSHAKES) };
                                    if (getHasAirConditionerFloorDrain(i))
                                    {
                                        var p1 = info.StartPoint.OffsetY(-ACHLOROPHYLLOUS);
                                        var p2 = p1.OffsetX(ACHONDROPLASIAC);
                                        var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                                        Dr.SetLabelStylesForWNote(line);
                                        var segs = vecs.GetYAxisMirror().ToGLineSegments(p1.OffsetY(-THESAURUSACQUIRE - THESAURUSACQUIREMENT));
                                        drawDomePipes(segs);
                                        var p = segs.Last().EndPoint;
                                        _DrawFloorDrain(p, THESAURUSABDOMINAL);
                                        drawDN(THESAURUSACQUISITION, segs[THESAURUSACCESSION].StartPoint.OffsetXY(INATTENTIVENESS + fixW, INATTENTIVENESS));
                                    }
                                    if (hanging.FloorDrainsCount > QUOTATIONSHAKES)
                                    {
                                        var bsPt = info.EndPoint;
                                        string getDN()
                                        {
                                            const string dft = THESAURUSACQUISITION;
                                            if (gpItem.PipeType == PipeType.Y2L) return viewModel?.Params.BalconyFloorDrainDN ?? dft;
                                            if (gpItem.PipeType == PipeType.NL) return viewModel?.Params.CondensePipeHorizontalDN ?? dft;
                                            return dft;
                                        }
                                        var wpCount = hanging.FloorDrainWrappingPipesCount;
                                        void tryDrawWrappingPipe(Point2d pt)
                                        {
                                            if (wpCount <= QUOTATIONSHAKES) return;
                                            DrawWrappingPipe(pt.ToPoint3d());
                                            --wpCount;
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSACCESSION)
                                        {
                                            if (!(storey == ACCLIMATIZATION && (gpItem.HasSingleFloorDrainDrainageForWaterWell || gpItem.HasSingleFloorDrainDrainageForRainPort)))
                                            {
                                                var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSACQUIRE - THESAURUSACQUIREMENT));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _DrawFloorDrain(p, THESAURUSABDOMINAL);
                                                tryDrawWrappingPipe(p.OffsetX(ACQUISITIVENESS));
                                                drawDN(getDN(), segs[THESAURUSACCESSION].EndPoint.OffsetXY(INATTENTIVENESS + fixW, INATTENTIVENESS));
                                            }
                                        }
                                        else if (hanging.FloorDrainsCount == THESAURUSACCIDENT)
                                        {
                                            {
                                                var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSACQUIRE - THESAURUSACQUIREMENT));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _DrawFloorDrain(p, THESAURUSABDOMINAL);
                                                tryDrawWrappingPipe(p.OffsetX(ACQUISITIVENESS));
                                                drawDN(getDN(), segs[THESAURUSACCESSION].EndPoint.OffsetXY(INATTENTIVENESS + fixW, INATTENTIVENESS));
                                            }
                                            {
                                                var segs = vecs.GetYAxisMirror().ToGLineSegments(info.EndPoint.OffsetY(-THESAURUSACQUIRE - THESAURUSACQUIREMENT));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _DrawFloorDrain(p, THESAURUSABDOMINAL);
                                                tryDrawWrappingPipe(p.OffsetX(-THESAURUSACOLYTE));
                                                drawDN(getDN(), segs[THESAURUSACCESSION].StartPoint.OffsetXY(INATTENTIVENESS + fixW, INATTENTIVENESS));
                                            }
                                        }
                                    }
                                    if (hanging.HasCondensePipe)
                                    {
                                        string getCondensePipeDN()
                                        {
                                            return viewModel?.Params.CondensePipeHorizontalDN ?? THESAURUSACQUISITION;
                                        }
                                        if (hanging.HasBrokenCondensePipes)
                                        {
                                            void f(double offsetY)
                                            {
                                                var segs = vecs.ToGLineSegments(info.StartPoint.OffsetY(offsetY));
                                                var h = THESAURUSACQUISITIVE;
                                                var w = ACCOMMODATIONAL;
                                                var p1 = segs.Last().EndPoint;
                                                var p3 = p1.OffsetY(h);
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                Dr.DrawCondensePipe(p3.OffsetXY(-INATTENTIVENESS, INATTENTIVENESS));
                                                drawDomePipes(segs);
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(INATTENTIVENESS + fixW, -INCOMPREHENSIBLE));
                                            }
                                            f(-THESAURUSACQUIT);
                                            f(-THESAURUSACQUIT - THESAURUSACQUITTAL);
                                        }
                                        else
                                        {
                                            var segs = vecs.ToGLineSegments(info.StartPoint.OffsetY(-THESAURUSACQUIT));
                                            var h = THESAURUSACQUISITIVE;
                                            var w = ACCOMMODATIONAL;
                                            var p1 = segs.Last().EndPoint;
                                            var p2 = p1.OffsetX(w);
                                            var p3 = p1.OffsetY(h);
                                            var p4 = p2.OffsetY(h);
                                            drawDomePipes(segs);
                                            if (hanging.HasNonBrokenCondensePipes)
                                            {
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(INATTENTIVENESS + fixW, -INCOMPREHENSIBLE));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                Dr.DrawCondensePipe(p3);
                                                drawDomePipe(new GLineSegment(p2, p4));
                                                Dr.DrawCondensePipe(p4);
                                            }
                                            else
                                            {
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(INATTENTIVENESS + fixW, -INCOMPREHENSIBLE));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                Dr.DrawCondensePipe(p3);
                                            }
                                        }
                                    }
                                }
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                if (run == null) continue;
                                if (run.HasShortTranslator)
                                {
                                    DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                                }
                                if (gpItem.Hangings[i].HasCheckPoint)
                                {
                                    var p = run.HasShortTranslator ? info.Segs.Last().StartPoint.OffsetY(ACHLOROPHYLLOUS).ToPoint3d() : info.EndPoint.OffsetY(ACHLOROPHYLLOUS).ToPoint3d();
                                    DrawCheckPoint(p, THESAURUSABDOMINAL);
                                    var seg = info.Segs.Last();
                                    var fixDy = run.HasShortTranslator ? -seg.Height : QUOTATIONSHAKES;
                                    DrawDimLabelRight(p, +fixDy - ACHLOROPHYLLOUS);
                                }
                            }
                        }
                    }
                    bool getHasAirConditionerFloorDrain(int i)
                    {
                        if (gpItem.PipeType != PipeType.Y2L) return THESAURUSABDOMEN;
                        var hanging = gpItem.Hangings[i];
                        if (hanging.HasBrokenCondensePipes || hanging.HasNonBrokenCondensePipes)
                        {
                            return viewModel?.Params.HasAirConditionerFloorDrain ?? THESAURUSABDOMEN;
                        }
                        return THESAURUSABDOMEN;
                    }
                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);
                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight, double height = POLYOXYMETHYLENE)
                    {
                        var gap = INCOMPREHENSIBLE;
                        var factor = PHARMACEUTICALS;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? QUOTATIONSHAKES, text2?.Length ?? QUOTATIONSHAKES) + INATTENTIVENESS;
                        if (width < ACRIMONIOUSNESS) width = ACRIMONIOUSNESS;
                        var vecs = new List<Vector2d> { new Vector2d(ACHONDROPLASTIC, ACHONDROPLASTIC), new Vector2d(width, QUOTATIONSHAKES) };
                        if (isLeftOrRight == THESAURUSABDOMINAL)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[THESAURUSACCESSION].EndPoint : segs[THESAURUSACCESSION].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(gap);
                        if (text1 != null)
                        {
                            var t = DrawTextLazy(text1, height, txtBasePt);
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DrawTextLazy(text2, height, txtBasePt.OffsetY(-height - gap));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                    }
                    static void drawLabel2(Point2d basePt, string text1, string text2, bool isLeftOrRight)
                    {
                        var height = POLYOXYMETHYLENE;
                        var gap = INCOMPREHENSIBLE;
                        var factor = PHARMACEUTICALS;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? QUOTATIONSHAKES, text2?.Length ?? QUOTATIONSHAKES);
                        if (width < ACRIMONIOUSNESS) width = ACRIMONIOUSNESS;
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSALLIED, QUOTATIONSHAKES), new Vector2d(width, QUOTATIONSHAKES) };
                        if (isLeftOrRight == THESAURUSABDOMINAL)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[THESAURUSACCESSION].EndPoint : segs[THESAURUSACCESSION].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(gap);
                        if (text1 != null)
                        {
                            var t = DrawTextLazy(text1, height, txtBasePt);
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DrawTextLazy(text2, height, txtBasePt.OffsetY(-height - gap));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                    }
                    for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                    {
                        var info = arr.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            var segs = info.DisplaySegs ?? info.Segs;
                            if (segs != null)
                            {
                                drawDomePipes(segs);
                            }
                        }
                        var storey = allStoreys[i];
                        if (storey == INTERCHANGEABLY && gpItem.HasLineAtBuildingFinishedSurfice)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                            drawDomePipe(new GLineSegment(p1, p2));
                        }
                    }
                    {
                        var has_label_storeys = new HashSet<string>();
                        {
                            var _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSACCIDENT), allNumStoreyLabels.GetLastOrDefault(THESAURUSACCUSTOMED) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == QUOTATIONSHAKES)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            _storeys = _storeys.Where(storey =>
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = arr.TryGet(i);
                                return info != null && info.Visible;
                            }).ToList();
                            if (_storeys.Count == QUOTATIONSHAKES)
                            {
                                _storeys = allNumStoreyLabels.Where(storey =>
                                {
                                    var i = allNumStoreyLabels.IndexOf(storey);
                                    var info = arr.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(THESAURUSACCESSION).ToList();
                            }
                            if (_storeys.Count == QUOTATIONSHAKES)
                            {
                                _storeys = allStoreys.Where(storey =>
                                {
                                    var i = allStoreys.IndexOf(storey);
                                    var info = arr.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(THESAURUSACCESSION).ToList();
                            }
                            foreach (var storey in _storeys)
                            {
                                has_label_storeys.Add(storey);
                                var i = allStoreys.IndexOf(storey);
                                var info = arr[i];
                                {
                                    string label1, label2;
                                    var labels = RainLabelItem.ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).ToList();
                                    if (labels.Count == THESAURUSACCIDENT)
                                    {
                                        label1 = labels[QUOTATIONSHAKES];
                                        label2 = labels[THESAURUSACCESSION];
                                    }
                                    else
                                    {
                                        label1 = labels.JoinWith(THESAURUSACRIMONIOUS);
                                        label2 = null;
                                    }
                                    var isLeftOrRight = (gpItem.Hangings.TryGet(i)?.FloorDrainsCount ?? QUOTATIONSHAKES) == QUOTATIONSHAKES && !(gpItem.Hangings.TryGet(i)?.HasCondensePipe ?? THESAURUSABDOMEN);
                                    {
                                        var run = runs.TryGet(i);
                                        if (run != null)
                                        {
                                            if (run.HasLongTranslator)
                                            {
                                                isLeftOrRight = run.IsLongTranslatorToLeftOrRight;
                                            }
                                        }
                                        var (ok, item) = gpItem.Items.TryGetValue(i);
                                        if (ok)
                                        {
                                            if (item.HasShort)
                                            {
                                                isLeftOrRight = THESAURUSABDOMEN;
                                            }
                                        }
                                    }
                                    if (getHasAirConditionerFloorDrain(i) && isLeftOrRight == THESAURUSABDOMEN)
                                    {
                                        var pt = info.EndPoint.OffsetY(THESAURUSACQUISITIVE);
                                        if (storey == INTERCHANGEABLY)
                                        {
                                            pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        drawLabel2(pt, label1, label2, isLeftOrRight);
                                    }
                                    else
                                    {
                                        var pt = info.PlBasePt;
                                        if (storey == INTERCHANGEABLY)
                                        {
                                            pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        drawLabel(pt, label1, label2, isLeftOrRight);
                                    }
                                }
                            }
                        }
                        {
                            var _allSmoothStoreys = new List<string>();
                            {
                                bool isMinFloor = THESAURUSABDOMINAL;
                                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
                                {
                                    var (ok, item) = gpItem.Items.TryGetValue(i);
                                    if (!(ok && item.Exist)) continue;
                                    if (item.Exist && isMinFloor)
                                    {
                                        isMinFloor = THESAURUSABDOMEN;
                                        continue;
                                    }
                                    var storey = allNumStoreyLabels[i];
                                    if (has_label_storeys.Contains(storey)) continue;
                                    var run = runs.TryGet(i);
                                    if (run == null) continue;
                                    if (!run.HasLongTranslator && !run.HasShortTranslator && (!(gpItem.Hangings.TryGet(i)?.HasCheckPoint ?? THESAURUSABDOMEN)))
                                    {
                                        _allSmoothStoreys.Add(storey);
                                    }
                                }
                            }
                            var _storeys = new string[] { _allSmoothStoreys.GetAt(QUOTATIONSHAKES), _allSmoothStoreys.GetLastOrDefault(THESAURUSACCESSION) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == QUOTATIONSHAKES)
                            {
                                _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSACCESSION), allNumStoreyLabels.GetLastOrDefault(THESAURUSACCIDENT) }.SelectNotNull().Distinct().ToList();
                            }
                            if (_storeys.Count == QUOTATIONSHAKES)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            const string dft = ACETYLSALICYLIC;
                            var dn = gpItem.PipeType switch
                            {
                                PipeType.Y2L => viewModel?.Params.BalconyRainPipeDN,
                                PipeType.NL => viewModel?.Params.CondensePipeVerticalDN,
                                _ => dft,
                            };
                            dn ??= dft;
                            foreach (var storey in _storeys)
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = arr.TryGet(i);
                                if (info != null && info.Visible)
                                {
                                    var run = runs.TryGet(i);
                                    if (run != null)
                                    {
                                        Dr.DrawDN_2(info.EndPoint.OffsetX(THESAURUSADDICTION), QUOTATIONABSORBENT, dn);
                                    }
                                }
                            }
                        }
                    }
                }
                {
                    var auto_conn = THESAURUSABDOMINAL;
                    if (auto_conn)
                    {
                        foreach (var g in GeoFac.GroupParallelLines(dome_lines, THESAURUSACCESSION, THESAURUSACRIMONY))
                        {
                            var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSACTING));
                            line.Layer = dome_layer;
                            ByLayer(line);
                        }
                    }
                    else
                    {
                        foreach (var dome_line in dome_lines)
                        {
                            var line = DrawLineSegmentLazy(dome_line);
                            line.Layer = dome_layer;
                            ByLayer(line);
                        }
                    }
                }
            }
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = QUOTATIONABSORBENT;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = PHARMACEUTICALS;
                    SetTextStyleLazy(t, ACTINOMYCETALES);
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-ACHLOROPHYLLOUS, THESAURUSABSTRACTED), new Vector2d(-THESAURUSACTION, QUOTATIONSHAKES) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[THESAURUSACCESSION].EndPoint : segs[THESAURUSACCESSION].StartPoint;
            var text = THESAURUSACTIVATE;
            var height = POLYOXYMETHYLENE;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: CHARACTERISTICS, basePt: basePt,
      cb: br =>
      {
          if (leftOrRight)
          {
              br.ScaleFactors = new Scale3d(-THESAURUSACCESSION, THESAURUSACCESSION, THESAURUSACCESSION);
          }
          ByLayer(br);
          br.Layer = THESAURUSABSENT;
      });
        }
        public static void DrawRainDiagram(List<RainDrawingData> drDatas, List<ThStoreysData> storeysItems, Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + UNINTENTIONALLY).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - THESAURUSACCESSION;
            var end = QUOTATIONSHAKES;
            var OFFSET_X = ACCOMMODATINGLY;
            var SPAN_X = ACCOMMODATIVENESS;
            var HEIGHT = THESAURUSACCOMPANIMENT;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSACCOMPANIMENT;
            DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, null);
        }
        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSABORTIVE;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = PHARMACEUTICALS;
                    SetTextStyleLazy(t, ACTINOMYCETALES);
                }
            }
        }
        public static bool Testing;
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference(blkName: THESAURUSACTIVE, basePt: basePt.OffsetXY(-THESAURUSACQUITTAL, QUOTATIONSHAKES), cb: br =>
            {
                SetLayerAndByLayer(REPRESENTATIONAL, br);
                if (br.IsDynamicBlock)
                {
                    br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSACTUAL);
                }
            });
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = QUOTATIONSHAKES, j = QUOTATIONSHAKES;
            for (int k = QUOTATIONSHAKES; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = QUOTATIONSHAKES;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = QUOTATIONSHAKES, j = QUOTATIONSHAKES;
            for (int k = QUOTATIONSHAKES; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, QUOTATIONSHAKES);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = QUOTATIONSHAKES;
                }
            }
        }
        public static void DrawRainWaterWell(Point2d basePt, string value)
        {
            DrawRainWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: THESAURUSACTUALLY, basePt: basePt.OffsetY(-ACHONDROPLASTIC),
            props: new Dictionary<string, string>() { { THESAURUSACCEPT, value } },
            cb: br =>
            {
                br.Layer = THESAURUSABSENCE;
                ByLayer(br);
            });
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = THESAURUSACTUATE)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSACUMEN, basePt, br =>
                {
                    br.Layer = THESAURUSABSENT;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, value);
                    }
                });
            }
            else
            {
                DrawBlockReference(THESAURUSACUMEN, basePt,
               br =>
               {
                   br.Layer = THESAURUSABSENT;
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, value);
                   }
               });
            }
        }
        public static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = ACUPUNCTURATION)
        {
            DrawBlockReference(blkName: THESAURUSACCORDING, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSACCORDING, label } }, cb: br => { ByLayer(br); });
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = THESAURUSABSTAIN;
            ByLayer(line);
        }
        public static void SetRainPipeLineStyle(Line line)
        {
            line.Layer = THESAURUSABSORB;
            ByLayer(line);
        }
        public static void DrawLine(string layer, params GLineSegment[] segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            foreach (var line in lines)
            {
                line.Layer = layer;
                ByLayer(line);
            }
        }
        public static void DrawRainPipes(params GLineSegment[] segs)
        {
            DrawRainPipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawRainPipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line => SetRainPipeLineStyle(line));
        }
    }
    public class RainLabelItem
    {
        public string Label;
        public string Prefix;
        public string D1S;
        public string D2S;
        public string Suffix;
        public int D1
        {
            get
            {
                int.TryParse(D1S, out int r); return r;
            }
        }
        public int D2
        {
            get
            {
                int.TryParse(D2S, out int r); return r;
            }
        }
        static readonly Regex re = new Regex(THESAURUSALREADY);
        public static RainLabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new RainLabelItem()
            {
                Label = label,
                Prefix = m.Groups[THESAURUSACCESSION].Value,
                D1S = m.Groups[THESAURUSACCIDENT].Value,
                D2S = m.Groups[THESAURUSACCUSTOMED].Value,
                Suffix = m.Groups[THESAURUSACCUSTOM].Value,
            };
        }
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
        {
            var items = pipeIds.Select(id => RainLabelItem.Parse(id)).Where(m => m != null);
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2S?.Length ?? QUOTATIONSHAKES).ThenBy(x => x.D2).ThenBy(x => x.D2S).ToList());
            foreach (var g in gs)
            {
                if (g.Count == THESAURUSACCESSION)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > THESAURUSACCIDENT && g.Count == g.Last().D2 - g.First().D2 + THESAURUSACCESSION)
                {
                    var m = g.First();
                    yield return $"{m.Prefix}{m.D1S}-{g.First().D2S}{m.Suffix}~{g.Last().D2S}{m.Suffix}";
                }
                else
                {
                    var sb = new StringBuilder();
                    {
                        var m = g.First();
                        sb.Append($"{m.Prefix}{m.D1S}-");
                    }
                    for (int i = QUOTATIONSHAKES; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - THESAURUSACCESSION)
                        {
                            sb.Append(THESAURUSADAPTABLE);
                        }
                    }
                    yield return sb.ToString();
                }
            }
            foreach (var r in rest)
            {
                yield return r;
            }
        }
    }
    public class Hanging
    {
        public int FloorDrainsCount;
        public bool IsSeries;
        public bool HasSCurve;
        public bool HasDoubleSCurve;
        public bool HasUnderBoardLabel;
        public bool IsFL0;
    }
    public class ThwOutput
    {
        public int LinesCount = THESAURUSACCESSION;
        public List<string> DirtyWaterWellValues;
        public bool HasVerticalLine2;
        public bool HasWrappingPipe1;
        public bool HasWrappingPipe2;
        public bool HasWrappingPipe3;
        public string DN1;
        public string DN2;
        public string DN3;
        public bool HasCleaningPort1;
        public bool HasCleaningPort2;
        public bool HasCleaningPort3;
        public bool HasLargeCleaningPort;
        public int HangingCount = QUOTATIONSHAKES;
        public Hanging Hanging1;
        public Hanging Hanging2;
    }
    public class ThwPipeRun
    {
        public string Storey;
        public bool ShowStoreyLabel;
        public bool HasShortTranslator;
        public bool HasLongTranslator;
        public bool IsShortTranslatorToLeftOrRight;
        public bool IsLongTranslatorToLeftOrRight;
        public bool ShowShortTranslatorLabel;
        public bool IsFirstItem;
        public bool IsLastItem;
        public Hanging LeftHanging;
        public Hanging RightHanging;
    }
    public class ThwPipeLine
    {
        public List<string> Labels;
        public bool? IsLeftOrMiddleOrRight;
        public double AiringValue;
        public List<ThwPipeRun> PipeRuns;
        public ThwOutput Output;
    }
    public class RainDrawingData
    {
        public GRect Boundary;
        public Point2d ContraPoint;
        public List<GRect> Y1LVerticalPipeRects;
        public List<string> Y1LVerticalPipeRectLabels;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> SideWaterBuckets;
        public List<GRect> _87WaterBuckets;
        public List<string> GravityWaterBucketLabels;
        public List<string> SideWaterBucketLabels;
        public List<string> _87WaterBucketLabels;
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public Dictionary<string, int> FloorDrainWrappingPipes;
        public HashSet<string> CleaningPorts;
        public Dictionary<string, string> WaterWellLabels;
        public Dictionary<string, int> WaterWellIds;
        public Dictionary<string, int> RainPortIds;
        public Dictionary<int, string> OutletWrappingPipeDict;
        public HashSet<string> HasCondensePipe;
        public HashSet<string> HasBrokenCondensePipes;
        public HashSet<string> HasNonBrokenCondensePipes;
        public HashSet<string> HasRainPortSymbols;
        public HashSet<string> ConnectedToGravityWaterBucket;
        public HashSet<string> ConnectedToSideWaterBucket;
        public List<string> Comments;
        public HashSet<KeyValuePair<string, string>> RoofWaterBuckets;
        public HashSet<int> HasSingleFloorDrainDrainageForWaterWell;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public HashSet<int> HasSingleFloorDrainDrainageForRainPort;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForRainPort;
        public Dictionary<int, string> WrappingPipeRadiusStringDict;
        public void Init()
        {
            Y1LVerticalPipeRects ??= new List<GRect>();
            Y1LVerticalPipeRectLabels ??= new List<string>();
            GravityWaterBuckets ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
            GravityWaterBucketLabels ??= new List<string>();
            SideWaterBucketLabels ??= new List<string>();
            _87WaterBucketLabels ??= new List<string>();
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            FloorDrainWrappingPipes ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            WaterWellLabels ??= new Dictionary<string, string>();
            WaterWellIds ??= new Dictionary<string, int>();
            RainPortIds ??= new Dictionary<string, int>();
            OutletWrappingPipeDict ??= new Dictionary<int, string>();
            Comments ??= new List<string>();
            HasCondensePipe ??= new HashSet<string>();
            HasBrokenCondensePipes ??= new HashSet<string>();
            HasNonBrokenCondensePipes ??= new HashSet<string>();
            HasRainPortSymbols ??= new HashSet<string>();
            ConnectedToGravityWaterBucket ??= new HashSet<string>();
            ConnectedToSideWaterBucket ??= new HashSet<string>();
            RoofWaterBuckets ??= new HashSet<KeyValuePair<string, string>>();
            WrappingPipeRadiusStringDict ??= new Dictionary<int, string>();
            HasSingleFloorDrainDrainageForWaterWell ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForWaterWell ??= new HashSet<int>();
            HasSingleFloorDrainDrainageForRainPort ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForRainPort ??= new HashSet<int>();
        }
    }
    public partial class RainService
    {
        public AcadDatabase adb;
        public RainDiagram RainDiagram;
        public List<ThStoreysData> Storeys;
        public RainGeoData GeoData;
        public RainCadData CadDataMain;
        public List<RainCadData> CadDatas;
        public List<RainDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static RainGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return null;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new RainGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }
        public static void CollectGeoData(AcadDatabase adb, RainGeoData geoData, CommandContext ctx)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(ctx);
            cl.CollectEntities();
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, RainGeoData geoData)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(range);
            cl.CollectEntities();
        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == QUOTATIONSHAKES) return THESAURUSABDOMEN;
            }
            return THESAURUSABDOMINAL;
        }
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == THESAURUSACCIDENT) yield return new GLineSegment(ls[QUOTATIONSHAKES].ToPoint2d(), ls[THESAURUSACCESSION].ToPoint2d());
                    else if (ls.NumPoints > THESAURUSACCIDENT)
                    {
                        for (int i = QUOTATIONSHAKES; i < ls.NumPoints - THESAURUSACCESSION; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + THESAURUSACCESSION].ToPoint2d());
                        }
                    }
                }
                else if (geo is GeometryCollection colle)
                {
                    foreach (var _geo in colle.Geometries)
                    {
                        foreach (var __geo in enumerate(_geo))
                        {
                            yield return __geo;
                        }
                    }
                }
            }
            return enumerate(geo).ToList();
        }
#pragma warning disable
        public static void DrawGeoData(RainGeoData geoData)
        {
            foreach (var s in geoData.Storeys) DrawRectLazy(s).ColorIndex = THESAURUSACCESSION;
            foreach (var o in geoData.LabelLines) DrawLineSegmentLazy(o).ColorIndex = THESAURUSACCESSION;
            foreach (var o in geoData.Labels)
            {
                DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = THESAURUSACCIDENT;
                DrawRectLazy(o.Boundary).ColorIndex = THESAURUSACCIDENT;
            }
            foreach (var o in geoData.VerticalPipes) DrawRectLazy(o).ColorIndex = THESAURUSACCUSTOMED;
            foreach (var o in geoData.FloorDrains)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSACCUSTOM;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSADMONITORY);
            }
            foreach (var o in geoData.GravityWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSACCUSTOM;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSADOLESCENCE);
            }
            foreach (var o in geoData.SideWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSACCUSTOM;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSADOLESCENT);
            }
            foreach (var o in geoData._87WaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSACCUSTOM;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSADMITTANCE);
            }
            foreach (var o in geoData.WaterPorts) DrawRectLazy(o).ColorIndex = THESAURUSADAPTATION;
            foreach (var o in geoData.WaterWells) DrawRectLazy(o).ColorIndex = THESAURUSADAPTATION;
            {
                var cl = Color.FromRgb(THESAURUSACCUSTOM, FAMILIARIZATION, THESAURUSADDENDUM);
                foreach (var o in geoData.DLines) DrawLineSegmentLazy(o).Color = cl;
            }
            foreach (var o in geoData.WLines) DrawLineSegmentLazy(o).ColorIndex = QUOTATIONSPENSER;
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = ACHONDROPLASTIC;
        public static IEnumerable<Geometry> GroupLinesByConnPoints<T>(List<T> geos, double radius) where T : Geometry
        {
            var lines = geos.SelectMany(o => GeoFac.GetLines(o)).Distinct().ToList();
            var _lines = lines.Select(x => x.ToLineString()).ToList();
            var _linesf = GeoFac.CreateIntersectsSelector(_lines);
            var _geos = lines.Select(line => GeoFac.CreateGeometryEx(new Geometry[] { GeoFac.CreateCirclePolygon(line.StartPoint, radius, QUOTATIONSPENSER), GeoFac.CreateCirclePolygon(line.EndPoint, radius, QUOTATIONSPENSER) })).ToList();
            for (int i1 = QUOTATIONSHAKES; i1 < _geos.Count; i1++)
            {
                var _geo = _geos[i1];
                foreach (var i in _linesf(_geo).Select(_lines))
                {
                    if (i == i1) continue;
                    var line = lines[i];
                    var _line = _lines[i];
                    var r1 = GeoFac.CreateCirclePolygon(line.StartPoint, radius, QUOTATIONSPENSER);
                    if (r1.Intersects(_line))
                    {
                        _geos[i1] = _geos[i1].Union(r1);
                    }
                    var r2 = GeoFac.CreateCirclePolygon(line.EndPoint, radius, QUOTATIONSPENSER);
                    if (r2.Intersects(_line))
                    {
                        _geos[i1] = _geos[i1].Union(r2);
                    }
                }
            }
            foreach (var list in GeoFac.GroupGeometries(_geos))
            {
                yield return GeoFac.CreateGeometry(list.Select(_geos).ToList(lines).Select(x => x.ToLineString()));
            }
        }
        public static void CreateDrawingDatas(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas, out string logString, out List<RainDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData = null)
        {
            _DrawingTransaction.Current.AbleToDraw = THESAURUSABDOMEN;
            roomData ??= new List<KeyValuePair<string, Geometry>>();
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSACCESSION;
            }
            var sb = new StringBuilder(THESAURUSADDICT);
            drDatas = new List<RainDrawingData>();
            for (int storeyI = QUOTATIONSHAKES; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainDrawingData();
                drData.Init();
                drData.Boundary = geoData.Storeys[storeyI];
                drData.ContraPoint = geoData.StoreyContraPoints[storeyI];
                var item = cadDatas[storeyI];
                {
                    var maxDis = THESAURUSABRUPT;
                    var angleTolleranceDegree = THESAURUSACCESSION;
                    var waterPortCvt = RainCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > QUOTATIONSHAKES).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
                        maxDis, angleTolleranceDegree).ToList();
                    geoData.DLines.AddRange(lines);
                    var dlineCvt = RainCadData.ConvertDLinesF();
                    var _lines = lines.Select(dlineCvt).ToList();
                    cadDataMain.DLines.AddRange(_lines);
                    item.DLines.AddRange(_lines);
                }
                var lbDict = new Dictionary<Geometry, string>();
                var notedPipesDict = new Dictionary<Geometry, string>();
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesf = F(labelLinesGeos);
                var shortTranslatorLabels = new HashSet<string>();
                var longTranslatorLabels = new HashSet<string>();
                var wlinesGeos = GroupLinesByConnPoints(item.WLines, AUTHORITARIANISM).ToList();
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        if (!IsRainLabel(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                        var lst = labellinesf(label);
                        if (lst.Count == THESAURUSACCESSION)
                        {
                            var labelline = lst[QUOTATIONSHAKES];
                            if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == QUOTATIONSHAKES)
                            {
                                var lines = ExplodeGLineSegments(labelline);
                                var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSACCUSE)).ToList(), label, radius: THESAURUSACCUSE);
                                if (points.Count == THESAURUSACCESSION)
                                {
                                    var pt = points[QUOTATIONSHAKES];
                                    var r = GRect.Create(pt, THESAURUSABSOLUTE);
                                    geoData.VerticalPipes.Add(r);
                                    var pl = r.ToPolygon();
                                    cadDataMain.VerticalPipes.Add(pl);
                                    item.VerticalPipes.Add(pl);
                                    DrawTextLazy(THESAURUSADDICTED, pl.GetCenter());
                                }
                            }
                        }
                    }
                }
                DrawGeoData(geoData, cadDataMain, storeyI, item);
                {
                    {
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                        {
                            var ok = THESAURUSABDOMEN;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == THESAURUSACCESSION && pipes.Count == THESAURUSACCESSION)
                                {
                                    var lb = labels[QUOTATIONSHAKES];
                                    var pp = pipes[QUOTATIONSHAKES];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSACCEPTABLE;
                                    if (IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = THESAURUSABDOMINAL;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                        {
                            var ok = THESAURUSABDOMEN;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > QUOTATIONSHAKES)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSACCEPTABLE).ToList();
                                    if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = QUOTATIONSHAKES; k < pipes.Count; k++)
                                        {
                                            var pp = pipes[k];
                                            var lb = labels[k];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp] = label;
                                        }
                                        ok_ents.AddRange(pipes);
                                        ok_ents.AddRange(labels);
                                        ok = THESAURUSABDOMINAL;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        {
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? THESAURUSACCEPTABLE;
                                if (!IsMaybeLabelText(lb)) continue;
                                var lst = labellinesf(label);
                                if (lst.Count == THESAURUSACCESSION)
                                {
                                    var labelline = lst[QUOTATIONSHAKES];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == THESAURUSACCESSION)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[QUOTATIONSHAKES].ToNTSPoint());
                                        if (pipes.Count == THESAURUSACCESSION)
                                        {
                                            var pp = pipes[QUOTATIONSHAKES];
                                            lbDict[pp] = lb;
                                            ok_ents.Add(pp);
                                            ok_ents.Add(label);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    (List<Geometry>, List<Geometry>) getPipes()
                    {
                        var pipes1 = new List<Geometry>(lbDict.Count);
                        var pipes2 = new List<Geometry>(lbDict.Count);
                        foreach (var pipe in item.VerticalPipes) if (lbDict.ContainsKey(pipe)) pipes1.Add(pipe); else pipes2.Add(pipe);
                        return (pipes1, pipes2);
                    }
                    {
                        bool recognise1()
                        {
                            var ok = THESAURUSABDOMEN;
                            for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in wlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == THESAURUSACCESSION && lst2.Count > QUOTATIONSHAKES)
                                    {
                                        var pp1 = lst1[QUOTATIONSHAKES];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (THESAURUSACCLAIM < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSABDOMINAL;
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSABDOMINAL;
                                            }
                                        }
                                    }
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        bool recognise2()
                        {
                            var ok = THESAURUSABDOMEN;
                            for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(THESAURUSACCUSE).ToGCircle(THESAURUSABDOMEN).ToCirclePolygon(QUOTATIONSPENSER));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > THESAURUSACCESSION)
                                        {
                                            fs.Add(() =>
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSABDOMINAL;
                                            });
                                        }
                                    }
                                    if (fs.Count == THESAURUSACCESSION) fs[QUOTATIONSHAKES]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                        {
                            if (!(recognise1() && recognise2())) break;
                        }
                    }
                }
                string getLabel(Geometry pipe)
                {
                    lbDict.TryGetValue(pipe, out string label);
                    return label;
                }
                {
                    var f = F(item.VerticalPipes);
                    foreach (var dlinesGeo in wlinesGeos)
                    {
                        var pipes = f(dlinesGeo);
                        var d = pipes.Select(getLabel).Where(x => x != null).ToCountDict();
                        foreach (var label in d.Where(x => x.Value > THESAURUSACCESSION).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == THESAURUSACCIDENT)
                            {
                                var dis = pps[QUOTATIONSHAKES].GetCenter().GetDistanceTo(pps[THESAURUSACCESSION].GetCenter());
                                if (THESAURUSACCLAIM < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                {
                                    shortTranslatorLabels.Add(label);
                                }
                                else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                {
                                    longTranslatorLabels.Add(label);
                                }
                            }
                            else
                            {
                                longTranslatorLabels.Add(label);
                            }
                        }
                    }
                }
                {
                    var ok_fds = new HashSet<Geometry>();
                    var floorDrainD = new Dictionary<string, int>();
                    var floorDrainWrappingPipeD = new Dictionary<string, int>();
                    {
                        var wpsf = F(item.WrappingPipes);
                        var wlinesGeosf = F(wlinesGeos);
                        {
                            var fdsf = F(item.FloorDrains);
                            foreach (var pipe in item.VerticalPipes)
                            {
                                var label = getLabel(pipe);
                                if (label != null)
                                {
                                    var wlines = wlinesGeosf(pipe);
                                    if (wlines.Any())
                                    {
                                        var pts = GeoFac.ToNodedLineSegments(wlines.SelectMany(x => GeoFac.GetLines(x)).ToList()).SelectMany(x => new Point2d[] { x.StartPoint, x.EndPoint }).ToList();
                                        var wlinesGeo = GeoFac.CreateGeometry(wlines);
                                        var fds = fdsf(GeoFac.CreateGeometry(pts.Select(x => x.ToNTSPoint())));
                                        ok_fds.AddRange(ok_fds);
                                        floorDrainD[label] = fds.Count;
                                        if (fds.Count > QUOTATIONSHAKES)
                                        {
                                            var wps = wpsf(wlinesGeo);
                                            floorDrainWrappingPipeD[label] = wps.Count;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var fd in item.FloorDrains.Except(ok_fds).Except(F(item.FloorDrains)(G(item.WLines))))
                        {
                            foreach (var pipe in pipesf(new GCircle(fd.GetCenter(), ACCOMMODATIONAL).ToCirclePolygon(QUOTATIONSPENSER)))
                            {
                                if (lbDict.TryGetValue(pipe, out string label))
                                {
                                    floorDrainD.TryGetValue(label, out int count);
                                    if (count == QUOTATIONSHAKES)
                                    {
                                        floorDrainD[label] = THESAURUSACCESSION;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    drData.FloorDrains = floorDrainD;
                    drData.FloorDrainWrappingPipes = floorDrainWrappingPipeD;
                }
                {
                    var ok_ents = new HashSet<Geometry>();
                    var todoD = new Dictionary<Geometry, string>();
                    var pipesf = F(item.VerticalPipes);
                    var gs = GeoFac.GroupGeometries(item.CondensePipes.Concat(item.WLines).ToList());
                    foreach (var g in gs)
                    {
                        var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        var pipes = pipesf(G(cps.Concat(wlines)));
                        if (pipes.Count == THESAURUSACCESSION)
                        {
                            var label = getLabel(pipes[QUOTATIONSHAKES]);
                            if (label != null)
                            {
                                if (cps.Count > QUOTATIONSHAKES)
                                {
                                    drData.HasCondensePipe.Add(label);
                                    ok_ents.AddRange(cps);
                                }
                                if (cps.Count == THESAURUSACCIDENT)
                                {
                                    drData.HasNonBrokenCondensePipes.Add(label);
                                }
                                if (cps.Count == THESAURUSACCESSION)
                                {
                                    var cp = cps[QUOTATIONSHAKES];
                                    todoD[cp] = label;
                                }
                            }
                        }
                    }
                    {
                        var cpsf = F(item.CondensePipes.Except(ok_ents).ToList());
                        foreach (var kv in todoD)
                        {
                            var cp = kv.Key;
                            var label = kv.Value;
                            var pt = cp.GetCenter();
                            var cps = cpsf(new GLineSegment(pt.OffsetX(-THESAURUSADDICTION), pt.OffsetX(THESAURUSADDICTION)).ToLineString()).Except(ok_ents).ToList();
                            if (cps.Count == THESAURUSACCESSION)
                            {
                                ok_ents.AddRange(cps);
                                drData.HasBrokenCondensePipes.Add(label);
                            }
                        }
                    }
                }
                IEnumerable<Geometry> getOkPipes()
                {
                    foreach (var kv in lbDict)
                    {
                        if (IsRainLabel(kv.Value))
                        {
                            yield return kv.Key;
                        }
                    }
                }
                {
                    var gbksf = F(item.GravityWaterBuckets);
                    var pipesf = F(getOkPipes().ToList());
                    foreach (var wlineGeo in wlinesGeos)
                    {
                        var gbks = gbksf(wlineGeo);
                        if (gbks.Count == THESAURUSACCESSION)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == THESAURUSACCESSION)
                            {
                                drData.ConnectedToGravityWaterBucket.Add(lbDict[pipes[QUOTATIONSHAKES]]);
                            }
                        }
                    }
                }
                {
                    var sbksf = F(item.SideWaterBuckets);
                    var pipesf = F(getOkPipes().ToList());
                    foreach (var wlineGeo in wlinesGeos)
                    {
                        var sbks = sbksf(wlineGeo);
                        if (sbks.Count == THESAURUSACCESSION)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == THESAURUSACCESSION)
                            {
                                drData.ConnectedToSideWaterBucket.Add(lbDict[pipes[QUOTATIONSHAKES]]);
                            }
                        }
                    }
                }
                {
                    var ok_vpipes = new HashSet<Geometry>();
                    var outletd = new Dictionary<string, string>();
                    var waterWellIdDict = new Dictionary<string, int>();
                    var rainPortIdDict = new Dictionary<string, int>();
                    var waterWellsIdDict = new Dictionary<Geometry, int>();
                    var waterWellsLabelDict = new Dictionary<Geometry, string>();
                    var outletWrappingPipe = new Dictionary<int, string>();
                    var hasRainPortSymbols = new HashSet<string>();
                    {
                        var rainPortsf = F(item.RainPortSymbols);
                        var pipesf = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        foreach (var wlinesGeo in GeoFac.GroupGeometries<Geometry>(wlinesGeos.Concat(item.WrappingPipes.OfType<Polygon>().Select(x => x.Shell)).ToList()).Select(g => g.Count == THESAURUSACCESSION ? g[QUOTATIONSHAKES] : GeoFac.CreateGeometry(g)).ToList())
                        {
                            var rainPorts = rainPortsf(wlinesGeo);
                            foreach (var rainPort in rainPorts)
                            {
                                var pipes = pipesf(wlinesGeo);
                                foreach (var pipe in pipesf(wlinesGeo))
                                {
                                    var label = getLabel(pipe);
                                    if (label != null)
                                    {
                                        hasRainPortSymbols.Add(label);
                                        ok_vpipes.Add(pipe);
                                        rainPortIdDict[label] = cadDataMain.RainPortSymbols.IndexOf(rainPort);
                                    }
                                }
                            }
                        }
                        drData.HasRainPortSymbols = hasRainPortSymbols;
                    }
                    {
                        void collect(Func<Geometry, List<Geometry>> waterWellsf, Func<Geometry, string> getWaterWellLabel, Func<Geometry, int> getWaterWellId)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                            foreach (var wlinesGeo in wlinesGeos)
                            {
                                var waterWells = waterWellsf(wlinesGeo);
                                if (waterWells.Count == THESAURUSACCESSION)
                                {
                                    var waterWell = waterWells[QUOTATIONSHAKES];
                                    var waterWellLabel = getWaterWellLabel(waterWell);
                                    waterWellsLabelDict[wlinesGeo] = waterWellLabel;
                                    waterWellsIdDict[wlinesGeo] = getWaterWellId(waterWell);
                                    var pipes = f2(wlinesGeo);
                                    ok_vpipes.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            outletd[label] = waterWellLabel;
                                            waterWellIdDict[label] = getWaterWellId(waterWell);
                                            var wrappingpipes = wrappingPipesf(wlinesGeo);
                                            foreach (var wp in wrappingpipes)
                                            {
                                                outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
                                                waterWellsLabelDict[wp] = waterWellLabel;
                                                waterWellsIdDict[wp] = getWaterWellId(waterWell);
                                                DrawTextLazy(waterWellLabel, wp.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        collect(F(item.WaterWells), waterWell => geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)], well => cadDataMain.WaterWells.IndexOf(well));
                        {
                            var spacialIndex = item.WaterWells.Select(cadDataMain.WaterWells).ToList();
                            var waterWells = spacialIndex.ToList(geoData.WaterWells).Select(x => x.Expand(ACHONDROPLASTIC).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterWellLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var radius = THESAURUSACCLAIM;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterWells);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(QUOTATIONSPENSER, THESAURUSABDOMEN)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterWells).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= ADRENOCORTICOTROPHIC)
                                    {
                                        var waterWellLabel = geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)];
                                        waterWellsLabelDict[dlinesGeo] = waterWellLabel;
                                        waterWellsIdDict[dlinesGeo] = cadDataMain.WaterWells.IndexOf(waterWell);
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                outletd[label] = waterWellLabel;
                                                waterWellIdDict[label] = cadDataMain.WaterWells.IndexOf(waterWell);
                                                ok_vpipes.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    waterWellsLabelDict[wp] = waterWellLabel;
                                                    waterWellsIdDict[wp] = cadDataMain.WaterWells.IndexOf(waterWell);
                                                    DrawTextLazy(waterWellLabel, wp.GetCenter());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var wp in item.WrappingPipes)
                        {
                            if (waterWellsLabelDict.TryGetValue(wp, out string v))
                            {
                                var pipes = pipesf(wp);
                                foreach (var pipe in pipes)
                                {
                                    if (lbDict.TryGetValue(pipe, out string label))
                                    {
                                        if (!outletd.ContainsKey(label))
                                        {
                                            outletd[label] = v;
                                            waterWellIdDict[label] = waterWellsIdDict[wp];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var points = geoData.WrappingPipeRadius.Select(x => x.Key).ToList();
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        var ptsf = GeoFac.CreateIntersectsSelector(pts);
                        var wpf = F(item.WrappingPipes.Where(wp => !waterWellsLabelDict.ContainsKey(wp)).ToList());
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            if (!waterWellsLabelDict.TryGetValue(dlinesGeo, out string v)) continue;
                            foreach (var wp in wpf(dlinesGeo))
                            {
                                if (!waterWellsLabelDict.ContainsKey(wp))
                                {
                                    waterWellsLabelDict[wp] = v;
                                    waterWellsIdDict[wp] = waterWellsIdDict[dlinesGeo];
                                }
                            }
                        }
                        {
                            foreach (var wp in item.WrappingPipes)
                            {
                                var _pts = ptsf(wp);
                                if (_pts.Count > QUOTATIONSHAKES)
                                {
                                    var kv = geoData.WrappingPipeRadius[pts.IndexOf(_pts[QUOTATIONSHAKES])];
                                    var radiusText = kv.Value;
                                    if (string.IsNullOrWhiteSpace(radiusText)) radiusText = THESAURUSALLUDE;
                                    drData.WrappingPipeRadiusStringDict[cadDataMain.WrappingPipes.IndexOf(wp)] = radiusText;
                                }
                            }
                        }
                    }
                    {
                        drData.WaterWellIds = waterWellIdDict;
                        drData.RainPortIds = rainPortIdDict;
                        drData.WaterWellLabels = outletd;
                        outletd.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                        {
                            var num = kv1.Value;
                            var pipe = kv2.Key;
                            DrawTextLazy(num, pipe.ToGRect().RightButtom);
                            return THESAURUSADDITIONAL;
                        }).Count();
                    }
                    {
                        drData.OutletWrappingPipeDict = outletWrappingPipe;
                    }
                }
                {
                    var wlinesf = F(wlinesGeos);
                    foreach (var kv in lbDict)
                    {
                        var m = Regex.Match(kv.Value, THESAURUSADDITIVE);
                        if (m.Success)
                        {
                            var floor = m.Groups[THESAURUSACCESSION].Value;
                            var pipe = kv.Key;
                            var pipes = getOkPipes().ToList();
                            var wlines = wlinesf(pipe);
                            if (wlines.Count == THESAURUSACCESSION)
                            {
                                foreach (var pp in F(pipes)(wlines[QUOTATIONSHAKES]))
                                {
                                    drData.RoofWaterBuckets.Add(new KeyValuePair<string, string>(lbDict[pp], floor));
                                }
                            }
                        }
                    }
                }
                {
                    var pipesf = F(item.VerticalPipes);
                    if (item.WaterWells.Count > QUOTATIONSHAKES)
                    {
                        var wlinesf = F(wlinesGeos);
                        var fdsf = F(item.FloorDrains);
                        foreach (var well in item.WaterWells)
                        {
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(THESAURUSACOLYTE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                if (fds.Count > QUOTATIONSHAKES)
                                {
                                    var id = cadDataMain.WaterWells.IndexOf(well);
                                    drData.HasSingleFloorDrainDrainageForWaterWell.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Add(id);
                                    }
                                }
                            }
                        }
                        foreach (var port in item.RainPortSymbols)
                        {
                            foreach (var wline in wlinesf(port.EnvelopeInternal.ToGRect().Expand(THESAURUSACOLYTE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                if (fds.Count > QUOTATIONSHAKES)
                                {
                                    var id = cadDataMain.RainPortSymbols.IndexOf(port);
                                    drData.HasSingleFloorDrainDrainageForRainPort.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
                {
                }
                foreach (var pp in item.VerticalPipes)
                {
                    lbDict.TryGetValue(pp, out string label);
                    if (label != null)
                    {
                        DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                    }
                }
                {
                    foreach (var kv in lbDict)
                    {
                        if (IsY1L(kv.Value))
                        {
                            drData.Y1LVerticalPipeRectLabels.Add(kv.Value);
                            drData.Y1LVerticalPipeRects.Add(kv.Key.EnvelopeInternal.ToGRect());
                        }
                    }
                    drData.GravityWaterBuckets.AddRange(item.GravityWaterBuckets.Select(x => geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(x)]).Distinct());
                    drData.SideWaterBuckets.AddRange(item.SideWaterBuckets.Select(x => geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(x)]).Distinct());
                    drData._87WaterBuckets.AddRange(item._87WaterBuckets.Select(x => geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(x)]).Distinct());
                    for (int i = QUOTATIONSHAKES; i < drData.GravityWaterBuckets.Count; i++)
                    {
                        drData.GravityWaterBucketLabels.Add(null);
                    }
                    for (int i = QUOTATIONSHAKES; i < drData.SideWaterBuckets.Count; i++)
                    {
                        drData.SideWaterBucketLabels.Add(null);
                    }
                    for (int i = QUOTATIONSHAKES; i < drData._87WaterBuckets.Count; i++)
                    {
                        drData._87WaterBucketLabels.Add(null);
                    }
                    {
                        var bklbDict = new Dictionary<Geometry, string>();
                        var labelsf = F(item.Labels);
                        var bksf = F(item.GravityWaterBuckets.Concat(item.SideWaterBuckets).Concat(item._87WaterBuckets).ToList());
                        foreach (var labelLinesGeo in labelLinesGeos)
                        {
                            var labels = labelsf(labelLinesGeo);
                            var bks = bksf(labelLinesGeo);
                            if (labels.Count == THESAURUSACCESSION && bks.Count == THESAURUSACCESSION)
                            {
                                var lb = labels[QUOTATIONSHAKES];
                                var bk = bks[QUOTATIONSHAKES];
                                var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSACCEPTABLE;
                                if (IsWaterBucketLabel(label))
                                {
                                    if (!label.ToUpper().Contains(THESAURUSADOPTION))
                                    {
                                        var lst = labelsf(lb.GetCenter().OffsetY(-ACCOMMODATIONAL).ToNTSPoint());
                                        lst.Remove(lb);
                                        if (lst.Count == THESAURUSACCESSION)
                                        {
                                            var _label = geoData.Labels[cadDataMain.Labels.IndexOf(lst[QUOTATIONSHAKES])].Text ?? THESAURUSACCEPTABLE;
                                            if (_label.Contains(THESAURUSADOPTION))
                                            {
                                                label += _label;
                                            }
                                        }
                                    }
                                    bklbDict[bk] = label;
                                }
                            }
                        }
                        foreach (var kv in bklbDict)
                        {
                            int i;
                            i = cadDataMain.GravityWaterBuckets.IndexOf(kv.Key);
                            if (i >= QUOTATIONSHAKES)
                            {
                                i = drData.GravityWaterBuckets.IndexOf(geoData.GravityWaterBuckets[i]);
                                if (i >= QUOTATIONSHAKES)
                                {
                                    drData.GravityWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain.SideWaterBuckets.IndexOf(kv.Key);
                            if (i >= QUOTATIONSHAKES)
                            {
                                i = drData.SideWaterBuckets.IndexOf(geoData.SideWaterBuckets[i]);
                                if (i >= QUOTATIONSHAKES)
                                {
                                    drData.SideWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain._87WaterBuckets.IndexOf(kv.Key);
                            if (i >= QUOTATIONSHAKES)
                            {
                                i = drData._87WaterBuckets.IndexOf(geoData._87WaterBuckets[i]);
                                if (i >= QUOTATIONSHAKES)
                                {
                                    drData._87WaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                        }
                    }
                }
                {
                    drData.VerticalPipeLabels.AddRange(lbDict.Values.Distinct());
                }
                {
                    var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                    _longTranslatorLabels.Sort();
                    drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);
                }
                {
                    var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                    _shortTranslatorLabels.Sort();
                    drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);
                }
                {
                }
                drDatas.Add(drData);
            }
            logString = sb.ToString();
            _DrawingTransaction.Current.AbleToDraw = THESAURUSABDOMINAL;
        }
        public static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas)
        {
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSACCESSION;
            }
            for (int storeyI = QUOTATIONSHAKES; storeyI < cadDatas.Count; storeyI++)
            {
                var item = cadDatas[storeyI];
                DrawGeoData(geoData, cadDataMain, storeyI, item);
            }
        }
        private static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, int storeyI, RainCadData item)
        {
            foreach (var o in item.LabelLines)
            {
                DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = THESAURUSACCESSION;
            }
            foreach (var pl in item.Labels)
            {
                var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                e.ColorIndex = THESAURUSACCIDENT;
                var _pl = DrawRectLazy(m.Boundary);
                _pl.ColorIndex = THESAURUSACCIDENT;
            }
            foreach (var o in item.PipeKillers)
            {
                DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(THESAURUSADJACENT, THESAURUSADJACENT, THESAURUSABSOLUTE);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)], THESAURUSACCLAIM);
            }
            foreach (var o in item.VerticalPipes)
            {
                DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSACCUSTOMED;
            }
            foreach (var o in item.FloorDrains)
            {
                DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = QUOTATIONSPENSER;
            }
            foreach (var o in item.WLines)
            {
                DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = QUOTATIONSPENSER;
            }
            foreach (var o in item.WaterPorts)
            {
                DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = THESAURUSADAPTATION;
                DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.WaterWells)
            {
                DrawRectLazy(geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ColorIndex = THESAURUSACCUSTOM;
                DrawTextLazy(geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.RainPortSymbols)
            {
                DrawRectLazy(geoData.RainPortSymbols[cadDataMain.RainPortSymbols.IndexOf(o)]).ColorIndex = THESAURUSADAPTATION;
            }
            foreach (var o in item.GravityWaterBuckets)
            {
                var r = geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSADAPTATION;
                Dr.DrawSimpleLabel(r.LeftTop, QUOTATIONADJACENT);
            }
            foreach (var o in item.SideWaterBuckets)
            {
                var r = geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSADAPTATION;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSADJOURN);
            }
            foreach (var o in item._87WaterBuckets)
            {
                var r = geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSADAPTATION;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSADMITTANCE);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)]).ColorIndex = THESAURUSACCESSION;
            }
            foreach (var o in item.CleaningPorts)
            {
                var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                if (THESAURUSABDOMEN) DrawGeometryLazy(new GCircle(m, INCOMPREHENSIBLE).ToCirclePolygon(INTERDIGITATING), ents => ents.ForEach(e => e.ColorIndex = THESAURUSADAPTATION));
                DrawRectLazy(GRect.Create(m, INTENSIFICATION));
            }
            {
                var cl = Color.FromRgb(THESAURUSADJOURNMENT, DISCONTINUATION, THESAURUSADJUDICATE);
                foreach (var o in item.WrappingPipes)
                {
                    DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSACCUSTOM, FAMILIARIZATION, THESAURUSADDENDUM);
                foreach (var o in item.DLines)
                {
                    DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSADJUDICATION, THESAURUSADJUNCT, THESAURUSADJUST);
                foreach (var o in item.VLines)
                {
                    DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                }
            }
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == THESAURUSADJUSTMENT).Select(x => x.ToNTSPolygon()).Cast<Geometry>().ToList();
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == EXTEMPORANEOUSLY).Select(x => new CText() { Text = x.Text, Boundary = x.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateIntersectsSelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == THESAURUSACCESSION)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[QUOTATIONSHAKES]));
                    }
                    else
                    {
                        foreach (var geo in l)
                        {
                            ranges.Remove(geo);
                        }
                    }
                }
            }
            foreach (var range in ranges.Except(list.Select(kv => kv.Value)))
            {
                list.Add(new KeyValuePair<string, Geometry>(THESAURUSACCEPTABLE, range));
            }
            return list;
        }
    }
    public class WaterBucketItem : IEquatable<WaterBucketItem>
    {
        public WaterBucketEnum WaterBucketType;
        public string DN;
        public bool Equals(WaterBucketItem other)
        {
            return this.WaterBucketType == other.WaterBucketType
                && this.DN == other.DN;
        }
        public static bool operator ==(WaterBucketItem x, WaterBucketItem y)
        {
            return (x is null && y is null) || (x is not null && y is not null && x.Equals(y));
        }
        public static bool operator !=(WaterBucketItem x, WaterBucketItem y) => !(x == y);
        public static WaterBucketItem TryParse(string text)
        {
            WaterBucketEnum bkType;
            if (text == null)
            {
                throw new System.Exception();
            }
            else if (IsGravityWaterBucketLabel(text))
            {
                bkType = WaterBucketEnum.Gravity;
            }
            else if (IsSideWaterBucketLabel(text))
            {
                bkType = WaterBucketEnum.Side;
            }
            else if (Is87WaterBucketLabel(text))
            {
                bkType = WaterBucketEnum._87;
            }
            else
            {
                bkType = WaterBucketEnum.Gravity;
            }
            string dn = GetDN(text);
            return new WaterBucketItem() { DN = dn, WaterBucketType = bkType, };
        }
        public string GetDisplayString()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.None:
                    throw new System.Exception();
                case WaterBucketEnum.Gravity:
                    return QUOTATIONADRENAL + DN;
                case WaterBucketEnum.Side:
                    return THESAURUSADROIT + DN;
                case WaterBucketEnum._87:
                    return THESAURUSADRIFT + DN;
                default:
                    throw new System.Exception();
            }
        }
        public override int GetHashCode()
        {
            return QUOTATIONSHAKES;
        }
    }
    public class RainGroupingPipeItem : IEquatable<RainGroupingPipeItem>
    {
        public string Label;
        public bool HasWaterWell;
        public bool HasBasinInKitchenAt1F;
        public bool HasWaterWellWrappingPipe;
        public string WrappingPipeRadius;
        public bool IsSingleOutlet;
        public string WaterWellLabel;
        public bool HasLineAtBuildingFinishedSurfice;
        public List<ValueItem> Items;
        public List<Hanging> Hangings;
        public bool HasRainPort;
        public string WaterWellWrappingPipeRadius;
        public PipeType PipeType;
        public string MinTl;
        public int FloorDrainsCountAt1F;
        public bool CanHaveAringSymbol;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public bool Equals(RainGroupingPipeItem other)
        {
            return this.HasWaterWell == other.HasWaterWell
                && this.HasRainPort == other.HasRainPort
                && this.HasWaterWellWrappingPipe == other.HasWaterWellWrappingPipe
                && this.WrappingPipeRadius == other.WrappingPipeRadius
                && this.HasBasinInKitchenAt1F == other.HasBasinInKitchenAt1F
                && this.HasLineAtBuildingFinishedSurfice == other.HasLineAtBuildingFinishedSurfice
                && this.PipeType == other.PipeType
                && this.MinTl == other.MinTl
                && this.IsSingleOutlet == other.IsSingleOutlet
                && this.FloorDrainsCountAt1F == other.FloorDrainsCountAt1F
                && this.CanHaveAringSymbol == other.CanHaveAringSymbol
                && this.HasSingleFloorDrainDrainageForWaterWell == other.HasSingleFloorDrainDrainageForWaterWell
                && this.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell == other.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell
                && this.HasSingleFloorDrainDrainageForRainPort == other.HasSingleFloorDrainDrainageForRainPort
                && this.IsFloorDrainShareDrainageWithVerticalPipeForRainPort == other.IsFloorDrainShareDrainageWithVerticalPipeForRainPort
                && this.WaterWellWrappingPipeRadius == other.WaterWellWrappingPipeRadius
                && this.Items.SeqEqual(other.Items)
                && this.Hangings.SeqEqual(other.Hangings);
        }
        public class Hanging : IEquatable<Hanging>
        {
            public int FloorDrainsCount;
            public int FloorDrainWrappingPipesCount;
            public bool HasCondensePipe;
            public bool HasBrokenCondensePipes;
            public bool HasNonBrokenCondensePipes;
            public bool HasCheckPoint;
            public WaterBucketItem WaterBucket;
            public override int GetHashCode()
            {
                return QUOTATIONSHAKES;
            }
            public bool Equals(Hanging other)
            {
                return this.FloorDrainsCount == other.FloorDrainsCount
                    && this.HasCondensePipe == other.HasCondensePipe
                    && this.HasBrokenCondensePipes == other.HasBrokenCondensePipes
                    && this.HasNonBrokenCondensePipes == other.HasNonBrokenCondensePipes
                    && this.FloorDrainWrappingPipesCount == other.FloorDrainWrappingPipesCount
                    && this.HasCheckPoint == other.HasCheckPoint
                    && this.WaterBucket == other.WaterBucket
                    ;
            }
        }
        public struct ValueItem
        {
            public bool Exist;
            public bool HasLong;
            public bool HasShort;
            public bool HasBalconyWashingMachineFloorDrain;
            public bool HasBalconyNonWashingMachineFloorDrain;
        }
        public override int GetHashCode()
        {
            return QUOTATIONSHAKES;
        }
    }
    public enum PipeType
    {
        Y1L, Y2L, NL, YL,
    }
    public class RainGroupedPipeItem
    {
        public List<string> Labels;
        public List<string> WaterWellLabels;
        public bool HasWrappingPipe;
        public bool HasWaterWell => WaterWellLabels != null && WaterWellLabels.Count > QUOTATIONSHAKES;
        public bool HasRainPort;
        public List<RainGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public bool HasTl => TlLabels != null && TlLabels.Count > QUOTATIONSHAKES;
        public PipeType PipeType;
        public List<RainGroupingPipeItem.Hanging> Hangings;
        public bool IsSingleOutlet;
        public bool HasBasinInKitchenAt1F;
        public int FloorDrainsCountAt1F;
        public bool CanHaveAringSymbol;
        public bool HasLineAtBuildingFinishedSurfice;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public string WaterWellWrappingPipeRadius;
    }
    public class PipeRunLocationInfo
    {
        public string Storey;
        public Point2d BasePoint;
        public Point2d StartPoint;
        public Point2d EndPoint;
        public Point2d HangingEndPoint;
        public List<Vector2d> Vector2ds;
        public List<GLineSegment> Segs;
        public List<GLineSegment> DisplaySegs;
        public List<GLineSegment> RightSegsFirst;
        public List<GLineSegment> RightSegsMiddle;
        public List<GLineSegment> RightSegsLast;
        public bool Visible;
        public Point2d PlBasePt;
    }
    public class StoreyContext
    {
        public List<ThStoreysData> thStoreysDatas;
        public List<ThMEPEngineCore.Model.Common.ThStoreys> thStoreys;
        public List<ObjectId> GetObjectIds()
        {
            return thStoreys.Select(o => o.ObjectId).ToList();
        }
    }
    public class CommandContext
    {
        public Point3dCollection range;
        public StoreyContext StoreyContext;
        public Diagram.ViewModel.RainSystemDiagramViewModel ViewModel;
        public System.Windows.Window window;
    }
    public class ThRainService
    {
        public static CommandContext commandContext;
        public static void ConnectLabelToLabelLine(RainGeoData geoData)
        {
            static bool f(string s)
            {
                if (s == null) return THESAURUSABDOMEN;
                if (IsMaybeLabelText(s)) return THESAURUSABDOMINAL;
                return THESAURUSABDOMEN;
            }
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Where(x => f(x.Text)).Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(THESAURUSACCLAIM)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-THESAURUSACCLAIM).OffsetY(-THESAURUSADMINISTER), ADRENOCORTICOTROPHIC, THESAURUSADMINISTER);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(INATTENTIVENESS, ACHONDROPLASTIC) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(THESAURUSABANDON, ACHONDROPLASTIC))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(THESAURUSABANDON));
                    }
                }
            }
        }
        public static void PreFixGeoData(RainGeoData geoData)
        {
            geoData.FixData();
            static bool isNotWantedText(string t)
            {
                if (t == null) return THESAURUSABDOMINAL;
                if (t.StartsWith(THESAURUSACCENT) || t.Contains(THESAURUSALTITUDE)) return THESAURUSABDOMINAL;
                return THESAURUSABDOMEN;
            }
            geoData.Labels = geoData.Labels.Where(x => !isNotWantedText(x.Text)).ToList();
            for (int i = QUOTATIONSHAKES; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(THESAURUSACCUSE))
                {
                    geoData.LabelLines[i] = seg.Extend(QUOTATIONSPENSER);
                }
                else if (seg.IsVertical(THESAURUSACCUSE))
                {
                    geoData.LabelLines[i] = seg.Extend(THESAURUSACCESSION);
                }
            }
            for (int i = QUOTATIONSHAKES; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(THESAURUSACCUSE);
            }
            for (int i = QUOTATIONSHAKES; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(THESAURUSACCUSE);
            }
            for (int i = QUOTATIONSHAKES; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(THESAURUSACCUSE);
            }
            for (int i = QUOTATIONSHAKES; i < geoData.VerticalPipes.Count; i++)
            {
                geoData.VerticalPipes[i] = geoData.VerticalPipes[i].Expand(THESAURUSACCLAIM);
            }
            {
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSADDICTION && x.Height < THESAURUSADDICTION).ToList();
            }
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSABANDON)).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSACCLAIM))).ToList();
            }
            {
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.GravityWaterBuckets = GeoFac.GroupGeometries(geoData.GravityWaterBuckets.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.RainPortSymbols = GeoFac.GroupGeometries(geoData.RainPortSymbols.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSABANDON);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.CondensePipes = geoData.CondensePipes.Distinct(cmp).ToList();
                geoData.PipeKillers = geoData.PipeKillers.Distinct(cmp).ToList();
            }
            {
                for (int i = QUOTATIONSHAKES; i < geoData.WaterWellLabels.Count; i++)
                {
                    var label = geoData.WaterWellLabels[i];
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        geoData.WaterWellLabels[i] = THESAURUSACCEPT;
                    }
                }
            }
            {
                var _pipes = geoData.VerticalPipes.Distinct().ToList();
                var pipes = _pipes.Select(x => x.Center.ToNTSPoint()).ToList();
                var tag = new object();
                var pipesf = GeoFac.CreateContainsSelector(pipes);
                foreach (var _killer in geoData.PipeKillers)
                {
                    foreach (var pipe in pipesf(_killer.ToPolygon()))
                    {
                        pipe.UserData = tag;
                    }
                }
                geoData.VerticalPipes = pipes.Where(x => x.UserData == null).Select(pipes).ToList(_pipes);
            }
            {
                foreach (var ct in geoData.Labels)
                {
                    if (ct.Text.StartsWith(THESAURUSADMINISTRATION))
                    {
                        ct.Text = ct.Text.Substring(THESAURUSACCUSTOMED);
                    }
                    else if (ct.Text.StartsWith(ADMINISTRATIVUS))
                    {
                        ct.Text = ct.Text.Substring(THESAURUSACCIDENT);
                    }
                    else if (ct.Text.StartsWith(THESAURUSALLOCATE))
                    {
                        ct.Text = THESAURUSALLOCATION + ct.Text.Substring(THESAURUSACCUSTOMED);
                    }
                }
            }
        }
        public static void CollectFloorListDatasEx(bool focus)
        {
            static StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb, List<BlockReference> brs)
            {
                var ctx = new StoreyContext();
                if (range != null)
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    ctx.thStoreys = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
                }
                else
                {
                    ctx.thStoreys = brs.Select(x => new ThMEPEngineCore.Model.Common.ThStoreys(x.ObjectId)).ToList();
                }
                var storeys = new List<ThStoreysData>();
                foreach (var s in ctx.thStoreys)
                {
                    var br = adb.Element<BlockReference>(s.ObjectId);
                    var data = new ThStoreysData()
                    {
                        Boundary = br.Bounds.ToGRect(),
                        Storeys = s.Storeys,
                        StoreyType = s.StoreyType,
                        ContraPoint = GetContraPoint(br),
                    };
                    storeys.Add(data);
                }
                FixStoreys(storeys);
                ctx.thStoreysDatas = storeys;
                return ctx;
            }
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = THESAURUSABDOMEN,
                    MessageForAdding = THESAURUSALTERNATE,
                };
                var dxfNames = new string[]
                {
                        RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                if (focus) FocusMainWindow();
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)
                {
                    var ctx = commandContext;
                    using var adb = AcadDatabase.Active();
                    var selectedIds = result.Value.GetObjectIds();
                    ctx.StoreyContext = GetStoreyContext(null, adb, selectedIds.Select(x => adb.Element<BlockReference>(x)).Where(x => x.GetEffectiveName() == ALTERNATIVENESS).ToList());
                    InitFloorListDatas(adb);
                }
            }
        }
        public static void CollectFloorListDatas()
        {
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return;
            var ctx = commandContext;
            ctx.range = range;
            using var adb = AcadDatabase.Active();
            ctx.StoreyContext = GetStoreyContext(range, adb);
            InitFloorListDatas(adb);
        }
        public static StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb)
        {
            var ctx = new StoreyContext();
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            ctx.thStoreys = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
            foreach (var s in ctx.thStoreys)
            {
                var e = adb.Element<Entity>(s.ObjectId);
                var data = new ThStoreysData()
                {
                    Boundary = e.Bounds.ToGRect(),
                    Storeys = s.Storeys,
                    StoreyType = s.StoreyType,
                };
                storeys.Add(data);
            }
            FixStoreys(storeys);
            ctx.thStoreysDatas = storeys;
            return ctx;
        }
        public static void InitFloorListDatas(AcadDatabase adb)
        {
            var ctx = commandContext.StoreyContext;
            var storeys = ctx.GetObjectIds()
            .Select(o => adb.Element<BlockReference>(o))
            .Where(o => o.ObjectId.IsValid && o.GetBlockEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
            .Select(o => o.ObjectId)
            .ToObjectIdCollection();
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            commandContext.ViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
        }
    }
    public static class THRainService
    {
        public static Point2d GetContraPoint(BlockReference br)
        {
            double dx = double.NaN;
            double dy = double.NaN;
            Point2d pt;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                if (p.PropertyName == THESAURUSALMANAC)
                {
                    dx = Convert.ToDouble(p.Value);
                }
                else if (p.PropertyName == THESAURUSALMIGHTY)
                {
                    dy = Convert.ToDouble(p.Value);
                }
            }
            if (!double.IsNaN(dx) && !double.IsNaN(dy))
            {
                pt = br.Position.ToPoint2d() + new Vector2d(dx, dy);
            }
            else
            {
                throw new System.Exception(THESAURUSALMOST);
            }
            return pt;
        }
        public static void FixStoreys(List<ThStoreysData> storeys)
        {
            var lst1 = storeys.Where(s => s.Storeys.Count == THESAURUSACCESSION).Select(s => s.Storeys[QUOTATIONSHAKES]).ToList();
            foreach (var s in storeys.Where(s => s.Storeys.Count > THESAURUSACCESSION).ToList())
            {
                var hs = new HashSet<int>(s.Storeys);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Storeys.Clear();
                s.Storeys.AddRange(hs.OrderBy(i => i));
            }
        }
        public const int QUOTATIONSHAKES = 0;
        public const int INTENSIFICATION = 40;
        public const int INTERDIGITATING = 36;
        public const int ADRENOCORTICOTROPHIC = 1500;
        public const int QUOTATIONSPENSER = 6;
        public const double THESAURUSABANDON = .1;
        public const int THESAURUSABANDONED = 4096;
        public const string THESAURUSABASEMENT = "卫生间";
        public const string THESAURUSABATEMENT = "主卫";
        public const string THESAURUSABBREVIATE = "公卫";
        public const string THESAURUSABBREVIATION = "次卫";
        public const string THESAURUSABDICATE = "客卫";
        public const string THESAURUSABDICATION = "洗手间";
        public const bool THESAURUSABDOMEN = false;
        public const bool THESAURUSABDOMINAL = true;
        public const string THESAURUSABDUCT = "卫";
        public const string THESAURUSABERRANT = @"^[卫]\d$";
        public const string THESAURUSABERRATION = "厨房";
        public const string REFRANGIBILITIES = "西厨";
        public const string THESAURUSABEYANCE = "厨";
        public const string THESAURUSABHORRENT = "阳台";
        public const string THESAURUSABIDING = "连廊";
        public const string THESAURUSABILITY = "Y1L";
        public const string ABITURIENTENEXAMEN = "Y2L";
        public const string THESAURUSABJECT = "NL";
        public const string THESAURUSABJURE = "YL";
        public const string THESAURUSABLAZE = @"^W\d?L";
        public const string THESAURUSABLUTION = @"^F\d?L";
        public const string THESAURUSABNEGATION = "-0";
        public const string THESAURUSABNORMAL = @"^P\d?L";
        public const string PROGNOSTICATION = @"^T\d?L";
        public const string THESAURUSABOLISH = @"^D\d?L";
        public const string THESAURUSABORTIVE = "W-DRAI-NOTE";
        public const string THESAURUSABRASIVE = "W-DRAI-DIMS";
        public const int THESAURUSABRUPT = 8000;
        public const string THESAURUSABSENCE = "W-RAIN-EQPM";
        public const string THESAURUSABSENT = "W-DRAI-EQPM";
        public const int THESAURUSABSOLUTE = 55;
        public const string THESAURUSABSORB = "W-RAIN-PIPE";
        public const string THESAURUSABSORBENT = "W-FRPT-NOTE";
        public const string QUOTATIONABSORBENT = "W-RAIN-NOTE";
        public const string ELECTROMAGNETIC = "W-RAIN-DIMS";
        public const string CHARACTERISTICALLY = "W-WSUP-DIMS";
        public const string THESAURUSABSORPTION = "W-WSUP-NOTE";
        public const string THESAURUSABSTAIN = "W-DRAI-DOME-PIPE";
        public const int THESAURUSABSTINENCE = 35;
        public const string REPRESENTATIONAL = "W-BUSH";
        public const string THESAURUSABSTRACT = "套管";
        public const int THESAURUSABSTRACTED = 1000;
        public const string THESAURUSABSTRACTION = "带定位立管";
        public const int INATTENTIVENESS = 100;
        public const string THESAURUSABSTRUSE = "WP_KTN_LG";
        public const int INCOMPREHENSIBLE = 50;
        public const string THESAURUSABSURD = "$LIGUAN";
        public const string THESAURUSABUNDANT = "VPIPE-污水";
        public const string THESAURUSABUSIVE = "立管编号";
        public const string THESAURUSABYSMAL = "VPIPE-给水";
        public const string CONVENTIONALIZED = "TCH_PIPE";
        public const int THESAURUSACCEDE = 20;
        public const string THESAURUSACCELERATE = "TCH_VPIPEDIM";
        public const string THESAURUSACCELERATION = "TCH_TEXT";
        public const string THESAURUSACCENT = "De";
        public const string THESAURUSACCENTUATE = "雨水井编号";
        public const string THESAURUSACCEPT = "-";
        public const string THESAURUSACCEPTABLE = "";
        public const string THESAURUSACCEPTANCE = "污废合流井编号";
        public const string ACKNOWLEDGEMENT = "地漏";
        public const string FACESFAVOURABLE = "雨水口";
        public const char THESAURUSACCESS = 'A';
        public const char APPROACHABILITY = 'B';
        public const string INTERCHANGEABLY = "RF";
        public const string THESAURUSACCESSIBLE = "RF+1";
        public const int THESAURUSACCESSION = 1;
        public const string THESAURUSACCESSORY = "RF+2";
        public const int THESAURUSACCIDENT = 2;
        public const string UNINTENTIONALLY = "F";
        public const string THESAURUSACCIDENTAL = @"^重力型雨水斗(DN\d+)$";
        public const string ENTHUSIASTICALLY = "接至";
        public const int THESAURUSACCLAIM = 10;
        public const string CONGRATULATIONS = "重力型雨水斗DN100";
        public const string THESAURUSACCLAMATION = "侧入型雨水斗DN100";
        public const string ACCLIMATIZATION = "1F";
        public const string THESAURUSACCOMMODATE = "W-NOTE";
        public const double ACCOMMODATINGLY = 2500.0;
        public const double ACCOMMODATIVENESS = 5500.0;
        public const int ACCOMMODATIONAL = 500;
        public const double THESAURUSACCOMPANIMENT = 1800.0;
        public const double THESAURUSACCOMPANY = 500.0;
        public const double THESAURUSACCOMPLICE = 2000.0;
        public const string THESAURUSACCOMPLISH = "通气帽系统";
        public const string ACCOMPLISSEMENT = "距离1";
        public const string THESAURUSACCOMPLISHMENT = "可见性1";
        public const string ACCOMPLISHMENTS = "伸顶通气管";
        public const string THESAURUSACCORD = "1000";
        public const int THESAURUSACCORDANCE = 580;
        public const string THESAURUSACCORDING = "标高";
        public const int CORRESPONDINGLY = 550;
        public const string THESAURUSACCORDINGLY = "建筑完成面";
        public const double THESAURUSACCOST = .0;
        public const int THESAURUSACCOUNT = 1800;
        public const int ACCOUNTABLENESS = 780;
        public const int THESAURUSACCOUNTABLE = 121;
        public const int THESAURUSACCREDIT = 1258;
        public const int THESAURUSACCRUE = 120;
        public const int ACCULTURATIONAL = 779;
        public const int THESAURUSACCUMULATE = 1679;
        public const int THESAURUSACCUMULATION = 658;
        public const int ACCUMULATIVENESS = 90;
        public const double THESAURUSACCURACY = 0.0;
        public const int THESAURUSACCURATE = 300;
        public const int THESAURUSACCUSATION = 24;
        public const int QUOTATIONACCUSATIVE = 9;
        public const int THESAURUSACCUSE = 5;
        public const int THESAURUSACCUSTOM = 4;
        public const int THESAURUSACCUSTOMED = 3;
        public const int POLYOXYMETHYLENE = 350;
        public const int DEHYDROGENATION = 3600;
        public const string NEUROTRANSMITTER = "散排至";
        public const string ACETYLCHOLINESTERASE = "重力";
        public const string ACETYLSALICYLIC = "DN100";
        public const int THESAURUSACHIEVE = 125;
        public const int COMPETITIVENESS = 83;
        public const int THESAURUSACHIEVEMENT = 215;
        public const int INVULNERABILITY = 318;
        public const int ACHLOROPHYLLOUS = 800;
        public const int ACHONDROPLASIAC = 2000;
        public const int ACHONDROPLASTIC = 400;
        public const int UNCHRONOLOGICAL = 600;
        public const int QUOTATIONCARLYLE = 479;
        public const int ACKNOWLEDGEABLE = 1879;
        public const int THESAURUSACKNOWLEDGE = 950;
        public const int THESAURUSACKNOWLEDGEMENT = 1300;
        public const int THESAURUSACOLYTE = 200;
        public const int THESAURUSACQUAINT = 1600;
        public const int ACQUAINTANCESHIP = 1650;
        public const string THESAURUSACQUAINTANCE = "接至排水沟";
        public const int THESAURUSACQUAINTED = 160;
        public const int THESAURUSACQUIESCE = 360;
        public const int THESAURUSACQUIESCENCE = 130;
        public const int THESAURUSACQUIESCENT = 1070;
        public const int THESAURUSACQUIRE = 650;
        public const int THESAURUSACQUIREMENT = 30;
        public const string THESAURUSACQUISITION = "DN25";
        public const int ACQUISITIVENESS = 750;
        public const int THESAURUSACQUISITIVE = 150;
        public const int THESAURUSACQUIT = 1280;
        public const int THESAURUSACQUITTAL = 450;
        public const double PHARMACEUTICALS = .7;
        public const int ACRIMONIOUSNESS = 1400;
        public const string THESAURUSACRIMONIOUS = ";";
        public const double THESAURUSACRIMONY = .01;
        public const double THESAURUSACTING = 10e6;
        public const string ACTINOMYCETALES = "TH-STYLE3";
        public const int THESAURUSACTION = 745;
        public const string THESAURUSACTIVATE = "乙字弯";
        public const string CHARACTERISTICS = "立管检查口";
        public const string THESAURUSACTIVE = "套管系统";
        public const string THESAURUSACTIVITY = "可见性";
        public const string THESAURUSACTUAL = "防水套管水平";
        public const string THESAURUSACTUALLY = "重力流雨水井编号";
        public const string THESAURUSACTUATE = "普通地漏无存水弯";
        public const string THESAURUSACUMEN = "地漏系统";
        public const string ACUPUNCTURATION = "666";
        public const string THESAURUSADAPTABLE = ",";
        public const int THESAURUSADAPTATION = 7;
        public const int FAMILIARIZATION = 229;
        public const int THESAURUSADDENDUM = 230;
        public const int THESAURUSADDICT = 8192;
        public const string THESAURUSADDICTED = "FromImagination";
        public const int THESAURUSADDICTION = 700;
        public const string THESAURUSADDITION = "排出：";
        public const int THESAURUSADDITIONAL = 666;
        public const string SUPERIMPOSITION = "排出套管：";
        public const string THESAURUSADDITIVE = @"接(\d+F)屋面雨水斗";
        public const string THESAURUSADDRESS = "立管：";
        public const string THESAURUSADDUCE = "长转管:";
        public const string THESAURUSADEQUACY = "短转管:";
        public const string THESAURUSADEQUATE = "地漏：";
        public const string TRANSUBSTANTIATION = "地漏套管：";
        public const string THESAURUSADHERE = "HasCondensePipe：";
        public const string THESAURUSADHERENT = "HasBrokenCondensePipes：";
        public const string THESAURUSADHESIVE = "HasNonBrokenCondensePipes：";
        public const string QUOTATIONADIPOSE = "HasRainPortSymbols：";
        public const int THESAURUSADJACENT = 255;
        public const string QUOTATIONADJACENT = "GravityWaterBucket";
        public const string THESAURUSADJOURN = "SideWaterBucket";
        public const int THESAURUSADJOURNMENT = 0x91;
        public const int DISCONTINUATION = 0xc7;
        public const int THESAURUSADJUDICATE = 0xae;
        public const int THESAURUSADJUDICATION = 211;
        public const int THESAURUSADJUNCT = 213;
        public const int THESAURUSADJUST = 111;
        public const string THESAURUSADJUSTMENT = "AI-空间框线";
        public const string EXTEMPORANEOUSLY = "AI-空间名称";
        public const int THESAURUSADMINISTER = 250;
        public const string THESAURUSADMINISTRATION = "73-";
        public const string ADMINISTRATIVUS = "1-";
        public const string ADMINISTRATIVELY = "单排";
        public const string THESAURUSADMINISTRATIVE = "雨水斗";
        public const string ADMINISTRATORSHIP = "RML";
        public const string ADMINISTRATRESS = "RMHL";
        public const string THESAURUSADMINISTRATOR = "J1L";
        public const string ADMINISTRATRICE = "J2L";
        public const string THESAURUSADMIRATION = "CYSD";
        public const string THESAURUSADMIRE = "$QYSD";
        public const string THESAURUSADMISSIBLE = "87型雨水斗DN100";
        public const string THESAURUSADMISSION = "87";
        public const string THESAURUSADMITTANCE = "87WaterBuckets";
        public const string THESAURUSADMONISH = "重力流雨水斗";
        public const string THESAURUSADMONITION = "bk对位";
        public const string THESAURUSADMONITORY = "FloorDrains";
        public const string THESAURUSADOLESCENCE = "GravityWaterBuckets";
        public const string THESAURUSADOLESCENT = "SideWaterBuckets";
        public const string THESAURUSADOPTION = "DN";
        public const string THESAURUSADORABLE = "侧入";
        public const string THESAURUSADORATION = @"(DN\d+)";
        public const int AUTHORITARIANISM = 15;
        public const string QUOTATIONADRENAL = "重力雨水斗";
        public const string THESAURUSADRIFT = "87雨水斗";
        public const string THESAURUSADROIT = "侧入式雨水斗";
        public const string ADSIGNIFICATION = "DL";
        public const string THESAURUSADULTERATE = "TCH_MULTILEADER";
        public const int THESAURUSADVANTAGE = 180;
        public const int ADVANTAGEOUSNESS = 2161;
        public const int THESAURUSADVANTAGEOUS = 387;
        public const int THESAURUSADVENT = 1106;
        public const int QUOTATIONSECOND = 1712;
        public const int THESAURUSADVENTITIOUS = 198;
        public const int THESAURUSADVENTURE = 353;
        public const int ADVENTUROUSNESS = 3860;
        public const string THESAURUSADVENTUROUS = "WaterWellIds:";
        public const string THESAURUSADVERSARY = "WaterWellWrappingPipeRadiusStringDict:";
        public const string THESAURUSADVERSE = "HasSingleFloorDrainDrainage:";
        public const string DISADVANTAGEOUS = "FloorDrainShareDrainageWithVerticalPipe:";
        public const int THESAURUSADVERSITY = 621;
        public const int THESAURUSADVERTISE = 1200;
        public const int ADVERTISEMENTAL = 431;
        public const int THESAURUSALLIED = 2200;
        public const string ALLITERATIVENESS = "13#雨水口";
        public const string THESAURUSALLOCATE = "LN-";
        public const string THESAURUSALLOCATION = "NL-";
        public const string THESAURUSALLOWANCE = "150";
        public const string THESAURUSALLUDE = "X.XX";
        public const double THESAURUSALLURE = .8;
        public const string QUOTATIONALMAIN = "TCH_MTEXT";
        public const string THESAURUSALMANAC = "基点 X";
        public const string THESAURUSALMIGHTY = "基点 Y";
        public const string THESAURUSALMOST = "error occured while getting baseX and baseY";
        public const string THESAURUSALREADY = @"^(Y1L|Y2L|NL)(\w*)\-(\d*)([a-zA-Z]*)$";
        public const string THESAURUSALTERATION = "RainPortIds:";
        public const string TRANSFIGURATION = "HasSingleFloorDrainDrainageForRainPort:";
        public const string THESAURUSALTERCATION = "FloorDrainShareDrainageWithVerticalPipeForRainPort:";
        public const string THESAURUSALTERNATE = "请选择楼层框线";
        public const string ALTERNATIVENESS = "楼层框定";
        public const string THESAURUSALTERNATIVE = "侧入式雨水斗DN100";
        public const string THESAURUSALTHOUGH = "TCH_EQUIPMENT";
        public const string THESAURUSALTITUDE = "地库";
        public const string THESAURUSALTOGETHER = "JL";
        public const string QUOTATIONALUMINIUM = "XL";
        public const string QUOTATIONALVEOLAR = "JG";
        public const string THESAURUSALWAYS = "X1";
        public const string THESAURUSAMALGAMATE = "X2";
        public const string THESAURUSAMATEUR = "ZP";
        public const string AMBIDEXTROUSNESS = "A$C6BDE4816";
        public static bool IsRainLabel(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsRainLabel(label) || IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsWaterBucketLabel(string label)
        {
            return label.Contains(THESAURUSADMINISTRATIVE);
        }
        public static bool IsGravityWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(ACETYLCHOLINESTERASE);
        }
        public static bool IsSideWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(THESAURUSADORABLE);
        }
        public static bool Is87WaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && !IsGravityWaterBucketLabel(label) && !IsSideWaterBucketLabel(label) && label.Contains(THESAURUSADMISSION);
        }
        public static string GetDN(string label, string dft = ACETYLSALICYLIC)
        {
            if (label == null) return dft;
            var m = Regex.Match(label, THESAURUSADORATION, RegexOptions.IgnoreCase);
            if (m.Success) return m.Value;
            return dft;
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label)
                || IsRainLabel(label) || label.StartsWith(THESAURUSABJURE)
                || label.Contains(ADMINISTRATIVELY) || label.Contains(THESAURUSADMINISTRATIVE)
                || label.StartsWith(ADMINISTRATORSHIP) || label.StartsWith(ADMINISTRATRESS)
                || label.StartsWith(THESAURUSADMINISTRATOR) || label.StartsWith(ADMINISTRATRICE) || label.StartsWith(THESAURUSALTOGETHER) || label.StartsWith(QUOTATIONALVEOLAR)
                || label.StartsWith(QUOTATIONALUMINIUM) || label.StartsWith(THESAURUSALWAYS) || label.StartsWith(THESAURUSAMALGAMATE)
                || label.StartsWith(THESAURUSAMATEUR)
                ;
        }
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                THESAURUSABASEMENT,THESAURUSABATEMENT,THESAURUSABBREVIATE,
                THESAURUSABBREVIATION,THESAURUSABDICATE,THESAURUSABDICATION,
            };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            if (roomName.Equals(THESAURUSABDUCT))
                return THESAURUSABDOMINAL;
            return Regex.IsMatch(roomName, THESAURUSABERRANT);
        }
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSABERRATION, REFRANGIBILITIES };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            if (roomName.Equals(THESAURUSABEYANCE))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsBalcony(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSABHORRENT };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSABIDING };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsY1L(string label)
        {
            return label.StartsWith(THESAURUSABILITY);
        }
        public static bool IsY2L(string label)
        {
            return label.StartsWith(ABITURIENTENEXAMEN);
        }
        public static bool IsNL(string label)
        {
            return label.StartsWith(THESAURUSABJECT);
        }
        public static bool IsYL(string label)
        {
            return label.StartsWith(THESAURUSABJURE);
        }
        public static bool IsWL(string label)
        {
            return Regex.IsMatch(label, THESAURUSABLAZE);
        }
        public static bool IsFL(string label)
        {
            return Regex.IsMatch(label, THESAURUSABLUTION);
        }
        public static bool IsFL0(string label)
        {
            return IsFL(label) && label.Contains(THESAURUSABNEGATION);
        }
        public static bool IsPL(string label)
        {
            return Regex.IsMatch(label, THESAURUSABNORMAL);
        }
        public static bool IsTL(string label)
        {
            return Regex.IsMatch(label, PROGNOSTICATION);
        }
        public static bool IsDL(string label)
        {
            return Regex.IsMatch(label, THESAURUSABOLISH);
        }
    }
    public class ThStoreysData
    {
        public Point2d ContraPoint;
        public GRect Boundary;
        public List<int> Storeys;
        public ThMEPEngineCore.Model.Common.StoreyType StoreyType;
    }
}