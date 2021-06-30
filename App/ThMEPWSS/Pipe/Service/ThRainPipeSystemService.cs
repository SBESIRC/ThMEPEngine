namespace ThMEPWSS.DebugNs
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using Autodesk.AutoCAD.DatabaseServices;
    using Dreambuild.AutoCAD;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using NetTopologySuite.Geometries;
    public class RainSystemService
    {
        public RainSystemCadData CadDataMain;
        public List<RainSystemCadData> CadDatas;
        public List<ThStoreysData> Storeys;
        public RainSystemGeoData GeoData;
        public ThWRainSystemDiagram RainSystemDiagram;
        public List<RainSystemDrawingData> DrawingDatas;

        public void BuildRainSystemDiagram<T>(string label, T sys, VerticalPipeType sysType) where T : ThWRainPipeSystem
        {
            for (int i = 0; i < RainSystemDiagram.WSDStoreys.Count; i++)
            {
                var run = new ThWRainPipeRun()
                {
                    MainRainPipe = new ThWSDPipe()
                    {
                        Label = label,
                        DN = "DN100",
                    },
                    Storey = RainSystemDiagram.WSDStoreys[i],
                    TranslatorPipe = new ThWSDTranslatorPipe(),
                };
                var bd = run.Storey.Boundary;
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var storeyI = Storeys.IndexOf(storey);
                    List<string> labels = sysType switch
                    {
                        VerticalPipeType.RoofVerticalPipe => DrawingDatas[storeyI].RoofLabels,
                        VerticalPipeType.BalconyVerticalPipe => DrawingDatas[storeyI].BalconyLabels,
                        VerticalPipeType.CondenseVerticalPipe => DrawingDatas[storeyI].CondenseLabels,
                        _ => throw new NotSupportedException(),
                    };
                    AddPipeRuns(label, sys, run, storeyI, labels);
                }
            }
        }
        private void AddPipeRuns<T>(string label, T sys, ThWRainPipeRun run, int storeyI, List<string> labels) where T : ThWRainPipeSystem
        {
            var drData = DrawingDatas[storeyI];
            if (labels.Contains(label))
            {
                if (drData.ShortTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Short;
                }
                else if (drData.LongTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Long;
                }
                foreach (var kv in drData.PipeLabelToWaterWellLabels)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.Label = kv.Value;
                        break;
                    }
                }
                foreach (var kv in drData.OutputTypes)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.OutputType = kv.Value;
                        if (kv.Value == RainOutputTypeEnum.WaterWell)
                        {
                            foreach (var _kv in drData.WaterWellWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        else if (kv.Value == RainOutputTypeEnum.RainPort)
                        {
                            foreach (var _kv in drData.RainPortWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                {
                    foreach (var kv in drData.CondensePipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value.Key; i++)
                            {
                                var cp = new ThWSDCondensePipe();
                                run.CondensePipes.Add(cp);
                                run.HasBrokenCondensePipe = kv.Value.Value;
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrains)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                run.FloorDrains.Add(new ThWSDFloorDrain());
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrainsWrappingPipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                if (i < run.FloorDrains.Count)
                                {
                                    run.FloorDrains[i].HasDrivePipe = true;
                                }
                            }
                        }
                    }
                }
                sys.PipeRuns.Add(run);
                AddPipeRunsForRF(label, sys);
                sys.PipeRuns = sys.PipeRuns.OrderBy(run => RainSystemDiagram.WSDStoreys.IndexOf(run.Storey)).ToList();
                ThWRainSystemDiagram.SetCheckPoints(sys);
                ThWRainSystemDiagram.SetCheckPoints(sys.PipeRuns);
            }
