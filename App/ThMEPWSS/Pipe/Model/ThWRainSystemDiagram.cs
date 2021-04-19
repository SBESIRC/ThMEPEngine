﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using Newtonsoft.Json;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;

namespace ThMEPWSS.Pipe.Model
{
    public class WaterBucketDrawingContext
    {
        public Point3d BasePoint;
        public List<ThWSDStorey> WSDStoreys;
        public WaterBucketDrawingContext Clone()
        {
            return (WaterBucketDrawingContext)this.MemberwiseClone();
        }
    }
    public class RoofRainSystemDrawingContext
    {
        public Point3d BasePoint;
        public List<ThWSDStorey> WSDStoreys;
        public List<StoreyDrawingContext> StoreyDrawingContexts;
        public RoofRainSystemDrawingContext Clone()
        {
            return (RoofRainSystemDrawingContext)this.MemberwiseClone();
        }
    }
    public class StoreyDrawingContext
    {
        public ThWSDStorey WSDStorey;
        public Point3d BasePoint;
        public double StoreyLineLength;
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
        public void CollectData(AcadDatabase acadDatabase, Point3dCollection range)
        {
            thRainSystemService = new ThMEPWSS.Pipe.Service.ThRainSystemService
            {
                adb = acadDatabase,
                CurrentSelectionExtent = range

            };
            thRainSystemService.CollectData();
        }
        public void InitStoreys(List<ThIfcSpatialElement> storeys)
        {
            CollectData(storeys);
            Init();
        }

