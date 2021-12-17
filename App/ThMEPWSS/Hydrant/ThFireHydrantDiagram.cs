namespace ThMEPWSS.FireNumFlatDiagramNs
{
    using AcHelper;
    using AcHelper.Commands;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using DotNetARX;
    using Dreambuild.AutoCAD;
    using Linq2Acad;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ThCADExtension;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.ViewModel;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.Runtime;
    using NetTopologySuite.Geometries;
    using NFox.Cad;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using ThMEPEngineCore.Algorithm;
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Service;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
    using ThMEPEngineCore.Model.Common;
    using NetTopologySuite.Operation.Buffer;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;
    using Exception = System.Exception;
    using NetTopologySuite.Geometries.Prepared;
    using static FlatDiagramService;
    using NetTopologySuite.Algorithm;
    using ThMEPWSS.UndergroundFireHydrantSystem.Service;
    using ThMEPWSS.UndergroundFireHydrantSystem.Model;
    using ThMEPWSS.UndergroundFireHydrantSystem.Method;
    using System.Runtime.Serialization;
    using System.Linq.Expressions;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    public class FireHydrantSystem
    {
        public Dictionary<Point3dEx, List<Point3dEx>> BranchDic;
        public Dictionary<Point3dEx, List<Point3dEx>> ValveDic;
        public List<List<Point3dEx>> MainPathList;
        public List<List<Point3dEx>> SubPathList;
        public FireHydrantSystemIn FireHydrantSystemIn;
        public FireHydrantSystemOut FireHydrantSystemOut;
    }
    public static class FlatDiagramService
    {
        public const string MLeaderLayer = THESAURUSBLEMISH;
        public static Geometry GetGeometry(Entity entity)
        {
            if (entity is null) return null;
            if (entity is DBText dbText) return GetGeometry(dbText);
            if (entity is Line line) return GetGeometry(line);
            if (entity is Circle circle) return GetGeometry(circle);
            if (entity is MText mText) return GetGeometry(mText);
            if (entity is Polyline polyline) return GetGeometry(polyline);
            if (entity is MLeader mLeader) return GetGeometry(mLeader);
            if (entity is Leader leader) return GetGeometry(leader);
            throw new NotSupportedException();
        }
        public static Geometry GetGeometry(Circle circle)
        {
            var geo = circle.ToGCircle().ToCirclePolygon(SUPERLATIVENESS);
            geo.UserData = circle;
            return geo;
        }
        public static Geometry GetGeometry(DBText dBText)
        {
            var geo = dBText.Bounds.ToGRect().ToPolygon();
            geo.UserData = dBText;
            return geo;
        }
        public static Geometry GetGeometry(MText mText)
        {
            return GeoFac.CreateGeometry(mText, mText.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(Line line)
        {
            var geo = line.ToGLineSegment().ToLineString();
            geo.UserData = line;
            return geo;
        }
        public static Geometry GetGeometry(Polyline polyline)
        {
            return GeoFac.CreateGeometry(polyline, polyline.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(Leader leader)
        {
            return GeoFac.CreateGeometry(leader, leader.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static Geometry GetGeometry(MLeader mLeader)
        {
            return GeoFac.CreateGeometry(mLeader, mLeader.ExplodeToDBObjectCollection().OfType<Entity>().Select(GetGeometry).ToArray());
        }
        public static void cbGenerate()
        {
            Generate(FireHydrantSystemUIViewModel.Singleton);
        }
        public static void cbLabelNode() { }
        public static void cbLabelRing() { }
        public const int THESAURUSHOUSING = 1;
        public const int THESAURUSPERMUTATION = 2;
        public const int THESAURUSSTAMPEDE = 0;
        public const bool INTRAVASCULARLY = false;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const int HYPERDISYLLABLE = 100;
        public const string THESAURUSDEPLORE = "";
        public const int THESAURUSENTREPRENEUR = 50;
        public const int QUOTATIONWITTIG = 500;
        public const int QUOTATIONEDIBLE = 4;
        public const int INTROPUNITIVENESS = 3;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const string THESAURUSBLEMISH = "W-FRPT-NOTE";
        public const string BACTERIOLOGICAL = "\n请指定环管标记起点: ";
        public const string THESAURUSINAPPOSITE = "\n请指定一个起点: ";
        public const string QUOTATIONPERISTALTIC = "([a-zA-Z])'?";
        public const string SEMITRANSPARENT = "1:150";
        public const double THESAURUSSOMBRE = 525.0;
        public const double THESAURUSCOMPULSIVE = 350.0;
        public static void Generate(FireHydrantSystemUIViewModel vm)
        {
            int @case = THESAURUSSTAMPEDE;
            if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Whole && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Whole)
            {
                @case = THESAURUSHOUSING;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Whole && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Single)
            {
                @case = THESAURUSPERMUTATION;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Single && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Whole)
            {
                @case = INTROPUNITIVENESS;
            }
            else if (vm.NumberingMethod is FireHydrantSystemUIViewModel.NumberingMethodEnum.Single && vm.ProcessingObject is FireHydrantSystemUIViewModel.ProcessingObjectEnum.Single)
            {
                @case = QUOTATIONEDIBLE;
            }
            else
            {
                throw new NotSupportedException();
            }
            Point3d selPt = default;
            FireHydrantSystem GetCtx()
            {
                FocusMainWindow();
                var _vm = new FireHydrantSystemViewModel();
                Point3d loopStartPt;
                {
                    var opt = new PromptPointOptions(BACTERIOLOGICAL);
                    var propPtRes = Active.Editor.GetPoint(opt);
                    if (propPtRes.Status != PromptStatus.OK) return null;
                    loopStartPt = propPtRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                }
                var selectArea = TrySelectRangeEx();
                if (selectArea == null) return null;
                if (@case is THESAURUSPERMUTATION or QUOTATIONEDIBLE)
                {
                    var opt = new PromptPointOptions(THESAURUSINAPPOSITE);
                    var propPtRes = Active.Editor.GetPoint(opt);
                    if (propPtRes.Status != PromptStatus.OK) return null;
                    selPt = propPtRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                }
                var fireHydrantSysIn = new FireHydrantSystemIn(_vm.SetViewModel.FloorLineSpace);
                var fireHydrantSysOut = new FireHydrantSystemOut();
                var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();
                var valveDic = new Dictionary<Point3dEx, List<Point3dEx>>();
                var subPathList = new List<List<Point3dEx>>();
                var mainPathList = new List<List<Point3dEx>>();
                var ctx = new FireHydrantSystem()
                {
                    BranchDic = branchDic,
                    ValveDic = valveDic,
                    FireHydrantSystemIn = fireHydrantSysIn,
                    FireHydrantSystemOut = fireHydrantSysOut,
                };
                using (DocLock)
                using (var adb = AcadDatabase.Active())
                {
                    GetInput.GetFireHydrantSysInput(adb, ref fireHydrantSysIn, selectArea, loopStartPt);
                    var _mainPathList = MainLoop.Get(ref fireHydrantSysIn);
                    if (_mainPathList.Count == THESAURUSSTAMPEDE) return null;
                    var _subPathList = SubLoop.Get(ref fireHydrantSysIn, _mainPathList);
                    var visited = new HashSet<Point3dEx>();
                    visited.AddVisit(_mainPathList);
                    visited.AddVisit(_subPathList);
                    PtDic.CreateBranchDic(ref branchDic, ref valveDic, _mainPathList, fireHydrantSysIn, visited);
                    PtDic.CreateBranchDic(ref branchDic, ref valveDic, _subPathList, fireHydrantSysIn, visited);
                    GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, _mainPathList[THESAURUSSTAMPEDE], fireHydrantSysIn, branchDic);
                    GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, _subPathList, fireHydrantSysIn, branchDic);
                    GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, valveDic, fireHydrantSysIn);
                    mainPathList = _mainPathList;
                    subPathList = _subPathList;
                }
                ctx.MainPathList = mainPathList;
                ctx.SubPathList = subPathList;
                return ctx;
            }
            var TEXTHEIGHT = vm.CurrentDwgRatio is SEMITRANSPARENT ? THESAURUSSOMBRE : THESAURUSCOMPULSIVE;
            MLeader DrawMLeader(string content, Point3d p1, Point3d p2)
            {
                var e = new MLeader();
                var mt = new MText() { Contents = content, TextHeight = TEXTHEIGHT, ColorIndex = DISPENSABLENESS, };
                ByLayer(mt);
                e.MText = mt;
                e.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
                e.ArrowSize = THESAURUSSTAMPEDE;
                e.DoglegLength = THESAURUSENTREPRENEUR;
                e.LandingGap = THESAURUSSTAMPEDE;
                e.ExtendLeaderToText = INTRAVASCULARLY;
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
                e.AddLeaderLine(p1);
                var bd = e.MText.Bounds.ToGRect();
                var p3 = p2.OffsetY(bd.Height + HYPERDISYLLABLE);
                if (p2.X < p1.X)
                {
                    p3 = p3.OffsetX(-bd.Width);
                }
                e.TextLocation = p3;
                e.Layer = MLeaderLayer;
                DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
                return e;
            }
            var ctx = GetCtx();
            if (ctx == null) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var mlGeosf = GeoFac.CreateIntersectsSelector(adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer is MLeaderLayer).Select(GetGeometry).ToList());
                var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
                foreach (var ppt in ctx.FireHydrantSystemIn.VerticalPosition)
                {
                    var pt = ppt._pt;
                    mlInfos.Add(MLeaderInfo.Create(pt, THESAURUSDEPLORE));
                }
                var pts = mlInfos.Select(x => x.BasePoint.ToNTSPoint(x)).Distinct().ToList();
                var ptsf = GeoFac.CreateIntersectsSelector(pts);
                switch (@case)
                {
                    case THESAURUSHOUSING:
                        {
                            var mainVisited = new List<Point3dEx>();
                            var k = vm.StartNum - THESAURUSHOUSING;
                            foreach (var mpt in ctx.MainPathList)
                            {
                                foreach (var ppt in mpt)
                                {
                                    mainVisited.Add(ppt);
                                    foreach (var kv in ctx.BranchDic)
                                    {
                                        if (kv.Key.Equals(ppt))
                                        {
                                            foreach (var v in kv.Value)
                                            {
                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                {
                                                    var info = (MLeaderInfo)pt.UserData;
                                                    if (info.Text == THESAURUSDEPLORE)
                                                    {
                                                        info.Text = vm.Prefix + ++k;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (var _mpt in ctx.SubPathList)
                            {
                                var mpt = _mpt.ToList();
                                {
                                    if (mpt.Count > THESAURUSHOUSING)
                                    {
                                        var first = mpt.First();
                                        var last = mpt.Last();
                                        var i = mainVisited.IndexOf(first);
                                        var j = mainVisited.IndexOf(last);
                                        var shouldReverse = INTRAVASCULARLY;
                                        if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                        {
                                            if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                        }
                                        else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                        {
                                            shouldReverse = THESAURUSOBSTINACY;
                                        }
                                        if (shouldReverse)
                                        {
                                            mpt.Reverse();
                                        }
                                    }
                                }
                                foreach (var ppt in mpt)
                                {
                                    foreach (var kv in ctx.BranchDic)
                                    {
                                        if (kv.Key.Equals(ppt))
                                        {
                                            foreach (var v in kv.Value)
                                            {
                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                {
                                                    var info = (MLeaderInfo)pt.UserData;
                                                    if (info.Text == THESAURUSDEPLORE)
                                                    {
                                                        info.Text = vm.Prefix + ++k;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case THESAURUSPERMUTATION:
                        {
                            void main()
                            {
                                var mainVisited = new List<Point3dEx>();
                                var k = vm.StartNum - THESAURUSHOUSING;
                                foreach (var _mpt in ctx.MainPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        var last = mpt.Last();
                                        if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            mpt.Reverse();
                                        }
                                    }
                                    mainVisited.AddRange(mpt);
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            foreach (var ppt in mpt)
                                            {
                                                foreach (var kv in ctx.BranchDic)
                                                {
                                                    if (kv.Key.Equals(ppt))
                                                    {
                                                        foreach (var v in kv.Value)
                                                        {
                                                            foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                            {
                                                                var info = (MLeaderInfo)pt.UserData;
                                                                if (info.Text == THESAURUSDEPLORE)
                                                                {
                                                                    info.Text = vm.Prefix + ++k;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            return;
                                        }
                                    }
                                }
                                foreach (var _mpt in ctx.SubPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    {
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            var i = mainVisited.IndexOf(first);
                                            var j = mainVisited.IndexOf(last);
                                            var shouldReverse = INTRAVASCULARLY;
                                            if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (first._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                shouldReverse = INTRAVASCULARLY;
                                            }
                                            else if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (shouldReverse)
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                    }
                                    if (mpt.Count > THESAURUSSTAMPEDE)
                                    {
                                        if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                        {
                                            foreach (var ppt in mpt)
                                            {
                                                foreach (var kv in ctx.BranchDic)
                                                {
                                                    if (kv.Key.Equals(ppt))
                                                    {
                                                        foreach (var v in kv.Value)
                                                        {
                                                            foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                            {
                                                                var info = (MLeaderInfo)pt.UserData;
                                                                if (info.Text == THESAURUSDEPLORE)
                                                                {
                                                                    info.Text = vm.Prefix + ++k;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                            main();
                        }
                        break;
                    case INTROPUNITIVENESS:
                        {
                            var mainVisited = new List<Point3dEx>();
                            {
                                var k = vm.StartNum - THESAURUSHOUSING;
                                foreach (var mpt in ctx.MainPathList)
                                {
                                    foreach (var ppt in mpt)
                                    {
                                        mainVisited.Add(ppt);
                                        foreach (var kv in ctx.BranchDic)
                                        {
                                            if (kv.Key.Equals(ppt))
                                            {
                                                foreach (var v in kv.Value)
                                                {
                                                    foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                    {
                                                        var info = (MLeaderInfo)pt.UserData;
                                                        if (info.Text == THESAURUSDEPLORE)
                                                        {
                                                            info.Text = vm.Prefix + ++k;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            {
                                var prefix = THESAURUSDEPLORE;
                                foreach (var _mpt in ctx.SubPathList)
                                {
                                    var mpt = _mpt.ToList();
                                    {
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            var i = mainVisited.IndexOf(first);
                                            var j = mainVisited.IndexOf(last);
                                            var shouldReverse = INTRAVASCULARLY;
                                            if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                            {
                                                shouldReverse = THESAURUSOBSTINACY;
                                            }
                                            if (shouldReverse)
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                    }
                                    var k = THESAURUSSTAMPEDE;
                                    if (mpt.Count > THESAURUSHOUSING)
                                    {
                                        var first = mpt.First();
                                        var last = mpt.Last();
                                        foreach (var ppt in new Point3dEx[] { first, last })
                                        {
                                            ctx.FireHydrantSystemIn.MarkList.TryGetValue(ppt, out string v);
                                            if (v != null)
                                            {
                                                var m = Regex.Match(v, QUOTATIONPERISTALTIC);
                                                if (m.Success)
                                                {
                                                    prefix = m.Groups[THESAURUSHOUSING].Value.ToLower();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var ppt in mpt)
                                    {
                                        foreach (var kv in ctx.BranchDic)
                                        {
                                            if (kv.Key.Equals(ppt))
                                            {
                                                foreach (var v in kv.Value)
                                                {
                                                    foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                    {
                                                        var info = (MLeaderInfo)pt.UserData;
                                                        if (info.Text == THESAURUSDEPLORE)
                                                        {
                                                            info.Text = vm.Prefix + prefix + ++k;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case QUOTATIONEDIBLE:
                        {
                            void main()
                            {
                                var mainVisited = new List<Point3dEx>();
                                {
                                    var k = vm.StartNum - THESAURUSHOUSING;
                                    foreach (var _mpt in ctx.MainPathList)
                                    {
                                        var mpt = _mpt.ToList();
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            var last = mpt.Last();
                                            if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                mpt.Reverse();
                                            }
                                        }
                                        mainVisited.AddRange(mpt);
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                foreach (var ppt in mpt)
                                                {
                                                    foreach (var kv in ctx.BranchDic)
                                                    {
                                                        if (kv.Key.Equals(ppt))
                                                        {
                                                            foreach (var v in kv.Value)
                                                            {
                                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                                {
                                                                    var info = (MLeaderInfo)pt.UserData;
                                                                    if (info.Text == THESAURUSDEPLORE)
                                                                    {
                                                                        info.Text = vm.Prefix + ++k;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                                {
                                    var prefix = THESAURUSDEPLORE;
                                    foreach (var _mpt in ctx.SubPathList)
                                    {
                                        var mpt = _mpt.ToList();
                                        {
                                            if (mpt.Count > THESAURUSHOUSING)
                                            {
                                                var first = mpt.First();
                                                var last = mpt.Last();
                                                var i = mainVisited.IndexOf(first);
                                                var j = mainVisited.IndexOf(last);
                                                var shouldReverse = INTRAVASCULARLY;
                                                if (i >= THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                                {
                                                    if (i > j) shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                else if (i < THESAURUSSTAMPEDE && j >= THESAURUSSTAMPEDE)
                                                {
                                                    shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                if (first._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                                {
                                                    shouldReverse = INTRAVASCULARLY;
                                                }
                                                else if (last._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                                {
                                                    shouldReverse = THESAURUSOBSTINACY;
                                                }
                                                if (shouldReverse)
                                                {
                                                    mpt.Reverse();
                                                }
                                            }
                                        }
                                        if (mpt.Count > THESAURUSHOUSING)
                                        {
                                            var first = mpt.First();
                                            var last = mpt.Last();
                                            foreach (var ppt in new Point3dEx[] { first, last })
                                            {
                                                ctx.FireHydrantSystemIn.MarkList.TryGetValue(ppt, out string v);
                                                if (v != null)
                                                {
                                                    var m = Regex.Match(v, QUOTATIONPERISTALTIC);
                                                    if (m.Success)
                                                    {
                                                        prefix = m.Groups[THESAURUSHOUSING].Value.ToLower();
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (mpt.Count > THESAURUSSTAMPEDE)
                                        {
                                            if (mpt.First()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()) || mpt.Last()._pt.ToGRect(HYPERDISYLLABLE).ToPolygon().Intersects(selPt.ToNTSPoint()))
                                            {
                                                var k = vm.StartNum - THESAURUSHOUSING;
                                                foreach (var ppt in mpt)
                                                {
                                                    foreach (var kv in ctx.BranchDic)
                                                    {
                                                        if (kv.Key.Equals(ppt))
                                                        {
                                                            foreach (var v in kv.Value)
                                                            {
                                                                foreach (var pt in ptsf(v._pt.ToGRect(THESAURUSHOUSING).ToPolygon()))
                                                                {
                                                                    var info = (MLeaderInfo)pt.UserData;
                                                                    if (info.Text == THESAURUSDEPLORE)
                                                                    {
                                                                        info.Text = vm.Prefix + prefix + ++k;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            main();
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
                foreach (var info in mlInfos)
                {
                    if (!string.IsNullOrWhiteSpace(info.Text))
                    {
                        foreach (var mlGeo in mlGeosf(info.BasePoint.ToGRect(HYPERDISYLLABLE).ToPolygon()))
                        {
                            adb.Element<Entity>(((Entity)mlGeo.UserData).ObjectId, THESAURUSOBSTINACY).Erase();
                        }
                        DrawMLeader(info.Text, info.BasePoint, info.BasePoint.OffsetXY(HYPERDISYLLABLE, QUOTATIONWITTIG));
                    }
                }
            }
        }
    }
    public class MLeaderInfo
    {
        public Point3d BasePoint;
        public string Text;
        public string Note;
        public static MLeaderInfo Create(Point2d pt, string text) => Create(pt.ToPoint3d(), text);
        public static MLeaderInfo Create(Point3d pt, string text)
        {
            return new MLeaderInfo() { BasePoint = pt, Text = text };
        }
    }
}