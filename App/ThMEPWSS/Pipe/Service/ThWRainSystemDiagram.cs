namespace ThMEPWSS.FlatDiagramNs
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
    public class MLeaderInfo
    {
        public Point3d BasePoint;
        public string Text;
        public static MLeaderInfo Create(Point2d pt, string text) => Create(pt.ToPoint3d(), text);
        public static MLeaderInfo Create(Point3d pt, string text)
        {
            return new MLeaderInfo() { BasePoint = pt, Text = text };
        }
    }
    public static class FlatDiagramService
    {
        public static void DrawRainFlatDiagram()
        {
            var range = CadCache.TryGetRange();
            if (range == null)
            {
                Active.Editor.WriteMessage(THESAURUSIMPRESSION);
                return;
            }
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                LayerTools.AddLayer(adb.Database, MLeaderLayer);
                if (adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer).Any())
                {
                    var r = MessageBox.Show(THESAURUSWHITEN, COLLABORATIVELY, MessageBoxButtons.YesNo);
                    if (r == DialogResult.No) return;
                }
                var mlPts = new List<Point>(PHOTOSENSITIZING);
                foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
                {
                    var pt=e.GetFirstVertex(NARCOTRAFICANTE).ToNTSPoint();
                    pt.UserData = e;
                    mlPts.Add(pt);
                }
                ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CollectRainGeoData(range, adb, out List<StoreyInfo> storeysItems, out ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData);
                var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CreateRainDrawingData(adb, geoData, UNTRACEABLENESS);
                exInfo.drDatas = drDatas;
                exInfo.geoData = geoData;
                Dispose();
                var f = GeoFac.CreateIntersectsSelector(geoData.WLines.Select(x => x.Buffer(THESAURUSFACTOR)).ToList());
                var pts = mlPts.Where(pt => f(pt).Any()).ToList();
                foreach (var pt in pts)
                {
                    adb.Element<Entity>(((Entity)pt.UserData).ObjectId, THESAURUSSEMBLANCE).Erase();
                }
                DrawFlatDiagram(exInfo);
                FlushDQ();
            }
        }
        public static void DrawDrainageFlatDiagram()
        {
            var range = CadCache.TryGetRange();
            if (range == null)
            {
                Active.Editor.WriteMessage(THESAURUSIMPRESSION);
                return;
            }
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                LayerTools.AddLayer(adb.Database, MLeaderLayer);
                if (adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer).Any())
                {
                    var r = MessageBox.Show(THESAURUSWHITEN, COLLABORATIVELY, MessageBoxButtons.YesNo);
                    if (r == DialogResult.No) return;
                }
                var mlPts = new List<Point>(PHOTOSENSITIZING);
                foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
                {
                    var pt = e.GetFirstVertex(NARCOTRAFICANTE).ToNTSPoint();
                    pt.UserData = e;
                    mlPts.Add(pt);
                }
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectDrainageGeoData(range, adb, out List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems, out ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData);
                var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CreateDrainageDrawingData(geoData, UNTRACEABLENESS);
                exInfo.drDatas = drDatas;
                exInfo.geoData = geoData;
                Dispose();
                var f = GeoFac.CreateIntersectsSelector(geoData.DLines.Select(x => x.Buffer(THESAURUSFACTOR)).ToList());
                var pts = mlPts.Where(pt => f(pt).Any()).ToList();
                foreach (var pt in pts)
                {
                    adb.Element<Entity>(((Entity)pt.UserData).ObjectId, THESAURUSSEMBLANCE).Erase();
                }
                DrawFlatDiagram(exInfo);
                FlushDQ();
            }
        }
        public static void DrawFlatDiagram(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo);
        }
        public static void DrawFlatDiagram(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo);
        }
        public static readonly PreparedGeometryFactory PreparedGeometryFactory = new PreparedGeometryFactory();
        public static (Func<Geometry, List<T>>, Action<T>) CreateIntersectsSelectorEngine<T>() where T : Geometry
        {
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>();
            return (geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            }, geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                engine.Insert(geo.EnvelopeInternal, geo);
            }
            );
        }
        public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
        }
        public static void DrawBackToFlatDiagram(List<ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.StoreysItem> storeysItems, ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData, List<ThMEPWSS.ReleaseNs.RainSystemNs.RainDrawingData> drDatas, ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
        {
            var mlInfos = new List<MLeaderInfo>(PHOTOSENSITIZING);
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = NARCOTRAFICANTE; si < cadDatas.Count; si++)
            {
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                var item = cadDatas[si];
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSNETHER).ToList();
                var wlinesGeosf = F(wlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                var cpsf = F(item.CondensePipes);
                var fdsf = F(item.FloorDrains);
                foreach (var wlinesGeo in wlinesGeos)
                {
                    var ok = UNTRACEABLENESS;
                    if (!ok)
                    {
                        foreach (var seg in GeoFac.GetLines(wlinesGeo))
                        {
                            if ((seg.Length > THESAURUSINDUSTRY && seg.IsHorizontalOrVertical(THESAURUSCONSUL)) || seg.Length > THESAURUSINTENTIONAL || cpsf(seg.ToLineString()).Count > NARCOTRAFICANTE || fdsf(seg.ToLineString()).Count > NARCOTRAFICANTE)
                            {
                                mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSATAVISM));
                            }
                        }
                        ok = THESAURUSSEMBLANCE;
                    }
                }
            }
            {
                var pts = mlInfos.Select(x => { var pt = x.BasePoint.ToNTSPoint(); pt.UserData = x; return pt; }).ToList();
                var ptsf = GeoFac.CreateIntersectsSelector(pts);
                void draw(string text, GRect r)
                {
                    if (r.IsValid)
                    {
                        foreach (var pt in ptsf(r.ToPolygon()))
                        {
                            ((MLeaderInfo)pt.UserData).Text = text;
                        }
                    }
                }
                var file = CadCache.CurrentFile;
                var name = System.IO.Path.GetFileName(file);
                if (name.Contains(THESAURUSBROACH))
                {
                    draw(THESAURUSOVERCHARGE, new GRect(THESAURUSMODIFICATION, THESAURUSDICTIONARY, THESAURUSOBSOLETE, SUPERESSENTIALLY));
                }
            }
            foreach (var info in mlInfos)
            {
                if (!string.IsNullOrWhiteSpace(info.Text)) DrawMLeader(info.Text, info.BasePoint, info.BasePoint.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
            }
        }
        public const int ADRENOCORTICOTROPHIC = 1;
        public const int THESAURUSFACTOR = 5;
        public const int PHOTOGONIOMETER = 2;
        public const int NARCOTRAFICANTE = 0;
        public const double THESAURUSDEPOSIT = 10e5;
        public const bool UNTRACEABLENESS = false;
        public const bool THESAURUSSEMBLANCE = true;
        public const int THESAURUSSENILE = 40;
        public const int PHOTOSENSITIZING = 4096;
        public const int THESAURUSINDUSTRY = 100;
        public const string THESAURUSREDOUND = "";
        public const int UNDERACHIEVEMENT = 50;
        public const int THESAURUSCONSUL = 10;
        public const int THESAURUSITINERANT = 90;
        public const int HYDROSTATICALLY = 500;
        public const int AUTHORITARIANISM = 180;
        public const int THESAURUSINTENTIONAL = 300;
        public const string SPLANCHNOPLEURE = "DN100";
        public const int THESAURUSENTREAT = 200;
        public const double QUOTATIONEXOPHTHALMIC = .01;
        public const string THESAURUSTRAFFIC = "TH-STYLE3";
        public const int THESAURUSNETHER = 15;
        public const string THESAURUSATAVISM = "DN50";
        public const string THESAURUSOVERCHARGE = "DN75";
        public const int THESAURUSEVENTUALLY = 270;
        public const string THESAURUSBROACH = "10#、11#";
        public const int THESAURUSMODIFICATION = 269510;
        public const int THESAURUSDICTIONARY = 436614;
        public const int THESAURUSOBSOLETE = 269941;
        public const int SUPERESSENTIALLY = 436937;
        public const string VERIFICATIONISM = "W-辅助-管径";
        public const double THESAURUSSTRENGTHEN = 5.01;
        public const double THESAURUSOBSTREPEROUS = 10.01;
        public const string THESAURUSPUBLISH = "5#7#";
        public const int MADAGASCARIENSIS = 591001;
        public const int ARCHITECTONICUS = 627292;
        public const int COUNTERCURRENTS = 591076;
        public const int THESAURUSGALVANIZE = 627334;
        public const int APOPHTHEGMATICAL = 623150;
        public const int THESAURUSPRECURSOR = 625041;
        public const int THESAURUSPOSSESSIVE = 623218;
        public const int THESAURUSABSCESS = 625110;
        public const int INTERCONNECTEDNESS = 590637;
        public const int THESAURUSAVAILABLE = 625874;
        public const int THESAURUSCONCEPTION = 590787;
        public const int SURREPTITIOUSNESS = 625980;
        public const int SUPEREXCELLENTLY = 590955;
        public const int SUPERABUNDANTLY = 626349;
        public const int THESAURUSSUBVERT = 591237;
        public const int THESAURUSDECADENCE = 626593;
        public const int THESAURUSDISSENTIENT = 590662;
        public const int THESAURUSVARNISH = 626482;
        public const int MISCHIEVOUSNESS = 590880;
        public const int THESAURUSCADENCE = 626703;
        public const int THESAURUSJANGLE = 590682;
        public const int THESAURUSCARELESS = 626494;
        public const int THESAURUSSCRIPT = 590852;
        public const int THESAURUSSOCIALIZE = 626709;
        public const int THESAURUSIMMATERIAL = 590949;
        public const int THESAURUSCOMEUPPANCE = 626992;
        public const int THESAURUSINVOICE = 591118;
        public const int THESAURUSCONVERSANT = 627133;
        public const int QUOTATION1BSTATUS = 590528;
        public const int ALTWEIBERSOMMER = 626858;
        public const int CRYSTALLOGENESIS = 590688;
        public const int THESAURUSREFERENCE = 627047;
        public const int THESAURUSCURATIVE = 590711;
        public const int THESAURUSRACISM = 627141;
        public const int THESAURUSINHIBIT = 590761;
        public const int THESAURUSFINERY = 627215;
        public const int QUOTATIONAUGUSTAL = 590897;
        public const int THESAURUSINTENSIFY = 627236;
        public const int UNSUBSTANTIATED = 590966;
        public const int THESAURUSCHAPTER = 627343;
        public const int THESAURUSVETERAN = 590951;
        public const int THESAURUSACCEPTANCE = 629530;
        public const int OPISTOGNATHIDAE = 591225;
        public const int THESAURUSDEMARCATION = 629688;
        public const int THESAURUSGULLIBLE = 590619;
        public const int THESAURUSFORCIBLE = 625725;
        public const int THESAURUSINTRINSIC = 590864;
        public const int THESAURUSPREDICTABLE = 625993;
        public const int THESAURUSCOMMUNICATE = 595463;
        public const int THESAURUSINTRUSIVE = 625862;
        public const int THESAURUSLEGATION = 595696;
        public const int QUOTATIONGRANULOMA = 626092;
        public const int THESAURUSTHREADBARE = 595103;
        public const int DRAMATISTICALLY = 626652;
        public const int THESAURUSCOINCIDENCE = 595269;
        public const int THESAURUSMONSTROSITY = 626830;
        public const int THESAURUSBOUNDARY = 595602;
        public const int TRIMETHYLBENZENE = 626860;
        public const int THESAURUSUNEASY = 595760;
        public const int IRRESISTIBILITY = 627002;
        public const int THESAURUSDEPLORABLE = 595284;
        public const int THESAURUSMORBID = 627833;
        public const int THESAURUSBLENCH = 595381;
        public const int THESAURUSLENGTH = 627972;
        public const int QUOTATIONNEMEAN = 595433;
        public const int EXPERIMENTATION = 626658;
        public const int THESAURUSSUBJECT = 595738;
        public const int THESAURUSAUTONOMY = 626840;
        public const int THESAURUSCONSTRAINT = 627463;
        public const int ETHOXYPHENYLUREA = 595254;
        public const int MULTILATERALISTS = 627656;
        public const int THESAURUSTREPIDATION = 595065;
        public const int CHF2RORCF2RCHFCL = 629346;
        public const int THESAURUSMANTLE = 595336;
        public const int THESAURUSCHUBBY = 629661;
        public const int THESAURUSSTRATEGY = 595583;
        public const int THESAURUSUNBROKEN = 631415;
        public const int THESAURUSSACKCLOTH = 595753;
        public const int THESAURUSUNMANNERLY = 631628;
        public const int THESAURUSSNOBBERY = 596911;
        public const int THESAURUSSORDID = 629791;
        public const int THESAURUSOFFHAND = 597084;
        public const int THESAURUSHERMIT = 630011;
        public const int AFFECTIONATENESS = 596692;
        public const int THESAURUSBANQUET = 630720;
        public const int THESAURUSIMPOSING = 596985;
        public const int THESAURUSLAMENTATION = 630913;
        public const int QUOTATIONLETTERS = 603749;
        public const int OLIGOMENORRHOEA = 626864;
        public const int CHARACTERISTICUS = 603983;
        public const int FORESIGHTEDNESS = 627088;
        public const int PREPONDERATINGLY = 605491;
        public const int THESAURUSQUARREL = 628227;
        public const int THESAURUSTRANSGRESS = 605682;
        public const int CARBOXYANTHRANILIC = 628413;
        public const int THESAURUSHUNGRY = 606823;
        public const int THESAURUSDEVILISH = 631807;
        public const int THESAURUSCHRISTEN = 607036;
        public const int THESAURUSOFFENCE = 632094;
        public const int ALSOSPASMATICAL = 603925;
        public const int THESAURUSUMPIRE = 624066;
        public const int COSTERMONGERDOM = 604173;
        public const int QUOTATIONGLANDULAR = 624263;
        public const int ALSOORCHESTRINA = 604227;
        public const int HEILSGESCHICHTE = 624615;
        public const int PHYSIOTHERAPIST = 604361;
        public const int THESAURUSOUTPOURING = 624722;
        public const int THESAURUSDECLAMATION = 603920;
        public const int THESAURUSDIDACTIC = 624932;
        public const int THESAURUSDEMURE = 604010;
        public const int THESAURUSEXPORT = 625007;
        public const int THERMOGRAVIMETRY = 604164;
        public const int THESAURUSCOLLIDE = 625060;
        public const int UNCOMPOUNDEDNESS = 604230;
        public const int MYRISTICAEFORMIS = 625100;
        public const int THESAURUSFIGURATIVE = 604116;
        public const int SENTIMENTALISTS = 624811;
        public const int METHYLENEDIOXYMETHAMPHETAMINE = 604191;
        public const int THESAURUSLIBERATOR = 624902;
        public const int MULTIPLICATIONAL = 604261;
        public const int THESAURUSOMISSION = 604320;
        public const int THESAURUSWAGGLE = 604259;
        public const int THESAURUSBURNING = 625464;
        public const int THESAURUSREGARDING = 604311;
        public const int THESAURUSPHOTOGRAPH = 625508;
        public const int THESAURUSMUSHROOM = 604251;
        public const int THESAURUSVICTORIOUS = 625941;
        public const int THESAURUSOPERATE = 604335;
        public const int STERNOPTYCHIDAE = 626020;
        public const int THESAURUSSPONTANEOUS = 605831;
        public const int THESAURUSLACERATE = 627662;
        public const int THESAURUSDILATE = 606084;
        public const int THESAURUSABSTENTION = 627889;
        public const int ALSOHEAVENWARDS = 607491;
        public const int THESAURUSTECHNIQUE = 631714;
        public const int THESAURUSPALPABLE = 607594;
        public const int DECONSTRUCTIONIST = 631875;
        public const int THESAURUSINFORMAL = 608894;
        public const int THESAURUSBANDAGE = 623984;
        public const int THESAURUSDEMORALIZE = 609053;
        public const int THESAURUSVICTIMIZE = 624219;
        public const int THESAURUSDWELLING = 608746;
        public const int THESAURUSEXACTLY = 624444;
        public const int IMPERISHABLENESS = 608909;
        public const int IRREVOCABLENESS = 624614;
        public const int DISPASSIONATENESS = 608739;
        public const int THESAURUSATHLETIC = 625027;
        public const int THESAURUSOUTLOOK = 608864;
        public const int EXPRESSIONLESSLY = 625193;
        public const int THESAURUSSCENTED = 608798;
        public const int THESAURUSSCRUFFY = 625440;
        public const int KHARTOPHULAKION = 608887;
        public const int ARKHITEKTONIKOS = 625576;
        public const int STRENGTHLESSNESS = 608700;
        public const int THESAURUSOPPOSING = 629558;
        public const int QUOTATIONWATTEAU = 608959;
        public const int THESAURUSUNCONDITIONAL = 630100;
        public const int APPREHENSIVENESS = 608980;
        public const int UNENDURABLENESS = 625118;
        public const int RECOMBINATIONALLY = 609054;
        public const int IRRECUPERABILIS = 625257;
        public const int THESAURUSDIATRIBE = 609805;
        public const int CONTROLLABILITY = 623932;
        public const int CONCENTRATIVENESS = 610026;
        public const int THESAURUSVIRILE = 624217;
        public const int THESAURUSBELLOW = 609966;
        public const int THESAURUSDEDUCE = 624491;
        public const int THESAURUSLUKEWARM = 610113;
        public const int IRRESISTIBLENESS = 624618;
        public const int THESAURUSUPPERMOST = 609839;
        public const int THESAURUSCOMPREHEND = 625106;
        public const int THESAURUSUNDISCIPLINED = 609936;
        public const int THESAURUSKINDLY = 625263;
        public const int QUOTATIONANTIMONIOUS = 609974;
        public const int QUOTATIONSAMIAN = 625038;
        public const int THESAURUSBLIZZARD = 610146;
        public const int INDISCRIMINATELY = 625166;
        public const int THESAURUSWEIGHT = 609999;
        public const int TERMINALIZATION = 610088;
        public const int THESAURUSEXHIBITION = 625556;
        public const int THESAURUSCARNAL = 609952;
        public const int THESAURUSCOALESCE = 629651;
        public const int EXCREMENTITIOUS = 610230;
        public const int THESAURUSPOISONOUS = 630002;
        public const int ULTRAMONTANISME = 614633;
        public const int QUOTATION1AFILL = 623955;
        public const int THESAURUSAPPLAUSE = 614942;
        public const int PSYCHOPATHOLOGIST = 624270;
        public const int QUOTATIONTOUCHED = 614530;
        public const int GYNANDROMORPHOUS = 624571;
        public const int QUOTATIONTHEORY = 614666;
        public const int THESAURUSABSORPTION = 624765;
        public const int QUOTATIONRADIOLARIAN = 614899;
        public const int THESAURUSEXPIATE = 624933;
        public const int THESAURUSSURPRISED = 614964;
        public const int DISRESPECTFULNESS = 625011;
        public const int THESAURUSEXULTANT = 614660;
        public const int IMPROVIDENTNESS = 625052;
        public const int THESAURUSCOMPASSION = 614720;
        public const int THERMOELECTRICITY = 625122;
        public const int THESAURUSANTAGONIZE = 614678;
        public const int ASTROPHYSICALLY = 624752;
        public const int THESAURUSOPULENCE = 614799;
        public const int THESAURUSOPTION = 624912;
        public const int THESAURUSKITCHEN = 614563;
        public const int QUOTATIONPROPOSITIONAL = 625051;
        public const int TRAUMATOTROPISM = 614607;
        public const int THESAURUSPROMULGATE = 625107;
        public const int THESAURUSATTEND = 614543;
        public const int THESAURUSFOOTSTEP = 625387;
        public const int THESAURUSBLACKOUT = 614655;
        public const int PALAEENCEPHALON = 625542;
        public const int THESAURUSCONFRONT = 625921;
        public const int THESAURUSDICTATORIAL = 614638;
        public const int OCCLUSOGINGIVAL = 626049;
        public const int THESAURUSCREEPY = 612638;
        public const int THESAURUSTUNNEL = 627764;
        public const int QUOTATIONBREWSTER = 612925;
        public const int CONSUETUDINARIUS = 628058;
        public const int THESAURUSINESCAPABLE = 610920;
        public const int ANTIPESTILENTIAL = 631821;
        public const int THESAURUSCLIMATE = 611141;
        public const int THESAURUSEMBRYONIC = 632083;
        public const int THESAURUSREVIVAL = 615012;
        public const int THESAURUSSABOTAGE = 626881;
        public const int THESAURUSPEDESTAL = 615141;
        public const int BIOLUMINESCENCE = 627011;
        public const int THESAURUSCOUNSELLOR = 614696;
        public const int THESAURUSDORMANT = 626889;
        public const int THESAURUSCONCISE = 614818;
        public const int THESAURUSFRUITION = 627006;
        public const int DEPARTMENTALIZE = 613028;
        public const int THESAURUSSPOTLIGHT = 628394;
        public const int ALPHABETIZATION = 613245;
        public const int THESAURUSVIRTUE = 628596;
        public const int THESAURUSENORMOUS = 611573;
        public const int THESAURUSFASHIONABLE = 632013;
        public const int THESAURUSENDORSEMENT = 611697;
        public const int THESAURUSTRANSITION = 632144;
        public const int THESAURUSINGRESS = 618877;
        public const int THESAURUSDEFLECT = 626843;
        public const int THESAURUSFIDDLE = 619127;
        public const int THESAURUSIMPENDING = 627029;
        public const int THESAURUSAPPRECIATION = 622828;
        public const int THESAURUSDENOUEMENT = 626792;
        public const int THESAURUSGRANDMOTHER = 622989;
        public const int SEMIMICROANALYSIS = 627068;
        public const int PREMILLENNIALIST = 624674;
        public const int THESAURUSELICIT = 628372;
        public const int ARISTOCRATICALLY = 624845;
        public const int TRISYLLABICALLY = 628566;
        public const int PROGNOSTICATING = 626140;
        public const int ULTRACENTRIFUGING = 631980;
        public const int THESAURUSPERTAIN = 626292;
        public const int THESAURUSEXECUTIVE = 632169;
        public const int THESAURUSDESPOIL = 622916;
        public const int THESAURUSINNOCENCE = 623988;
        public const int THESAURUSCATARACT = 623164;
        public const int THESAURUSBEATIFIC = 624250;
        public const int QUOTATIONQUEBEC = 623226;
        public const int THESAURUSAFFECTING = 624626;
        public const int THESAURUSMEASURE = 623411;
        public const int THESAURUSEXPLOIT = 624783;
        public const int THESAURUSFORTITUDE = 622859;
        public const int HOLOCRYSTALLINE = 624878;
        public const int THESAURUSFLATTERY = 623021;
        public const int ELECTROENCEPHALOGRAPH = 625121;
        public const int PYROMETAMORPHISM = 623125;
        public const int THESAURUSCONSUMPTION = 624781;
        public const int THESAURUSHALLMARK = 623181;
        public const int THESAURUSCONTEST = 624927;
        public const int THESAURUSOPACITY = 623272;
        public const int THESAURUSFACTUAL = 625056;
        public const int SUPEREXCELLENCE = 623317;
        public const int INDISPENSABLENESS = 625097;
        public const int ACRIMONIOUSNESS = 623258;
        public const int THESAURUSREFERENDUM = 625637;
        public const int THESAURUSSCEPTICAL = 623328;
        public const int THESAURUSHAPPILY = 625709;
        public const int LATITUDINARIANISM = 624928;
        public const int THESAURUSINDEMNITY = 627780;
        public const int THESAURUSALTERATION = 625274;
        public const int THESAURUSTRANSCENDENT = 628045;
        public const int THESAURUSCRIPPLE = 626762;
        public const int QUOTATIONCOSMOLOGICAL = 631742;
        public const int NOVAESEELANDIAE = 627031;
        public const int THESAURUSSLUMBER = 632040;
        public const int THESAURUSDISCOURTEOUS = 627864;
        public const int THESAURUSPARADOX = 623968;
        public const int THESAURUSINFLUENCE = 628230;
        public const int THESAURUSFRUITFUL = 624239;
        public const int THESAURUSNAMELY = 627750;
        public const int THESAURUSTHRASH = 624422;
        public const int THESAURUSFOREWARN = 627948;
        public const int THESAURUSORIGINALITY = 624620;
        public const int UNADVISABLENESS = 627958;
        public const int THESAURUSINDELICATE = 625139;
        public const int THESAURUSANSWERABLE = 628061;
        public const int MICROAEROPHILIC = 625272;
        public const int THESAURUSSPIRIT = 628749;
        public const int THESAURUSABORIGINAL = 623933;
        public const int THESAURUSEUPHEMISTIC = 629070;
        public const int THESAURUSOVERLOAD = 624225;
        public const int PLAINSPOKENNESS = 628937;
        public const int THESAURUSCOMPATRIOT = 624467;
        public const int MALAPPORTIONMENT = 629139;
        public const int THESAURUSBEDRIDDEN = 628783;
        public const int HEXYLRESORCINOL = 625084;
        public const int THESAURUSMANUFACTURER = 628946;
        public const int THESAURUSDEFORMITY = 625262;
        public const int THESAURUSPRECONCEPTION = 627791;
        public const int THESAURUSAUSTERE = 625034;
        public const int THESAURUSSEVERE = 627906;
        public const int MISTRUSTFULNESS = 625184;
        public const int UNDERNOURISHMENT = 627797;
        public const int THESAURUSBOILING = 625460;
        public const int THESAURUSSKIMPY = 627874;
        public const int THESAURUSPROFUSE = 625567;
        public const int THESAURUSSINGER = 627755;
        public const int THESAURUSNECESSARILY = 629775;
        public const int THESAURUSLESSON = 627926;
        public const int THESAURUSANCESTOR = 629936;
        public const int QUOTATIONADIPOSE = 628957;
        public const int THESAURUSENCOURAGE = 629154;
        public const int THESAURUSSEESAW = 625148;
        public const int INDISCIPLINABLE = 629005;
        public const int INCONSISTENCIES = 625480;
        public const int THESAURUSCOMPETE = 629072;
        public const int THESAURUSPERSUASION = 625557;
        public const int DISILLUSIONMENT = 628871;
        public const int THESAURUSFORTUNE = 629666;
        public const int UNPROPORTIONATE = 629207;
        public const int THESAURUSTHICKNESS = 629976;
        public const int THESAURUSIRKSOME = 633685;
        public const int DISINFLATIONARY = 624036;
        public const int THESAURUSHOLLOW = 633948;
        public const int THESAURUSRATION = 624253;
        public const int THESAURUSHAGGARD = 633438;
        public const int THESAURUSHABITAT = 624483;
        public const int PRESCRIPTIVENESS = 633767;
        public const int THESAURUSCONVOY = 624772;
        public const int THESAURUSRELISH = 633662;
        public const int DISFRANCHISEMENT = 625047;
        public const int CLASSIFICATIONS = 633715;
        public const int THESAURUSMAKESHIFT = 625117;
        public const int THESAURUSINEXPRESSIVE = 633907;
        public const int THESAURUSTRAINER = 624926;
        public const int THESAURUSDEJECTED = 633978;
        public const int THESAURUSCHUCKLE = 625000;
        public const int THESAURUSLIBERAL = 633850;
        public const int VZAIMOPOMOSHCHI = 626853;
        public const int UNTREATABLENESS = 634046;
        public const int QUOTATIONWALDEYER = 627076;
        public const int QUOTATIONLYNDON = 630896;
        public const int PRESTIGIOUSNESS = 631832;
        public const int AEQUIPONDERATUS = 631007;
        public const int THESAURUSLOQUACIOUS = 632054;
        public const int THESAURUSHOODLUM = 633676;
        public const int INTERPERSONALLY = 624767;
        public const int THESAURUSENDURANCE = 633757;
        public const int SLANTINDICULARLY = 624894;
        public const int THESAURUSBETTERMENT = 633567;
        public const int THESAURUSAPROPOS = 625042;
        public const int THESAURUSINFORMATIVE = 633627;
        public const int THESAURUSSPITEFUL = 633547;
        public const int THESAURUSHEAVENLY = 625651;
        public const int THESAURUSGLUTTONOUS = 633618;
        public const int PROPIONALDEHYDE = 625731;
        public const int THESAURUSMEADOW = 631817;
        public const int COMBUSTIBLENESS = 627691;
        public const int THESAURUSWHIMPER = 632042;
        public const int INDISTINGUISHABLY = 632216;
        public const int THESAURUSCONFORMIST = 628267;
        public const int THESAURUSCOSMETIC = 632347;
        public const int THESAURUSFAMILIARITY = 628360;
        public const int RADIOSENSITIZER = 630906;
        public const int THESAURUSREJECTION = 631858;
        public const int THESAURUSOVERTHROW = 631034;
        public const int THESAURUSEQUANIMITY = 632004;
        public const int THESAURUSPARTLY = 630193;
        public const int ALSOTHITHERWARDS = 631605;
        public const int THESAURUSCUNNING = 630432;
        public const int THESAURUSTENANT = 631982;
        public const int PSYCHOACOUSTICIAN = 642130;
        public const int APOPHTHEGMATICALLY = 625679;
        public const int THESAURUSRESERVATION = 642501;
        public const int HIEROGRAMMATEUS = 626197;
        public const int THESAURUSLITURGY = 642601;
        public const int THESAURUSENRAPTURE = 626596;
        public const int THESAURUSMONOTONOUS = 642854;
        public const int QUOTATION1ABLACK = 626867;
        public const int THESAURUSINVOLVE = 642203;
        public const int THESAURUSCONDUCT = 626673;
        public const int THESAURUSINGENIOUS = 642387;
        public const int EUAGGELIZESTHAI = 626838;
        public const int THESAURUSDISRUPTION = 642635;
        public const int THESAURUSCOURTSHIP = 627449;
        public const int THESAURUSAPPROVE = 642834;
        public const int THESAURUSOUTRAGEOUS = 642609;
        public const int THESAURUSARCADE = 629266;
        public const int THESAURUSARCHITECT = 642842;
        public const int UNCONVENTIONALISM = 629604;
        public const int THESAURUSRESTRAINED = 642751;
        public const int THESAURUSWARNING = 631333;
        public const int PHOTODYNAMICALLY = 642881;
        public const int THESAURUSCORRESPONDENCE = 631472;
        public const int QUOTATIONSEEING = 642465;
        public const int THESAURUSSATANIC = 627733;
        public const int QUOTATIONTOUJOURS = 642590;
        public const int QUOTATIONNITROUS = 627963;
        public const int THESAURUSUNDOUBTED = 642118;
        public const int OVERELABORATELY = 626898;
        public const int THESAURUSREFRACTORY = 642258;
        public const int THESAURUSCORONET = 596857;
        public const int THESAURUSACQUIT = 629720;
        public const int NEUROPSYCHOLOGIST = 597078;
        public const int THESAURUSMATRIMONIAL = 630660;
        public const int THESAURUSLIMBER = 596861;
        public const int THESAURUSESPOUSE = 630987;
        public const int REPRESENTATIVES = 647120;
        public const int THESAURUSEARTHLY = 625534;
        public const int INTERMARRIAGEABLE = 647429;
        public const int APPRECIATIVENESS = 626036;
        public const int THESAURUSNAVIGATION = 646679;
        public const int THESAURUSEFFIGY = 626401;
        public const int THESAURUSDOVETAIL = 646989;
        public const int UNPARLIAMENTARILY = 626641;
        public const int THESAURUSPREOCCUPATION = 647126;
        public const int ALSOTRIBUNITIAL = 627144;
        public const int THESAURUSPUBLICATION = 647164;
        public const int THESAURUSSAVIOUR = 627235;
        public const int THESAURUSDEVELOPMENT = 647186;
        public const int CRUMENOPHTHALMUS = 626885;
        public const int SUPERNUMERARIUS = 647337;
        public const int QUOTATIONISTRIAN = 627061;
        public const int THESAURUSHORRIFY = 647049;
        public const int THESAURUSUNINHABITED = 626527;
        public const int THESAURUSANNOYANCE = 647147;
        public const int THESAURUSEXPENSE = 626605;
        public const int AUTOTROPHICALLY = 646801;
        public const int QUOTATIONURINIFEROUS = 626977;
        public const int THESAURUSCRABBED = 646876;
        public const int PSEUDEPIGRAPHOUS = 627069;
        public const int PHANTASIESTÜCKE = 646811;
        public const int SEROEPIDEMIOLOGICAL = 627284;
        public const int THESAURUSIMPREGNABLE = 646861;
        public const int THESAURUSBASEMENT = 627335;
        public const int THESAURUSMULTITUDE = 646777;
        public const int THESAURUSCOFFER = 629471;
        public const int COMMUNICATIVELY = 646979;
        public const int THESAURUSRAGGED = 629740;
        public const int COLONIZATIONISTS = 594317;
        public const int THESAURUSDISTURB = 617353;
        public const int THESAURUSMANAGER = 594589;
        public const int THESAURUSEXCELLENCE = 617680;
        public const int THESAURUSREPORT = 600113;
        public const int QUOTATIONFUSIBLE = 616627;
        public const int CHIROGRAPHARIUS = 600391;
        public const int PROBABILISTICALLY = 616832;
        public const int THESAURUSSECRETION = 618645;
        public const int THESAURUSSUBSCRIPTION = 616601;
        public const int THESAURUSBATTERY = 618814;
        public const int SYMMETRICALNESS = 616820;
        public const int THESAURUSDONATE = 619021;
        public const int CONSTITUTIONALIST = 616604;
        public const int COUNTERDISTINCTION = 619209;
        public const int OVERDEVELOPMENT = 616848;
        public const int THERMODYNAMICALLY = 637465;
        public const int THESAURUSATTITUDE = 616602;
        public const int GLYCEROPHOSPHORIC = 637703;
        public const int COCCOLITHOPHORE = 616924;
        public const int ORTHOGENETICALLY = 637866;
        public const int THESAURUSZEALOTRY = 616743;
        public const int GENTLEWOMANLINESS = 638007;
        public const int FERROMAGNETICALLY = 616973;
        public const int MISCELLANEOUSNESS = 638023;
        public const int THESAURUSADJOURNMENT = 617933;
        public const int ELECTROCHROMISM = 638153;
        public const int THESAURUSREGARD = 618062;
        public const int THESAURUSDEATHLESS = 638113;
        public const int COCCIDIOIDOMYCOSIS = 618546;
        public const int PERICLYMENOIDES = 638382;
        public const int THESAURUSMISLAY = 618755;
        public const int INTERCOMMUNICARE = 643092;
        public const int THESAURUSSCULPTURE = 617116;
        public const int ALSOCONVALESCENCY = 643714;
        public const int THESAURUSCONTROL = 617721;
        public const int THESAURUSSWAMPY = 642753;
        public const int COMPETITIVENESS = 615494;
        public const int THESAURUSSPOUSE = 643028;
        public const int ANTIPHLOGISTINE = 615889;
        public const int THESAURUSCODDLE = 247836;
        public const int THESAURUSFACETIOUS = 437965;
        public const int THESAURUSLEARNED = 250159;
        public const int THESAURUSSCRATCH = 444257;
        public const int THESAURUSWOMANISH = 254304;
        public const int THESAURUSINORDINATE = 442727;
        public const int THESAURUSPENSIONER = 254509;
        public const int HORC6H4RNHRCH2COOH = 443622;
        public const int CONSPICUOUSNESS = 254241;
        public const int INTERCOMMUNICAT = 445220;
        public const int QUOTATIONMAGNETO = 254530;
        public const int THESAURUSBEHAVIOUR = 445544;
        public const int THESAURUSSTRANDED = 253628;
        public const int THESAURUSIMPERIOUS = 440566;
        public const int THESAURUSEXORDIUM = 254618;
        public const int THESAURUSJOINTLY = 441852;
        public const int THESAURUSCONTEMPLATIVE = 253020;
        public const int ECCLESIASTICIZE = 438468;
        public const int THESAURUSINACCURACY = 254151;
        public const int VERGISSMEINNICHT = 439407;
        public const int THESAURUSCOMPOSITE = 257332;
        public const int THESAURUSASTRINGENT = 443288;
        public const int THESAURUSEXCRESCENCE = 258750;
        public const int THESAURUSDOWNHEARTED = 444649;
        public const int THESAURUSMINDFUL = 255777;
        public const int QUOTATIONPHOTOCHEMICAL = 439534;
        public const int REPRESENTATIONALIST = 256674;
        public const int THESAURUSPETULANT = 441213;
        public const int THESAURUSDISINCENTIVE = 254665;
        public const int QUOTATIONNEWARK = 440033;
        public const int THESAURUSREALIZATION = 255289;
        public const int PHARMACOKINETICS = 440793;
        public const int THESAURUSEMBARRASSED = 255630;
        public const int SUPRACHIASMATIC = 437697;
        public const int QUOTATIONZODIACAL = 256787;
        public const int THESAURUSMISJUDGE = 439137;
        public const int THESAURUSDISTRICT = 261010;
        public const int THESAURUSINQUEST = 442312;
        public const int THESAURUSINARTICULATE = 261477;
        public const int THESAURUSRAUCOUS = 442822;
        public const int THESAURUSGRUESOME = 260707;
        public const int LITHOGRAPHICALLY = 440010;
        public const int DENOMINATIONALLY = 261081;
        public const int THESAURUSPLEASANTRY = 440527;
        public const int THESAURUSARISTOCRAT = 260362;
        public const int THESAURUSDEFORMED = 438168;
        public const int THESAURUSOBLIVIOUS = 261067;
        public const int THESAURUSABNORMAL = 438685;
        public const int THESAURUSCOUNTRY = 260124;
        public const int THESAURUSSPECIAL = 437030;
        public const int THESAURUSEMBEZZLE = 260836;
        public const int THESAURUSENDEAVOUR = 437850;
        public const int THESAURUSESSENCE = 268138;
        public const int THESAURUSENDANGER = 436642;
        public const int THESAURUSRARITY = 272105;
        public const int THESAURUSENVIRONMENT = 439539;
        public const int THESAURUSEGRESS = 278301;
        public const int DISHONOURABLENESS = 443102;
        public const int THESAURUSGUARDED = 278626;
        public const int PHYSIOPATHOLOGY = 443357;
        public const int SUBCATEGORIZING = 279042;
        public const int THESAURUSNOTICE = 442391;
        public const int THESAURUSSHOULDER = 279306;
        public const int THESAURUSENGULF = 442686;
        public const int THESAURUSDEVICE = 278766;
        public const int QUOTATIONTALKING = 436855;
        public const int THESAURUSDEPENDABLE = 280724;
        public const int QUOTATIONFOETAL = 439440;
        public const int CORRUPTIBLENESS = 283809;
        public const int THESAURUSOBLIQUE = 437656;
        public const int INTUSSUSCEPTION = 284955;
        public const int SUPERVACANEOUSLY = 438974;
        public const int MISAPPROPRIATED = 269747;
        public const int QUOTATIONPHTHALIC = 437650;
        public const int DYSLOGISTICALLY = 270564;
        public const int RETICULOENDOTHELI = 437989;
        public const int THESAURUSRECRIMINATION = 270627;
        public const int THESAURUSGLADDEN = 438050;
        public const int SUPERORDINATING = 270714;
        public const int THESAURUSACCUSATION = 438192;
        public const int THESAURUSARROGANT = 270897;
        public const int THESAURUSCERTAINLY = 437889;
        public const int QUOTATIONMAUVAIS = 271013;
        public const int THESAURUSUNUTTERABLE = 437980;
        public const int THESAURUSMIDDLE = 270667;
        public const int PSYCHOLINGUISTICALLY = 437541;
        public const int THESAURUSBURGLAR = 270886;
        public const int THESAURUSSINCERELY = 437773;
        public const string THESAURUSWEATHER = "12#";
        public const int UNCHARACTERISTIC = 248346;
        public const int THESAURUSDISTORT = 445215;
        public const int QUOTATIONPHRASAL = 248964;
        public const int INADVENTUROUSNESS = 445598;
        public const int THESAURUSEXONERATION = 248499;
        public const int QUOTATIONFORESEEABLE = 446442;
        public const int ARCHIEPISCOPALLY = 248766;
        public const int QUOTATIONZYGOMATIC = 446584;
        public const int THESAURUSINAPPRECIABLE = 248743;
        public const int INEQUITABLENESS = 446919;
        public const int COMMISERATIVELY = 248839;
        public const int THESAURUSTRICKERY = 447029;
        public const int THESAURUSSECONDLY = 248339;
        public const int THESAURUSTINGLE = 446851;
        public const int THESAURUSLOQUACITY = 248428;
        public const int BLOODTHIRSTINESS = 447020;
        public const int MISREPRESENTING = 248356;
        public const int THESAURUSIMMODERATION = 451328;
        public const int THESAURUSRESENTMENT = 248768;
        public const int THESAURUSPREDOMINATE = 451839;
        public const int THESAURUSDISCUSS = 248571;
        public const int ANTHROPOLOGICALLY = 445997;
        public const int THESAURUSACCLAIM = 248741;
        public const int CIRCUMNAVIGABLE = 446241;
        public const int THESAURUSFORMULA = 248903;
        public const int THESAURUSCOMPETITION = 446523;
        public const int THESAURUSRANCOUR = 249134;
        public const int THESAURUSPLIABLE = 446688;
        public const int MECHANORECEPTOR = 248901;
        public const int UNCONVENTIONALLY = 446971;
        public const int THESAURUSRESTORE = 249012;
        public const int POLYSYNTHETICALLY = 447033;
        public const int THESAURUSHOTCHPOTCH = 248838;
        public const int THESAURUSFRUGAL = 451302;
        public const int THESAURUSIRREVERSIBLE = 249197;
        public const int NH2CH2C6H10COOH = 451831;
        public const int THESAURUSSPRINKLING = 248864;
        public const int THESAURUSPHILOSOPHER = 445960;
        public const int THESAURUSHYPOCRISY = 249176;
        public const int THESAURUSDUPLICATE = 446133;
        public const int HYDROCHLOROTHIAZIDE = 253417;
        public const int PRETENSIOUSNESS = 445269;
        public const int SOPHISTICATIONS = 253725;
        public const int THESAURUSIMPERCEPTIBLE = 445614;
        public const int THESAURUSMAYHEM = 252875;
        public const int QUOTATIONCHALCEDONIAN = 446178;
        public const int THESAURUSASPIRATION = 253207;
        public const int THESAURUSLATTER = 446384;
        public const int PSYCHODIAGNOSTICS = 253303;
        public const int THESAURUSADMINISTRATOR = 446159;
        public const int THESAURUSDIVERGENCE = 253747;
        public const int THESAURUSIRREGULAR = 446379;
        public const int QUOTATIONREQUIRED = 252923;
        public const int THESAURUSENCOMPASS = 446857;
        public const int ECHOENCEPHALOGRAPHY = 253219;
        public const int THESAURUSFISSION = 447532;
        public const int BRACHYCEPHALOUS = 252903;
        public const int OPHTHALMOLOGISTS = 451444;
        public const int POLYGRAPHICALLY = 253202;
        public const int THESAURUSMYSTERY = 451952;
        public const int THESAURUSARBITRARY = 253289;
        public const int THESAURUSABSOLUTE = 446982;
        public const int THESAURUSINFURIATE = 253424;
        public const int THESAURUSENLIGHTENMENT = 447093;
        public const int THESAURUSDEGENERATE = 253519;
        public const int SEMICONSCIOUSNESS = 446639;
        public const int NEUROPSYCHIATRY = 253647;
        public const int ALSOPENTOBARBITAL = 446790;
        public const int QUOTATIONIBICENCAN = 273094;
        public const int THESAURUSJAILER = 442828;
        public const int THESAURUSCHIRPY = 273773;
        public const int THESAURUSGRANULE = 443501;
        public const int FLUOROCARBONATE = 272657;
        public const int THESAURUSACADEMIC = 444064;
        public const int QUOTATIONMAILLE = 272949;
        public const int INDISCRIMINATIVELY = 444325;
        public const int THESAURUSCAREER = 273085;
        public const int THESAURUSNASTINESS = 443890;
        public const int THESAURUSTELEPHONE = 273296;
        public const int THESAURUSIDIOCY = 444139;
        public const int THESAURUSBRAVADO = 273696;
        public const int THESAURUSEXASPERATION = 444439;
        public const int THESAURUSFEATHER = 274477;
        public const int THESAURUSBEFRIEND = 444584;
        public const int SUPERFICIALNESS = 273631;
        public const int THESAURUSTREMENDOUS = 443997;
        public const int THESAURUSMISPLACE = 273970;
        public const int AUTOSCHEDIASTIC = 444246;
        public const int ANTICHOLINERGIC = 274726;
        public const int THESAURUSCOSTUME = 450316;
        public const int THESAURUSSUPPORT = 275423;
        public const int THESAURUSINEFFABLE = 450541;
        public const int ENTERCOMMUNICATION = 277388;
        public const int QUOTATIONNONAGESIMAL = 442801;
        public const int ALSOVITRESCENCY = 277729;
        public const int RHOMBENCEPHALON = 443119;
        public const int NOVAEHOLLANDIAE = 277162;
        public const int THESAURUSDISPATCH = 443541;
        public const int THESAURUSUNDECEIVE = 277382;
        public const int THESAURUSONLOOKER = 443688;
        public const int ANTEPENULTIMATE = 277470;
        public const int THESAURUSPERTURB = 444068;
        public const int THESAURUSABJURE = 277680;
        public const int QUOTATIONPANDEAN = 444326;
        public const int THESAURUSFORGERY = 277358;
        public const int PSYCHOTHERAPIST = 444536;
        public const int THESAURUSSEEMLY = 277443;
        public const int THESAURUSEXCISE = 444713;
        public const int THESAURUSREQUIREMENT = 277416;
        public const int MICROMINIATURIZE = 443599;
        public const int THESAURUSFRATERNIZE = 277667;
        public const int CYTOMEGALOVIRUS = 443837;
        public const int THESAURUSPOTENTATE = 277172;
        public const int THESAURUSNIMBLE = 444115;
        public const int SPINOCEREBELLAR = 277331;
        public const int DISCIPLINARIANISM = 444842;
        public const int ARTERIOSCLEROTIC = 276888;
        public const int KHRUSOMĒLOLONTHION = 450217;
        public const int THESAURUSCHARGE = 277380;
        public const int THESAURUSTERMINUS = 450532;
        public const int THESAURUSIRRITABLE = 278644;
        public const int BIOSTRATIGRAPHER = 278897;
        public const int THESAURUSREGRET = 443044;
        public const int THESAURUSLAWYER = 278685;
        public const int THESAURUSCANDOUR = 444083;
        public const int THESAURUSEVANGELIZE = 278833;
        public const int THESAURUSCONDITIONED = 444356;
        public const int NANOTECHNOLOGICAL = 278864;
        public const int EXTERRITORIALLY = 444501;
        public const int THESAURUSEGGHEAD = 279002;
        public const int THESAURUSASSAILANT = 444757;
        public const int CONVENTIONALISE = 278968;
        public const int THESAURUSCONSCIENCE = 443570;
        public const int CHLORTETRACYCLINE = 279235;
        public const int THESAURUSNARCISSISM = 443722;
        public const int PHYSIOLOGICALLY = 278705;
        public const int THESAURUSDISGUISE = 443641;
        public const int THESAURUSPARTITION = 278958;
        public const int THESAURUSANNOTATE = 443840;
        public const int THESAURUSLICENTIOUS = 279012;
        public const int THESAURUSGROUND = 444179;
        public const int SUPRADECOMPOUND = 279215;
        public const int THESAURUSELEMENTAL = 444873;
        public const int THESAURUSGENTEEL = 278986;
        public const int THESAURUSEXPOSITION = 450307;
        public const int MALDISTRIBUTION = 279195;
        public const int THESAURUSTERROR = 450501;
        public const int PHOTOCONDUCTIVITY = 283068;
        public const int SPLANCHNOCRANIUM = 443156;
        public const int BENZENEHEXACARBOXYLIC = 283295;
        public const int THESAURUSINCLINE = 443290;
        public const int THESAURUSFOLKLORE = 282503;
        public const int THESAURUSNEGOTIATION = 444036;
        public const int THESAURUSENTRAP = 282730;
        public const int PSYCHOLOGICALLY = 444156;
        public const int STEREOGRAPHICUS = 283441;
        public const int THESAURUSCONSENSUS = 283646;
        public const int THESAURUSLIONIZE = 444298;
        public const int THESAURUSVERSATILE = 283051;
        public const int THESAURUSCOUNTERMAND = 443856;
        public const int THESAURUSANALOGY = 283374;
        public const int THESAURUSCARESS = 444136;
        public const int PRAEMONSTRATENSIS = 282538;
        public const int THESAURUSCREDIT = 444452;
        public const int THESAURUSTHICKEN = 282596;
        public const int TRANSFINALIZATION = 444547;
        public const int SUPERNATURALISM = 281923;
        public const int HYDROCOTYLACEAE = 444344;
        public const int THESAURUSEXCESSIVE = 282123;
        public const int THESAURUSQUALIFIED = 444594;
        public const int CONTRARIOUSNESS = 281201;
        public const int ZANNICHELLIACEAE = 444562;
        public const int THESAURUSNUMEROUS = 281469;
        public const int THESAURUSEXEMPTION = 444769;
        public const int PLATITUDINARIANISM = 281026;
        public const int THESAURUSBEHOVE = 450276;
        public const int THESAURUSLARGELY = 281351;
        public const int THESAURUSDEGREE = 450606;
        public const int THESAURUSPROFITABLE = 260054;
        public const int THESAURUSSUNLESS = 442594;
        public const int THESAURUSOPPOSITION = 260196;
        public const int THESAURUSSULLEN = 442772;
        public const int THESAURUSSUBSTANCE = 260606;
        public const int MISRECOLLECTION = 443189;
        public const int THESAURUSMERRIMENT = 260749;
        public const int MULTIPLICATIVUS = 443323;
        public const int THESAURUSCIRCUMSCRIBE = 260819;
        public const int QUOTATIONANDAMAN = 443152;
        public const int EUPHEMISTICALLY = 260901;
        public const int CONCUPISCIBILIS = 443250;
        public const int RECONVALESCENCE = 260313;
        public const int MICROCIRCULATION = 442644;
        public const int THESAURUSACCENT = 260677;
        public const int HYDROELECTRICITY = 442909;
        public const int ERYTHROPHTHALMA = 260215;
        public const int QUOTATIONDORIAN = 442291;
        public const int THESAURUSFINITE = 260386;
        public const int THESAURUSCHANNEL = 442442;
        public const int THESAURUSPERPETUITY = 260175;
        public const int THESAURUSCOLLATE = 438129;
        public const int QUOTATIONARSENIC = 438460;
        public const int QUOTATIONPOSTAL = 268672;
        public const int THESAURUSIMMERSE = 436644;
        public const int THESAURUSCOOPERATE = 268903;
        public const int THESAURUSMESSAGE = 437027;
        public const int THESAURUSRESULT = 287443;
        public const int PHILANTHROPISTS = 436689;
        public const int THESAURUSMEDICINE = 287635;
        public const int QUOTATIONCUSTOS = 437043;
        public const int THESAURUSACHIEVE = 295565;
        public const int COMPARTMENTALLY = 443151;
        public const int THESAURUSNASCENT = 295716;
        public const int COMMISERATINGLY = 443298;
        public const int THESAURUSPHOBIA = 295973;
        public const int DISCIPLINARIANS = 442678;
        public const int THESAURUSOBSEQUIOUS = 296155;
        public const int PALAEOPATHOLOGY = 442798;
        public const int THESAURUSVISIBLE = 295577;
        public const int THESAURUSCUTTING = 442907;
        public const int SUPERCLUSTERING = 295685;
        public const int THESAURUSRESTRAINT = 443011;
        public const int THESAURUSINSANE = 295789;
        public const int TRICHLOROPHENOXYACETIC = 442602;
        public const int MONOCHROMATICALLY = 295915;
        public const int CONSEQUENTIALNESS = 442750;
        public const int THESAURUSORATORICAL = 295947;
        public const int CONSIDERABLENESS = 438287;
        public const int THESAURUSDISRESPECT = 296207;
        public const int MICROINSTRUCTIONS = 438572;
        public const string THESAURUSIMPOLITE = "13#";
        public const int QUOTATIONMARVELL = 248547;
        public const int THESAURUSHENCHMAN = 445303;
        public const int THESAURUSQUAGMIRE = 248677;
        public const int UNNEIGHBOURLINESS = 445458;
        public const int THESAURUSPHRASEOLOGY = 248900;
        public const int THESAURUSSARDONIC = 445966;
        public const int THESAURUSIDOLIZE = 249165;
        public const int LEPIDOPTEROLOGY = 446129;
        public const int THESAURUSBLASPHEME = 248304;
        public const int THESAURUSAMMUNITION = 446142;
        public const int THESAURUSINDIVIDUAL = 248463;
        public const int THESAURUSCOMICAL = 446285;
        public const int QUOTATIONAMYGDALOID = 248644;
        public const int THESAURUSREPEAT = 446444;
        public const int PLUVIOMETRICALLY = 248770;
        public const int THESAURUSRECOLLECTION = 446549;
        public const int UNDEMANDINGNESS = 248757;
        public const int THESAURUSPRINTER = 446973;
        public const int CONTEMPLATIVENESS = 248875;
        public const int THESAURUSINCURSION = 447095;
        public const int THESAURUSBESEECH = 248336;
        public const int THESAURUSREVERENCE = 446893;
        public const int THESAURUSBESIDE = 248405;
        public const int THESAURUSVEXATIOUS = 446983;
        public const int TRANSILLUMINATING = 248587;
        public const int THESAURUSSPROUT = 447963;
        public const int THESAURUSRASCAL = 248760;
        public const int THESAURUSINEXTINGUISHABLE = 448135;
        public const int THESAURUSPASTICHE = 248642;
        public const int THESAURUSREPLENISH = 446053;
        public const int DEMINERALIZATION = 248828;
        public const int PYROTECHNICALLY = 446234;
        public const int QUOTATIONDICKENS = 248947;
        public const int EXTRAORDINARILY = 446563;
        public const int PHOTOMULTIPLIER = 248996;
        public const int VASOCONSTRICTING = 446612;
        public const int NONDISCRIMINATING = 248924;
        public const int THESAURUSADVANCEMENT = 446933;
        public const int THESAURUSOVERTONE = 249052;
        public const int THESAURUSPADDING = 447015;
        public const int THESAURUSCOMMERCE = 248905;
        public const int THESAURUSFOREFATHER = 448022;
        public const int THESAURUSDRINKER = 249025;
        public const int RECONSOLIDATION = 448155;
        public const int OLIGODENDROCYTES = 253596;
        public const int INCONSIDERATENESS = 445026;
        public const int THESAURUSSYMPATHIZE = 254017;
        public const int DEUTERANOMALOUS = 445392;
        public const int THESAURUSPAUPER = 253243;
        public const int THESAURUSSWATHE = 445946;
        public const int THESAURUSORIGINATE = 253454;
        public const int QUOTATIONDERNIER = 446105;
        public const int THESAURUSFORGOTTEN = 253963;
        public const int MAGNETOCARDIOGRAPH = 446160;
        public const int THESAURUSSUBTLETY = 254039;
        public const int UNSYSTEMATICALLY = 446262;
        public const int THESAURUSINSTIGATE = 253557;
        public const int ANTHROPOCENTRIC = 446438;
        public const int MISUNDERSTANDINGS = 253665;
        public const int THESAURUSHIBERNATE = 446514;
        public const int TETRAHEXAHEDRON = 253337;
        public const int QUOTATIONCAPIAS = 446536;
        public const int THESAURUSRECORDING = 253394;
        public const int THESAURUSMISBEGOTTEN = 446587;
        public const int QUOTATIONUNDULATORY = 253593;
        public const int THESAURUSSPARKLE = 446123;
        public const int THESAURUSHOSIERY = 253685;
        public const int THESAURUSEXPECTANCY = 446220;
        public const int THESAURUSDEFECTIVE = 253296;
        public const int INSTRUMENTALITY = 447867;
        public const int QUOTATIONBOURBON = 253485;
        public const int THESAURUSCRUMBLE = 448100;
        public const int ACCOMMODATIONAL = 261792;
        public const int THESAURUSPALACE = 443190;
        public const int THESAURUSEMERGENT = 262064;
        public const int SCROPHULARIACEAE = 443448;
        public const int THESAURUSATTACH = 261226;
        public const int UNAPPEASABLENESS = 442984;
        public const int THESAURUSJUNGLE = 261419;
        public const int SPHAEROCEPHALUS = 443255;
        public const int THESAURUSPOLITIC = 260620;
        public const int PHTHISIOTHERAPIST = 443133;
        public const int THESAURUSEXTENSION = 260765;
        public const int THESAURUSHIGHFALUTIN = 443269;
        public const int THESAURUSECONOMIC = 260186;
        public const int THESAURUSPLAYBOY = 442681;
        public const int SPACEWORTHINESS = 442852;
        public const int THESAURUSMIRACULOUS = 259911;
        public const int TREASONABLENESS = 439017;
        public const int ENTRECHANGEMENT = 260074;
        public const int INDISSOLUBLENESS = 439257;
        public const int ORTHOSTATICALLY = 260405;
        public const int THESAURUSSUSPENSE = 442640;
        public const int THESAURUSRECONCILE = 260536;
        public const int QUOTATIONQUARTAN = 442824;
        public const int PHTHISIOTHERAPY = 260205;
        public const int LEXICOSTATISTICALLY = 442397;
        public const int COMMUNICATIONAL = 260303;
        public const int THESAURUSAPPETITE = 442470;
        public const int THESAURUSEXPRESSIONLESS = 260116;
        public const int DETERMINATIVELY = 438329;
        public const int THESAURUSDISTASTE = 260540;
        public const int QUOTATIONCOCKEYE = 438742;
        public const int PSYCHOBIOLOGICAL = 272772;
        public const int QUOTATIONALFVÉN = 442443;
        public const int QUOTATIONUGANDA = 272918;
        public const int THESAURUSADDICTION = 442664;
        public const int PHOTOMULTIPLIERS = 273411;
        public const int ANTHROPOMORPHITES = 442719;
        public const int THESAURUSSNATCH = 273708;
        public const int CONSUBSTANTIALLY = 443026;
        public const int THESAURUSSTREAK = 273713;
        public const int THESAURUSDETERMINED = 443333;
        public const int THESAURUSVACATION = 273979;
        public const int THESAURUSEMPHASIZE = 443498;
        public const int THESAURUSDEMAND = 272913;
        public const int CONSEQUENTIALIST = 443725;
        public const int CHEMOAUTOTROPHICALLY = 273064;
        public const int THESAURUSREFINEMENT = 443941;
        public const int CHROMATOGRAPHIC = 273596;
        public const int DISINTEGRATIVELY = 443937;
        public const int THESAURUSGOSPEL = 273657;
        public const int THESAURUSESCAPE = 444028;
        public const int THESAURUSUNQUESTIONABLE = 273196;
        public const int THESAURUSEXPLOSION = 444382;
        public const int ENTREPRENEURIALISM = 273284;
        public const int TETRACHLORODIBENZO = 444499;
        public const int QUOTATIONDOPPLER = 273603;
        public const int THESAURUSINVITATION = 444543;
        public const int THESAURUSMENTALLY = 273698;
        public const int DISADVANTAGEOUS = 444623;
        public const int DECAHYDRONAPHTHALENE = 272868;
        public const int THESAURUSEMPORIUM = 444550;
        public const int QUOTATION1ARAMUS = 272995;
        public const int THESAURUSSTRIPLING = 444687;
        public const int QUOTATIONEQUIANGULAR = 273352;
        public const int THESAURUSOFFICIAL = 445740;
        public const int QUOTATIONAMERICAN = 273563;
        public const int THESAURUSDECORATIVE = 445995;
        public const int RESOURCEFULNESS = 273740;
        public const int THESAURUSAWKWARD = 443878;
        public const int DISCRIMINATIVELY = 273852;
        public const int GESAMTKUNSTWERK = 443986;
        public const int THESAURUSIMPRECISE = 273716;
        public const int HYDROXYPROPANOIC = 444361;
        public const int TRIGONOMETRICAL = 273868;
        public const int THESAURUSPRETENDED = 444468;
        public const int THESAURUSTRAVERSE = 273694;
        public const int THESAURUSMULTIFARIOUS = 445634;
        public const int PHOTOMACROGRAPHY = 273957;
        public const int RESTRICTIONISTS = 445942;
        public const int THESAURUSMINDLESS = 277448;
        public const int THESAURUSEMINENTLY = 442759;
        public const int THESAURUSUNASSAILABLE = 277688;
        public const int THESAURUSMIDDLEMAN = 443066;
        public const int QUOTATIONSHELLEY = 277068;
        public const int THESAURUSLUMINARY = 443539;
        public const int THESAURUSJEJUNE = 277303;
        public const int THESAURUSLEANING = 443745;
        public const int THESAURUSPLAYER = 277142;
        public const int TINTINNABULATED = 444026;
        public const int THESAURUSCOHORT = 277180;
        public const int THESAURUSCOLLEGE = 444073;
        public const int THESAURUSCONTRADICTORY = 277500;
        public const int THESAURUSEMPHASIS = 444243;
        public const int INTERLAMINATING = 277564;
        public const int ELECTROPAINTING = 444336;
        public const int THESAURUSFLAMING = 277312;
        public const int THESAURUSSTRENGTH = 444680;
        public const int THESAURUSOBLIGATION = 444794;
        public const int THESAURUSBUBBLE = 277587;
        public const int PHOTOFLUOROGRAMS = 277669;
        public const int THESAURUSSPOKEN = 444804;
        public const int UNCHALLENGEABLE = 277801;
        public const int THESAURUSGARGANTUAN = 444575;
        public const int UNSOPHISTICATED = 277896;
        public const int THESAURUSMISCHIEF = 444670;
        public const int THESAURUSSWELLING = 277428;
        public const int CIRCUMCELLIONES = 445984;
        public const int THESAURUSVACUUM = 277554;
        public const int THESAURUSFLUCTUATE = 446151;
        public const int THESAURUSBOLSTER = 277389;
        public const int THESAURUSUNAWARE = 443778;
        public const int PICTURESQUENESS = 277544;
        public const int THESAURUSSOVEREIGNTY = 443917;
        public const int THESAURUSFOREGONE = 277121;
        public const int THESAURUSEXPONENT = 444359;
        public const int QUOTATIONCRUCIATE = 277227;
        public const int THESAURUSFEMININE = 444484;
        public const int INSURRECTIONIST = 277115;
        public const int IATROMATHEMATICUS = 444741;
        public const int THESAURUSFRAGILE = 277210;
        public const int CONSIGNIFICATIO = 444876;
        public const int THESAURUSNATION = 277098;
        public const int THESAURUSSCENERY = 445843;
        public const int THESAURUSPROPRIETY = 277319;
        public const int THESAURUSCOMBINE = 446259;
        public const int INEFFECTUALNESS = 268641;
        public const int ALSOGENICULATED = 436744;
        public const int THESAURUSQUAINT = 268835;
        public const int UNPREJUDICEDNESS = 437032;
        public const int THESAURUSCONTAIN = 278982;
        public const int THESAURUSSEGMENT = 442793;
        public const int THESAURUSSYCOPHANTIC = 279507;
        public const int THESAURUSBROKEN = 443122;
        public const int QUOTATIONSILESIAN = 279390;
        public const int THESAURUSKISMET = 443486;
        public const int THESAURUSINCONTINENT = 279837;
        public const int THESAURUSFURROW = 443754;
        public const int REINTERPRETATION = 279075;
        public const int QUOTATIONPAIRED = 444205;
        public const int THESAURUSJACKPOT = 279282;
        public const int OBSTRUCTIONISTS = 444374;
        public const int THESAURUSMISOGYNIST = 279274;
        public const int THESAURUSDECEIT = 444675;
        public const int OVERCAUTIOUSNESS = 279403;
        public const int PLECTONEMICALLY = 444814;
        public const int THESAURUSMOTIONLESS = 279032;
        public const int STEREOREGULATING = 444678;
        public const int THESAURUSCANDID = 279183;
        public const int QUOTATION2ASTADIAL = 444816;
        public const int DOLICHOCEPHALISM = 278847;
        public const int THESAURUSCOLLECTIVE = 444524;
        public const int QUOTATIONJUSTINIANIAN = 278959;
        public const int THESAURUSADJUST = 444673;
        public const int PARTICULARISMUS = 279148;
        public const int SYLLABIFICATION = 443793;
        public const int THESAURUSPRODIGAL = 279296;
        public const int PEROXOSULPHURIC = 443970;
        public const int MELODRAMATICALLY = 279462;
        public const int ZOOIDTRANSFORMER = 444304;
        public const int PREMEDITATEDNESS = 279615;
        public const int THESAURUSLIBATION = 444447;
        public const int THESAURUSMACERATE = 279487;
        public const int THESAURUSLAGOON = 444736;
        public const int THESAURUSMINUTELY = 279573;
        public const int DISACKNOWLEDGEMENT = 444832;
        public const int THESAURUSILLEGAL = 279429;
        public const int NEUROANATOMICAL = 445943;
        public const int DISPROPORTIONABLENESS = 279686;
        public const int THESAURUSCLIENT = 446208;
        public const int THESAURUSINHIBITION = 279496;
        public const int DIASTATOCHROMOGENES = 444032;
        public const int RESOURCELESSNESS = 279544;
        public const int PROPRIETORIALLY = 444076;
        public const int THESAURUSINDEPENDENT = 279141;
        public const int THESAURUSDEBONAIR = 445940;
        public const int INDISCRIMINATINGLY = 279325;
        public const int PROSELYTIZATION = 446180;
        public const int THESAURUSEMBROIDER = 283698;
        public const int THESAURUSINSPIRE = 442282;
        public const int THESAURUSENHANCE = 284073;
        public const int THESAURUSMONUMENTAL = 442753;
        public const int UNDEREMPLOYMENT = 283070;
        public const int THESAURUSDIRECTOR = 442693;
        public const int THESAURUSIMPERFECTION = 283396;
        public const int REACTIONARINESS = 443019;
        public const int METHYLTHIOURACIL = 282743;
        public const int THESAURUSBUCOLIC = 443309;
        public const int UNPROTECTEDNESS = 283039;
        public const int THESAURUSEVENTUAL = 283414;
        public const int MEPHISTOPHELIAN = 444404;
        public const int BIOTECHNOLOGIST = 283521;
        public const int THESAURUSSEGREGATE = 283619;
        public const int THESAURUSHERALD = 443799;
        public const int NEUROTRANSMITTERS = 283721;
        public const int THESAURUSTIMETABLE = 443928;
        public const int CEPHALOCHORDATE = 282993;
        public const int QUOTATIONKEPLER = 444527;
        public const int REINTERMEDIATION = 283091;
        public const int THESAURUSINFLEXIBLE = 444639;
        public const int THESAURUSCOMPONENT = 283029;
        public const int THESAURUSIMPROVISE = 443944;
        public const int THESAURUSESTABLISHMENT = 283117;
        public const int THESAURUSSWALLOW = 444065;
        public const int UNDISSEMBLINGLY = 283443;
        public const int THESAURUSLUBBERLY = 443485;
        public const int DIBENZANTHRACENE = 283638;
        public const int ALIMENTATIVENESS = 443649;
        public const int QUOTATIONLUMBAR = 282831;
        public const int THESAURUSDEVOTED = 443884;
        public const int QUOTATIONCONNOTATIVE = 283002;
        public const int QUOTATIONCARTHAGINIAN = 443984;
        public const int THESAURUSUNEARTH = 282848;
        public const int THESAURUSSTRIKE = 283014;
        public const int CONDYLARTHROSIS = 444482;
        public const int THESAURUSINFREQUENT = 445829;
        public const int THESAURUSDETRITUS = 282983;
        public const int HYPOMAGNESAEMIA = 445990;
        public const int THESAURUSMILKSOP = 283616;
        public const int DIAPHOTOTROPISM = 444711;
        public const int THESAURUSGIFTED = 283811;
        public const int DISCONTINUATION = 444892;
        public const int PSYCHODRAMATICS = 283162;
        public const int THESAURUSCOUNTERACT = 445883;
        public const int THESAURUSFOREGROUND = 283352;
        public const int HELIOTROPICALLY = 446112;
        public const int THESAURUSCAFETERIA = 287746;
        public const int INSCRUTABLENESS = 436544;
        public const int THESAURUSSCANDALOUS = 288061;
        public const int THESAURUSIMMORTALITY = 437093;
        public const int DIFFERENTIATION = 294686;
        public const int THESAURUSGABBLE = 443155;
        public const int THESAURUSWASTED = 294897;
        public const int THESAURUSBREATHLESS = 443345;
        public const int THESAURUSEXORBITANT = 295294;
        public const int TETRAHYDROXYHEXANEDIOIC = 442913;
        public const int THESAURUSMORTIFY = 295510;
        public const int THESAURUSRENOWNED = 443146;
        public const int THESAURUSUNRELIABLE = 296074;
        public const int CRYSTALLOGRAPHICALLY = 443080;
        public const int THESAURUSLIVELIHOOD = 296325;
        public const int THESAURUSGLIMMER = 443221;
        public const int THESAURUSCAVERNOUS = 297960;
        public const int THESAURUSSEVERAL = 443070;
        public const int TRANSUBSTANTIALISM = 298098;
        public const int THESAURUSCONVENIENCE = 443287;
        public const int THESAURUSIMPECUNIOUS = 299411;
        public const int THESAURUSAPPRAISE = 443163;
        public const int THESAURUSDEPLETION = 299526;
        public const int THESAURUSPRESENTIMENT = 443349;
        public const int EXCOMMUNICATIVE = 298750;
        public const int THESAURUSINTELLIGENCE = 442937;
        public const int POLIOENCEPHALITIS = 298905;
        public const int UNCONSCIENTIOUS = 443145;
        public const int THESAURUSLAVISH = 295711;
        public const int GENTLEMANLINESS = 443110;
        public const int THESAURUSMAGNANIMITY = 295804;
        public const int CONSOCIATIONALISM = 443238;
        public const int THESAURUSMAGNETISM = 296110;
        public const int HYPERCONJUGATION = 442687;
        public const int MICROLEPIDOPTERA = 296234;
        public const int THESAURUSMINSTREL = 442861;
        public const int THESAURUSDUFFER = 296407;
        public const int THESAURUSADJACENT = 442405;
        public const int SPECTROCHEMISTRY = 296474;
        public const int THESAURUSSPINELESS = 442500;
        public const int THESAURUSFRACTIOUS = 296301;
        public const int FLUOROPHOTOMETRY = 438325;
        public const int THESAURUSRESIGN = 296623;
        public const int THESAURUSGENERIC = 438712;
        public const int THESAURUSFLABBY = 296537;
        public const int THESAURUSABHORRENT = 434592;
        public const int THESAURUSINEQUALITY = 296611;
        public const int QUOTATIONHALCYON = 434733;
        public const int THESAURUSEXCOMMUNICATE = 296368;
        public const int THESAURUSSHORTAGE = 442672;
        public const int THESAURUSFETTER = 296509;
        public const int THESAURUSOBDURATE = 442778;
        public const int THESAURUSCOSMIC = 298451;
        public const int NORTESTOSTERONE = 443134;
        public const int QUOTATIONQUEENSLAND = 298527;
        public const int QUOTATIONHUNGARY = 443208;
        public const int THESAURUSCORROSIVE = 298009;
        public const int QUOTATIONWOLFFIAN = 442697;
        public const int ULTRAMICROSCOPICALLY = 298170;
        public const int THESAURUSPERSONALIZE = 442846;
        public const int UNCOMMUNICATING = 297776;
        public const int THESAURUSMAXIMUM = 442413;
        public const int THESAURUSDISGORGE = 297827;
        public const int DIAHELIOTROPISM = 442456;
        public const int ETHNOMUSICOLOGY = 297758;
        public const int QUOTATIONSTELLER = 438450;
        public const int IATROMECHANICIAN = 297909;
        public const int INFRALAPSARIANISM = 438678;
        public const int THESAURUSPIRATE = 297617;
        public const int INCONSIDERATION = 434529;
        public const int INAPPROPRIATELY = 297706;
        public const int QUOTATION1AFALL = 434728;
        public const int THESAURUSGOSSAMER = 306207;
        public const int THESAURUSBARRISTER = 436532;
        public const int PTEROYLMONOGLUTAMIC = 306491;
        public const int THESAURUSELASTICITY = 437024;
        public const int THESAURUSFAILING = 311585;
        public const int THESAURUSCONSTRAIN = 443581;
        public const int THESAURUSDEPLETE = 311817;
        public const int QUOTATIONZENKER = 443809;
        public const int INEFFICACIOUSNESS = 311902;
        public const int THESAURUSFEROCIOUS = 444322;
        public const int DEZINFORMATSIYA = 312147;
        public const int THESAURUSTAMPER = 444505;
        public const int THESAURUSOPERATOR = 311621;
        public const int THESAURUSCOMFORTABLE = 445027;
        public const int HYDROXYBUTANOIC = 311796;
        public const int UNCHRISTIANLIKE = 445218;
        public const int THESAURUSDIPLOMAT = 311952;
        public const int THESAURUSIMPROVIDENT = 445085;
        public const int THESAURUSPASTORAL = 312098;
        public const int THESAURUSPASSIVE = 445211;
        public const int SULPHAEMOGLOBIN = 446380;
        public const int PSEUDOSOLARIZATION = 312131;
        public const int THESAURUSGIMCRACK = 446531;
        public const int THESAURUSEMENDATION = 311662;
        public const int QUOTATIONAZYGOS = 444498;
        public const int OTHERWORLDLINESS = 311874;
        public const int THESAURUSINCAUTIOUS = 444710;
        public const int PHOSPHODIESTERASE = 314842;
        public const int PSYCHOGENICALLY = 441858;
        public const int THERMOREGULATES = 314952;
        public const int THESAURUSSCHISM = 442011;
        public const int THESAURUSORDINARY = 315295;
        public const int THESAURUSSINISTER = 441917;
        public const int DISENTHRONEMENT = 315418;
        public const int THESAURUSFONDLE = 441990;
        public const int THESAURUSCANKER = 315595;
        public const int CH2OHRCHOHRCH2OH = 441888;
        public const int THESAURUSPROTECTION = 315689;
        public const int THESAURUSMAGICIAN = 442006;
        public const int THESAURUSINELIGIBLE = 316405;
        public const int THESAURUSTRAGIC = 441856;
        public const int THESAURUSGETAWAY = 316628;
        public const int MICROMETEOROIDS = 441974;
        public const int THESAURUSILLUSTRATIVE = 315600;
        public const int TROCKENBEERENAUSLESE = 441427;
        public const int IDENTITÄTSPHILOSOPHIE = 315737;
        public const int THESAURUSHOMELESS = 441558;
        public const int GOSUDARSTVENNOE = 314938;
        public const int CONSCRIPTIONIST = 442077;
        public const int THESAURUSACOLYTE = 315051;
        public const int NATIONALISATION = 442185;
        public const int THESAURUSDEVOTEE = 315377;
        public const int QUOTATIONJONATHAN = 442228;
        public const int THESAURUSSLIVER = 315476;
        public const int THESAURUSOBJECTION = 442346;
        public const int THESAURUSAPPLAUD = 315616;
        public const int THESAURUSDERIDE = 441400;
        public const int QUOTATION2BPUBLISHER = 315772;
        public const int MICROCRYSTALLINE = 441507;
        public const string SYSTEMATIZATION = "A1、A2";
        public const int SCHRAMMELQUARTETT = 501246;
        public const int THESAURUSVOLLEY = 305376;
        public const int QUOTATIONIBIZAN = 501308;
        public const int THESAURUSABRIDGE = 305451;
        public const int THESAURUSPILLAGE = 500591;
        public const int THESAURUSSUBWAY = 305354;
        public const int THESAURUSDOGSBODY = 500699;
        public const int UNCHANGEABLENESS = 305452;
        public const int THESAURUSDELECTABLE = 499984;
        public const int THESAURUSFIDGETY = 305363;
        public const int INTELLECTUALISING = 500084;
        public const int THESAURUSDISCLOSURE = 305441;
        public const int THESAURUSFATHERLY = 499611;
        public const int UNTRACTABLENESS = 305531;
        public const int PARAGRAPHICALLY = 499716;
        public const int THESAURUSGRISLY = 305635;
        public const int MOUTHIMPROVIDENTLY = 499384;
        public const int THESAURUSESPECIAL = 308378;
        public const int THESAURUSENLIST = 499599;
        public const int THESAURUSINDICTMENT = 308639;
        public const int THESAURUSCONSCRIPT = 501228;
        public const int THESAURUSMETTLE = 305623;
        public const int ENCEPHALISATION = 501348;
        public const int THESAURUSMIGRANT = 305724;
        public const int ALSOORTHOPRAXIS = 501126;
        public const int THESAURUSSOVEREIGN = 306019;
        public const int PROFIBRINOLYSIN = 501244;
        public const int MAXILLOPALATINE = 306172;
        public const int INCREDULOUSNESS = 500462;
        public const int ALSOTHAUMATURGIST = 305870;
        public const int UNCHARITABLENESS = 500556;
        public const int UNSPORTSMANLIKE = 305974;
        public const int CONSTRUCTIVISTS = 500039;
        public const int IONTOPHORETICALLY = 305631;
        public const int THESAURUSCOLOSSAL = 500177;
        public const int THESAURUSCONSERVATORY = 305771;
        public const int THESAURUSDIALOGUE = 499644;
        public const int UNPROFITABLENESS = 308515;
        public const int THESAURUSFIGUREHEAD = 499756;
        public const int TRANSMUTATIONIST = 308646;
        public const int UNCONDITIONALLY = 500227;
        public const int QUOTATIONFATHER = 304920;
        public const int THESAURUSTHREAT = 500360;
        public const int EMPOVERISSEMENT = 305067;
        public const int UNINDIVIDUALIZED = 503852;
        public const int SPECULATIVENESS = 306717;
        public const int QUOTATION3BLARGE = 503940;
        public const int THESAURUSMORNING = 306905;
        public const int CORYNEBACTERIUM = 503395;
        public const int THESAURUSBEWITCH = 306875;
        public const int THESAURUSCORSAIR = 503580;
        public const int THESAURUSSWEETEN = 307041;
        public const int THESAURUSVINTAGE = 503286;
        public const int VESTIBULOCOCHLEAR = 307506;
        public const int PHENOLPHTHALEIN = 503466;
        public const int UNCONSTITUTIONALLY = 307638;
        public const int MONOSYLLABICITY = 503785;
        public const int STRATIGRAPHICAL = 308074;
        public const int UNCOMPREHENDINGLY = 503930;
        public const int QUOTATIONMERINGUE = 308194;
        public const int THESAURUSREQUIRED = 503430;
        public const int THESAURUSINFERNAL = 308201;
        public const int THESAURUSWRENCH = 503495;
        public const int QUOTATION1BPARASITIC = 308267;
        public const int THESAURUSOUTGOING = 308708;
        public const int THESAURUSCRITICAL = 504098;
        public const int THESAURUSAFFRAY = 308895;
        public const int THESAURUSCONCILIATE = 503571;
        public const int THESAURUSRESOURCE = 310037;
        public const int QUOTATIONCATHOLIC = 503714;
        public const int THESAURUSBRACKET = 310264;
        public const int THESAURUSARTFUL = 503687;
        public const int THESAURUSCLERICAL = 307712;
        public const int THESAURUSLONELINESS = 503848;
        public const int THESAURUSVIRGINAL = 307941;
        public const int QUOTATIONEXTERIOR = 503262;
        public const int QUOTATIONPUERPERAL = 308116;
        public const int THESAURUSDELIVERY = 503421;
        public const int IRRESPONSIBLENESS = 308252;
        public const int THESAURUSARRIVE = 503296;
        public const int PARALLÉLOGRAMME = 309696;
        public const int UNENTHUSIASTICALLY = 503407;
        public const int QUOTATIONSHASTA = 309905;
        public const int THESAURUSREVELLER = 514141;
        public const int THESAURUSPORCELAIN = 307381;
        public const int VYALOTEKUSHCHIĬ = 514321;
        public const int THESAURUSFLYING = 307598;
        public const int THESAURUSALLOTMENT = 514050;
        public const int THESAURUSVEHICLE = 306632;
        public const int THESAURUSPORTLY = 514430;
        public const int THESAURUSDESPONDENT = 306976;
        public const int WASHINGTONIANUM = 513980;
        public const int THESAURUSINVENTION = 301709;
        public const int THESAURUSEQUITABLE = 514461;
        public const int INTROPUNITIVENESS = 302531;
        public const int THESAURUSACCUMULATION = 523117;
        public const int THESAURUSGAFFER = 307397;
        public const int THESAURUSDISSATISFIED = 523274;
        public const int THESAURUSACCURACY = 307566;
        public const int THESAURUSBROOCH = 523045;
        public const int THESAURUSSKELETON = 308264;
        public const int THESAURUSBOOMERANG = 523177;
        public const int SUCCESSLESSNESS = 308504;
        public const int THESAURUSDEXTERITY = 522700;
        public const int SPECTROCHEMICALLY = 302938;
        public const int THESAURUSTROUSERS = 523142;
        public const int BRACHISTOCHRONE = 303447;
        public const int THESAURUSPROVINCIAL = 523036;
        public const int TENDENTIOUSNESS = 301786;
        public const int GEOMORPHOLOGICAL = 523353;
        public const int THESAURUSOFFICIATE = 302343;
        public const int TUBERCULOSTATIC = 523082;
        public const int MISREPRESENTATIVE = 306678;
        public const int COMMUNICATIVENESS = 523287;
        public const int QUOTATIONPLEOCHROIC = 306935;
        public const int PNEUMATOMACHIANS = 533232;
        public const int SPLANCHNOLOGIST = 306573;
        public const int ACKNOWLEDGEMENTS = 533623;
        public const int THESAURUSCREDIBILITY = 306997;
        public const int THESAURUSDESERVE = 533849;
        public const int THESAURUSEXHILARATE = 306798;
        public const int THESAURUSSUCCULENT = 534094;
        public const int PROTONEPHRIDIUM = 307176;
        public const int THESAURUSBYSTANDER = 533981;
        public const int CATEGOREMATICALLY = 307368;
        public const int THESAURUSWOODED = 534273;
        public const int THESAURUSPROVINCE = 307640;
        public const int THESAURUSINTERFERENCE = 533485;
        public const int BIOGEOGRAPHICAL = 308050;
        public const int THESAURUSINDISPUTABLE = 533628;
        public const int THESAURUSPRETEXT = 308196;
        public const int THESAURUSINVULNERABLE = 533342;
        public const int THESAURUSTACITURN = 533543;
        public const int UNREMITTINGNESS = 308877;
        public const int THESAURUSUNWORLDLY = 533585;
        public const int QUOTATIONTHACKERAY = 310021;
        public const int INDUBITABLENESS = 533839;
        public const int THESAURUSTRANSIT = 310221;
        public const int THESAURUSNARRATIVE = 533568;
        public const int QUOTATION1ADIGITAL = 307729;
        public const int INHARMONIOUSNESS = 533798;
        public const int THESAURUSDEFECT = 307946;
        public const int INVOLUNTARINESS = 533973;
        public const int THESAURUSHEIGHT = 308105;
        public const int THESAURUSBREAST = 534127;
        public const int HYPERTRIGLYCERI = 308230;
        public const int THESAURUSCOMMENT = 533957;
        public const int THESAURUSDELUDE = 309712;
        public const int THESAURUSDISFAVOUR = 534132;
        public const int THESAURUSTOLERANCE = 309908;
        public const int UNDANGEROUSNESS = 536046;
        public const int STEREOSPECIFICALLY = 305317;
        public const int THESAURUSEXTINCTION = 536197;
        public const int QUOTATION1ABARRY = 305475;
        public const int THESAURUSCONSIDERABLE = 536671;
        public const int NEUROTRANSMISSION = 305301;
        public const int THESAURUSWOMANKIND = 536842;
        public const int THESAURUSITEMIZE = 305468;
        public const int THESAURUSMIDDLING = 537594;
        public const int THESAURUSFORSAKE = 305435;
        public const int THESAURUSDRINKABLE = 537867;
        public const int SPERMATOGENESIS = 305648;
        public const int THESAURUSNEWSPAPER = 537829;
        public const int THESAURUSFLUCTUATION = 308358;
        public const int IRREPROACHABLENESS = 538037;
        public const int THESAURUSDECEPTIVE = 308629;
        public const int THESAURUSPROPERTY = 536074;
        public const int THESAURUSEXHAUST = 305621;
        public const int THESAURUSCOPIOUS = 536203;
        public const int THESAURUSSTRATEGIC = 305725;
        public const int THESAURUSPATTER = 536959;
        public const int CONVERSATIONALISTS = 304913;
        public const int THESAURUSDASTARDLY = 537310;
        public const int COMPANIABLENESS = 305118;
        public const int THESAURUSPREPARATION = 536160;
        public const int THESAURUSREPRIEVE = 306031;
        public const int THESAURUSEMISSION = 536299;
        public const int THESAURUSMISCARRY = 306168;
        public const int QUOTATIONDUCTED = 536802;
        public const int THESAURUSFLOWER = 305835;
        public const int INTELLIGIBLENESS = 537002;
        public const int QUOTATIONCHROMIC = 305961;
        public const int THESAURUSRARELY = 537275;
        public const int THESAURUSROTTEN = 305617;
        public const int THESAURUSGLACIAL = 537442;
        public const int THESAURUSAUDACIOUS = 305767;
        public const int ALSOMONONUCLEATE = 537613;
        public const int THESAURUSTENDENCY = 308462;
        public const int JUNGERMANNIALES = 537783;
        public const int PRISCILLIANISTE = 308662;
        public const int THESAURUSMORGUE = 501305;
        public const int INDECIPHERABLENESS = 405091;
        public const int INSENSITIVITIES = 502250;
        public const int HERMENEUTICALLY = 406583;
        public const int THESAURUSSUBTRACT = 503648;
        public const int THESAURUSEXPEDIENT = 407743;
        public const int SUBLAPSARIANISM = 504624;
        public const int THESAURUSMENTALITY = 409433;
        public const int THESAURUSEXECUTIONER = 513480;
        public const int VISCERALIZATION = 409019;
        public const int DIALLYLBARBITURIC = 513565;
        public const int THESAURUSACCENTUATE = 409124;
        public const int THESAURUSGYRATE = 513947;
        public const int THESAURUSMONSTROUS = 407440;
        public const int DISPROPORTIONED = 514508;
        public const int INTERNATIONALLY = 408793;
        public const int THESAURUSSUCCOUR = 522851;
        public const int TRIBOLUMINESCENCE = 407563;
        public const int QUOTATION1BMEDITERRANEAN = 523402;
        public const int THESAURUSPLEASANT = 409077;
        public const int THESAURUSDISEMBOWEL = 532975;
        public const int THESAURUSINCIDENCE = 408017;
        public const int THESAURUSVOLATILE = 533661;
        public const int THESAURUSCULPABLE = 409240;
        public const int THESAURUSRECESSION = 535321;
        public const int INSIGNIFICATIVE = 404434;
        public const int THESAURUSCOMBINATION = 537010;
        public const int THESAURUSCOVERAGE = 406554;
        public const int THESAURUSEXCRETE = 501226;
        public const int THESAURUSTRUSTY = 505229;
        public const int THESAURUSRIGHTEOUSNESS = 502547;
        public const int FRAGMENTARINESS = 506650;
        public const int THESAURUSREFUGEE = 503835;
        public const int THESAURUSLEATHERY = 507804;
        public const int THESAURUSBURGEON = 504660;
        public const int QUOTATIONPERFORANT = 509108;
        public const int PROSLAMBANOMENOS = 513948;
        public const int THESAURUSSPHERICAL = 507284;
        public const int THESAURUSPROMONTORY = 514547;
        public const int THESAURUSREFUND = 508631;
        public const int ORTHOSTEREOSCOPICALLY = 522823;
        public const int QUOTATIONJULIAN = 507652;
        public const int THESAURUSABLAZE = 523401;
        public const int THESAURUSSHOCKING = 508705;
        public const int PALAEOLIMNOLOGY = 533030;
        public const int THESAURUSSCENARIO = 508086;
        public const int THESAURUSHURRICANE = 533333;
        public const int IMPRESSIONISTICALLY = 508948;
        public const int THESAURUSEFFERVESCENT = 535363;
        public const int THESAURUSDISCRIMINATE = 505021;
        public const int THESAURUSPHILANDER = 536172;
        public const int QUOTATIONCHARACTERISTIC = 506309;
        public const string RECOLLECTIVENESS = "A9 A10 A11";
        public const int THESAURUSCONSIDERATION = 569756;
        public const int EICOSAPENTAENOIC = 356728;
        public const int THESAURUSTEMERITY = 570420;
        public const int THESAURUSOBSTRUCTIVE = 357499;
        public const int THESAURUSASSIDUOUS = 571374;
        public const int THESAURUSFOREIGN = 358047;
        public const int THESAURUSLUNACY = 571510;
        public const int THESAURUSCONVENTIONAL = 358184;
        public const int THESAURUSDAWDLE = 570367;
        public const int THESAURUSCONSULTATION = 357900;
        public const int THESAURUSCONSIDERING = 571129;
        public const int PARLIAMENTARISM = 358249;
        public const int INTERPRETATIONAL = 570188;
        public const int THESAURUSSODDEN = 360698;
        public const int THESAURUSCORPORAL = 570379;
        public const int QUOTATION1BDIMENSIONAL = 361021;
        public const int THESAURUSLIVERY = 574748;
        public const int THESAURUSUPROARIOUS = 358664;
        public const int THESAURUSRANDOM = 575065;
        public const int QUOTATIONSUSTAINING = 359133;
        public const int THESAURUSDIALECTIC = 574364;
        public const int THESAURUSINVESTIGATE = 359415;
        public const int IMPLICATIVENESS = 574618;
        public const int THESAURUSMONOPOLIZE = 359656;
        public const int THESAURUSPHLEGMATIC = 574811;
        public const int THESAURUSOBSCURITY = 360172;
        public const int THESAURUSDOLEFUL = 574989;
        public const int THESAURUSINFRINGE = 360375;
        public const int THESAURUSFATEFUL = 574586;
        public const int THESAURUSQUALIFY = 360324;
        public const int THESAURUSDIRECT = 574704;
        public const int THESAURUSDEFRAUD = 360454;
        public const int THESAURUSRETAINER = 574552;
        public const int THESAURUSBATTALION = 360701;
        public const int THESAURUSPEDAGOGUE = 574697;
        public const int QUOTATIONZENITHAL = 360834;
        public const int CHAMAELEONTIDAE = 574941;
        public const int THESAURUSPICTURE = 360822;
        public const int THESAURUSPENURY = 575249;
        public const int QUOTATIONMELODIC = 361069;
        public const int THESAURUSCUDDLE = 362217;
        public const int THESAURUSHESITANT = 574897;
        public const int SALICYLALDEHYDE = 362420;
        public const int MONOCHLORINATED = 574593;
        public const int THESAURUSACCESSION = 359738;
        public const int UNTEMPERATENESS = 574973;
        public const int UNWARRANTABLENESS = 360105;
        public const int THESAURUSSLANDEROUS = 574460;
        public const int HYPEROXYGENATED = 360273;
        public const int THESAURUSDISTRESS = 574580;
        public const int THESAURUSARBITRATOR = 360416;
        public const int THESAURUSEXPUNGE = 574448;
        public const int THESAURUSLITIGIOUS = 360654;
        public const int ETHELOTHRĒSKEIA = 574535;
        public const int DECARTELIZATION = 360754;
        public const int THESAURUSMOTHERLY = 574451;
        public const int THESAURUSRECOGNIZE = 362042;
        public const int VAINGLORIOUSNESS = 574573;
        public const int THESAURUSMEDIATE = 362365;
        public const int THESAURUSINTERFERE = 587063;
        public const int THESAURUSEMINENT = 358728;
        public const int THESAURUSPERMIT = 587316;
        public const int PHOTORECONNAISSANCE = 359122;
        public const int THESAURUSWAREHOUSE = 587356;
        public const int THESAURUSCONSTRUCTIVE = 359370;
        public const int DEIPNOSOPHISTĒS = 587772;
        public const int HYPERINSULINAEMIA = 359696;
        public const int PHOTOLITHOGRAPHER = 587051;
        public const int THESAURUSCONTRIVANCE = 360197;
        public const int COUNTERIRRITANT = 587150;
        public const int THESAURUSPEDAGOGIC = 360350;
        public const int THESAURUSENGAGING = 587355;
        public const int ALSOCOENAESTHESIA = 360355;
        public const int THESAURUSDEBUNK = 587429;
        public const int DNIPRODZERZHINSK = 360461;
        public const int STEREOMETRICALLY = 587354;
        public const int THESAURUSDETRACT = 360700;
        public const int THESAURUSTRANSACTION = 587425;
        public const int THESAURUSFICTIONAL = 360793;
        public const int THESAURUSDIFFUSION = 586939;
        public const int THESAURUSGUTTURAL = 360860;
        public const int THESAURUSAGITATE = 587024;
        public const int THESAURUSPOTENT = 361048;
        public const int THESAURUSCOMPULSION = 587120;
        public const int THESAURUSABSORB = 362151;
        public const int THESAURUSTRIUMPH = 587352;
        public const int THESAURUSMECHANICAL = 362379;
        public const int THERMOREGULATION = 359792;
        public const int CHRONOBIOLOGICAL = 587320;
        public const int THESAURUSACCOST = 360031;
        public const int THESAURUSDISEASED = 359931;
        public const int THESAURUSCORRESPONDENT = 587476;
        public const int UNCONCEPTUALIZED = 360130;
        public const int KULTURGESCHICHTE = 587469;
        public const int THESAURUSENTANGLEMENT = 360269;
        public const int THESAURUSANTEDILUVIAN = 587601;
        public const int QUOTATIONBEHAVIOURAL = 360379;
        public const int THESAURUSDISADVANTAGE = 587397;
        public const int ENTREPRENEURSHIP = 361752;
        public const int THESAURUSMORALITY = 587731;
        public const int CONCEPTUALIZATION = 362108;
        public const int STEREOTYPICALLY = 590378;
        public const int THESAURUSSYMMETRY = 358013;
        public const int UNUNDERSTANDABLY = 590612;
        public const int HYDROCARBURETTED = 358270;
        public const int THESAURUSDOWNRIGHT = 591711;
        public const int THESAURUSFAWNING = 357196;
        public const int THESAURUSKINGDOM = 591850;
        public const int INDISCRIMINATING = 357398;
        public const int THESAURUSANTAGONISM = 591880;
        public const int UNDIFFERENTIATED = 357041;
        public const int HYPERCOAGULABILITY = 592057;
        public const int THESAURUSACCORDANCE = 357185;
        public const int ALSOMETACHROMASY = 592561;
        public const int THESAURUSHARRIDAN = 357056;
        public const int THESAURUSCOMPOSITION = 592701;
        public const int THESAURUSCOMMENDABLE = 357152;
        public const int CONSUBSTANTIATION = 592771;
        public const int THEOPHILANTHROPISTS = 592918;
        public const int THESAURUSUNFOLD = 357409;
        public const int THESAURUSMATERIALLY = 590856;
        public const int THESAURUSINTREPID = 357924;
        public const int ORTHOPANTOMOGRAPHY = 591007;
        public const int THESAURUSTREATMENT = 358075;
        public const int THESAURUSEXTEMPORIZE = 590971;
        public const int ELECTROPHOTOGRAPHY = 591155;
        public const int CONSUBSTANTIATUS = 358215;
        public const int QUOTATIONDURHAM = 591331;
        public const int UNSTRAIGHTFORWARD = 358038;
        public const int REPROACHFULNESS = 591538;
        public const int THESAURUSUNDERRATE = 358182;
        public const int HAEMATOGLOBULIN = 591614;
        public const int THESAURUSREVIEW = 360633;
        public const int THESAURUSEMPLOYEE = 591840;
        public const int THESAURUSSCIENCE = 361151;
        public const int THESAURUSOFFEND = 593919;
        public const int THESAURUSEXPENDITURE = 357985;
        public const int PARAPSYCHOLOGICAL = 594176;
        public const int THESAURUSTEDIOUS = 593610;
        public const int THESAURUSINGLORIOUS = 357944;
        public const int THESAURUSPARALYTIC = 593787;
        public const int UNCOMPROMISINGNESS = 358053;
        public const int QUOTATIONMOSAIC = 593425;
        public const int PROGRESSIONISTS = 358011;
        public const int RHYNCHOCEPHALIA = 593621;
        public const int THESAURUSOBEDIENCE = 358207;
        public const int PERFUNCTORINESS = 593086;
        public const int HYPERCATALECTIC = 358060;
        public const int QUOTATION3BCOUPÉ = 593195;
        public const int ALSOCANCELLATED = 358200;
        public const int THESAURUSCONCOMITANT = 592848;
        public const int THESAURUSHOTBED = 360711;
        public const int MACROPHOTOGRAPHY = 593014;
        public const int THESAURUSCONTEMPTIBLE = 360949;
        public const int THESAURUSGOVERN = 597348;
        public const int THESAURUSINDEFINITE = 358774;
        public const int THESAURUSHELLISH = 597740;
        public const int THESAURUSEVIDENCE = 359071;
        public const int THESAURUSUNBECOMING = 596960;
        public const int INCONVERTIBLENESS = 359414;
        public const int HUPOKHONDRIAKOS = 597276;
        public const int SIMULTANEOUSNESS = 359627;
        public const int DIPHENHYDRAMINE = 597409;
        public const int THESAURUSMALTREATMENT = 360202;
        public const int THESAURUSINTUITIVE = 597653;
        public const int THESAURUSDEXTEROUS = 360369;
        public const int THESAURUSINVADE = 597214;
        public const int THESAURUSEXAMPLE = 597278;
        public const int PLANIMETRICALLY = 360779;
        public const int THESAURUSEXCAVATE = 597207;
        public const int THESAURUSIMMEDIATELY = 360352;
        public const int CONTEMPORANEITY = 597248;
        public const int HYPOCRATERIFORMIS = 360439;
        public const int THESAURUSFOREWORD = 597598;
        public const int INTERDEPENDENTLY = 360918;
        public const int PARTICULARIZING = 597660;
        public const int THESAURUSUNDESIRABLE = 361007;
        public const int QUOTATIONBACTERIOLOGICAL = 597293;
        public const int THESAURUSINCIVILITY = 361978;
        public const int THESAURUSHIDDEN = 597621;
        public const int CONTRADISTINCTION = 362446;
        public const int THESAURUSPERVERT = 597394;
        public const int THESAURUSREPETITION = 359763;
        public const int THESAURUSHIGHLY = 597523;
        public const int THESAURUSBRASSY = 359968;
        public const int THESAURUSPREVENT = 597181;
        public const int THESAURUSUNDERWATER = 359954;
        public const int THESAURUSLEGISLATE = 597292;
        public const int THESAURUSSCARCELY = 360076;
        public const int THESAURUSRUNAWAY = 597042;
        public const int RHINOSPORIDIOSIS = 360227;
        public const int KINDHEARTEDNESS = 597177;
        public const int THESAURUSBLOODLESS = 360386;
        public const int THESAURUSINDULGENCE = 597033;
        public const int INAPPRECIATIVELY = 360635;
        public const int THESAURUSSOCIABLE = 597174;
        public const int THESAURUSPANTRY = 360749;
        public const int AUTOLITHOGRAPHY = 597050;
        public const int IMPATRONIZATION = 362061;
        public const int SPECTROHELIOGRAPH = 597184;
        public const int THESAURUSINSUFFICIENT = 362294;
        public const int NEUROEMBRYOLOGY = 609704;
        public const int THESAURUSCREDULOUS = 358608;
        public const int MANIFESTATIVELY = 609847;
        public const int QUOTATIONCOEQUATE = 359057;
        public const int THESAURUSOVERWEIGHT = 609894;
        public const int THESAURUSCLEARLY = 359294;
        public const int THESAURUSSCHOOLING = 610574;
        public const int THESAURUSPERPLEXITY = 359708;
        public const int THESAURUSIRRETRIEVABLE = 609628;
        public const int THESAURUSCOGNATE = 360200;
        public const int DISTINGUISHABLY = 609759;
        public const int QUOTATIONLIFTING = 360330;
        public const int THESAURUSRECEIPT = 609949;
        public const int THESAURUSDOWNGRADE = 360708;
        public const int THESAURUSIMPURE = 610011;
        public const int THESAURUSFRESHEN = 360803;
        public const int THESAURUSASPERITY = 609937;
        public const int THESAURUSALIMONY = 610041;
        public const int THESAURUSINCEPTION = 360480;
        public const int THESAURUSLETHARGY = 609554;
        public const int UNRECOMMENDABLE = 360884;
        public const int ELECTROPHORESIS = 609647;
        public const int THESAURUSCONTRADICTION = 361059;
        public const int THESAURUSFRAILTY = 609709;
        public const int THESAURUSMATCHMAKER = 362124;
        public const int THESAURUSPENETRATION = 609901;
        public const int THESAURUSCRISIS = 362515;
        public const int THESAURUSMISTAKEN = 609758;
        public const int THESAURUSINFERIOR = 359805;
        public const int THESAURUSTELESCOPE = 609855;
        public const int THESAURUSTIMELY = 360008;
        public const int THESAURUSIRRELIGIOUS = 609926;
        public const int THESAURUSDISMANTLE = 359932;
        public const int THESAURUSCRUCIFY = 610042;
        public const int THESAURUSSLEEPY = 360135;
        public const int THESAURUSINTERPOSE = 610028;
        public const int PROHIBITIVENESS = 360264;
        public const int QUOTATIONCRYSTALLIZED = 610193;
        public const int THESAURUSEVENING = 361888;
        public const int THESAURUSDEBATE = 610226;
        public const int THESAURUSEXEMPLAR = 362102;
        public const int DISREGARDFULNESS = 614099;
        public const int ORGANOGENICALLY = 356801;
        public const int THESAURUSEXTRAORDINARY = 615169;
        public const int CONTRACONSCIENTIOUSLY = 357538;
        public const int INDEFATIGABILIS = 613080;
        public const int DEPALATALIZATION = 358057;
        public const int THESAURUSCOMPARATIVE = 613233;
        public const int THESAURUSDOCUMENT = 358220;
        public const int THESAURUSINSUFFERABLE = 613580;
        public const int THESAURUSENVELOPE = 358056;
        public const int THESAURUSIMMINENT = 614174;
        public const int THESAURUSSOLICITUDE = 358192;
        public const int QUOTATIONSPENSERIAN = 613449;
        public const int THESAURUSCORRODE = 357955;
        public const int THESAURUSEXORCISE = 613633;
        public const int EPIGRAMMATISTĒS = 614036;
        public const int PREDETERMINATIVE = 360664;
        public const int THESAURUSLIMITATION = 614402;
        public const int INTERPENETRATION = 361078;
        public const string THESAURUSEPISODIC = "W20-7# ";
        public const int THESAURUSSURPASS = 217059;
        public const int THESAURUSPLEASURE = 349667;
        public const int THESAURUSCLIQUE = 219451;
        public const int THESAURUSMISUSE = 353199;
        public const int DIACETYLMORPHINE = 257342;
        public const int THESAURUSCOMPRISE = 349893;
        public const int THESAURUSDELINQUENT = 260502;
        public const int QUOTATIONMEIBOMIAN = 353255;
        public const int ECONOMETRICALLY = 217553;
        public const int THESAURUSIMPRISONMENT = 449670;
        public const int THESAURUSPORTABLE = 220776;
        public const int THESAURUSGUARDIAN = 453992;
        public const int THESAURUSINACTIVE = 256147;
        public const int QUOTATIONISOIONIC = 449685;
        public const int THESAURUSNOTWITHSTANDING = 259000;
        public const int UNREMUNERATIVENESS = 454253;
        public const int MONTMORILLONOID = 216929;
        public const int THESAURUSSHUTTER = 647960;
        public const int THESAURUSINAPPROPRIATE = 220496;
        public const int QUOTATIONBANDED = 654160;
        public const int APOLLINARIANISM = 255867;
        public const int BALANOPHORACEAE = 649083;
        public const int THESAURUSREMOVAL = 259488;
        public const int QUOTATION1ALENGTHEN = 654368;
        public const int GASTEROMYCETOUS = 217183;
        public const int THESAURUSPERFORMER = 748952;
        public const int ALSOOPISTHOCOELIAN = 221044;
        public const int THESAURUSINDIRECTLY = 754451;
        public const int THESAURUSMASSAGE = 255190;
        public const int THESAURUSCONVOCATION = 748179;
        public const int DISCOUNTENANCING = 261274;
        public const int THESAURUSINDICATION = 754789;
        public const string ALSOBROBDINGNAG = "26#，27#";
        public const string THESAURUSFRINGE = "DN15";
        public const string THESAURUSIMPRESSION = "\n处理中断。未找到有效楼层。";
        public const string THESAURUSWHITEN = "已有的管径标注将被覆盖，是否继续？";
        public const string COLLABORATIVELY = "Quetion";
        const string MLeaderLayer = VERIFICATIONISM;
        public static MLeader DrawMLeader(string content, Point2d p1, Point2d p2)
        {
            var e = new MLeader();
            e.ColorIndex = THESAURUSSENILE;
            e.MText = new MText() { Contents = content, TextHeight = THESAURUSENTREAT, ColorIndex = THESAURUSSENILE, };
            e.TextStyleId = GetTextStyleId(THESAURUSTRAFFIC);
            e.ArrowSize = UNDERACHIEVEMENT;
            e.DoglegLength = NARCOTRAFICANTE;
            e.LandingGap = NARCOTRAFICANTE;
            e.ExtendLeaderToText = UNTRACEABLENESS;
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            e.AddLeaderLine(p1.ToPoint3d());
            var bd = e.MText.Bounds.ToGRect();
            var p3 = p2.OffsetY(bd.Height + THESAURUSINDUSTRY).ToPoint3d();
            if (p2.X < p1.X)
            {
                p3 = p3.OffsetX(-bd.Width);
            }
            e.TextLocation = p3;
            e.Layer = MLeaderLayer;
            DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            return e;
        }
        public static MLeader DrawMLeader(string content, Point3d p1, Point3d p2)
        {
            var e = new MLeader();
            e.ColorIndex = THESAURUSSENILE;
            e.MText = new MText() { Contents = content, TextHeight = THESAURUSENTREAT, ColorIndex = THESAURUSSENILE, };
            e.TextStyleId = GetTextStyleId(THESAURUSTRAFFIC);
            e.ArrowSize = UNDERACHIEVEMENT;
            e.DoglegLength = NARCOTRAFICANTE;
            e.LandingGap = NARCOTRAFICANTE;
            e.ExtendLeaderToText = UNTRACEABLENESS;
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            e.AddLeaderLine(p1);
            var bd = e.MText.Bounds.ToGRect();
            var p3 = p2.OffsetY(bd.Height + THESAURUSINDUSTRY);
            if (p2.X < p1.X)
            {
                p3 = p3.OffsetX(-bd.Width);
            }
            e.TextLocation = p3;
            e.Layer = MLeaderLayer;
            DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            return e;
        }
        private static void ClearMLeader()
        {
            var adb = _DrawingTransaction.Current.adb;
            LayerTools.AddLayer(adb.Database, MLeaderLayer);
            foreach (var e in adb.ModelSpace.OfType<MLeader>().Where(x => x.Layer == MLeaderLayer))
            {
                adb.Element<Entity>(e.ObjectId, THESAURUSSEMBLANCE).Erase();
            }
        }
        public static void DrawBackToFlatDiagram(List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems, ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData, List<ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageDrawingData> drDatas, ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
        {
            var mlInfos = new List<MLeaderInfo>(PHOTOSENSITIZING);
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = NARCOTRAFICANTE; si < cadDatas.Count; si++)
            {
                var item = cadDatas[si];
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, THESAURUSNETHER).ToList();
                var dlinesGeosf = F(dlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                var fdsf = F(item.FloorDrains);
                var ppsf = F(item.VerticalPipes);
                var dpsf = F(item.DownWaterPorts);
                foreach (var dlinesGeo in dlinesGeos)
                {
                    var ok = UNTRACEABLENESS;
                    var segs = GeoFac.ToNodedLineSegments(GeoFac.GetLines(dlinesGeo).ToList()).Where(x => x.Length > THESAURUSSTRENGTHEN).ToList();
                    segs = GeoFac.GroupParallelLines(segs, THESAURUSOBSTREPEROUS, QUOTATIONEXOPHTHALMIC).Select(g => GeoFac.GetCenterLine(g, work_around: THESAURUSDEPOSIT)).ToList();
                    segs = GeoFac.ToNodedLineSegments(segs);
                    var _pts = segs.SelectMany(seg => new Point2d[] { seg.StartPoint, seg.EndPoint }).GroupBy(x => x).Select(x => x.Key).Distinct().ToList();
                    var pts = _pts.Select(x => x.ToNTSPoint()).ToList();
                    var ptsf = GeoFac.CreateIntersectsSelector(pts);
                    var fds = fdsf(dlinesGeo);
                    var pps = ppsf(dlinesGeo);
                    if (fds.Count == PHOTOGONIOMETER && pps.Count == ADRENOCORTICOTROPHIC)
                    {
                        var stPts = _pts.Where(x => fds.Any(fd => GRect.Create(x, THESAURUSCONSUL).ToPolygon().Intersects(fd))).ToList();
                        var edPts = _pts.Where(x => pps.Any(pp => GRect.Create(x, THESAURUSCONSUL).ToPolygon().Intersects(pp))).Except(stPts).ToList();
                        var mdPts = _pts.Except(stPts).Except(edPts).ToList();
                        if (stPts.Count == PHOTOGONIOMETER && edPts.Count == ADRENOCORTICOTROPHIC && mdPts.Count > NARCOTRAFICANTE)
                        {
                            var nodes = _pts.Select(x => new GraphNode<Point2d>(x)).ToList();
                            var kvs = new HashSet<KeyValuePair<int, int>>();
                            foreach (var seg in segs)
                                {
                                    var i = pts.IndexOf(seg.StartPoint.ToNTSPoint());
                                    var j = pts.IndexOf(seg.EndPoint.ToNTSPoint());
                                    if (i != j)
                                    {
                                        if (i > j)
                                        {
                                            ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref i, ref j);
                                        }
                                        kvs.Add(new KeyValuePair<int, int>(i, j));
                                    }
                                }
                            foreach (var kv in kvs)
                            {
                                nodes[kv.Key].AddNeighbour(nodes[kv.Value], ADRENOCORTICOTROPHIC);
                            }
                            var dijkstra = new Dijkstra<Point2d>(nodes);
                            var path1 = dijkstra.FindShortestPathBetween(nodes[_pts.IndexOf(stPts[NARCOTRAFICANTE])], nodes[_pts.IndexOf(edPts[NARCOTRAFICANTE])]);
                            var path2 = dijkstra.FindShortestPathBetween(nodes[_pts.IndexOf(stPts[ADRENOCORTICOTROPHIC])], nodes[_pts.IndexOf(edPts[NARCOTRAFICANTE])]);
                            var joinPts = path1.Select(x => x.Value).Intersect(path2.Select(x => x.Value)).Intersect(mdPts).ToList();
                            if (joinPts.Count == ADRENOCORTICOTROPHIC)
                            {
                                var joinPt = joinPts[NARCOTRAFICANTE];
                                var todoSegs = new HashSet<GLineSegment>(segs);
                                foreach (var pt in path1.TakeWhile(x => x.Value != joinPt).Select(x => x.Value))
                                {
                                    foreach (var seg in segs)
                                    {
                                        if (seg.Buffer(THESAURUSFACTOR).Intersects(pt.ToNTSPoint()))
                                        {
                                            if (seg.Length > THESAURUSINDUSTRY) mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSATAVISM));
                                            todoSegs.Remove(seg);
                                        }
                                    }
                                }
                                foreach (var pt in path2.TakeWhile(x => x.Value != joinPt).Select(x => x.Value))
                                {
                                    foreach (var seg in segs)
                                    {
                                        if (seg.Buffer(THESAURUSFACTOR).Intersects(pt.ToNTSPoint()))
                                        {
                                            if (seg.Length > THESAURUSINDUSTRY) mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSATAVISM));
                                            todoSegs.Remove(seg);
                                        }
                                    }
                                }
                                foreach (var seg in todoSegs)
                                {
                                    if (seg.Length > THESAURUSINDUSTRY) mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSOVERCHARGE));
                                }
                                ok = THESAURUSSEMBLANCE;
                            }
                        }
                    }
                    if (!ok)
                    {
                        if (dpsf(dlinesGeo).Any())
                        {
                            foreach (var seg in GeoFac.GetLines(dlinesGeo))
                            {
                                if (seg.Length > THESAURUSINDUSTRY && seg.IsHorizontalOrVertical(THESAURUSCONSUL) || seg.Length > THESAURUSINTENTIONAL) mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSOVERCHARGE));
                            }
                            ok = THESAURUSSEMBLANCE;
                        }
                    }
                    if (!ok)
                    {
                        foreach (var seg in GeoFac.GetLines(dlinesGeo))
                        {
                            if (seg.Length > THESAURUSINDUSTRY && seg.IsHorizontalOrVertical(THESAURUSCONSUL) || seg.Length > THESAURUSINTENTIONAL) mlInfos.Add(MLeaderInfo.Create(seg.Center, THESAURUSOVERCHARGE));
                        }
                        ok = THESAURUSSEMBLANCE;
                    }
                }
            }
            {
                var pts = mlInfos.Select(x => { var pt = x.BasePoint.ToNTSPoint(); pt.UserData = x; return pt; }).ToList();
                var ptsf = GeoFac.CreateIntersectsSelector(pts);
                void draw(string text, GRect r)
                {
                    if (r.IsValid)
                    {
                        foreach (var pt in ptsf(r.ToPolygon()))
                        {
                            ((MLeaderInfo)pt.UserData).Text = text;
                        }
                    }
                }
                var file = CadCache.CurrentFile;
                var name = System.IO.Path.GetFileName(file);
                if (name.Contains(THESAURUSPUBLISH))
                {
                    draw(SPLANCHNOPLEURE, new GRect(MADAGASCARIENSIS, ARCHITECTONICUS, COUNTERCURRENTS, THESAURUSGALVANIZE));
                    draw(THESAURUSATAVISM, new GRect(APOPHTHEGMATICAL, THESAURUSPRECURSOR, THESAURUSPOSSESSIVE, THESAURUSABSCESS));
                    draw(THESAURUSATAVISM, new GRect(INTERCONNECTEDNESS, THESAURUSAVAILABLE, THESAURUSCONCEPTION, SURREPTITIOUSNESS));
                    draw(THESAURUSATAVISM, new GRect(SUPEREXCELLENTLY, SUPERABUNDANTLY, THESAURUSSUBVERT, THESAURUSDECADENCE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISSENTIENT, THESAURUSVARNISH, MISCHIEVOUSNESS, THESAURUSCADENCE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSJANGLE, THESAURUSCARELESS, THESAURUSSCRIPT, THESAURUSSOCIALIZE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSIMMATERIAL, THESAURUSCOMEUPPANCE, THESAURUSINVOICE, THESAURUSCONVERSANT));
                    draw(THESAURUSATAVISM, new GRect(QUOTATION1BSTATUS, ALTWEIBERSOMMER, CRYSTALLOGENESIS, THESAURUSREFERENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCURATIVE, THESAURUSRACISM, THESAURUSINHIBIT, THESAURUSFINERY));
                    draw(THESAURUSOVERCHARGE, new GRect(QUOTATIONAUGUSTAL, THESAURUSINTENSIFY, UNSUBSTANTIATED, THESAURUSCHAPTER));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSVETERAN, THESAURUSACCEPTANCE, OPISTOGNATHIDAE, THESAURUSDEMARCATION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSGULLIBLE, THESAURUSFORCIBLE, THESAURUSINTRINSIC, THESAURUSPREDICTABLE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCOMMUNICATE, THESAURUSINTRUSIVE, THESAURUSLEGATION, QUOTATIONGRANULOMA));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSTHREADBARE, DRAMATISTICALLY, THESAURUSCOINCIDENCE, THESAURUSMONSTROSITY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBOUNDARY, TRIMETHYLBENZENE, THESAURUSUNEASY, IRRESISTIBILITY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEPLORABLE, THESAURUSMORBID, THESAURUSBLENCH, THESAURUSLENGTH));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONNEMEAN, EXPERIMENTATION, THESAURUSSUBJECT, THESAURUSAUTONOMY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSTHREADBARE, THESAURUSCONSTRAINT, ETHOXYPHENYLUREA, MULTILATERALISTS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSTREPIDATION, CHF2RORCF2RCHFCL, THESAURUSMANTLE, THESAURUSCHUBBY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSTRATEGY, THESAURUSUNBROKEN, THESAURUSSACKCLOTH, THESAURUSUNMANNERLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSNOBBERY, THESAURUSSORDID, THESAURUSOFFHAND, THESAURUSHERMIT));
                    draw(THESAURUSATAVISM, new GRect(AFFECTIONATENESS, THESAURUSBANQUET, THESAURUSIMPOSING, THESAURUSLAMENTATION));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONLETTERS, OLIGOMENORRHOEA, CHARACTERISTICUS, FORESIGHTEDNESS));
                    draw(THESAURUSATAVISM, new GRect(PREPONDERATINGLY, THESAURUSQUARREL, THESAURUSTRANSGRESS, CARBOXYANTHRANILIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSHUNGRY, THESAURUSDEVILISH, THESAURUSCHRISTEN, THESAURUSOFFENCE));
                    draw(THESAURUSATAVISM, new GRect(ALSOSPASMATICAL, THESAURUSUMPIRE, COSTERMONGERDOM, QUOTATIONGLANDULAR));
                    draw(THESAURUSATAVISM, new GRect(ALSOORCHESTRINA, HEILSGESCHICHTE, PHYSIOTHERAPIST, THESAURUSOUTPOURING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDECLAMATION, THESAURUSDIDACTIC, THESAURUSDEMURE, THESAURUSEXPORT));
                    draw(THESAURUSATAVISM, new GRect(THERMOGRAVIMETRY, THESAURUSCOLLIDE, UNCOMPOUNDEDNESS, MYRISTICAEFORMIS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFIGURATIVE, SENTIMENTALISTS, METHYLENEDIOXYMETHAMPHETAMINE, THESAURUSLIBERATOR));
                    draw(SPLANCHNOPLEURE, new GRect(MULTIPLICATIONAL, THESAURUSCOLLIDE, THESAURUSOMISSION, MYRISTICAEFORMIS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSWAGGLE, THESAURUSBURNING, THESAURUSREGARDING, THESAURUSPHOTOGRAPH));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMUSHROOM, THESAURUSVICTORIOUS, THESAURUSOPERATE, STERNOPTYCHIDAE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSPONTANEOUS, THESAURUSLACERATE, THESAURUSDILATE, THESAURUSABSTENTION));
                    draw(SPLANCHNOPLEURE, new GRect(ALSOHEAVENWARDS, THESAURUSTECHNIQUE, THESAURUSPALPABLE, DECONSTRUCTIONIST));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINFORMAL, THESAURUSBANDAGE, THESAURUSDEMORALIZE, THESAURUSVICTIMIZE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDWELLING, THESAURUSEXACTLY, IMPERISHABLENESS, IRREVOCABLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(DISPASSIONATENESS, THESAURUSATHLETIC, THESAURUSOUTLOOK, EXPRESSIONLESSLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSCENTED, THESAURUSSCRUFFY, KHARTOPHULAKION, ARKHITEKTONIKOS));
                    draw(SPLANCHNOPLEURE, new GRect(STRENGTHLESSNESS, THESAURUSOPPOSING, QUOTATIONWATTEAU, THESAURUSUNCONDITIONAL));
                    draw(THESAURUSATAVISM, new GRect(APPREHENSIVENESS, UNENDURABLENESS, RECOMBINATIONALLY, IRRECUPERABILIS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDIATRIBE, CONTROLLABILITY, CONCENTRATIVENESS, THESAURUSVIRILE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBELLOW, THESAURUSDEDUCE, THESAURUSLUKEWARM, IRRESISTIBLENESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUPPERMOST, THESAURUSCOMPREHEND, THESAURUSUNDISCIPLINED, THESAURUSKINDLY));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONANTIMONIOUS, QUOTATIONSAMIAN, THESAURUSBLIZZARD, INDISCRIMINATELY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSWEIGHT, THESAURUSBURNING, TERMINALIZATION, THESAURUSEXHIBITION));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCARNAL, THESAURUSCOALESCE, EXCREMENTITIOUS, THESAURUSPOISONOUS));
                    draw(THESAURUSATAVISM, new GRect(ULTRAMONTANISME, QUOTATION1AFILL, THESAURUSAPPLAUSE, PSYCHOPATHOLOGIST));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONTOUCHED, GYNANDROMORPHOUS, QUOTATIONTHEORY, THESAURUSABSORPTION));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONRADIOLARIAN, THESAURUSEXPIATE, THESAURUSSURPRISED, DISRESPECTFULNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEXULTANT, IMPROVIDENTNESS, THESAURUSCOMPASSION, THERMOELECTRICITY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSANTAGONIZE, ASTROPHYSICALLY, THESAURUSOPULENCE, THESAURUSOPTION));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSKITCHEN, QUOTATIONPROPOSITIONAL, TRAUMATOTROPISM, THESAURUSPROMULGATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSATTEND, THESAURUSFOOTSTEP, THESAURUSBLACKOUT, PALAEENCEPHALON));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSKITCHEN, THESAURUSCONFRONT, THESAURUSDICTATORIAL, OCCLUSOGINGIVAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCREEPY, THESAURUSTUNNEL, QUOTATIONBREWSTER, CONSUETUDINARIUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINESCAPABLE, ANTIPESTILENTIAL, THESAURUSCLIMATE, THESAURUSEMBRYONIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSREVIVAL, THESAURUSSABOTAGE, THESAURUSPEDESTAL, BIOLUMINESCENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCOUNSELLOR, THESAURUSDORMANT, THESAURUSCONCISE, THESAURUSFRUITION));
                    draw(THESAURUSATAVISM, new GRect(DEPARTMENTALIZE, THESAURUSSPOTLIGHT, ALPHABETIZATION, THESAURUSVIRTUE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSENORMOUS, THESAURUSFASHIONABLE, THESAURUSENDORSEMENT, THESAURUSTRANSITION));
                    draw(THESAURUSREDOUND, new GRect(THESAURUSINGRESS, THESAURUSDEFLECT, THESAURUSFIDDLE, THESAURUSIMPENDING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSAPPRECIATION, THESAURUSDENOUEMENT, THESAURUSGRANDMOTHER, SEMIMICROANALYSIS));
                    draw(THESAURUSATAVISM, new GRect(PREMILLENNIALIST, THESAURUSELICIT, ARISTOCRATICALLY, TRISYLLABICALLY));
                    draw(THESAURUSATAVISM, new GRect(PROGNOSTICATING, ULTRACENTRIFUGING, THESAURUSPERTAIN, THESAURUSEXECUTIVE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDESPOIL, THESAURUSINNOCENCE, THESAURUSCATARACT, THESAURUSBEATIFIC));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONQUEBEC, THESAURUSAFFECTING, THESAURUSMEASURE, THESAURUSEXPLOIT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFORTITUDE, HOLOCRYSTALLINE, THESAURUSFLATTERY, ELECTROENCEPHALOGRAPH));
                    draw(SPLANCHNOPLEURE, new GRect(PYROMETAMORPHISM, THESAURUSCONSUMPTION, THESAURUSHALLMARK, THESAURUSCONTEST));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSOPACITY, THESAURUSFACTUAL, SUPEREXCELLENCE, INDISPENSABLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(ACRIMONIOUSNESS, THESAURUSREFERENDUM, THESAURUSSCEPTICAL, THESAURUSHAPPILY));
                    draw(SPLANCHNOPLEURE, new GRect(LATITUDINARIANISM, THESAURUSINDEMNITY, THESAURUSALTERATION, THESAURUSTRANSCENDENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCRIPPLE, QUOTATIONCOSMOLOGICAL, NOVAESEELANDIAE, THESAURUSSLUMBER));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDISCOURTEOUS, THESAURUSPARADOX, THESAURUSINFLUENCE, THESAURUSFRUITFUL));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSNAMELY, THESAURUSTHRASH, THESAURUSFOREWARN, THESAURUSORIGINALITY));
                    draw(THESAURUSATAVISM, new GRect(UNADVISABLENESS, THESAURUSINDELICATE, THESAURUSANSWERABLE, MICROAEROPHILIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSPIRIT, THESAURUSABORIGINAL, THESAURUSEUPHEMISTIC, THESAURUSOVERLOAD));
                    draw(THESAURUSATAVISM, new GRect(PLAINSPOKENNESS, THESAURUSCOMPATRIOT, MALAPPORTIONMENT, HEILSGESCHICHTE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBEDRIDDEN, HEXYLRESORCINOL, THESAURUSMANUFACTURER, THESAURUSDEFORMITY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPRECONCEPTION, THESAURUSAUSTERE, THESAURUSSEVERE, MISTRUSTFULNESS));
                    draw(SPLANCHNOPLEURE, new GRect(UNDERNOURISHMENT, THESAURUSBOILING, THESAURUSSKIMPY, THESAURUSPROFUSE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSINGER, THESAURUSNECESSARILY, THESAURUSLESSON, THESAURUSANCESTOR));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONADIPOSE, THESAURUSEXPORT, THESAURUSENCOURAGE, THESAURUSSEESAW));
                    draw(SPLANCHNOPLEURE, new GRect(INDISCIPLINABLE, INCONSISTENCIES, THESAURUSCOMPETE, THESAURUSPERSUASION));
                    draw(SPLANCHNOPLEURE, new GRect(DISILLUSIONMENT, THESAURUSFORTUNE, UNPROPORTIONATE, THESAURUSTHICKNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSIRKSOME, DISINFLATIONARY, THESAURUSHOLLOW, THESAURUSRATION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSHAGGARD, THESAURUSHABITAT, PRESCRIPTIVENESS, THESAURUSCONVOY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSRELISH, DISFRANCHISEMENT, CLASSIFICATIONS, THESAURUSMAKESHIFT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINEXPRESSIVE, THESAURUSTRAINER, THESAURUSDEJECTED, THESAURUSCHUCKLE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSLIBERAL, VZAIMOPOMOSHCHI, UNTREATABLENESS, QUOTATIONWALDEYER));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONLYNDON, PRESTIGIOUSNESS, AEQUIPONDERATUS, THESAURUSLOQUACIOUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSHOODLUM, INTERPERSONALLY, THESAURUSENDURANCE, SLANTINDICULARLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSBETTERMENT, THESAURUSAPROPOS, THESAURUSINFORMATIVE, THESAURUSABSCESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSPITEFUL, THESAURUSHEAVENLY, THESAURUSGLUTTONOUS, PROPIONALDEHYDE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMEADOW, COMBUSTIBLENESS, THESAURUSWHIMPER, THESAURUSFOREWARN));
                    draw(THESAURUSATAVISM, new GRect(INDISTINGUISHABLY, THESAURUSCONFORMIST, THESAURUSCOSMETIC, THESAURUSFAMILIARITY));
                    draw(THESAURUSATAVISM, new GRect(RADIOSENSITIZER, THESAURUSREJECTION, THESAURUSOVERTHROW, THESAURUSEQUANIMITY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPARTLY, ALSOTHITHERWARDS, THESAURUSCUNNING, THESAURUSTENANT));
                    draw(THESAURUSATAVISM, new GRect(PSYCHOACOUSTICIAN, APOPHTHEGMATICALLY, THESAURUSRESERVATION, HIEROGRAMMATEUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSLITURGY, THESAURUSENRAPTURE, THESAURUSMONOTONOUS, QUOTATION1ABLACK));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINVOLVE, THESAURUSCONDUCT, THESAURUSINGENIOUS, EUAGGELIZESTHAI));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISRUPTION, THESAURUSCOURTSHIP, THESAURUSAPPROVE, MULTILATERALISTS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSOUTRAGEOUS, THESAURUSARCADE, THESAURUSARCHITECT, UNCONVENTIONALISM));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSRESTRAINED, THESAURUSWARNING, PHOTODYNAMICALLY, THESAURUSCORRESPONDENCE));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONSEEING, THESAURUSSATANIC, QUOTATIONTOUJOURS, QUOTATIONNITROUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUNDOUBTED, OVERELABORATELY, THESAURUSREFRACTORY, IRRESISTIBILITY));
                    draw(THESAURUSOVERCHARGE, new GRect(THESAURUSCORONET, THESAURUSACQUIT, NEUROPSYCHOLOGIST, THESAURUSTHICKNESS));
                    draw(THESAURUSOVERCHARGE, new GRect(AFFECTIONATENESS, THESAURUSMATRIMONIAL, THESAURUSLIMBER, THESAURUSESPOUSE));
                    draw(THESAURUSATAVISM, new GRect(REPRESENTATIVES, THESAURUSEARTHLY, INTERMARRIAGEABLE, APPRECIATIVENESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSNAVIGATION, THESAURUSEFFIGY, THESAURUSDOVETAIL, UNPARLIAMENTARILY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPREOCCUPATION, ALSOTRIBUNITIAL, THESAURUSPUBLICATION, THESAURUSSAVIOUR));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEVELOPMENT, CRUMENOPHTHALMUS, SUPERNUMERARIUS, QUOTATIONISTRIAN));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSHORRIFY, THESAURUSUNINHABITED, THESAURUSANNOYANCE, THESAURUSEXPENSE));
                    draw(SPLANCHNOPLEURE, new GRect(AUTOTROPHICALLY, QUOTATIONURINIFEROUS, THESAURUSCRABBED, PSEUDEPIGRAPHOUS));
                    draw(SPLANCHNOPLEURE, new GRect(PHANTASIESTÜCKE, SEROEPIDEMIOLOGICAL, THESAURUSIMPREGNABLE, THESAURUSBASEMENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMULTITUDE, THESAURUSCOFFER, COMMUNICATIVELY, THESAURUSRAGGED));
                    draw(THESAURUSATAVISM, new GRect(COLONIZATIONISTS, THESAURUSDISTURB, THESAURUSMANAGER, THESAURUSEXCELLENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSREPORT, QUOTATIONFUSIBLE, CHIROGRAPHARIUS, PROBABILISTICALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSECRETION, THESAURUSSUBSCRIPTION, THESAURUSBATTERY, SYMMETRICALNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDONATE, CONSTITUTIONALIST, COUNTERDISTINCTION, OVERDEVELOPMENT));
                    draw(THESAURUSATAVISM, new GRect(THERMODYNAMICALLY, THESAURUSATTITUDE, GLYCEROPHOSPHORIC, COCCOLITHOPHORE));
                    draw(THESAURUSATAVISM, new GRect(ORTHOGENETICALLY, THESAURUSZEALOTRY, GENTLEWOMANLINESS, FERROMAGNETICALLY));
                    draw(THESAURUSATAVISM, new GRect(MISCELLANEOUSNESS, THESAURUSADJOURNMENT, ELECTROCHROMISM, THESAURUSREGARD));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEATHLESS, COCCIDIOIDOMYCOSIS, PERICLYMENOIDES, THESAURUSMISLAY));
                    draw(THESAURUSATAVISM, new GRect(INTERCOMMUNICARE, THESAURUSSCULPTURE, ALSOCONVALESCENCY, THESAURUSCONTROL));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSWAMPY, COMPETITIVENESS, THESAURUSSPOUSE, ANTIPHLOGISTINE));
                }
                else if (name.Contains(THESAURUSBROACH))
                {
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCODDLE, THESAURUSFACETIOUS, THESAURUSLEARNED, THESAURUSSCRATCH));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSWOMANISH, THESAURUSINORDINATE, THESAURUSPENSIONER, HORC6H4RNHRCH2COOH));
                    draw(SPLANCHNOPLEURE, new GRect(CONSPICUOUSNESS, INTERCOMMUNICAT, QUOTATIONMAGNETO, THESAURUSBEHAVIOUR));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSTRANDED, THESAURUSIMPERIOUS, THESAURUSEXORDIUM, THESAURUSJOINTLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCONTEMPLATIVE, ECCLESIASTICIZE, THESAURUSINACCURACY, VERGISSMEINNICHT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOMPOSITE, THESAURUSASTRINGENT, THESAURUSEXCRESCENCE, THESAURUSDOWNHEARTED));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMINDFUL, QUOTATIONPHOTOCHEMICAL, REPRESENTATIONALIST, THESAURUSPETULANT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISINCENTIVE, QUOTATIONNEWARK, THESAURUSREALIZATION, PHARMACOKINETICS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEMBARRASSED, SUPRACHIASMATIC, QUOTATIONZODIACAL, THESAURUSMISJUDGE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISTRICT, THESAURUSINQUEST, THESAURUSINARTICULATE, THESAURUSRAUCOUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSGRUESOME, LITHOGRAPHICALLY, DENOMINATIONALLY, THESAURUSPLEASANTRY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSARISTOCRAT, THESAURUSDEFORMED, THESAURUSOBLIVIOUS, THESAURUSABNORMAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOUNTRY, THESAURUSSPECIAL, THESAURUSEMBEZZLE, THESAURUSENDEAVOUR));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSESSENCE, THESAURUSENDANGER, THESAURUSRARITY, THESAURUSENVIRONMENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEGRESS, DISHONOURABLENESS, THESAURUSGUARDED, PHYSIOPATHOLOGY));
                    draw(SPLANCHNOPLEURE, new GRect(SUBCATEGORIZING, THESAURUSNOTICE, THESAURUSSHOULDER, THESAURUSENGULF));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDEVICE, QUOTATIONTALKING, THESAURUSDEPENDABLE, QUOTATIONFOETAL));
                    draw(SPLANCHNOPLEURE, new GRect(CORRUPTIBLENESS, THESAURUSOBLIQUE, INTUSSUSCEPTION, SUPERVACANEOUSLY));
                    draw(THESAURUSATAVISM, new GRect(MISAPPROPRIATED, QUOTATIONPHTHALIC, DYSLOGISTICALLY, RETICULOENDOTHELI));
                    draw(THESAURUSOVERCHARGE, new GRect(THESAURUSRECRIMINATION, THESAURUSGLADDEN, SUPERORDINATING, THESAURUSACCUSATION));
                    draw(THESAURUSOVERCHARGE, new GRect(THESAURUSARROGANT, THESAURUSCERTAINLY, QUOTATIONMAUVAIS, THESAURUSUNUTTERABLE));
                    draw(THESAURUSOVERCHARGE, new GRect(THESAURUSMIDDLE, PSYCHOLINGUISTICALLY, THESAURUSBURGLAR, THESAURUSSINCERELY));
                }
                else if (name.Contains(THESAURUSWEATHER))
                {
                    draw(THESAURUSATAVISM, new GRect(UNCHARACTERISTIC, THESAURUSDISTORT, QUOTATIONPHRASAL, INADVENTUROUSNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEXONERATION, QUOTATIONFORESEEABLE, ARCHIEPISCOPALLY, QUOTATIONZYGOMATIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINAPPRECIABLE, INEQUITABLENESS, COMMISERATIVELY, THESAURUSTRICKERY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSECONDLY, THESAURUSTINGLE, THESAURUSLOQUACITY, BLOODTHIRSTINESS));
                    draw(THESAURUSATAVISM, new GRect(MISREPRESENTING, THESAURUSIMMODERATION, THESAURUSRESENTMENT, THESAURUSPREDOMINATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISCUSS, ANTHROPOLOGICALLY, THESAURUSACCLAIM, CIRCUMNAVIGABLE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFORMULA, THESAURUSCOMPETITION, THESAURUSRANCOUR, THESAURUSPLIABLE));
                    draw(SPLANCHNOPLEURE, new GRect(MECHANORECEPTOR, UNCONVENTIONALLY, THESAURUSRESTORE, POLYSYNTHETICALLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSHOTCHPOTCH, THESAURUSFRUGAL, THESAURUSIRREVERSIBLE, NH2CH2C6H10COOH));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSPRINKLING, THESAURUSPHILOSOPHER, THESAURUSHYPOCRISY, THESAURUSDUPLICATE));
                    draw(THESAURUSATAVISM, new GRect(HYDROCHLOROTHIAZIDE, PRETENSIOUSNESS, SOPHISTICATIONS, THESAURUSIMPERCEPTIBLE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMAYHEM, QUOTATIONCHALCEDONIAN, THESAURUSASPIRATION, THESAURUSLATTER));
                    draw(SPLANCHNOPLEURE, new GRect(PSYCHODIAGNOSTICS, THESAURUSADMINISTRATOR, THESAURUSDIVERGENCE, THESAURUSIRREGULAR));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONREQUIRED, THESAURUSENCOMPASS, ECHOENCEPHALOGRAPHY, THESAURUSFISSION));
                    draw(SPLANCHNOPLEURE, new GRect(BRACHYCEPHALOUS, OPHTHALMOLOGISTS, POLYGRAPHICALLY, THESAURUSMYSTERY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSARBITRARY, THESAURUSABSOLUTE, THESAURUSINFURIATE, THESAURUSENLIGHTENMENT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEGENERATE, SEMICONSCIOUSNESS, NEUROPSYCHIATRY, ALSOPENTOBARBITAL));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONIBICENCAN, THESAURUSJAILER, THESAURUSCHIRPY, THESAURUSGRANULE));
                    draw(THESAURUSATAVISM, new GRect(FLUOROCARBONATE, THESAURUSACADEMIC, QUOTATIONMAILLE, INDISCRIMINATIVELY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCAREER, THESAURUSNASTINESS, THESAURUSTELEPHONE, THESAURUSIDIOCY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSBRAVADO, THESAURUSEXASPERATION, THESAURUSFEATHER, THESAURUSBEFRIEND));
                    draw(THESAURUSATAVISM, new GRect(SUPERFICIALNESS, THESAURUSTREMENDOUS, THESAURUSMISPLACE, AUTOSCHEDIASTIC));
                    draw(SPLANCHNOPLEURE, new GRect(ANTICHOLINERGIC, THESAURUSCOSTUME, THESAURUSSUPPORT, THESAURUSINEFFABLE));
                    draw(THESAURUSATAVISM, new GRect(ENTERCOMMUNICATION, QUOTATIONNONAGESIMAL, ALSOVITRESCENCY, RHOMBENCEPHALON));
                    draw(THESAURUSATAVISM, new GRect(NOVAEHOLLANDIAE, THESAURUSDISPATCH, THESAURUSUNDECEIVE, THESAURUSONLOOKER));
                    draw(THESAURUSATAVISM, new GRect(ANTEPENULTIMATE, THESAURUSPERTURB, THESAURUSABJURE, QUOTATIONPANDEAN));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFORGERY, PSYCHOTHERAPIST, THESAURUSSEEMLY, THESAURUSEXCISE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSREQUIREMENT, MICROMINIATURIZE, THESAURUSFRATERNIZE, CYTOMEGALOVIRUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPOTENTATE, THESAURUSNIMBLE, SPINOCEREBELLAR, DISCIPLINARIANISM));
                    draw(SPLANCHNOPLEURE, new GRect(ARTERIOSCLEROTIC, KHRUSOMĒLOLONTHION, THESAURUSCHARGE, THESAURUSTERMINUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSIRRITABLE, QUOTATIONNONAGESIMAL, BIOSTRATIGRAPHER, THESAURUSREGRET));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSLAWYER, THESAURUSCANDOUR, THESAURUSEVANGELIZE, THESAURUSCONDITIONED));
                    draw(THESAURUSATAVISM, new GRect(NANOTECHNOLOGICAL, EXTERRITORIALLY, THESAURUSEGGHEAD, THESAURUSASSAILANT));
                    draw(THESAURUSATAVISM, new GRect(CONVENTIONALISE, THESAURUSCONSCIENCE, CHLORTETRACYCLINE, THESAURUSNARCISSISM));
                    draw(SPLANCHNOPLEURE, new GRect(PHYSIOLOGICALLY, THESAURUSDISGUISE, THESAURUSPARTITION, THESAURUSANNOTATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSLICENTIOUS, THESAURUSGROUND, SUPRADECOMPOUND, THESAURUSELEMENTAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSGENTEEL, THESAURUSEXPOSITION, MALDISTRIBUTION, THESAURUSTERROR));
                    draw(THESAURUSATAVISM, new GRect(PHOTOCONDUCTIVITY, SPLANCHNOCRANIUM, BENZENEHEXACARBOXYLIC, THESAURUSINCLINE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFOLKLORE, THESAURUSNEGOTIATION, THESAURUSENTRAP, PSYCHOLOGICALLY));
                    draw(THESAURUSATAVISM, new GRect(STEREOGRAPHICUS, THESAURUSCANDOUR, THESAURUSCONSENSUS, THESAURUSLIONIZE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSVERSATILE, THESAURUSCOUNTERMAND, THESAURUSANALOGY, THESAURUSCARESS));
                    draw(SPLANCHNOPLEURE, new GRect(PRAEMONSTRATENSIS, THESAURUSCREDIT, THESAURUSTHICKEN, TRANSFINALIZATION));
                    draw(SPLANCHNOPLEURE, new GRect(SUPERNATURALISM, HYDROCOTYLACEAE, THESAURUSEXCESSIVE, THESAURUSQUALIFIED));
                    draw(SPLANCHNOPLEURE, new GRect(CONTRARIOUSNESS, ZANNICHELLIACEAE, THESAURUSNUMEROUS, THESAURUSEXEMPTION));
                    draw(SPLANCHNOPLEURE, new GRect(PLATITUDINARIANISM, THESAURUSBEHOVE, THESAURUSLARGELY, THESAURUSDEGREE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPROFITABLE, THESAURUSSUNLESS, THESAURUSOPPOSITION, THESAURUSSULLEN));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSUBSTANCE, MISRECOLLECTION, THESAURUSMERRIMENT, MULTIPLICATIVUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCIRCUMSCRIBE, QUOTATIONANDAMAN, EUPHEMISTICALLY, CONCUPISCIBILIS));
                    draw(SPLANCHNOPLEURE, new GRect(RECONVALESCENCE, MICROCIRCULATION, THESAURUSACCENT, HYDROELECTRICITY));
                    draw(SPLANCHNOPLEURE, new GRect(ERYTHROPHTHALMA, QUOTATIONDORIAN, THESAURUSFINITE, THESAURUSCHANNEL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPERPETUITY, THESAURUSCOLLATE, THESAURUSFINITE, QUOTATIONARSENIC));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONPOSTAL, THESAURUSIMMERSE, THESAURUSCOOPERATE, THESAURUSMESSAGE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSRESULT, PHILANTHROPISTS, THESAURUSMEDICINE, QUOTATIONCUSTOS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSACHIEVE, COMPARTMENTALLY, THESAURUSNASCENT, COMMISERATINGLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPHOBIA, DISCIPLINARIANS, THESAURUSOBSEQUIOUS, PALAEOPATHOLOGY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSVISIBLE, THESAURUSCUTTING, SUPERCLUSTERING, THESAURUSRESTRAINT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINSANE, TRICHLOROPHENOXYACETIC, MONOCHROMATICALLY, CONSEQUENTIALNESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSORATORICAL, CONSIDERABLENESS, THESAURUSDISRESPECT, MICROINSTRUCTIONS));
                }
                else if (name.Contains(THESAURUSIMPOLITE))
                {
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONMARVELL, THESAURUSHENCHMAN, THESAURUSQUAGMIRE, UNNEIGHBOURLINESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPHRASEOLOGY, THESAURUSSARDONIC, THESAURUSIDOLIZE, LEPIDOPTEROLOGY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBLASPHEME, THESAURUSAMMUNITION, THESAURUSINDIVIDUAL, THESAURUSCOMICAL));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONAMYGDALOID, THESAURUSREPEAT, PLUVIOMETRICALLY, THESAURUSRECOLLECTION));
                    draw(THESAURUSATAVISM, new GRect(UNDEMANDINGNESS, THESAURUSPRINTER, CONTEMPLATIVENESS, THESAURUSINCURSION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBESEECH, THESAURUSREVERENCE, THESAURUSBESIDE, THESAURUSVEXATIOUS));
                    draw(THESAURUSATAVISM, new GRect(TRANSILLUMINATING, THESAURUSSPROUT, THESAURUSRASCAL, THESAURUSINEXTINGUISHABLE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPASTICHE, THESAURUSREPLENISH, DEMINERALIZATION, PYROTECHNICALLY));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONDICKENS, EXTRAORDINARILY, PHOTOMULTIPLIER, VASOCONSTRICTING));
                    draw(SPLANCHNOPLEURE, new GRect(NONDISCRIMINATING, THESAURUSADVANCEMENT, THESAURUSOVERTONE, THESAURUSPADDING));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOMMERCE, THESAURUSFOREFATHER, THESAURUSDRINKER, RECONSOLIDATION));
                    draw(THESAURUSATAVISM, new GRect(OLIGODENDROCYTES, INCONSIDERATENESS, THESAURUSSYMPATHIZE, DEUTERANOMALOUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPAUPER, THESAURUSSWATHE, THESAURUSORIGINATE, QUOTATIONDERNIER));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFORGOTTEN, MAGNETOCARDIOGRAPH, THESAURUSSUBTLETY, UNSYSTEMATICALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINSTIGATE, ANTHROPOCENTRIC, MISUNDERSTANDINGS, THESAURUSHIBERNATE));
                    draw(SPLANCHNOPLEURE, new GRect(TETRAHEXAHEDRON, QUOTATIONCAPIAS, THESAURUSRECORDING, THESAURUSMISBEGOTTEN));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONUNDULATORY, THESAURUSSPARKLE, THESAURUSHOSIERY, THESAURUSEXPECTANCY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDEFECTIVE, INSTRUMENTALITY, QUOTATIONBOURBON, THESAURUSCRUMBLE));
                    draw(THESAURUSATAVISM, new GRect(ACCOMMODATIONAL, THESAURUSPALACE, THESAURUSEMERGENT, SCROPHULARIACEAE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSATTACH, UNAPPEASABLENESS, THESAURUSJUNGLE, SPHAEROCEPHALUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPOLITIC, PHTHISIOTHERAPIST, THESAURUSEXTENSION, THESAURUSHIGHFALUTIN));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSECONOMIC, THESAURUSPLAYBOY, RECONVALESCENCE, SPACEWORTHINESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMIRACULOUS, TREASONABLENESS, ENTRECHANGEMENT, INDISSOLUBLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(ORTHOSTATICALLY, THESAURUSSUSPENSE, THESAURUSRECONCILE, QUOTATIONQUARTAN));
                    draw(SPLANCHNOPLEURE, new GRect(PHTHISIOTHERAPY, LEXICOSTATISTICALLY, COMMUNICATIONAL, THESAURUSAPPETITE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEXPRESSIONLESS, DETERMINATIVELY, THESAURUSDISTASTE, QUOTATIONCOCKEYE));
                    draw(THESAURUSATAVISM, new GRect(PSYCHOBIOLOGICAL, QUOTATIONALFVÉN, QUOTATIONUGANDA, THESAURUSADDICTION));
                    draw(THESAURUSATAVISM, new GRect(PHOTOMULTIPLIERS, ANTHROPOMORPHITES, THESAURUSSNATCH, CONSUBSTANTIALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSTREAK, THESAURUSDETERMINED, THESAURUSVACATION, THESAURUSEMPHASIZE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEMAND, CONSEQUENTIALIST, CHEMOAUTOTROPHICALLY, THESAURUSREFINEMENT));
                    draw(THESAURUSATAVISM, new GRect(CHROMATOGRAPHIC, DISINTEGRATIVELY, THESAURUSGOSPEL, THESAURUSESCAPE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUNQUESTIONABLE, THESAURUSEXPLOSION, ENTREPRENEURIALISM, TETRACHLORODIBENZO));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONDOPPLER, THESAURUSINVITATION, THESAURUSMENTALLY, DISADVANTAGEOUS));
                    draw(THESAURUSATAVISM, new GRect(DECAHYDRONAPHTHALENE, THESAURUSEMPORIUM, QUOTATION1ARAMUS, THESAURUSSTRIPLING));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONEQUIANGULAR, THESAURUSOFFICIAL, QUOTATIONAMERICAN, THESAURUSDECORATIVE));
                    draw(SPLANCHNOPLEURE, new GRect(RESOURCEFULNESS, THESAURUSAWKWARD, DISCRIMINATIVELY, GESAMTKUNSTWERK));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSIMPRECISE, HYDROXYPROPANOIC, TRIGONOMETRICAL, THESAURUSPRETENDED));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSTRAVERSE, THESAURUSMULTIFARIOUS, PHOTOMACROGRAPHY, RESTRICTIONISTS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMINDLESS, THESAURUSEMINENTLY, THESAURUSUNASSAILABLE, THESAURUSMIDDLEMAN));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONSHELLEY, THESAURUSLUMINARY, THESAURUSJEJUNE, THESAURUSLEANING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPLAYER, TINTINNABULATED, THESAURUSCOHORT, THESAURUSCOLLEGE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCONTRADICTORY, THESAURUSEMPHASIS, INTERLAMINATING, ELECTROPAINTING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFLAMING, THESAURUSSTRENGTH, THESAURUSCHARGE, THESAURUSOBLIGATION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBUBBLE, THESAURUSEXCISE, PHOTOFLUOROGRAMS, THESAURUSSPOKEN));
                    draw(THESAURUSATAVISM, new GRect(UNCHALLENGEABLE, THESAURUSGARGANTUAN, UNSOPHISTICATED, THESAURUSMISCHIEF));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSWELLING, CIRCUMCELLIONES, THESAURUSVACUUM, THESAURUSFLUCTUATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSBOLSTER, THESAURUSUNAWARE, PICTURESQUENESS, THESAURUSSOVEREIGNTY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFOREGONE, THESAURUSEXPONENT, QUOTATIONCRUCIATE, THESAURUSFEMININE));
                    draw(SPLANCHNOPLEURE, new GRect(INSURRECTIONIST, IATROMATHEMATICUS, THESAURUSFRAGILE, CONSIGNIFICATIO));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSNATION, THESAURUSSCENERY, THESAURUSPROPRIETY, THESAURUSCOMBINE));
                    draw(THESAURUSATAVISM, new GRect(INEFFECTUALNESS, ALSOGENICULATED, THESAURUSQUAINT, UNPREJUDICEDNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCONTAIN, THESAURUSSEGMENT, THESAURUSSYCOPHANTIC, THESAURUSBROKEN));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONSILESIAN, THESAURUSKISMET, THESAURUSINCONTINENT, THESAURUSFURROW));
                    draw(THESAURUSATAVISM, new GRect(REINTERPRETATION, QUOTATIONPAIRED, THESAURUSJACKPOT, OBSTRUCTIONISTS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMISOGYNIST, THESAURUSDECEIT, OVERCAUTIOUSNESS, PLECTONEMICALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMOTIONLESS, STEREOREGULATING, THESAURUSCANDID, QUOTATION2ASTADIAL));
                    draw(THESAURUSATAVISM, new GRect(DOLICHOCEPHALISM, THESAURUSCOLLECTIVE, QUOTATIONJUSTINIANIAN, THESAURUSADJUST));
                    draw(SPLANCHNOPLEURE, new GRect(PARTICULARISMUS, SYLLABIFICATION, THESAURUSPRODIGAL, PEROXOSULPHURIC));
                    draw(SPLANCHNOPLEURE, new GRect(MELODRAMATICALLY, ZOOIDTRANSFORMER, PREMEDITATEDNESS, THESAURUSLIBATION));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMACERATE, THESAURUSLAGOON, THESAURUSMINUTELY, DISACKNOWLEDGEMENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSILLEGAL, NEUROANATOMICAL, DISPROPORTIONABLENESS, THESAURUSCLIENT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINHIBITION, DIASTATOCHROMOGENES, RESOURCELESSNESS, PROPRIETORIALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINDEPENDENT, THESAURUSDEBONAIR, INDISCRIMINATINGLY, PROSELYTIZATION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEMBROIDER, THESAURUSINSPIRE, THESAURUSENHANCE, THESAURUSMONUMENTAL));
                    draw(THESAURUSATAVISM, new GRect(UNDEREMPLOYMENT, THESAURUSDIRECTOR, THESAURUSIMPERFECTION, REACTIONARINESS));
                    draw(THESAURUSATAVISM, new GRect(METHYLTHIOURACIL, THESAURUSBUCOLIC, UNPROTECTEDNESS, THESAURUSLUMINARY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEVENTUAL, MEPHISTOPHELIAN, BIOTECHNOLOGIST, EXTERRITORIALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSSEGREGATE, THESAURUSHERALD, NEUROTRANSMITTERS, THESAURUSTIMETABLE));
                    draw(THESAURUSATAVISM, new GRect(CEPHALOCHORDATE, QUOTATIONKEPLER, REINTERMEDIATION, THESAURUSINFLEXIBLE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCOMPONENT, THESAURUSIMPROVISE, THESAURUSESTABLISHMENT, THESAURUSSWALLOW));
                    draw(SPLANCHNOPLEURE, new GRect(UNDISSEMBLINGLY, THESAURUSLUBBERLY, DIBENZANTHRACENE, ALIMENTATIVENESS));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONLUMBAR, THESAURUSDEVOTED, QUOTATIONCONNOTATIVE, QUOTATIONCARTHAGINIAN));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSUNEARTH, THESAURUSCONDITIONED, THESAURUSSTRIKE, CONDYLARTHROSIS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSUNEARTH, THESAURUSINFREQUENT, THESAURUSDETRITUS, HYPOMAGNESAEMIA));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSMILKSOP, DIAPHOTOTROPISM, THESAURUSGIFTED, DISCONTINUATION));
                    draw(THESAURUSATAVISM, new GRect(PSYCHODRAMATICS, THESAURUSCOUNTERACT, THESAURUSFOREGROUND, HELIOTROPICALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCAFETERIA, INSCRUTABLENESS, THESAURUSSCANDALOUS, THESAURUSIMMORTALITY));
                    draw(THESAURUSATAVISM, new GRect(DIFFERENTIATION, THESAURUSGABBLE, THESAURUSWASTED, THESAURUSBREATHLESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEXORBITANT, TETRAHYDROXYHEXANEDIOIC, THESAURUSMORTIFY, THESAURUSRENOWNED));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUNRELIABLE, CRYSTALLOGRAPHICALLY, THESAURUSLIVELIHOOD, THESAURUSGLIMMER));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCAVERNOUS, THESAURUSSEVERAL, TRANSUBSTANTIALISM, THESAURUSCONVENIENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSIMPECUNIOUS, THESAURUSAPPRAISE, THESAURUSDEPLETION, THESAURUSPRESENTIMENT));
                    draw(THESAURUSATAVISM, new GRect(EXCOMMUNICATIVE, THESAURUSINTELLIGENCE, POLIOENCEPHALITIS, UNCONSCIENTIOUS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSLAVISH, GENTLEMANLINESS, THESAURUSMAGNANIMITY, CONSOCIATIONALISM));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMAGNETISM, HYPERCONJUGATION, MICROLEPIDOPTERA, THESAURUSMINSTREL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDUFFER, THESAURUSADJACENT, SPECTROCHEMISTRY, THESAURUSSPINELESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFRACTIOUS, FLUOROPHOTOMETRY, THESAURUSRESIGN, THESAURUSGENERIC));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFLABBY, THESAURUSABHORRENT, THESAURUSINEQUALITY, QUOTATIONHALCYON));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEXCOMMUNICATE, THESAURUSSHORTAGE, THESAURUSFETTER, THESAURUSOBDURATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOSMIC, NORTESTOSTERONE, QUOTATIONQUEENSLAND, QUOTATIONHUNGARY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCORROSIVE, QUOTATIONWOLFFIAN, ULTRAMICROSCOPICALLY, THESAURUSPERSONALIZE));
                    draw(SPLANCHNOPLEURE, new GRect(UNCOMMUNICATING, THESAURUSMAXIMUM, THESAURUSDISGORGE, DIAHELIOTROPISM));
                    draw(SPLANCHNOPLEURE, new GRect(ETHNOMUSICOLOGY, QUOTATIONSTELLER, IATROMECHANICIAN, INFRALAPSARIANISM));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPIRATE, INCONSIDERATION, INAPPROPRIATELY, QUOTATION1AFALL));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSGOSSAMER, THESAURUSBARRISTER, PTEROYLMONOGLUTAMIC, THESAURUSELASTICITY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFAILING, THESAURUSCONSTRAIN, THESAURUSDEPLETE, QUOTATIONZENKER));
                    draw(THESAURUSATAVISM, new GRect(INEFFICACIOUSNESS, THESAURUSFEROCIOUS, DEZINFORMATSIYA, THESAURUSTAMPER));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSOPERATOR, THESAURUSCOMFORTABLE, HYDROXYBUTANOIC, UNCHRISTIANLIKE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDIPLOMAT, THESAURUSIMPROVIDENT, THESAURUSPASTORAL, THESAURUSPASSIVE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDIPLOMAT, SULPHAEMOGLOBIN, PSEUDOSOLARIZATION, THESAURUSGIMCRACK));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEMENDATION, QUOTATIONAZYGOS, OTHERWORLDLINESS, THESAURUSINCAUTIOUS));
                    draw(SPLANCHNOPLEURE, new GRect(PHOSPHODIESTERASE, PSYCHOGENICALLY, THERMOREGULATES, THESAURUSSCHISM));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSORDINARY, THESAURUSSINISTER, DISENTHRONEMENT, THESAURUSFONDLE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCANKER, CH2OHRCHOHRCH2OH, THESAURUSPROTECTION, THESAURUSMAGICIAN));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINELIGIBLE, THESAURUSTRAGIC, THESAURUSGETAWAY, MICROMETEOROIDS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSILLUSTRATIVE, TROCKENBEERENAUSLESE, IDENTITÄTSPHILOSOPHIE, THESAURUSHOMELESS));
                    draw(THESAURUSATAVISM, new GRect(GOSUDARSTVENNOE, CONSCRIPTIONIST, THESAURUSACOLYTE, NATIONALISATION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEVOTEE, QUOTATIONJONATHAN, THESAURUSSLIVER, THESAURUSOBJECTION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSAPPLAUD, THESAURUSDERIDE, QUOTATION2BPUBLISHER, MICROCRYSTALLINE));
                }
                else if (name.Contains(SYSTEMATIZATION))
                {
                    draw(SPLANCHNOPLEURE, new GRect(SCHRAMMELQUARTETT, THESAURUSVOLLEY, QUOTATIONIBIZAN, THESAURUSABRIDGE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPILLAGE, THESAURUSSUBWAY, THESAURUSDOGSBODY, UNCHANGEABLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDELECTABLE, THESAURUSFIDGETY, INTELLECTUALISING, THESAURUSDISCLOSURE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSFATHERLY, UNTRACTABLENESS, PARAGRAPHICALLY, THESAURUSGRISLY));
                    draw(SPLANCHNOPLEURE, new GRect(MOUTHIMPROVIDENTLY, THESAURUSESPECIAL, THESAURUSENLIST, THESAURUSINDICTMENT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCONSCRIPT, THESAURUSMETTLE, ENCEPHALISATION, THESAURUSMIGRANT));
                    draw(THESAURUSATAVISM, new GRect(ALSOORTHOPRAXIS, THESAURUSSOVEREIGN, PROFIBRINOLYSIN, MAXILLOPALATINE));
                    draw(THESAURUSATAVISM, new GRect(INCREDULOUSNESS, ALSOTHAUMATURGIST, UNCHARITABLENESS, UNSPORTSMANLIKE));
                    draw(THESAURUSATAVISM, new GRect(CONSTRUCTIVISTS, IONTOPHORETICALLY, THESAURUSCOLOSSAL, THESAURUSCONSERVATORY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDIALOGUE, UNPROFITABLENESS, THESAURUSFIGUREHEAD, TRANSMUTATIONIST));
                    draw(THESAURUSATAVISM, new GRect(UNCONDITIONALLY, QUOTATIONFATHER, THESAURUSTHREAT, EMPOVERISSEMENT));
                    draw(THESAURUSATAVISM, new GRect(UNINDIVIDUALIZED, SPECULATIVENESS, QUOTATION3BLARGE, THESAURUSMORNING));
                    draw(THESAURUSATAVISM, new GRect(CORYNEBACTERIUM, THESAURUSBEWITCH, THESAURUSCORSAIR, THESAURUSSWEETEN));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSVINTAGE, VESTIBULOCOCHLEAR, PHENOLPHTHALEIN, UNCONSTITUTIONALLY));
                    draw(THESAURUSATAVISM, new GRect(MONOSYLLABICITY, STRATIGRAPHICAL, UNCOMPREHENDINGLY, QUOTATIONMERINGUE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSREQUIRED, THESAURUSINFERNAL, THESAURUSWRENCH, QUOTATION1BPARASITIC));
                    draw(THESAURUSATAVISM, new GRect(UNCOMPREHENDINGLY, THESAURUSOUTGOING, THESAURUSCRITICAL, THESAURUSAFFRAY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCONCILIATE, THESAURUSRESOURCE, QUOTATIONCATHOLIC, THESAURUSBRACKET));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSARTFUL, THESAURUSCLERICAL, THESAURUSLONELINESS, THESAURUSVIRGINAL));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONEXTERIOR, QUOTATIONPUERPERAL, THESAURUSDELIVERY, IRRESPONSIBLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSARRIVE, PARALLÉLOGRAMME, UNENTHUSIASTICALLY, QUOTATIONSHASTA));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSREVELLER, THESAURUSPORCELAIN, VYALOTEKUSHCHIĬ, THESAURUSFLYING));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSALLOTMENT, THESAURUSVEHICLE, THESAURUSPORTLY, THESAURUSDESPONDENT));
                    draw(SPLANCHNOPLEURE, new GRect(WASHINGTONIANUM, THESAURUSINVENTION, THESAURUSEQUITABLE, INTROPUNITIVENESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSACCUMULATION, THESAURUSGAFFER, THESAURUSDISSATISFIED, THESAURUSACCURACY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBROOCH, THESAURUSSKELETON, THESAURUSBOOMERANG, SUCCESSLESSNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDEXTERITY, SPECTROCHEMICALLY, THESAURUSTROUSERS, BRACHISTOCHRONE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPROVINCIAL, TENDENTIOUSNESS, GEOMORPHOLOGICAL, THESAURUSOFFICIATE));
                    draw(SPLANCHNOPLEURE, new GRect(TUBERCULOSTATIC, MISREPRESENTATIVE, COMMUNICATIVENESS, QUOTATIONPLEOCHROIC));
                    draw(THESAURUSATAVISM, new GRect(PNEUMATOMACHIANS, SPLANCHNOLOGIST, ACKNOWLEDGEMENTS, THESAURUSCREDIBILITY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDESERVE, THESAURUSEXHILARATE, THESAURUSSUCCULENT, PROTONEPHRIDIUM));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSBYSTANDER, CATEGOREMATICALLY, THESAURUSWOODED, THESAURUSPROVINCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINTERFERENCE, BIOGEOGRAPHICAL, THESAURUSINDISPUTABLE, THESAURUSPRETEXT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINVULNERABLE, THESAURUSOUTGOING, THESAURUSTACITURN, UNREMITTINGNESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUNWORLDLY, QUOTATIONTHACKERAY, INDUBITABLENESS, THESAURUSTRANSIT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSNARRATIVE, QUOTATION1ADIGITAL, INHARMONIOUSNESS, THESAURUSDEFECT));
                    draw(SPLANCHNOPLEURE, new GRect(INVOLUNTARINESS, THESAURUSHEIGHT, THESAURUSBREAST, HYPERTRIGLYCERI));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOMMENT, THESAURUSDELUDE, THESAURUSDISFAVOUR, THESAURUSTOLERANCE));
                    draw(SPLANCHNOPLEURE, new GRect(UNDANGEROUSNESS, STEREOSPECIFICALLY, THESAURUSEXTINCTION, QUOTATION1ABARRY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCONSIDERABLE, NEUROTRANSMISSION, THESAURUSWOMANKIND, THESAURUSITEMIZE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMIDDLING, THESAURUSFORSAKE, THESAURUSDRINKABLE, SPERMATOGENESIS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSNEWSPAPER, THESAURUSFLUCTUATION, IRREPROACHABLENESS, THESAURUSDECEPTIVE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPROPERTY, THESAURUSEXHAUST, THESAURUSCOPIOUS, THESAURUSSTRATEGIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPATTER, CONVERSATIONALISTS, THESAURUSDASTARDLY, COMPANIABLENESS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPREPARATION, THESAURUSREPRIEVE, THESAURUSEMISSION, THESAURUSMISCARRY));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONDUCTED, THESAURUSFLOWER, INTELLIGIBLENESS, QUOTATIONCHROMIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSRARELY, THESAURUSROTTEN, THESAURUSGLACIAL, THESAURUSAUDACIOUS));
                    draw(THESAURUSATAVISM, new GRect(ALSOMONONUCLEATE, THESAURUSTENDENCY, JUNGERMANNIALES, PRISCILLIANISTE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMORGUE, INDECIPHERABLENESS, INSENSITIVITIES, HERMENEUTICALLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSUBTRACT, THESAURUSEXPEDIENT, SUBLAPSARIANISM, THESAURUSMENTALITY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEXECUTIONER, VISCERALIZATION, DIALLYLBARBITURIC, THESAURUSACCENTUATE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSGYRATE, THESAURUSMONSTROUS, DISPROPORTIONED, INTERNATIONALLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSUCCOUR, TRIBOLUMINESCENCE, QUOTATION1BMEDITERRANEAN, THESAURUSPLEASANT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISEMBOWEL, THESAURUSINCIDENCE, THESAURUSVOLATILE, THESAURUSCULPABLE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSRECESSION, INSIGNIFICATIVE, THESAURUSCOMBINATION, THESAURUSCOVERAGE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEXCRETE, THESAURUSTRUSTY, THESAURUSRIGHTEOUSNESS, FRAGMENTARINESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSREFUGEE, THESAURUSLEATHERY, THESAURUSBURGEON, QUOTATIONPERFORANT));
                    draw(SPLANCHNOPLEURE, new GRect(PROSLAMBANOMENOS, THESAURUSSPHERICAL, THESAURUSPROMONTORY, THESAURUSREFUND));
                    draw(SPLANCHNOPLEURE, new GRect(ORTHOSTEREOSCOPICALLY, QUOTATIONJULIAN, THESAURUSABLAZE, THESAURUSSHOCKING));
                    draw(SPLANCHNOPLEURE, new GRect(PALAEOLIMNOLOGY, THESAURUSSCENARIO, THESAURUSHURRICANE, IMPRESSIONISTICALLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEFFERVESCENT, THESAURUSDISCRIMINATE, THESAURUSPHILANDER, QUOTATIONCHARACTERISTIC));
                }
                else if (name.Contains(RECOLLECTIVENESS))
                {
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCONSIDERATION, EICOSAPENTAENOIC, THESAURUSTEMERITY, THESAURUSOBSTRUCTIVE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSASSIDUOUS, THESAURUSFOREIGN, THESAURUSLUNACY, THESAURUSCONVENTIONAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDAWDLE, THESAURUSCONSULTATION, THESAURUSCONSIDERING, PARLIAMENTARISM));
                    draw(SPLANCHNOPLEURE, new GRect(INTERPRETATIONAL, THESAURUSSODDEN, THESAURUSCORPORAL, QUOTATION1BDIMENSIONAL));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSLIVERY, THESAURUSUPROARIOUS, THESAURUSRANDOM, QUOTATIONSUSTAINING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDIALECTIC, THESAURUSINVESTIGATE, IMPLICATIVENESS, THESAURUSMONOPOLIZE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPHLEGMATIC, THESAURUSOBSCURITY, THESAURUSDOLEFUL, THESAURUSINFRINGE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFATEFUL, THESAURUSQUALIFY, THESAURUSDIRECT, THESAURUSDEFRAUD));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSRETAINER, THESAURUSBATTALION, THESAURUSPEDAGOGUE, QUOTATIONZENITHAL));
                    draw(THESAURUSATAVISM, new GRect(CHAMAELEONTIDAE, THESAURUSPICTURE, THESAURUSPENURY, QUOTATIONMELODIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSPEDAGOGUE, THESAURUSCUDDLE, THESAURUSHESITANT, SALICYLALDEHYDE));
                    draw(SPLANCHNOPLEURE, new GRect(MONOCHLORINATED, THESAURUSACCESSION, UNTEMPERATENESS, UNWARRANTABLENESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSLANDEROUS, HYPEROXYGENATED, THESAURUSDISTRESS, THESAURUSARBITRATOR));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEXPUNGE, THESAURUSLITIGIOUS, ETHELOTHRĒSKEIA, DECARTELIZATION));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMOTHERLY, THESAURUSRECOGNIZE, VAINGLORIOUSNESS, THESAURUSMEDIATE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINTERFERE, THESAURUSEMINENT, THESAURUSPERMIT, PHOTORECONNAISSANCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSWAREHOUSE, THESAURUSCONSTRUCTIVE, DEIPNOSOPHISTĒS, HYPERINSULINAEMIA));
                    draw(THESAURUSATAVISM, new GRect(PHOTOLITHOGRAPHER, THESAURUSCONTRIVANCE, COUNTERIRRITANT, THESAURUSPEDAGOGIC));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSENGAGING, ALSOCOENAESTHESIA, THESAURUSDEBUNK, DNIPRODZERZHINSK));
                    draw(THESAURUSATAVISM, new GRect(STEREOMETRICALLY, THESAURUSDETRACT, THESAURUSTRANSACTION, THESAURUSFICTIONAL));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDIFFUSION, THESAURUSGUTTURAL, THESAURUSAGITATE, THESAURUSPOTENT));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSCOMPULSION, THESAURUSABSORB, THESAURUSTRIUMPH, THESAURUSMECHANICAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCOMPULSION, THERMOREGULATION, CHRONOBIOLOGICAL, THESAURUSACCOST));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPERMIT, THESAURUSDISEASED, THESAURUSCORRESPONDENT, UNCONCEPTUALIZED));
                    draw(SPLANCHNOPLEURE, new GRect(KULTURGESCHICHTE, THESAURUSENTANGLEMENT, THESAURUSANTEDILUVIAN, QUOTATIONBEHAVIOURAL));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSDISADVANTAGE, ENTREPRENEURSHIP, THESAURUSMORALITY, CONCEPTUALIZATION));
                    draw(THESAURUSATAVISM, new GRect(STEREOTYPICALLY, THESAURUSSYMMETRY, UNUNDERSTANDABLY, HYDROCARBURETTED));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSDOWNRIGHT, THESAURUSFAWNING, THESAURUSKINGDOM, INDISCRIMINATING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSANTAGONISM, UNDIFFERENTIATED, HYPERCOAGULABILITY, THESAURUSACCORDANCE));
                    draw(THESAURUSATAVISM, new GRect(ALSOMETACHROMASY, THESAURUSHARRIDAN, THESAURUSCOMPOSITION, THESAURUSCOMMENDABLE));
                    draw(THESAURUSATAVISM, new GRect(CONSUBSTANTIATION, THESAURUSFAWNING, THEOPHILANTHROPISTS, THESAURUSUNFOLD));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMATERIALLY, THESAURUSINTREPID, ORTHOPANTOMOGRAPHY, THESAURUSTREATMENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSEXTEMPORIZE, THESAURUSSYMMETRY, ELECTROPHOTOGRAPHY, CONSUBSTANTIATUS));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONDURHAM, UNSTRAIGHTFORWARD, REPROACHFULNESS, THESAURUSUNDERRATE));
                    draw(SPLANCHNOPLEURE, new GRect(HAEMATOGLOBULIN, THESAURUSREVIEW, THESAURUSEMPLOYEE, THESAURUSSCIENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSOFFEND, THESAURUSEXPENDITURE, PARAPSYCHOLOGICAL, PARLIAMENTARISM));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSTEDIOUS, THESAURUSINGLORIOUS, THESAURUSPARALYTIC, UNCOMPROMISINGNESS));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONMOSAIC, PROGRESSIONISTS, RHYNCHOCEPHALIA, THESAURUSOBEDIENCE));
                    draw(SPLANCHNOPLEURE, new GRect(PERFUNCTORINESS, HYPERCATALECTIC, QUOTATION3BCOUPÉ, ALSOCANCELLATED));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCONCOMITANT, THESAURUSHOTBED, MACROPHOTOGRAPHY, THESAURUSCONTEMPTIBLE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSGOVERN, THESAURUSINDEFINITE, THESAURUSHELLISH, THESAURUSEVIDENCE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSUNBECOMING, INCONVERTIBLENESS, HUPOKHONDRIAKOS, SIMULTANEOUSNESS));
                    draw(THESAURUSATAVISM, new GRect(DIPHENHYDRAMINE, THESAURUSMALTREATMENT, THESAURUSINTUITIVE, THESAURUSDEXTEROUS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSINVADE, THESAURUSHOTBED, THESAURUSEXAMPLE, PLANIMETRICALLY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSEXCAVATE, THESAURUSIMMEDIATELY, CONTEMPORANEITY, HYPOCRATERIFORMIS));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFOREWORD, INTERDEPENDENTLY, PARTICULARIZING, THESAURUSUNDESIRABLE));
                    draw(THESAURUSATAVISM, new GRect(QUOTATIONBACTERIOLOGICAL, THESAURUSINCIVILITY, THESAURUSHIDDEN, CONTRADISTINCTION));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPERVERT, THESAURUSREPETITION, THESAURUSHIGHLY, THESAURUSBRASSY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSPREVENT, THESAURUSUNDERWATER, THESAURUSLEGISLATE, THESAURUSSCARCELY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSRUNAWAY, RHINOSPORIDIOSIS, KINDHEARTEDNESS, THESAURUSBLOODLESS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINDULGENCE, INAPPRECIATIVELY, THESAURUSSOCIABLE, THESAURUSPANTRY));
                    draw(SPLANCHNOPLEURE, new GRect(AUTOLITHOGRAPHY, IMPATRONIZATION, SPECTROHELIOGRAPH, THESAURUSINSUFFICIENT));
                    draw(THESAURUSATAVISM, new GRect(NEUROEMBRYOLOGY, THESAURUSCREDULOUS, MANIFESTATIVELY, QUOTATIONCOEQUATE));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSOVERWEIGHT, THESAURUSCLEARLY, THESAURUSSCHOOLING, THESAURUSPERPLEXITY));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSIRRETRIEVABLE, THESAURUSCOGNATE, DISTINGUISHABLY, QUOTATIONLIFTING));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSRECEIPT, THESAURUSDOWNGRADE, THESAURUSIMPURE, THESAURUSFRESHEN));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSASPERITY, QUOTATIONLIFTING, THESAURUSALIMONY, THESAURUSINCEPTION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSLETHARGY, UNRECOMMENDABLE, ELECTROPHORESIS, THESAURUSCONTRADICTION));
                    draw(THESAURUSATAVISM, new GRect(THESAURUSFRAILTY, THESAURUSMATCHMAKER, THESAURUSPENETRATION, THESAURUSCRISIS));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMISTAKEN, THESAURUSINFERIOR, THESAURUSTELESCOPE, THESAURUSTIMELY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSIRRELIGIOUS, THESAURUSDISMANTLE, THESAURUSCRUCIFY, THESAURUSSLEEPY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINTERPOSE, PROHIBITIVENESS, QUOTATIONCRYSTALLIZED, THESAURUSINFRINGE));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSCRUCIFY, THESAURUSEVENING, THESAURUSDEBATE, THESAURUSEXEMPLAR));
                    draw(THESAURUSATAVISM, new GRect(DISREGARDFULNESS, ORGANOGENICALLY, THESAURUSEXTRAORDINARY, CONTRACONSCIENTIOUSLY));
                    draw(THESAURUSATAVISM, new GRect(INDEFATIGABILIS, DEPALATALIZATION, THESAURUSCOMPARATIVE, THESAURUSDOCUMENT));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINSUFFERABLE, THESAURUSENVELOPE, THESAURUSIMMINENT, THESAURUSSOLICITUDE));
                    draw(SPLANCHNOPLEURE, new GRect(QUOTATIONSPENSERIAN, THESAURUSCORRODE, THESAURUSEXORCISE, THESAURUSFOREIGN));
                    draw(SPLANCHNOPLEURE, new GRect(EPIGRAMMATISTĒS, PREDETERMINATIVE, THESAURUSLIMITATION, INTERPENETRATION));
                }
                else if (name.Contains(THESAURUSEPISODIC))
                {
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSSURPASS, THESAURUSPLEASURE, THESAURUSCLIQUE, THESAURUSMISUSE));
                    draw(SPLANCHNOPLEURE, new GRect(DIACETYLMORPHINE, THESAURUSCOMPRISE, THESAURUSDELINQUENT, QUOTATIONMEIBOMIAN));
                    draw(SPLANCHNOPLEURE, new GRect(ECONOMETRICALLY, THESAURUSIMPRISONMENT, THESAURUSPORTABLE, THESAURUSGUARDIAN));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSINACTIVE, QUOTATIONISOIONIC, THESAURUSNOTWITHSTANDING, UNREMUNERATIVENESS));
                    draw(SPLANCHNOPLEURE, new GRect(MONTMORILLONOID, THESAURUSSHUTTER, THESAURUSINAPPROPRIATE, QUOTATIONBANDED));
                    draw(SPLANCHNOPLEURE, new GRect(APOLLINARIANISM, BALANOPHORACEAE, THESAURUSREMOVAL, QUOTATION1ALENGTHEN));
                    draw(SPLANCHNOPLEURE, new GRect(GASTEROMYCETOUS, THESAURUSPERFORMER, ALSOOPISTHOCOELIAN, THESAURUSINDIRECTLY));
                    draw(SPLANCHNOPLEURE, new GRect(THESAURUSMASSAGE, THESAURUSCONVOCATION, DISCOUNTENANCING, THESAURUSINDICATION));
                }
                else if (name.Contains(ALSOBROBDINGNAG))
                {
                }
            }
            foreach (var info in mlInfos)
            {
                if (!string.IsNullOrWhiteSpace(info.Text)) DrawMLeader(info.Text, info.BasePoint, info.BasePoint.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
            }
        }
        private static void NewMethod2(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
        {
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = NARCOTRAFICANTE; si < cadDatas.Count; si++)
            {
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                foreach (var kv in lbdict)
                {
                }
                var item = cadDatas[si];
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSNETHER).ToList();
                var wlinesGeosf = F(wlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var wlines = item.WLines.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).ToList();
                    var wlf = GeoFac.CreateIntersectsSelector(wlines);
                    foreach (var fd in item.FloorDrains)
                    {
                        foreach (var wl in wlf(fd))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var kv in lbdict)
                    {
                        var pp = kv.Key;
                        foreach (var wl in wlf(pp))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.WaterWellFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var cp in item.CondensePipes)
                    {
                        foreach (var wl in wlf(cp))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(vm?.Params?.CondensePipeHorizontalDN ?? THESAURUSATAVISM, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(vm?.Params?.CondensePipeHorizontalDN ?? THESAURUSATAVISM, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var ws in item.WaterSealingWells)
                    {
                        foreach (var wl in wlf(ws.Buffer(HYDROSTATICALLY)))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var well in item.WaterWells)
                    {
                        foreach (var wl in wlf(well.Buffer(HYDROSTATICALLY)))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var ws in item.RainPortSymbols)
                    {
                        foreach (var wl in wlf(ws))
                        {
                            var p = wl.ToGRect().Center;
                            var seg = wl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                drawMLeader(THESAURUSFRINGE, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                }
            }
            void drawMLeader(string content, Point2d p1, Point2d p2)
            {
                var e = new MLeader();
                e.ColorIndex = THESAURUSSENILE;
                e.MText = new MText() { Contents = content, TextHeight = THESAURUSENTREAT, ColorIndex = THESAURUSSENILE, };
                e.TextStyleId = GetTextStyleId(THESAURUSTRAFFIC);
                e.ArrowSize = UNDERACHIEVEMENT;
                e.DoglegLength = NARCOTRAFICANTE;
                e.LandingGap = NARCOTRAFICANTE;
                e.ExtendLeaderToText = UNTRACEABLENESS;
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
                e.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
                e.AddLeaderLine(p1.ToPoint3d());
                var bd = e.MText.Bounds.ToGRect();
                var p3 = p2.OffsetY(bd.Height + THESAURUSINDUSTRY).ToPoint3d();
                if (p2.X < p1.X)
                {
                    p3 = p3.OffsetX(-bd.Width);
                }
                e.TextLocation = p3;
                DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(e); });
            }
        }
        public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo)
        {
            if (exInfo is null) return;
            DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
        }
        private static void NewMethod1(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
        {
            var cadDatas = exInfo.CadDatas;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            for (int si = NARCOTRAFICANTE; si < cadDatas.Count; si++)
            {
                var lbdict = exInfo.Items[si].LabelDict.ToDictionary(x => x.Item1, x => x.Item2);
                foreach (var kv in lbdict)
                {
                }
                var item = cadDatas[si];
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, THESAURUSNETHER).ToList();
                var dlinesGeosf = F(dlinesGeos);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var dlines = item.DLines.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).ToList();
                    var dlf = GeoFac.CreateIntersectsSelector(dlines);
                    foreach (var fd in item.FloorDrains)
                    {
                        foreach (var dl in dlf(fd))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var fd in item.DownWaterPorts)
                    {
                        foreach (var dl in dlf(fd))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                DrawMLeader(THESAURUSOVERCHARGE, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                DrawMLeader(THESAURUSOVERCHARGE, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var kv in lbdict)
                    {
                        var pp = kv.Key;
                        foreach (var dl in dlf(pp))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                DrawMLeader(vm?.Params?.OtherFloorDrainDN ?? THESAURUSATAVISM, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                    foreach (var port in item.WaterPorts)
                    {
                        foreach (var dl in dlf(port.Buffer(HYDROSTATICALLY)))
                        {
                            var p = dl.ToGRect().Center;
                            var seg = dl.ToGLineSegments().First();
                            var dg = (seg.StartPoint - seg.EndPoint).Angle.AngleToDegree();
                            if (NARCOTRAFICANTE <= dg && dg <= THESAURUSITINERANT || AUTHORITARIANISM <= dg && dg <= THESAURUSEVENTUALLY)
                            {
                                DrawMLeader(THESAURUSFRINGE, p, p.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                            else
                            {
                                DrawMLeader(THESAURUSFRINGE, p, p.OffsetXY(THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                            }
                        }
                    }
                }
            }
        }
        public class Ref<T>
        {
            public T Value;
            public Ref() { }
            public Ref(T v) { Value = v; }
        }
        public class BlockInfo
        {
            public string LayerName;
            public string BlockName;
            public Point3d BasePoint;
            public double Rotate;
            public double Scale;
            public Dictionary<string, string> PropDict;
            public Dictionary<string, object> DynaDict;
            public BlockInfo(string blockName, string layerName, Point3d basePoint)
            {
                this.LayerName = layerName;
                this.BlockName = blockName;
                this.BasePoint = basePoint;
                this.PropDict = new Dictionary<string, string>();
                this.DynaDict = new Dictionary<string, object>();
                this.Rotate = NARCOTRAFICANTE;
                this.Scale = ADRENOCORTICOTROPHIC;
            }
        }
        public class LineInfo
        {
            public GLineSegment Line;
            public string LayerName;
            public LineInfo(GLineSegment line, string layerName)
            {
                this.Line = line;
                this.LayerName = layerName;
            }
        }
        public class DBTextInfo
        {
            public string LayerName;
            public string TextStyle;
            public Point3d BasePoint;
            public string Text;
            public double Rotation;
            public DBTextInfo(Point3d point, string text, string layerName, string textStyle)
            {
                text ??= THESAURUSREDOUND;
                this.LayerName = layerName;
                this.TextStyle = textStyle;
                this.BasePoint = point;
                this.Text = text;
            }
        }
    }
    public class Dijkstra<T>
    {
        private readonly List<GraphNode<T>> _graph;
        private IPriorityQueue<GraphNode<T>> _unvistedNodes;
        public Dijkstra(IEnumerable<GraphNode<T>> graph)
        {
            _graph = graph.ToList();
        }
        public IList<GraphNode<T>> FindShortestPathBetween(GraphNode<T> start, GraphNode<T> finish)
        {
            PrepareGraphForDijkstra();
            start.TentativeDistance = NARCOTRAFICANTE;
            var current = start;
            while (THESAURUSSEMBLANCE)
            {
                foreach (var neighbour in current.Neighbours.Where(x => !x.GraphNode.Visited))
                {
                    var newTentativeDistance = current.TentativeDistance + neighbour.Distance;
                    if (newTentativeDistance < neighbour.GraphNode.TentativeDistance)
                    {
                        neighbour.GraphNode.TentativeDistance = newTentativeDistance;
                    }
                }
                current.Visited = THESAURUSSEMBLANCE;
                var next = _unvistedNodes.Pop();
                if (next == null || next.TentativeDistance == int.MaxValue)
                {
                    if (finish.TentativeDistance == int.MaxValue)
                    {
                        return new List<GraphNode<T>>();
                    }
                    finish.Visited = THESAURUSSEMBLANCE;
                    break;
                }
                var smallest = next;
                current = smallest;
            }
            return DeterminePathFromWeightedGraph(start, finish);
        }
        private static List<GraphNode<T>> DeterminePathFromWeightedGraph(GraphNode<T> start, GraphNode<T> finish)
        {
            var current = finish;
            var path = new List<GraphNode<T>> { current };
            var currentTentativeDistance = finish.TentativeDistance;
            while (THESAURUSSEMBLANCE)
            {
                if (current == start)
                {
                    break;
                }
                foreach (var neighbour in current.Neighbours.Where(x => x.GraphNode.Visited))
                {
                    if (currentTentativeDistance - neighbour.Distance == neighbour.GraphNode.TentativeDistance)
                    {
                        current = neighbour.GraphNode;
                        path.Add(current);
                        currentTentativeDistance -= neighbour.Distance;
                        break;
                    }
                }
            }
            path.Reverse();
            return path;
        }
        private void PrepareGraphForDijkstra()
        {
            _unvistedNodes = new PriorityQueue<GraphNode<T>>(new CompareNeighbour<T>());
            _graph.ForEach(x =>
            {
                x.Visited = UNTRACEABLENESS;
                x.TentativeDistance = int.MaxValue;
                _unvistedNodes.Push(x);
            });
        }
    }
    internal class CompareNeighbour<T> : IComparer<GraphNode<T>>
    {
        public int Compare(GraphNode<T> x, GraphNode<T> y)
        {
            if (x.TentativeDistance > y.TentativeDistance)
            {
                return ADRENOCORTICOTROPHIC;
            }
            if (x.TentativeDistance < y.TentativeDistance)
            {
                return -ADRENOCORTICOTROPHIC;
            }
            return NARCOTRAFICANTE;
        }
    }
    public class GraphNode<T>
    {
        public readonly List<Neighbour> Neighbours;
        public bool Visited = UNTRACEABLENESS;
        public T Value;
        public int TentativeDistance;
        public GraphNode(T value)
        {
            Value = value;
            Neighbours = new List<Neighbour>();
        }
        public void AddNeighbour(GraphNode<T> graphNode, int distance)
        {
            Neighbours.Add(new Neighbour(graphNode, distance));
            graphNode.Neighbours.Add(new Neighbour(this, distance));
        }
        public struct Neighbour
        {
            public int Distance;
            public GraphNode<T> GraphNode;
            public Neighbour(GraphNode<T> graphNode, int distance)
            {
                GraphNode = graphNode;
                Distance = distance;
            }
        }
    }
    public interface IPriorityQueue<T>
    {
        void Push(T item);
        T Pop();
        bool Contains(T item);
    }
    public class PriorityQueue<T> : IPriorityQueue<T>
    {
        private readonly List<T> _innerList = new List<T>();
        private readonly IComparer<T> _comparer;
        public int Count
        {
            get { return _innerList.Count; }
        }
        public PriorityQueue(IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }
        public void Push(T item)
        {
            _innerList.Add(item);
        }
        public T Pop()
        {
            if (_innerList.Count <= NARCOTRAFICANTE)
            {
                return default(T);
            }
            Sort();
            var item = _innerList[NARCOTRAFICANTE];
            _innerList.RemoveAt(NARCOTRAFICANTE);
            return item;
        }
        public bool Contains(T item)
        {
            return _innerList.Contains(item);
        }
        private void Sort()
        {
            _innerList.Sort(_comparer);
        }
    }
}