        private void CollectData(List<ThIfcSpatialElement> storeys)
        {
            // Init WSDStoreys
            using (var db = AcadDatabase.Active())
            {
                var LargeRoofVPTexts = new List<string>();
                foreach (var s in storeys)
                {
                    if (s is ThWStoreys sobj)
                    {
                        var blk = db.Element<BlockReference>(sobj.ObjectId);
                        var pts = blk.GeometricExtents.ToRectangle().Vertices();

                        switch (sobj.StoreyType)
                        {
                            case StoreyType.LargeRoof:
                                {
                                    LargeRoofVPTexts = thRainSystemService.GetVerticalPipeNotes(pts);

                                    var vps1 = new List<ThWSDPipe>();
                                    LargeRoofVPTexts.ForEach(pt =>
                                    {
                                        thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string nd);
                                        vps1.Add(new ThWSDPipe() { Label = pt, ND = nd });
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
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string nd);
                                                vps1.Add(new ThWSDPipe() { Label = pt, ND = nd });
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
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string nd);
                                                rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, ND = nd });
                                            });
                                            rf1Storey.VerticalPipes = rf1VerticalPipeObjects;

                                            var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                                            rf2VerticalPipeText.ForEach(pt =>
                                            {
                                                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(pt, out string nd);
                                                rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, ND = nd });
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

        public void Init()
        {
            SetHigherLowerStorey();
        }
        public void InitVerticalPipeSystems(Point3dCollection range)
        {
            //var sw = new Stopwatch();
            //sw.Start();

            //Init Roof Pipe Systems
            InitRoofPipeSystems(range);//21s
                                       //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //todo: Init Balcony Pipe Systems
            //InitBalconyPipeSystems(range);
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //todo: Init Condense Pipe Systems
            //InitCondensePipeSystems(range);
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //sw.Stop();

        }
        public void SetHigherLowerStorey()
        {
            for (int i = 0; i < WSDStoreys.Count; ++i)
            {
                if (i + 1 < WSDStoreys.Count)
                    WSDStoreys[i].HigerStorey = WSDStoreys[i + 1];

                if (i - 1 >= 0)
                    WSDStoreys[i].LowerStorey = WSDStoreys[i - 1];
            }
        }



        private void InitBalconyPipeSystems(Point3dCollection selectedRange)
        {
            //1. get all notes of roof vertical pipes
            var allBalconyPipeNotes = thRainSystemService.GetBalconyVerticalPipeNotes(selectedRange);
            var distinctBalconyPipeNotes = allBalconyPipeNotes.Distinct();

            foreach (var balconyPipeNote in distinctBalconyPipeNotes)
            {
                var rainPipeSystem = new ThWBalconyRainPipeSystem() { VerticalPipeId = balconyPipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                AddPipeRunsForBalcony(balconyPipeNote, rainPipeSystem, PipeRuns);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;

                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, balconyPipeNote);

                this.BalconyVerticalRainPipes.Add(rainPipeSystem);
            }

        }

        private bool AddPipeRunsForBalcony(string balconyPipeNote, ThWBalconyRainPipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns)
        {
            bool bSetWaterBucket = false;
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
                    var translatorType = thRainSystemService.GetTranslatorType(s.Range, balconyPipeNote);
                    if (pipes.Contains(balconyPipeNote))
                    {
                        thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(balconyPipeNote, out string nd);
                        PipeRuns.Add(new ThWRainPipeRun()
                        {
                            Storey = s,
                            MainRainPipe = new ThWSDPipe()
                            {
                                Label = balconyPipeNote,
                                PipeType = VerticalPipeType.BalconyVerticalPipe,
                                ND = nd,
                            },
                            TranslatorPipe = new ThWSDTranslatorPipe()
                            {
                                ND = nd,
                                Label = "",
                                PipeType = VerticalPipeType.BalconyVerticalPipe,
                                TranslatorType = translatorType
                            }
                            //todo: set check point
                        });
                    }
                }
                if (bSetWaterBucket == false)
                {
                    //water bucket, too slow
                    Point3d pipeCenter = new Point3d();
                    var brst = thRainSystemService.GetCenterOfVerticalPipe(s.Range, balconyPipeNote, ref pipeCenter);
                    if (brst)
                    {
                        var waterBucketType = thRainSystemService.GetRelatedSideWaterBucket(pipeCenter);

                        //side
                        if (!waterBucketType.Equals(WaterBucketEnum.None))
                        {
                            if (s.VerticalPipes.Select(p => p.Label).Contains(balconyPipeNote))
                            {
                                rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s };
                                bSetWaterBucket = true;
                            }
                        }
                    }

                    //gravity
                    if (bSetWaterBucket == false)
                    {
                        var allWaterBucketsInThisRange = thRainSystemService.GetRelatedGravityWaterBucket(s.Range);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (s.LowerStorey != null)
                            {
                                Point3d PipeCenterInLowerStorey = new Point3d();

                                var brst2 = thRainSystemService.GetCenterOfVerticalPipe(s.LowerStorey.Range, balconyPipeNote, ref PipeCenterInLowerStorey);
                                if (brst2)
                                {
                                    var lowerBasePt = s.LowerStorey.Position;
                                    var pipeCenterInLowerUcs = new Point3d(PipeCenterInLowerStorey.X - lowerBasePt.X, PipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

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
                                            rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s };
                                            bSetWaterBucket = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return bSetWaterBucket;
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

        private void InitCondensePipeSystems(Point3dCollection selectedRange)
        {
            //1. get all notes of roof vertical pipes
            var allCondensePipeNotes = thRainSystemService.GetCondenseVerticalPipeNotes(selectedRange);
            var distinctCondensePipeNotes = allCondensePipeNotes.Distinct();

            foreach (var condensePipeNote in distinctCondensePipeNotes)
            {
                var rainPipeSystem = new ThWCondensePipeSystem() { VerticalPipeId = condensePipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
                bool bSetWaterBucket = false;
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
                        var translatorType = thRainSystemService.GetTranslatorType(s.Range, condensePipeNote);
                        if (pipes.Contains(condensePipeNote))
                        {
                            thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(condensePipeNote, out string nd);
                            PipeRuns.Add(new ThWRainPipeRun()
                            {
                                Storey = s,
                                MainRainPipe = new ThWSDPipe()
                                {
                                    Label = condensePipeNote,
                                    PipeType = VerticalPipeType.CondenseVerticalPipe,
                                    ND = nd,
                                },
                                TranslatorPipe = new ThWSDTranslatorPipe()
                                {
                                    ND = nd,
                                    Label = "",
                                    PipeType = VerticalPipeType.CondenseVerticalPipe,
                                    TranslatorType = translatorType
                                }
                                //todo: set check point
                            });
                        }
                    }
                    if (bSetWaterBucket == false)
                    {
                        //water bucket, too slow
                        Point3d pipeCenter = new Point3d();
                        var brst = thRainSystemService.GetCenterOfVerticalPipe(s.Range, condensePipeNote, ref pipeCenter);

                        if (brst)
                        {
                            var waterBucketType = thRainSystemService.GetRelatedSideWaterBucket(pipeCenter);

                            //side
                            if (!waterBucketType.Equals(WaterBucketEnum.None))
                            {
                                if (s.VerticalPipes.Select(p => p.Label).Contains(condensePipeNote))
                                {
                                    rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s };
                                    bSetWaterBucket = true;
                                }
                            }
                        }

                        //gravity
                        if (bSetWaterBucket == false)
                        {
                            var allWaterBucketsInThisRange = thRainSystemService.GetRelatedGravityWaterBucket(s.Range);
                            if (allWaterBucketsInThisRange.Count > 0)
                            {
                                if (s.LowerStorey != null)
                                {
                                    Point3d PipeCenterInLowerStorey = new Point3d();

                                    var brst2 = thRainSystemService.GetCenterOfVerticalPipe(s.LowerStorey.Range, condensePipeNote, ref PipeCenterInLowerStorey);
                                    if (brst2)
                                    {
                                        var lowerBasePt = s.LowerStorey.Position;
                                        var pipeCenterInLowerUcs = new Point3d(PipeCenterInLowerStorey.X - lowerBasePt.X, PipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

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
                                                rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s };
                                                bSetWaterBucket = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                //set check points for every pipe
                for (int i = 1; i < PipeRuns.Count; i++)
                {
                    if (PipeRuns[i - 1].TranslatorPipe.TranslatorType != TranslatorTypeEnum.None)
                    {
                        ThWSDCheckPoint checkPoint = new ThWSDCheckPoint() { HasCheckPoint = true };
                        PipeRuns[i].CheckPoint = checkPoint;
                    }
                }

                rainPipeSystem.PipeRuns = PipeRuns;

                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, condensePipeNote);

                this.CondenseVerticalRainPipes.Add(rainPipeSystem);
            }

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
                thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(roofPipeNote, out string nd);
                AddPipeRunsForRoof(roofPipeNote, rainPipeSystem, PipeRuns, nd);
                SetCheckPoints(PipeRuns);
                rainPipeSystem.PipeRuns = PipeRuns;
                //set output type of pipe system
                rainPipeSystem.OutputType = thRainSystemService.GetPipeOutputType(WSDStoreys.First().Range, roofPipeNote);
                SetCheckPoints(rainPipeSystem);
                this.RoofVerticalRainPipes.Add(rainPipeSystem);
            }
        }

        private static void SetCheckPoints(ThWRoofRainPipeSystem rainPipeSystem)
        {
            if (rainPipeSystem.OutputType.OutputType.Equals(RainOutputTypeEnum.RainPort) || rainPipeSystem.OutputType.OutputType.Equals(RainOutputTypeEnum.WaterWell))
            {
                if (rainPipeSystem.PipeRuns.Count >= 2)
                    rainPipeSystem.PipeRuns[0].CheckPoint = new ThWSDCheckPoint { HasCheckPoint = true };
            }
        }

        private void AddPipeRunsForRoof(string roofPipeNote, ThWRoofRainPipeSystem rainPipeSystem, List<ThWRainPipeRun> PipeRuns, string nd)
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
                    var translatorType = thRainSystemService.GetTranslatorType(s.Range, roofPipeNote);
                    if (pipes.Contains(roofPipeNote))
                    {
                        PipeRuns.Add(new ThWRainPipeRun()
                        {
                            Storey = s,
                            MainRainPipe = new ThWSDPipe()
                            {
                                Label = roofPipeNote,
                                PipeType = VerticalPipeType.RoofVerticalPipe,
                                ND = nd,
                            },
                            TranslatorPipe = new ThWSDTranslatorPipe()
                            {
                                ND = nd,
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
                        var waterBucketType = thRainSystemService.GetRelatedSideWaterBucket(roofPipeCenter);

                        //side
                        if (!waterBucketType.Equals(WaterBucketEnum.None))
                        {
                            if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                            {
                                //Dbg.ShowWhere(roofPipeCenter);
                                rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s };
                                bSetWaterBucket = true;
                            }
                        }
                    }

                    //gravity
                    if (bSetWaterBucket == false)
                    {
                        var allWaterBucketsInThisRange = thRainSystemService.GetRelatedGravityWaterBucket(s.Range);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (s.LowerStorey != null)
                            {
                                Point3d roofPipeCenterInLowerStorey = new Point3d();

                                var brst2 = thRainSystemService.GetCenterOfVerticalPipe(s.LowerStorey.Range, roofPipeNote, ref roofPipeCenterInLowerStorey);
                                if (brst2)
                                {
                                    var lowerBasePt = s.LowerStorey.Position;
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
                                            rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s };
                                            bSetWaterBucket = true;
                                            PipeRuns.Add(
                                                new ThWRainPipeRun()
                                                {
                                                    Storey = s,
                                                    MainRainPipe = new ThWSDPipe()
                                                    {
                                                        Label = roofPipeNote,
                                                        PipeType = VerticalPipeType.RoofVerticalPipe,
                                                        ND = nd
                                                    },
                                                }
                                                );
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        public const double VERTICAL_STOREY_SPAN = 2000;
        public const double HORIZONTAL_STOREY_SPAN = 5500;



        public override void Draw(Point3d basePt)
        {
            var RoofRainPipesGroup = RoofVerticalRainPipes.GroupBy(rs => rs).ToList();
            var balconyRainPipesGroup = BalconyVerticalRainPipes.GroupBy(bs => bs).ToList();
            var condenseRainPipesGroup = CondenseVerticalRainPipes.GroupBy(bs => bs).ToList();
            var storeyLineLength = HORIZONTAL_STOREY_SPAN * (RoofRainPipesGroup.Count + balconyRainPipesGroup.Count + condenseRainPipesGroup.Count + 2);

            //draw horizental storey lines
            var sdCtxs = new List<StoreyDrawingContext>();

            for (int i = 0; i < WSDStoreys.Count; i++)
            {
                var s = WSDStoreys[i];
                var ctx = new StoreyDrawingContext();
                ctx.StoreyLineLength = storeyLineLength;
                ctx.BasePoint = basePt.OffsetY(i * VERTICAL_STOREY_SPAN);
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
                var roofRainSystem = g.Key;

                var ctx = new RoofRainSystemDrawingContext();
                ctx.BasePoint = basePt.OffsetX(k * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
                ctx.WSDStoreys = WSDStoreys;
                ctx.StoreyDrawingContexts = sdCtxs;
                roofRainSystem.Draw(ctx);


                Point3d pt = ctx.BasePoint;
                //pt = DrLazy.Default.BasePoint.ReplaceY(pt.Y);
                //pt = DrLazy.Default.BasePoint;

                //Dbg.ShowWhere(newBasePt);
                //DrawUtils.DrawTextLazy("qrjrq5 roofRainSystem", 100, newBasePt);

                //todo: draw outputs
                //todo: draw vertical pipe id
                var outputs = g.Select(p => p.OutputType).ToList();
                var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
                var y = -400 - 2000;

                for (int j = 0; j < outputs.Count(); ++j)
                {
                    var output = outputs[j];
                    if (output.OutputType == RainOutputTypeEnum.None) continue;
                    //DrawUtils.DrawTextLazy(outputs[j].OutputType.ToString(), 100, newBasePt.OffsetY(y));
                    switch (output.OutputType)
                    {
                        case RainOutputTypeEnum.None:
                            break;
                        case RainOutputTypeEnum.WaterWell:
                            Dr.DrawWaterWell(pt.OffsetY(y));
                            break;
                        case RainOutputTypeEnum.RainPort:
                            Dr.DrawWaterWell(pt.OffsetY(y));
                            break;
                        case RainOutputTypeEnum.DrainageDitch:
                            Dr.DrawWaterWell(pt.OffsetY(y));
                            break;
                        default:
                            break;
                    }
                    y -= 400;
                    //NoDraw.Text(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1))).AddToCurrentSpace();
                }
                for (int j = 0; j < pipeIds.Count(); ++j)
                {
                    DrawUtils.DrawTextLazy(pipeIds[j], 300, pt.OffsetY(y));
                    y -= 300;
                    //NoDraw.Text(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1))).AddToCurrentSpace();
                }

                k++;
            }


            //draw balcony pipe line systems



            foreach (var g in balconyRainPipesGroup)
            {
                var rainSystem = g.Key;
                //todo 210326
                //todo add x offset
                var newBasePt = basePt.OffsetX(k * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
                rainSystem.Draw(newBasePt);

                //todo: draw outputs
                var outputs = g.Select(p => p.OutputType);

                //todo: draw vertical pipe id
                var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
                for (int j = 0; j < pipeIds.Count(); ++j)
                {
                    DrawUtils.DrawTextLazy(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1)));
                    //NoDraw.Text(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1))).AddToCurrentSpace();
                }
                k++;
            }


            //draw condense pipe line systems



            foreach (var g in condenseRainPipesGroup)
            {
                var rainSystem = g.Key;
                //todo 210326
                //todo add x offset
                var newBasePt = basePt.OffsetX(k * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
                rainSystem.Draw(newBasePt);

                //todo: draw outputs
                var outputs = g.Select(p => p.OutputType);

                //todo: draw vertical pipe id
                var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
                for (int j = 0; j < pipeIds.Count(); ++j)
                {
                    DrawUtils.DrawTextLazy(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1)));
                    //NoDraw.Text(pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j + 1))).AddToCurrentSpace();
                }
                k++;
            }

            //todo:
            //foreach (var ls in BalconyVerticalRainPipes)
            //{
            //    ls.Draw(basePt);
            //}

            //foreach (var ls in CondenseVerticalRainPipes)
            //{
            //    ls.Draw(basePt);
            //}
        }

    }
}