#pragma warning disable
        }
       
        public static void SortStoreys(List<ThWSDStorey> wsdStoreys)
        {
            static int getScore(string label)
            {
                if (label == null) return 0;
                switch (label)
                {
                    case "RF": return ushort.MaxValue;
                    case "RF+1": return ushort.MaxValue + 1;
                    case "RF+2": return ushort.MaxValue + 2;
                    default:
                        {
                            int.TryParse(label.Replace("F", ""), out int ret);
                            return ret;
                        }
                }
            }
            wsdStoreys.Sort((x, y) => getScore(x.Label) - getScore(y.Label));
        }
        public void CreateRainSystemDiagram()
        {
            var wsdStoreys = new List<ThWSDStorey>();
            CollectStoreys(wsdStoreys);
            SortStoreys(wsdStoreys);
            var dg = new ThWRainSystemDiagram();
            this.RainSystemDiagram = dg;
            dg.WSDStoreys.AddRange(wsdStoreys);

            foreach (var label in DrawingDatas.SelectMany(drData => drData.RoofLabels).Distinct())
            {
                var sys = new ThWRoofRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.RoofVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.RoofVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.BalconyLabels).Distinct())
            {
                var sys = new ThWBalconyRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.BalconyVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.BalconyVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.CondenseLabels).Distinct())
            {
                var sys = new ThWCondensePipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.CondenseVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.CondenseVerticalPipe);
            }
            fixDiagramData(RainSystemDiagram);
            FixWaterBucketDN();
        }

        static void fixDiagramData(ThWRainSystemDiagram dg)
        {
            //根据实际业务修正

            fixOutput(dg.RoofVerticalRainPipes);
            fixOutput(dg.BalconyVerticalRainPipes);
            fixOutput(dg.CondenseVerticalRainPipes);
        }

        private static void fixOutput<T>(IList<T> systems) where T : ThWRainPipeSystem
        {
            foreach (var sys in systems)
            {
                //没有1楼的一律散排
                var r = sys.PipeRuns.FirstOrDefault(r => r.Storey?.Label == "1F");
                if (r == null)
                {
                    sys.OutputType.OutputType = RainOutputTypeEnum.None;
                }
            }
        }

        public void CollectStoreys(List<ThWSDStorey> wsdStoreys)
        {
            if (false)
            {
                var lst = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.StandardStorey || s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey)
              .SelectMany(s => s.Storeys).ToList();
                var min = lst.Min();
                var max = lst.Max();
            }
            List<string> GetVerticalPipeNotes(ThStoreysData storey)
            {
                var storeyI = Storeys.IndexOf(storey);
                if (storeyI < 0) return new List<string>();
                return DrawingDatas[storeyI].GetAllLabels();
            }
            {
                var largeRoofVPTexts = new List<string>();
                foreach (var storey in Storeys)
                {
                    var bd = storey.Boundary;
                    switch (storey.StoreyType)
                    {
                        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                            {
                                largeRoofVPTexts = GetVerticalPipeNotes(storey);

                                var vps1 = new List<ThWSDPipe>();
                                largeRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF", Boundary = bd, VerticalPipes = vps1 });
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Storeys.ForEach(i => wsdStoreys.Add(new ThWSDStorey() { Label = $"{i}F", Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == 1)
                    {
                        var storey = storeys[0];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        var rf1Storey = new ThWSDStorey() { Label = $"RF+1", Boundary = bd };
                        wsdStoreys.Add(rf1Storey);

                        if (largeRoofVPTexts.Count > 0)
                        {
                            var rf2VerticalPipeText = smallRoofVPTexts.Except(largeRoofVPTexts);

                            if (rf2VerticalPipeText.Count() == 0)
                            {
                                //just has rf + 1, do nothing
                                var vps1 = new List<ThWSDPipe>();
                                smallRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
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
                                    rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });
                                rf1Storey.VerticalPipes = rf1VerticalPipeObjects;

                                var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                                rf2VerticalPipeText.ForEach(pt =>
                                {
                                    rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd, VerticalPipes = rf2VerticalPipeObjects });
                            }
                        }
                    }
                    else if (storeys.Count == 2)
                    {
                        var s1 = storeys[0];
                        var s2 = storeys[1];
                        var bd1 = s1.Boundary;
                        var bd2 = s2.Boundary;
                        SwapBy2DSpace(ref s1, ref s2, bd1, bd2);
                        var vpts1 = GetVerticalPipeNotes(s1);
                        var vpts2 = GetVerticalPipeNotes(s2);
                        var vps1 = vpts1.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        var vps2 = vpts2.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+1", Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
        }
        static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        static void SwapBy2DSpace<T>(ref T v1, ref T v2, GRect bd1, GRect bd2)
        {
            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
            if (deltaY > bd1.Height)
            {
                if (bd2.MaxY > bd1.MaxY)
                {
                    Swap(ref v1, ref v2);
                }
            }
            else if (deltaX > bd1.Width)
            {
                if (bd2.MinX < bd1.MinX)
                {
                    Swap(ref v1, ref v2);
                }
            }

        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == 0) return false;
            }
            return true;
        }
        public static List<Geometry> ToList(params List<Geometry>[] plss)
        {
            return plss.SelectMany(pls => pls).ToList();
        }
        public static bool IsWantedText(string text)
        {
            return ThRainSystemService.IsWantedLabelText(text) || ThRainSystemService.HasGravityLabelConnected(text)
                || text.StartsWith("YL") || IsWaterPortLabel(text) || IsGravityWaterBucketDNText(text);
        }

        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }

        static readonly Regex re = new Regex(@"^重力型雨水斗(DN\d+)$");
        public static bool IsWaterPortLabel(string text)
        {
            return text.Contains("接至") && text.Contains("雨水口");
        }
        public void CreateDrawingDatas()
        {
            var cadDataMain = CadDataMain;
            var geoData = GeoData;
            var cadDatas = CadDatas;
            var storeys = Storeys;

            var drawingDatas = new List<RainSystemDrawingData>();
            this.DrawingDatas = drawingDatas;

            var sb = new StringBuilder(8192);
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainSystemDrawingData();
                drawingDatas.Add(drData);
                {
                    var s = storeys[storeyI];
                    sb.AppendLine("楼层");
                    sb.AppendLine(s.Storeys.ToJson());
                    sb.AppendLine(s.StoreyType.ToString());
                }
                {
                    var s = geoData.Storeys[storeyI];
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                var item = cadDatas[storeyI];
                {
                    var wantedLabels = new List<string>();
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        if (IsWantedText(m.Text))
                        {
                            wantedLabels.Add(m.Text);
                        }
                    }
                    sb.AppendLine("立管");
                    sb.AppendLine(ThRainSystemService.GetRoofLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetBalconyLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetCondenseLabels(wantedLabels).ToJson());
                    drData.RoofLabels.AddRange(ThRainSystemService.GetRoofLabels(wantedLabels));
                    drData.BalconyLabels.AddRange(ThRainSystemService.GetBalconyLabels(wantedLabels));
                    drData.CondenseLabels.AddRange(ThRainSystemService.GetCondenseLabels(wantedLabels));
                    drData.CommentLabels.AddRange(wantedLabels.Where(x => ThRainSystemService.HasGravityLabelConnected(x)));
                }
                var waterBucketFixingList = new List<KeyValuePair<string, Geometry>>();
                var lbDict = new Dictionary<Geometry, string>();
                {
                    //凭空生成一个雨水口
                    if (false)
                    {
                        var f = GeoFac.CreateIntersectsSelector(item.LabelLines);
                        foreach (var lb in item.Labels)
                        {
                            var j = cadDataMain.Labels.IndexOf(lb);
                            var m = geoData.Labels[j];
                            var label = m.Text;
                            if (IsWaterPortLabel(label))
                            {
                                var ok_ents = new HashSet<Geometry>();
                                var lines = f(lb);
                                if (lines.Count == 1)
                                {
                                    var line1 = lines[0];
                                    ok_ents.Add(line1);
                                    lines = f(line1).Except(ok_ents).ToList();
                                    if (lines.Count == 1)
                                    {
                                        var line2 = lines[0];
                                        ok_ents.Add(line2);
                                        lines = f(line2).Except(ok_ents).ToList();
                                        if (lines.Count == 1)
                                        {
                                            var line3 = lines[0];
                                            ok_ents.Add(line3);
                                            var seg2 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line2)];
                                            var seg3 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line3)];
                                            var pt = ThRainSystemService.GetTargetPoint(seg2, seg3);
                                            var r = GRect.Create(pt, 50);
                                            geoData.WaterPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.WaterPortSymbols.Add(pl);
                                            item.WaterPortSymbols.Add(pl);
                                        }
                                    }
                                }
                            }
                        }
                    }


                    {
                        var gs = GeoFac.GroupGeometries(item.LabelLines).Where(g => g.Count >= 3).ToList();

                        //凭空生成一个雨水口
                        {
                            var labels = new List<Geometry>();
                            foreach (var lb in item.Labels)
                            {
                                var j = cadDataMain.Labels.IndexOf(lb);
                                var m = geoData.Labels[j];
                                var label = m.Text;
                                if (IsWaterPortLabel(label))
                                {
                                    labels.Add(lb);
                                }
                            }
                            var f = GeoFac.CreateIntersectsSelector(item.LabelLines);
                            foreach (var lb in labels)
                            {
                                var _lines = f(GRect.Create(lb.GetCenter(), 100).ToPolygon());
                                if (_lines.Count == 1)
                                {
                                    var firstLine = _lines[0];
                                    var g = gs.FirstOrDefault(g => g.Contains(firstLine));
                                    if (g != null)
                                    {
                                        var segs = g.Select(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).ToList();
                                        var h = GeoFac.LineGrouppingHelper.Create(segs);
                                        h.InitPointGeos(radius: 10);
                                        h.DoGroupingByPoint();
                                        h.CalcAlonePoints();
                                        var pointGeos = h.AlonePoints;
                                        {
                                            var hLabelLines = item.LabelLines.Select(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).Where(seg => seg.IsHorizontal(5)).ToList();
                                            var lst = item.Labels.Distinct().ToList();
                                            lst.Remove(lb);
                                            lst.AddRange(hLabelLines.Select(seg => seg.Buffer(10)));
                                            lst.Add(firstLine);
                                            lst.AddRange(item.VerticalPipes);
                                            lst.AddRange(item.WaterPort13s);
                                            lst.AddRange(item.WaterWells);
                                            lst.AddRange(item.WaterPortSymbols);
                                            var _geo = GeoFac.CreateGeometry(lst.Distinct().ToArray());
                                            pointGeos = pointGeos.Except(GeoFac.CreateIntersectsSelector(pointGeos)(_geo)).Distinct().ToList();
                                        }
                                        foreach (var geo in pointGeos)
                                        {
                                            var pt = geo.GetCenter();
                                            var r = GRect.Create(pt, 50);
                                            geoData.WaterPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.WaterPortSymbols.Add(pl);
                                            item.WaterPortSymbols.Add(pl);
                                        }
                                    }
                                }
                            }
                        }

                        if (false)
                        {
                            //修复屋面雨水斗，后面继续补上相关信息
                            var f1 = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                            var lbLinesGroups = GeoFac.GroupGeometries(item.LabelLines).Select(g => GeoFac.CreateGeometry(g.ToArray())).ToList();
                            var f2 = GeoFac.CreateIntersectsSelector(lbLinesGroups);
                            var f3 = GeoFac.CreateIntersectsSelector(item.WLines);
                            foreach (var lb in item.Labels)
                            {
                                var ct = geoData.Labels[cadDataMain.Labels.IndexOf(lb)];
                                var m = ThRainSystemService.TestGravityLabelConnected(ct.Text);
                                if (m.Success)
                                {
                                    var targetFloor = m.Groups[1].Value;
                                    var lst = f2(lb);
                                    if (lst.Count == 1)
                                    {
                                        var lblineGroupGeo = lst[0];
                                        var pipes = f1(lblineGroupGeo);
                                        if (pipes.Count == 1)
                                        {
                                            var pipe = pipes[0];
                                            lst = f3(pipe);
                                            lst.Remove(pipe);
                                            if (lst.Count == 1)
                                            {
                                                waterBucketFixingList.Add(new KeyValuePair<string, Geometry>(targetFloor, lst[0]));
                                            }
                                        }
                                    }
                                }
                            }
                        }


                    }


                }
                foreach (var o in item.LabelLines)
                {
                    var j = cadDataMain.LabelLines.IndexOf(o);
                    var m = geoData.LabelLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 1;
                }

                var labelLinesGroup = GeoFac.GroupGeometries(item.LabelLines);
                var lbsGeosFilter = GeoFac.CreateIntersectsSelector(labelLinesGroup.Select(lbs => GeoFac.CreateGeometry(lbs)).ToList());
                foreach (var pl in item.Labels)
                {
                    var j = cadDataMain.Labels.IndexOf(pl);
                    var m = geoData.Labels[j];
                    var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var _pl = DU.DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = 2;
                }
                foreach (var o in item.VerticalPipes)
                {
                    var j = cadDataMain.VerticalPipes.IndexOf(o);
                    var m = geoData.VerticalPipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    var j = cadDataMain.FloorDrains.IndexOf(o);
                    var m = geoData.FloorDrains[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 6;
                }
                foreach (var o in item.CondensePipes)
                {
                    var j = cadDataMain.CondensePipes.IndexOf(o);
                    var m = geoData.CondensePipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 2;
                }
                foreach (var o in item.WaterWells)
                {
                    var j = cadDataMain.WaterWells.IndexOf(o);
                    var m = geoData.WaterWells[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                }
                foreach (var o in item.SideWaterBuckets)
                {
                    var j = cadDataMain.SideWaterBuckets.IndexOf(o);
                    var m = geoData.SideWaterBuckets[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                    Dbg.ShowXLabel(m.Center, 100);
                }


                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var o in item.WaterPortSymbols)
                    {
                        var j = cadDataMain.WaterPortSymbols.IndexOf(o);
                        var m = geoData.WaterPortSymbols[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                    foreach (var o in item.WaterPort13s)
                    {
                        var j = cadDataMain.WaterPort13s.IndexOf(o);
                        var m = geoData.WaterPort13s[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var o in item.WrappingPipes)
                    {
                        var j = cadDataMain.WrappingPipes.IndexOf(o);
                        var m = geoData.WrappingPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                var shortTranslatorLabels = new HashSet<string>();

                {
                    var gbkf = GeoFac.CreateIntersectsSelector(item.GravityWaterBuckets);
                    foreach (var lb in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(lb);
                        var m = geoData.Labels[j];
                        var label = m.Text;
                        if (IsGravityWaterBucketDNText(label))
                        {
                            var dn = re.Match(label).Groups[1].Value;
                            var lst = lbsGeosFilter(m.Boundary.ToPolygon());
                            if (lst.Count == 1)
                            {
                                lst = gbkf(lst[0]);
                                if (lst.Count == 1)
                                {
                                    drData.GravityWaterBuckets.Add(new KeyValuePair<string, GRect>(dn, geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(lst[0])]));
                                }
                            }
                        }
                    }

                }
                {
                    var ok_ents = new HashSet<Geometry>();
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeoFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                {
                                    //过滤掉不正确的text
                                    var tmp = new List<Geometry>();
                                    foreach (var lb in _labels)
                                    {
                                        var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text;
                                        if (ThRainSystemService.IsWantedLabelText(label) || label.StartsWith("YL"))
                                        {
                                            tmp.Add(lb);
                                        }
                                    }
                                    _labels = tmp;
                                }
                                if (_labels.Count == 1 && _pipes.Count == 1)
                                {
                                    var pp = _pipes[0];
                                    if (lbDict.ContainsKey(pp)) continue;
                                    var lb = _labels[0];
                                    var j = cadDataMain.Labels.IndexOf(lb);
                                    var m = geoData.Labels[j];
                                    var label = m.Text;
                                    lbDict[pp] = label;
                                    //OK，识别成功
                                    ok_ents.Add(pp);
                                    ok_ents.Add(lb);
                                    break;
                                }
                            }
                        }
                    }
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeoFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_labels.Count == 1 && _pipes.Count == 2)
                                {
                                    var pp1 = _pipes[0];
                                    var pp2 = _pipes[1];
                                    {
                                        if (!lbDict.ContainsKey(pp1))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp1] = label;
                                            ok_ents.Add(pp1);
                                        }
                                        if (!lbDict.ContainsKey(pp2))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp2] = label;
                                            ok_ents.Add(pp2);
                                        }
                                        shortTranslatorLabels.Add(lbDict[pp1]);
                                    }
                                }
                            }
                        }
                    }
                    //上面的提取一遍，然后再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeoFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                //{
                                //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                                //    r.Expand(3);
                                //    var pl = DU.DrawRectLazy(r);
                                //    pl.ColorIndex = 3;
                                //}
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                                //这是原先识别短转管的代码，碰到某种case，会出问题，先注释掉
                                //else if (lbs.Count == 1)
                                //{
                                //    var lb = lbs[0];
                                //    var j = cadDataMain.Labels.IndexOf(lb);
                                //    var m = geoData.Labels[j];
                                //    var label = m.Text;
                                //    foreach (var pp in pps)
                                //    {
                                //        lbDict[pp] = label;
                                //        shortTranslatorLabels.Add(label);
                                //    }
                                //}
                            }
                        }
                    }

                    //再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeoFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                if (!g.Any(pl => labelLines.Contains(pl))) continue;
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                            }
                        }
                    }
                }
                List<List<Geometry>> wLinesGroups;
                {
                    var gs = GeoFac.GroupGeometries(item.WLines);
                    wLinesGroups = gs;

                    //var wlines = item.WLines;
                    //var gwlines = wlines.Select(wl => cadDataMain.WLines.IndexOf(wl)).Select(i => geoData.WLines[i]).ToList();
                    //var h = GeometryFac.LineGrouppingHelper.Create(gwlines);
                    //h.InitPointGeos(20);
                    //h.DoGroupingByPoint();
                    //{
                    //    var gs = h.GeoGroupsByPoint;
                    //    var pts = h.DoublePoints;
                    //    var gs2 = new List<List<Geometry>>();
                    //    foreach (var g in gs)
                    //    {
                    //        var _g = new List<Geometry>();
                    //        foreach (var pt in g)
                    //        {
                    //            var i = pts.IndexOf(pt);
                    //            _g.Add(wlines[i]);
                    //        }
                    //        gs2.Add(_g);
                    //    }
                    //    wLinesGroups = gs2;
                    //}

                    //foreach (var g in gs)
                    //{
                    //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                    //    r.Expand(3);
                    //    var pl = DU.DrawRectLazy(r);
                    //    pl.ColorIndex = 3;
                    //}

                }
                foreach (var o in item.WLines)
                {
                    var j = cadDataMain.WLines.IndexOf(o);
                    var m = geoData.WLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 4;
                    //if (m.IsVertical(5))
                    //{
                    //    var ee = DU.DrawGeometryLazy(m);
                    //    ee.ColorIndex = 4;
                    //    ee.ConstantWidth = 100;
                    //}
                    //{
                    //    //DU.DrawTextLazy(m.AngleDegree.ToString(),100, m.StartPoint.ToPoint3d());
                    //}
                }

                var longTranslatorLabels = new HashSet<string>();
                {
                    foreach (var wlines in wLinesGroups)
                    {
                        var gs = GeoFac.GroupGeometries(ToList(wlines, item.VerticalPipes));
                        foreach (var g in gs)
                        {
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes, _wlines)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                //两根立管都要与wline相连才行
                                bool test(Geometry pipe)
                                {
                                    var lst = ToList(_wlines);
                                    lst.Add(pipe);
                                    var _gs = GeoFac.GroupGeometries(lst);
                                    foreach (var _g in _gs)
                                    {
                                        if (!_g.Contains(pipe)) continue;
                                        var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                        if (!AllNotEmpty(__wlines)) continue;
                                        return true;
                                    }
                                    return false;
                                }
                                if (test(pp1) && test(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    {
                                        var lst = ToList(_wlines);
                                        lst.Add(pp1);
                                        lst.Add(pp2);
                                        var _gs = GeoFac.GroupGeometries(lst).Where(_g => _g.Count == 3 && _g.Contains(pp1) && _g.Contains(pp2)).ToList();
                                        foreach (var _g in _gs)
                                        {
                                            var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                            if (__wlines.Count == 1)
                                            {
                                                var gWLine = geoData.WLines[cadDataMain.WLines.IndexOf(__wlines[0])];
                                                if (gWLine.Length <= 300)
                                                {
                                                    isShort = true;
                                                    shortTranslatorLabels.Add(label);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }
                {
                    //临时修复wline中间被其他wline横插一脚的情况
                    foreach (var wline in item.WLines)
                    {
                        var lst = ToList(item.VerticalPipes);
                        lst.Add(wline);
                        var gs = GeoFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(wline)) continue;
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(wline);
                                //两根立管都要与wline相连才行
                                if (gf.Intersects(pp1) && gf.Intersects(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    if (geoData.WLines[cadDataMain.WLines.IndexOf(wline)].Length <= 300)
                                    {
                                        isShort = true;
                                        shortTranslatorLabels.Add(label);
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }


                {
                    //var pps = new List<GRect>();
                    //foreach (var o in item.VerticalPipes)
                    //{
                    //    var j = cadData.VerticalPipes.IndexOf(o);
                    //    var m = geoData.VerticalPipes[j];
                    //    pps.Add(m);
                    //}
                    GRect getRect(Geometry o)
                    {
                        return geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)];
                    }
                    ThRainSystemService.Triangle(item.VerticalPipes, (_pp1, _pp2) =>
                    {
                        Geometry pp1, pp2;
                        if (lbDict.ContainsKey(_pp1) && !lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp1; pp2 = _pp2;
                        }
                        else if (!lbDict.ContainsKey(_pp1) && lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp2; pp2 = _pp1;
                        }
                        else
                        {
                            return;
                        }
                        var r1 = getRect(pp1);
                        var r2 = getRect(pp2);
                        if (r1.Center.GetDistanceTo(r2.Center) < r1.OuterRadius + r2.OuterRadius + 5)
                        {
                            var label = lbDict[pp1];
                            lbDict[pp2] = label;
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }
                {
                    var pipes = item.VerticalPipes.Where(p => lbDict.ContainsKey(p)).ToList();
                    ThRainSystemService.Triangle(pipes, (p1, p2) =>
                    {
                        if (lbDict[p1] != lbDict[p2]) return;
                        var label = lbDict[p1];
                        if (p1.GetCenter().GetDistanceTo(p2.GetCenter()) <= 300)
                        {
                            longTranslatorLabels.Remove(label);
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }

                var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                _longTranslatorLabels.Sort();
                sb.AppendLine("长转管:" + _longTranslatorLabels.JoinWith(","));
                drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);

                var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                _shortTranslatorLabels.Sort();
                sb.AppendLine("短转管:" + _shortTranslatorLabels.JoinWith(","));
                drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);

                #region 地漏
                //var floorDrainsLabelAndCount = new CountDict<string>();
                var floorDrainsLabelAndEnts = new ListDict<string, Geometry>();
                var floorDrainsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.FloorDrains));
                        var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.FloorDrains));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            //var wrappingPipes = g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                            var wrappingPipes = new List<Geometry>();
                            {
                                var f = GeoFac.CreateIntersectsSelector(item.WrappingPipes);
                                foreach (var wline in wlines)
                                {
                                    wrappingPipes.AddRange(f(wline));
                                }
                                wrappingPipes = wrappingPipes.Distinct().ToList();
                            }

                            if (!AllNotEmpty(pps, fds, wlines)) continue;

                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 1;
                            //}

                            foreach (var pp in pps)
                            {
                                //新的逻辑
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label == null) continue;

                                    {
                                        var lst = ToList(wlines, fds);
                                        lst.Add(pp);
                                        var _gs = GeoFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            if (!_g.Contains(pp)) continue;
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            if (!AllNotEmpty(_fds, _wlines)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeoFac.CreateIntersectsSelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var _wrappingPipes = new List<Geometry>();
                                                var f = GeoFac.CreateIntersectsSelector(wrappingPipes);
                                                foreach (var wline in _wlines)
                                                {
                                                    _wrappingPipes.AddRange(f(wline));
                                                }
                                                _wrappingPipes = wrappingPipes.Distinct().ToList();
                                                foreach (var wp in _wrappingPipes)
                                                {
                                                    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                            }
                                        }
                                    }
                                    if (false)
                                    {
                                        var lst = ToList(wlines, fds, wrappingPipes);
                                        lst.Add(pp);
                                        var _gs = GeoFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            var _wrappingPipes = g.Where(pl => wrappingPipes.Contains(pl)).ToList();
                                            var _pps = g.Where(pl => pl == pp).ToList();
                                            if (!AllNotEmpty(_fds, _wlines, _pps)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeoFac.CreateIntersectsSelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var __gs = GeoFac.GroupGeometries(ToList(_wrappingPipes, _wlines));
                                                foreach (var __g in __gs)
                                                {
                                                    var __wlines = __g.Where(pl => _wlines.Contains(pl)).ToList();
                                                    var wps = __g.Where(pl => _wrappingPipes.Contains(pl)).ToList();
                                                    if (!AllNotEmpty(wps, __wlines)) continue;
                                                    foreach (var wp in wps)
                                                    {
                                                        floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ////原先的逻辑
                                //if (false)
                                //{
                                //    lbDict.TryGetValue(pp, out string label);
                                //    if (label != null)
                                //    {
                                //        //floorDrainsLabelAndCount[label] += fds.Count;
                                //        foreach (var fd in fds)
                                //        {
                                //            floorDrainsLabelAndEnts.Add(label, fd);
                                //        }
                                //        //foreach (var wp in wrappingPipes)
                                //        //{
                                //        //    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //        //}
                                //        //套管还要在wline上才行
                                //        var _gs = GeometryFac.GroupGeometries(ToList(wrappingPipes, item.WLines));
                                //        foreach (var _g in _gs)
                                //        {
                                //            var _wlines = _g.Where(pl => item.WLines.Contains(pl)).ToList();
                                //            var wps = _g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                                //            if (!AllNotEmpty(wps, _wlines)) continue;
                                //            foreach (var wp in wps)
                                //            {
                                //                floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //            }
                                //        }
                                //    }
                                //}
                            }
                        }
                    }
                }


                {


                }
                {
                    //😗佳兆业滨江新城--NL1-4和NL2-4莫名多了地漏
                    foreach (var kv in floorDrainsLabelAndEnts.Where(x => x.Value.Distinct().Count() == 2).ToList())
                    {
                        var fds = kv.Value.Distinct().ToList();
                        var label = kv.Key;
                        var pipes = item.VerticalPipes.Where(pipe =>
                        {
                            lbDict.TryGetValue(pipe, out string _label);
                            return _label == label;
                        }).ToList();
                        if (pipes.Count == 2)
                        {
                            var c1 = pipes[0].GetCenter();
                            var c2 = pipes[1].GetCenter();
                            var dis1 = c1.GetDistanceTo(c2);
                            var c3 = fds[0].GetCenter();
                            var c4 = fds[1].GetCenter();
                            var dis2 = c3.GetDistanceTo(c4);

                            if (Math.Abs(dis1 - dis2) < 1 && (c1 - c2).IsParallelTo(c3 - c4, new Tolerance(1, 1)))
                            {
                                {
                                    var lst = floorDrainsLabelAndEnts[label];
                                    lst.Clear();
                                    lst.Add(fds[0]);
                                }
                                {
                                    floorDrainsWrappingPipesLabelAndEnts.TryGetValue(label, out List<Geometry> lst);
                                    if (lst != null)
                                    {
                                        if (lst.Count > 1)
                                        {
                                            var x = lst[0];
                                            lst.Clear();
                                            lst.Add(x);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                {
                    var ok_labels = new HashSet<string>(floorDrainsLabelAndEnts.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key));
                    //当地漏和立管非常靠近的情况下，有的设计师会不画横管，导致程序识别时只找到了立管。
                    //处理方式：
                    //若地漏没有连接任何横管，则在地漏圆心500的范围内找没有连接任何地漏的最近的NL或Y2L，认为两者相连。
                    var gs = GeoFac.GroupGeometries(ToList(item.FloorDrains, item.WLines));
                    var f = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    foreach (var g in gs)
                    {
                        if (g.Count == 1)
                        {
                            var _x = g[0];
                            if (item.FloorDrains.Contains(_x))
                            {
                                var fd = _x;
                                var center = fd.GetCenter().ToPoint3d();
                                var range = GeoFac.CreateCirclePolygon(center, 500, 6);
                                var pipes = f(range).Where(pipe =>
                                {
                                    lbDict.TryGetValue(pipe, out string label);
                                    if (label != null && label.StartsWith("Y2L") && label.StartsWith("NL"))
                                    {
                                        if (!ok_labels.Contains(label))
                                        {
                                            return true;
                                        }
                                    }
                                    return false;
                                }).ToList();
                                var pipe = GeoFac.NearestNeighbourPoint3dF(pipes)(center);
                                if (pipe != null)
                                {
                                    lbDict.TryGetValue(pipe, out string label);
                                    if (label != null)
                                    {
                                        floorDrainsLabelAndEnts.Add(label, fd);
                                        ok_labels.Add(label);
                                    }
                                }
                            }
                        }
                    }
                }
                //sb.AppendLine("地漏:" + floorDrainsLabelAndCount.Select(kv => $"{kv.Key}({kv.Value})").JoinWith(","));
                sb.AppendLine("地漏:" + floorDrainsLabelAndEnts
                    .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("地漏套管:" + floorDrainsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in floorDrainsLabelAndEnts)
                {
                    drData.FloorDrains.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in floorDrainsWrappingPipesLabelAndEnts)
                {
                    drData.FloorDrainsWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                #endregion

                #region 冷凝管
                var condensePipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.CondensePipes));
                        var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.CondensePipes));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, cps, wlines)) continue;

                            if (pps.Count != 1) continue;
                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 5;
                            //}
                            var pp = pps[0];
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                //floorDrainsLabelAndCount[label] += fds.Count;
                                foreach (var cp in cps)
                                {
                                    condensePipesLabelAndEnts.Add(label, cp);
                                }
                            }
                        }
                    }
                }
                //sb.AppendLine("冷凝管:" + condensePipesLabelAndEnts
                //   .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));

                //生成辅助线
                var brokenCondensePipeLines = new List<GLineSegment>();
                {
                    var wlines = item.WLines.Select(o => geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ToList();
                    var li = wlines.SelectInts(wl => wl.IsHorizontal(5)).ToList();
                    ThRainSystemService.Triangle(li, (i, j) =>
                    {
                        var kvs = GeoAlgorithm.YieldPoints(wlines[i], wlines[j]).ToList();
                        var pts = kvs.Flattern().ToList();
                        var tol = 5;
                        var _y = pts[0].Y;
                        if (pts.All(pt => GeoAlgorithm.InRange(pt.Y, _y, tol)))
                        {
                            var dis = kvs.Select(kv => kv.Key.GetDistanceTo(kv.Value)).Min();
                            if (dis > 100 && dis < 300)
                            {
                                var x1 = pts.Select(pt => pt.X).Min();
                                var x2 = pts.Select(pt => pt.X).Max();
                                var newSeg = new GLineSegment(x1, _y, x2, _y);
                                if (newSeg.Length > 0) brokenCondensePipeLines.Add(newSeg);
                                //var pl=DU.DrawGeometryLazy(newSeg);
                                //pl.ConstantWidth = 20;
                            }
                        }
                    });
                }
                //收集断开的冷凝管
                var brokenCondensePipes = new List<List<Geometry>>();
                {
                    var bkCondensePipeLines = brokenCondensePipeLines.Select(seg => seg.Buffer(10)).ToList();
                    var gs = GeoFac.GroupGeometries(ToList(item.CondensePipes, bkCondensePipeLines));
                    foreach (var g in gs)
                    {
                        var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => bkCondensePipeLines.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        if (cps.Count < 2) continue;
                        brokenCondensePipes.Add(cps);
                    }
                }
                {
                    IEnumerable<KeyValuePair<string, KeyValuePair<int, bool>>> GetCondensePipesData()
                    {
                        foreach (var kv in condensePipesLabelAndEnts)
                        {
                            List<Geometry> f()
                            {
                                var lst = kv.Value.ToList();
                                foreach (var cp1 in lst)
                                {
                                    foreach (var lst2 in brokenCondensePipes)
                                    {
                                        foreach (var cp2 in lst2)
                                        {
                                            if (cp1 == cp2)
                                            {
                                                return lst2;
                                            }
                                        }
                                    }
                                }
                                return null;
                            }
                            var ret = f();
                            if (ret == null)
                            {
                                //yield return $"{kv.Key}({kv.Value.Count},非断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(kv.Value.Count, false));
                            }
                            else
                            {
                                //yield return $"{kv.Key}({ret.Count},断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(ret.Count, true));
                            }
                        }
                    }
                    var lst = GetCondensePipesData().ToList();
                    sb.AppendLine("冷凝管:" + lst.Select(kv => kv.Value.Value ? $"{kv.Key}({kv.Value.Value},断开)" : $"{kv.Key}({kv.Value.Value},非断开)").JoinWith(","));
                    drData.CondensePipes.AddRange(lst);
                }

                #endregion

                var waterWellsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var rainPortsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var outputDict = new Dictionary<string, RainOutputTypeEnum>();

                bool ok = false;
                void CollectOutputs(List<Geometry> item_WLines, List<Geometry> item_WaterWells,
                    List<Geometry> item_WrappingPipes, List<Geometry> item_WaterPortSymbols, List<Geometry> item_VerticalPipes)
                {
                    if (ok)
                    {
                        foreach (var geo in item_WLines)
                        {
                            DU.DrawGeometryLazy(geo);
                        }
                    }
                    var wlinesGeo = new List<Geometry>();
                    {
                        var gs = GeoFac.GroupGeometries(item_WLines);
                        foreach (var g in gs)
                        {
                            wlinesGeo.Add(GeoFac.CreateGeometry(g.ToArray()));
                        }
                    }
                    var filtWLines = GeoFac.CreateIntersectsSelector(wlinesGeo);
                    {
                        var f1 = GeoFac.CreateIntersectsSelector(item_WrappingPipes);
                        var f2 = GeoFac.CreateIntersectsSelector(item_WaterPortSymbols);
                        var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterPortSymbols));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                            var symbols = g.Where(pl => item_WaterPortSymbols.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, wlines, symbols)) continue;
                            foreach (var pp in pps)
                            {
                                lbDict.TryGetValue(pp, out string label);
                                if (label != null)
                                {
                                    if (outputDict.ContainsKey(label)) continue;
                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                    var lst = new List<Geometry>();
                                    lst.Add(pp);
                                    lst.AddRange(wlines);
                                    var geo = GeoFac.CreateGeometry(lst.ToArray());
                                    var wp = f1(geo).FirstOrDefault();
                                    if (wp != null)
                                    {
                                        rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                    }
                                }
                            }
                        }
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        var wps = f1(pipe);
                                        if (wps.Count == 1)
                                        {
                                            var wp = wps[0];
                                            var wlines = filtWLines(wp);
                                            foreach (var wline in wlines)
                                            {
                                                if (f2(wline).Count > 0)
                                                {
                                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                    {
                        var pipeLabelToWaterWellLabels = new List<KeyValuePair<string, string>>();
                        var ok_wells = new HashSet<Geometry>();
                        {
                            var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterWells));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(w)];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////套管还要在wline上才行
                                        //var _lst = ToList(wrappingPipes, wlines);
                                        //_lst.Add(pp);
                                        //var _gs = GeometryFac.GroupGeometries(_lst);
                                        //foreach (var _g in _gs)
                                        //{
                                        //    if (!_g.Contains(pp)) continue;
                                        //    //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //    var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //    var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //    if (!AllNotEmpty(wps, _wlines)) continue;
                                        //    foreach (var wp in wps)
                                        //    {
                                        //        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //    }
                                        //}
                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeoFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }


                                    }
                                }
                            }
                        }
                        {
                            var _wells = item_WaterWells.Except(ok_wells).ToList();
                            var gwells = _wells.Select(o => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ToList();
                            for (int k = 0; k < gwells.Count; k++)
                            {
                                gwells[k] = GRect.Create(gwells[k].Center, 1500);
                            }
                            var shadowWells = gwells.Select(r => r.ToLinearRing()).Cast<Geometry>().ToList();
                            var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, shadowWells, item.WaterPort13s));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl =>
                                {
                                    var k = shadowWells.IndexOf(pl);
                                    if (k < 0) return false;
                                    return item_WaterWells.Contains(_wells[k]);
                                }).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();

                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(_wells[shadowWells.IndexOf(w)])];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////检查是否有套管
                                        //if (wrappingPipes.Count > 0)
                                        //{
                                        //    //套管还要在wline上才行
                                        //    var _lst = ToList(wrappingPipes, wlines);
                                        //    _lst.Add(pp);
                                        //    var _gs = GeometryFac.GroupGeometries(_lst);
                                        //    foreach (var _g in _gs)
                                        //    {
                                        //        if (!_g.Contains(pp)) continue;
                                        //        //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //        var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //        var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //        if (!AllNotEmpty(wps, _wlines)) continue;
                                        //        foreach (var wp in wps)
                                        //        {
                                        //            waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //        }
                                        //    }
                                        //}

                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeoFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }
                                    }
                                }
                            }
                        }

                        sb.AppendLine("pipeLabelToWaterWellLabels：" + pipeLabelToWaterWellLabels.ToCadJson());
                        drData.PipeLabelToWaterWellLabels.AddRange(pipeLabelToWaterWellLabels);
                    }
                    {
                        var f1 = GeoFac.CreateIntersectsSelector(item_WrappingPipes);
                        var f3 = GeoFac.CreateIntersectsSelector(item_WaterPortSymbols);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            lbDict.TryGetValue(pipe, out string label);
                            if (label != null)
                            {
                                if (!outputDict.ContainsKey(label))
                                {
                                    var wrappingPipes = f1(pipe);
                                    if (wrappingPipes.Count == 1)
                                    {
                                        var wp = wrappingPipes[0];
                                        var _wlines = filtWLines(wp);
                                        if (_wlines.Count == 1)
                                        {
                                            var wline = _wlines[0];
                                            var symbols = f3(wline);
                                            if (symbols.Count == 1)
                                            {
                                                var symbol = symbols[0];
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        var f2 = GeoFac.CreateIntersectsSelector(item.WaterPort13s);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        foreach (var wlineG in filtWLines(pipe))
                                        {
                                            if (f2(wlineG).Any())
                                            {
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                foreach (var wp in f1(pipe).Concat(f1(wlineG).Distinct()))
                                                {
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                }

                CollectOutputs(item.WLines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                {
                    var wlines1 = item.WLines.Select(pl => geoData.WLines[cadDataMain.WLines.IndexOf(pl)]).ToList();
                    var rs = item.WaterWells.Select(pl => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(pl)])
                        .Concat(item.WrappingPipes.Select(pl => geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(pl)]))
                        .Concat(item.WaterPortSymbols.Select(pl => geoData.WaterPortSymbols[cadDataMain.WaterPortSymbols.IndexOf(pl)]))
                        .Concat(item.VerticalPipes.Select(pl => geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(pl)]))
                        .ToList();

                    void f5(List<GLineSegment> segs)
                    {
                        var _wlines1 = wlines1.Select(seg => seg.Buffer(10)).ToList();
                        var f1 = GeoFac.CreateIntersectsSelector(_wlines1);
                        var _wlines2 = segs.Select(seg => seg.Extend(-20).Buffer(10)).ToList();
                        var _wlines3 = segs.Select(seg => seg.Extend(.1).Buffer(10)).ToList();
                        var f2 = GeoFac.CreateIntersectsSelector(_wlines2);
                        var _rs = item.WaterWells.Concat(item.WrappingPipes).Concat(item.WaterPortSymbols).Concat(item.VerticalPipes).Distinct().ToArray();
                        var __wlines2 = _wlines2.Except(f2(GeoFac.CreateGeometry(_rs))).Distinct().ToList();
                        var __wlines3 = __wlines2.Select(_wlines2).ToList().Select(_wlines3).ToList();
                        var wlines = _wlines1.Except(f1(GeoFac.CreateGeometry(__wlines2.ToArray()))).Concat(__wlines3).Distinct().ToList();

                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var wline in wlines)
                        //    {
                        //        DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                        //    }
                        //    DU.Draw();
                        //});
                        {
                            foreach (var wline in __wlines3)
                            {
                                DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                            }
                        }
                        CollectOutputs(wlines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                    }
                    {
                        var segs = FengDbgTesting.GetSegsToConnect(wlines1, rs, 8000, radius: 15).Distinct().ToList();
                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var seg in segs)
                        //    {
                        //        DU.DrawLineSegmentLazy(seg);
                        //    }
                        //    DU.Draw();
                        //});
                        f5(segs);
                    }
                    {
                        var segs = item.WLinesAddition.Select(cadDataMain.WLinesAddition).ToList().Select(geoData.WLinesAddition).ToList();
                        ok = true;
                        f5(segs);
                    }
                }


                sb.AppendLine("排出方式：" + outputDict.ToCadJson());
                sb.AppendLine("雨水井套管:" + waterWellsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("雨水口套管:" + rainPortsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in outputDict)
                {
                    drData.OutputTypes.Add(kv);
                }
                foreach (var kv in waterWellsWrappingPipesLabelAndEnts)
                {
                    drData.WaterWellWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in rainPortsWrappingPipesLabelAndEnts)
                {
                    drData.RainPortWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }

                var gravityWaterBucketTranslatorLabels = new List<string>();
                {
                    var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.GravityWaterBuckets));
                    foreach (var g in gs)
                    {
                        var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                        var gbks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                        if (!AllNotEmpty(pps, wlines, gbks)) continue;

                        foreach (var pp in pps)
                        {
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                gravityWaterBucketTranslatorLabels.Add(label);
                            }
                        }
                    }
                }
                gravityWaterBucketTranslatorLabels = gravityWaterBucketTranslatorLabels.Distinct().ToList();
                gravityWaterBucketTranslatorLabels.Sort();
                sb.AppendLine("重力雨水斗转管:" + gravityWaterBucketTranslatorLabels.JoinWith(","));
                drData.GravityWaterBucketTranslatorLabels.AddRange(gravityWaterBucketTranslatorLabels);


                {
                    //补丁：把部分重力雨水斗变成87雨水斗
                    var _87bks = new List<Geometry>();
                    var labels = item.Labels.Where(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text == "87型雨水斗").ToList();
                    foreach (var label in labels)
                    {
                        var lst = item.LabelLines.ToList();
                        lst.Add(label);
                        lst.AddRange(item.GravityWaterBuckets);
                        var gs = GeoFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(label)) continue;
                            var labelLines = g.Where(pl => item.LabelLines.Contains(pl)).ToList();
                            var bks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                            if (!AllNotEmpty(labelLines, bks)) continue;
                            _87bks.AddRange(bks);
                        }
                    }
                    _87bks = _87bks.Distinct().ToList();
                    item._87WaterBuckets.AddRange(_87bks);
                    cadDataMain._87WaterBuckets.AddRange(_87bks);
                    geoData._87WaterBuckets.AddRange(_87bks.Select(pl => geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(pl)]));
                    foreach (var bk in _87bks)
                    {
                        item.GravityWaterBuckets.Remove(bk);
                        var i = cadDataMain.GravityWaterBuckets.IndexOf(bk);
                        cadDataMain.GravityWaterBuckets.RemoveAt(i);
                        geoData.GravityWaterBuckets.RemoveAt(i);
                    }
                    foreach (var o in item.GravityWaterBuckets)
                    {
                        var j = cadDataMain.GravityWaterBuckets.IndexOf(o);
                        var m = geoData.GravityWaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 200);
                    }
                    foreach (var o in item._87WaterBuckets)
                    {
                        var j = cadDataMain._87WaterBuckets.IndexOf(o);
                        var m = geoData._87WaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 500);
                    }
                }




                {
                    foreach (var kv in waterBucketFixingList)
                    {
                        break;
                        lbDict.TryGetValue(kv.Value, out string label);
                        if (label != null)
                        {
                            break;
                        }
                    }

                }
                {
                    var f1 = GeoFac.CreateIntersectsSelector(item.WLines);
                    var f2 = GeoFac.CreateIntersectsSelector(lbDict.Keys.ToList());
                    foreach (var kv in lbDict)
                    {
                        var m = ThRainSystemService.TestGravityLabelConnected(kv.Value);
                        if (m.Success)
                        {
                            var pipe = kv.Key;
                            var targetFloor = m.Groups[1].Value;
                            var lst = f1(pipe);
                            if (lst.Count == 1)
                            {
                                lst = f2(lst[0]);
                                lst.Remove(pipe);
                                if (lst.Count == 1)
                                {
                                    lbDict.TryGetValue(lst[0], out string label);
                                    if (label != null)
                                    {
                                        var tp = new Tuple<int, string, string>(storeyI, label, targetFloor);
                                        drData.GravityWaterBucketTranslatorLabels.Add(label);
                                    }
                                }
                            }


                        }
                    }
                }

                {
                    //标出所有的立管编号（看看识别成功了没）
                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            DU.DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                        }
                    }

                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            var r = GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(pp)];
                            drData.VerticalPipes.Add(new KeyValuePair<string, GRect>(label, r));
                        }
                    }
                }
            }

            if (ShouldPrintReadableDrawingData) Dbg.PrintText(sb.ToString());
        }
        static bool ShouldPrintReadableDrawingData = true;
        //static bool ShouldPrintReadableDrawingData = false;


        public void FixWaterBucketDN()
        {
            static bool f(ThWSDWaterBucket x) => x.Storey != null && x.Boundary.IsValid;
            var list = new List<ThWSDWaterBucket>();
            list.AddRange(RainSystemDiagram.RoofVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            list.AddRange(RainSystemDiagram.BalconyVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            list.AddRange(RainSystemDiagram.CondenseVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            foreach (var drData in DrawingDatas)
            {
                list.Join(drData.GravityWaterBuckets, x => x.Boundary, x => x.Value, (x, y) =>
                {
                    x.DN = y.Key;
                    return 0;
                }).Count();
            }
        }
        public void AddPipeRunsForRF<T>(string roofPipeNote, T sys) where T : ThWRainPipeSystem
        {
            var runs = sys.PipeRuns;
            var WSDStoreys = RainSystemDiagram.WSDStoreys;
            bool HasGravityLabelConnected(GRect bd, string pipeId)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var drData = DrawingDatas[i];
                    return drData.GravityWaterBucketTranslatorLabels.Contains(pipeId);
                }
                return false;
            }
            List<Extents3d> GetRelatedGravityWaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].GravityWaterBuckets.Select(o => GeoData.GravityWaterBuckets[CadDataMain.GravityWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetRelated87WaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i]._87WaterBuckets.Select(o => GeoData._87WaterBuckets[CadDataMain._87WaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetSideWaterBucketsInRange(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].SideWaterBuckets.Select(o => GeoData.SideWaterBuckets[CadDataMain.SideWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            WaterBucketEnum GetRelatedSideWaterBucket(Point3d center)
            {
                var p = center.ToPoint2d();
                foreach (var bd in GeoData.SideWaterBuckets)
                {
                    if (bd.ContainsPoint(p)) return WaterBucketEnum.Side;
                }
                return WaterBucketEnum.None;
            }
            IEnumerable<Point3d> GetCenterOfVerticalPipe(GRect bd, string label)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    foreach (var kv in drData.VerticalPipes)
                    {
                        if (kv.Key == label)
                        {
                            var center = kv.Value.Center.ToPoint3d();
                            yield return center;
                        }
                    }
                }
            }
            TranslatorTypeEnum GetTranslatorType(GRect bd, string label)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    if (drData.ShortTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Short;
                    if (drData.LongTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Long;
                }
                return TranslatorTypeEnum.None;
            }
            foreach (var s in WSDStoreys)
            {
                if (s.Label == "RF+2" || s.Label == "RF+1")
                {
                    var matchedPipe = s.VerticalPipes.FirstOrDefault(vp => vp.Label == roofPipeNote);
                    if (matchedPipe != null) runs.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                }
                else
                {
                }


                {
                    //for gravity bucket, still need to check label
                    //尝试通过label得到重力雨水斗
                    var hasWaterBucket = HasGravityLabelConnected(s.Boundary, roofPipeNote);
                    if (hasWaterBucket)
                    {
                        sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = RainSystemDiagram.GetHigerStorey(s) };

                        //???加这个piperun干啥？
                        //runs.Add(new ThWRainPipeRun()
                        //{
                        //    Storey = RainSystemDiagram.GetHigerStorey(s),
                        //});
                        return;
                    }
                }

                //var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(s.Boundary));
                //if (storey == null) continue;
                if (sys.WaterBucket.Storey == null)
                {
                    var lowerStorey = RainSystemDiagram.GetLowerStorey(s);
                    if (lowerStorey != null)
                    {
                        void f()
                        {
                            var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                            foreach (var roofPipeCenter in q)
                            {
                                var waterBucketType = GetRelatedSideWaterBucket(roofPipeCenter);

                                //side
                                if (!waterBucketType.Equals(WaterBucketEnum.None))
                                {
                                    if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                                    {
                                        //Dbg.ShowWhere(roofPipeCenter);
                                        sys.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s, };
                                        return;
                                    }
                                }
                            }
                        }
                        f();
                    }
                    //尝试通过对位得到侧入雨水斗
                    var allSideWaterBucketsInThisRange = GetSideWaterBucketsInRange(s.Boundary);

                    if (sys.WaterBucket.Storey == null && allSideWaterBucketsInThisRange.Count > 0)
                    {
                        if (lowerStorey != null)
                        {
                            void f()
                            {
                                var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                foreach (var roofPipeCenterInLowerStorey in q)
                                {
                                    var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                    var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                    //compute ucs
                                    foreach (var wbe in allSideWaterBucketsInThisRange)
                                    {
                                        var minPt = wbe.MinPoint;
                                        var maxPt = wbe.MaxPoint;

                                        var basePt = s.Boundary.LeftTop.ToPoint3d();

                                        var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                        var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                        var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                        if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                        {
                                            sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Side, Storey = s, Boundary = wbe.ToGRect() };
                                            return;
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }

                    //gravity
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到重力雨水斗
                        var allWaterBucketsInThisRange = GetRelatedGravityWaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (lowerStorey != null)
                            {
                                void f()
                                {
                                    var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                    foreach (var roofPipeCenterInLowerStorey in q)
                                    {
                                        var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                        var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                        //compute ucs
                                        foreach (var wbe in allWaterBucketsInThisRange)
                                        {
                                            var minPt = wbe.MinPoint;
                                            var maxPt = wbe.MaxPoint;

                                            var basePt = s.Boundary.LeftTop.ToPoint3d();

                                            var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                            var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                            var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                            if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                            {
                                                sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s, Boundary = wbe.ToGRect(), };
                                                return;
                                            }
                                        }
                                    }


                                }
                                f();
                            }
                        }
                    }

                    //87waterbucket
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到87waterbucket
                        var allWaterBucketsInThisRange = GetRelated87WaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (lowerStorey != null)
                            {

                                void f()
                                {
                                    var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                    foreach (var roofPipeCenterInLowerStorey in q)
                                    {
                                        var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                        var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                        //compute ucs
                                        foreach (var wbe in allWaterBucketsInThisRange)
                                        {
                                            var minPt = wbe.MinPoint;
                                            var maxPt = wbe.MaxPoint;

                                            var basePt = s.Boundary.LeftTop.ToPoint3d();

                                            var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                            var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                            var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                            if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                            {
                                                sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum._87, Storey = s, Boundary = wbe.ToGRect() };
                                                return;
                                            }
                                        }
                                    }

                                }
                                f();
                            }
                        }
                    }


                }
            }
        }
    }
}