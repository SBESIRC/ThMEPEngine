using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Uitl.ExtensionsNs;


namespace ThMEPWSS.Pipe.Model
{
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
        public void InitCacheData(AcadDatabase acadDatabase, Point3dCollection range)
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

            for (int i = 0; i < WSDStoreys.Count; ++i)
            {
                if (i + 1 < WSDStoreys.Count)
                    WSDStoreys[i].HigerStorey = WSDStoreys[i + 1];

                if (i - 1 >= 0)
                    WSDStoreys[i].LowerStorey = WSDStoreys[i - 1];
            }
        }

        public void InitVerticalPipeSystems(Point3dCollection range)
        {
            //var sw = new Stopwatch();
            //sw.Start();

            //Init Roof Pipe Systems
            InitRoofPipeSystems(range);//21s
                                       //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //todo: Init Balcony Pipe Systems
            InitBalconyPipeSystems();
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //todo: Init Condense Pipe Systems
            InitCondensePipeSystems();
            //Dbg.PrintLine(sw.Elapsed.TotalSeconds.ToString());

            //sw.Stop();

        }
        void InitBalconyPipeSystems()
        {

        }
        void InitCondensePipeSystems()
        {

        }
        private void InitRoofPipeSystems(Point3dCollection selectedRange)
        {
            //1. get all notes of roof vertical pipes
            var allRoofPipeNotes = thRainSystemService.GetRoofVerticalPipeNotes(selectedRange);
            var distinctRoofPipeNotes = allRoofPipeNotes.Distinct();

            foreach (var roofPipeNote in distinctRoofPipeNotes)
            {
                var rainPipeSystem = new ThWRoofRainPipeSystem() { VerticalPipeId = roofPipeNote };
                var PipeRuns = new List<ThWRainPipeRun>();
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
                            thRainSystemService.VerticalPipeLabelToDNDict.TryGetValue(roofPipeNote, out string nd);
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
                                    Label= "",
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
                rainPipeSystem.PipeRuns = PipeRuns;
                //todo:
                //rainPipeSystem.OutputType
                this.RoofVerticalRainPipes.Add(rainPipeSystem);
            }
        }
        public const double VERTICAL_STOREY_SPAN = 2000;
        public const double HORIZONTAL_STOREY_SPAN = 5500;
        public override void Draw(Point3d basePt)
        {
            var storeyLineLength = HORIZONTAL_STOREY_SPAN * (WSDStoreys.Count + 1);
            //draw horizental storey lines
            {
                for (int i = 0; i < WSDStoreys.Count; i++)
                {
                    ThWSDStorey s = WSDStoreys[i];
                    var storeyBasePt = new Point3d(basePt.X, basePt.Y + i * VERTICAL_STOREY_SPAN, basePt.Z);
                    s.StoreyBasePoint = storeyBasePt;
                    s.LINE_LENGTH = storeyLineLength;
                    s.Draw(storeyBasePt);
                    //new Circle()
                    //{
                    //  Center = storeyBasePt.OffsetX(i * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN),
                    //  Thickness = 5,
                    //  Radius = 100,
                    //  ColorIndex = 3,
                    //}.AddToCurrentSpace();
                    //new Circle()
                    //{
                    //  Center = basePt.OffsetX(i * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN),
                    //  Thickness = 5,
                    //  Radius = 150,
                    //  ColorIndex = 4,
                    //}.AddToCurrentSpace();
                    //new Circle()
                    //{
                    //  Center = basePt,
                    //  Thickness = 5,
                    //  Radius = 200,
                    //  ColorIndex = 5,
                    //}.AddToCurrentSpace();
                    //new Circle()
                    //{
                    //  Center = storeyBasePt,
                    //  Thickness = 5,
                    //  Radius = 250,
                    //  ColorIndex = 6,
                    //}.AddToCurrentSpace();
                }
            }

            var RoofRainPipesGroup = RoofVerticalRainPipes.GroupBy(rs => rs);

            //draw pipe line systems
            {
                var i = 0;
                foreach (var g in RoofRainPipesGroup)
                {
                    var roofRainSystem = g.Key;
                    //todo 210326
                    //todo add x offset
                    var newBasePt = basePt.OffsetX(i * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN);
                    roofRainSystem.Draw(newBasePt);

                    //todo: draw outputs
                    var outputs = g.Select(p => p.OutputType);

                    //todo: draw vertical pipe id
                    var pipeIds = g.Select(p => p.VerticalPipeId).ToList();
                    for(int j = 0; j < pipeIds.Count(); ++j)
                    {
                        NoDraw.Text("ThWRoofRainPipeSystem: " + pipeIds[j], 100, newBasePt.OffsetY(-1000 * (j+1))).AddToCurrentSpace();
                    }
                    i++;
                }
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
