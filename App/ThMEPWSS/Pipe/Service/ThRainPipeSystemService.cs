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

                        //???老王加这个piperun干啥？
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


namespace ThUtilExtensionsNs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NFox.Cad;
    using AcHelper;
    using Linq2Acad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.ApplicationServices;
    using ThMEPWSS.Uitl;
    using ThCADExtension;
    using Autodesk.AutoCAD.Geometry;
    using ThMEPWSS.CADExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using LS = System.Collections.Generic.List<string>;
    public static class ThDataItemExtensions
    {
        public static GRect ToRect(this Point3dCollection colle)
        {
            if (colle.Count == 0) return default;
            var arr = colle.Cast<Point3d>().ToArray();
            var x1 = arr.Select(p => p.X).Min();
            var x2 = arr.Select(p => p.X).Max();
            var y1 = arr.Select(p => p.Y).Max();
            var y2 = arr.Select(p => p.Y).Min();
            return new GRect(x1, y1, x2, y2);
        }
        public static string GetBlockEffectiveName(this BlockReference br)
        {
            if (!br.ObjectId.IsValid) return null;
            return DotNetARX.BlockTools.GetBlockName(br.ObjectId.GetObject(OpenMode.ForRead) as BlockReference);
        }
        public static DBObject[] ToArray(this DBObjectCollection colle)
        {
            var arr = new DBObject[colle.Count];
            System.Collections.IList list = colle;
            for (int i = 0; i < list.Count; i++)
            {
                var @object = (DBObject)list[i];
                arr[i] = @object;
            }
            return arr;
        }
        public static Dictionary<string, object> ToDict(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new Dictionary<string, object>();
            foreach (var p in colle.ToList())
            {
                ret[p.PropertyName] = p.Value;
            }
            return ret;
        }
        public static List<DynamicBlockReferenceProperty> ToList(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new List<DynamicBlockReferenceProperty>();
            foreach (DynamicBlockReferenceProperty item in colle)
            {
                ret.Add(item);
            }
            return ret;
        }
    }
}
#pragma warning disable
namespace ThMEPWSS.Pipe.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NFox.Cad;
    using AcHelper;
    using Linq2Acad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.ApplicationServices;
    using ThMEPWSS.Uitl;
    using ThCADExtension;
    using Autodesk.AutoCAD.Geometry;
    using ThMEPWSS.CADExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using LS = System.Collections.Generic.List<string>;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Internal;
    using Autodesk.AutoCAD.Runtime;
    using DotNetARX;
    using Dreambuild.AutoCAD;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Index.Strtree;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.DebugNs;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe.Engine;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThUtilExtensionsNs;
    using ThMEPWSS.DebugNs.RainSystemNs;
    #region Tools
    public static class PolylineTools
    {
        public static Polyline CreatePolyline(IList<Point3d> pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, pts[i].ToPoint2d(), 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(Point2dCollection pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolyline(params Point2d[] pts)
        {
            var pline = new Polyline();
            for (int i = 0; i < pts.Length; i++)
            {
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreateRectangle(Point2d pt1, Point2d pt2)
        {
            var minX = Math.Min(pt1.X, pt2.X);
            var maxX = Math.Max(pt1.X, pt2.X);
            var minY = Math.Min(pt1.Y, pt2.Y);
            var maxY = Math.Max(pt1.Y, pt2.Y);
            var pts = new Point2dCollection
{
new Point2d(minX, minY),
new Point2d(minX, maxY),
new Point2d(maxX, maxY),
new Point2d(maxX, minY)
};
            var pline = CreatePolyline(pts);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolygon(Point2d centerPoint, int num, double radius)
        {
            var pts = new Point2dCollection(num);
            double angle = 2 * Math.PI / num;
            for (int i = 0; i < num; i++)
            {
                var pt = new Point2d(centerPoint.X + radius * Math.Cos(i * angle),
                centerPoint.Y + radius * Math.Sin(i * angle));
                pts.Add(pt);
            }
            var pline = CreatePolyline(pts);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolyCircle(Point2d centerPoint, double radius)
        {
            var pt1 = new Point2d(centerPoint.X + radius, centerPoint.Y);
            var pt2 = new Point2d(centerPoint.X - radius, centerPoint.Y);
            var pline = new Polyline();
            pline.AddVertexAt(0, pt1, 1, 0, 0);
            pline.AddVertexAt(1, pt2, 1, 0, 0);
            pline.AddVertexAt(2, pt1, 1, 0, 0);
            pline.Closed = true;
            return pline;
        }
        public static Polyline CreatePolyArc(Point2d centerPoint, double radius, double startAngle, double endAngle)
        {
            var pt1 = new Point2d(centerPoint.X + radius * Math.Cos(startAngle),
            centerPoint.Y + radius * Math.Sin(startAngle));
            var pt2 = new Point2d(centerPoint.X + radius * Math.Cos(endAngle),
            centerPoint.Y + radius * Math.Sin(endAngle));
            var pline = new Polyline();
            pline.AddVertexAt(0, pt1, Math.Tan((endAngle - startAngle) / 4), 0, 0);
            pline.AddVertexAt(1, pt2, 0, 0, 0);
            return pline;
        }
        public static double DegreeToRadian(double angle)
        {
            return angle * (Math.PI / 180.0);
        }
    }
    public static class EllipseTools
    {
        public static Ellipse CreateEllipse(Point3d pt1, Point3d pt2)
        {
            var center = GeTools.MidPoint(pt1, pt2);
            var normal = Vector3d.ZAxis;
            var majorAxis = new Vector3d(Math.Abs(pt1.X - pt2.X) / 2, 0, 0);
            var ratio = Math.Abs((pt1.Y - pt2.Y) / (pt1.X - pt2.X));
            var ellipse = new Ellipse();
            ellipse.Set(center, normal, majorAxis, ratio, 0, 2 * Math.PI);
            return ellipse;
        }
        public static Point3d MidPoint(Point3d pt1, Point3d pt2)
        {
            return new Point3d((pt1.X + pt2.X) / 2,
            (pt1.Y + pt2.Y) / 2,
            (pt1.Z + pt2.Z) / 2);
        }
    }
    public static class CircleTools
    {
        public static Circle CreateCircle(Point3d pt1, Point3d pt2, Point3d pt3)
        {
            var va = pt1.GetVectorTo(pt2);
            var vb = pt1.GetVectorTo(pt3);
            var angle = va.GetAngleTo(vb);
            if (angle == 0 || angle == Math.PI)
            {
                return null;
            }
            else
            {
                var circle = new Circle();
                var geArc = new CircularArc3d(pt1, pt2, pt3);
                circle.Center = geArc.Center;
                circle.Radius = geArc.Radius;
                return circle;
            }
        }
        public static double AngleFromXAxis(Point3d pt1, Point3d pt2)
        {
            var vec = new Vector2d(pt1.X - pt2.X, pt1.Y - pt2.Y);
            return vec.Angle;
        }
        public static void AddFan(Point3d startPoint, Point3d pointOnArc, Point3d endPoint,
        out Arc arc, out Line line1, out Line line2)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                arc = new Arc();
                arc.CreateArc(startPoint, pointOnArc, endPoint);
                line1 = new Line(arc.Center, startPoint);
                line2 = new Line(arc.Center, endPoint);
                db.AddToModelSpace(line1, line2, arc);
                trans.Commit();
            }
        }
    }
    public static class ThBlockTools
    {
        public static ObjectId AddBlockTableRecord(Database db, string blkName, params Entity[] ents)
        {
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName))
            {
                var btr = new BlockTableRecord();
                btr.Name = blkName;
                ents.ForEach(ent => btr.AppendEntity(ent));
                bt.UpgradeOpen();
                bt.Add(btr);
                db.TransactionManager.AddNewlyCreatedDBObject(btr, true);
                bt.DowngradeOpen();
            }
            return bt[blkName];
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer,
        string blkName, Point3d position, Scale3d scale, double rotateAngle)
        {
            ObjectId blkRefId;
            var db = spaceId.Database;
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName)) return ObjectId.Null;
            var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            var br = new BlockReference(position, bt[blkName]);
            br.ScaleFactors = scale;
            br.Layer = layer;
            br.Rotation = rotateAngle;
            blkRefId = space.AppendEntity(br);
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            space.DowngradeOpen();
            return blkRefId;
        }
        public static void AddAttsToBlock(ObjectId blkId, params AttributeDefinition[] atts)
        {
            var db = blkId.Database;
            var btr = (BlockTableRecord)blkId.GetObject(OpenMode.ForWrite);
            foreach (AttributeDefinition att in atts)
            {
                btr.AppendEntity(att);
                db.TransactionManager.AddNewlyCreatedDBObject(att, true);
            }
            btr.DowngradeOpen();
        }
        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blkName,
        Point3d position, Scale3d scale, double rotateAngle, IDictionary<string, string> attrNameValues)
        {
            var db = spaceId.Database;
            var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blkName)) return ObjectId.Null;
            var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            var btrId = bt[blkName];
            var record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            var br = new BlockReference(position, btrId);
            br.ScaleFactors = scale;
            br.Layer = layer;
            br.Rotation = rotateAngle;
            space.AppendEntity(br);
            if (record.HasAttributeDefinitions)
            {
                foreach (ObjectId id in record)
                {
                    if (id.GetObject(OpenMode.ForRead) is AttributeDefinition attDef)
                    {
                        var attribute = new AttributeReference();
                        attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
                        attribute.Position = attDef.Position.TransformBy(br.BlockTransform);
                        attribute.Rotation = attDef.Rotation;
                        attribute.AdjustAlignment(db);
                        if (attrNameValues.ContainsKey(attDef.Tag.ToUpper()))
                        {
                            attribute.TextString = attrNameValues[attDef.Tag.ToUpper()];
                        }
                        br.AttributeCollection.AppendAttribute(attribute);
                        db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;
        }
        public static void UpdateAttributesInBlock(ObjectId blkRefId, IDictionary<string, string> attNameValues)
        {
            if (blkRefId.GetObject(OpenMode.ForRead) is BlockReference blkRef)
            {
                foreach (ObjectId id in blkRef.AttributeCollection)
                {
                    var attRef = (AttributeReference)id.GetObject(OpenMode.ForRead);
                    if (attNameValues.ContainsKey(attRef.Tag.ToUpper()))
                    {
                        attRef.UpgradeOpen();
                        attRef.TextString = attNameValues[attRef.Tag.ToUpper()];
                        attRef.DowngradeOpen();
                    }
                }
            }
        }
        public static string GetDynBlockValue(ObjectId blockId, string propName)
        {
            return blockId.GetDynProperties().Cast<DynamicBlockReferenceProperty>().FirstOrDefault(prop => prop.PropertyName == propName)?.Value?.ToString();
        }
        public static DynamicBlockReferencePropertyCollection GetDynProperties(ObjectId blockId)
        {
            var br = blockId.GetObject(OpenMode.ForRead) as BlockReference;
            if (br == null || !br.IsDynamicBlock) return null;
            return br.DynamicBlockReferencePropertyCollection;
        }
        public static void SetDynBlockValue(ObjectId blockId, string propName, object value)
        {
            var props = blockId.GetDynProperties();
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                if (!prop.ReadOnly && prop.PropertyName == propName)
                {
                    switch (prop.PropertyTypeCode)
                    {
                        case (short)DynBlockPropTypeCode.Short:
                            prop.Value = Convert.ToInt16(value);
                            break;
                        case (short)DynBlockPropTypeCode.Long:
                            prop.Value = Convert.ToInt64(value);
                            break;
                        case (short)DynBlockPropTypeCode.Real:
                            prop.Value = Convert.ToDouble(value);
                            break;
                        default:
                            prop.Value = value;
                            break;
                    }
                }
            }
        }
        public static string GetBlockName(BlockReference bref)
        {
            if (bref == null) return null;
            if (bref.IsDynamicBlock)
            {
                var idDyn = bref.DynamicBlockTableRecord;
                var btr = (BlockTableRecord)idDyn.GetObject(OpenMode.ForRead);
                return btr.Name;
            }
            else
            {
                return bref.Name;
            }
        }
        public static AnnotationScale AddScale(string scaleName, double paperUnits, double drawingUnits)
        {
            var db = Active.Database;
            var ocm = db.ObjectContextManager;
            var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            if (!occ.HasContext(scaleName))
            {
                var scale = new AnnotationScale();
                scale.Name = scaleName;
                scale.PaperUnits = paperUnits;
                scale.DrawingUnits = drawingUnits;
                return scale;
            }
            return null;
        }
        public static void AttachScale(ObjectId entId, params string[] scaleNames)
        {
            var db = entId.Database;
            var obj = entId.GetObject(OpenMode.ForRead);
            if (obj.Annotative != AnnotativeStates.NotApplicable)
            {
                if (obj is BlockReference br)
                {
                    var btr = (BlockTableRecord)br.BlockTableRecord.GetObject(OpenMode.ForWrite);
                    btr.Annotative = AnnotativeStates.True;
                }
                else if (obj.Annotative == AnnotativeStates.False)
                {
                    obj.Annotative = AnnotativeStates.True;
                    obj.UpgradeOpen();
                    var ocm = db.ObjectContextManager;
                    var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    foreach (var scaleName in scaleNames)
                    {
                        var scale = occ.GetContext(scaleName);
                        if (scale == null) continue;
                        ObjectContexts.AddContext(obj, scale);
                    }
                    obj.DowngradeOpen();
                }
            }
        }
    }
    #endregion
    public class Ref<T> : IComparable<Ref<T>>
    {
        public T Value;
        public Ref(T value)
        {
            this.Value = value;
        }
        public Ref() { }
        readonly int _id = _NewId();
        static int _tk = int.MinValue;
        static int _NewId()
        {
            return ++_tk;
        }
        public int CompareTo(Ref<T> other)
        {
            return this._id - other._id;
        }
        public override int GetHashCode()
        {
            return _id;
        }
    }
    public class BFSHelper2<T> where T : class
    {
        public KeyValuePair<T, T>[] Pairs;
        public T[] Items;
        public Action<BFSHelper2<T>, T> Callback;
        public HashSet<T> visited = new HashSet<T>();
        public Queue<T> queue = new Queue<T>();
        public T root;
        public T GetFirstNotVisited()
        {
            foreach (var item in Items) if (!visited.Contains(item)) return item;
            return null;
        }
        T[] getChildren(T item)
        {
            IEnumerable<T> f()
            {
                foreach (var kv in Pairs)
                {
                    if (kv.Key == item) yield return kv.Value;
                    if (kv.Value == item) yield return kv.Key;
                }
            }
            return f().Where(x => x != item).Distinct().ToArray();
        }
        public void BFS()
        {
            while (true)
            {
                var item = GetFirstNotVisited();
                if (item == null) break;
                BFS(item);
            }
        }
        public void BFS(T start)
        {
            root = start;
            visit(start);
            while (queue.Count > 0)
            {
                var sz = queue.Count;
                for (int i = 0; i < sz; i++)
                {
                    var cur = queue.Dequeue();
                    var children = getChildren(cur);
                    foreach (var c in children)
                    {
                        if (!visited.Contains(c))
                        {
                            visit(c);
                        }
                    }
                }
            }
        }
        private void visit(T i)
        {
            queue.Enqueue(i);
            visited.Add(i);
            Callback?.Invoke(this, i);
        }
    }
    public class BFSHelper
    {
        public KeyValuePair<int, int>[] Pairs;
        public int TotalCount;
        public Action<BFSHelper, int> Callback;
        public HashSet<int> visited = new HashSet<int>();
        public Queue<int> queue = new Queue<int>();
        public int root;
        public int GetFirstNotVisited()
        {
            for (int i = 0; i < TotalCount; i++)
            {
                if (!visited.Contains(i)) return i;
            }
            return -1;
        }
        int[] getChildren(int i)
        {
            IEnumerable<int> f()
            {
                foreach (var kv in Pairs)
                {
                    if (kv.Key == i) yield return kv.Value;
                    if (kv.Value == i) yield return kv.Key;
                }
            }
            return f().Where(x => x != i).Distinct().ToArray();
        }
        public void BFS()
        {
            while (true)
            {
                var start = GetFirstNotVisited();
                if (start < 0) break;
                BFS(start);
            }
        }
        public void BFS(int start)
        {
            root = start;
            visit(start);
            while (queue.Count > 0)
            {
                var sz = queue.Count;
                for (int i = 0; i < sz; i++)
                {
                    var cur = queue.Dequeue();
                    var children = getChildren(cur);
                    foreach (var c in children)
                    {
                        if (!visited.Contains(c))
                        {
                            visit(c);
                        }
                    }
                }
            }
        }
        private void visit(int i)
        {
            queue.Enqueue(i);
            visited.Add(i);
            Callback?.Invoke(this, i);
        }
    }
    public class FlagsArray<T>
    {
        public T[] Items { get; }
        bool[] flags;
        int cur;
        public FlagsArray(T[] items)
        {
            Items = items;
            flags = new bool[items.Length];
        }
        public void Clear()
        {
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = false;
            }
            cur = 0;
        }
        public void SetFlag()
        {
            flags[cur] = true;
        }
        public bool IsVisited(int i)
        {
            return flags[i];
        }
        public IEnumerable<T> Filt(IEnumerable<int> list)
        {
            foreach (var i in list)
            {
                if (!flags[i]) yield return Items[i];
            }
        }
        public IEnumerable<KeyValuePair<int, T>> Yield()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                cur = i;
                if (!flags[i]) yield return new KeyValuePair<int, T>(i, Items[i]);
            }
        }
        public int GetTrueCount()
        {
            return flags.Where(x => x).Count();
        }
        public int GetFalseCount()
        {
            return flags.Where(x => !x).Count();
        }
        public bool TryGetFirstFalseItem(out T value)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                var flag = flags[i];
                if (!flag)
                {
                    value = Items[i];
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
    public class ListDict<T>
    {
        Dictionary<T, List<T>> dict = new Dictionary<T, List<T>>();
        public void Add(T item, IEnumerable<T> items)
        {
            if (dict.TryGetValue(item, out List<T> list))
            {
                list.AddRange(items);
            }
            else
            {
                dict[item] = items.ToList();
            }
        }
        public void Add(T item, T value)
        {
            if (dict.TryGetValue(item, out List<T> list))
            {
                list.Add(value);
            }
            else
            {
                dict[item] = new List<T>() { value };
            }
        }
        public void ForEach(Action<T, List<T>> cb)
        {
            foreach (var kv in this.dict) cb(kv.Key, kv.Value);
        }
        public List<T> this[T item]
        {
            get
            {
                dict.TryGetValue(item, out List<T> list);
                return list;
            }
        }
        public IEnumerable<T> Get(T item)
        {
            dict.TryGetValue(item, out List<T> list);
            return list == null ? Enumerable.Empty<T>() : list;
        }
        public Dictionary<T, List<T>>.KeyCollection Keys => dict.Keys;
        public Dictionary<T, List<T>>.ValueCollection Values => dict.Values;
    }
    public class ListDict<K, V>
    {
        public Dictionary<K, int> ToCountDict(bool preserveZero = false)
        {
            var d = new Dictionary<K, int>();
            foreach (var kv in dict)
            {
                var lst = kv.Value.Distinct().ToList();
                if (lst.Count > 0 || (lst.Count == 0 && preserveZero))
                {
                    d[kv.Key] = lst.Count;
                }
            }
            return d;
        }
        Dictionary<K, List<V>> dict = new Dictionary<K, List<V>>();
        public void Add(K item, IEnumerable<V> items)
        {
            if (dict.TryGetValue(item, out List<V> list))
            {
                list.AddRange(items);
            }
            else
            {
                dict[item] = items.ToList();
            }
        }
        public void Add(K item, V value)
        {
            if (dict.TryGetValue(item, out List<V> list))
            {
                list.Add(value);
            }
            else
            {
                dict[item] = new List<V>() { value };
            }
        }
        public void ForEach(Action<K, List<V>> cb)
        {
            foreach (var kv in this.dict) cb(kv.Key, kv.Value);
        }
        public List<V> this[K item]
        {
            get
            {
                dict.TryGetValue(item, out List<V> list);
                return list;
            }
        }
        public IEnumerable<V> Get(K item)
        {
            dict.TryGetValue(item, out List<V> list);
            return list == null ? Enumerable.Empty<V>() : list;
        }
        public Dictionary<K, List<V>>.KeyCollection Keys => dict.Keys;
        public Dictionary<K, List<V>>.ValueCollection Values => dict.Values;
    }
    public class ThGravityService
    {
        public AcadDatabase adb;
        public Point3dCollection CurrentSelectionExtent { get; private set; }
        private List<Entity> _Gravities = null;
        private List<Entity> Gravities
        {
            get
            {
                if (_Gravities == null)
                {
                    var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                    gravityBucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                    _Gravities = gravityBucketEngine.Elements.Select(g => g.Outline).ToList();
                }
                return _Gravities;
            }
        }
        public Func<Point3dCollection, List<Entity>> GetGravityWaterBuckets;
        private ThCADCoreNTSSpatialIndex _AllGravityWaterBucketSpatialIndex = null;
        private ThCADCoreNTSSpatialIndex AllGravityWaterBucketSpatialIndex
        {
            get
            {
                if (_AllGravityWaterBucketSpatialIndex == null)
                    _AllGravityWaterBucketSpatialIndex = new ThCADCoreNTSSpatialIndex(Gravities.ToCollection());
                return _AllGravityWaterBucketSpatialIndex;
            }
        }
        private List<Entity> _Sides = null;
        private List<Entity> Sides
        {
            get
            {
                if (_Sides == null)
                {
                    var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
                    sidebucketEngine.Recognize(adb.Database, CurrentSelectionExtent);
                    _Sides = sidebucketEngine.Elements.Select(e => e.Outline).ToList();
                }
                return _Sides;
            }
        }
        private ThCADCoreNTSSpatialIndex AllSideWaterBucketSpatialIndex
        {
            get
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Sides.ToCollection());
                return spatialIndex;
            }
        }
        private List<Extents3d> _AllSideWaterBucketExtents = null;
        private List<Extents3d> AllSideWaterBucketExtents
        {
            get
            {
                if (_AllSideWaterBucketExtents == null)
                    _AllSideWaterBucketExtents = Sides.Select(g => g.GeometricExtents).ToList();
                return _AllSideWaterBucketExtents;
            }
        }
        public void Init(Point3dCollection baseRange)
        {
            CurrentSelectionExtent = baseRange;
        }
        public List<Extents3d> GetRelatedGravityWaterBucket(Point3dCollection range)
        {
            var rst = new List<Extents3d>();
            var selected = AllGravityWaterBucketSpatialIndex.SelectCrossingPolygon(range);
            foreach (Entity e in selected)
            {
                rst.Add(e.GeometricExtents);
            }
            return rst;
        }
        public Pipe.Model.WaterBucketEnum GetRelatedSideWaterBucket(Point3d centerOfPipe)
        {
            foreach (var e in AllSideWaterBucketExtents)
            {
                if (e.IsPointIn(centerOfPipe))
                {
                    return WaterBucketEnum.Side;
                }
            }
            return Pipe.Model.WaterBucketEnum.None;
        }
    }
    public class ThRainSystemService
    {
        const string ROOF_RAIN_PIPE_PREFIX = "Y1";
        const string BALCONY_PIPE_PREFIX = "Y2";
        const string CONDENSE_PIPE_PREFIX = "NL";
        public AcadDatabase adb;
        public Dictionary<Entity, GRect> BoundaryDict = new Dictionary<Entity, GRect>();
        public Dictionary<Entity, string> VerticalPipeDBTextDict = new Dictionary<Entity, string>();
        public List<Entity> VerticalPipeLines = new List<Entity>();
        public List<DBText> VerticalPipeDBTexts = new List<DBText>();
        public List<Entity> VerticalPipes = new List<Entity>();
        public Dictionary<string, string> VerticalPipeLabelToDNDict = new Dictionary<string, string>();
        public Dictionary<Entity, string> VerticalPipeToLabelDict = new Dictionary<Entity, string>();
        public List<Tuple<Entity, Entity>> ShortConverters = new List<Tuple<Entity, Entity>>();
        public List<Entity> LongConverterLines = new List<Entity>();
        public ListDict<Entity> LongConverterToPipesDict = new ListDict<Entity>();
        public ListDict<Entity> LongConverterToLongConvertersDict = new ListDict<Entity>();
        public List<BlockReference> WrappingPipes = new List<BlockReference>();
        public List<Entity> DraiDomePipes = new List<Entity>();
        public List<Entity> WaterWells = new List<Entity>();
        public Dictionary<Entity, string> WaterWellDNs = new Dictionary<Entity, string>();
        public List<Entity> RainDrain13s = new List<Entity>();
        public List<Entity> ConnectToRainPortSymbols = new List<Entity>();
        public List<DBText> ConnectToRainPortDBTexts = new List<DBText>();
        public List<Entity> WRainLines = new List<Entity>();
        public List<Entity> WRainRealLines = new List<Entity>();
        public Dictionary<Entity, Entity> WRainLinesMapping = new Dictionary<Entity, Entity>();
        public Dictionary<Entity, Entity> ConnectToRainPortSymbolToLongConverterLineDict = new Dictionary<Entity, Entity>();
        public Dictionary<Entity, DBText> ConnectToRainPortSymbolToConnectToRainDrainDBTextDict = new Dictionary<Entity, DBText>();
        public List<Entity> CondensePipes = new List<Entity>();
        private ThCADCoreNTSSpatialIndex DbTextSpatialIndex;
        public IEnumerable<Entity> AllShortConverters
        {
            get
            {
                IEnumerable<Entity> f()
                {
                    foreach (var item in ShortConverters)
                    {
                        yield return item.Item1;
                        yield return item.Item2;
                    }
                }
                return f().Distinct();
            }
        }
        public bool IsCondensePipeLow(Entity cp)
        {
            cpIsLowDict.TryGetValue(cp, out bool r);
            return r;
        }
        bool inited;
        public class Context
        {
            public LS VerticalPipes = new LS();
            public Dictionary<string, SRect> BoundaryDict = new Dictionary<string, SRect>();
            public LS WRainLines = new LS();
            public Dictionary<string, SLine> WRainLinesDict = new Dictionary<string, SLine>();
        }
        public Context GetCurrentContext()
        {
            var c = new Context(); var d = new GuidDict(4096);
            d.AddObjs(VerticalPipes); foreach (var e in VerticalPipes) c.VerticalPipes.Add(d[e]);
            d.AddObjs(BoundaryDict.Keys); foreach (var kv in BoundaryDict) c.BoundaryDict[d[kv.Key]] = kv.Value.ToSRect();
            d.AddObjs(WRainLines);
            foreach (var line in WRainLines) if (GeoAlgorithm.TryConvertToLineSegment(line, out GLineSegment seg)) c.WRainLinesDict[d[line]] = seg.ToSLine();
            return c;
        }
        public bool HasBrokenCondensePipe(Point3dCollection range, string id)
        {
            return FiltByRect(range, brokenCondensePipes.Where(kv => kv.Value == id).Select(kv => kv.Key)).Any();
        }
        HashSet<Entity> wrappingEnts = new HashSet<Entity>();
        public bool HasDrivePipe(Entity e)
        {
            return wrappingEnts.Contains(e);
        }
        public IEnumerable<Entity> EnumerateTianzhengElements()
        {
            return adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x));
        }
        public bool HasShortConverters(Entity ent)
        {
            return AllShortConverters.Contains(ent);
        }
        public bool HasLongConverters(Entity ent)
        {
            var ret = LongConverterPipes.Contains(ent) || LongPipes.Contains(ent);
            return ret;
        }
        public RainOutputTypeEnum GetOutputType(Point3dCollection pts, string pipeId, out bool hasDrivePipe)
        {
            var rt = _GetOutputType(pts, pipeId, out hasDrivePipe);
            return rt;
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToPipesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public TranslatorTypeEnum GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            if (HasGravityLabelConnected(range, verticalPipeID)) return TranslatorTypeEnum.None;
            var ret = _GetTranslatorType(range, verticalPipeID);
            return ret;
        }
        private bool HasGravityConverters(Entity pipe)
        {
            foreach (var g in LongConverterLineToWaterBucketsGroups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => WaterBuckets.Contains(e))) continue;
                if (g.Contains(pipe)) return true;
            }
            return false;
        }
        private TranslatorTypeEnum _GetTranslatorType(Point3dCollection range, string verticalPipeID)
        {
            List<Entity> pipes = GetVerticalPipes(range);
            var pipe = _GetVerticalPipe(pipes, verticalPipeID);
            if (pipe != null)
            {
                if (HasShortConverters(pipe)) return TranslatorTypeEnum.Short;
                if (HasLongConverters(pipe)) return TranslatorTypeEnum.Long;
            }
            foreach (var pp in _GetVerticalPipes(pipes, verticalPipeID))
            {
                if (HasLongConverters(pp)) return TranslatorTypeEnum.Long;
            }
            if (hasLongConverter?.Invoke(range.ToRect(), verticalPipeID) ?? false)
            {
                return TranslatorTypeEnum.Long;
            }
            return TranslatorTypeEnum.None;
        }
        public static void SortByY(ref Point2d pt1, ref Point2d pt2)
        {
            if (pt1.Y > pt2.Y)
            {
                var tmp = pt1;
                pt1 = pt2;
                pt2 = tmp;
            }
        }
        public static Point2d GetTargetPoint(GLineSegment line1, GLineSegment line2)
        {
            var y1 = line1.Center.Y;
            var y2 = line2.Center.Y;
            var pt1 = line2.StartPoint;
            var pt2 = line2.EndPoint;
            SortByY(ref pt1, ref pt2);
            if (y1 > y2)
            {
                return pt1;
            }
            else if (y1 < y2)
            {
                return pt2;
            }
            else
            {
                var xArr = new double[] { line1.StartPoint.X, line1.EndPoint.X, line2.StartPoint.X, line2.EndPoint.X };
                var minx = xArr.Min();
                var maxx = xArr.Max();
                if (line1.Center.X < line2.Center.X)
                {
                    return new Point2d(maxx, y1);
                }
                else
                {
                    return new Point2d(minx, y1);
                }
            }
        }
        public static void TempPatch(AcadDatabase adb, ThRainSystemService sv)
        {
            var txts = new List<Entity>();
            foreach (var ent in sv.EnumerateTianzhengElements().ToList())
            {
                var lst = ent.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                if (lst.Count == 1)
                {
                    var e = lst.First();
                    txts.Add(e);
                }
            }
            var pipes = new List<Entity>();
            foreach (var ent in sv.EnumerateTianzhengElements().ToList())
            {
                if (ent.Layer == "W-RAIN-EQPM" || ent.Layer == "WP_KTN_LG")
                {
                    var lst = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    if (lst.Count == 1)
                    {
                        var e = lst.First();
                        pipes.Add(ent);
                    }
                    else
                    {
                        pipes.Add(ent);
                    }
                }
            }
            {
                var ps = sv.VerticalPipeToLabelDict.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
                pipes = pipes.Except(ps).ToList();
            }
            var lines = adb.ModelSpace.OfType<Line>().Where(x => x.Length > 0 && x.Layer == "W-RAIN-NOTE").Cast<Entity>().ToList();
            var d = new Dictionary<Entity, GRect>();
            foreach (var e in pipes.Concat(lines).Concat(txts).Distinct())
            {
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            var gs = ThRainSystemService.GroupLines(lines);
            foreach (var g in gs)
            {
                string lb = null;
                foreach (Line e in g)
                {
                    var s = e.ToGLineSegment();
                    if (s.IsHorizontal(10))
                    {
                        foreach (var t in txts)
                        {
                            var bd = d[t];
                            if (bd.CenterY > d[e].Center.Y)
                            {
                                if (GeoAlgorithm.Distance(bd.Center, d[e].Center) < 500)
                                {
                                    lb = ((DBText)t).TextString;
                                    goto xx;
                                }
                            }
                        }
                    }
                }
                xx:
                if (lb != null)
                {
                    var pts = new List<Point2d>(8);
                    foreach (Line line in g)
                    {
                        var s = line.ToGLineSegment();
                        pts.Add(s.StartPoint);
                        pts.Add(s.EndPoint);
                    }
                    foreach (var p in pipes)
                    {
                        var bd = d[p];
                        foreach (var pt in pts)
                        {
                            if (bd.ContainsPoint(pt))
                            {
                                if (!sv.VerticalPipeToLabelDict.ContainsKey(p))
                                {
                                    sv.VerticalPipeToLabelDict[p] = lb;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        public static void Triangle<T>(IList<T> lst, Action<T, T> cb)
        {
            for (int i = 0; i < lst.Count; i++)
            {
                for (int j = i + 1; j < lst.Count; j++)
                {
                    cb(lst[i], lst[j]);
                }
            }
        }
        public static void Triangle(int count, Action<int, int> cb)
        {
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    cb(i, j);
                }
            }
        }
        public static List<List<int>> ConnectLines(IList<GLineSegment> segs, double tollerance)
        {
            var linesGroup = new List<List<int>>();
            var pairs = new List<KeyValuePair<int, int>>();
            Triangle(segs.Count, (i, j) =>
            {
                if (GeoAlgorithm.YieldPoints(segs[i], segs[j]).Any(kv => kv.Key.GetDistanceTo(kv.Value) <= tollerance))
                {
                    pairs.Add(new KeyValuePair<int, int>(i, j));
                }
            });
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = segs.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.ToList());
            });
            return linesGroup;
        }
        public List<Entity> GetVerticalPipes(Point3dCollection range)
        {
            if (!RangeToPipesDict.TryGetValue(range, out List<Entity> pipes))
            {
                pipes = _GetVerticalPipes(range);
                RangeToPipesDict[range] = pipes;
            }
            return pipes;
        }
        private IEnumerable<Entity> _GetVerticalPipes(List<Entity> pipes, string id)
        {
            return pipes.Where(p =>
            {
                VerticalPipeToLabelDict.TryGetValue(p, out string lb); return lb == id;
            });
        }
        private Entity _GetVerticalPipe(List<Entity> pipes, string id)
        {
            return pipes.FirstOrDefault(p =>
            {
                VerticalPipeToLabelDict.TryGetValue(p, out string lb); return lb == id;
            });
        }
        ThCADCoreNTSSpatialIndex _verticalPipesSpatialIndex;
        private List<Entity> _GetVerticalPipes(Point3dCollection pts)
        {
            _verticalPipesSpatialIndex ??= BuildSpatialIndex(VerticalPipes);
            return _verticalPipesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        private RainOutputTypeEnum _GetOutputType(Point3dCollection pts, string pipeId, out bool hasDrivePipe)
        {
            if (IsRainPort(pipeId))
            {
                hasDrivePipe = false;
                return RainOutputTypeEnum.RainPort;
            }
            if (IsWaterWell(pipeId))
            {
                hasDrivePipe = HasOutputDrivePipeForWaterWell(pts, pipeId);
                return RainOutputTypeEnum.WaterWell;
            }
            {
                hasDrivePipe = false;
                return RainOutputTypeEnum.None;
            }
        }
        Dictionary<Point3dCollection, List<Entity>> WrappingPipesRangeDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWrappingPipes(Point3dCollection range)
        {
            if (!WrappingPipesRangeDict.TryGetValue(range, out List<Entity> ret))
            {
                ret = FiltEntityByRange(range.ToRect(), WrappingPipes).ToList();
                WrappingPipesRangeDict[range] = ret;
            }
            return ret;
        }
        public bool HasOutputDrivePipeForWaterWell(Point3dCollection range, string pipeId)
        {
            return GetWrappingPipes(range).Any(e => GetLabel(e) == pipeId);
        }
        public static void ConnectLabelToLabelLine(RainSystemGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                {
                    var e = DU.DrawRectLazy(g);
                    e.ColorIndex = 2;
                }
                var _lineHGs = f1(g.ToPolygon());
                var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                if (geo == null) continue;
                {
                    var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(100, 400) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(.1, 400))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(.1));
                    }
                }
            }
        }
        public static void PreFixGeoData(RainSystemGeoData geoData, double labelHeight)
        {
            if (labelHeight > 0)
            {
                foreach (var x in geoData.Labels)
                {
                    if (RainSystemService.IsWantedText(x.Text))
                    {
                        geoData.LabelLines.Add(new GLineSegment(x.Boundary.Center, x.Boundary.Center.OffsetY(-labelHeight)));
                    }
                }
            }
            for (int i = 0; i < geoData.WaterWells.Count; i++)
            {
                geoData.WaterWells[i] = geoData.WaterWells[i].Expand(60);
            }
            for (int i = 0; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(10);
            }
            for (int i = 0; i < geoData.SideWaterBuckets.Count; i++)
            {
                geoData.SideWaterBuckets[i] = geoData.SideWaterBuckets[i].Expand(100);
            }
            for (int i = 0; i < geoData.GravityWaterBuckets.Count; i++)
            {
                geoData.GravityWaterBuckets[i] = geoData.GravityWaterBuckets[i].Expand(100);
            }
        }
        public static void AppendSideWaterBuckets(AcadDatabase adb, Point3dCollection range, RainSystemGeoData geoData)
        {
            var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
            sidebucketEngine.Recognize(adb.Database, range);
            geoData.SideWaterBuckets.AddRange(sidebucketEngine.Elements.Select(e => e.Outline.Bounds.ToGRect()).Where(r => r.IsValid));
        }
        public static void AppendGravityWaterBuckets(AcadDatabase adb, Point3dCollection range, RainSystemGeoData geoData)
        {
            var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
            gravityBucketEngine.Recognize(adb.Database, range);
            geoData.GravityWaterBuckets.AddRange(gravityBucketEngine.Elements.Select(e => e.Outline.Bounds.ToGRect()).Where(r => r.IsValid));
        }
        public class ThRainSystemServiceGeoCollector
        {
            public AcadDatabase adb;
            public List<Entity> entities;
            public RainSystemGeoData geoData;
            List<GLineSegment> labelLines => geoData.LabelLines;
            List<CText> cts => geoData.Labels;
            List<GRect> pipes => geoData.VerticalPipes;
            List<GRect> storeys => geoData.Storeys;
            List<GLineSegment> wLines => geoData.WLines;
            List<GLineSegment> wLinesAddition => geoData.WLinesAddition;
            List<GRect> condensePipes => geoData.CondensePipes;
            List<GRect> floorDrains => geoData.FloorDrains;
            List<GRect> waterWells => geoData.WaterWells;
            List<string> waterWellLabels => geoData.WaterWellLabels;
            List<GRect> waterPortSymbols => geoData.WaterPortSymbols;
            List<GRect> waterPort13s => geoData.WaterPort13s;
            List<GRect> wrappingPipes => geoData.WrappingPipes;
            public void CollectEntities()
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
                                if (br.GetEffectiveName()=="11" || (r.Width > 20000 && r.Width < 80000 && r.Height > 5000))
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
                this.entities = entities;
            }
            public void CollectLabelLines()
            {
                foreach (var e in entities.OfType<Line>()
                .Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS" || e.Layer == "W-FRPT-NOTE")
                && e.Length > 0))
                {
                    labelLines.Add(e.ToGLineSegment());
                }
            }
            public void CollectCTexts()
            {
                foreach (var e in entities.OfType<DBText>()
                .Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS" || e.Layer == "W-FRPT-NOTE")))
                {
                    cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                }
                foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e)))
                {
                    foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                    {
                        var ct = new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() };
                        if (!ct.Boundary.IsValid)
                        {
                            var p = e.Position.ToPoint2d();
                            var h = e.Height;
                            var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                            var r = new GRect(p, p.OffsetXY(w, h));
                            ct.Boundary = r;
                        }
                        cts.Add(ct);
                    }
                }
            }
            bool sig1;
            int distinguishDiameter = 35;
            public void CollectVerticalPipes()
            {
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(x => x.Layer == "W-RAIN-EQPM")
                    .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == "带定位立管"));
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                        var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                        if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                        return GeoAlgorithm.GetBoundaryRect(ent);
                    }
                    foreach (var pp in pps)
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                    if (pps.Count > 0)
                    {
                        sig1 = true;
                    }
                }
                {
                    var pps = new List<Circle>();
                    pps.AddRange(entities.OfType<Circle>()
                    .Where(x => x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM" || x.Layer == "W-RAIN-DIMS")
                    .Where(c => distinguishDiameter <= c.Radius && c.Radius <= 100));
                    static GRect getRealBoundaryForPipe(Circle c)
                    {
                        return c.Bounds.ToGRect();
                    }
                    foreach (var pp in pps.Distinct())
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities
                    .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                    && ThRainSystemService.IsTianZhengElement(x))
                    .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                    );
                    static GRect getRealBoundaryForPipe(Entity ent)
                    {
                        return ent.Bounds.ToGRect(50);
                    }
                    foreach (var pp in pps.Distinct())
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
                {
                    Util1.CollectTianzhengVerticalPipes(labelLines, cts, entities);
                }
                {
                    var pps = new List<Entity>();
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(x => x.ObjectId.IsValid ? x.Layer == "W-RAIN-EQPM" && x.GetBlockEffectiveName() == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                    );
                    pps.AddRange(entities.OfType<BlockReference>()
                    .Where(e =>
                    {
                        return e.ObjectId.IsValid && (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE")
    && !e.GetBlockEffectiveName().Contains("井");
                    }));
                    foreach (var pp in pps)
                    {
                        pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                    }
                }
            }
            public void CollectWLines()
            {
                if (true)
                {
                    if (false) NewMethod();
                    {
                        foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                        {
                            if (e is Line line && line.Length > 0)
                            {
                                wLines.Add(line.ToGLineSegment());
                            }
                            else if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                                {
                                    if (seg.Length > 0)
                                    {
                                        var lst = e.ExplodeToDBObjectCollection().OfType<Line>().Where(ln => ln.Length > 0).ToList();
                                        if (lst.Count == 1)
                                        {
                                            wLines.Add(lst[0].ToGLineSegment());
                                        }
                                        else if (lst.Count > 1)
                                        {
                                            wLines.Add(lst[0].ToGLineSegment());
                                            Point3d p1 = default, p2 = default;
                                            var tmp = new List<GLineSegment>();
                                            for (int i = 1; i < lst.Count; i++)
                                            {
                                                wLines.Add(lst[i].ToGLineSegment());
                                                p1 = lst[i - 1].EndPoint;
                                                p2 = lst[i].StartPoint;
                                                var sg = new GLineSegment(p1, p2);
                                                if (sg.Length > 0)
                                                    tmp.Add(sg);
                                            }
                                            wLinesAddition.AddRange(tmp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    wLines.AddRange(Util1.GetWRainLines(entities));
                }
            }
            private void NewMethod()
            {
                foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                {
                    if (e is Line line && line.Length > 0)
                    {
                        wLines.Add(line.ToGLineSegment());
                    }
                    else if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            if (ln.Length > 0)
                            {
                                wLines.Add(ln.ToGLineSegment());
                            }
                        }
                    }
                }
            }
            public void CollectCondensePipes()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<Circle>()
                .Where(c => c.Layer == "W-RAIN-EQPM")
                .Where(c => 20 < c.Radius && c.Radius < distinguishDiameter));
                condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            }
            public void CollectFloorDrains()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName().Contains("地漏")));
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                floorDrains.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWaterWells()
            {
                var ents = new List<BlockReference>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                waterWellLabels.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
            }
            public void CollectWaterPortSymbols()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                ents.AddRange(entities.Where(e => FengDbgTesting.IsTianZhengRainPort(e)));
                waterPortSymbols.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWaterPort13s()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName().Contains("雨水口")));
                waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            }
            public void CollectWrappingPipes()
            {
                var ents = new List<Entity>();
                ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetBlockEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
            }
            public void CollectStoreys(Point3dCollection range)
            {
                var storeysRecEngine = new ThStoreysRecognitionEngine();
                storeysRecEngine.Recognize(adb.Database, range);
                foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                {
                    var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                    storeys.Add(bd);
                }
            }
        }
        public static List<ThStoreysData> GetStoreys(Geometry range, AcadDatabase adb, ThDrainageService.CommandContext ctx)
        {
            return ctx.StoreyContext.thStoreysDatas;
        }
        public static List<ThStoreysData> GetStoreys(Geometry range, AcadDatabase adb, ThRainService.CommandContext ctx)
        {
            return ctx.StoreyContext.thStoreysDatas;
        }
        public static List<ThStoreysData> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            foreach (var s in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
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
            return storeys;
        }

        public static void FixStoreys(List<ThStoreysData> storeys)
        {
            var lst1 = storeys.Where(s => s.Storeys.Count == 1).Select(s => s.Storeys[0]).ToList();
            foreach (var s in storeys.Where(s => s.Storeys.Count > 1).ToList())
            {
                var hs = new HashSet<int>(s.Storeys);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Storeys.Clear();
                s.Storeys.AddRange(hs.OrderBy(i => i));
            }
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
            public Diagram.ViewModel.RainSystemDiagramViewModel rainSystemDiagramViewModel;
            public System.Windows.Window window;
        }
        public static CommandContext commandContext;
        public static void InitFloorListDatas()
        {
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
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
            .Where(o => o.GetBlockEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
            .Select(o => o.ObjectId)
            .ToObjectIdCollection();
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            commandContext.rainSystemDiagramViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
        }
        public static void DrawRainSystemDiagram1()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();
                using (var adb = AcadDatabase.Active())
                {
                    IEnumerable<Entity> GetEntities()
                    {
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
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
                    }
                    var entities = GetEntities().ToList();
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.GetBlockEffectiveName().Contains("地漏")));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetBlockEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                    }
                    {
                        foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }
                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }
                    {
                        var pps = new List<Entity>();
                        var blockNameOfVerticalPipe = "带定位立管";
                        pps.AddRange(entities.OfType<BlockReference>()
                        .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                        .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == blockNameOfVerticalPipe));
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                            var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                            if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                            return GeoAlgorithm.GetBoundaryRect(ent);
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }
                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }
                    {
                        foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                        {
                            if (e is Line line && line.Length > 0)
                            {
                                wLines.Add(line.ToGLineSegment());
                            }
                            else if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                                {
                                    if (ln.Length > 0)
                                    {
                                        wLines.Add(ln.ToGLineSegment());
                                    }
                                }
                            }
                        }
                    }
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                geoData.FixData();
                return geoData;
            }
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var range = Dbg.SelectRange();
                    var basePt = Dbg.SelectPoint();
                    ThRainSystemService.ImportElementsFromStdDwg();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = getGeoData(range);
                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, 150);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                    DU.Dispose();
                    sv.RainSystemDiagram.Draw(basePt);
                    DU.Draw(adb);
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }
        public static void DrawRainSystemDiagram2()
        {
            if (commandContext != null) return;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = new RainSystemGeoData();
                    geoData.Init();
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);
                    var labelHeight = -1;
                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    ThRainSystemService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                    DU.Dispose();
                    sv.RainSystemDiagram.Draw(basePt);
                    DU.Draw(adb);
                    Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }
        public static void DrawRainSystemDiagram3()
        {
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            DU.Dispose();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.range == null) return;
            if (commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var range = commandContext.range;
                    var storeys = commandContext.StoreyContext.thStoreysDatas;
                    var geoData = new RainSystemGeoData();
                    geoData.Init();
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);
                    var labelHeight = -1;
                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    ThRainSystemService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                    DU.Dispose();
                    sv.RainSystemDiagram.Draw(basePt);
                    DU.Draw(adb);
                    Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }
        public static bool IsIntersects(Polyline p1, Polyline p2)
        {
            return new ThCADCore.NTS.ThCADCoreNTSRelate(p1.MinimumBoundingBox(), p2.MinimumBoundingBox()).IsIntersects;
        }
        public Polyline CreatePolygon(Entity e, int num = 4, double expand = 0)
        {
            var bd = BoundaryDict[e];
            var pl = PolylineTools.CreatePolygon(bd.Center, num, bd.Radius + expand);
            return pl;
        }
        const double tol = 800;
        public static Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint - lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint + lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint + lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint - lineDir * tol - moveDir * distance;
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
        public ThGravityService thGravityService;
        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }
        private static bool IsTianZhengElement(Type type)
        {
            return type.IsNotPublic && type.Name.StartsWith("Imp") && type.Namespace == "Autodesk.AutoCAD.DatabaseServices";
        }
        public List<Entity> TianZhengEntities = new List<Entity>();
        public List<Entity> SingleTianzhengElements = new List<Entity>();
        public void CollectTianZhengEntities()
        {
            TianZhengEntities.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x)));
        }
        public void ExplodeSingleTianZhengElements()
        {
            foreach (var e in TianZhengEntities)
            {
                var colle = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                if (colle.Count == 1)
                {
                    SingleTianzhengElements.Add(colle[0]);
                }
            }
        }
        public List<Entity> ExplodedEntities = new List<Entity>();
        public List<Entity> vps = new List<Entity>();
        public List<DBText> txts = new List<DBText>();
        public void CollectExplodedEntities()
        {
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 10000 && r.Width < 60000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is BlockReference br2)
                        {
                            if (br2.Name == "*U398")
                            {
                                vps.Add(br2);
                            }
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e))
                        {
                            var lst = e.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                            foreach (var t in lst)
                            {
                                txts.Add(t);
                            }
                        }
                    }
                }
            }
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 1000 && r.Width < 60000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is DBText t && t.Layer == "W-RAIN-NOTE")
                        {
                            txts.Add(t);
                        }
                        else if (e is Circle c && e.Layer == "W-RAIN-EQPM")
                        {
                            vps.Add(e);
                        }
                    }
                }
            }
            foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
            {
                var r = GeoAlgorithm.GetBoundaryRect(br);
                if (r.Width > 10000 && r.Width < 100000)
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                    {
                        if (e is Circle && e.Layer == "W-RAIN-EQPM")
                        {
                            vps.Add(e);
                        }
                        else if (e is DBText t)
                        {
                            txts.Add(t);
                        }
                    }
                }
            }
            foreach (var br1 in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "C-SHET-SHET"))
            {
                foreach (var br2 in br1.ExplodeToDBObjectCollection().OfType<BlockReference>())
                {
                    foreach (var e in br2.ExplodeToDBObjectCollection().Cast<Entity>())
                    {
                        if (e is DBText t)
                        {
                            txts.Add(t);
                        }
                        else if (e is Circle)
                        {
                            vps.Add(e);
                        }
                    }
                }
            }
        }
        public static bool ImportElementsFromStdDwg()
        {
            var file = ThCADCommon.WSSDwgPath();
            if (!File.Exists(file))
            {
                MessageBox.Show($"\"{file}\"不存在");
                return false;
            }
            {
                using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase adb = AcadDatabase.Active())
                using (AcadDatabase blockDb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                {
                    var fs = new Dictionary<Action, string>();
                    {
                        var blocks = blockDb.Blocks.Select(x => x.Name).ToList();
                        foreach (var blk in blocks)
                        {
                            fs.Add(() => adb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blk)), blk);
                        }
                    }
                    {
                        blockDb.DimStyles.ForEach(x => adb.DimStyles.Import(x));
                        foreach (var txtStyle in blockDb.TextStyles)
                        {
                            adb.TextStyles.Import(txtStyle);
                        }
                    }
                    {
                        var layers = blockDb.Layers.Select(x => x.Name).ToList();
                        foreach (var layer in layers)
                        {
                            fs.Add(() => adb.Layers.Import(blockDb.Layers.ElementOrDefault(layer)), layer);
                        }
                    }
                    foreach (var kv in fs)
                    {
                        try
                        {
                            kv.Key();
                        }
                        catch (System.Exception ex)
                        {
                            Dbg.PrintText(kv.Value + "导入失败," + ex.Message);
                        }
                    }
                }
            }
            return true;
        }
        public List<Entity> SideWaterBuckets = new List<Entity>();
        public void InitCache()
        {
            if (inited) return;
            CollectTianZhengEntities();
            ExplodeSingleTianZhengElements();
            CollectExplodedEntities();
            CollectVerticalPipeLines();
            CollectVerticalPipeDBTexts();
            CollectVerticalPipes();
            CollectLongConverterLines();
            CollectDraiDomePipes();
            CollectWrappingPipes();
            CollectWaterWells();
            CollectWaterWell13s();
            CollectConnectToRainPortDBTexts();
            CollectConnectToRainPortSymbols();
            CollectWRainLines();
            CollectCondensePipes();
            CollectFloorDrains();
            CollectSideWaterBuckets();
            inited = true;
        }
        public void CollectSideWaterBuckets()
        {
            SideWaterBuckets.AddRange(EnumerateEntities<BlockReference>().Where(x => x.Name == "CYSD" || x.GetBlockEffectiveName() == "CYSD"));
        }
        public List<Entity> FloorDrains = new List<Entity>();
        public void CollectFloorDrains()
        {
            IEnumerable<Entity> q;
            {
                string strFloorDrain = "地漏";
                q = adb.ModelSpace.OfType<BlockReference>()
                .Where(e => e.ObjectId.IsValid)
                .Where(x =>
                {
                    if (x.IsDynamicBlock)
                    {
                        return x.ObjectId.GetDynBlockValue("可见性")?.Contains(strFloorDrain) ?? false;
                    }
                    else
                    {
                        return x.GetBlockEffectiveName().Contains(strFloorDrain);
                    }
                }
                );
            }
            {
                static bool IsFloorDrawin(BlockReference br)
                {
                    return br.Name == "*U400";
                }
                var lst = new List<Entity>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().OfType<BlockReference>().ToList())
                        {
                            if (IsFloorDrawin(e))
                            {
                                lst.Add(e);
                            }
                        }
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    if (IsFloorDrawin(e))
                    {
                        lst.Add(e);
                    }
                }
                q = q.Concat(lst);
            }
            FloorDrains.AddRange(q.Distinct());
            static GRect getRealBoundary(Entity ent)
            {
                var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                var et = ents.FirstOrDefault(e =>
                {
                    var m = Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width);
                    return m == 120;
                });
                if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                return GeoAlgorithm.GetBoundaryRect(ent);
            }
            foreach (var e in FloorDrains)
            {
                BoundaryDict[e] = getRealBoundary(e);
            }
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToWRainLinesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWRainLines(Point3dCollection range)
        {
            if (!RangeToWRainLinesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetWRainLines(range);
                RangeToWRainLinesDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _WRainLinessSpatialIndex;
        private List<Entity> _GetWRainLines(Point3dCollection pts)
        {
            _WRainLinessSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(WRainLines);
            return _WRainLinessSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToCondensePipesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetCondensePipes(Point3dCollection range)
        {
            if (!RangeToCondensePipesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetCondensePipes(range);
                RangeToCondensePipesDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _CondensePipesSpatialIndex;
        private List<Entity> _GetCondensePipes(Point3dCollection pts)
        {
            _CondensePipesSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(CondensePipes);
            return _CondensePipesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToFloorDrainsDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetFloorDrains(Point3dCollection range)
        {
            if (!RangeToFloorDrainsDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetFloorDrains(range);
                RangeToFloorDrainsDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _FloorDrainsSpatialIndex;
        private List<Entity> _GetFloorDrains(Point3dCollection pts)
        {
            _FloorDrainsSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(FloorDrains);
            return _FloorDrainsSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        public void CollectCondensePipes()
        {
            CondensePipes.AddRange(adb.ModelSpace.OfType<Circle>().Where(e => e.Layer == "W-RAIN-EQPM"));
            foreach (var e in CondensePipes)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToLongConverterLinesDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetLongConverterLines(Point3dCollection range)
        {
            if (!RangeToLongConverterLinesDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetLongConverterLines(range);
                RangeToLongConverterLinesDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _LongConverterLinesSpatialIndex;
        private List<Entity> _GetLongConverterLines(Point3dCollection pts)
        {
            _LongConverterLinesSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(LongConverterLines);
            return _LongConverterLinesSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        Dictionary<Point3dCollection, List<Entity>> RangeToLWaterWellsDict = new Dictionary<Point3dCollection, List<Entity>>();
        public List<Entity> GetWaterWells(Point3dCollection range)
        {
            if (!RangeToLWaterWellsDict.TryGetValue(range, out List<Entity> ents))
            {
                ents = _GetWaterWells(range);
                RangeToLWaterWellsDict[range] = ents;
            }
            return ents;
        }
        ThCADCoreNTSSpatialIndex _WaterWellSpatialIndex;
        private List<Entity> _GetWaterWells(Point3dCollection pts)
        {
            _WaterWellSpatialIndex ??= ThRainSystemService.BuildSpatialIndex(WaterWells);
            return _WaterWellSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }
        public void CollectWRainLines()
        {
            WRainRealLines.AddRange(adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE"));
            foreach (var e in WRainRealLines)
            {
                if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                {
                    var line = new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() };
                    WRainLines.Add(line);
                    WRainLinesMapping[e] = line;
                    BoundaryDict[line] = new GRect(seg.StartPoint, seg.EndPoint);
                }
            }
        }
        public static Func<Polyline, List<Entity>> BuildSpatialIndexLazy(IList<Entity> ents)
        {
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(ents.ToCollection());
            List<Entity> f(Polyline pline)
            {
                return si.SelectCrossingPolygon(pline).Cast<Entity>().ToList();
            }
            return f;
        }
        public class EntitiesCollector
        {
            public List<Entity> Entities = new List<Entity>();
            public EntitiesCollector Add<T>(IEnumerable<T> ents) where T : Entity
            {
                Entities.AddRange(ents);
                return this;
            }
        }
        public static EntitiesCollector CollectEnts() => new EntitiesCollector();
        public static ThCADCoreNTSSpatialIndex BuildSpatialIndex<T>(IList<T> ents) where T : Entity
        {
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(ents.ToCollection());
            return si;
        }
        public void BuildConnectToRainDrainSymbolToConnectToRainDrainDBTextDict()
        {
            foreach (var ent in ConnectToRainPortSymbols)
            {
                var r = BoundaryDict[ent];
                foreach (var e1 in VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], r)))
                {
                    foreach (var e2 in VerticalPipeLines.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], BoundaryDict[e1])))
                    {
                        if (e2 != e1)
                        {
                            foreach (var e3 in ConnectToRainPortDBTexts.Where(e => GeoAlgorithm.IsRectCross(BoundaryDict[e], BoundaryDict[e2].Expand(200))))
                            {
                                ConnectToRainPortSymbolToConnectToRainDrainDBTextDict[ent] = e3;
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void CollectConnectToRainPortDBTexts()
        {
            ConnectToRainPortDBTexts.AddRange(adb.ModelSpace.OfType<DBText>().Where(x => x.TextString == "接至雨水口"));
            foreach (var e in ConnectToRainPortDBTexts)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectConnectToRainPortSymbols()
        {
            IEnumerable<Entity> q = adb.ModelSpace.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS");
            q = q.Concat(adb.ModelSpace.OfType<Entity>().Where(x => IsTianZhengElement(x)).Where(x =>
            {
                return x.ExplodeToDBObjectCollection().OfType<BlockReference>().Any(x => x.Name == "$TwtSys$00000132");
            }));
            ConnectToRainPortSymbols.AddRange(q.Distinct());
            foreach (var e in ConnectToRainPortSymbols)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWell13s()
        {
            RainDrain13s.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetBlockEffectiveName() == "13#雨水口"));
            foreach (var e in RainDrain13s)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWaterWells()
        {
            WaterWells.AddRange(adb.ModelSpace.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
            WaterWells.ForEach(e => WaterWellDNs[e] = (e as BlockReference)?.GetAttributesStrValue("-") ?? "");
            foreach (var e in WaterWells)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public List<MText> AhTexts = new List<MText>();
        public void CollectAhTexts(Point3dCollection range)
        {
            var engine = new ThMEPWSS.Engine.ThAHMarkRecognitionEngine();
            engine.Recognize(adb.Database, range);
            AhTexts.AddRange(engine.Texts.OfType<MText>());
            foreach (var e in AhTexts)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void FixVPipes()
        {
            var d = new Dictionary<Entity, GRect>();
            var ld = new Dictionary<Entity, string>();
            var txts = new List<Entity>();
            var lines = new List<Entity>();
            var pipes = new List<Entity>();
            foreach (var e in adb.ModelSpace.OfType<Circle>().Where(c => Convert.ToInt32(c.Radius) == 50).ToList())
            {
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-EQPM").Where(x => ThRainSystemService.IsTianZhengElement(x)))
            {
                pipes.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            foreach (var e in adb.ModelSpace.OfType<DBText>().ToList())
            {
                if (ThRainSystemService.IsWantedLabelText(e.TextString))
                {
                    txts.Add(e);
                    d[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
            }
            foreach (var e in adb.ModelSpace.OfType<Line>().Where(line => line.Length > 0).ToList())
            {
                lines.Add(e);
                d[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
            var gs = ThRainSystemService.GroupLines(lines);
            foreach (var g in gs)
            {
                void f()
                {
                    foreach (var line in g.OfType<Line>())
                    {
                        var seg = line.ToGLineSegment();
                        if (seg.IsHorizontal(10))
                        {
                            foreach (var t in txts.OfType<DBText>())
                            {
                                var bd = d[t];
                                var dt = bd.CenterY - seg.StartPoint.Y;
                                if (dt > 0 && dt < 250)
                                {
                                    var x1 = Math.Min(seg.StartPoint.X, seg.EndPoint.X);
                                    var x2 = Math.Max(seg.StartPoint.X, seg.EndPoint.X);
                                    if (x1 < bd.CenterX && x2 > bd.CenterX)
                                    {
                                        var pts = g.OfType<Line>().SelectMany(line => new Point2d[] { line.StartPoint.ToPoint2d(), line.EndPoint.ToPoint2d() }).ToList();
                                        foreach (var p in pipes)
                                        {
                                            foreach (var pt in pts)
                                            {
                                                if (d[p].ContainsPoint(pt))
                                                {
                                                    var lb = t.TextString;
                                                    ld[p] = lb;
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                f();
            }
            foreach (var pipe in pipes)
            {
                ld.TryGetValue(pipe, out string lb);
                if (lb != null)
                {
                    VerticalPipeToLabelDict[pipe] = lb;
                }
            }
            foreach (var pipe in pipes)
            {
                if (!VerticalPipes.Contains(pipe)) VerticalPipes.Add(pipe);
            }
            foreach (var txt in txts.OfType<DBText>())
            {
                if (!VerticalPipeDBTexts.Contains(txt)) VerticalPipeDBTexts.Add(txt);
            }
            foreach (var line in lines)
            {
                if (!VerticalPipeLines.Contains(line)) VerticalPipeLines.Add(line);
            }
            foreach (var e in pipes.Concat(txts).Concat(lines))
            {
                BoundaryDict[e] = d[e];
            }
            var longPipes = new List<Entity>();
            var lines2 = new List<Entity>();
            string getLabel(Entity e)
            {
                ld.TryGetValue(e, out string v);
                return v;
            }
            foreach (var line in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-PIPE").Where(x => ThRainSystemService.IsTianZhengElement(x)))
            {
                if (GeoAlgorithm.TryConvertToLineSegment(line, out GLineSegment seg))
                {
                    var pts = new Point2d[] { seg.StartPoint, seg.EndPoint };
                    var ps = pipes.Where(pipe => pts.Any(pt => d[pipe].ContainsPoint(pt)));
                    var pp1 = ps.FirstOrDefault(p => getLabel(p) != null);
                    var pp2 = ps.FirstOrDefault(p => getLabel(p) == null);
                    if (pp1 != null && pp2 != null)
                    {
                        longPipes.Add(pp1);
                        longPipes.Add(pp2);
                        if (!VerticalPipes.Contains(pp1)) VerticalPipes.Add(pp1);
                        if (!VerticalPipes.Contains(pp2)) VerticalPipes.Add(pp2);
                        VerticalPipeToLabelDict[pp2] = getLabel(pp1);
                    }
                }
            }
            foreach (var pp in longPipes)
            {
                LongPipes.Add(pp);
            }
            IEnumerable<Entity> getLongPipes(GRect range)
            {
                foreach (var pp in longPipes)
                {
                    if (range.ContainsRect(d[pp]))
                    {
                        yield return pp;
                    }
                }
            }
            IEnumerable<Entity> getPipes(GRect range)
            {
                foreach (var pp in pipes)
                {
                    if (range.ContainsRect(d[pp]))
                    {
                        yield return pp;
                    }
                }
            }
            bool hasLongConverter(GRect range, string lb)
            {
                return getLongPipes(range).Any(pp => getLabel(pp) == lb);
            }
            GRect getVPipeBoundary(GRect range, string lb)
            {
                var pp = getPipes(range).FirstOrDefault(pp => getLabel(pp) == lb);
                if (pp != null) return d[pp];
                return default;
            }
            this.getVPipeBoundary = getVPipeBoundary;
            getDbTexts = r =>
            {
                var ret = getPipes(r).Select(pp => GetLabel(pp)).Where(lb => lb != null).Distinct().ToList();
                return ret;
            };
            this.hasLongConverter = hasLongConverter;
        }
        Func<GRect, string, GRect> getVPipeBoundary;
        Func<GRect, string, bool> hasLongConverter;
        public List<Entity> LongPipes = new List<Entity>();
        public void CollectData()
        {
            InitCache();
            CollectVerticalPipesData();
            FindShortConverters();
            LabelEnts();
            FindOutBrokenCondensePipes();
            CalcCondensePipeIsLow();
            TempPatch(adb, this);
            FixVPipes();
        }
        Dictionary<Entity, bool> cpIsLowDict = new Dictionary<Entity, bool>();
        public void CalcCondensePipeIsLow()
        {
            var sv = this;
            var si = ThRainSystemService.BuildSpatialIndex(sv.AhTexts);
            foreach (var cp in sv.CondensePipes)
            {
                var isLow = false;
                var center = sv.BoundaryDict[cp].Center.ToPoint3d();
                var r = center.Expand(1000).ToGRect();
                var pl = r.ToCadPolyline();
                var ahs = si.SelectCrossingPolygon(pl).Cast<Entity>().ToList();
                if (ahs.Count > 0)
                {
                    var si2 = ThRainSystemService.BuildSpatialIndex(ahs);
                    var ah = si2.NearestNeighbours(center.Expand(.1).ToGRect().ToCadPolyline(), 1).Cast<Entity>().FirstOrDefault();
                    if (ah != null)
                    {
                        if (ah is MText mt)
                        {
                            if (mt.Contents.ToLower() == "ah1")
                            {
                                isLow = true;
                            }
                        }
                    }
                }
                cpIsLowDict[cp] = isLow;
            }
        }
        public List<KeyValuePair<Entity, Entity>> LongConverterLineToWaterBuckets = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LongConverterLineToWaterBucketsGroups;
        public void CollectData(Point3dCollection range)
        {
            CollectData();
            InitThGravityService(range);
            CollectLongConverterLineToWaterBucketsData();
            CollectAhTexts(range);
        }
        private void CollectLongConverterLineToWaterBucketsData()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.LongConverterLines);
            totalList.AddRange(sv.WaterBuckets);
            totalList.AddRange(sv.VerticalPipes);
            ThRainSystemService.MakePairs(GetLongConverterLinesGroup(), pairs);
            LongConverterLineToWaterBuckets.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.WaterBuckets, 10));
            pairs.AddRange(LongConverterLineToWaterBuckets);
            pairs.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.VerticalPipes, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            LongConverterLineToWaterBucketsGroups = groups;
        }
        public List<Entity> WaterBuckets;
        private void InitThGravityService(Point3dCollection range)
        {
            thGravityService = new ThGravityService() { adb = adb };
            thGravityService.Init(range);
            var WaterBuckets = thGravityService.GetRelatedGravityWaterBucket(range);
            var pls = new List<Entity>();
            foreach (var ext in WaterBuckets)
            {
                var r = GRect.Create(ext);
                var pl = EntityFactory.CreatePolyline(r.ToPoint3dCollection());
                pls.Add(pl);
                BoundaryDict[pl] = r;
            }
            this.WaterBuckets = pls;
            var si = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(pls.ToCollection());
            thGravityService.GetGravityWaterBuckets = rg => si.SelectCrossingPolygon(rg).Cast<Entity>().ToList();
        }
        List<KeyValuePair<Entity, string>> brokenCondensePipes = new List<KeyValuePair<Entity, string>>();
        void FindOutBrokenCondensePipes()
        {
            var sv = this;
            var cps1 = new HashSet<Entity>();
            var cps2 = new HashSet<Entity>();
            foreach (var e in sv.CondensePipes)
            {
                var lb = sv.GetLabel(e);
                if (lb != null)
                {
                    cps1.Add(e);
                }
                else
                {
                    cps2.Add(e);
                }
            }
            foreach (var e in cps2)
            {
                var bd = sv.BoundaryDict[e];
                Entity ee = null;
                double dis = double.MaxValue;
                foreach (var c in cps1)
                {
                    var d = GeoAlgorithm.Distance(sv.BoundaryDict[c].Center, bd.Center);
                    if (d < dis)
                    {
                        dis = d;
                        ee = c;
                    }
                }
                if (ee != null && dis < 500)
                {
                    var lb = sv.GetLabel(ee);
                    brokenCondensePipes.Add(new KeyValuePair<Entity, string>(e, lb));
                    sv.SetLabel(e, lb);
                }
            }
        }
        public void CollectLongConverterLines()
        {
            LongConverterLines.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-RAIN-PIPE").Where(x => x is Line || x is Polyline || x.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve"));
            foreach (var e in LongConverterLines)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectDraiDomePipes()
        {
            DraiDomePipes.AddRange(adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE").Where(x => x is Line || x is Polyline || x.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve"));
            foreach (var e in DraiDomePipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        public void CollectWrappingPipes()
        {
            var blockNameOfVerticalPipe = "套管";
            WrappingPipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
            .Where(x => x.Layer == "W-BUSH")
            .Where(x => x.GetBlockEffectiveName() == blockNameOfVerticalPipe));
            foreach (var e in WrappingPipes)
            {
                if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
        Dictionary<KeyValuePair<object, object>, object> cacheDict = new Dictionary<KeyValuePair<object, object>, object>();
        List<T> FiltEntsByRect<T>(Point3dCollection range, IList<T> ents) where T : Entity
        {
            var kv = new KeyValuePair<object, object>(range, ents);
            if (!cacheDict.TryGetValue(kv, out object obj))
            {
                var ret = FiltByRect(range, ents).Cast<T>().ToList();
                cacheDict[kv] = ret;
                return ret;
            }
            return (List<T>)obj;
        }
        static readonly Regex re = new Regex(@"接(\d+F)屋面雨水斗");
        public static bool HasGravityLabelConnected(string text)
        {
            return re.IsMatch(text);
        }
        public static Match TestGravityLabelConnected(string text)
        {
            return re.Match(text);
        }
        public bool HasGravityLabelConnected(Point3dCollection range, string pipeId)
        {
            var e1 = FiltEntsByRect(range, VerticalPipes).FirstOrDefault(e => GetLabel(e) == pipeId);
            if (e1 == null) return false;
            var ents = FiltEntsByRect(range, VerticalPipes).Where(e => re.IsMatch(GetLabel(e) ?? "")).ToList();
            if (ents.Count == 0) return false;
            var gs = GetLongConverterGroup();
            foreach (var g in gs)
            {
                if (g.Count <= 1) continue;
                if (!g.Contains(e1)) continue;
                foreach (var e3 in g)
                {
                    if (ents.Contains(e3)) return true;
                }
            }
            return false;
        }
        public List<string> GetCondenseVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
        }
        public List<string> GetBalconyVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(BALCONY_PIPE_PREFIX)).ToList();
        }
        public List<string> GetRoofVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX)).ToList();
        }
        public List<string> GetVerticalPipeNotes(Point3dCollection pts)
        {
            var vpTexts = GetDBText(pts);
            return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX) || t.StartsWith(BALCONY_PIPE_PREFIX) || t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
        }
        public List<string> GetDBText(Point3dCollection pts)
        {
            return _CodeByFeng(pts);
        }
        private LS _CodeByFeng(Point3dCollection pts)
        {
            if (DbTextSpatialIndex == null)
            {
                DbTextSpatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(VerticalPipeDBTexts.ToCollection());
            }
            var temps = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
            var texts = temps.OfType<DBText>().Select(e => e.TextString);
            if (getDbTexts != null)
            {
                texts = texts.Concat(getDbTexts(pts.ToRect()));
            }
            return texts.Distinct().ToList();
        }
        Func<GRect, IEnumerable<string>> getDbTexts;
        private LS _CodeByWang(Point3dCollection pts)
        {
            var textEntities = GetDBTextEntities(pts);
            var texts = textEntities.OfType<DBText>().Select(e => e.TextString);
            return texts.ToList();
        }
        public List<Entity> GetDBTextEntities(Point3dCollection pts)
        {
            if (DbTextSpatialIndex != null)
            {
                var rst = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
                return rst;
            }
            else
            {
                using (var db = Linq2Acad.AcadDatabase.Use(adb.Database))
                {
                    var rst = new List<Entity>();
                    var tvs = new List<TypedValue>();
                    tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName + "," + RXClass.GetClass(typeof(MText)).DxfName));
                    tvs.Add(new TypedValue((int)DxfCode.LayerName, ThWPipeCommon.W_RAIN_NOTE));
                    var sf = new SelectionFilter(tvs.ToArray());
                    var psr = Active.Editor.SelectAll(sf);
                    if (psr.Status == PromptStatus.OK)
                    {
                        foreach (var id in psr.Value.GetObjectIds())
                            rst.Add(db.Element<Entity>(id));
                    }
                    var lst2 = SingleTianzhengElements.OfType<DBText>().ToList();
                    rst = rst.Union(lst2).ToList();
                    if (pts.Count >= 3)
                    {
                        DbTextSpatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(rst.ToCollection());
                        rst = DbTextSpatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
                    }
                    return rst;
                }
            }
        }
        public IEnumerable<Entity> FiltByRect(Point3dCollection range, IEnumerable<Entity> ents)
        {
            var rg = range.ToRect();
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out GRect r))
                {
                    if (rg.ContainsRect(r))
                    {
                        yield return e;
                    }
                }
            }
        }
        public bool GetCenterOfVerticalPipe(Point3dCollection range, string verticalPipeID, ref Point3d outPt)
        {
            var rg = range.ToRect();
            foreach (var pipe in VerticalPipes)
            {
                VerticalPipeToLabelDict.TryGetValue(pipe, out string id);
                if (id == verticalPipeID)
                {
                    var bd = BoundaryDict[pipe];
                    if (rg.ContainsRect(bd))
                    {
                        outPt = bd.Center.ToPoint3d();
                        return true;
                    }
                }
            }
            {
                var bd = getVPipeBoundary(range.ToRect(), verticalPipeID);
                if (bd.IsValid)
                {
                    outPt = bd.Center.ToPoint3d();
                    return true;
                }
            }
            return false;
        }
        public ThWSDOutputType GetPipeOutputType(Point3dCollection range, string verticalPipeID)
        {
            ThWSDOutputType outputType = new ThWSDOutputType();
            outputType.OutputType = GetOutputType(range, verticalPipeID, out bool hasDrivePipe);
            outputType.HasDrivePipe = hasDrivePipe;
            if (outputType.OutputType == RainOutputTypeEnum.WaterWell)
            {
                var dn = GetWaterWellDNValue(verticalPipeID, range);
                if (dn != null)
                {
                    outputType.Label = dn;
                }
            }
            return outputType;
        }
        public TranslatorTypeEnum GetTranslatorType(string verticalPipeID, GRect rect)
        {
            var ret = _GetTrans(verticalPipeID, rect);
            return ret;
        }
        private TranslatorTypeEnum _GetTrans(string verticalPipeID, GRect rect)
        {
            var shortCvts = FiltEntityByRange(rect, ShortConverters.SelectMany(x => new Entity[] { x.Item1, x.Item2 })).ToList();
            foreach (var pipe in FiltEntityByRange(rect, VerticalPipeToLabelDict.Keys))
            {
                VerticalPipeToLabelDict.TryGetValue(pipe, out string label);
                if (label == verticalPipeID)
                {
                    if (shortCvts.Contains(pipe)) return TranslatorTypeEnum.Short;
                    if (FiltEntityByRange(rect, LongConverterLines).Any(cvt => LongConverterToPipesDict.Get(cvt).Any(p => p == pipe))) return TranslatorTypeEnum.Long;
                    return TranslatorTypeEnum.None;
                }
            }
            return TranslatorTypeEnum.None;
        }
        private IEnumerable<Entity> FiltEntityByRange(GRect range, IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (BoundaryDict.TryGetValue(e, out GRect r))
                {
                    if (range.ContainsRect(r)) yield return e;
                }
            }
        }
        public List<KeyValuePair<Entity, Entity>> CollectVerticalPipesData()
        {
            var lines = this.VerticalPipeLines;
            var pipes = this.VerticalPipes;
            var dbTxtToHLineDict = new Dictionary<Entity, Entity>();
            var linesGroup = new List<List<Entity>>();
            var groups = new List<List<Entity>>();
            var plDict = new Dictionary<Entity, Polyline>();
            var lineToPipesDict = new ListDict<Entity>();
            CollectDbTxtToLbLines(dbTxtToHLineDict, VerticalPipeDBTexts, VerticalPipeLines);
            GroupLines(lines, linesGroup, 10);
            var pls1 = new List<Polyline>();
            var pls2 = new List<Polyline>();
            foreach (var e in this.VerticalPipes)
            {
                var bd = this.BoundaryDict[e];
                var pl = PolylineTools.CreatePolygon(bd.Center, 4, bd.Radius);
                plDict[e] = pl;
                pls1.Add(pl);
            }
            foreach (var e in this.VerticalPipeLines)
            {
                var pl = (e as Line).Buffer(10);
                plDict[e] = pl;
                pls2.Add(pl);
            }
            var si = ThRainSystemService.BuildSpatialIndex(pls1);
            foreach (var pl2 in pls2)
            {
                foreach (var pl1 in si.SelectCrossingPolygon(pl2).Cast<Polyline>().ToList())
                {
                    var pipe = this.VerticalPipes[pls1.IndexOf(pl1)];
                    var line = this.VerticalPipeLines[pls2.IndexOf(pl2)];
                    lineToPipesDict.Add(line, pipe);
                }
            }
            {
                var totalList = new List<Entity>();
                totalList.AddRange(this.VerticalPipeDBTexts);
                totalList.AddRange(this.VerticalPipes);
                totalList.AddRange(this.VerticalPipeLines);
                var pairs = new List<KeyValuePair<Entity, Entity>>();
                foreach (var kv in dbTxtToHLineDict) pairs.Add(kv);
                lineToPipesDict.ForEach((e, l) => { l.ForEach(o => pairs.Add(new KeyValuePair<Entity, Entity>(e, o))); });
                MakePairs(linesGroup, pairs);
                GroupByBFS(groups, totalList, pairs);
                foreach (var g in groups)
                {
                    var targetPipes = SortEntitiesBy2DSpacePosition(g.Where(e => this.VerticalPipes.Contains(e))).ToList();
                    var targetTexts = SortEntitiesBy2DSpacePosition(g.Where(e => this.VerticalPipeDBTexts.Contains(e))).ToList();
                    if (targetPipes.Count == targetTexts.Count && targetTexts.Count > 0)
                    {
                        setVisibilities(targetPipes, targetTexts);
                    }
                }
                return pairs;
            }
        }
        public IEnumerable<KeyValuePair<Entity, Entity>> EnumerateDbTxtToLbLine(List<DBText> dbTxts, List<Entity> lblines)
        {
            foreach (var e1 in dbTxts)
            {
                foreach (var e2 in lblines)
                {
                    if (e2 is Line line)
                    {
                        var seg = line.ToGLineSegment();
                        if (seg.IsHorizontal(10))
                        {
                            var c1 = this.BoundaryDict[e1].Center;
                            var c2 = this.BoundaryDict[e2].Center;
                            if (c1.Y > c2.Y && GeoAlgorithm.Distance(c1, c2) < 500)
                            {
                                yield return new KeyValuePair<Entity, Entity>(e1, e2);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void CollectDbTxtToLbLines(Dictionary<Entity, Entity> dbTxtToHLineDict, List<DBText> dbTxts, List<Entity> lblines)
        {
            foreach (var kv in EnumerateDbTxtToLbLine(dbTxts, lblines)) dbTxtToHLineDict[kv.Key] = kv.Value;
        }
        public static void MakePairs(List<List<Entity>> linesGroup, List<KeyValuePair<Entity, Entity>> pairs)
        {
            foreach (var g in linesGroup) for (int i = 1; i < g.Count; i++) pairs.Add(new KeyValuePair<Entity, Entity>(g[i - 1], g[i]));
        }
        public static List<KeyValuePair<Entity, Entity>> GroupLinesBySpatialIndex(List<Entity> lines)
        {
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var bfs = lines.Select(e => (e as Line)?.Buffer(10)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    lst.ForEach(j => pairs.Add(new KeyValuePair<Entity, Entity>(lines[i], lines[j])));
                }
            }
            return pairs;
        }
        public void LabelEnts()
        {
            LabelWRainLinesAndVerticalPipes();
            LabelCondensePipes();
            LabelFloorDrains();
            LabelWrappingPipes();
            LabelWaterPorts();
            LabelRainPortSymbols();
            LabelRainPortLinesAndTexts();
            LabelWaterWells();
            LabelFloorDrainsWrappingPipe();
            LabelWaterWellsWrappingPipe();
        }
        public List<List<Entity>> LabelFloorDrainsWrappingPipe()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.FloorDrains);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            pairs.AddRange(WRainLinesToFloorDrains);
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var lines = new HashSet<Entity>(sv.WRainLines);
            var fds = new HashSet<Entity>(sv.FloorDrains);
            var wps = new HashSet<Entity>(sv.WrappingPipes);
            foreach (var g in groups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => lines.Contains(e)) || !g.Any(e => fds.Contains(e)) || !g.Any(e => wps.Contains(e))) continue;
                wrappingEnts.AddRange(g.Where(e => fds.Contains(e)));
            }
            return groups;
        }
        public List<List<Entity>> LabelWaterWellsWrappingPipe()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WaterWells);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            pairs.AddRange(WRainLinesToWaterWells);
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var lines = new HashSet<Entity>(sv.WRainLines);
            var wells = new HashSet<Entity>(sv.WaterWells);
            var wps = new HashSet<Entity>(sv.WrappingPipes);
            foreach (var g in groups)
            {
                if (g.Count < 3) continue;
                if (!g.Any(e => lines.Contains(e)) || !g.Any(e => wells.Contains(e)) || !g.Any(e => wps.Contains(e))) continue;
                wrappingEnts.AddRange(g.Where(e => wells.Contains(e)));
            }
            return groups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToFloorDrains = new List<KeyValuePair<Entity, Entity>>();
        public List<KeyValuePair<Entity, Entity>> WRainLinesToWaterWells = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelFloorDrains()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.FloorDrains);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToFloorDrains.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.FloorDrains, 1));
            pairs.AddRange(WRainLinesToFloorDrains);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        HashSet<string> waterWellLabels = new HashSet<string>();
        List<KeyValuePair<Entity, string>> WaterWellToPipeId = new List<KeyValuePair<Entity, string>>();
        public void LabelWaterWells()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WaterWells);
            WRainLinesToWaterWells.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.WaterWells, 10));
            pairs.AddRange(WRainLinesToWaterWells);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
            var f = ThRainSystemService.BuildSpatialIndexLazy(sv.WRainLines);
            foreach (var well in sv.WaterWells.ToList())
            {
                var pl = sv.CreatePolygon(well, 6, 100);
                foreach (var line in f(pl))
                {
                    var lb = GetLabel(line);
                    if (lb != null)
                    {
                        waterWellLabels.Add(lb);
                        WaterWellToPipeId.Add(new KeyValuePair<Entity, string>(well, lb));
                    }
                }
            }
            foreach (var well in sv.WaterWells.Where(e => !sv.VerticalPipeToLabelDict.ContainsKey(e)).ToList())
            {
                var pl = sv.CreatePolygon(well, 6, 1500);
                foreach (var line in f(pl))
                {
                    var lb = GetLabel(line);
                    if (lb != null)
                    {
                        waterWellLabels.Add(lb);
                        WaterWellToPipeId.Add(new KeyValuePair<Entity, string>(well, lb));
                    }
                }
            }
        }
        public bool SetLabel(Entity e, string lb, bool force = false)
        {
            if (force)
            {
                VerticalPipeToLabelDict[e] = lb;
                return true;
            }
            if (GetLabel(e) == null)
            {
                VerticalPipeToLabelDict[e] = lb;
                return true;
            }
            return false;
        }
        public string GetLabel(Entity e)
        {
            VerticalPipeToLabelDict.TryGetValue(e, out string lb); return lb;
        }
        public string GetLabelFromList(IEnumerable<Entity> ents)
        {
            foreach (var e in ents) if (VerticalPipeToLabelDict.TryGetValue(e, out string lb)) return lb;
            return null;
        }
        public List<List<Entity>> RainPortsGroups = new List<List<Entity>>();
        public void LabelWaterPorts()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.VerticalPipeLines);
            totalList.AddRange(sv.ConnectToRainPortDBTexts);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            ThRainSystemService.MakePairs(ThRainSystemService.GroupLines(sv.VerticalPipeLines), pairs);
            pairs.AddRange(sv.EnumerateDbTxtToLbLine(sv.ConnectToRainPortDBTexts, sv.VerticalPipeLines));
            pairs.AddRange(sv.EnumerateEntities(sv.VerticalPipeLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            var enumerateEnts = sv.EnumerateEntities(sv.WRainLines);
            RainPortsGroups = groups;
            foreach (var g in groups)
            {
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.ConnectToRainPortSymbols.Contains(e))
                    {
                        {
                            foreach (var kv in enumerateEnts(new List<Entity>() { e }, 10))
                            {
                                if (sv.VerticalPipeToLabelDict.TryGetValue(kv.Key, out lb))
                                {
                                    foreach (var _e in g)
                                    {
                                        if (!sv.VerticalPipeToLabelDict.ContainsKey(_e))
                                        {
                                            sv.VerticalPipeToLabelDict[_e] = lb;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }
        List<List<Entity>> LongConverterGroups;
        public List<List<Entity>> GetLongConverterGroup()
        {
            if (LongConverterGroups == null)
            {
                var sv = this;
                var pairs = new List<KeyValuePair<Entity, Entity>>();
                var groups = new List<List<Entity>>();
                var totalList = new List<Entity>();
                totalList.AddRange(sv.LongConverterLines);
                totalList.AddRange(sv.VerticalPipes);
                ThRainSystemService.MakePairs(GetLongConverterLinesGroup(), pairs);
                pairs.AddRange(sv.EnumerateEntities(sv.LongConverterLines, sv.VerticalPipes.Cast<Entity>().ToList(), 1));
                ThRainSystemService.GroupByBFS(groups, totalList, pairs);
                LongConverterGroups = groups;
            }
            return LongConverterGroups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToVerticalPipes = new List<KeyValuePair<Entity, Entity>>();
        public List<KeyValuePair<Entity, Entity>> WRainLinesToCondensePipes = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelCondensePipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.CondensePipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToCondensePipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.CondensePipes, 1));
            pairs.AddRange(WRainLinesToCondensePipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        public List<KeyValuePair<Entity, Entity>> WRainLinesToWrappingPipes = new List<KeyValuePair<Entity, Entity>>();
        public List<List<Entity>> LabelWrappingPipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.WrappingPipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToWrappingPipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.WrappingPipes.Cast<Entity>().ToList(), 1));
            pairs.AddRange(WRainLinesToWrappingPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.WRainLines.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
            }
            return groups;
        }
        public List<List<Entity>> LabelWRainLinesAndVerticalPipesGroups = new List<List<Entity>>();
        public void LabelWRainLinesAndVerticalPipes()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.VerticalPipes);
            ThRainSystemService.MakePairs(GetWRainLinesGroup(), pairs);
            WRainLinesToVerticalPipes.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.VerticalPipes, 100));
            pairs.AddRange(WRainLinesToVerticalPipes);
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            foreach (var g in groups)
            {
                if (g.Count == 1) continue;
                string lb = null;
                foreach (var e in g)
                {
                    if (sv.VerticalPipes.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            break;
                        }
                    }
                }
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        {
                            if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                            {
                                sv.VerticalPipeToLabelDict[e] = lb;
                            }
                        }
                    }
                }
                var pipes = g.Where(e =>
                {
                    if (VerticalPipes.Contains(e))
                    {
                        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb))
                        {
                            if (!string.IsNullOrWhiteSpace(lb)) return true;
                        }
                    }
                    return false;
                })
                .Where(e => BoundaryDict[e].IsValid)
                .ToList();
                if (pipes.Count > 1)
                {
                    LongConverterPipes.AddRange(pipes);
                }
            }
            LabelWRainLinesAndVerticalPipesGroups = groups;
        }
        public HashSet<Entity> LongConverterPipes = new HashSet<Entity>();
        List<List<Entity>> WRainLinesGroup;
        private List<List<Entity>> GetWRainLinesGroup()
        {
            WRainLinesGroup ??= ThRainSystemService.GroupLines_SkipCrossingCases(WRainLines);
            return WRainLinesGroup;
        }
        public static List<List<Entity>> GroupLines_SkipCrossingCases(List<Entity> lines)
        {
            var linesGroup = new List<List<Entity>>();
            GroupLines_SkipCrossingCases(lines, linesGroup);
            return linesGroup;
        }
        public static void GroupLines_SkipCrossingCases(List<Entity> lines, List<List<Entity>> linesGroup)
        {
            var pairs = new List<KeyValuePair<int, int>>();
            var bfs = lines.Select(e => (TryConvertToLine(e))?.Buffer(10)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    lst.ForEach(j =>
                    {
                        var line1 = lines[i];
                        var line2 = lines[j];
                        if (GeoAlgorithm.TryConvertToLineSegment(line1, out GLineSegment seg1) && GeoAlgorithm.TryConvertToLineSegment(line2, out GLineSegment seg2))
                        {
                            if (GeoAlgorithm.IsLineConnected(line1, line2))
                            {
                                pairs.Add(new KeyValuePair<int, int>(i, j));
                            }
                        }
                    });
                }
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        List<List<Entity>> LongConverterLinesGroup;
        private List<List<Entity>> GetLongConverterLinesGroup()
        {
            LongConverterLinesGroup ??= ThRainSystemService.GroupLines(LongConverterLines);
            return LongConverterLinesGroup;
        }
        public Func<List<Entity>, double, IEnumerable<KeyValuePair<Entity, Entity>>> EnumerateEntities(List<Entity> lines)
        {
            var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (TryConvertToLine(e))?.Buffer(10))).ToList();
            var bfs = mps1.Select(kv => kv.Value).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            IEnumerable<KeyValuePair<Entity, Entity>> f(List<Entity> wells, double expand)
            {
                var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, this.CreatePolygon(e, expand: expand))).ToList();
                var pls = mps2.Select(kv => kv.Value).ToList();
                foreach (var pl in pls)
                {
                    foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
                    {
                        var line = mps1.First(kv => kv.Value == bf).Key;
                        var well = mps2.First(kv => kv.Value == pl).Key;
                        yield return new KeyValuePair<Entity, Entity>(line, well);
                    }
                }
            }
            return f;
        }
        public void LabelGroups(List<List<Entity>> groups)
        {
            LabelGroups(this, groups);
        }
        public void LabelRainPortSymbols()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.WRainLines);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            pairs.AddRange(sv.EnumerateEntities(sv.WRainLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
        }
        public bool IsRainPort(string pipeId)
        {
            var ok = VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && ConnectToRainPortDBTexts.Contains(kv.Key));
            if (!ok)
            {
                ok = VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && ConnectToRainPortSymbols.Contains(kv.Key));
            }
            return ok;
        }
        public bool IsWaterWell(string pipeId)
        {
            return waterWellLabels.Contains(pipeId) || VerticalPipeToLabelDict.Any(kv => kv.Value == pipeId && WaterWells.Contains(kv.Key));
        }
        public Entity GetWaterWell(string pipeId, List<Entity> wells)
        {
            var e = VerticalPipeToLabelDict.FirstOrDefault(kv => kv.Value == pipeId && WaterWells.Contains(kv.Key)).Key;
            return e;
        }
        public string GetWaterWellDNValue(string pipeId, Point3dCollection range)
        {
            string ret = null;
            var wells = GetWaterWells(range);
            var well = GetWaterWell(pipeId, wells);
            if (well != null)
            {
                WaterWellDNs.TryGetValue(well, out ret);
            }
            if (string.IsNullOrEmpty(ret))
            {
                foreach (var kv in WaterWellToPipeId)
                {
                    if (kv.Value == pipeId && wells.Contains(kv.Key))
                    {
                        WaterWellDNs.TryGetValue(kv.Key, out ret);
                        return ret;
                    }
                }
            }
            return ret;
        }
        public void LabelRainPortLinesAndTexts()
        {
            var sv = this;
            var pairs = new List<KeyValuePair<Entity, Entity>>();
            var groups = new List<List<Entity>>();
            var totalList = new List<Entity>();
            totalList.AddRange(sv.VerticalPipeLines);
            totalList.AddRange(sv.ConnectToRainPortDBTexts);
            totalList.AddRange(sv.ConnectToRainPortSymbols);
            ThRainSystemService.MakePairs(ThRainSystemService.GroupLines(sv.VerticalPipeLines), pairs);
            pairs.AddRange(sv.EnumerateDbTxtToLbLine(sv.ConnectToRainPortDBTexts, sv.VerticalPipeLines));
            pairs.AddRange(sv.EnumerateEntities(sv.VerticalPipeLines, sv.ConnectToRainPortSymbols, 10));
            ThRainSystemService.GroupByBFS(groups, totalList, pairs);
            sv.LabelGroups(groups);
        }
        private static void LabelGroups(ThRainSystemService sv, List<List<Entity>> groups)
        {
            foreach (var g in groups)
            {
                string lb = null;
                foreach (var e in g) if (sv.VerticalPipeToLabelDict.TryGetValue(e, out lb)) break;
                if (lb != null)
                {
                    foreach (var e in g)
                    {
                        if (!sv.VerticalPipeToLabelDict.ContainsKey(e))
                        {
                            sv.VerticalPipeToLabelDict[e] = lb;
                        }
                    }
                }
            }
        }
        public IEnumerable<KeyValuePair<Entity, Entity>> EnumerateEntities(List<Entity> lines, List<Entity> wells, double expand)
        {
            var mps1 = lines.Select(e => new KeyValuePair<Entity, Polyline>(e, (TryConvertToLine(e))?.Buffer(10))).ToList();
            var mps2 = wells.Select(e => new KeyValuePair<Entity, Polyline>(e, this.CreatePolygon(e, expand: expand))).ToList();
            var bfs = mps1.Select(kv => kv.Value).ToList();
            var pls = mps2.Select(kv => kv.Value).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            foreach (var pl in pls)
            {
                foreach (var bf in si.SelectCrossingPolygon(pl).Cast<Polyline>().ToList())
                {
                    var line = mps1.First(kv => kv.Value == bf).Key;
                    var well = mps2.First(kv => kv.Value == pl).Key;
                    yield return new KeyValuePair<Entity, Entity>(line, well);
                }
            }
        }
        public List<Entity> ConvertToPolylines(List<Entity> ents)
        {
            return ents.Select(e => BoundaryDict[e].CreatePolygon(6)).Cast<Entity>().ToList();
        }
        public static void GroupByBFS(List<List<Entity>> groups, List<Entity> totalList, List<KeyValuePair<Entity, Entity>> pairs)
        {
            var dict = new ListDict<Entity>();
            var h = new BFSHelper2<Entity>()
            {
                Pairs = pairs.ToArray(),
                Items = totalList.ToArray(),
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_start, ents) =>
            {
                groups.Add(ents);
            });
        }
        public static List<List<Entity>> GroupLines(List<Entity> lines)
        {
            var linesGroup = new List<List<Entity>>();
            GroupLines(lines, linesGroup, 10);
            return linesGroup;
        }
        static Line TryConvertToLine(Entity e)
        {
            var line = _TryConvertToLine(e);
            if (line != null)
            {
                if (line.Length > 0) return line;
            }
            return null;
        }
        static Line _TryConvertToLine(Entity e)
        {
            var r = e as Line;
            if (r != null) return r;
            if (e.GetType().ToString() == "Autodesk.AutoCAD.DatabaseServices.ImpCurve")
            {
                GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg);
                return new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() };
            }
            return r;
        }
        public static List<List<Polyline>> GroupPolylines(List<Polyline> lines)
        {
            var linesGroup = new List<List<Polyline>>();
            GroupPolylines(lines, linesGroup);
            return linesGroup;
        }
        public static void GroupPolylines(List<Polyline> lines, List<List<Polyline>> linesGroup)
        {
            if (lines.Count == 0) return;
            var pairs = new List<KeyValuePair<int, int>>();
            var si = ThRainSystemService.BuildSpatialIndex(lines);
            for (int i = 0; i < lines.Count; i++)
            {
                var pl = lines[i];
                var lst = si.SelectCrossingPolygon(pl).Cast<Polyline>().Select(e => lines.IndexOf(e)).Where(j => i < j).ToList();
                lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        public static void GroupLines(List<Entity> lines, List<List<Entity>> linesGroup, double bufferDistance)
        {
            if (lines.Count == 0) return;
            var pairs = new List<KeyValuePair<int, int>>();
            var bfs = lines.Select(e => (TryConvertToLine(e))?.Buffer(bufferDistance)).ToList();
            var si = ThRainSystemService.BuildSpatialIndex(bfs.Where(e => e != null).ToList());
            for (int i = 0; i < bfs.Count; i++)
            {
                Polyline bf = bfs[i];
                if (bf != null)
                {
                    var lst = si.SelectCrossingPolygon(bf).Cast<Polyline>().Select(e => bfs.IndexOf(e)).Where(j => i < j).ToList();
                    lst.ForEach(j => pairs.Add(new KeyValuePair<int, int>(i, j)));
                }
            }
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs.ToArray(),
                TotalCount = lines.Count,
                Callback = (g, i) =>
                {
                    dict.Add(g.root, i);
                },
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                linesGroup.Add(l.Select(i => lines[i]).ToList());
            });
        }
        private void setVisibilities(List<Entity> targetPipes, List<Entity> targetTexts)
        {
            var dnProp = "可见性1";
            for (int i = 0; i < targetPipes.Count; i++)
            {
                var pipeEnt = targetPipes[i];
                var dbText = targetTexts[i];
                var label = (dbText as DBText)?.TextString;
                if (label != null)
                {
                    var dnText =/* (pipeEnt.ObjectId.IsValid ? pipeEnt.GetCustomPropertiyStrValue(dnProp) : null) ?? */"DN100";
                    VerticalPipeLabelToDNDict[label] = dnText;
                    VerticalPipeToLabelDict[pipeEnt] = label;
                }
            }
        }
        public static IEnumerable<Geometry> SortGeometrysBy2DSpacePosition(IEnumerable<Geometry> list)
        {
            return from e in list
                   let bd = e.EnvelopeInternal
                   orderby bd.MinX ascending
                   orderby bd.MaxY descending
                   select e;
        }
        public static object CloneObj(object value)
        {
            var memoryStream = new MemoryStream();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(memoryStream, value);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }
        public static T Clone<T>(T value)
        {
            var memoryStream = new MemoryStream();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(memoryStream, value);
            memoryStream.Position = 0;
            return (T)formatter.Deserialize(memoryStream);
        }
        public IEnumerable<T> SortEntitiesBy2DSpacePosition<T>(IEnumerable<T> list) where T : Entity
        {
            return from e in list
                   let bd = BoundaryDict[e]
                   orderby bd.MinX ascending
                   orderby bd.MaxY descending
                   select e;
        }
        public void FindShortConverters()
        {
            var pipeBoundaries = (from pipe in VerticalPipes
                                  let boundary = BoundaryDict[pipe]
                                  where !Equals(boundary, default(GRect))
                                  select new { pipe, boundary }).ToList();
            var d = VerticalPipeToLabelDict;
            for (int i = 0; i < pipeBoundaries.Count; i++)
            {
                for (int j = i + 1; j < pipeBoundaries.Count; j++)
                {
                    var bd1 = pipeBoundaries[i].boundary;
                    var bd2 = pipeBoundaries[j].boundary;
                    if (!bd1.IsValid || !bd2.IsValid) continue;
                    if (!bd1.EqualsTo(bd2, 5)) continue;
                    var dis = GeoAlgorithm.Distance(bd1.Center, bd2.Center);
                    var dis1 = (bd1.Width + bd2.Width) / 2;
                    if (dis <= 5 + dis1)
                    {
                        var pipe1 = pipeBoundaries[i].pipe;
                        var pipe2 = pipeBoundaries[j].pipe;
                        ShortConverters.Add(new Tuple<Entity, Entity>(pipe1, pipe2));
                    }
                }
            }
            foreach (var item in ShortConverters)
            {
                string v;
                if (!d.TryGetValue(item.Item1, out v) && d.TryGetValue(item.Item2, out v))
                {
                    d[item.Item1] = v;
                }
                else if (!d.TryGetValue(item.Item2, out v) && d.TryGetValue(item.Item1, out v))
                {
                    d[item.Item2] = v;
                }
            }
        }
        IEnumerable<T> EnumerateEntities<T>() where T : Entity
        {
            return adb.ModelSpace.OfType<T>()
            .Concat(SingleTianzhengElements.OfType<T>())
            .Concat(ExplodedEntities.OfType<T>())
            .Where(e => e != null && e.ObjectId.IsValid)
            .Distinct();
        }
        public void CollectVerticalPipes()
        {
            var pipes = new List<Entity>();
            {
                var blockNameOfVerticalPipe = "带定位立管";
                pipes.AddRange(adb.ModelSpace.OfType<BlockReference>()
                .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == blockNameOfVerticalPipe));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                foreach (var e in pipes)
                {
                    BoundaryDict[e] = getRealBoundaryForPipe(e);
                }
            }
            {
                var ents = adb.ModelSpace.OfType<Entity>()
                .Where(x => IsTianZhengElement(x))
                .Where(x => x.Layer == "W-RAIN-EQPM")
                .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Count() == 1);
                foreach (var e in ents)
                {
                    BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(ents);
            }
            {
                var ents = adb.ModelSpace.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Layer == "W-RAIN-PIPE-RISR").ToList();
                foreach (var e in ents)
                {
                    BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(ents);
            }
            {
                var lst = adb.ModelSpace.OfType<Entity>()
                .Where(x => IsTianZhengElement(x))
                .Where(x => x.Layer == "WP_KTN_LG").ToList();
                foreach (var c in lst)
                {
                    if (!BoundaryDict.ContainsKey(c)) BoundaryDict[c] = GeoAlgorithm.GetBoundaryRect(c);
                }
                pipes.AddRange(lst);
            }
            {
                var q = EnumerateEntities<Circle>()
                .Where(c => c.Radius >= 50 && c.Radius <= 200
                && (c.Layer.Contains("W-")
                && c.Layer.Contains("-EQPM")
                && c.Layer != "W-EQPM"
                && c.Layer != "W-WSUP-EQPM")
                );
                var lst = q.ToList();
                foreach (var c in lst)
                {
                    if (!BoundaryDict.ContainsKey(c)) BoundaryDict[c] = GeoAlgorithm.GetBoundaryRect(c);
                }
                pipes.AddRange(lst);
            }
            {
                var q = EnumerateEntities<BlockReference>()
                .Where(x => x.Name == "*U398");
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e =>
                    {
                        var m = Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width);
                        return m == 110 || m == 88;
                    });
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }
                var lst = q.ToList();
                lst.AddRange(vps.OfType<BlockReference>());
                foreach (var e in lst)
                {
                    if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = getRealBoundaryForPipe(e);
                }
                pipes.AddRange(lst);
                pipes.AddRange(vps);
            }
            {
                var q = EnumerateEntities<BlockReference>()
                .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                .Where(x => x.Name == "$LIGUAN");
                var lst = q.ToList();
                foreach (var e in lst)
                {
                    if (!BoundaryDict.ContainsKey(e)) BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                }
                pipes.AddRange(lst);
            }
            VerticalPipes.AddRange(pipes.Distinct());
            foreach (var p in VerticalPipes)
            {
                if (!BoundaryDict.ContainsKey(p))
                {
                    BoundaryDict[p] = GeoAlgorithm.GetBoundaryRect(p);
                }
            }
        }
        public static IEnumerable<string> GetRoofLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(ROOF_RAIN_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static IEnumerable<string> GetBalconyLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(BALCONY_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static IEnumerable<string> GetCondenseLabels(IEnumerable<string> labels)
        {
            return labels.Where(x => x.StartsWith(CONDENSE_PIPE_PREFIX)).OrderBy(x => x);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith(ROOF_RAIN_PIPE_PREFIX) || label.StartsWith(BALCONY_PIPE_PREFIX) || label.StartsWith(CONDENSE_PIPE_PREFIX);
        }
        public void CollectVerticalPipeDBTexts()
        {
            var q = adb.ModelSpace.OfType<DBText>()
            .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE);
            {
                var lst = new List<DBText>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (IsTianZhengElement(e))
                    {
                        lst.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                    }
                }
                q = q.Concat(lst);
            }
            {
                var lst = new List<DBText>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                        {
                            if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                var lst3 = e.ExplodeToDBObjectCollection()
                                .OfType<Entity>()
                                .Where(x => ThRainSystemService.IsTianZhengElement(x))
                                .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>())
                                .ToList();
                                lst.AddRange(lst3);
                            }
                        }
                    }
                }
                q = q.Concat(lst);
            }
            {
                var lst = new List<DBText>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        var lst3 = e.ExplodeToDBObjectCollection()
                        .OfType<Entity>()
                        .Where(x => ThRainSystemService.IsTianZhengElement(x))
                        .SelectMany(x => x.ExplodeToDBObjectCollection().OfType<DBText>())
                        .ToList();
                        lst.AddRange(lst3);
                    }
                }
                q = q.Concat(lst);
            }
            q = q.Concat(txts);
            {
                IEnumerable<DBText> f()
                {
                    foreach (var e in adb.ModelSpace.OfType<DBText>().ToList())
                    {
                        if (ThRainSystemService.IsWantedLabelText(e.TextString))
                        {
                            yield return e;
                        }
                    }
                }
                q = q.Concat(f());
            }
            VerticalPipeDBTexts.AddRange(
            q.Distinct().Where(t => IsWantedLabelText(t.TextString))
            );
            foreach (var e in VerticalPipeDBTexts)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
                VerticalPipeDBTextDict[e] = e.TextString;
            }
        }
        public void CollectVerticalPipeLines()
        {
            IEnumerable<Entity> q;
            {
                var lines = new List<Entity>();
                foreach (var e in EnumerateEntities<Entity>())
                {
                    if (e is Line) lines.Add(e);
                    else if (e is Polyline)
                    {
                        lines.AddRange(e.ExplodeToDBObjectCollection().OfType<Line>());
                    }
                }
                lines = lines.Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE).ToList();
                q = lines;
            }
            {
                var lst = new List<Line>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().ToList())
                {
                    var r = GeoAlgorithm.GetBoundaryRect(br);
                    if (r.Width > 10000 && r.Width < 60000)
                    {
                        foreach (var e in br.ExplodeToDBObjectCollection().Cast<Entity>().ToList())
                        {
                            if (ThRainSystemService.IsTianZhengElement(e))
                            {
                                var lst3 = e.ExplodeToDBObjectCollection()
                                .OfType<Line>()
                                .ToList();
                                foreach (var t in lst3)
                                {
                                    lst.Add(t);
                                }
                            }
                        }
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<Entity>().ToList())
                {
                    if (ThRainSystemService.IsTianZhengElement(e))
                    {
                        var lst3 = e.ExplodeToDBObjectCollection()
                        .OfType<Line>()
                        .ToList();
                        foreach (var t in lst3)
                        {
                            lst.Add(t);
                        }
                    }
                }
                q = q.Concat(lst);
            }
            VerticalPipeLines.AddRange(q.OfType<Line>().Where(x => x.Length > 0).Distinct());
            foreach (var e in VerticalPipeLines)
            {
                BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
            }
        }
    }
}