using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using Newtonsoft.Json;
using NFox.Cad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Assistant;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
namespace ThMEPWSS.Pipe.Model
{
    public struct VT1<T1>
    {
        public T1 Item1;
        public VT1(T1 m1) { Item1 = m1; }
    }
    public struct VT2<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public VT2(T1 m1, T2 m2) { Item1 = m1; Item2 = m2; }
    }
    public struct VT3<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public VT3(T1 m1, T2 m2, T3 m3) { Item1 = m1; Item2 = m2; Item3 = m3; }
    }
    public struct VT4<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public VT4(T1 m1, T2 m2, T3 m3, T4 m4) { Item1 = m1; Item2 = m2; Item3 = m3; Item4 = m4; }
    }
    public static class VTFac
    {
        public static VT1<T1> Create<T1>(T1 m1) => new VT1<T1>(m1);
        public static VT2<T1, T2> Create<T1, T2>(T1 m1, T2 m2) => new VT2<T1, T2>(m1, m2);
        public static VT3<T1, T2, T3> Create<T1, T2, T3>(T1 m1, T2 m2, T3 m3) => new VT3<T1, T2, T3>(m1, m2, m3);
        public static VT4<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 m1, T2 m2, T3 m3, T4 m4) => new VT4<T1, T2, T3, T4>(m1, m2, m3, m4);
    }
    public class LabelItem
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
        static readonly Regex re = new Regex(@"^(Y1L|Y2L|NL)(\w*)\-(\w*)([a-zA-Z]*)$");
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[1].Value,
                D1S = m.Groups[2].Value,
                D2S = m.Groups[3].Value,
                Suffix = m.Groups[4].Value,
            };
        }
    }
    public class WaterBucketDrawingContext
    {
        public Point3d BasePoint;
        public List<ThWSDStorey> WSDStoreys;
        public RainSystemDrawingContext RainSystemDrawingContext;
        public WaterBucketDrawingContext Clone()
        {
            return (WaterBucketDrawingContext)this.MemberwiseClone();
        }
    }
    public class RainSystemDrawingContext
    {
        public Point3d BasePoint;
        public Point3d? WaterBucketPoint;
        public List<ThWSDStorey> WSDStoreys => RainSystemDiagram.WSDStoreys;
        public List<StoreyDrawingContext> StoreyDrawingContexts;
        public ThWRainSystemDiagram RainSystemDiagram;
        public IList ThWRainPipeSystemGroup;
        public Point3d OutputBasePoint;
        public VerticalPipeType VerticalPipeType;
        public RainSystemDrawingContext Clone()
        {
            return (RainSystemDrawingContext)this.MemberwiseClone();
        }
    }
    public class StoreyDrawingContext
    {
        public ThWSDStorey WSDStorey;
        public Point3d BasePoint;
        public double StoreyLineLength;
        public ThWSDStorey Storey;
        public List<StoreyDrawingContext> StoreyDrawingContexts;
        public StoreyDrawingContext Clone()
        {
            return (StoreyDrawingContext)this.MemberwiseClone();
        }
    }
    public class ThWRainSystemDiagram : ThWSystemDiagram
    {
        private int VerticalStoreyLineSpan;
        public List<ThWSDStorey> WSDStoreys { get; set; } = new List<ThWSDStorey>();
        public List<ThWRoofRainPipeSystem> RoofVerticalRainPipes { get; set; } = new List<ThWRoofRainPipeSystem>();
        public List<ThWBalconyRainPipeSystem> BalconyVerticalRainPipes { get; set; } = new List<ThWBalconyRainPipeSystem>();
        public List<ThWCondensePipeSystem> CondenseVerticalRainPipes { get; set; } = new List<ThWCondensePipeSystem>();
        private ThMEPWSS.Pipe.Service.ThRainSystemService thRainSystemService;
        public ThWRainSystemDiagram(int span = 2000)
        {
            VerticalStoreyLineSpan = span;
        }
        public void InitServices(AcadDatabase acadDatabase, Point3dCollection range)
        {
            ThMEPWSS.Pipe.Service.ThRainSystemService.ImportElementsFromStdDwg();
            thRainSystemService = new ThMEPWSS.Pipe.Service.ThRainSystemService
            {
                adb = acadDatabase,
            };
            thRainSystemService.InitCache();
            thRainSystemService.CollectData(range);
        }
        public void InitStoreys(List<ThIfcSpatialElement> storeys)
        {
            CollectData(storeys);
        }
        private void CollectData(List<ThIfcSpatialElement> storeys)
        {
            using (var adb = AcadDatabase.Active())
            {
                var LargeRoofVPTexts = new List<string>();
                foreach (var s in storeys)
                {
                    if (s is ThStoreys sobj)
                    {
                        var blk = adb.Element<BlockReference>(sobj.ObjectId);
                        var pts = blk.GeometricExtents.ToRectangle().Vertices();
                        switch (sobj.StoreyType)
                        {
                            case StoreyType.LargeRoof:
                                {
                                    LargeRoofVPTexts = thRainSystemService.GetVerticalPipeNotes(pts);
                                    var vps1 = new List<ThWSDPipe>();
                                    LargeRoofVPTexts.ForEach(pt =>
                                    {
                                        thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string dn);
                                        vps1.Add(new ThWSDPipe() { Label = pt, DN = dn });
                                    });
                                    WSDStoreys.Add(new ThWSDStorey() { Label = $"RF", Range = pts, VerticalPipes = vps1, ObjectID = sobj.ObjectId, BlockRef = blk });
                                    break;
                                }
                            case StoreyType.SmallRoof:
                                {
                                    var smallRoofVPTexts = thRainSystemService.GetVerticalPipeNotes(pts);
                                    var rf1Storey = new ThWSDStorey() { Label = $"RF+1", Range = pts, ObjectID = sobj.ObjectId, BlockRef = blk };
                                    WSDStoreys.Add(rf1Storey);
                                    if (LargeRoofVPTexts.Count > 0)
                                    {
                                        var rf2VerticalPipeText = smallRoofVPTexts.Except(LargeRoofVPTexts);
                                        if (rf2VerticalPipeText.Count() == 0)
                                        {
                                            var vps1 = new List<ThWSDPipe>();
                                            smallRoofVPTexts.ForEach(pt =>
                                            {
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string dn);
                                                vps1.Add(new ThWSDPipe() { Label = pt, DN = dn });
                                            });
                                            rf1Storey.VerticalPipes = vps1;
                                        }
                                        else
                                        {
                                            var rf1VerticalPipeObjects = new List<ThWSDPipe>();
                                            var rf1VerticalPipeTexts = smallRoofVPTexts.Except(rf2VerticalPipeText);
                                            rf1VerticalPipeTexts.ForEach(pt =>
                                            {
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string dn);
                                                rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, DN = dn });
                                            });
                                            rf1Storey.VerticalPipes = rf1VerticalPipeObjects;
                                            var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                                            rf2VerticalPipeText.ForEach(pt =>
                                            {
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string dn);
                                                rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, DN = dn });
                                            });
                                            WSDStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Range = pts, VerticalPipes = rf2VerticalPipeObjects, ObjectID = sobj.ObjectId, BlockRef = blk });
                                        }
                                    }
                                    break;
                                }
                            case StoreyType.StandardStorey:
                            case StoreyType.NonStandardStorey:
                                sobj.Storeys.ForEach(i => WSDStoreys.Add(new ThWSDStorey() { Label = $"{i}F", Range = pts, ObjectID = sobj.ObjectId, BlockRef = blk }));
                                break;
                            case StoreyType.Unknown:
                            default:
                                break;
                        }
                    }
                }
            }
        }
        public void InitVerticalPipeSystems(Point3dCollection range)
        {
            InitRoofPipeSystems(range);
            InitBalconyPipeSystems(range);
            InitCondensePipeSystems(range);
            var sv = thRainSystemService;
            var condensePipesCache = new Cache<Point3dCollection, List<Entity>>(rg =>
            {
                return sv.FiltByRect(rg, sv.CondensePipes).ToList();
            });
            var floorDrainsCache = new Cache<Point3dCollection, List<Entity>>(rg =>
            {
                return sv.FiltByRect(rg, sv.FloorDrains).ToList();
            });
            foreach (var sys in BalconyVerticalRainPipes)
            {
                CollectEnts(condensePipesCache, floorDrainsCache, sys);
            }
            foreach (var sys in CondenseVerticalRainPipes)
            {
                CollectEnts(condensePipesCache, floorDrainsCache, sys);
            }
            foreach (var sys in BalconyVerticalRainPipes)
            {
                foreach (var r in sys.PipeRuns)
                {
                    r.HasBrokenCondensePipe = sv.HasBrokenCondensePipe(r.Storey.Range, r.MainRainPipe.Label);
                }
            }
            foreach (var sys in CondenseVerticalRainPipes)
            {
                foreach (var r in sys.PipeRuns)
                {
                    r.HasBrokenCondensePipe = sv.HasBrokenCondensePipe(r.Storey.Range, r.MainRainPipe.Label);
                }
            }
        }
        private void CollectEnts<T>(Cache<Point3dCollection, List<Entity>> condensePipesCache, Cache<Point3dCollection, List<Entity>> floorDrainsCache, T sys) where T : ThWRainPipeSystem
        {
            CollectCondensePipes(condensePipesCache, sys);
            CollectFloorDrains(floorDrainsCache, sys);
        }
        private void CollectFloorDrains<T>(Cache<Point3dCollection, List<Entity>> floorDrainsCache, T sys) where T : ThWRainPipeSystem
        {
            foreach (var r in sys.PipeRuns)
            {
                foreach (var e in floorDrainsCache[r.Storey.Range])
                {
                    if (thRainSystemService.GetLabel(e) == r.MainRainPipe.Label)
                    {
                        r.FloorDrains.Add(new ThWSDFloorDrain() { DN = "DN75", HasDrivePipe = thRainSystemService.HasDrivePipe(e) });
                    }
                }
            }
        }
        private void CollectCondensePipes<T>(Cache<Point3dCollection, List<Entity>> condensePipesCache, T sys) where T : ThWRainPipeSystem
        {
            foreach (var r in sys.PipeRuns)
            {
                foreach (var e in condensePipesCache[r.Storey.Range])
                {
                    if (thRainSystemService.GetLabel(e) == r.MainRainPipe.Label)
                    {
                        r.CondensePipes.Add(new ThWSDCondensePipe() { DN = "DN25", IsLow = thRainSystemService.IsCondensePipeLow(e) });
                    }
                }
            }
        }
        public ThWSDStorey GetHigerStorey(ThWSDStorey storey)
        {
            if (storey == null) return null;
            storey = WSDStoreys.FirstOrDefault(s => s.Label == storey.Label);
            if (storey == null) return null;
            var i = WSDStoreys.IndexOf(storey);
            if (i + 1 < WSDStoreys.Count) return WSDStoreys[i + 1];
            return null;
        }
        public ThWSDStorey GetStorey(string label)
        {
            var i = GetStoreyIndex(label);
            if (i >= 0) return WSDStoreys[i];
            return null;
        }
        public int GetStoreyIndex(string label)
        {
            var s = WSDStoreys.FirstOrDefault(s => s.Label == label);
            return WSDStoreys.IndexOf(s);
        }
        public ThWSDStorey GetLowerStorey(ThWSDStorey storey)
        {
            if (storey == null) return null;
            storey = WSDStoreys.FirstOrDefault(s => s.Label == storey.Label);
            if (storey == null) return null;
            var i = WSDStoreys.IndexOf(storey);
            if (i - 1 >= 0) return WSDStoreys[i - 1];
            return null;
        }
        private void InitRoofPipeSystems(Point3dCollection selectedRange)
        {
            var allRoofPipeNotes = thRainSystemService.GetRoofVerticalPipeNotes(selectedRange);
            var distinctRoofPipeNotes = allRoofPipeNotes.Distinct().ToList();
            foreach (var roofPipeNote in distinctRoofPipeNotes)
            {
                var rainPipeSystem = new ThWRoofRainPipeSystem() { VerticalPipeId = roofPipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(roofPipeNote, out string dn);
                if (string.IsNullOrEmpty(dn)) dn = "DN100";
                AddPipeRunsForRoof(roofPipeNote, rainPipeSystem, PipeRuns, dn);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, roofPipeNote);
                SetCheckPoints(rainPipeSystem);
                this.RoofVerticalRainPipes.Add(rainPipeSystem);
            }
        }
        private void InitCondensePipeSystems(Point3dCollection selectedRange)
        {
            var allCondensePipeNotes = thRainSystemService.GetCondenseVerticalPipeNotes(selectedRange);
            var distinctCondensePipeNotes = allCondensePipeNotes.Distinct().ToList();
            foreach (var condensePipeNote in distinctCondensePipeNotes)
            {
                var rainPipeSystem = new ThWCondensePipeSystem() { VerticalPipeId = condensePipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                AddPipeRunsForCondensePipe(condensePipeNote, rainPipeSystem, PipeRuns);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, condensePipeNote);
                SetCheckPoints(rainPipeSystem);
                this.CondenseVerticalRainPipes.Add(rainPipeSystem);
            }
        }
        private void AddPipeRunsForCondensePipe(string condensePipeNote, ThWCondensePipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns)
        {
            foreach (var s in WSDStoreys)
            {
                if (s.Label.Equals("RF+2"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(condensePipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(condensePipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else if (s.Label.Equals("RF+1"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(condensePipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(condensePipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else
                {
                    var pipes = thRainSystemService.GetCondenseVerticalPipeNotes(s.Range);
                    var translatorType = GetTranslatorType(s, condensePipeNote);
                    if (pipes.Contains(condensePipeNote))
                    {
                        thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(condensePipeNote, out string dn);
                        PipeRuns.Add(new ThWRainPipeRun()
                        {
                            Storey = s,
                            MainRainPipe = new ThWSDPipe()
                            {
                                Label = condensePipeNote,
                                PipeType = VerticalPipeType.CondenseVerticalPipe,
                                DN = dn,
                            },
                            TranslatorPipe = new ThWSDTranslatorPipe()
                            {
                                DN = dn,
                                Label = "",
                                PipeType = VerticalPipeType.CondenseVerticalPipe,
                                TranslatorType = translatorType
                            }
                        });
                    }
                }
            }
        }
        private void InitBalconyPipeSystems(Point3dCollection selectedRange)
        {
            var allBalconyPipeNotes = thRainSystemService.GetBalconyVerticalPipeNotes(selectedRange);
            var distinctBalconyPipeNotes = allBalconyPipeNotes.Distinct().ToList();
            foreach (var balconyPipeNote in distinctBalconyPipeNotes)
            {
                var rainPipeSystem = new ThWBalconyRainPipeSystem() { VerticalPipeId = balconyPipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                AddPipeRunsForBalcony(balconyPipeNote, rainPipeSystem, PipeRuns);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, balconyPipeNote);
                SetCheckPoints(rainPipeSystem);
                this.BalconyVerticalRainPipes.Add(rainPipeSystem);
            }
        }
        private void AddPipeRunsForBalcony(string balconyPipeNote, ThWBalconyRainPipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns)
        {
            foreach (var s in WSDStoreys)
            {
                if (s.Label.Equals("RF+2"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(balconyPipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(balconyPipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else if (s.Label.Equals("RF+1"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(balconyPipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(balconyPipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else
                {
                    var pipes = thRainSystemService.GetBalconyVerticalPipeNotes(s.Range);
                    var translatorType = GetTranslatorType(s, balconyPipeNote);
                    if (pipes.Contains(balconyPipeNote))
                    {
                        thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(balconyPipeNote, out string dn);
                        PipeRuns.Add(new ThWRainPipeRun()
                        {
                            Storey = s,
                            MainRainPipe = new ThWSDPipe()
                            {
                                Label = balconyPipeNote,
                                PipeType = VerticalPipeType.BalconyVerticalPipe,
                                DN = dn,
                            },
                            TranslatorPipe = new ThWSDTranslatorPipe()
                            {
                                DN = dn,
                                Label = "",
                                PipeType = VerticalPipeType.BalconyVerticalPipe,
                                TranslatorType = translatorType
                            }
                        });
                    }
                }
            }
        }
        public static void SetCheckPoints(List<ThWRainPipeRun> runs)
        {
            for (int i = 1; i < runs.Count; i++)
            {
                if (runs[i - 1].TranslatorPipe.TranslatorType == TranslatorTypeEnum.Long)
                {
                    runs[i].CheckPoint.HasCheckPoint = true;
                }
            }
        }
        public static void SetCheckPoints(ThWRainPipeSystem sys)
        {
            if (sys.OutputType.OutputType.Equals(RainOutputTypeEnum.RainPort) || sys.OutputType.OutputType.Equals(RainOutputTypeEnum.WaterWell))
            {
                if (sys.PipeRuns.Count >= 2)
                    sys.PipeRuns[0].CheckPoint.HasCheckPoint = true;
            }
            foreach (var run in sys.PipeRuns)
            {
                if (run.Storey?.Label == "1F")
                {
                    if (sys.PipeRuns.Count > 1 || sys.WaterBucket.Storey != null)
                    {
                        run.CheckPoint.HasCheckPoint = true;
                    }
                }
            }
        }
        private TranslatorTypeEnum GetTranslatorType(ThWSDStorey s, string verticalPipeID)
        {
            var ret = thRainSystemService.GetTranslatorType(s.Range, verticalPipeID);
            if (s.Label.Equals("1F") && ret == TranslatorTypeEnum.Long)
            {
                return TranslatorTypeEnum.None;
            }
            return ret;
        }
        private void AddPipeRunsForRoof(string roofPipeNote, ThWRoofRainPipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns, string dn)
        {
            bool bSetWaterBucket = false;
            foreach (var s in WSDStoreys)
            {
                if (s.Label.Equals("RF+2"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(roofPipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(roofPipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else if (s.Label.Equals("RF+1"))
                {
                    if (s.VerticalPipes.Select(vp => vp.Label).Contains(roofPipeNote))
                    {
                        var matchedPipe = s.VerticalPipes.Where(vp => vp.Label.Equals(roofPipeNote)).FirstOrDefault();
                        PipeRuns.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                    }
                }
                else
                {
                    var pipes = thRainSystemService.GetRoofVerticalPipeNotes(s.Range);
                    var translatorType = GetTranslatorType(s, roofPipeNote);
                    if (pipes.Contains(roofPipeNote))
                    {
                        PipeRuns.Add(new ThWRainPipeRun()
                        {
                            Storey = s,
                            MainRainPipe = new ThWSDPipe()
                            {
                                Label = roofPipeNote,
                                PipeType = VerticalPipeType.RoofVerticalPipe,
                                DN = dn,
                            },
                            TranslatorPipe = new ThWSDTranslatorPipe()
                            {
                                DN = dn,
                                Label = "",
                                PipeType = VerticalPipeType.RoofVerticalPipe,
                                TranslatorType = translatorType
                            }
                        });
                    }
                }
                if (bSetWaterBucket == false)
                {
                    Point3d roofPipeCenter = new Point3d();
                    var brst = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote, ref roofPipeCenter);
                    if (brst)
                    {
                        var waterBucketType = thRainSystemService.thGravityService.GetRelatedSideWaterBucket(roofPipeCenter);
                        if (!waterBucketType.Equals(WaterBucketEnum.None))
                        {
                            if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                            {
                                rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s, DN = dn };
                                bSetWaterBucket = true;
                            }
                        }
                    }
                    if (bSetWaterBucket == false)
                    {
                        var allWaterBucketsInThisRange = thRainSystemService.thGravityService.GetRelatedGravityWaterBucket(s.Range);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            var lowerStorey = GetLowerStorey(s);
                            if (lowerStorey != null)
                            {
                                Point3d roofPipeCenterInLowerStorey = new Point3d();
                                var brst2 = thRainSystemService.GetCenterOfVerticalPipe(lowerStorey.Range, roofPipeNote, ref roofPipeCenterInLowerStorey);
                                if (brst2)
                                {
                                    var lowerBasePt = lowerStorey.Position;
                                    var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);
                                    foreach (var wbe in allWaterBucketsInThisRange)
                                    {
                                        var minPt = wbe.MinPoint;
                                        var maxPt = wbe.MaxPoint;
                                        var basePt = s.Position;
                                        var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                        var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);
                                        var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);
                                        if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                        {
                                            rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s, DN = dn };
                                            bSetWaterBucket = true;
                                            PipeRuns.Add(
                                            new ThWRainPipeRun()
                                            {
                                                Storey = s,
                                                MainRainPipe = new ThWSDPipe()
                                                {
                                                    Label = roofPipeNote,
                                                    PipeType = VerticalPipeType.RoofVerticalPipe,
                                                    DN = dn
                                                },
                                                TranslatorPipe = new ThWSDTranslatorPipe()
                                                {
                                                    DN = dn,
                                                    Label = "",
                                                    PipeType = VerticalPipeType.RoofVerticalPipe
                                                }
                                            }
                                            );
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (bSetWaterBucket == false)
                    {
                        var hasWaterBucket = thRainSystemService.HasGravityLabelConnected(s.Range, roofPipeNote);
                        if (hasWaterBucket)
                        {
                            rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = GetHigerStorey(s), DN = dn };
                            bSetWaterBucket = true;
                            PipeRuns.Add(new ThWRainPipeRun()
                            {
                                Storey = GetHigerStorey(s),
                                MainRainPipe = new ThWSDPipe()
                                {
                                    Label = roofPipeNote,
                                    PipeType = VerticalPipeType.RoofVerticalPipe,
                                    DN = dn,
                                },
                                TranslatorPipe = new ThWSDTranslatorPipe()
                                {
                                    DN = dn,
                                    Label = "",
                                    PipeType = VerticalPipeType.RoofVerticalPipe,
                                }
                            });
                            break;
                        }
                    }
                }
            }
        }
        public static double VERTICAL_STOREY_SPAN => ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.StoreySpan ?? 2000;
        public const double HORIZONTAL_STOREY_SPAN = 5500;
        private void SortRainPipeGroup<T>(List<List<T>> pipeGroups) where T : ThWRainPipeSystem
        {
            foreach (var g in pipeGroups)
            {
                g.Sort();
            }
            pipeGroups.Sort(
            delegate (List<T> x, List<T> y)
            {
                return x.First().CompareTo(y.First());
            }
            );
        }
        public void Draw(Point3d basePt)
        {
            var roofRainPipesGroup = RoofVerticalRainPipes.GroupBy(rs => rs).Select(x => x.ToList()).ToList();
            var balconyRainPipesGroup = BalconyVerticalRainPipes.GroupBy(bs => bs).Select(x => x.ToList()).ToList();
            var condenseRainPipesGroup = CondenseVerticalRainPipes.GroupBy(bs => bs).Select(x => x.ToList()).ToList();
            SortRainPipeGroup(roofRainPipesGroup);
            SortRainPipeGroup(balconyRainPipesGroup);
            SortRainPipeGroup(condenseRainPipesGroup);
            var storeyLineLength = HORIZONTAL_STOREY_SPAN * (roofRainPipesGroup.Count + balconyRainPipesGroup.Count + condenseRainPipesGroup.Count + 2);
            var sdCtxs = new List<StoreyDrawingContext>();
            for (int i = 0; i < WSDStoreys.Count; i++)
            {
                var ctx = new StoreyDrawingContext();
                ctx.StoreyLineLength = storeyLineLength;
                ctx.BasePoint = basePt.OffsetY(i * VERTICAL_STOREY_SPAN);
                ctx.Storey = WSDStoreys[i];
                ctx.StoreyDrawingContexts = sdCtxs;
                sdCtxs.Add(ctx);
            }
            for (int i = 0; i < WSDStoreys.Count; i++)
            {
                var s = WSDStoreys[i];
                s.Draw(sdCtxs[i]);
            }
            var k = 0;
            foreach (var g in roofRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
            foreach (var g in balconyRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
            foreach (var g in condenseRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
        }
        public static VerticalPipeType GetVerticalPipeType(Type type)
        {
            if (type == typeof(ThWRoofRainPipeSystem)) return VerticalPipeType.RoofVerticalPipe;
            if (type == typeof(ThWBalconyRainPipeSystem)) return VerticalPipeType.BalconyVerticalPipe;
            if (type == typeof(ThWCondensePipeSystem)) return VerticalPipeType.CondenseVerticalPipe;
            throw new NotSupportedException();
        }
        private void DrawSystem<T>(Point3d basePt, List<StoreyDrawingContext> sdCtxs, int k, List<T> g) where T : ThWRainPipeSystem
        {
            var sys = g.First();
            var ctx = new RainSystemDrawingContext();
            ctx.BasePoint = basePt.OffsetX(k * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
            ctx.StoreyDrawingContexts = sdCtxs;
            ctx.RainSystemDiagram = this;
            ctx.ThWRainPipeSystemGroup = g;
            ctx.VerticalPipeType = GetVerticalPipeType(typeof(T));
            sys.Draw(ctx);
            DrawOutputs(g, ctx);
        }
        public HashSet<ThWRainPipeSystem> ScatteredOutputs = new HashSet<ThWRainPipeSystem>();
        private void DrawOutputs<T>(List<T> g, RainSystemDrawingContext ctx) where T : ThWRainPipeSystem
        {
            var drawedLabels = new HashSet<string>();
            var outputs = g.Select(p => p.OutputType).ToList();
            var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
            var _basePt = ctx.BasePoint.ReplaceX(ctx.OutputBasePoint.X);
            var basePt = _basePt.OffsetY(-700 - 400 - 400);
            double y = basePt.Y;
            var points = GetBasePoints(basePt.OffsetXY(-800, -400), 2, outputs.Count, 800, 800).ToList();
            var k = 0;
            var count = 0;
            Point3d pt2 = default;
            for (int j = 0; j < outputs.Count; ++j)
            {
                var bsPt = points[k++];
                y = bsPt.Y;
                var output = outputs[j];
                if (output.OutputType == RainOutputTypeEnum.None)
                {
                    if (j == 0)
                    {
                        var sys = g[j];
                        if (sys.PipeRuns.Count == 0) continue;
                        var floor = (from r in sys.PipeRuns
                                     where r.Storey != null
                                     orderby ctx.RainSystemDiagram.GetStoreyIndex(r.Storey.Label)
                                     select r.Storey.Label).First();
                        if (floor == "1F")
                        {
                            Dr.DrawLabel(_basePt, "接至排水沟");
                        }
                        else
                        {
                            var pt = ctx.StoreyDrawingContexts[ctx.RainSystemDiagram.GetStoreyIndex(floor)].BasePoint.ReplaceX(_basePt.X);
                            pt = pt.OffsetXY(-1500, 150);
                            if (floor == "RF")
                            {
                                pt = pt.OffsetXY(1500, 350);
                            }
                            else if (floor == "RF+1")
                            {
                                pt = pt.OffsetY(200);
                            }
                            Dr.DrawLabel(pt, "散排至" + floor);
                            ScatteredOutputs.Add(sys);
                        }
                    }
                    continue;
                }
                if (output.HasDrivePipe)
                {
                }
                switch (output.OutputType)
                {
                    case RainOutputTypeEnum.None:
                        break;
                    case RainOutputTypeEnum.WaterWell:
                        if (drawedLabels.Contains(output.Label))
                        {
                            k--;//序号回退，防止占位
                            break;
                        }
                        Dr.DrawWaterWell(bsPt, output.Label);
                        drawedLabels.Add(output.Label);
                        count++;
                        pt2 = bsPt;
                        break;
                    case RainOutputTypeEnum.RainPort:
                        if (j == 0)
                        {
                            var pt = basePt;
                            {
                                Dr.DrawRainPort(pt.OffsetX(400));//雨水口
                                Dr.DrawRainPortLabel(pt.OffsetX(-50));//接至雨水口
                                Dr.DrawStarterPipeHeightLabel(pt.OffsetX(-50 + 1650));//起端管底标高
                            }
                        }
                        break;
                    case RainOutputTypeEnum.DrainageDitch:
                        DrawUtils.DrawTextLazy("DrainageDitch", bsPt);
                        break;
                    default:
                        break;
                }
            }
            for (int j = 0; j < pipeIds.Count(); ++j)
            {
                DrawUtils.DrawTextLazy(pipeIds[j], 300, basePt.ReplaceY(y - 2000));
                y -= 300;
            }
            if (count == 1)
            {
                var p1 = pt2.OffsetXY(400, 400);
                var p2 = p1.OffsetX(400);
                var line = DrawUtils.DrawLineLazy(p1, p2);
                line.Layer = "W-RAIN-PIPE";
                line.ColorIndex = 256;
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, 0);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static void DrawingTest()
        {
            var rd = new Random();
            var dg = new ThWRainSystemDiagram();
            for (int i = 1; i <= 31; i++)
            {
                dg.WSDStoreys.Add(new ThWSDStorey()
                {
                    Label = i + "F",
                });
            }
            foreach (var id in new string[] { "Y1L1-1", "Y1L1-1c", "Y1L1-1d", "Y1L1-2", "Y1L1-2c", "Y1L1-2d", "Y1L1-3", "Y1L1-3c", "Y1L1-3d", "Y1L1-4", "Y1L1-5", "Y1L2-1", "Y1L2-1c", "Y1L2-1d", "Y1L2-2", "Y1L2-2c", "Y1L2-2d", "Y1L2-3", "Y1L2-3c", "Y1L2-3d", "Y1L2-4", "Y1L2-5" })
            {
                var sys = new ThWRoofRainPipeSystem()
                {
                    VerticalPipeId = id,
                    OutputType = new ThWSDOutputType() { OutputType = RainOutputTypeEnum.WaterWell, Label = "666", HasDrivePipe = true },
                };
                dg.RoofVerticalRainPipes.Add(sys);
                for (int i = 0; i < dg.WSDStoreys.Count; i++)
                {
                    var run = new ThWRainPipeRun()
                    {
                        MainRainPipe = new ThWSDPipe() { Label = id, DN = "DN100" },
                        Storey = dg.WSDStoreys[i],
                    };
                    sys.PipeRuns.Add(run);
                }
            }
            foreach (var id in new string[] { "Y2L1-1", "Y2L1-2", "Y2L1-3", "Y2L1-4", "Y2L2-1", "Y2L2-2", "Y2L2-3", "Y2L2-4" })
            {
                var sys = new ThWBalconyRainPipeSystem()
                {
                    VerticalPipeId = id,
                    OutputType = new ThWSDOutputType() { OutputType = RainOutputTypeEnum.RainPort, Label = "666", HasDrivePipe = true },
                };
                dg.BalconyVerticalRainPipes.Add(sys);
                var n = rd.Next(1, dg.WSDStoreys.Count + 1);
                for (int i = 0; i < n; i++)
                {
                    var run = new ThWRainPipeRun()
                    {
                        MainRainPipe = new ThWSDPipe() { Label = id, DN = "DN100" },
                        Storey = dg.WSDStoreys[i],
                    };
                    sys.PipeRuns.Add(run);
                }
            }
            foreach (var id in new string[] { "NL1-1", "NL1-10", "NL1-2", "NL1-3", "NL1-4", "NL1-5", "NL1-6", "NL1-9", "NL2-1", "NL2-10", "NL2-2", "NL2-3", "NL2-4", "NL2-5", "NL2-6", "NL2-9" })
            {
                var sys = new ThWCondensePipeSystem()
                {
                    VerticalPipeId = id,
                    OutputType = new ThWSDOutputType() { OutputType = RainOutputTypeEnum.None, Label = "666", HasDrivePipe = true },
                };
                dg.CondenseVerticalRainPipes.Add(sys);
                for (int i = 0; i < dg.WSDStoreys.Count; i++)
                {
                    var run = new ThWRainPipeRun()
                    {
                        MainRainPipe = new ThWSDPipe() { Label = id, DN = "DN100" },
                        Storey = dg.WSDStoreys[i],
                    };
                    sys.PipeRuns.Add(run);
                }
            }
            ThMEPWSS.Common.Utils.FocusMainWindow();
            using (var lockDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var db = adb.Database;
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");
                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;
                dg.Draw(basePt);
            }
        }
    }
}