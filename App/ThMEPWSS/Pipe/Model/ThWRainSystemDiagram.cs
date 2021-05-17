﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
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
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;

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
        static readonly Regex re = new Regex(@"^(Y1L|Y2L|NL)(\w+)\-(\w+)([a-zA-Z]*)$");
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

        //sorted from base to top
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
            // Init WSDStoreys
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
                                            //just has rf + 1, do nothing
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
                                            //has rf + 1, rf + 2
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
            //var sw = new Stopwatch();
            //sw.Start();

            //Init Roof Pipe Systems
            InitRoofPipeSystems(range);
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //Init Balcony Pipe Systems
            InitBalconyPipeSystems(range);
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //Init Condense Pipe Systems
            InitCondensePipeSystems(range);
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());
            var sv = thRainSystemService;
            var condensePipesCache = new Cache<Point3dCollection, List<Entity>>(rg =>
            {
                return sv.FiltByRect(rg, sv.CondensePipes).ToList();
                //return sv.GetCondensePipes(rg);
            });
            var floorDrainsCache = new Cache<Point3dCollection, List<Entity>>(rg =>
            {
                return sv.FiltByRect(rg, sv.FloorDrains).ToList();
                //return sv.GetFloorDrains(rg);
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
            //sw.Stop();

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
            //1. get all notes of roof vertical pipes
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
                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, roofPipeNote);
                SetCheckPoints(rainPipeSystem);
                this.RoofVerticalRainPipes.Add(rainPipeSystem);
            }
        }
        private void InitCondensePipeSystems(Point3dCollection selectedRange)
        {
            //1. get all notes of roof vertical pipes
            var allCondensePipeNotes = thRainSystemService.GetCondenseVerticalPipeNotes(selectedRange);
            var distinctCondensePipeNotes = allCondensePipeNotes.Distinct().ToList();
            foreach (var condensePipeNote in distinctCondensePipeNotes)
            {
                var rainPipeSystem = new ThWCondensePipeSystem() { VerticalPipeId = condensePipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                AddPipeRunsForCondensePipe(condensePipeNote, rainPipeSystem, PipeRuns);
                //set check points for every pipe
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, condensePipeNote);
                SetCheckPoints(rainPipeSystem);
                this.CondenseVerticalRainPipes.Add(rainPipeSystem);
            }
        }

        private void AddPipeRunsForCondensePipe(string condensePipeNote, ThWCondensePipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns)
        {
            foreach (var s in WSDStoreys)
            {
                //var pipeCenter = new Point3d();
                //var brst0 = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote, ref pipeCenter);

                //if (!brst0) continue;

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
                            //todo: set check point
                        });
                    }
                }
            }
        }

        private void InitBalconyPipeSystems(Point3dCollection selectedRange)
        {
            //1. get all notes of roof vertical pipes
            var allBalconyPipeNotes = thRainSystemService.GetBalconyVerticalPipeNotes(selectedRange);
            var distinctBalconyPipeNotes = allBalconyPipeNotes.Distinct().ToList();
            foreach (var balconyPipeNote in distinctBalconyPipeNotes)
            {
                var rainPipeSystem = new ThWBalconyRainPipeSystem() { VerticalPipeId = balconyPipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                AddPipeRunsForBalcony(balconyPipeNote, rainPipeSystem, PipeRuns);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, balconyPipeNote);
                SetCheckPoints(rainPipeSystem);
                this.BalconyVerticalRainPipes.Add(rainPipeSystem);
            }
        }

        private void AddPipeRunsForBalcony(string balconyPipeNote, ThWBalconyRainPipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns)
        {
            foreach (var s in WSDStoreys)
            {
                //var pipeCenter = new Point3d();
                //var brst0 = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote, ref pipeCenter);

                //if (!brst0) continue;
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
                            //todo: set check point
                        });
                    }
                }
            }
        }

        private void SetCheckPoints(List<ThWRainPipeRun> PipeRuns)
        {
            //set check points for every pipe
            for (int i = 1; i < PipeRuns.Count; i++)
            {
                if (PipeRuns[i - 1].TranslatorPipe.TranslatorType == TranslatorTypeEnum.Long)
                {
                    ThWSDCheckPoint checkPoint = new ThWSDCheckPoint() { HasCheckPoint = true };
                    PipeRuns[i].CheckPoint = checkPoint;
                }
            }
        }
        private static void SetCheckPoints(ThWRainPipeSystem rainPipeSystem)
        {
            if (rainPipeSystem.OutputType.OutputType.Equals(RainOutputTypeEnum.RainPort) || rainPipeSystem.OutputType.OutputType.Equals(RainOutputTypeEnum.WaterWell))
            {
                if (rainPipeSystem.PipeRuns.Count >= 2)
                    rainPipeSystem.PipeRuns[0].CheckPoint = new ThWSDCheckPoint { HasCheckPoint = true };
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
                //var pipeCenter = new Point3d();
                //var brst0 = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote, ref pipeCenter);

                //if (!brst0) continue;

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
                    if (translatorType.Equals(TranslatorTypeEnum.Gravity))
                    {
                        //重力雨水斗转管
                        rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s, DN = dn };
                        bSetWaterBucket = true;
                    }

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
                            //todo: set check point
                        });
                    }
                }
                if (bSetWaterBucket == false)
                {
                    //water bucket, too slow
                    Point3d roofPipeCenter = new Point3d();
                    var brst = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote, ref roofPipeCenter);

                    if (brst)
                    {
                        var waterBucketType = thRainSystemService.thGravityService.GetRelatedSideWaterBucket(roofPipeCenter);

                        //side
                        if (!waterBucketType.Equals(WaterBucketEnum.None))
                        {
                            if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                            {
                                //Dbg.ShowWhere(roofPipeCenter);
                                rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s, DN = dn };
                                bSetWaterBucket = true;
                            }
                        }
                    }

                    //gravity
                    if (bSetWaterBucket == false)
                    {
                        //尝试通过对位得到重力雨水斗
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

                                    //compute ucs
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
                                            //Dbg.ShowWhere(new ThWGRect(minPt, maxPt));
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

                    //for gravity bucket, still need to check label
                    if (bSetWaterBucket == false)
                    {
                        //尝试通过 label 得到重力雨水斗
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
                                //todo: set check point
                            });

                            break;
                        }
                    }
                }
            }
        }
        public const double VERTICAL_STOREY_SPAN = 2000;
        public const double HORIZONTAL_STOREY_SPAN = 5500;


        public List<List<ThWRoofRainPipeSystem>> RoofRainPipesGroup;
        public List<List<ThWBalconyRainPipeSystem>> BalconyRainPipesGroup;
        public List<List<ThWCondensePipeSystem>> CondenseRainPipesGroup;

        private void GroupRainPipes()
        {
            if (RoofRainPipesGroup == null)
            {
                RoofRainPipesGroup = RoofVerticalRainPipes.GroupBy(rs => rs).Select(x => x.ToList()).ToList();
                BalconyRainPipesGroup = BalconyVerticalRainPipes.GroupBy(bs => bs).Select(x => x.ToList()).ToList();
                CondenseRainPipesGroup = CondenseVerticalRainPipes.GroupBy(bs => bs).Select(x => x.ToList()).ToList();
            }
        }

        private void SortRainPipeGroups()
        {
            SortRainPipeGroup<ThWRoofRainPipeSystem>(RoofRainPipesGroup);
            SortRainPipeGroup<ThWBalconyRainPipeSystem>(BalconyRainPipesGroup);
            SortRainPipeGroup<ThWCondensePipeSystem>(CondenseRainPipesGroup);
        }

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

        public override void Draw(Point3d basePt)
        {
            GroupRainPipes();
            SortRainPipeGroups();
            draw(basePt);
        }

        private void draw(Point3d basePt)
        {
            var storeyLineLength = HORIZONTAL_STOREY_SPAN * (RoofRainPipesGroup.Count + BalconyRainPipesGroup.Count + CondenseRainPipesGroup.Count + 2);

            //draw horizental storey lines
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
            //draw roof pipe line systems
            var k = 0;
            foreach (var g in RoofRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
            //draw balcony pipe line systems
            foreach (var g in BalconyRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
            //draw condense pipe line systems
            foreach (var g in CondenseRainPipesGroup)
            {
                DrawSystem(basePt, sdCtxs, k, g);
                k++;
            }
        }
        private void DrawSystem<T>(Point3d basePt, List<StoreyDrawingContext> sdCtxs, int k, List<T> g) where T : ThWRainPipeSystem
        {
            var sys = g.First();
            var ctx = new RainSystemDrawingContext();
            ctx.BasePoint = basePt.OffsetX(k * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
            ctx.StoreyDrawingContexts = sdCtxs;
            ctx.RainSystemDiagram = this;
            ctx.ThWRainPipeSystemGroup = g;
            sys.Draw(ctx);
            DrawUtils.DrawTextLazy(sys.OutputType.OutputType.ToString(), 200, ctx.BasePoint.ReplaceX(ctx.OutputBasePoint.X));
            DrawOutputs(g, ctx);
        }
        public HashSet<ThWRainPipeSystem> ScatteredOutputs = new HashSet<ThWRainPipeSystem>();
        private void DrawOutputs<T>(List<T> g, RainSystemDrawingContext ctx) where T : ThWRainPipeSystem
        {
            var outputs = g.Select(p => p.OutputType).ToList();
            var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
            var _basePt = ctx.BasePoint.ReplaceX(ctx.OutputBasePoint.X);
            var basePt = _basePt.OffsetY(-700);
            double y = basePt.Y;
            var em = GetBasePoints(basePt, 2, outputs.Count, 400, 400).GetEnumerator();
            for (int j = 0; j < outputs.Count; ++j)
            {
                em.MoveNext();
                var bsPt = em.Current;
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
                            //Dbg.ShowWhere(pt);
                            pt = pt.OffsetXY(-1500,150);
                            Dr.DrawLabel(pt, "散排至" + floor);
                            ScatteredOutputs.Add(sys);
                        }
                    }
                    continue;
                }
                if (output.HasDrivePipe)
                {
                    //DrawUtils.DrawTextLazy("HasDrivePipe", basePt.OffsetX(900));
                    Dr.DrawWrappingPipe(basePt.OffsetX(1400));
                    //Dbg.ShowWhere(basePt);
                }
                switch (output.OutputType)
                {
                    case RainOutputTypeEnum.None:

                        break;
                    case RainOutputTypeEnum.WaterWell:
                        //Dr.DrawWaterWell(pt.OffsetY(y));
                        //Dr.DrawWaterWell(bsPt, $"{j}");
                        Dr.DrawWaterWell(bsPt, output.Label);
                        break;
                    case RainOutputTypeEnum.RainPort:
                        //Dr.DrawRainPort(pt.OffsetY(y));
                        //Dr.DrawRainPort(bsPt);
                        if (j == 0)
                        {
                            Dr.DrawRainPort(basePt.OffsetX(400));
                            Dr.DrawRainPortLabel(basePt.OffsetX(-50));
                            Dr.DrawStarterPipeHeightLabel(basePt.OffsetX(-50 + 1650));
                        }
                        break;
                    case RainOutputTypeEnum.DrainageDitch:
                        //DrawUtils.DrawTextLazy("DrainageDitch", pt.OffsetY(y));
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
            //DrawUtils.DrawTextLazy(xx, 300, basePt.ReplaceY(y - 2000));
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
    }
}
