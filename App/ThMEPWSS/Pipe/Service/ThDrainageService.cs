namespace ThMEPWSS.ReleaseNs.DrainageSystemNs
{
  using AcHelper;
  using Autodesk.AutoCAD.ApplicationServices;
  using Autodesk.AutoCAD.Colors;
  using Autodesk.AutoCAD.DatabaseServices;
  using Autodesk.AutoCAD.EditorInput;
  using Autodesk.AutoCAD.Geometry;
  using Autodesk.AutoCAD.Runtime;
  using DotNetARX;
  using Dreambuild.AutoCAD;
  using Linq2Acad;
  using NetTopologySuite.Geometries;
  using NFox.Cad;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Windows.Forms;
  using ThCADExtension;
  using ThMEPEngineCore.Algorithm;
  using ThMEPEngineCore.Engine;
  using ThMEPWSS.Assistant;
  using ThMEPWSS.CADExtensionsNs;
  using ThMEPWSS.Diagram.ViewModel;
  using ThMEPWSS.JsonExtensionsNs;
  using ThMEPWSS.Pipe;
  using ThMEPWSS.Pipe.Model;
  using ThMEPWSS.Pipe.Service;
  using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
  using ThMEPWSS.Uitl;
  using ThMEPWSS.Uitl.ExtensionsNs;
  using static THDrainageService;
  using static ThMEPWSS.Assistant.DrawUtils;
  using ThMEPEngineCore.Model.Common;
  using NetTopologySuite.Operation.Buffer;
  using Newtonsoft.Json;
  using System.Diagnostics;
  using Newtonsoft.Json.Linq;
  using StoreyContext = Pipe.Model.StoreyContext;
  public static class THDrainageService
  {
    public static bool DrawWLPipeSystem()
    {
      TOILET_WELLS_INTERVAL = null;
      if (((DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos?.Count) ?? THESAURUSSTAMPEDE) is THESAURUSSTAMPEDE)
      {
        return INTRAVASCULARLY;
      }
      TOILET_WELLS_INTERVAL = GeoFac.CreateGeometry(DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary.ToPolygon())).Envelope.ToGRect().Expand(POLYOXYMETHYLENE).ToPt3dCollection();
      {
        var vm = DrainageSystemDiagramViewModel.Singleton;
        static bool isRainLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSABJURE);
        static bool isDraiLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSREMNANT);
        static bool isDrainageLayer(string layer) => isRainLayer(layer) || isDraiLayer(layer);
        static string GetEffectiveLayer(string TOILET_BUFFER_DISTANCE)
        {
          return GetEffectiveName(TOILET_BUFFER_DISTANCE);
        }
        static string GetEffectiveName(string WELLS_MAX_AREA)
        {
          WELLS_MAX_AREA ??= THESAURUSDEPLORE;
          var MAX_ANGEL_TOLLERANCE = WELLS_MAX_AREA.LastIndexOf(THESAURUSCONTEND);
          if (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE && !WELLS_MAX_AREA.EndsWith(MULTIPROCESSING))
          {
            WELLS_MAX_AREA = WELLS_MAX_AREA.Substring(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
          }
          MAX_ANGEL_TOLLERANCE = WELLS_MAX_AREA.LastIndexOf(SUPERREGENERATIVE);
          if (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE && !WELLS_MAX_AREA.EndsWith(THESAURUSCOURIER))
          {
            WELLS_MAX_AREA = WELLS_MAX_AREA.Substring(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
          }
          return WELLS_MAX_AREA;
        }
        static string GetEffectiveBRName(string MIN_WELL_TO_URINAL_DISTANCE)
        {
          return GetEffectiveName(MIN_WELL_TO_URINAL_DISTANCE);
        }
        static bool IsWantedBlock(BlockTableRecord blockTableRecord)
        {
          if (blockTableRecord.IsDynamicBlock)
          {
            return INTRAVASCULARLY;
          }
          if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
          {
            return INTRAVASCULARLY;
          }
          if (!blockTableRecord.Explodable)
          {
            return INTRAVASCULARLY;
          }
          return THESAURUSOBSTINACY;
        }
        bool MAX_ROOM_INTERVAL = INTRAVASCULARLY;
        var KITCHEN_BUFFER_DISTANCE = new List<StoreyInfo>();
        var MAX_TOILET_TO_KITCHEN_DISTANCE = INTRAVASCULARLY;
        var MAX_TOILET_TO_KITCHEN_DISTANCE1 = new List<Geometry>();
        var MAX_KITCHEN_TO_RAINPIPE_DISTANCE = new List<Geometry>();
        var MAX_BALCONY_TO_RAINPIPE_DISTANCE = new List<Geometry>();
        var MAX_TOILET_TO_CONDENSEPIPE_DISTANCE = new List<Geometry>();
        var MAX_TOILET_TO_FLOORDRAIN_DISTANCE = new List<Geometry>();
        var MAX_TOILET_TO_FLOORDRAIN_DISTANCE2 = new List<Geometry>();
        var MAX_TOILET_TO_FLOORDRAIN_DISTANCE1 = new List<Geometry>();
        var MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE = new List<Geometry>();
        var MAX_BALCONYRAINPIPE_TO_FLOORDRAIN_DISTANCE = new List<Geometry>();
        var MAX_BALCONY_TO_BALCONY_DISTANCE = new List<CText>();
        var MAX_KITCHEN_TO_BALCONY_DISTANCE = new List<Geometry>();
        var MIN_DEVICEPLATFORM_AREA = new List<Geometry>();
        var MAX_DEVICEPLATFORM_AREA = new List<Geometry>();
        var MAX_BASECIRCLE_AREA = new List<Geometry>();
        var max_balconywashingfloordrain_to_balconyfloordrain = new List<Geometry>();
        FocusMainWindow();
        if (!TrySelectPoint(out Point3d SIDEWATERBUCKET_X_INDENT)) return INTRAVASCULARLY;
if (!ThRainSystemService.ImportElementsFromStdDwg()) return INTRAVASCULARLY;
        using var lck = DocLock;
        using var adb = AcadDatabase.Active();
        using var tr = new _DrawingTransaction(adb);
        static string TryParseWrappingPipeDNText(string repeated_point_distance)
        {
          if (repeated_point_distance is null) return null;
          var TolLightRangeMin = Regex.Replace(repeated_point_distance, THESAURUSRESUSCITATE, THESAURUSDEPLORE, RegexOptions.IgnoreCase);
          TolLightRangeMin = Regex.Replace(TolLightRangeMin, QUOTATION3BABOVE, THESAURUSDEPLORE);
          TolLightRangeMin = Regex.Replace(TolLightRangeMin, THESAURUSMISTRUST, THESAURUSSPECIFICATION);
          TolLightRangeMin = TolLightRangeMin.Replace(INTELLECTUALNESS, THESAURUSDEPLORE);
          return TolLightRangeMin;
        }
        Point3dCollection range = TOILET_WELLS_INTERVAL;
        {
          var max_device_to_device = GetStoreyBlockReferences(adb);
          var max_device_to_balcony = new List<BlockReference>();
          foreach (var tolReturnValueRangeTo in max_device_to_device)
          {
            var TolLightRangeSingleSideMin = GetStoreyInfo(tolReturnValueRangeTo);
            if (range?.ToGRect().ToPolygon().Contains(TolLightRangeSingleSideMin.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY)
            {
              max_device_to_balcony.Add(tolReturnValueRangeTo);
              KITCHEN_BUFFER_DISTANCE.Add(TolLightRangeSingleSideMin);
            }
          }
          FixStoreys(KITCHEN_BUFFER_DISTANCE);
        }
        var max_balconywashingfloordrain_to_rainpipe = CreateEnvelopeTester(KITCHEN_BUFFER_DISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary).ToList());
        foreach (var maxToiletToCondensepipeDistance in adb.ModelSpace.OfType<Entity>())
          {
            if (maxToiletToCondensepipeDistance is BlockReference tolReturnValueRangeTo)
            {
              if (!tolReturnValueRangeTo.BlockTableRecord.IsValid) continue;
              var minDeviceplatformArea = adb.Blocks.Element(tolReturnValueRangeTo.BlockTableRecord);
              var _fs = new List<KeyValuePair<Geometry, Action>>();
              Action maxDeviceplatformArea = null;
              try
              {
                MAX_ROOM_INTERVAL = minDeviceplatformArea.XrefStatus != XrefStatus.NotAnXref;
                handleBlockReference(tolReturnValueRangeTo, Matrix3d.Identity, _fs);
              }
              finally
              {
                MAX_ROOM_INTERVAL = INTRAVASCULARLY;
              }
              {
                var TolLightRangeSingleSideMin = tolReturnValueRangeTo.XClipInfo();
                if (TolLightRangeSingleSideMin.IsValid)
                {
                  TolLightRangeSingleSideMin.TransformBy(tolReturnValueRangeTo.BlockTransform);
                  var default_voltage = TolLightRangeSingleSideMin.PreparedPolygon;
                  foreach (var max_rainpipe_to_balconyfloordrain in _fs)
                  {
                    if (default_voltage.Intersects(max_rainpipe_to_balconyfloordrain.Key))
                    {
                      maxDeviceplatformArea += max_rainpipe_to_balconyfloordrain.Value;
                    }
                  }
                }
                else
                {
                  foreach (var max_rainpipe_to_balconyfloordrain in _fs)
                  {
                    maxDeviceplatformArea += max_rainpipe_to_balconyfloordrain.Value;
                  }
                }
                maxDeviceplatformArea?.Invoke();
              }
            }
            else
            {
              var _fs = new List<KeyValuePair<Geometry, Action>>();
              handleEntity(maxToiletToCondensepipeDistance, Matrix3d.Identity, _fs);
              foreach (var max_rainpipe_to_balconyfloordrain in _fs)
              {
                max_rainpipe_to_balconyfloordrain.Value();
              }
            }
          }
        StoreyInfo max_balconywashingmachine_to_balconybasinline(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
        {
          if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE is null) return null;
          foreach (var TolLightRangeSingleSideMin in KITCHEN_BUFFER_DISTANCE)
          {
            if (IsNumStorey(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
            {
              if (TolLightRangeSingleSideMin.Numbers.Contains(GetStoreyScore(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))) return TolLightRangeSingleSideMin;
            }
            else if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == THESAURUSARGUMENTATIVE)
            {
              if (TolLightRangeSingleSideMin.StoreyType == StoreyType.LargeRoof) return TolLightRangeSingleSideMin;
            }
          }
          var min_downspout_to_balconyfloordrain = KITCHEN_BUFFER_DISTANCE.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.StoreyType == StoreyType.SmallRoof).OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary.CenterY).ToList();
          if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == ANTHROPOMORPHICALLY)
          {
            return min_downspout_to_balconyfloordrain.FirstOrDefault();
          }
          if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == THESAURUSSCUFFLE)
          {
            if (min_downspout_to_balconyfloordrain.Count >= THESAURUSPERMUTATION) return min_downspout_to_balconyfloordrain[THESAURUSHOUSING];
            return null;
          }
          return null;
        }
        var SIDEWATERBUCKET_Y_INDENT = new List<string>();
        var max_condensepipe_to_washmachine = KITCHEN_BUFFER_DISTANCE.SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers).Max();
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < max_condensepipe_to_washmachine; MAX_ANGEL_TOLLERANCE++)
        {
          SIDEWATERBUCKET_Y_INDENT.Add((MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING) + THESAURUSASPIRATION);
        }
        SIDEWATERBUCKET_Y_INDENT.Add(THESAURUSARGUMENTATIVE);
        if (max_balconywashingmachine_to_balconybasinline(ANTHROPOMORPHICALLY) is not null)
        {
          SIDEWATERBUCKET_Y_INDENT.Add(ANTHROPOMORPHICALLY);
          if (max_balconywashingmachine_to_balconybasinline(THESAURUSSCUFFLE) is not null)
          {
            SIDEWATERBUCKET_Y_INDENT.Add(THESAURUSSCUFFLE);
          }
        }
        IEnumerable<string> max_downspout_to_balconywashingfloordrain(StoreyInfo TolLightRangeSingleSideMin) => SIDEWATERBUCKET_Y_INDENT.Where(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => max_balconywashingmachine_to_balconybasinline(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN) == TolLightRangeSingleSideMin);
        string max_rainpipe_to_washmachine(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
        {
          return SIDEWATERBUCKET_Y_INDENT.TryGet(SIDEWATERBUCKET_Y_INDENT.IndexOf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) + THESAURUSHOUSING);
        }
        string commonradius(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
        {
          return SIDEWATERBUCKET_Y_INDENT.TryGet(SIDEWATERBUCKET_Y_INDENT.IndexOf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) - THESAURUSHOUSING);
        }
        static Func<Envelope, bool> CreateEnvelopeTester(ICollection<GRect> rects)
        {
          if (rects.Count == THESAURUSSTAMPEDE) return DEFAULT_FIRE_VALVE_WIDTH => INTRAVASCULARLY;
          var engine = new NetTopologySuite.Index.Strtree.STRtree<object>(rects.Count > THESAURUSACRIMONIOUS ? rects.Count : THESAURUSACRIMONIOUS);
          foreach (var DEFAULT_FIRE_VALVE_WIDTH in rects) engine.Insert(DEFAULT_FIRE_VALVE_WIDTH.ToEnvolope(), null);
          return envo =>
          {
            if (envo is null) throw new ArgumentNullException();
            return engine.Query(envo).Any();
          };
        }
        var max_balconybasin_to_balcony = MAX_TOILET_TO_FLOORDRAIN_DISTANCE.Select(TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax.GetCenter(), TolLightRangeSingleSideMax.ToGRect().InnerRadius)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Radius > THESAURUSDRAGOON).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(SUPERLATIVENESS)).Where(TolLightRangeSingleSideMax => max_balconywashingfloordrain_to_rainpipe(TolLightRangeSingleSideMax.EnvelopeInternal)).ToList();
        var balcony_buffer_distance = MAX_TOILET_TO_FLOORDRAIN_DISTANCE.Select(TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax.GetCenter(), TolLightRangeSingleSideMax.ToGRect().InnerRadius)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Radius <= THESAURUSDRAGOON).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(SUPERLATIVENESS)).Where(TolLightRangeSingleSideMax => max_balconywashingfloordrain_to_rainpipe(TolLightRangeSingleSideMax.EnvelopeInternal)).ToList();
          {
            var min_balconybasin_to_balcony = GeoFac.CreateIntersectsSelector(MAX_BALCONY_TO_BALCONY_DISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList());
            foreach (var max_tag_yposition in GeoFac.GroupGeometries(MAX_TOILET_TO_KITCHEN_DISTANCE1.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSCOMMUNICATION, EndCapStyle.Square).Tag(TolLightRangeSingleSideMax)).ToList()))
            {
              var tolReturnValueRange = max_tag_yposition.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData).OfType<Geometry>().ToList();
              var TolLane = GeoFac.GetManyLines(tolReturnValueRange).ToList();
              var hsegs = TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSCOMMUNICATION)).ToList();
              if (hsegs.Count is THESAURUSPERMUTATION)
              {
                var _tol_avg_column_dist = TolLane.YieldPoints().Distinct().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
                var ptsdf = GeoFac.CreateDisjointSelector(_tol_avg_column_dist);
                _tol_avg_column_dist = ptsdf(GeoFac.CreateGeometryEx(hsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSCOMMUNICATION, EndCapStyle.Flat)).ToList()));
                var max_kitchen_to_balcony_distance = hsegs.SelectMany(default_fire_valve_length =>
                {
                  static bool IsWantedText(string repeated_point_distance)
                  {
                    if (repeated_point_distance is null) return INTRAVASCULARLY;
                    return IsDrainageLabel(repeated_point_distance) || repeated_point_distance.Contains(THESAURUSLECHER);
                  }
                  var _raiseDistanceToStartDefault = default_fire_valve_length.Center;
                  var MAX_DEVICE_TO_BALCONY = THESAURUSARRIVE;
                  var max_tag_xposition = min_balconybasin_to_balcony(new GLineSegment(_raiseDistanceToStartDefault, _raiseDistanceToStartDefault.OffsetY(MAX_DEVICE_TO_BALCONY)).ToLineString()).Where(TolLightRangeSingleSideMax => IsWantedText(TolLightRangeSingleSideMax.UserData as string)).ToList();
                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE <= DISPENSABLENESS; MAX_ANGEL_TOLLERANCE++)
                  {
                    if (max_tag_xposition.Count > THESAURUSSTAMPEDE) break;
                    MAX_DEVICE_TO_BALCONY = HYPERDISYLLABLE + MAX_ANGEL_TOLLERANCE * THESAURUSACRIMONIOUS;
                    max_tag_xposition = min_balconybasin_to_balcony(new GLineSegment(_raiseDistanceToStartDefault, _raiseDistanceToStartDefault.OffsetY(MAX_DEVICE_TO_BALCONY)).ToLineString()).Where(TolLightRangeSingleSideMax => IsDrainageLabel(TolLightRangeSingleSideMax.UserData as string)).ToList();
                  }
                  return max_tag_xposition.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData).OfType<string>();
                }).Distinct().ToList();
                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint3d()).Distinct(new Point3dComparer(THESAURUSACRIMONIOUS)))
                {
                  foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in max_kitchen_to_balcony_distance)
                  {
                    MAX_DEVICEPLATFORM_AREA.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint().Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                  }
                }
              }
            }
          }
        {
          var sidewaterbucket_x_indent = GeoFac.CreateIntersectsSelector(max_balconybasin_to_balcony);
          foreach (var sidewaterbucket_y_indent in MAX_DEVICEPLATFORM_AREA)
          {
            if (sidewaterbucket_y_indent.UserData is not string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) continue;
            if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(INTELLECTUALNESS))
            {
              var well_to_wall_offset = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Split(THESAURUSHABITAT);
              if (well_to_wall_offset.Length == THESAURUSPERMUTATION)
              {
                if (Regex.IsMatch(well_to_wall_offset[THESAURUSHOUSING], UREDINIOMYCETES))
                {
                  if (IsDrainageLabel(well_to_wall_offset[THESAURUSSTAMPEDE]))
                  {
                    MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = well_to_wall_offset[THESAURUSSTAMPEDE];
                  }
                }
              }
            }
            if (IsDrainageLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
            {
              foreach (var toilet_wells_interval in sidewaterbucket_x_indent(sidewaterbucket_y_indent))
              {
                toilet_wells_interval.UserData = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
              }
            }
          }
          foreach (var sidewaterbucket_y_indent in MAX_DEVICEPLATFORM_AREA)
          {
            if (sidewaterbucket_y_indent.UserData is not string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) continue;
            if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSELIGIBLE))
            {
              if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSADVENT))
              {
                MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE.Add(GeoFac.CreateGeometryEx(GeoFac.GetPoints(sidewaterbucket_y_indent).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
              }
              else if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(QUOTATIONMALTESE))
              {
                MAX_BALCONYRAINPIPE_TO_FLOORDRAIN_DISTANCE.Add(GeoFac.CreateGeometryEx(GeoFac.GetPoints(sidewaterbucket_y_indent).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
              }
            }
            if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSLECHER))
            {
              string toilet_buffer_distance = null, MAX_CONDENSEPIPE_TO_WASHMACHINE = null;
              if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSPLUMMET) || MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(ALSOHEAVENWARDS))
              {
                toilet_buffer_distance = THESAURUSTOPICAL;
              }
              else if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSPROLONG))
              {
                toilet_buffer_distance = THESAURUSBANDAGE;
              }
              else if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(QUOTATIONSPENSERIAN))
              {
                toilet_buffer_distance = THESAURUSCONSERVATION;
              }
              var tolReturnValueMinRange = Regex.Match(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, UREDINIOMYCETES);
              if (tolReturnValueMinRange.Success) MAX_CONDENSEPIPE_TO_WASHMACHINE = tolReturnValueMinRange.Groups[THESAURUSSTAMPEDE].Value;
              if (toilet_buffer_distance is not null)
              {
                MAX_CONDENSEPIPE_TO_WASHMACHINE ??= THESAURUSIMPETUOUS;
                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in GeoFac.GetPoints(sidewaterbucket_y_indent))
                {
                  MAX_TOILET_TO_FLOORDRAIN_DISTANCE2.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.Tag(toilet_buffer_distance + INTELLECTUALNESS + MAX_CONDENSEPIPE_TO_WASHMACHINE));
                }
              }
            }
          }
          var min_balconybasin_to_balcony = GeoFac.CreateIntersectsSelector(MAX_BALCONY_TO_BALCONY_DISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList());
          foreach (var DEFAULT_FIRE_VALVE_LENGTH in GeoFac.GroupGeometries(GeoFac.GetManyLines(MAX_TOILET_TO_KITCHEN_DISTANCE1).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList()).Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax)))
          {
            var wells_max_area = GeoFac.GetLines(DEFAULT_FIRE_VALVE_LENGTH).ToList();
            var min_well_to_urinal_distance = wells_max_area.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSCOMMUNICATION)).ToList();
            var _tol_avg_column_dist = GeoFac.GetAlivePoints(wells_max_area, THESAURUSCOMMUNICATION).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).Where(TolLightRangeSingleSideMax => !min_well_to_urinal_distance.Any(default_fire_valve_length => default_fire_valve_length.Buffer(THESAURUSCOMMUNICATION, EndCapStyle.Square).Intersects(TolLightRangeSingleSideMax))).ToList();
            if (min_well_to_urinal_distance.Count == THESAURUSHOUSING && _tol_avg_column_dist.Count > THESAURUSSTAMPEDE)
            {
              foreach (var default_fire_valve_length in min_well_to_urinal_distance)
              {
                var _raiseDistanceToStartDefault = default_fire_valve_length.Center;
                {
                  var MAX_DEVICE_TO_BALCONY = THESAURUSARRIVE;
                  var max_tag_xposition = min_balconybasin_to_balcony(new GLineSegment(_raiseDistanceToStartDefault, _raiseDistanceToStartDefault.OffsetY(MAX_DEVICE_TO_BALCONY)).ToLineString()).Where(TolLightRangeSingleSideMax => IsDrainageLabel(TolLightRangeSingleSideMax.UserData as string)).ToList();
                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE <= DISPENSABLENESS; MAX_ANGEL_TOLLERANCE++)
                  {
                    if (max_tag_xposition.Count > THESAURUSSTAMPEDE) break;
                    MAX_DEVICE_TO_BALCONY = HYPERDISYLLABLE + MAX_ANGEL_TOLLERANCE * THESAURUSACRIMONIOUS;
                    max_tag_xposition = min_balconybasin_to_balcony(new GLineSegment(_raiseDistanceToStartDefault, _raiseDistanceToStartDefault.OffsetY(MAX_DEVICE_TO_BALCONY)).ToLineString()).Where(TolLightRangeSingleSideMax => IsDrainageLabel(TolLightRangeSingleSideMax.UserData as string)).ToList();
                  }
                  if (max_tag_xposition.Count == THESAURUSHOUSING)
                  {
                    foreach (var maxToiletToFloordrainDistance1 in max_tag_xposition)
                    {
                      foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
                      {
                        MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData = maxToiletToFloordrainDistance1.UserData;
                        foreach (var pipe in sidewaterbucket_x_indent(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN))
                        {
                          if (pipe.UserData is not null)
                          {
                            continue;
                          }
                          pipe.UserData = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                        }
                      }
                    }
                  }
                }
                {
                  static bool IsWantedLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                  {
                    if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE is null) return INTRAVASCULARLY;
                    return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSELIGIBLE);
                  }
                  var max_tag_xposition = min_balconybasin_to_balcony(new GLineSegment(_raiseDistanceToStartDefault, _raiseDistanceToStartDefault.OffsetY(THESAURUSSURPRISED)).ToLineString()).Where(TolLightRangeSingleSideMax => IsWantedLabel(TolLightRangeSingleSideMax.UserData as string)).ToList();
                  if (max_tag_xposition.Count == THESAURUSHOUSING)
                  {
                    foreach (var maxToiletToFloordrainDistance1 in max_tag_xposition)
                    {
                      var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = maxToiletToFloordrainDistance1.UserData as string;
                      foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
                      {
                        MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData = maxToiletToFloordrainDistance1.UserData;
                      }
                      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSADVENT))
                      {
                        MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE.Add(GeoFac.CreateGeometryEx(_tol_avg_column_dist.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                      }
                      else if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(QUOTATIONMALTESE))
                      {
                        MAX_BALCONYRAINPIPE_TO_FLOORDRAIN_DISTANCE.Add(GeoFac.CreateGeometryEx(_tol_avg_column_dist.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                      }
                    }
                  }
                }
              }
            }
          }
        }
        var max_room_interval = GeoFac.CreateContainsSelector(max_balconybasin_to_balcony);
        var kitchen_buffer_distance = new List<HashSet<string>>();
        var max_toilet_to_kitchen_distance = new List<List<Geometry>>();
        var max_toilet_to_kitchen_distance1 = new List<List<Geometry>>();
        var max_kitchen_to_rainpipe_distance = new List<List<Geometry>>();
        var max_balcony_to_rainpipe_distance = new List<List<HashSet<string>>>();
        var max_toilet_to_condensepipe_distance = new List<Dictionary<string, string>>();
        var max_toilet_to_floordrain_distance = new List<Dictionary<string, string>>();
        var max_toilet_to_floordrain_distance2 = new List<Dictionary<string, string>>();
        var max_toilet_to_floordrain_distance1 = new List<List<Geometry>>();
        var max_balcony_to_deviceplatform_distance = new List<HashSet<string>>();
        var max_balconyrainpipe_to_floordrain_distance = new List<HashSet<string>>();
        using (var max_balcony_to_balcony_distance = new PriorityQueue(THESAURUSINCOMPLETE))
        {
          foreach (var TolLightRangeSingleSideMin in KITCHEN_BUFFER_DISTANCE)
          {
            var max_kitchen_to_balcony_distance = new HashSet<string>();
            kitchen_buffer_distance.Add(max_kitchen_to_balcony_distance);
            var min_deviceplatform_area = new List<HashSet<string>>();
            max_balcony_to_rainpipe_distance.Add(min_deviceplatform_area);
            var max_deviceplatform_area = new List<Geometry>();
            max_toilet_to_kitchen_distance.Add(max_deviceplatform_area);
            var max_basecircle_area = new List<Geometry>();
            max_toilet_to_kitchen_distance1.Add(max_basecircle_area);
            var MaxBalconywashingfloordrainToBalconyfloordrain = new List<Geometry>();
            max_kitchen_to_rainpipe_distance.Add(MaxBalconywashingfloordrainToBalconyfloordrain);
            var MaxBalconywashingfloordrainToRainpipe = new Dictionary<string, string>();
            max_toilet_to_condensepipe_distance.Add(MaxBalconywashingfloordrainToRainpipe);
            var MaxDeviceToDevice = new Dictionary<string, string>();
            max_toilet_to_floordrain_distance.Add(MaxDeviceToDevice);
            var MaxDeviceToBalcony = new List<Geometry>();
            max_toilet_to_floordrain_distance1.Add(MaxDeviceToBalcony);
            var MaxRainpipeToBalconyfloordrain = new Dictionary<string, string>();
            max_toilet_to_floordrain_distance2.Add(MaxRainpipeToBalconyfloordrain);
            var MinDownspoutToBalconyfloordrain = new HashSet<string>();
            max_balcony_to_deviceplatform_distance.Add(MinDownspoutToBalconyfloordrain);
            var MaxCondensepipeToWashmachine = new HashSet<string>();
            max_balconyrainpipe_to_floordrain_distance.Add(MaxCondensepipeToWashmachine);
            var MaxBalconywashingmachineToBalconybasinline = TolLightRangeSingleSideMin.Boundary.ToPolygon();
            var MaxDownspoutToBalconywashingfloordrain = max_room_interval(MaxBalconywashingmachineToBalconybasinline);
            var MaxRainpipeToWashmachine = GeoFac.CreateIntersectsSelector(MaxDownspoutToBalconywashingfloordrain);
            var Commonradius = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MAX_KITCHEN_TO_BALCONY_DISTANCE)(MaxBalconywashingmachineToBalconybasinline));
            var MaxBalconybasinToBalcony = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(balcony_buffer_distance)(MaxBalconywashingmachineToBalconybasinline));
            var BalconyBufferDistance = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MAX_TOILET_TO_FLOORDRAIN_DISTANCE1)(MaxBalconywashingmachineToBalconybasinline));
            var MinBalconybasinToBalcony = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MAX_BASECIRCLE_AREA)(MaxBalconywashingmachineToBalconybasinline));
            var MaxTagYposition = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE)(MaxBalconywashingmachineToBalconybasinline));
            var MaxTagXposition = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MAX_BALCONYRAINPIPE_TO_FLOORDRAIN_DISTANCE)(MaxBalconywashingmachineToBalconybasinline));
            var MaxTagLength = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(MIN_DEVICEPLATFORM_AREA.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSDERELICTION)).ToList())(MaxBalconywashingmachineToBalconybasinline));
            var wlineGeos = GeoFac.GroupLinesByConnPoints(GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(GeoFac.GetManyLines(MAX_TOILET_TO_CONDENSEPIPE_DISTANCE).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList())(MaxBalconywashingmachineToBalconybasinline))(MaxBalconywashingmachineToBalconybasinline), THESAURUSACRIMONIOUS).ToList();
            var TextIndent = GeoFac.CreateIntersectsSelector(wlineGeos);
            var dlineGeos = GeoFac.GroupLinesByConnPoints(GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(GeoFac.GetManyLines(MAX_KITCHEN_TO_RAINPIPE_DISTANCE).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList())(MaxBalconywashingmachineToBalconybasinline))(MaxBalconywashingmachineToBalconybasinline), THESAURUSACRIMONIOUS).ToList();
            var TextHeight = GeoFac.CreateIntersectsSelector(dlineGeos);
            max_balcony_to_balcony_distance.Enqueue((int)GeoCalState.GroupPipe, () =>
            {
              var MaxDownspoutToBalconywashingfloordrain = max_room_interval(TolLightRangeSingleSideMin.Boundary.ToPolygon());
              var vpsnf = GeoFac.NearestNeighboursGeometryF(MaxDownspoutToBalconywashingfloordrain.OfType<Geometry>().ToList());
              var okvps = new HashSet<Geometry>();
              foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
              {
                var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = toilet_wells_interval.UserData as string;
                if (!IsDraiLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                {
                  continue;
                }
                {
                  max_kitchen_to_balcony_distance.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                  if (IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                  {
                    var center = toilet_wells_interval.GetCenter();
                    var _vps = vpsnf(toilet_wells_interval.GetCenter().ToGRect(POLYOXYMETHYLENE).ToPolygon(), MaxDownspoutToBalconywashingfloordrain.Count).Where(TolLightRangeSingleSideMax => !okvps.Contains(TolLightRangeSingleSideMax)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter().GetDistanceTo(center) < POLYOXYMETHYLENE).ToList();
                                {
                      var text_indent = new List<Geometry>();
                      var text_height = new List<Geometry>();
                      foreach (var ToiletWellsInterval in _vps)
                      {
                        var SidewaterbucketYIndent = ToiletWellsInterval.UserData as string;
                        if (text_indent.Count == THESAURUSSTAMPEDE && text_height.Count == THESAURUSSTAMPEDE)
                        {
                          if (IsFL(SidewaterbucketYIndent)) text_indent.Add(ToiletWellsInterval);
                          else if (IsWL(SidewaterbucketYIndent)) text_height.Add(ToiletWellsInterval);
                        }
                        else if (text_indent.Count == THESAURUSHOUSING && text_height.Count == THESAURUSSTAMPEDE)
                        {
                          if (IsWL(SidewaterbucketYIndent)) text_height.Add(ToiletWellsInterval);
                        }
                        if (text_height.Count == THESAURUSHOUSING && text_indent.Count == THESAURUSSTAMPEDE)
                        {
                          if (IsFL(SidewaterbucketYIndent)) text_indent.Add(ToiletWellsInterval);
                        }
                        if (text_indent.Count == THESAURUSHOUSING && text_height.Count == THESAURUSHOUSING) break;
                                  }
                      if (text_indent.Count == THESAURUSHOUSING && text_height.Count == THESAURUSHOUSING)
                      {
                        okvps.AddRange(text_height);
                        okvps.AddRange(text_indent);
                        okvps.Add(toilet_wells_interval);
                                    var lbs = new HashSet<string>();
                        min_deviceplatform_area.Add(lbs);
                        foreach (var wl in text_height)
                        {
                          lbs.Add(wl.UserData as string);
                        }
                        foreach (var fl in text_indent)
                        {
                          lbs.Add(fl.UserData as string);
                        }
                        lbs.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                                    continue;
                      }
                    }
                    {
                      var max_tag_xposition = new List<Geometry>();
                      var text_height = new List<Geometry>();
                      foreach (var ToiletWellsInterval in _vps)
                      {
                        var SidewaterbucketYIndent = ToiletWellsInterval.UserData as string;
                        if (IsWL(SidewaterbucketYIndent))
                        {
                          text_height.Add(ToiletWellsInterval);
                          break;
                        }
                        if (IsPL(SidewaterbucketYIndent))
                        {
                          max_tag_xposition.Add(ToiletWellsInterval);
                          break;
                        }
                      }
                      if (max_tag_xposition.Count == THESAURUSHOUSING || text_height.Count == THESAURUSHOUSING)
                      {
                        okvps.AddRange(text_height);
                        okvps.AddRange(max_tag_xposition);
                        okvps.Add(toilet_wells_interval);
                                    var lbs = new HashSet<string>();
                        min_deviceplatform_area.Add(lbs);
                        foreach (var wl in text_height)
                        {
                          lbs.Add(wl.UserData as string);
                        }
                        foreach (var fl in max_tag_xposition)
                        {
                          lbs.Add(fl.UserData as string);
                        }
                        lbs.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                                    continue;
                      }
                    }
                  }
                }
              }
            });
            max_balcony_to_balcony_distance.Enqueue((int)GeoCalState.MarkCompsToPipe, () =>
            {
                          max_balcony_to_balcony_distance.Enqueue((int)GeoCalState.MarkTranslator, () =>
                          {
                              foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
                  {
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = toilet_wells_interval.UserData as string;
                    if (string.IsNullOrEmpty(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      var SidewaterbucketXIndent = INTRAVASCULARLY;
                      {
                        var WellToWallOffset = GeoFac.CreateGeometry(TextIndent(toilet_wells_interval));
                        foreach (var ToiletWellsInterval in MaxRainpipeToWashmachine(WellToWallOffset))
                        {
                          var SidewaterbucketYIndent = ToiletWellsInterval.UserData as string;
                          if (IsDrainageLabel(SidewaterbucketYIndent))
                          {
                            toilet_wells_interval.UserData = SidewaterbucketYIndent;
                            if (toilet_wells_interval.GetCenter().GetDistanceTo(ToiletWellsInterval.GetCenter()) > THESAURUSHYPNOTIC)
                            {
                              MinDownspoutToBalconyfloordrain.Add(SidewaterbucketYIndent);
                            }
                            else
                            {
                              MaxCondensepipeToWashmachine.Add(SidewaterbucketYIndent);
                            }
                            SidewaterbucketXIndent = THESAURUSOBSTINACY;
                            break;
                          }
                        }
                      }
                      if (!SidewaterbucketXIndent)
                      {
                        var ToiletBufferDistance = GeoFac.CreateGeometry(TextHeight(toilet_wells_interval));
                        foreach (var ToiletWellsInterval in MaxRainpipeToWashmachine(ToiletBufferDistance))
                        {
                          var SidewaterbucketYIndent = ToiletWellsInterval.UserData as string;
                          if (IsDrainageLabel(SidewaterbucketYIndent))
                          {
                            toilet_wells_interval.UserData = SidewaterbucketYIndent;
                            if (toilet_wells_interval.GetCenter().GetDistanceTo(ToiletWellsInterval.GetCenter()) > THESAURUSHYPNOTIC)
                            {
                              MinDownspoutToBalconyfloordrain.Add(SidewaterbucketYIndent);
                                          {
                                var WellsMaxArea = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(max_balconywashingfloordrain_to_balconyfloordrain)(MaxBalconywashingmachineToBalconybasinline));
                                var shooters = WellsMaxArea(toilet_wells_interval).Concat(WellsMaxArea(ToiletWellsInterval)).Distinct().ToList();
                                            if (shooters.Count == THESAURUSPERMUTATION)
                                {
                                  var TolRegroupMainYRange = Regex.Match(shooters[THESAURUSSTAMPEDE].UserData as string, NEUROTRANSMITTER);
                                  var TolConnectSecPtRange = Regex.Match(shooters[THESAURUSHOUSING].UserData as string, NEUROTRANSMITTER);
                                  if (TolRegroupMainYRange.Success && TolConnectSecPtRange.Success)
                                  {
                                    var well_to_wall_offset = new double[] { double.Parse(TolRegroupMainYRange.Groups[THESAURUSHOUSING].Value), double.Parse(TolConnectSecPtRange.Groups[THESAURUSHOUSING].Value) };
                                    var _tolReturnValueMaxDistance = well_to_wall_offset.Min();
                                    var _tolReturnValueMinRange = well_to_wall_offset.Max();
                                    if (_tolReturnValueMinRange / _tolReturnValueMaxDistance > THESAURUSPERMUTATION)
                                    {
                                                  MinDownspoutToBalconyfloordrain.Remove(SidewaterbucketYIndent);
                                    }
                                  }
                                }
                              }
                            }
                            else
                            {
                              MaxCondensepipeToWashmachine.Add(SidewaterbucketYIndent);
                            }
                            SidewaterbucketXIndent = THESAURUSOBSTINACY;
                            break;
                          }
                        }
                      }
                    }
                  }
                              {
                    var shooters = GeoFac.CreateContainsSelector(MAX_DEVICEPLATFORM_AREA)(MaxBalconywashingmachineToBalconybasinline);
                    foreach (var default_fire_valve_width in shooters.Where(TolLightRangeSingleSideMax => IsDrainageLabel(TolLightRangeSingleSideMax.UserData as string)).GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData))
                    {
                      var MinWellToUrinalDistance = default_fire_valve_width.ToList();
                      if (MinWellToUrinalDistance.Count == THESAURUSPERMUTATION)
                      {
                        if (MinWellToUrinalDistance[THESAURUSSTAMPEDE].GetCenter().GetDistanceTo(MinWellToUrinalDistance[THESAURUSHOUSING].GetCenter()) > THESAURUSHYPNOTIC)
                        {
                          MinDownspoutToBalconyfloordrain.Add(default_fire_valve_width.Key as string);
                        }
                      }
                    }
                  }
                              foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
                  {
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = toilet_wells_interval.UserData as string;
                    if (string.IsNullOrEmpty(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      foreach (var ToiletWellsInterval in MaxRainpipeToWashmachine(toilet_wells_interval.ToGRect().Expand(THESAURUSENTREPRENEUR).ToPolygon()))
                      {
                        var SidewaterbucketYIndent = ToiletWellsInterval.UserData as string;
                        if (IsDrainageLabel(SidewaterbucketYIndent))
                        {
                          toilet_wells_interval.UserData = SidewaterbucketYIndent;
                          MaxCondensepipeToWashmachine.Add(SidewaterbucketYIndent);
                          break;
                        }
                      }
                    }
                  }
                });
              max_balcony_to_balcony_distance.Enqueue((int)GeoCalState.MarkCompsToPipe, () =>
                          {
                  foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
                  {
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = toilet_wells_interval.UserData as string;
                    if (IsDrainageLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      max_kitchen_to_balcony_distance.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                      var WellToWallOffset = GeoFac.CreateGeometry(TextIndent(toilet_wells_interval));
                      foreach (var wp in BalconyBufferDistance(WellToWallOffset))
                      {
                        foreach (var sidewaterbucket_y_indent in MinBalconybasinToBalcony(wp))
                        {
                          var repeated_point_distance = TryParseWrappingPipeDNText(sidewaterbucket_y_indent.UserData as string);
                          if (repeated_point_distance is not null)
                          {
                            MaxDeviceToDevice[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = repeated_point_distance;
                            break;
                          }
                        }
                        MaxDeviceToBalcony.Add(wp.Clone().Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                      }
                      if (IsRainLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                      {
                        foreach (var fd in Commonradius(WellToWallOffset))
                        {
                          max_basecircle_area.Add(fd.Clone().Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                        }
                        foreach (var cp in MaxBalconybasinToBalcony(WellToWallOffset))
                        {
                          MaxBalconywashingfloordrainToBalconyfloordrain.Add(cp.Clone().Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                        }
                        if (MaxTagYposition(WellToWallOffset).Any())
                        {
                          MaxBalconywashingfloordrainToRainpipe[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = THESAURUSADVENT;
                        }
                        else if (MaxTagXposition(WellToWallOffset).Any())
                        {
                          MaxBalconywashingfloordrainToRainpipe[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = VICISSITUDINOUS;
                        }
                        else if (MaxTagLength(WellToWallOffset).Any())
                        {
                          MaxBalconywashingfloordrainToRainpipe[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = THESAURUSINTENTIONAL;
                        }
                      }
                      else if (IsDraiLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                      {
                        if (IsDraiFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                        {
                          foreach (var fd in Commonradius(GeoFac.CreateGeometry(TextHeight(toilet_wells_interval))))
                          {
                            max_basecircle_area.Add(fd.Clone().Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                          }
                        }
                      }
                    }
                  }
                });
            });
            max_balcony_to_balcony_distance.Enqueue((int)GeoCalState.MarkWaterBucketToPipe, () =>
            {
              foreach (var MaxRoomInterval in GeoFac.CreateContainsSelector(MAX_TOILET_TO_FLOORDRAIN_DISTANCE2)(MaxBalconywashingmachineToBalconybasinline))
              {
                            var KitchenBufferDistance = max_downspout_to_balconywashingfloordrain(TolLightRangeSingleSideMin).ToList();
                            if (KitchenBufferDistance.Count > THESAURUSSTAMPEDE)
                {
                  var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = KitchenBufferDistance[THESAURUSSTAMPEDE];
                  var MaxToiletToKitchenDistance = commonradius(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
                  if (MaxToiletToKitchenDistance is not null)
                  {
                    var MaxToiletToKitchenDistance1 = max_balconywashingmachine_to_balconybasinline(MaxToiletToKitchenDistance);
                    if (MaxToiletToKitchenDistance1 is not null)
                    {
                      var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = MaxToiletToKitchenDistance1.ContraPoint - TolLightRangeSingleSideMin.ContraPoint;
                      var MaxKitchenToRainpipeDistance = MaxRoomInterval.UserData as string;
                      var MaxBalconyToRainpipeDistance = (MaxRoomInterval.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).ToGRect(QUOTATIONWITTIG).ToPolygon();
                                  if (!string.IsNullOrEmpty(MaxKitchenToRainpipeDistance))
                      {
                        var MaxDownspoutToBalconywashingfloordrain = max_room_interval(MaxToiletToKitchenDistance1.Boundary.ToPolygon());
                        foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
                        {
                          var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = toilet_wells_interval.UserData as string;
                                      if (IsY1L(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                          {
                            if (MaxBalconyToRainpipeDistance.Intersects(toilet_wells_interval))
                            {
                              MaxRainpipeToBalconyfloordrain[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = MaxKitchenToRainpipeDistance;
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            });
          }
        }
        var MaxToiletToCondensepipeDistance = kitchen_buffer_distance.SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToHashSet();
        List<PipeLine> pipeLines = new();
        static bool IsDraiType(PipeType MaxToiletToFloordrainDistance)
        {
          var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = MaxToiletToFloordrainDistance.ToString();
          return MaxToiletToFloordrainDistance != PipeType.FL0 && (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Contains(THESAURUSBASELESS) || MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Contains(THESAURUSDECLAIM) || MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Contains(THESAURUSPOSSESSIVE) || MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Contains(INCORRESPONDENCE) || MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Contains(THESAURUSCONFIRM));
        }
        static bool IsRainType(PipeType MaxToiletToFloordrainDistance)
        {
          return MaxToiletToFloordrainDistance is PipeType.Y1L or PipeType.Y2L or PipeType.YL or PipeType.NL or PipeType.FL0;
        }
        static PipeType GetPipeType(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
        {
          if (IsY1L(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.Y1L;
          if (IsY2L(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.Y2L;
          if (IsNL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.NL;
          if (IsYL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.YL;
          if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.FL0;
          if (IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.FL;
          if (IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.PL;
          if (IsWL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.WL;
          if (IsDL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.DL;
          if (IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return PipeType.TL;
          return PipeType.Unknown;
        }
        var MaxToiletToFloordrainDistance2 = new HashSet<string>();
        foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in MaxToiletToCondensepipeDistance)
        {
          if (MaxToiletToFloordrainDistance2.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) continue;
          var MaxBalconyToBalconyDistance = new PipeLine();
          var MaxKitchenToBalconyDistance = new HashSet<string>() { MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE };
          pipeLines.Add(MaxBalconyToBalconyDistance);
          PipeType MaxBalconyrainpipeToFloordrainDistance = default;
          string MaxBalconyToDeviceplatformDistance = default;
          string MaxToiletToFloordrainDistance1 = default;
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < SIDEWATERBUCKET_Y_INDENT.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE];
            var TolLightRangeSingleSideMin = max_balconywashingmachine_to_balconybasinline(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
            string MinDeviceplatformArea()
            {
              if (TolLightRangeSingleSideMin is null) return null;
              max_toilet_to_floordrain_distance2[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out var MaxRoomInterval);
              return MaxRoomInterval;
            }
            bool MaxDeviceplatformArea()
            {
              if (TolLightRangeSingleSideMin is null) return INTRAVASCULARLY;
              var tolGroupEmgLightEvac = max_balcony_to_deviceplatform_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              if (tolGroupEmgLightEvac)
              {
                var MinWellToUrinalDistance = max_downspout_to_balconywashingfloordrain(TolLightRangeSingleSideMin).ToList();
                if (MinWellToUrinalDistance.Count > THESAURUSHOUSING)
                {
                  return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == MinWellToUrinalDistance.OrderBy(GetStoreyScore).Last();
                }
              }
              return tolGroupEmgLightEvac;
            }
            bool MaxBasecircleArea()
            {
              if (TolLightRangeSingleSideMin is null) return INTRAVASCULARLY;
              var tolGroupEmgLightEvac = !MaxDeviceplatformArea() && max_balconyrainpipe_to_floordrain_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              if (tolGroupEmgLightEvac)
              {
                var MinWellToUrinalDistance = max_downspout_to_balconywashingfloordrain(TolLightRangeSingleSideMin).ToList();
                if (MinWellToUrinalDistance.Count > THESAURUSHOUSING)
                {
                  return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == MinWellToUrinalDistance.OrderBy(GetStoreyScore).Last();
                }
              }
              return tolGroupEmgLightEvac;
            }
            int maxBalconywashingfloordrainToBalconyfloordrain()
            {
              if (TolLightRangeSingleSideMin is null) return THESAURUSSTAMPEDE;
              var maxBalconyrainpipeToFloordrainDistance = THESAURUSSTAMPEDE;
              foreach (var wp in max_toilet_to_floordrain_distance1[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)])
              {
                if (wp.UserData as string == MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) ++maxBalconyrainpipeToFloordrainDistance;
              }
              return maxBalconyrainpipeToFloordrainDistance;
            }
            int maxBalconywashingfloordrainToRainpipe()
            {
              if (TolLightRangeSingleSideMin is null) return THESAURUSSTAMPEDE;
              var maxBalconyrainpipeToFloordrainDistance = THESAURUSSTAMPEDE;
              foreach (var fd in max_toilet_to_kitchen_distance1[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)])
              {
                if (fd.UserData as string == MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) ++maxBalconyrainpipeToFloordrainDistance;
              }
              if (maxBalconyrainpipeToFloordrainDistance > THESAURUSPERMUTATION) maxBalconyrainpipeToFloordrainDistance = THESAURUSPERMUTATION;
              return maxBalconyrainpipeToFloordrainDistance;
            }
            int maxDeviceToDevice()
            {
              if (TolLightRangeSingleSideMin is null) return THESAURUSSTAMPEDE;
              var maxBalconyrainpipeToFloordrainDistance = THESAURUSSTAMPEDE;
              foreach (var cp in max_kitchen_to_rainpipe_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)])
              {
                if (cp.UserData as string == MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) ++maxBalconyrainpipeToFloordrainDistance;
              }
              if (maxBalconyrainpipeToFloordrainDistance > THESAURUSPERMUTATION) maxBalconyrainpipeToFloordrainDistance = THESAURUSPERMUTATION;
              return maxBalconyrainpipeToFloordrainDistance;
            }
            bool maxDeviceToBalcony()
            {
              if (TolLightRangeSingleSideMin is null) return INTRAVASCULARLY;
              if (kitchen_buffer_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return THESAURUSOBSTINACY;
              if (max_balcony_to_rainpipe_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return THESAURUSOBSTINACY;
              return INTRAVASCULARLY;
            }
            string maxRainpipeToBalconyfloordrain()
            {
              if (MAX_ANGEL_TOLLERANCE != THESAURUSSTAMPEDE) return null;
              if (TolLightRangeSingleSideMin is null) return null;
              max_toilet_to_floordrain_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out var DEFAULT_FIRE_VALVE_WIDTH);
              return DEFAULT_FIRE_VALVE_WIDTH;
            }
            string minDownspoutToBalconyfloordrain()
            {
              if (IsDraiType(maxCondensepipeToWashmachine())) return THESAURUSPAGEANT;
              if (MAX_ANGEL_TOLLERANCE != THESAURUSSTAMPEDE) return null;
              if (TolLightRangeSingleSideMin is null) return null;
              max_toilet_to_condensepipe_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out var MaxBalconyToDeviceplatformDistance);
              return MaxBalconyToDeviceplatformDistance;
            }
            PipeType maxCondensepipeToWashmachine()
            {
              if (TolLightRangeSingleSideMin is null) return PipeType.Unknown;
              var max_kitchen_to_balcony_distance = max_balcony_to_rainpipe_distance[KITCHEN_BUFFER_DISTANCE.IndexOf(TolLightRangeSingleSideMin)].FirstOrDefault(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
              if (max_kitchen_to_balcony_distance is null)
              {
                MaxToiletToFloordrainDistance2.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                MaxKitchenToBalconyDistance.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                return GetPipeType(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              }
              MaxToiletToFloordrainDistance2.AddRange(max_kitchen_to_balcony_distance);
              MaxKitchenToBalconyDistance.AddRange(max_kitchen_to_balcony_distance);
              if (max_kitchen_to_balcony_distance.Count == INTROPUNITIVENESS)
              {
                return PipeType.WLTLFL;
              }
              if (max_kitchen_to_balcony_distance.Count == THESAURUSPERMUTATION)
              {
                if (max_kitchen_to_balcony_distance.Any(IsTL) && max_kitchen_to_balcony_distance.Any(IsPL)) return PipeType.PLTL;
                if (max_kitchen_to_balcony_distance.Any(IsTL) && max_kitchen_to_balcony_distance.Any(IsWL)) return PipeType.WLTL;
              }
              return PipeType.Unknown;
            }
            var MaxToiletToFloordrainDistance = maxCondensepipeToWashmachine();
            if (MaxBalconyrainpipeToFloordrainDistance == PipeType.Unknown) MaxBalconyrainpipeToFloordrainDistance = MaxToiletToFloordrainDistance;
            MaxBalconyToBalconyDistance.Runs.Add(new() { Index = MAX_ANGEL_TOLLERANCE, Storey = MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN, HasLong = INTRAVASCULARLY, HasShort = INTRAVASCULARLY, Exists = maxDeviceToBalcony(), });
            var DEFAULT_FIRE_VALVE_WIDTH = MaxBalconyToBalconyDistance.Runs.Last();
            if (DEFAULT_FIRE_VALVE_WIDTH.Exists)
            {
              DEFAULT_FIRE_VALVE_WIDTH.HasLong = MaxDeviceplatformArea();
              DEFAULT_FIRE_VALVE_WIDTH.HasShort = MaxBasecircleArea();
              DEFAULT_FIRE_VALVE_WIDTH.FDSCount = maxBalconywashingfloordrainToRainpipe();
              DEFAULT_FIRE_VALVE_WIDTH.CPSCount = maxDeviceToDevice();
              DEFAULT_FIRE_VALVE_WIDTH.WPSCount = maxBalconywashingfloordrainToBalconyfloordrain();
              if (DEFAULT_FIRE_VALVE_WIDTH.WPSCount > DEFAULT_FIRE_VALVE_WIDTH.FDSCount) DEFAULT_FIRE_VALVE_WIDTH.WPSCount = DEFAULT_FIRE_VALVE_WIDTH.FDSCount;
              MaxBalconyToDeviceplatformDistance ??= minDownspoutToBalconyfloordrain();
              MaxToiletToFloordrainDistance1 ??= maxRainpipeToBalconyfloordrain();
              if (MaxBalconyrainpipeToFloordrainDistance.ToString().Contains(THESAURUSDECLAIM))
              {
                if (GetStoreyScore(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN) < GetStoreyScore(SIDEWATERBUCKET_Y_INDENT[max_condensepipe_to_washmachine]))
                {
                  DEFAULT_FIRE_VALVE_WIDTH.HasCleaningPort = THESAURUSOBSTINACY;
                }
              }
            }
            DEFAULT_FIRE_VALVE_WIDTH.WaterBucket = MinDeviceplatformArea();
          }
          if (MaxBalconyrainpipeToFloordrainDistance == PipeType.Unknown) MaxBalconyrainpipeToFloordrainDistance = PipeType.FL;
          MaxBalconyToBalconyDistance.PipeType = MaxBalconyrainpipeToFloordrainDistance;
          MaxBalconyToBalconyDistance.Outlet = MaxBalconyToDeviceplatformDistance;
          MaxToiletToFloordrainDistance1 ??= THESAURUSRECTIFY;
          MaxBalconyToBalconyDistance.WPRadius = MaxToiletToFloordrainDistance1;
          MaxBalconyToBalconyDistance.Labels.AddRange(MaxKitchenToBalconyDistance.Distinct().OrderBy(y => GetPipeType(y)).ThenBy(y => y));
          {
            if (MaxBalconyToBalconyDistance.PipeType is PipeType.TL) MaxBalconyToBalconyDistance.PipeType = PipeType.PL;
            if (string.IsNullOrEmpty(MaxBalconyToBalconyDistance.Outlet))
            {
              if (IsDraiType((MaxBalconyToBalconyDistance.PipeType)))
              {
                MaxBalconyToBalconyDistance.Outlet = THESAURUSPAGEANT;
              }
              else
              {
                MaxBalconyToBalconyDistance.Outlet = THESAURUSINTENTIONAL;
              }
            }
            for (int MAX_ANGEL_TOLLERANCE = MaxBalconyToBalconyDistance.Runs.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE--)
            {
              var DEFAULT_FIRE_VALVE_WIDTH = MaxBalconyToBalconyDistance.Runs[MAX_ANGEL_TOLLERANCE];
              if (DEFAULT_FIRE_VALVE_WIDTH.Exists)
              {
                --MAX_ANGEL_TOLLERANCE;
                while (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE)
                {
                  DEFAULT_FIRE_VALVE_WIDTH = MaxBalconyToBalconyDistance.Runs[MAX_ANGEL_TOLLERANCE];
                  DEFAULT_FIRE_VALVE_WIDTH.Exists = THESAURUSOBSTINACY;
                  --MAX_ANGEL_TOLLERANCE;
                }
                break;
              }
            }
            {
              if (MaxBalconyToBalconyDistance.PipeType is PipeType.Y1L)
              {
                var wbk = MaxBalconyToBalconyDistance.Runs.FirstOrDefault(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.WaterBucket is not null);
                if (wbk is null)
                {
                  for (int MAX_ANGEL_TOLLERANCE = MaxBalconyToBalconyDistance.Runs.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE; --MAX_ANGEL_TOLLERANCE)
                  {
                    var DEFAULT_FIRE_VALVE_WIDTH = MaxBalconyToBalconyDistance.Runs[MAX_ANGEL_TOLLERANCE];
                    DEFAULT_FIRE_VALVE_WIDTH.Exists = THESAURUSOBSTINACY;
                    DEFAULT_FIRE_VALVE_WIDTH.WaterBucket = THESAURUSIMPOUND;
                  }
                }
              }
            }
            {
              if (MaxBalconyToBalconyDistance.Runs.Count(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists) <= THESAURUSHOUSING)
              {
                foreach (var DEFAULT_FIRE_VALVE_WIDTH in MaxBalconyToBalconyDistance.Runs)
                {
                  if (IsDraiType(MaxBalconyToBalconyDistance.PipeType))
                  {
                    if (GetStoreyScore((DEFAULT_FIRE_VALVE_WIDTH.Storey)) <= GetStoreyScore(THESAURUSARGUMENTATIVE))
                    {
                      DEFAULT_FIRE_VALVE_WIDTH.Exists = THESAURUSOBSTINACY;
                    }
                  }
                  else
                  {
                    DEFAULT_FIRE_VALVE_WIDTH.Exists = THESAURUSOBSTINACY;
                    if (IsRainType(MaxBalconyToBalconyDistance.PipeType))
                    {
                      DEFAULT_FIRE_VALVE_WIDTH.WaterBucket = THESAURUSIMPOUND;
                    }
                  }
                }
              }
              if (MaxBalconyToBalconyDistance.Runs.Count(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists) <= THESAURUSHOUSING)
              {
                foreach (var DEFAULT_FIRE_VALVE_WIDTH in MaxBalconyToBalconyDistance.Runs)
                {
                  DEFAULT_FIRE_VALVE_WIDTH.Exists = THESAURUSOBSTINACY;
                  if (IsRainType(MaxBalconyToBalconyDistance.PipeType))
                  {
                    DEFAULT_FIRE_VALVE_WIDTH.WaterBucket = THESAURUSIMPOUND;
                  }
                }
              }
            }
          }
          for (int MAX_ANGEL_TOLLERANCE = MaxBalconyToBalconyDistance.Runs.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE;)
          {
            var DEFAULT_FIRE_VALVE_WIDTH = MaxBalconyToBalconyDistance.Runs[MAX_ANGEL_TOLLERANCE];
            if (DEFAULT_FIRE_VALVE_WIDTH.WaterBucket is not null)
            {
              --MAX_ANGEL_TOLLERANCE;
              while (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE)
              {
                MaxBalconyToBalconyDistance.Runs[MAX_ANGEL_TOLLERANCE].WaterBucket = null;
                --MAX_ANGEL_TOLLERANCE;
              }
            }
            else
            {
              --MAX_ANGEL_TOLLERANCE;
            }
          }
          if (MaxBalconyrainpipeToFloordrainDistance == PipeType.FL)
          {
            if (MaxBalconyToBalconyDistance.Runs.Where(DEFAULT_FIRE_VALVE_WIDTH => DEFAULT_FIRE_VALVE_WIDTH.Exists).All(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.FDSCount == THESAURUSSTAMPEDE))
            {
              foreach (var DEFAULT_FIRE_VALVE_WIDTH in MaxBalconyToBalconyDistance.Runs)
              {
                if (DEFAULT_FIRE_VALVE_WIDTH.Exists)
                {
                  if (GetStoreyScore(DEFAULT_FIRE_VALVE_WIDTH.Storey) < GetStoreyScore(SIDEWATERBUCKET_Y_INDENT[max_condensepipe_to_washmachine]))
                  {
                    DEFAULT_FIRE_VALVE_WIDTH.HasBasin = THESAURUSOBSTINACY;
                  }
                }
              }
            }
          }
        }
        if (!vm.Params.YS)
        {
          pipeLines = pipeLines.Where(TolLightRangeSingleSideMax => !IsRainType(TolLightRangeSingleSideMax.PipeType)).ToList();
        }
        if (!vm.Params.WFS)
        {
          pipeLines = pipeLines.Where(TolLightRangeSingleSideMax => !IsDraiType(TolLightRangeSingleSideMax.PipeType)).ToList();
        }
        var gpItems = pipeLines.GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.OrderBy(y => GetPipeType(y.Labels.First())).ThenBy(y => y.Labels.First()).ToList()).OrderBy(TolLightRangeSingleSideMax => GetPipeType(TolLightRangeSingleSideMax.First().Labels.First())).ThenBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.First().Labels.First()).ToList();
        using (var max_balcony_to_balcony_distance = new PriorityQueue(THESAURUSINCOMPLETE))
        {
          var MAX_RAINPIPE_TO_WASHMACHINE = new List<DBTextInfo>(THESAURUSREPERCUSSION);
          var COMMONRADIUS = new List<BlockInfo>(THESAURUSREPERCUSSION);
          var MAX_BALCONYBASIN_TO_BALCONY = new List<LineInfo>(THESAURUSREPERCUSSION);
          var BALCONY_BUFFER_DISTANCE = new List<CircleInfo>(THESAURUSREPERCUSSION);
          var MIN_BALCONYBASIN_TO_BALCONY = new List<DimInfo>(THESAURUSREPERCUSSION);
          var maxBalconywashingmachineToBalconybasinline = new DrainageLayoutManager(MAX_RAINPIPE_TO_WASHMACHINE, COMMONRADIUS, MAX_BALCONYBASIN_TO_BALCONY, BALCONY_BUFFER_DISTANCE, MIN_BALCONYBASIN_TO_BALCONY);
          var HEIGHT = THESAURUSINCOMING;
          var _tolReturnValueDistCheck = new List<int>(SIDEWATERBUCKET_Y_INDENT.Count);
          {
            var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = THESAURUSSTAMPEDE;
            var _vm = FloorHeightsViewModel.Instance;
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < SIDEWATERBUCKET_Y_INDENT.Count; MAX_ANGEL_TOLLERANCE++)
            {
              _tolReturnValueDistCheck.Add(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
              var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _vm.GeneralFloor;
              if (_vm.ExistsSpecialFloor) MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _vm.Items.FirstOrDefault(tolReturnValueMinRange => test(tolReturnValueMinRange.Floor, GetStoreyScore(SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE])))?.Height ?? MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
              MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN += MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
              static bool test(string TolLightRangeSingleSideMax, int TolLightRangeMin)
              {
                var tolReturnValueMinRange = Regex.Match(TolLightRangeSingleSideMax, QUOTATIONSTYLOGRAPHIC);
                if (tolReturnValueMinRange.Success)
                {
                  if (int.TryParse(tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value, out int _tolReturnValue0Approx) && int.TryParse(tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value, out int TolGroupEvcaEmg))
                  {
                    var _tolReturnValueMaxDistance = Math.Min(_tolReturnValue0Approx, TolGroupEvcaEmg);
                    var _tolReturnValueMinRange = Math.Max(_tolReturnValue0Approx, TolGroupEvcaEmg);
                    for (int MAX_ANGEL_TOLLERANCE = _tolReturnValueMaxDistance; MAX_ANGEL_TOLLERANCE <= _tolReturnValueMinRange; MAX_ANGEL_TOLLERANCE++)
                    {
                      if (MAX_ANGEL_TOLLERANCE == TolLightRangeMin) return THESAURUSOBSTINACY;
                    }
                  }
                  else
                  {
                    return INTRAVASCULARLY;
                  }
                }
                tolReturnValueMinRange = Regex.Match(TolLightRangeSingleSideMax, TETRAIODOTHYRONINE);
                if (tolReturnValueMinRange.Success)
                {
                  if (int.TryParse(TolLightRangeSingleSideMax, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN))
                  {
                    if (MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN == TolLightRangeMin) return THESAURUSOBSTINACY;
                  }
                }
                return INTRAVASCULARLY;
              }
            }
          }
          const double maxDownspoutToBalconywashingfloordrain = ThWSDStorey.RF_OFFSET_Y;
          var maxRainpipeToWashmachine = gpItems.Count;
          var maxBalconybasinToBalcony = BALANOPHORACEAE;
          var balconyBufferDistance = maxBalconybasinToBalcony * maxRainpipeToWashmachine * QUOTATIONEDIBLE;
          var minBalconybasinToBalcony = THESAURUSLEGISLATION;
          var maxTagYposition = LAUTENKLAVIZIMBEL;
          var WELL_TO_WALL_OFFSET = minBalconybasinToBalcony + maxRainpipeToWashmachine * maxBalconybasinToBalcony + maxTagYposition;
          Point3d maxTagXposition(int MAX_ANGEL_TOLLERANCE, int MAX_ANGLE_TOLLERANCE)
          {
            return maxTagLength(MAX_ANGEL_TOLLERANCE).OffsetX(minBalconybasinToBalcony + MAX_ANGLE_TOLLERANCE * maxBalconybasinToBalcony);
          }
          Point3d maxTagLength(int MAX_ANGEL_TOLLERANCE)
          {
            return SIDEWATERBUCKET_X_INDENT.OffsetY(HEIGHT * MAX_ANGEL_TOLLERANCE);
          }
          foreach (var MAX_ANGEL_TOLLERANCE in Enumerable.Range(THESAURUSSTAMPEDE, SIDEWATERBUCKET_Y_INDENT.Count).OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax))
          {
            var tolGroupBlkLane = SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE];
            string textIndent()
            {
              if (tolGroupBlkLane is THESAURUSREGION) return MULTINATIONALLY;
              var tolGroupEmgLightEvac = (_tolReturnValueDistCheck[MAX_ANGEL_TOLLERANCE] / LAUTENKLAVIZIMBEL).ToString(THESAURUSINFINITY); ;
              if (tolGroupEmgLightEvac == THESAURUSINFINITY) return MULTINATIONALLY;
              return tolGroupEmgLightEvac;
            }
            var _tolReturnValueMax = maxTagLength(MAX_ANGEL_TOLLERANCE);
            textHeight(tolGroupBlkLane, _tolReturnValueMax, WELL_TO_WALL_OFFSET, textIndent());
            void textHeight(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, Point3d basePt, double WELL_TO_WALL_OFFSET, string repeated_point_distance)
            {
              {
                var TolUniformSideLenth = DrawLineLazy(basePt.X, basePt.Y, basePt.X + WELL_TO_WALL_OFFSET, basePt.Y);
                var maxBalconyToDeviceplatformDistance = DrawTextLazy(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
                Dr.SetLabelStylesForWNote(TolUniformSideLenth, maxBalconyToDeviceplatformDistance);
                DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.OffsetX(QUOTATIONPITUITARY), layer: COSTERMONGERING, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, repeated_point_distance } });
              }
              if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == THESAURUSARGUMENTATIVE)
              {
                var TolUniformSideLenth = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + maxDownspoutToBalconywashingfloordrain, THESAURUSSTAMPEDE), new Point3d(basePt.X + WELL_TO_WALL_OFFSET, basePt.Y + maxDownspoutToBalconywashingfloordrain, THESAURUSSTAMPEDE));
                var maxBalconyToDeviceplatformDistance = DrawTextLazy(THESAURUSSHADOWY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + maxDownspoutToBalconywashingfloordrain + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
                Dr.SetLabelStylesForWNote(TolUniformSideLenth, maxBalconyToDeviceplatformDistance);
              }
            }
          }
          var sidewaterbucketXIndent = SIDEWATERBUCKET_Y_INDENT.IndexOf(THESAURUSARGUMENTATIVE);
          var sidewaterbucketYIndent = SIDEWATERBUCKET_Y_INDENT.Count - THESAURUSHOUSING;
          static string GetPipeLayer(PipeType MaxBalconyrainpipeToFloordrainDistance)
          {
            if (MaxBalconyrainpipeToFloordrainDistance is PipeType.TL) return THUNDERSTRICKEN;
            if (MaxBalconyrainpipeToFloordrainDistance is PipeType.FL) return THESAURUSADVERSITY;
            return IsDraiType(MaxBalconyrainpipeToFloordrainDistance) ? THESAURUSCONTROVERSY : INSTRUMENTALITY;
          }
          static string GetNoteLayer(PipeType MaxBalconyrainpipeToFloordrainDistance)
          {
            return IsDraiType(MaxBalconyrainpipeToFloordrainDistance) ? THESAURUSSTRIPED : CIRCUMCONVOLUTION;
          }
          static string GetEQPMLayer(PipeType MaxBalconyrainpipeToFloordrainDistance)
          {
            return IsDraiType(MaxBalconyrainpipeToFloordrainDistance) ? THESAURUSJUBILEE : DENDROCHRONOLOGIST;
          }
          foreach (var MAX_ANGLE_TOLLERANCE in Enumerable.Range(THESAURUSSTAMPEDE, maxRainpipeToWashmachine))
          {
            var _tolReturnValueRange = gpItems[MAX_ANGLE_TOLLERANCE].First();
            var max_kitchen_to_balcony_distance = gpItems[MAX_ANGLE_TOLLERANCE].SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Labels).ToHashSet();
            var eMax = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Max();
            var pipeLineInfos = new List<LineInfo>(THESAURUSREPERCUSSION);
            var MAX_DEVICE_TO_DEVICE = QUOTATIONTRANSFERABLE;
            var wellToWallOffset = new List<Point3d>();
            var toiletWellsInterval = new List<Point3d>();
            var toiletBufferDistance = new List<GRect>();
            foreach (var MAX_ANGEL_TOLLERANCE in Enumerable.Range(THESAURUSSTAMPEDE, SIDEWATERBUCKET_Y_INDENT.Count).OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax))
            {
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = maxTagXposition(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE).OffsetX(MAX_DEVICE_TO_DEVICE);
              if (_tolReturnValueRange.Outlet is null)
              {
                max_balcony_to_balcony_distance.Enqueue((int)LayoutState.SanPaiMark, () =>
                {
                                  var eMin = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Min();
                  if (eMin is THESAURUSSTAMPEDE) return;
                  if (MAX_ANGEL_TOLLERANCE == eMin)
                  {
                                    if (_tolReturnValueRange.Runs.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING)?.WaterBucket is not null)
                    {
                                      wellToWallOffset.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED));
                      toiletWellsInterval.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED - ASSOCIATIONISTS));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY)), CIRCUMCONVOLUTION));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -QUOTATIONPITUITARY)), CIRCUMCONVOLUTION));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFRISKY, -QUOTATIONWITTIG), THESAURUSEXECUTIVE + _tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].Storey, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                    }
                  }
                });
              }
              if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].WaterBucket is not null)
              {
                var MaxRoomInterval = _tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].WaterBucket;
                var toilet_buffer_distance = MaxRoomInterval.Split(THESAURUSHABITAT)[THESAURUSSTAMPEDE];
                var MAX_CONDENSEPIPE_TO_WASHMACHINE = MaxRoomInterval.Split(THESAURUSHABITAT)[THESAURUSHOUSING];
                if (toilet_buffer_distance.Contains(THESAURUSPROLONG) || toilet_buffer_distance.Contains(QUOTATIONSPENSERIAN))
                {
                  var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE;
                  MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(THESAURUSUNDERSTANDING);
                  if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent) MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(maxDownspoutToBalconywashingfloordrain);
                  COMMONRADIUS.Add(new BlockInfo(THESAURUSCURDLE, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSBEHOVE)));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-CONSUMMATIVENESS, UNACCEPTABLENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDEPUTIZE, THESAURUSHEARTLESS)), CIRCUMCONVOLUTION));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDEPUTIZE, THESAURUSHEARTLESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONWORSTED, THESAURUSHEARTLESS)), CIRCUMCONVOLUTION));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONWORSTED, THESAURUSHEARTLESS), toilet_buffer_distance, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSVISIBLE, QUOTATIONETHIOPS), MAX_CONDENSEPIPE_TO_WASHMACHINE, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                  wellToWallOffset.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(-THESAURUSBEHOVE));
                  toiletWellsInterval.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(-THESAURUSBEHOVE + ASSOCIATIONISTS));
                  MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = _raiseDistanceToStartDefault;
                }
                else
                {
                  var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE;
                  if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent) MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(maxDownspoutToBalconywashingfloordrain);
                  COMMONRADIUS.Add(new BlockInfo(THESAURUSPRECOCIOUS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, PHOSPHORYLATION), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMISUNDERSTANDING, THESAURUSINSTITUTE)), CIRCUMCONVOLUTION));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMISUNDERSTANDING, THESAURUSINSTITUTE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINCONCLUSIVE, THESAURUSINSTITUTE)), CIRCUMCONVOLUTION));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINCONCLUSIVE, THESAURUSINSTITUTE), toilet_buffer_distance, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ALSOBIPINNATISECT, CATECHOLAMINERGIC), MAX_CONDENSEPIPE_TO_WASHMACHINE, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                  MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = _raiseDistanceToStartDefault;
                }
                if (eMax + THESAURUSHOUSING == MAX_ANGEL_TOLLERANCE)
                {
                  maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                  if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent)
                  {
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, maxDownspoutToBalconywashingfloordrain)), INSTRUMENTALITY));
                  }
                  if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE - THESAURUSHOUSING].HasLong || _tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE - THESAURUSHOUSING].HasShort)
                  {
                    var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE;
                    MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(-HEIGHT);
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE)), INSTRUMENTALITY));
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE)), INSTRUMENTALITY));
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS)), INSTRUMENTALITY));
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC)), INSTRUMENTALITY));
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), INSTRUMENTALITY));
                    MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = _raiseDistanceToStartDefault;
                  }
                  else
                  {
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -HEIGHT)), INSTRUMENTALITY));
                  }
                  maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                  pipeLineInfos.AddRange(maxBalconywashingmachineToBalconybasinline.GetLineInfos());
                  var _tol_avg_column_dist = maxBalconywashingmachineToBalconybasinline.GetLineVertices().OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).ToList();
                  if (_tol_avg_column_dist.Count > THESAURUSSTAMPEDE) MAX_DEVICE_TO_DEVICE += _tol_avg_column_dist.Last().X - _tol_avg_column_dist.First().X;
                }
              }
              if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].Exists)
              {
                {
                  if (_tolReturnValueRange.PipeType is PipeType.PLTL or PipeType.WLTL)
                  {
                    maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                    void maxDeviceplatformArea()
                    {
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSMOULDER, THESAURUSBEAUTIFY)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION, });
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ACETYLSALICYLIC, THESAURUSEFFULGENT)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ACETYLSALICYLIC, THESAURUSEFFULGENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ACETYLSALICYLIC, THESAURUSINDISPENSABLE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSREPRESSIVE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSREPRESSIVE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSMADNESS)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSMADNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSMADNESS)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSMADNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, CONTRADISTINGUISHED)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, CONTRADISTINGUISHED), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ACETYLSALICYLIC, THESAURUSINDISPENSABLE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSWINDING, THESAURUSBEAUTIFY)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSWINDING, THESAURUSBEAUTIFY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSMOULDER, THESAURUSBEAUTIFY)), THESAURUSCONTROVERSY));
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                      MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, INTERNATIONALLY), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN));
                      MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSDOMESTIC), new(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), CONSECUTIVENESS, QUOTATIONBENJAMIN));
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDISCERNIBLE), new Vector2d(-THESAURUSDOMESTIC, THESAURUSSTAMPEDE) };
                      var _tol_avg_column_dist = LineCoincideTolerance.ToPoint2ds(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.ToPoint2d());
                      MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(_tol_avg_column_dist[THESAURUSHOUSING], _tol_avg_column_dist[THESAURUSPERMUTATION], _tol_avg_column_dist[INTROPUNITIVENESS] - _tol_avg_column_dist[THESAURUSPERMUTATION], INTERNALIZATION, QUOTATIONBENJAMIN));
                    }
                    if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                    {
                      if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasLong)
                      {
                        maxDeviceplatformArea();
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSHALLOW, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSHALLOW, PALAEOICHTHYOLOGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, IMMEASURABLENESS)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, IMMEASURABLENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, ALSOWATERLANDIAN)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, ALSOWATERLANDIAN), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, INTERNATIONALLY)), THUNDERSTRICKEN));
                      }
                      else
                      {
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSDISCERNIBLE)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSDISCERNIBLE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INTERNATIONALLY)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                        MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INTERNATIONALLY), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN));
                        MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC), new(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), CONSECUTIVENESS, QUOTATIONBENJAMIN));
                      }
                    }
                    else if (MAX_ANGEL_TOLLERANCE == eMax)
                    {
                      if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent)
                      {
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, maxDownspoutToBalconywashingfloordrain), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      }
                    }
                    else if (MAX_ANGEL_TOLLERANCE == eMax - THESAURUSHOUSING)
                    {
                      if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasLong)
                      {
                        if (MAX_TOILET_TO_KITCHEN_DISTANCE)
                        {
                          COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSOUTLANDISH)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSCIRCULAR)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSCIRCULAR), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSBUSINESS)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSBUSINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSBUSINESS)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSBUSINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, QUOTATIONSHELLEY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, QUOTATIONSHELLEY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSCOMPLAINT, THESAURUSCONVOY)), THUNDERSTRICKEN));
                        }
                        else
                        {
                          maxDeviceplatformArea();
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ALSOPALIMBACCHIC, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ALSOPALIMBACCHIC, THESAURUSPREFER)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-ALSOPALIMBACCHIC, THESAURUSPREFER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSEQUATION)), THUNDERSTRICKEN));
                        }
                      }
                      else
                      {
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, INTROJECTIONISM), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSEUPHORIA), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, INTROJECTIONISM)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                      }
                    }
                    else
                    {
                      if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasLong)
                      {
                        if (MAX_TOILET_TO_KITCHEN_DISTANCE)
                        {
                          COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSOUTLANDISH)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSCIRCULAR)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSCIRCULAR), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSBUSINESS)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSBUSINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSBUSINESS)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, THESAURUSBUSINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSPRINGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSCONVOY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, THESAURUSREPARATION)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, THESAURUSREPARATION), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSBLESSING, THESAURUSREPARATION)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSBLESSING, THESAURUSREPARATION), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, QUOTATIONSHELLEY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, QUOTATIONSHELLEY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSCONVOY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DOCTRINARIANISM, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSGETAWAY)), THUNDERSTRICKEN));
                        }
                        else
                        {
                          maxDeviceplatformArea();
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINFAMOUS, ANTHROPOPHAGINIAN), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSCOMPLAINT, QUOTATIONNAMAQUA)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLEGALIZE, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLEGALIZE, PALAEOICHTHYOLOGY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINFAMOUS, IMMEASURABLENESS)), THUNDERSTRICKEN));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINFAMOUS, IMMEASURABLENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINFAMOUS, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                        }
                      }
                      else
                      {
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSGETAWAY)), THUNDERSTRICKEN));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                      }
                    }
                    maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                    pipeLineInfos.AddRange(maxBalconywashingmachineToBalconybasinline.GetLineInfos().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.LayerName.Contains(THESAURUSPENNILESS)));
                    {
                      var _tol_avg_column_dist = maxBalconywashingmachineToBalconybasinline.GetLineInfos().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.LayerName.Contains(THESAURUSPENNILESS)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line).YieldPoints().OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).ToList();
                      if (_tol_avg_column_dist.Count > THESAURUSSTAMPEDE) MAX_DEVICE_TO_DEVICE += _tol_avg_column_dist.Last().X - _tol_avg_column_dist.First().X;
                    }
                  }
                  else if (_tolReturnValueRange.PipeType is PipeType.WLTLFL)
                  {
                    maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                    if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                    {
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                    }
                    else if (MAX_ANGEL_TOLLERANCE == eMax)
                    {
                      if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent)
                      {
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, maxDownspoutToBalconywashingfloordrain), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                      }
                    }
                    else if (MAX_ANGEL_TOLLERANCE == eMax - THESAURUSHOUSING)
                    {
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                    }
                    else
                    {
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                    }
                    maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                    pipeLineInfos.AddRange(maxBalconywashingmachineToBalconybasinline.GetLineInfos().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.LayerName.Contains(THESAURUSDEVIANT)));
                  }
                  else
                  {
                    var h = HEIGHT;
                    if (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent && MAX_ANGEL_TOLLERANCE == eMax) h = maxDownspoutToBalconywashingfloordrain;
                    if (MAX_ANGEL_TOLLERANCE != eMax || (MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent && MAX_ANGEL_TOLLERANCE == eMax))
                    {
                      maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                      if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasLong)
                      {
                        if (MAX_ANGEL_TOLLERANCE != sidewaterbucketXIndent)
                        {
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                        }
                      }
                      else if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasShort)
                      {
                        if (MAX_ANGEL_TOLLERANCE != sidewaterbucketXIndent)
                        {
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, COOPERATIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, COOPERATIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, THESAURUSSTAMPEDE)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, ALSOMULTIFLORAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSACQUIESCENT, ALSOMULTIFLORAL)), GetNoteLayer(_tolReturnValueRange.PipeType)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, ALSOMULTIFLORAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHESITANCY, THESAURUSUNOCCUPIED)), GetNoteLayer(_tolReturnValueRange.PipeType)));
                          MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSCELESTIAL), THESAURUSTENACIOUS, GetNoteLayer(_tolReturnValueRange.PipeType), CONTROVERSIALLY));
                        }
                      }
                      else
                      {
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, h), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                      }
                      maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                      pipeLineInfos.AddRange(maxBalconywashingmachineToBalconybasinline.GetLineInfos());
                      var _tol_avg_column_dist = maxBalconywashingmachineToBalconybasinline.GetLineVertices().OrderByDescending(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).ToList();
                      if (_tol_avg_column_dist.Count > THESAURUSSTAMPEDE) MAX_DEVICE_TO_DEVICE += _tol_avg_column_dist.Last().X - _tol_avg_column_dist.First().X;
                    }
                  }
                }
                if (_tolReturnValueRange.PipeType is PipeType.FL && _tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasBasin && MAX_ANGEL_TOLLERANCE != THESAURUSSTAMPEDE && MAX_ANGEL_TOLLERANCE != eMax)
                {
                  maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                  COMMONRADIUS.Add(new BlockInfo(UNACCEPTABILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)) { Scale = THESAURUSPERMUTATION, DynaDict = new() { { THESAURUSENTERPRISE, PERIODONTOCLASIA } } });
                  COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSATTENDANT, PHOTOSYNTHETICALLY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)), THESAURUSADVERSITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, PROMORPHOLOGIST), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, PHOTOSYNTHETICALLY)), THESAURUSADVERSITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, PHOTOSYNTHETICALLY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSATTENDANT, PHOTOSYNTHETICALLY)), THESAURUSADVERSITY));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSATTENDANT, THESAURUSEFFICACY), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                  maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                  var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                  max_balcony_to_balcony_distance.Enqueue((int)LayoutState.Basin, () =>
                  {
                    var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.X).Last();
                    var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-maxBalconybasinToBalcony), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(maxBalconybasinToBalcony)).ToLineString());
                    if (target is Point _raiseDistanceToStartDefault)
                    {
                      var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                      tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    }
                  });
                }
                if (MAX_ANGEL_TOLLERANCE == eMax)
                {
                  if (_tolReturnValueRange.PipeType == PipeType.Y1L)
                  {
                  }
                  else
                  {
                    maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                    var layer = GetPipeLayer(_tolReturnValueRange.PipeType);
                    if (_tolReturnValueRange.PipeType is PipeType.WLTLFL) layer = THUNDERSTRICKEN;
                    var canPeopleBeOnRoof = RainSystemDiagramViewModel.Singleton.Params.CouldHavePeopleOnRoof;
                    COMMONRADIUS.Add(new BlockInfo(THESAURUSNARCOTIC, layer, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, MAX_ANGEL_TOLLERANCE == sidewaterbucketXIndent ? maxDownspoutToBalconywashingfloordrain : THESAURUSSTAMPEDE)) { DynaDict = new() { { QUINQUAGENARIAN, MAX_ANGEL_TOLLERANCE >= sidewaterbucketXIndent ? (canPeopleBeOnRoof ? THESAURUSPARTNER : THESAURUSINEFFECTUAL) : HYPERVENTILATION } } });
                    maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                    var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                    max_balcony_to_balcony_distance.Enqueue((int)LayoutState.FixAirBlock, () =>
                    {
                      var vlineInfos = pipeLineInfos.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.IsVertical(THESAURUSHOUSING)).ToList();
                      if (vlineInfos.Count > THESAURUSSTAMPEDE)
                      {
                        var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = vlineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line).YieldPoints().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).Last();
                        var _raiseDistanceToStartDefault = tolReturnValueMinRange.GetBrBasePoints().First();
                        if (_raiseDistanceToStartDefault.Y < MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.Y)
                        {
                          tolReturnValueMinRange.MoveElements(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN - _raiseDistanceToStartDefault.ToPoint2d());
                        }
                      }
                    });
                  }
                }
                if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].CPSCount == THESAURUSPERMUTATION)
                {
                  maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                  BALCONY_BUFFER_DISTANCE.Add(new CircleInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                  BALCONY_BUFFER_DISTANCE.Add(new CircleInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-INCONSIDERABILIS, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSMAGNETIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, QUOTATIONWITTIG)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISAGREEABLE, QUOTATIONWITTIG)), INSTRUMENTALITY));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, THESAURUSDERELICTION), THESAURUSDISREPUTABLE, THESAURUSINVOICE, CONTROVERSIALLY));
                  maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                  var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                  max_balcony_to_balcony_distance.Enqueue((int)LayoutState.CondensePipe, () =>
                  {
                    var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).First();
                    var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                    if (target is Point _raiseDistanceToStartDefault)
                    {
                      var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                      tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    }
                  });
                }
                if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].CPSCount == THESAURUSHOUSING)
                {
                  maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                  BALCONY_BUFFER_DISTANCE.Add(new CircleInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSMAGNETIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER)), INSTRUMENTALITY));
                  MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, QUOTATIONWITTIG)), INSTRUMENTALITY));
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, THESAURUSGETAWAY), THESAURUSDISREPUTABLE, THESAURUSINVOICE, CONTROVERSIALLY));
                  maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                  var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                  max_balcony_to_balcony_distance.Enqueue((int)LayoutState.CondensePipe, () =>
                  {
                    var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.X).Last();
                    var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                    if (target is Point _raiseDistanceToStartDefault)
                    {
                      var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                      tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    }
                  });
                }
                if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                {
                  max_balcony_to_balcony_distance.Enqueue((int)LayoutState.Outlet, () =>
                  {
                    MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = maxTagXposition(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE).OffsetX(MAX_DEVICE_TO_DEVICE);
                                      if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].WPSCount == THESAURUSHOUSING)
                    {
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSSTRINGENT, THESAURUSDEFAULTER, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, -THESAURUSBELLOW)));
                    }
                    if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].FDSCount == THESAURUSHOUSING)
                    {
                      if (IsRainType(_tolReturnValueRange.PipeType))
                      {
                        MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISAGREEABLE, -THESAURUSCOMPOUND), QUOTATIONBREWSTER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -REACTIONARINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS)), INSTRUMENTALITY));
                        COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSEXCHANGE, -PORTMANTOLOGISM)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                        var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSBELLOW), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINHERIT), new Vector2d(-THESAURUSSCINTILLATE, THESAURUSINHERIT) };
                        var _tol_avg_column_dist = LineCoincideTolerance.ToPoint2ds(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.ToPoint2d());
                        MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(_tol_avg_column_dist[THESAURUSHOUSING], _tol_avg_column_dist[INTROPUNITIVENESS], _tol_avg_column_dist[THESAURUSPERMUTATION] - _tol_avg_column_dist[THESAURUSHOUSING], METACOMMUNICATION, THESAURUSINVOICE));
                      }
                    }
                                      {
                      if (IsDraiType(_tolReturnValueRange.PipeType))
                      {
                        if (_tolReturnValueRange.PipeType is PipeType.WLTLFL)
                        {
                          COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-PROGNOSTICATORY, -THESAURUSHALLUCINATE)));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, -THROMBOEMBOLISM)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, -THROMBOEMBOLISM), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, -THESAURUSSATIATE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(ALSOMEGACEPHALOUS, -THESAURUSSATIATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSSATIATE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, -QUOTATIONQUICHE)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, -QUOTATIONQUICHE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSTRAUMATIC, -STENTOROPHŌNIKOS)), THESAURUSCONTROVERSY));
                          MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSTRAUMATIC, -STENTOROPHŌNIKOS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -STENTOROPHŌNIKOS)), THESAURUSCONTROVERSY));
                          MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSASTUTE, -THESAURUSASSIMILATE), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                          MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-SPECTROFLUORIMETER, -THESAURUSRUINOUS), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                        }
                        else if (_tolReturnValueRange.PipeType is PipeType.TL)
                        {
                        }
                        else
                        {
                          var layer = GetPipeLayer(_tolReturnValueRange.PipeType);
                          if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].HasBasin)
                          {
                            COMMONRADIUS.Add(new BlockInfo(UNACCEPTABILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)) { Scale = THESAURUSPERMUTATION, DynaDict = new() { { THESAURUSENTERPRISE, PERIODONTOCLASIA } } });
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-DIFFERENTIATEDNESS, -THESAURUSIMPRACTICABLE)));
                                              MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSRECONCILE, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSRECONCILE, -POLYOXYMETHYLENE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHOMICIDAL, -POLYOXYMETHYLENE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHOMICIDAL, -POLYOXYMETHYLENE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, -METROPOLITANATE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, -METROPOLITANATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, -THESAURUSHYPNOTIC)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-POLYOXYMETHYLENE, -THESAURUSHYPNOTIC)), layer));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-IMAGINATIVENESS, -THESAURUSLOITER), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-IMAGINATIVENESS, -THESAURUSLAWYER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                          }
                          else if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].FDSCount == THESAURUSPERMUTATION)
                          {
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(SCIENTIFICALNESS, -THESAURUSPRETTY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSISOLATION)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONBUBONIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(SCIENTIFICALNESS, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONBUBONIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONBUBONIC, -THESAURUSISOLATION)), layer));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHEADSTRONG, -THESAURUSCONTEMPT), QUOTATIONDOPPLER, THESAURUSSTRIPED, CONTROVERSIALLY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSNECESSITOUS, -THESAURUSMATTER), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-PROGNOSTICATORY, -THESAURUSIMPRACTICABLE)));
                            COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                            COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(REPRESENTATIONAL, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                          }
                          else if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].FDSCount == THESAURUSHOUSING)
                          {
                            COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-PROGNOSTICATORY, -THESAURUSIMPRACTICABLE)));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPRETTY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(TRIGONOCEPHALIC, -THESAURUSISOLATION)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSABSORBENT, -OLIGOSACCHARIDES), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                          }
                          else if (_tolReturnValueRange.PipeType is PipeType.PLTL or PipeType.WLTL)
                          {
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSRECONCILE, -THESAURUSIMPRACTICABLE)));
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDERELICTION, -THESAURUSHYPNOTIC)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION, });
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE)), THESAURUSCONTROVERSY));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY)), THESAURUSCONTROVERSY));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINTEGRITY, -THESAURUSPRETTY)), THESAURUSCONTROVERSY));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINTEGRITY, -POLYOXYMETHYLENE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISTASTEFUL, -POLYOXYMETHYLENE)), THESAURUSCONTROVERSY));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDISTASTEFUL, -POLYOXYMETHYLENE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDERELICTION, -METROPOLITANATE)), THESAURUSCONTROVERSY));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDERELICTION, -METROPOLITANATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDERELICTION, -THESAURUSHYPNOTIC)), THESAURUSCONTROVERSY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPROGRESSIVE, -THESAURUSLOITER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPROGRESSIVE, -THESAURUSLAWYER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                          }
                          else
                          {
                            COMMONRADIUS.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-PROGNOSTICATORY, -THESAURUSHALLUCINATE)));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                            MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                            MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                          }
                        }
                      }
                      else
                      {
                        COMMONRADIUS.Add(new BlockInfo(THESAURUSSUPERFICIAL, CIRCUMCONVOLUTION, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY)) { PropDict = new() { { THESAURUSSUPERFICIAL, _tolReturnValueRange.WPRadius } } });
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG)), INSTRUMENTALITY));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                        MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                        MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, -INCOMPREHENSIBILIS), IRRESPONSIBLENESS, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                        switch (_tolReturnValueRange.Outlet)
                        {
                          case THESAURUSINTENTIONAL:
                            {
                              COMMONRADIUS.Add(new BlockInfo(THESAURUSGAUCHE, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-PROGNOSTICATORY, -THESAURUSINSINCERE)) { PropDict = new() { { THESAURUSSPECIFICATION, THESAURUSSPECIFICATION } } });
                            }
                            break;
                          case THESAURUSADVENT:
                            {
                              COMMONRADIUS.Add(new BlockInfo(THESAURUSEMPHASIS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)));
                              MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                              MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSUNGRATEFUL, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                              MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSUPPOSITION, -QUOTATION1ASHANKS), CHRISTADELPHIAN, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                            }
                            break;
                          case VICISSITUDINOUS:
                            {
                              MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                              MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSUNGRATEFUL, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                              MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSUPPOSITION, -QUOTATION1ASHANKS), QUOTATIONSECOND, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                            }
                            break;
                          default:
                            break;
                        }
                      }
                    }
                  });
                }
                if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                {
                  if (_tolReturnValueRange.PipeType is not (PipeType.WLTLFL or PipeType.WLTL or PipeType.PLTL))
                  {
                    maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                    COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                    MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, THESAURUSINVOICE));
                    maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                    var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                    max_balcony_to_balcony_distance.Enqueue((int)LayoutState.CheckPoint, () =>
                    {
                      var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetBrBasePoints().First();
                      var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                      if (target is Point _raiseDistanceToStartDefault)
                      {
                        var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint3d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                        tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                      }
                    });
                  }
                }
                if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].FDSCount == THESAURUSPERMUTATION)
                {
                  if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                  {
                  }
                  else
                  {
                    if (IsRainType(_tolReturnValueRange.PipeType))
                    {
                      maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                      COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                      COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), });
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDETERMINED, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDETERMINED, -QUOTATIONPITUITARY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, -THESAURUSGETAWAY), QUOTATIONBREWSTER, THESAURUSINVOICE, CONTROVERSIALLY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(PSEUDEPIGRAPHOS, -THESAURUSMANIFESTATION), QUOTATIONBREWSTER, THESAURUSINVOICE, CONTROVERSIALLY));
                      maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                      var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                      max_balcony_to_balcony_distance.Enqueue((int)LayoutState.FloorDrain, () =>
                      {
                        var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                        var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).First();
                        var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                        if (target is Point _raiseDistanceToStartDefault)
                        {
                          var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                          tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                        }
                      });
                    }
                    else
                    {
                      maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                      COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MAXILLOPALATINE, -THESAURUSINTRENCH)) { ScaleEx = new(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, ACCOMMODATINGLY } }, });
                      COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSATTENDANT, -THESAURUSINTRENCH)) { ScaleEx = new(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, ACCOMMODATINGLY } }, });
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-MAXILLOPALATINE, -THESAURUSINTRENCH), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINDECOROUS, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINDECOROUS, -THESAURUSEXPERIMENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMISAPPREHEND, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-SYNAESTHETICALLY, -THESAURUSEXPERIMENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSATTENDANT, -THESAURUSINTRENCH)), THESAURUSADVERSITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSALLEGIANCE, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSALLEGIANCE, -THESAURUSEXPERIMENT), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSINTRENCH)), THESAURUSADVERSITY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSDESTRUCTION), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-OLIGOMENORRHOEA, -THESAURUSDESTRUCTION), QUOTATIONDOPPLER, THESAURUSSTRIPED, CONTROVERSIALLY));
                      maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                      var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                      max_balcony_to_balcony_distance.Enqueue((int)LayoutState.FloorDrain, () =>
                      {
                        var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                        var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.X).Last();
                        var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                        if (target is Point _raiseDistanceToStartDefault)
                        {
                          var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                          tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                        }
                      });
                    }
                  }
                }
                if (_tolReturnValueRange.Runs[MAX_ANGEL_TOLLERANCE].FDSCount == THESAURUSHOUSING)
                {
                  if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
                  {
                    if (INTRAVASCULARLY)
                    {
                      {
                        var seg1 = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSOBSERVANCE));
                        var seg2 = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSOBSERVANCE));
                        var pt1 = seg1.StartPoint.ToPoint3d();
                        var _tol_lane = pt1.OffsetX(seg2.X1 - seg1.X1);
                        MIN_BALCONYBASIN_TO_BALCONY.Add(new DimInfo(pt1, _tol_lane, new(THESAURUSSTAMPEDE, -seg2.Length, THESAURUSSTAMPEDE), METACOMMUNICATION, THESAURUSINVOICE));
                      }
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSSUPERFICIAL, CIRCUMCONVOLUTION, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY)));
                      COMMONRADIUS.Add(new BlockInfo(THESAURUSSTRINGENT, THESAURUSDEFAULTER, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDICTATORIAL, -THESAURUSBELLOW)));
                      COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSEXCHANGE, -PORTMANTOLOGISM)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMAYHEM, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMAYHEM, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSMAYHEM, -THESAURUSPROSPEROUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-QUOTATIONELECTRICIAN, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG)), INSTRUMENTALITY));
                      MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -REACTIONARINESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS)), INSTRUMENTALITY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSUNGRATEFUL, -QUOTATION1ASHANKS), QUOTATIONSECOND, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, -INCOMPREHENSIBILIS), QUOTATIONDOPPLER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                      MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, -THESAURUSCOMPOUND), QUOTATIONBREWSTER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                    }
                  }
                  else
                  {
                    maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                    COMMONRADIUS.Add(new BlockInfo(PERSUADABLENESS, GetEQPMLayer(_tolReturnValueRange.PipeType), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                    MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), GetPipeLayer(_tolReturnValueRange.PipeType)));
                    MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSSEDATE, -THESAURUSGETAWAY), QUOTATIONBREWSTER, GetNoteLayer(_tolReturnValueRange.PipeType), CONTROVERSIALLY));
                    maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                    var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                    max_balcony_to_balcony_distance.Enqueue((int)LayoutState.FloorDrain, () =>
                    {
                      var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.X).Last();
                      var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                      if (target is Point _raiseDistanceToStartDefault)
                      {
                        var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                        tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                      }
                    });
                  }
                }
              }
            }
            string wellsMaxArea = null, minWellToUrinalDistance = null, maxRoomInterval = null, kitchenBufferDistance = null, maxToiletToKitchenDistance = null, maxToiletToKitchenDistance1 = null;
            if (_tolReturnValueRange.PipeType is PipeType.PLTL)
            {
              var pllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsPL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              var tllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsTL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              if (pllabels.Count == THESAURUSPERMUTATION)
              {
                wellsMaxArea = pllabels[THESAURUSSTAMPEDE];
                minWellToUrinalDistance = pllabels[THESAURUSHOUSING];
              }
              else if (pllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = pllabels.Count / THESAURUSPERMUTATION + pllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = pllabels.Count - tol_blk_max_connect;
                wellsMaxArea = pllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = pllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                wellsMaxArea = pllabels.JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = null;
              }
              if (tllabels.Count == THESAURUSPERMUTATION)
              {
                maxRoomInterval = tllabels[THESAURUSSTAMPEDE];
                kitchenBufferDistance = tllabels[THESAURUSHOUSING];
              }
              else if (tllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = tllabels.Count - tol_blk_max_connect;
                maxRoomInterval = tllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = tllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                maxRoomInterval = tllabels.JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = null;
              }
            }
            else if (_tolReturnValueRange.PipeType is PipeType.WLTL)
            {
              var wllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsWL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              var tllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsTL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              if (wllabels.Count == THESAURUSPERMUTATION)
              {
                wellsMaxArea = wllabels[THESAURUSSTAMPEDE];
                minWellToUrinalDistance = wllabels[THESAURUSHOUSING];
              }
              else if (wllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = wllabels.Count / THESAURUSPERMUTATION + wllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = wllabels.Count - tol_blk_max_connect;
                wellsMaxArea = wllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = wllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                wellsMaxArea = wllabels.JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = null;
              }
              if (tllabels.Count == THESAURUSPERMUTATION)
              {
                maxRoomInterval = tllabels[THESAURUSSTAMPEDE];
                kitchenBufferDistance = tllabels[THESAURUSHOUSING];
              }
              else if (tllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = tllabels.Count - tol_blk_max_connect;
                maxRoomInterval = tllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = tllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                maxRoomInterval = tllabels.JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = null;
              }
            }
            else if (_tolReturnValueRange.PipeType is PipeType.WLTLFL)
            {
              var wllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsWL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              var tllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsTL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              var fllabels = ConvertLabelStrings(max_kitchen_to_balcony_distance.Where(IsDraiFL)).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              if (wllabels.Count == THESAURUSPERMUTATION)
              {
                wellsMaxArea = wllabels[THESAURUSSTAMPEDE];
                minWellToUrinalDistance = wllabels[THESAURUSHOUSING];
              }
              else if (wllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = wllabels.Count / THESAURUSPERMUTATION + wllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = wllabels.Count - tol_blk_max_connect;
                wellsMaxArea = wllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = wllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                wellsMaxArea = wllabels.JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = null;
              }
              if (tllabels.Count == THESAURUSPERMUTATION)
              {
                maxRoomInterval = tllabels[THESAURUSSTAMPEDE];
                kitchenBufferDistance = tllabels[THESAURUSHOUSING];
              }
              else if (tllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = tllabels.Count - tol_blk_max_connect;
                maxRoomInterval = tllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = tllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                maxRoomInterval = tllabels.JoinWith(THESAURUSCAVALIER);
                kitchenBufferDistance = null;
              }
              if (fllabels.Count == THESAURUSPERMUTATION)
              {
                maxToiletToKitchenDistance = fllabels[THESAURUSSTAMPEDE];
                maxToiletToKitchenDistance1 = fllabels[THESAURUSHOUSING];
              }
              else if (fllabels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = fllabels.Count / THESAURUSPERMUTATION + fllabels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = fllabels.Count - tol_blk_max_connect;
                maxToiletToKitchenDistance = fllabels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                maxToiletToKitchenDistance1 = fllabels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                maxToiletToKitchenDistance = fllabels.JoinWith(THESAURUSCAVALIER);
                maxToiletToKitchenDistance1 = null;
              }
            }
            else
            {
              var _labels = ConvertLabelStrings(max_kitchen_to_balcony_distance).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              if (_labels.Count == THESAURUSPERMUTATION)
              {
                wellsMaxArea = _labels[THESAURUSSTAMPEDE];
                minWellToUrinalDistance = _labels[THESAURUSHOUSING];
              }
              else if (_labels.Count > THESAURUSPERMUTATION)
              {
                var tol_blk_max_connect = _labels.Count / THESAURUSPERMUTATION + _labels.Count % THESAURUSPERMUTATION;
                var _tol_order_side_lane_pt_on_frame = _labels.Count - tol_blk_max_connect;
                wellsMaxArea = _labels.Take(tol_blk_max_connect).JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = _labels.Skip(tol_blk_max_connect).Take(_tol_order_side_lane_pt_on_frame).JoinWith(THESAURUSCAVALIER);
              }
              else
              {
                wellsMaxArea = _labels.JoinWith(THESAURUSCAVALIER);
                minWellToUrinalDistance = null;
              }
            }
            var maxKitchenToRainpipeDistance = new HashSet<int>();
            var maxBalconyToRainpipeDistance = new HashSet<int>();
            {
              void maxDeviceplatformArea()
              {
                {
                  var maxBalconyrainpipeToFloordrainDistance = _tolReturnValueRange.Runs.Count(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists && !TolLightRangeSingleSideMax.HasLong);
                  if (maxBalconyrainpipeToFloordrainDistance > SUPERLATIVENESS)
                  {
                    var _tolReturnValueMaxDistance = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists && !TolLightRangeSingleSideMax.HasLong).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Min();
                    var _tolReturnValueMinRange = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists && !TolLightRangeSingleSideMax.HasLong).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Max();
                    maxKitchenToRainpipeDistance.Add(_tolReturnValueMaxDistance + THESAURUSPERMUTATION);
                    maxKitchenToRainpipeDistance.Add(_tolReturnValueMinRange - THESAURUSPERMUTATION);
                    maxBalconyToRainpipeDistance.Add(_tolReturnValueMaxDistance + THESAURUSHOUSING);
                    maxBalconyToRainpipeDistance.Add(_tolReturnValueMinRange - THESAURUSHOUSING);
                    return;
                  }
                }
                {
                  var maxBalconyrainpipeToFloordrainDistance = _tolReturnValueRange.Runs.Count(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists);
                  if (maxBalconyrainpipeToFloordrainDistance > THESAURUSSTAMPEDE)
                  {
                    if (maxBalconyrainpipeToFloordrainDistance == THESAURUSHOUSING)
                    {
                      maxKitchenToRainpipeDistance.Add(_tolReturnValueRange.Runs.First(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists).Index);
                    }
                    else
                    {
                      var _tolReturnValueMaxDistance = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Min();
                      var _tolReturnValueMinRange = _tolReturnValueRange.Runs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Exists).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Index).Max();
                      if (maxBalconyrainpipeToFloordrainDistance <= INTROPUNITIVENESS)
                      {
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMaxDistance);
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMinRange);
                      }
                      else if (maxBalconyrainpipeToFloordrainDistance <= SUPERLATIVENESS)
                      {
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMaxDistance + THESAURUSHOUSING);
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMinRange - THESAURUSHOUSING);
                        maxBalconyToRainpipeDistance.Add(_tolReturnValueMaxDistance);
                        maxBalconyToRainpipeDistance.Add(_tolReturnValueMinRange);
                      }
                      else
                      {
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMaxDistance + THESAURUSPERMUTATION);
                        maxKitchenToRainpipeDistance.Add(_tolReturnValueMinRange - THESAURUSPERMUTATION);
                        maxBalconyToRainpipeDistance.Add(_tolReturnValueMaxDistance + THESAURUSHOUSING);
                        maxBalconyToRainpipeDistance.Add(_tolReturnValueMinRange - THESAURUSHOUSING);
                      }
                    }
                  }
                }
              }
              maxDeviceplatformArea();
            }
            var (_tol_group_evca_emg, _) = GetDBTextSize(wellsMaxArea, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (_tol_light_range_single_side_min, _) = GetDBTextSize(minWellToUrinalDistance, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (w3, _) = GetDBTextSize(maxRoomInterval, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (w4, _) = GetDBTextSize(kitchenBufferDistance, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (w5, _) = GetDBTextSize(maxToiletToKitchenDistance, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (w6, _) = GetDBTextSize(maxToiletToKitchenDistance1, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var wa = Math.Max(_tol_group_evca_emg, _tol_light_range_single_side_min);
            var wb = Math.Max(w3, w4);
            var wc = Math.Max(w5, w6);
            foreach (var MAX_ANGEL_TOLLERANCE in maxBalconyToRainpipeDistance.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax))
            {
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = maxTagXposition(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
            }
            if (maxKitchenToRainpipeDistance.Count >= THESAURUSPERMUTATION)
            {
              if (maxKitchenToRainpipeDistance.Max() == eMax)
              {
                maxKitchenToRainpipeDistance.Remove(maxKitchenToRainpipeDistance.Max());
              }
            }
            foreach (var MAX_ANGEL_TOLLERANCE in maxKitchenToRainpipeDistance.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax))
            {
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = maxTagXposition(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
              var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE;
              if (MAX_ANGEL_TOLLERANCE == THESAURUSSTAMPEDE)
              {
                MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetY(THESAURUSDOMESTIC);
              }
              if (_tolReturnValueRange.PipeType is PipeType.PLTL or PipeType.WLTL)
              {
                maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDOMESTIC, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDOMESTIC, THESAURUSFORMULATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDOMESTIC - wa, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSFORMULATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE + wb, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDOMESTIC - wa, THESAURUSATTACHMENT), wellsMaxArea, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSATTACHMENT), maxRoomInterval, THESAURUSSTRIPED, CONTROVERSIALLY));
                if (minWellToUrinalDistance != null)
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSDOMESTIC - wa, THESAURUSHYPNOTIC), minWellToUrinalDistance, THESAURUSSTRIPED, CONTROVERSIALLY));
                if (kitchenBufferDistance != null)
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSHYPNOTIC), kitchenBufferDistance, THESAURUSSTRIPED, CONTROVERSIALLY));
                maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                max_balcony_to_balcony_distance.Enqueue((int)LayoutState.PipeLabel, () =>
                {
                  var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                  var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).First();
                  var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                  if (target is Point _raiseDistanceToStartDefault)
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                    tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                  }
                });
              }
              else if (_tolReturnValueRange.PipeType is PipeType.WLTLFL)
              {
                maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSHYPNOTIC, MISAPPREHENSIVE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSDERELICTION)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, THESAURUSDERELICTION), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE + wc, THESAURUSDERELICTION)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFORMULATE, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFORMULATE, THESAURUSFORMULATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFORMULATE - wa, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, POLYOXYMETHYLENE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, REPRESENTATIONAL)), THESAURUSSTRIPED));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, REPRESENTATIONAL), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC + wb, REPRESENTATIONAL)), THESAURUSSTRIPED));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFORMULATE - wa, THESAURUSATTACHMENT), wellsMaxArea, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(-THESAURUSFORMULATE - wa, THESAURUSENDANGER), minWellToUrinalDistance, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSCAPITALISM), maxRoomInterval, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, QUOTATIONCOLERIDGE), kitchenBufferDistance, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, CONSCRIPTIONIST), maxToiletToKitchenDistance, THESAURUSSTRIPED, CONTROVERSIALLY));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSFORMULATE, PHYSIOLOGICALLY), maxToiletToKitchenDistance1, THESAURUSSTRIPED, CONTROVERSIALLY));
                maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                max_balcony_to_balcony_distance.Enqueue((int)LayoutState.PipeLabel, () =>
                {
                  var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                  var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).First();
                  var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                  if (target is Point _raiseDistanceToStartDefault)
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                    tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                  }
                });
              }
              else
              {
                maxBalconywashingmachineToBalconybasinline.TakeStartSnap();
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSFORMULATE)), GetNoteLayer(_tolReturnValueRange.PipeType)));
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSFORMULATE), MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC + wa, THESAURUSFORMULATE)), GetNoteLayer(_tolReturnValueRange.PipeType)));
                MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSATTACHMENT), wellsMaxArea, GetNoteLayer(_tolReturnValueRange.PipeType), CONTROVERSIALLY));
                if (minWellToUrinalDistance != null)
                {
                  MAX_RAINPIPE_TO_WASHMACHINE.Add(new DBTextInfo(MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE.OffsetXY(THESAURUSDOMESTIC, THESAURUSENDANGER), minWellToUrinalDistance, GetNoteLayer(_tolReturnValueRange.PipeType), CONTROVERSIALLY));
                }
                maxBalconywashingmachineToBalconybasinline.TakeStopSnap();
                var tolReturnValueMinRange = maxBalconywashingmachineToBalconybasinline.GetSnapshot();
                max_balcony_to_balcony_distance.Enqueue((int)LayoutState.PipeLabel, () =>
                {
                  var TolUniformSideLenth = GeoFac.CreateGeometry(pipeLineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToArray());
                  var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueMinRange.GetLineVertices().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Y).First();
                  var target = TolUniformSideLenth.Intersection(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-balconyBufferDistance), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(balconyBufferDistance)).ToLineString());
                  if (target is Point _raiseDistanceToStartDefault)
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _raiseDistanceToStartDefault.ToPoint2d() - MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                    tolReturnValueMinRange.MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                  }
                });
              }
              MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = _raiseDistanceToStartDefault;
            }
            max_balcony_to_balcony_distance.Enqueue((int)LayoutState.FixPipeVLines, () =>
            {
              if (wellToWallOffset.Count + toiletWellsInterval.Count + toiletBufferDistance.Count == THESAURUSSTAMPEDE) return;
              if (pipeLineInfos.Count == THESAURUSSTAMPEDE) return;
              var layer = pipeLineInfos.First().LayerName;
              var vlineInfos = pipeLineInfos.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.IsVertical(THESAURUSHOUSING)).ToList();
              if (vlineInfos.Count == THESAURUSSTAMPEDE) return;
              foreach (var TolLightRangeSingleSideMin in vlineInfos)
              {
                MAX_BALCONYBASIN_TO_BALCONY.Remove(TolLightRangeSingleSideMin);
              }
              var vlines = vlineInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Line.ToLineString()).ToList();
              if (wellToWallOffset.Count > THESAURUSSTAMPEDE && toiletWellsInterval.Count > THESAURUSSTAMPEDE)
              {
                            var kill = GeoFac.CreateGeometryEx(toiletWellsInterval.Distinct().Select(TolLightRangeSingleSideMax => GRect.Create(TolLightRangeSingleSideMax.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL).ToPolygon()).ToList());
                var tolReturnValueRange = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(wellToWallOffset.Distinct().Select(TolLightRangeSingleSideMax => GRect.Create(TolLightRangeSingleSideMax.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL).ToPolygon()).ToList()));
                vlines = vlines.Except(tolReturnValueRange).ToList();
                tolReturnValueRange.AddRange(wellToWallOffset.Distinct().Select(TolLightRangeSingleSideMax => GRect.Create(TolLightRangeSingleSideMax.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL)).Select(DEFAULT_FIRE_VALVE_WIDTH => new GLineSegment(DEFAULT_FIRE_VALVE_WIDTH.LeftTop, DEFAULT_FIRE_VALVE_WIDTH.RightButtom).ToLineString()));
                var MinWellToUrinalDistance = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(tolReturnValueRange.ToArray())).ToList()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSHOUSING).ToList();
                vlines.AddRange(MinWellToUrinalDistance.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).Where(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.Intersects(kill)));
              }
              if (toiletBufferDistance.Count > THESAURUSSTAMPEDE)
              {
                vlines = GeoFac.GetLines(new MultiLineString(vlines.ToArray()).Difference(GeoFac.CreateGeometryEx(toiletBufferDistance.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList()))).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
              }
              foreach (var TolUniformSideLenth in GeoFac.GetManyLines(vlines))
              {
                MAX_BALCONYBASIN_TO_BALCONY.Add(new LineInfo(TolUniformSideLenth, layer));
              }
            });
          }
          max_balcony_to_balcony_distance.Enqueue((int)LayoutState.Finished, () =>
          {
            foreach (var TolLightRangeSingleSideMin in MAX_BALCONYBASIN_TO_BALCONY)
            {
              var TolUniformSideLenth = DrawLineSegmentLazy(TolLightRangeSingleSideMin.Line);
              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.LayerName))
              {
                TolUniformSideLenth.Layer = TolLightRangeSingleSideMin.LayerName;
              }
              ByLayer(TolUniformSideLenth);
            }
            foreach (var TolLightRangeSingleSideMin in BALCONY_BUFFER_DISTANCE)
            {
              var maxBalconyrainpipeToFloordrainDistance = DrawCircleLazy(TolLightRangeSingleSideMin.Circle.Center, TolLightRangeSingleSideMin.Circle.Radius);
              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.LayerName)) maxBalconyrainpipeToFloordrainDistance.Layer = TolLightRangeSingleSideMin.LayerName;
              ByLayer(maxBalconyrainpipeToFloordrainDistance);
            }
            foreach (var TolLightRangeSingleSideMin in MAX_RAINPIPE_TO_WASHMACHINE)
            {
              var maxBalconyToDeviceplatformDistance = DrawTextLazy(TolLightRangeSingleSideMin.Text, TolLightRangeSingleSideMin.BasePoint.ToPoint2d());
              maxBalconyToDeviceplatformDistance.Rotation = TolLightRangeSingleSideMin.Rotation;
              maxBalconyToDeviceplatformDistance.WidthFactor = THESAURUSDISPASSIONATE;
              maxBalconyToDeviceplatformDistance.Height = THESAURUSENDANGER;
              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.LayerName)) maxBalconyToDeviceplatformDistance.Layer = TolLightRangeSingleSideMin.LayerName;
              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.TextStyle)) DrawingQueue.Enqueue(adb => { SetTextStyle(maxBalconyToDeviceplatformDistance, TolLightRangeSingleSideMin.TextStyle); });
              ByLayer(maxBalconyToDeviceplatformDistance);
            }
            foreach (var TolLightRangeSingleSideMin in COMMONRADIUS)
            {
              DrawBlockReference(TolLightRangeSingleSideMin.BlockName, TolLightRangeSingleSideMin.BasePoint, layer: TolLightRangeSingleSideMin.LayerName, cb: tolReturnValueRangeTo =>
                        {
                    ByLayer(tolReturnValueRangeTo);
                    if (TolLightRangeSingleSideMin.ScaleEx.HasValue) tolReturnValueRangeTo.ScaleFactors = TolLightRangeSingleSideMin.ScaleEx.Value;
                              {
                      if (TolLightRangeSingleSideMin.DynaDict != null && tolReturnValueRangeTo.IsDynamicBlock) tolReturnValueRangeTo.DynamicBlockReferencePropertyCollection.Cast<DynamicBlockReferenceProperty>().Where(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.ReadOnly).Join(TolLightRangeSingleSideMin.DynaDict, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.PropertyName, y => y.Key, (TolLightRangeSingleSideMax, y) => TolLightRangeSingleSideMax.Value = y.Value).Count();
                    }
                  }, props: TolLightRangeSingleSideMin.PropDict, scale: TolLightRangeSingleSideMin.Scale, rotateDegree: TolLightRangeSingleSideMin.Rotate.AngleToDegree());
            }
            foreach (var TolLightRangeSingleSideMin in MIN_BALCONYBASIN_TO_BALCONY)
            {
              var dim = new AlignedDimension
              {
                XLine1Point = TolLightRangeSingleSideMin.Point1,
                XLine2Point = TolLightRangeSingleSideMin.Point2,
                DimLinePoint = GeoAlgorithm.MidPoint(TolLightRangeSingleSideMin.Point1, TolLightRangeSingleSideMin.Point2) + TolLightRangeSingleSideMin.Vector,
                DimensionText = TolLightRangeSingleSideMin.Text,
                Layer = TolLightRangeSingleSideMin.Layer
              };
              ByLayer(dim);
              DrawEntityLazy(dim);
            }
          });
        }
        FlushDQ(adb);
        void handleEntity(Entity maxToiletToCondensepipeDistance, Matrix3d MAX_RAINPIPE_TO_BALCONYFLOORDRAIN, List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance)
        {
          if (!IsLayerVisible(maxToiletToCondensepipeDistance)) return;
          var maxToiletToFloordrainDistance2 = maxToiletToCondensepipeDistance.GetRXClass().DxfName.ToUpper();
          var TOILET_BUFFER_DISTANCE = maxToiletToCondensepipeDistance.Layer;
          TOILET_BUFFER_DISTANCE = GetEffectiveLayer(TOILET_BUFFER_DISTANCE);
          static bool isDLineLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && !layer.Contains(THESAURUSDEVIANT);
          static bool isVentLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && layer.Contains(THESAURUSDEVIANT);
          if (!maxToiletToFloordrainDistance2.StartsWith(QUOTATIONCHROMIC))
          {
            var bdx = maxToiletToCondensepipeDistance.Bounds;
            if (bdx.HasValue)
            {
              var maxKitchenToBalconyDistance = bdx.Value;
              maxKitchenToBalconyDistance.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              if (!max_balconywashingfloordrain_to_rainpipe(maxKitchenToBalconyDistance.ToEnvelope())) return;
            }
          }
          {
            if (TOILET_BUFFER_DISTANCE is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
            {
              if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth)
              {
                if (TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
                {
                  var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                }
                return;
              }
              else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
              {
                foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
                {
                  if (maxBalconyToBalconyDistance.Length > THESAURUSSTAMPEDE)
                  {
                    var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  }
                }
                return;
              }
              else if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance)
              {
                var repeated_point_distance = maxBalconyToDeviceplatformDistance.TextString;
                if (!string.IsNullOrWhiteSpace(repeated_point_distance))
                {
                  var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  var ct = new CText() { Text = repeated_point_distance, Boundary = maxKitchenToBalconyDistance };
                }
                return;
              }
            }
          }
          {
            if (TOILET_BUFFER_DISTANCE is THESAURUSINVOICE)
            {
              if (maxToiletToCondensepipeDistance is Spline)
              {
                var maxKitchenToBalconyDistance = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE);
                return;
              }
            }
          }
          {
            if (maxToiletToFloordrainDistance2 == QUOTATIONSWALLOW && TOILET_BUFFER_DISTANCE is THESAURUSINVOICE)
            {
              var DEFAULT_FIRE_VALVE_WIDTH = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE);
            }
          }
          {
            if (maxToiletToCondensepipeDistance is Circle maxBalconyrainpipeToFloordrainDistance && isDrainageLayer(TOILET_BUFFER_DISTANCE))
            {
              if (THESAURUSSTAMPEDE < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= HYPERDISYLLABLE)
              {
                var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_TOILET_TO_FLOORDRAIN_DISTANCE);
                return;
              }
            }
          }
          {
            if (maxToiletToCondensepipeDistance is Circle maxBalconyrainpipeToFloordrainDistance)
            {
              if (THESAURUSSTAMPEDE < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= HYPERDISYLLABLE)
              {
                if (isDrainageLayer(maxBalconyrainpipeToFloordrainDistance.Layer))
                {
                  var DEFAULT_FIRE_VALVE_WIDTH = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, MAX_TOILET_TO_FLOORDRAIN_DISTANCE);
                  return;
                }
              }
            }
          }
          if (TOILET_BUFFER_DISTANCE is INSTRUMENTALITY)
          {
            if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_CONDENSEPIPE_DISTANCE);
              return;
            }
            else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
            {
              foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
              {
                var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_CONDENSEPIPE_DISTANCE);
              }
              return;
            }
            if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
            {
              dynamic o = maxToiletToCondensepipeDistance;
              var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_CONDENSEPIPE_DISTANCE);
              return;
            }
          }
          if (isDLineLayer(TOILET_BUFFER_DISTANCE))
          {
            if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_KITCHEN_TO_RAINPIPE_DISTANCE);
              return;
            }
            else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
            {
              foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
              {
                var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_KITCHEN_TO_RAINPIPE_DISTANCE);
              }
              return;
            }
            if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
            {
              dynamic o = maxToiletToCondensepipeDistance;
              var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_KITCHEN_TO_RAINPIPE_DISTANCE);
              return;
            }
          }
          if (isVentLayer(TOILET_BUFFER_DISTANCE))
          {
            if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_BALCONY_TO_RAINPIPE_DISTANCE);
              return;
            }
            else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
            {
              foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
              {
                var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_BALCONY_TO_RAINPIPE_DISTANCE);
              }
              return;
            }
            if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
            {
              dynamic o = maxToiletToCondensepipeDistance;
              var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_BALCONY_TO_RAINPIPE_DISTANCE);
              return;
            }
          }
          if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
          {
            if (TOILET_BUFFER_DISTANCE is THESAURUSSINCERE || TOILET_BUFFER_DISTANCE.Contains(SEROEPIDEMIOLOGY))
            {
              foreach (var maxBalconyrainpipeToFloordrainDistance in maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
              {
                if (maxBalconyrainpipeToFloordrainDistance.Radius > THESAURUSSTAMPEDE && isDrainageLayer(maxBalconyrainpipeToFloordrainDistance.Layer))
                {
                  var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_TOILET_TO_FLOORDRAIN_DISTANCE);
                }
              }
            }
          }
          {
            if (isDrainageLayer(TOILET_BUFFER_DISTANCE) && maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_KITCHEN_DISTANCE1);
              return;
            }
          }
          if (maxToiletToFloordrainDistance2 == THESAURUSWINDFALL)
          {
            dynamic o = maxToiletToCondensepipeDistance.AcadObject;
            var repeated_point_distance = (string)o.DimStyleText + THESAURUSSPECIFICATION + (string)o.VPipeNum;
            var colle = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
            var ts = new List<DBText>();
            foreach (var tolReturnValue0Approx in colle.OfType<Entity>().Where(IsLayerVisible))
            {
              if (tolReturnValue0Approx is Line TolUniformSideLenth && isDrainageLayer(TolUniformSideLenth.Layer))
              {
                if (TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
                {
                  var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_KITCHEN_DISTANCE1);
                  continue;
                }
              }
              else if (tolReturnValue0Approx.GetRXClass().DxfName.ToUpper() == THESAURUSDURESS)
              {
                ts.AddRange(tolReturnValue0Approx.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                continue;
              }
            }
            if (ts.Count > THESAURUSSTAMPEDE)
            {
              GRect maxKitchenToBalconyDistance;
              if (ts.Count == THESAURUSHOUSING) maxKitchenToBalconyDistance = ts[THESAURUSSTAMPEDE].Bounds.ToGRect();
              else
              {
                maxKitchenToBalconyDistance = GeoFac.CreateGeometry(ts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Bounds.ToGRect()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon())).EnvelopeInternal.ToGRect();
              }
              maxKitchenToBalconyDistance = maxKitchenToBalconyDistance.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              var ct = new CText() { Text = repeated_point_distance, Boundary = maxKitchenToBalconyDistance };
              _tol_lane_protect(maxToiletToFloordrainDistance, ct, MAX_BALCONY_TO_BALCONY_DISTANCE);
            }
            return;
          }
          {
            static bool default_fire_valve_width(string TolLightRangeMin) => !TolLightRangeMin.StartsWith(THESAURUSNOTATION) && !TolLightRangeMin.ToLower().Contains(PERPENDICULARITY) && !TolLightRangeMin.ToUpper().Contains(THESAURUSIMPOSTER);
            if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance && isDrainageLayer(TOILET_BUFFER_DISTANCE) && default_fire_valve_width(maxBalconyToDeviceplatformDistance.TextString))
            {
              var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              var ct = new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxKitchenToBalconyDistance };
              _tol_lane_protect(maxToiletToFloordrainDistance, ct, MAX_BALCONY_TO_BALCONY_DISTANCE);
              return;
            }
          }
          if (maxToiletToFloordrainDistance2 == THESAURUSDURESS)
          {
            dynamic o = maxToiletToCondensepipeDistance.AcadObject;
            string repeated_point_distance = o.Text;
            if (!string.IsNullOrWhiteSpace(repeated_point_distance))
            {
              var ct = new CText() { Text = repeated_point_distance, Boundary = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN) };
              _tol_lane_protect(maxToiletToFloordrainDistance, ct, MAX_BALCONY_TO_BALCONY_DISTANCE);
            }
            return;
          }
          if (maxToiletToFloordrainDistance2 == THESAURUSINHARMONIOUS)
          {
            {
              dynamic o = maxToiletToCondensepipeDistance.AcadObject;
              string UpText = o.UpText;
              string DownText = o.DownText;
              var MAX_TAG_YPOSITION = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
              var TolLane = MAX_TAG_YPOSITION.OfType<Line>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGLineSegment()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
              var points = GeoFac.GetAlivePoints(TolLane, THESAURUSHOUSING);
              var _tol_avg_column_dist = points.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
              points = points.Except(GeoFac.CreateIntersectsSelector(_tol_avg_column_dist)(GeoFac.CreateGeometryEx(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSHOUSING)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(_tol_avg_column_dist).ToList(points)).ToList();
              if (TOILET_BUFFER_DISTANCE is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
              {
                if (points.Count == THESAURUSSTAMPEDE) return;
                string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = null;
                if (!string.IsNullOrWhiteSpace(UpText) && !string.IsNullOrWhiteSpace(DownText))
                {
                  MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = UpText + INTELLECTUALNESS + DownText;
                }
                else
                {
                  MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = UpText + DownText;
                }
                if (string.IsNullOrWhiteSpace(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return;
                MAX_BASECIRCLE_AREA.Add(GeoFac.CreateGeometry(points.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint())).Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                return;
              }
              else if (isDrainageLayer(TOILET_BUFFER_DISTANCE))
              {
                if (points.Count == THESAURUSSTAMPEDE) return;
                string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = null;
                if (!string.IsNullOrWhiteSpace(UpText) && !string.IsNullOrWhiteSpace(DownText))
                {
                  MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = UpText + INTELLECTUALNESS + DownText;
                }
                else
                {
                  MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = UpText + DownText;
                }
                if (string.IsNullOrWhiteSpace(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return;
                MAX_DEVICEPLATFORM_AREA.Add(GeoFac.CreateGeometry(points.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint())).Tag(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                return;
              }
              if (MAX_TOILET_TO_KITCHEN_DISTANCE)
              {
                var colle = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
                {
                  foreach (var tolReturnValue0Approx in colle.OfType<Entity>().Where(tolReturnValue0Approx => tolReturnValue0Approx.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE).Where(TolLightRangeSingleSideMax => isDrainageLayer(TolLightRangeSingleSideMax.Layer)).Where(IsLayerVisible))
                  {
                    foreach (var maxBalconyToDeviceplatformDistance in tolReturnValue0Approx.ExplodeToDBObjectCollection().OfType<DBText>().Where(TolLightRangeSingleSideMax => !string.IsNullOrWhiteSpace(TolLightRangeSingleSideMax.TextString)).Where(IsLayerVisible))
                    {
                      var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                      var ct = new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxKitchenToBalconyDistance };
                      if (IsDrainageLabel(ct.Text)) _tol_lane_protect(maxToiletToFloordrainDistance, ct, MAX_BALCONY_TO_BALCONY_DISTANCE);
                    }
                  }
                  foreach (var default_fire_valve_length in colle.OfType<Line>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSSTAMPEDE).Where(TolLightRangeSingleSideMax => isDrainageLayer(TolLightRangeSingleSideMax.Layer)).Where(IsLayerVisible).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN)))
                  {
                    _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_KITCHEN_DISTANCE1);
                  }
                }
              }
              return;
            }
            return;
          }
          if (isDrainageLayer(TOILET_BUFFER_DISTANCE))
          {
            {
              if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth)
              {
                var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_KITCHEN_DISTANCE1);
                return;
              }
            }
            {
              if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
                {
                  foreach (var TolUniformSideLenth in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSSTAMPEDE))
                  {
                    var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                    _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, MAX_TOILET_TO_KITCHEN_DISTANCE1);
                  }
                  return;
                }
            }
          }
        }
        void handleBlockReference(BlockReference tolReturnValueRangeTo, Matrix3d MAX_RAINPIPE_TO_BALCONYFLOORDRAIN, List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance)
        {
          if (!tolReturnValueRangeTo.ObjectId.IsValid || !tolReturnValueRangeTo.BlockTableRecord.IsValid) return;
          if (!tolReturnValueRangeTo.Visible) return;
          if (IsLayerVisible(tolReturnValueRangeTo))
          {
            var _name = tolReturnValueRangeTo.GetEffectiveName() ?? THESAURUSDEPLORE;
            var toilet_buffer_distance = GetEffectiveBRName(_name);
            {
              if (isDrainageLayer(tolReturnValueRangeTo.Layer))
                if (toilet_buffer_distance.Contains(INTELLECTUALISTS))
                {
                  var center = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                  if (toilet_buffer_distance is SUPERINDUCEMENT && tolReturnValueRangeTo.IsDynamicBlock)
                  {
                    var props = tolReturnValueRangeTo.DynamicBlockReferencePropertyCollection;
                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                      if (prop.PropertyName == QUINQUAGENARIAN)
                      {
                        var propValue = prop.Value.ToString();
                        if (!string.IsNullOrEmpty(propValue))
                        {
                          max_balconywashingfloordrain_to_balconyfloordrain.Add(center.ToNTSPoint().Tag(propValue));
                        }
                        break;
                      }
                    }
                  }
                  var maxKitchenToBalconyDistance = center.ToGRect(THESAURUSENTREPRENEUR);
                  _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_TOILET_TO_FLOORDRAIN_DISTANCE);
                  return;
                }
            }
            if (toilet_buffer_distance.Contains(THESAURUSTHOROUGHBRED))
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              if (maxKitchenToBalconyDistance.IsValid)
              {
                if (maxKitchenToBalconyDistance.Width < POLYOXYMETHYLENE && maxKitchenToBalconyDistance.Height < POLYOXYMETHYLENE)
                {
                  _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_TOILET_TO_FLOORDRAIN_DISTANCE1);
                }
              }
              return;
            }
            if (toilet_buffer_distance.Contains(THESAURUSINDULGENT))
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              if (maxKitchenToBalconyDistance.IsValid)
              {
                if (maxKitchenToBalconyDistance.Width < POLYOXYMETHYLENE && maxKitchenToBalconyDistance.Height < POLYOXYMETHYLENE)
                {
                  _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MAX_KITCHEN_TO_BALCONY_DISTANCE);
                }
              }
              return;
            }
            if (toilet_buffer_distance.Contains(QUOTATIONBITTER))
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              if (maxKitchenToBalconyDistance.IsValid)
              {
                if (maxKitchenToBalconyDistance.Width < POLYOXYMETHYLENE && maxKitchenToBalconyDistance.Height < POLYOXYMETHYLENE)
                {
                  _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, MIN_DEVICEPLATFORM_AREA);
                }
              }
              return;
            }
          }
          var minDeviceplatformArea = adb.Element<BlockTableRecord>(tolReturnValueRangeTo.BlockTableRecord);
          if (!IsWantedBlock(minDeviceplatformArea)) return;
          var _fs = new List<KeyValuePair<Geometry, Action>>();
          foreach (var objId in minDeviceplatformArea)
          {
            var dbObj = adb.Element<Entity>(objId);
            if (dbObj is BlockReference b)
            {
              handleBlockReference(b, tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN), _fs);
            }
            else
            {
              handleEntity(dbObj, tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN), _fs);
            }
          }
          {
            var MinWellToUrinalDistance = new List<KeyValuePair<Geometry, Action>>();
            var TolLightRangeSingleSideMin = tolReturnValueRangeTo.XClipInfo();
            if (TolLightRangeSingleSideMin.IsValid)
            {
              TolLightRangeSingleSideMin.TransformBy(tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN));
              var default_voltage = TolLightRangeSingleSideMin.PreparedPolygon;
              foreach (var max_rainpipe_to_balconyfloordrain in _fs)
              {
                if (default_voltage.Intersects(max_rainpipe_to_balconyfloordrain.Key))
                {
                  MinWellToUrinalDistance.Add(max_rainpipe_to_balconyfloordrain);
                }
              }
            }
            else
            {
              foreach (var max_rainpipe_to_balconyfloordrain in _fs)
              {
                MinWellToUrinalDistance.Add(max_rainpipe_to_balconyfloordrain);
              }
            }
            maxToiletToFloordrainDistance.AddRange(MinWellToUrinalDistance);
          }
        }
      }
      return THESAURUSOBSTINACY;
    }
    public static Point3dCollection TOILET_WELLS_INTERVAL;
    public static Polygon ConvertToPolygon(Polyline maxToiletToFloordrainDistance1)
    {
      if (maxToiletToFloordrainDistance1.NumberOfVertices <= THESAURUSPERMUTATION)
        return null;
      var list = new List<Point2d>();
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < maxToiletToFloordrainDistance1.NumberOfVertices; MAX_ANGEL_TOLLERANCE++)
      {
        var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = maxToiletToFloordrainDistance1.GetPoint2dAt(MAX_ANGEL_TOLLERANCE);
        if (list.Count == THESAURUSSTAMPEDE || !Equals(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, list.Last()))
        {
          list.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
        }
      }
      if (list.Count <= THESAURUSPERMUTATION) return null;
      try
      {
        var tmp = list.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSCoordinate()).ToList(list.Count + THESAURUSHOUSING);
        if (!tmp[THESAURUSSTAMPEDE].Equals(tmp[tmp.Count - THESAURUSHOUSING]))
        {
          tmp.Add(tmp[THESAURUSSTAMPEDE]);
        }
        var ring = new LinearRing(tmp.ToArray());
        return new Polygon(ring);
      }
      catch (System.Exception ex)
      {
        return null;
      }
    }
    public static string TryParseWrappingPipeRadiusText(string repeated_point_distance)
    {
      if (repeated_point_distance == null) return null;
      var TolLightRangeMin = Regex.Replace(repeated_point_distance, UREDINIOMYCETES, THESAURUSDEPLORE, RegexOptions.IgnoreCase);
      TolLightRangeMin = Regex.Replace(TolLightRangeMin, QUOTATION3BABOVE, THESAURUSDEPLORE);
      TolLightRangeMin = Regex.Replace(TolLightRangeMin, THESAURUSMISTRUST, THESAURUSSPECIFICATION);
      return TolLightRangeMin;
    }
    public static StoreyType GetStoreyType(string MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN)
    {
      return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN switch
      {
        ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR => StoreyType.SmallRoof,
        ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR => StoreyType.LargeRoof,
        ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR => StoreyType.StandardStorey,
        ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR => StoreyType.NonStandardStorey,
        ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR => StoreyType.NonStandardStorey,
        _ => StoreyType.Unknown,
      };
    }
    public static List<int> ParseFloorNums(string floorStr)
    {
      if (string.IsNullOrWhiteSpace(floorStr)) return new List<int>();
      floorStr = floorStr.Replace(THESAURUSMETROPOLIS, THESAURUSPROMINENT).Replace(CHROMATOGRAPHER, NATIONALDEMOKRATISCHE).Replace(THESAURUSASPIRATION, THESAURUSDEPLORE).Replace(STEREOPHOTOGRAMMETRY, THESAURUSDEPLORE).Replace(THESAURUSPOLISH, THESAURUSDEPLORE);
      var defaultFireValveLength = new HashSet<int>();
      foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in floorStr.Split(THESAURUSPROMINENT))
      {
        if (string.IsNullOrEmpty(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN)) continue;
        var tolReturnValueMinRange = Regex.Match(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN, THESAURUSAGITATION);
        if (tolReturnValueMinRange.Success)
        {
          var _tolReturnValue0Approx = int.Parse(tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value);
          var TolGroupEvcaEmg = int.Parse(tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value);
          var _tolReturnValueMaxDistance = Math.Min(_tolReturnValue0Approx, TolGroupEvcaEmg);
          var _tolReturnValueMinRange = Math.Max(_tolReturnValue0Approx, TolGroupEvcaEmg);
          for (int MAX_ANGEL_TOLLERANCE = _tolReturnValueMaxDistance; MAX_ANGEL_TOLLERANCE <= _tolReturnValueMinRange; MAX_ANGEL_TOLLERANCE++)
          {
            defaultFireValveLength.Add(MAX_ANGEL_TOLLERANCE);
          }
          continue;
        }
        tolReturnValueMinRange = Regex.Match(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN, THESAURUSSANITY);
        if (tolReturnValueMinRange.Success)
        {
          defaultFireValveLength.Add(int.Parse(tolReturnValueMinRange.Value));
        }
      }
      defaultFireValveLength.Remove(THESAURUSSTAMPEDE);
      return defaultFireValveLength.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
    }
    public static bool IsNumStorey(string tolGroupBlkLane)
    {
      return GetStoreyScore(tolGroupBlkLane) < ushort.MaxValue;
    }
    public static int GetStoreyScore(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return THESAURUSSTAMPEDE;
      switch (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        case THESAURUSARGUMENTATIVE: return ushort.MaxValue;
        case ANTHROPOMORPHICALLY: return ushort.MaxValue + THESAURUSHOUSING;
        case THESAURUSSCUFFLE: return ushort.MaxValue + THESAURUSPERMUTATION;
        default:
          {
            int.TryParse(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Replace(THESAURUSASPIRATION, THESAURUSDEPLORE), out int tolGroupEmgLightEvac);
            return tolGroupEmgLightEvac;
          }
      }
    }
    public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
    {
      pipeIds = pipeIds.Distinct().ToList();
      {
        var max_kitchen_to_balcony_distance = pipeIds.Where(TolLightRangeSingleSideMax => Regex.IsMatch(TolLightRangeSingleSideMax, THESAURUSJAILER)).ToList();
        pipeIds = pipeIds.Except(max_kitchen_to_balcony_distance).ToList();
        foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in ConvertLabelString(max_kitchen_to_balcony_distance))
        {
          yield return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        }
        static IEnumerable<string> ConvertLabelString(IEnumerable<string> strs)
        {
          var kvs = new List<KeyValuePair<string, string>>();
          foreach (var WELLS_MAX_AREA in strs)
          {
            var tolReturnValueMinRange = Regex.Match(WELLS_MAX_AREA, THESAURUSJAILER);
            if (tolReturnValueMinRange.Success)
            {
              kvs.Add(new KeyValuePair<string, string>(tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value, tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value));
            }
            else
            {
              throw new System.Exception();
            }
          }
          return kvs.GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key + THESAURUSSPECIFICATION + string.Join(DEMATERIALISING, GetLabelString(TolLightRangeSingleSideMax.Select(y => y.Value[THESAURUSSTAMPEDE]))));
        }
        static IEnumerable<string> GetLabelString(IEnumerable<char> chars)
        {
          foreach (var max_rainpipe_to_balconyfloordrain in GetPairs(chars.Select(TolLightRangeSingleSideMax => (int)TolLightRangeSingleSideMax).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax)))
          {
            if (max_rainpipe_to_balconyfloordrain.Key == max_rainpipe_to_balconyfloordrain.Value)
            {
              yield return Convert.ToChar(max_rainpipe_to_balconyfloordrain.Key).ToString();
            }
            else
            {
              yield return Convert.ToChar(max_rainpipe_to_balconyfloordrain.Key) + THESAURUSEXCREMENT + Convert.ToChar(max_rainpipe_to_balconyfloordrain.Value);
            }
          }
        }
      }
      {
        var max_kitchen_to_balcony_distance = pipeIds.Where(TolLightRangeSingleSideMax => Regex.IsMatch(TolLightRangeSingleSideMax, THESAURUSCAPRICIOUS)).ToList();
        pipeIds = pipeIds.Except(max_kitchen_to_balcony_distance).ToList();
        foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in ConvertLabelString(max_kitchen_to_balcony_distance))
        {
          yield return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        }
        static IEnumerable<string> ConvertLabelString(IEnumerable<string> strs)
        {
          var kvs = new List<ValueTuple<string, string, int>>();
          foreach (var WELLS_MAX_AREA in strs)
          {
            var tolReturnValueMinRange = Regex.Match(WELLS_MAX_AREA, UNIMPRESSIONABLE);
            if (tolReturnValueMinRange.Success)
            {
              kvs.Add(new ValueTuple<string, string, int>(tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value, tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value, int.Parse(tolReturnValueMinRange.Groups[INTROPUNITIVENESS].Value)));
            }
            else
            {
              throw new System.Exception();
            }
          }
          return kvs.GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1).OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key + string.Join(DEMATERIALISING, GetLabelString(TolLightRangeSingleSideMax.First().Item2, TolLightRangeSingleSideMax.Select(y => y.Item3))));
        }
        static IEnumerable<string> GetLabelString(string prefix, IEnumerable<int> nums)
        {
          foreach (var max_rainpipe_to_balconyfloordrain in GetPairs(nums.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax)))
          {
            if (max_rainpipe_to_balconyfloordrain.Key == max_rainpipe_to_balconyfloordrain.Value)
            {
              yield return prefix + max_rainpipe_to_balconyfloordrain.Key;
            }
            else
            {
              yield return prefix + max_rainpipe_to_balconyfloordrain.Key + THESAURUSEXCREMENT + prefix + max_rainpipe_to_balconyfloordrain.Value;
            }
          }
        }
      }
      var REGION_BORDER_BUFFE_RDISTANCE = pipeIds.Select(id => DrainageLabelItem.Parse(id)).Where(tolReturnValueMinRange => tolReturnValueMinRange != null).ToList();
      var rest = pipeIds.Except(REGION_BORDER_BUFFE_RDISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Label)).ToList();
      var TolLaneProtect = REGION_BORDER_BUFFE_RDISTANCE.GroupBy(tolReturnValueMinRange => VTFac.Create(tolReturnValueMinRange.Prefix, tolReturnValueMinRange.D1S, tolReturnValueMinRange.Suffix)).Select(l => l.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.D2).ToList());
      foreach (var default_fire_valve_width in TolLaneProtect)
      {
        if (default_fire_valve_width.Count == THESAURUSHOUSING)
        {
          yield return default_fire_valve_width.First().Label;
        }
        else if (default_fire_valve_width.Count > THESAURUSPERMUTATION && default_fire_valve_width.Count == default_fire_valve_width.Last().D2 - default_fire_valve_width.First().D2 + THESAURUSHOUSING)
        {
          var tolReturnValueMinRange = default_fire_valve_width.First();
          yield return $"{tolReturnValueMinRange.Prefix}{tolReturnValueMinRange.D1S}-{default_fire_valve_width.First().D2S}{tolReturnValueMinRange.Suffix}~{default_fire_valve_width.Last().D2S}{tolReturnValueMinRange.Suffix}";
        }
        else
        {
          var sb = new StringBuilder();
          {
            var tolReturnValueMinRange = default_fire_valve_width.First();
            sb.Append($"{tolReturnValueMinRange.Prefix}{tolReturnValueMinRange.D1S}-");
          }
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < default_fire_valve_width.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var tolReturnValueMinRange = default_fire_valve_width[MAX_ANGEL_TOLLERANCE];
            sb.Append($"{tolReturnValueMinRange.D2S}{tolReturnValueMinRange.Suffix}");
            if (MAX_ANGEL_TOLLERANCE != default_fire_valve_width.Count - THESAURUSHOUSING)
            {
              sb.Append(DEMATERIALISING);
            }
          }
          yield return sb.ToString();
        }
      }
      foreach (var DEFAULT_FIRE_VALVE_WIDTH in rest)
      {
        yield return DEFAULT_FIRE_VALVE_WIDTH;
      }
    }
    public static IEnumerable<KeyValuePair<int, int>> GetPairs(IEnumerable<int> ints)
    {
      int sidewaterbucket_y_indent = int.MinValue;
      int ed = int.MinValue;
      foreach (var MAX_ANGEL_TOLLERANCE in ints)
      {
        if (sidewaterbucket_y_indent == int.MinValue)
        {
          sidewaterbucket_y_indent = MAX_ANGEL_TOLLERANCE;
          ed = MAX_ANGEL_TOLLERANCE;
        }
        else if (ed + THESAURUSHOUSING == MAX_ANGEL_TOLLERANCE)
        {
          ed = MAX_ANGEL_TOLLERANCE;
        }
        else
        {
          yield return new KeyValuePair<int, int>(sidewaterbucket_y_indent, ed);
          sidewaterbucket_y_indent = MAX_ANGEL_TOLLERANCE;
          ed = MAX_ANGEL_TOLLERANCE;
        }
      }
      if (sidewaterbucket_y_indent != int.MinValue)
      {
        yield return new KeyValuePair<int, int>(sidewaterbucket_y_indent, ed);
      }
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GLineSegment default_fire_valve_length, Action maxDeviceplatformArea)
    {
      if (default_fire_valve_length.IsValid) maxToiletToFloordrainDistance.Add(new KeyValuePair<Geometry, Action>(default_fire_valve_length.ToLineString(), maxDeviceplatformArea));
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GLineSegment default_fire_valve_length, List<Geometry> MinWellToUrinalDistance)
    {
      if (default_fire_valve_length.IsValid) _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, () => { MinWellToUrinalDistance.Add(default_fire_valve_length.ToLineString()); });
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GRect DEFAULT_FIRE_VALVE_WIDTH, Action maxDeviceplatformArea)
    {
      if (DEFAULT_FIRE_VALVE_WIDTH.IsValid) maxToiletToFloordrainDistance.Add(new KeyValuePair<Geometry, Action>(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon(), maxDeviceplatformArea));
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GRect DEFAULT_FIRE_VALVE_WIDTH, List<Geometry> MinWellToUrinalDistance)
    {
      if (DEFAULT_FIRE_VALVE_WIDTH.IsValid) _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, () => { MinWellToUrinalDistance.Add(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()); });
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, CText ct, Action maxDeviceplatformArea)
    {
      _tol_lane_protect(maxToiletToFloordrainDistance, ct.Boundary, maxDeviceplatformArea);
    }
    public static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, CText ct, List<CText> MinWellToUrinalDistance)
    {
      _tol_lane_protect(maxToiletToFloordrainDistance, ct, () => { MinWellToUrinalDistance.Add(ct); });
    }
    public static void DrawRainFlatDiagram(RainSystemDiagramViewModel vm)
    {
      var range = CadCache.TryGetRange();
      if (range == null)
      {
        Active.Editor.WriteMessage(THESAURUSPOWERLESS);
        return;
      }
      FocusMainWindow();
      using (DocLock)
      using (var adb = AcadDatabase.Active())
      using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
      {
        if (adb.ModelSpace.OfType<MLeader>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer == MLeaderLayer).Any())
        {
          var DEFAULT_FIRE_VALVE_WIDTH = MessageBox.Show(QUOTATIONISOPHANE, THESAURUSMISADVENTURE, MessageBoxButtons.YesNo);
          if (DEFAULT_FIRE_VALVE_WIDTH == DialogResult.No) return;
        }
        if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
        var mlPts = new List<Point>(THESAURUSREPERCUSSION);
        foreach (var tolReturnValue0Approx in adb.ModelSpace.OfType<MLeader>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer == MLeaderLayer))
        {
          var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValue0Approx.GetFirstVertex(THESAURUSSTAMPEDE).ToNTSPoint();
          MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData = tolReturnValue0Approx;
          mlPts.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
        }
        ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CollectRainGeoData(range, adb, out List<StoreyInfo> storeysItems, out ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData);
        var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.CreateRainDrawingData(adb, geoData, INTRAVASCULARLY);
        exInfo.drDatas = drDatas;
        exInfo.geoData = geoData;
        exInfo.storeysItems = storeysItems;
        exInfo.vm = vm;
        Dispose();
        var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(geoData.WLines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSCOMMUNICATION)).ToList());
        var _tol_avg_column_dist = mlPts.Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => maxDeviceplatformArea(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN).Any()).ToList();
        foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
        {
          adb.Element<Entity>(((Entity)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).ObjectId, THESAURUSOBSTINACY).Erase();
        }
        DrawFlatDiagram(exInfo);
        FlushDQ();
      }
    }
    public static void DrawDrainageFlatDiagram(DrainageSystemDiagramViewModel vm)
    {
      var range = CadCache.TryGetRange();
      if (range == null)
      {
        Active.Editor.WriteMessage(THESAURUSPOWERLESS);
        return;
      }
      FocusMainWindow();
      using (DocLock)
      using (var adb = AcadDatabase.Active())
      using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
      {
        if (adb.ModelSpace.OfType<MLeader>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer == MLeaderLayer).Any())
        {
          var DEFAULT_FIRE_VALVE_WIDTH = MessageBox.Show(QUOTATIONISOPHANE, THESAURUSMISADVENTURE, MessageBoxButtons.YesNo);
          if (DEFAULT_FIRE_VALVE_WIDTH == DialogResult.No) return;
        }
        if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
        var mlPts = new List<Point>(THESAURUSREPERCUSSION);
        foreach (var tolReturnValue0Approx in adb.ModelSpace.OfType<MLeader>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer == MLeaderLayer))
        {
          var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValue0Approx.GetFirstVertex(THESAURUSSTAMPEDE).ToNTSPoint();
          MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData = tolReturnValue0Approx;
          mlPts.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
        }
        ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectDrainageGeoData(range, adb, out List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems, out ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData);
        var (drDatas, exInfo) = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CreateDrainageDrawingData(geoData, INTRAVASCULARLY);
        exInfo.drDatas = drDatas;
        exInfo.geoData = geoData;
        exInfo.storeysItems = storeysItems;
        exInfo.vm = vm;
        Dispose();
        var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(geoData.DLines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSCOMMUNICATION)).ToList());
        var _tol_avg_column_dist = mlPts.Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => maxDeviceplatformArea(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN).Any()).ToList();
        foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
        {
          adb.Element<Entity>(((Entity)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).ObjectId, THESAURUSOBSTINACY).Erase();
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
    public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo)
    {
      if (exInfo is null) return;
      DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
    }
    static List<GLineSegment> Substract(List<GLineSegment> TolLane, IEnumerable<Geometry> DEFAULT_VOLTAGE)
    {
      static Func<Geometry, List<T>> CreateIntersectsSelector<T>(ICollection<T> DEFAULT_VOLTAGE) where T : Geometry
      {
        if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) return DEFAULT_FIRE_VALVE_WIDTH => new List<T>();
        var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(DEFAULT_VOLTAGE.Count > THESAURUSACRIMONIOUS ? DEFAULT_VOLTAGE.Count : THESAURUSACRIMONIOUS);
        foreach (var DEFAULT_FIRE_VALVE_LENGTH in DEFAULT_VOLTAGE) engine.Insert(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal, DEFAULT_FIRE_VALVE_LENGTH);
        return DEFAULT_FIRE_VALVE_LENGTH =>
        {
          if (DEFAULT_FIRE_VALVE_LENGTH == null) throw new ArgumentNullException();
          var default_voltage = GeoFac.PreparedGeometryFactory.Create(DEFAULT_FIRE_VALVE_LENGTH);
          return engine.Query(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal).Where(default_fire_valve_width => default_voltage.Intersects(default_fire_valve_width)).ToList();
        };
      }
      var tolReturnValueRange = new HashSet<LineString>(TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()));
      foreach (var DEFAULT_FIRE_VALVE_LENGTH in DEFAULT_VOLTAGE)
      {
        var maxDeviceplatformArea = CreateIntersectsSelector(tolReturnValueRange);
        var _lines = maxDeviceplatformArea(DEFAULT_FIRE_VALVE_LENGTH);
        if (_lines.Count > THESAURUSSTAMPEDE)
        {
          foreach (var TolUniformSideLenth in _lines)
          {
            tolReturnValueRange.Remove(TolUniformSideLenth);
          }
          tolReturnValueRange.AddRange(_lines.SelectMany(TolUniformSideLenth => GeoFac.GetLines(TolUniformSideLenth.Difference(DEFAULT_FIRE_VALVE_LENGTH)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString())));
        }
      }
      return tolReturnValueRange.SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).ToList();
    }
    static GLineSegment GetCenterLine(List<GLineSegment> TolLane)
    {
      if (TolLane.Count == THESAURUSSTAMPEDE) throw new ArgumentException();
      if (TolLane.Count == THESAURUSHOUSING) return TolLane[THESAURUSSTAMPEDE];
      var angles = TolLane.Select(default_fire_valve_length => default_fire_valve_length.SingleAngle).ToList();
      if (angles.Max() - angles.Min() >= Math.PI / THESAURUSPERMUTATION)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < angles.Count; MAX_ANGEL_TOLLERANCE++)
        {
          if (angles[MAX_ANGEL_TOLLERANCE] > Math.PI / THESAURUSPERMUTATION)
          {
            angles[MAX_ANGEL_TOLLERANCE] -= Math.PI;
          }
        }
      }
      var angle = angles.Average();
      var DEFAULT_FIRE_VALVE_WIDTH = Extents2dCalculator.Calc(TolLane.YieldPoints()).ToGRect();
      var tolReturnValueMinRange = Matrix2d.Displacement(-DEFAULT_FIRE_VALVE_WIDTH.Center.ToVector2d()).PreMultiplyBy(Matrix2d.Rotation(-angle, default));
      DEFAULT_FIRE_VALVE_WIDTH = Extents2dCalculator.Calc(TolLane.Select(default_fire_valve_length => default_fire_valve_length.TransformBy(tolReturnValueMinRange))).ToGRect();
      return new GLineSegment(GetMidPoint(DEFAULT_FIRE_VALVE_WIDTH.LeftButtom, DEFAULT_FIRE_VALVE_WIDTH.LeftTop), GetMidPoint(DEFAULT_FIRE_VALVE_WIDTH.RightButtom, DEFAULT_FIRE_VALVE_WIDTH.RightTop)).TransformBy(tolReturnValueMinRange.Inverse());
    }
    static double FixValue(double MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)
    {
      return Math.Floor(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN + THESAURUSCONFECTIONERY);
    }
    public static void DrawBackToFlatDiagram(List<StoreyInfo> storeysItems, ThMEPWSS.ReleaseNs.RainSystemNs.RainGeoData geoData, List<ThMEPWSS.ReleaseNs.RainSystemNs.RainDrawingData> drDatas, ThMEPWSS.ReleaseNs.RainSystemNs.ExtraInfo exInfo, RainSystemDiagramViewModel vm)
    {
      static string getDn(int MAX_CONDENSEPIPE_TO_WASHMACHINE)
      {
        return DnToString(MAX_CONDENSEPIPE_TO_WASHMACHINE);
      }
      static string DnToString(int MAX_CONDENSEPIPE_TO_WASHMACHINE)
      {
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSREVERSE) return PHOTOFLUOROGRAM;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSSYMMETRICAL) return THESAURUSDISREPUTABLE;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSDRAGOON) return ALLITERATIVENESS;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSMORTALITY) return QUOTATIONBREWSTER;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSNEGATE) return QUOTATIONDOPPLER;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSINNOCUOUS) return QUOTATIONDOPPLER;
        return IRRESPONSIBLENESS;
      }
      int parseDn(string MAX_CONDENSEPIPE_TO_WASHMACHINE)
      {
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE.StartsWith(THESAURUSVICTORIOUS)) return THESAURUSENTREPRENEUR;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is THESAURUSJOURNAL) return THESAURUSEVERLASTING;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is CHRISTIANIZATION or UNPREMEDITATEDNESS) return HYPERDISYLLABLE;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is THESAURUSFINICKY) return parseDn(vm.Params.CondensePipeVerticalDN);
        var tolReturnValueMinRange = Regex.Match(MAX_CONDENSEPIPE_TO_WASHMACHINE, SUPERNATURALIZE);
        if (tolReturnValueMinRange.Success) return int.Parse(tolReturnValueMinRange.Value);
        return THESAURUSSTAMPEDE;
      }
      var vp2fdpts = new HashSet<Point2d>();
      var y1lpts = new HashSet<Point2d>();
      var y2lpts = new HashSet<Point2d>();
      var nlpts = new HashSet<Point2d>();
      var fl0pts = new HashSet<Point2d>();
      {
        var toCmp = new HashSet<KeyValuePair<int, int>>();
        {
          var REGION_BORDER_BUFFE_RDISTANCE = storeysItems.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Any()).ToList();
          var minlst = REGION_BORDER_BUFFE_RDISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Min()).ToList();
          var maxlst = REGION_BORDER_BUFFE_RDISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Max()).ToList();
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < maxlst.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var _tolReturnValueMinRange = maxlst[MAX_ANGEL_TOLLERANCE];
            for (int MAX_ANGLE_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGLE_TOLLERANCE < maxlst.Count; MAX_ANGLE_TOLLERANCE++)
            {
              if (MAX_ANGLE_TOLLERANCE == MAX_ANGEL_TOLLERANCE) continue;
              var _tolReturnValueMaxDistance = minlst[MAX_ANGLE_TOLLERANCE];
              if (_tolReturnValueMaxDistance + THESAURUSHOUSING == _tolReturnValueMinRange)
              {
                toCmp.Add(new KeyValuePair<int, int>(storeysItems.IndexOf(REGION_BORDER_BUFFE_RDISTANCE[MAX_ANGLE_TOLLERANCE]), storeysItems.IndexOf(REGION_BORDER_BUFFE_RDISTANCE[MAX_ANGEL_TOLLERANCE])));
              }
            }
          }
        }
        foreach (var max_rainpipe_to_balconyfloordrain in toCmp)
        {
          var low = storeysItems[max_rainpipe_to_balconyfloordrain.Key];
          var high = storeysItems[max_rainpipe_to_balconyfloordrain.Value];
          var max_basecircle_area = geoData.FloorDrains.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS, THESAURUSOBSTINACY)).ToList();
          var Commonradius = GeoFac.CreateEnvelopeSelector(max_basecircle_area);
          max_basecircle_area = Commonradius(high.Boundary.ToPolygon());
          var pps = geoData.VerticalPipes.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList();
          var ppsf = GeoFac.CreateEnvelopeSelector(pps);
          pps = ppsf(low.Boundary.ToPolygon());
          var vhigh = -high.ContraPoint.ToVector2d();
          var vlow = -low.ContraPoint.ToVector2d();
          {
            var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = low.ContraPoint - high.ContraPoint;
            var si = max_rainpipe_to_balconyfloordrain.Value;
            var lbdict = exInfo.Items[si].LabelDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2);
            var y1ls = lbdict.Where(TolLightRangeSingleSideMax => IsY1L(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            var y2ls = lbdict.Where(TolLightRangeSingleSideMax => IsY2L(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            var nls = lbdict.Where(TolLightRangeSingleSideMax => IsNL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            var fl0s = lbdict.Where(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            y1lpts.AddRange(y1ls.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN));
            y2lpts.AddRange(y2ls.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN));
            nlpts.AddRange(nls.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN));
            fl0pts.AddRange(fl0s.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN));
          }
          var _fds = max_basecircle_area.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vhigh)).ToList();
          var _pps = pps.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vlow)).ToList();
          var _ppsf = GeoFac.CreateIntersectsSelector(_pps);
          foreach (var fd in _fds)
          {
            var ToiletWellsInterval = _ppsf(fd.GetCenter().ToNTSPoint()).FirstOrDefault();
            if (ToiletWellsInterval != null)
            {
              vp2fdpts.Add(pps[_pps.IndexOf(ToiletWellsInterval)].GetCenter());
            }
          }
        }
      }
      var vp2fdptst = GeoFac.CreateIntersectsTester(vp2fdpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList());
      var vp2fdptrgs = vp2fdpts.Select(TolLightRangeSingleSideMax => GRect.Create(TolLightRangeSingleSideMax, THESAURUSHESITANCY).ToPolygon()).ToList();
      var vp2fdptrgst = GeoFac.CreateIntersectsTester(vp2fdptrgs);
      var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
      var cadDatas = exInfo.CadDatas;
      Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
      Func<List<Geometry>, Func<Geometry, bool>> T = GeoFac.CreateIntersectsTester;
      Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
      Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
      static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(MinWellToUrinalDistance => GeoFac.CreateGeometry(MinWellToUrinalDistance)).ToList();
      {
        var after = new List<ValueTuple<string, Geometry>>();
        var (sankakuptsf, addsankaku) = GeoFac.CreateIntersectsSelectorEngine(mlInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.BasePoint.ToNTSPoint(TolLightRangeSingleSideMax)));
        void modify(Geometry range, Action<MLeaderInfo> cb)
        {
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(range))
          {
            cb((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData);
          }
        }
        void draw(string repeated_point_distance, Geometry DEFAULT_FIRE_VALVE_LENGTH, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY, string note = null)
        {
          Point2d center;
          if (DEFAULT_FIRE_VALVE_LENGTH is Point point)
          {
            center = point.ToPoint2d();
            DEFAULT_FIRE_VALVE_LENGTH = GRect.Create(center, UNCONSEQUENTIAL).ToPolygon();
          }
          else
          {
            center = DEFAULT_FIRE_VALVE_LENGTH.GetCenter();
          }
          {
            var SidewaterbucketXIndent = INTRAVASCULARLY;
            foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(DEFAULT_FIRE_VALVE_LENGTH))
            {
              var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
              if (string.IsNullOrWhiteSpace(TolLightRangeSingleSideMin.Text) || overWrite)
              {
                TolLightRangeSingleSideMin.Text = repeated_point_distance;
              }
              if (!string.IsNullOrWhiteSpace(note)) TolLightRangeSingleSideMin.Note = note;
              SidewaterbucketXIndent = THESAURUSOBSTINACY;
            }
            if (!SidewaterbucketXIndent && autoCreate)
            {
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = center;
              var TolLightRangeSingleSideMin = MLeaderInfo.Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, repeated_point_distance);
              mlInfos.Add(TolLightRangeSingleSideMin);
              var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint();
              _raiseDistanceToStartDefault.UserData = TolLightRangeSingleSideMin;
              if (!string.IsNullOrWhiteSpace(note)) TolLightRangeSingleSideMin.Note = note;
              addsankaku(_raiseDistanceToStartDefault);
            }
          }
        }
        {
          using var max_balcony_to_balcony_distance = new PriorityQueue(THESAURUSINCOMPLETE);
          var precisePts = new HashSet<Point2d>();
          {
            foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
            {
              var djPts = new HashSet<Point>();
              var _linesGroup = new HashSet<HashSet<GLineSegment>>();
              var tolGroupBlkLane = geoData.Storeys[si].ToPolygon();
              var gpGeos = GeoFac.CreateEnvelopeSelector(geoData.Groups.Select(GeoFac.CreateGeometry).ToList())(tolGroupBlkLane);
              var wlsegs = geoData.OWLines.ToList();
              var vertices = GeoFac.CreateEnvelopeSelector(wlsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.StartPoint.ToNTSPoint().Tag(TolLightRangeSingleSideMax)).Concat(wlsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.EndPoint.ToNTSPoint().Tag(TolLightRangeSingleSideMax))).ToList())(tolGroupBlkLane);
              var verticesf = GeoFac.CreateIntersectsSelector(vertices);
              wlsegs = vertices.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData).Cast<GLineSegment>().Distinct().ToList();
              max_balcony_to_balcony_distance.Enqueue(THESAURUSOCCASIONALLY, () =>
              {
                var linesGeos = _linesGroup.Select(tolReturnValueRange => new MultiLineString(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToArray())).ToList();
                var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                foreach (var tolReturnValueRange in _linesGroup)
                {
                  var kvs = new List<KeyValuePair<GLineSegment, MLeaderInfo>>();
                  foreach (var TolUniformSideLenth in tolReturnValueRange.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > QUOTATIONLUCANIAN))
                  {
                    var _tol_avg_column_dist = sankakuptsf(TolUniformSideLenth.Buffer(THESAURUSPERMUTATION));
                    if (_tol_avg_column_dist.Count == THESAURUSHOUSING)
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist[THESAURUSSTAMPEDE];
                      var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                      kvs.Add(new KeyValuePair<GLineSegment, MLeaderInfo>(TolUniformSideLenth, TolLightRangeSingleSideMin));
                    }
                  }
                  {
                    var TolLane = kvs.Select(max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Key).Distinct().ToList();
                    var lns = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
                    var vertexs = TolLane.YieldPoints().Distinct().ToList();
                    var lnsf = GeoFac.CreateIntersectsSelector(lns);
                    var lnscf = GeoFac.CreateContainsSelector(TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(-THESAURUSPERMUTATION).ToLineString()).ToList());
                    var opts = new List<Ref<RegexOptions>>();
                    foreach (var vertex in vertexs)
                    {
                      var MinWellToUrinalDistance = lnsf(GeoFac.CreateCirclePolygon(vertex, THESAURUSCOMMUNICATION, SUPERLATIVENESS));
                      RegexOptions opt;
                      if (MinWellToUrinalDistance.Count == THESAURUSHOUSING)
                      {
                        opt = RegexOptions.IgnoreCase;
                      }
                      else if (MinWellToUrinalDistance.Count == THESAURUSPERMUTATION)
                      {
                        opt = RegexOptions.Multiline;
                      }
                      else if (MinWellToUrinalDistance.Count > THESAURUSPERMUTATION)
                      {
                        opt = RegexOptions.ExplicitCapture;
                      }
                      else
                      {
                        opt = RegexOptions.None;
                      }
                      opts.Add(new Ref<RegexOptions>(opt));
                    }
                    foreach (var DEFAULT_FIRE_VALVE_LENGTH in GeoFac.GroupLinesByConnPoints(GeoFac.GetLines(GeoFac.CreateGeometry(lns).Difference(GeoFac.CreateGeometryEx(opts.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value == RegexOptions.ExplicitCapture).Select(opts).ToList(vertexs).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle(THESAURUSPERMUTATION).ToCirclePolygon(SUPERLATIVENESS)).ToList()))).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList(), UNCONSEQUENTIAL))
                    {
                      var bf = DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSPERMUTATION);
                      var _tol_avg_column_dist = sankakuptsf(bf);
                      var _tol_light_range_single_side_max = _tol_avg_column_dist.Select(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).ToList();
                      if (_tol_light_range_single_side_max.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Text).Distinct().Count() == THESAURUSHOUSING)
                      {
                        var repeated_point_distance = _tol_light_range_single_side_max[THESAURUSSTAMPEDE];
                        var _segs = lnscf(bf).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).ToList();
                        if (_segs.Count > THESAURUSHOUSING)
                        {
                          const double LEN = PHOTOCONDUCTION;
                          if (_segs.Any(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= LEN))
                          {
                            foreach (var default_fire_valve_length in _segs)
                            {
                              if (default_fire_valve_length.Length < LEN)
                              {
                                draw(null, default_fire_valve_length.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                              }
                            }
                          }
                          else
                          {
                            var _tolReturnValueMinRange = _segs.Max(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length);
                            foreach (var default_fire_valve_length in _segs)
                            {
                              if (default_fire_valve_length.Length != _tolReturnValueMinRange)
                              {
                                draw(null, default_fire_valve_length.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                              }
                            }
                          }
                        }
                        else
                        {
                        }
                      }
                    }
                  }
                }
              });
              var REPEATED_POINT_DISTANCE = cadDatas[si];
              var wlinesGeos = GeoFac.GroupLinesByConnPoints(REPEATED_POINT_DISTANCE.WLines, DINOFLAGELLATES).ToList();
              var wlinesGeosf = F(wlinesGeos);
              var portst = T(REPEATED_POINT_DISTANCE.RainPortSymbols.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSENTREPRENEUR)).ToList());
              var portsf = F(REPEATED_POINT_DISTANCE.RainPortSymbols);
              var wellst = T(REPEATED_POINT_DISTANCE.WaterWells.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(MISAPPREHENSIVE)).ToList());
              var MaxTagLength = F(REPEATED_POINT_DISTANCE.WaterWells);
              var swellst = T(REPEATED_POINT_DISTANCE.WaterSealingWells.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(MISAPPREHENSIVE)).ToList());
              var swellsf = F(REPEATED_POINT_DISTANCE.WaterSealingWells);
              var ditchest = T(REPEATED_POINT_DISTANCE.Ditches.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSENTREPRENEUR)).ToList());
              var MaxTagXposition = F(REPEATED_POINT_DISTANCE.Ditches);
              var fldrs = REPEATED_POINT_DISTANCE.FloorDrains;
              var MaxDownspoutToBalconywashingfloordrain = REPEATED_POINT_DISTANCE.VerticalPipes;
              var _vps = MaxDownspoutToBalconywashingfloordrain.ToList();
              {
                var max_basecircle_area = MaxDownspoutToBalconywashingfloordrain.Where(vp2fdptst).ToList();
                fldrs = fldrs.Concat(max_basecircle_area).Distinct().ToList();
                MaxDownspoutToBalconywashingfloordrain = MaxDownspoutToBalconywashingfloordrain.Except(max_basecircle_area).ToList();
              }
              var Commonradius = F(fldrs);
              var fdst = T(fldrs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSPERMUTATION)).ToList());
              var fdrings = geoData.FloorDrainRings.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(THESAURUSDISINGENUOUS).Shell.Buffer(THESAURUSACRIMONIOUS)).ToList();
              var fdringsf = F(fdrings);
              var fdringst = T(fdrings);
              var ppsf = F(MaxDownspoutToBalconywashingfloordrain);
              var ppst = T(MaxDownspoutToBalconywashingfloordrain);
              var cpst = T(REPEATED_POINT_DISTANCE.CondensePipes);
              var MaxBalconybasinToBalcony = F(REPEATED_POINT_DISTANCE.CondensePipes);
              var lbdict = exInfo.Items[si].LabelDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2);
              var y1ls = lbdict.Where(TolLightRangeSingleSideMax => IsY1L(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
              var y2ls = lbdict.Where(TolLightRangeSingleSideMax => IsY2L(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
              var nls = lbdict.Where(TolLightRangeSingleSideMax => IsNL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
              var fl0s = lbdict.Where(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
              {
                var MaxRainpipeToWashmachine = F(MaxDownspoutToBalconywashingfloordrain);
                var _y1ls = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((y1lpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
                var _y2ls = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((y2lpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
                var _nls = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((nlpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
                var _fl0s = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((fl0pts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
                {
                  var default_voltage = G(REPEATED_POINT_DISTANCE.LabelLines).ToIPreparedGeometry();
                  y1ls = y1ls.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
                  y2ls = y2ls.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
                  nls = nls.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
                  fl0s = fl0s.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
                }
                {
                  y1ls = y1ls.Concat(_y1ls).Distinct().ToList();
                  y2ls = y2ls.Concat(_y2ls).Distinct().ToList();
                  nls = nls.Concat(_nls).Distinct().ToList();
                  fl0s = fl0s.Concat(_fl0s).Distinct().ToList();
                }
              }
              var fldrst = T(fldrs);
              var nlst = T(nls);
              var y1lst = T(y1ls);
              var y2lst = T(y2ls);
              var fl0st = T(fl0s);
              var fl0sf = F(fl0s);
              PipeType maxCondensepipeToWashmachine(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)
              {
                var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint();
                if (nlst(_raiseDistanceToStartDefault)) return PipeType.NL;
                if (y1lst(_raiseDistanceToStartDefault)) return PipeType.Y1L;
                if (y2lst(_raiseDistanceToStartDefault)) return PipeType.Y2L;
                if (fl0st(_raiseDistanceToStartDefault)) return PipeType.FL0;
                return PipeType.Unknown;
              }
              string getPipeDn(PipeType MaxToiletToFloordrainDistance)
              {
                return MaxToiletToFloordrainDistance switch
                {
                  PipeType.FL0 => vm.Params.WaterWellPipeVerticalDN,
                  PipeType.Y2L => vm.Params.BalconyRainPipeDN,
                  PipeType.NL => vm.Params.CondensePipeVerticalDN,
                  PipeType.Unknown => null,
                  _ => IRRESPONSIBLENESS,
                };
              }
              string getDN(Geometry MaxBalconyToRainpipeDistance)
              {
                if (nlst(MaxBalconyToRainpipeDistance)) return vm.Params.CondensePipeVerticalDN;
                if (y2lst(MaxBalconyToRainpipeDistance)) return vm.Params.BalconyRainPipeDN;
                if (fl0st(MaxBalconyToRainpipeDistance)) return vm.Params.WaterWellFloorDrainDN;
                if (y1lst(MaxBalconyToRainpipeDistance)) return IRRESPONSIBLENESS;
                if (fldrst(MaxBalconyToRainpipeDistance)) return vm.Params.BalconyFloorDrainDN;
                return null;
              }
              var nlfdpts = nls.SelectMany(nl => wlinesGeosf(nl)).Distinct().SelectMany(TolLightRangeSingleSideMax => Commonradius(TolLightRangeSingleSideMax))
                      .Concat(nls.SelectMany(TolLightRangeSingleSideMax => Commonradius(TolLightRangeSingleSideMax.Buffer(THESAURUSHYPNOTIC))))
                      .Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter()).Distinct().ToList();
              foreach (var wlinesGeo in wlinesGeos)
              {
                var wlinesGeoBuf = wlinesGeo.Buffer(THESAURUSACRIMONIOUS);
                var wlbufgf = wlinesGeoBuf.ToIPreparedGeometry();
                max_balcony_to_balcony_distance.Enqueue(THESAURUSCENSURE, () =>
                {
                  foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(wlinesGeo.Buffer(THESAURUSHOUSING)))
                  {
                    var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                    if (TolLightRangeSingleSideMin.Text is THESAURUSDEPLORE)
                    {
                      if (nlst(wlinesGeo))
                      {
                        TolLightRangeSingleSideMin.Text = vm.Params.CondensePipeVerticalDN;
                      }
                      else if (y1lst(wlinesGeo) || y2lst(wlinesGeo) || fl0st(wlinesGeo))
                      {
                        TolLightRangeSingleSideMin.Text = IRRESPONSIBLENESS;
                      }
                    }
                  }
                });
                max_balcony_to_balcony_distance.Enqueue(THESAURUSSTAMPEDE, () =>
                {
                  var tolReturnValueRange = Substract(GeoFac.GetLines(wlinesGeo).ToList(), MaxDownspoutToBalconywashingfloordrain.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(-SUPERLATIVENESS)).Concat(REPEATED_POINT_DISTANCE.CondensePipes.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(-SUPERLATIVENESS))));
                  tolReturnValueRange = GeoFac.ToNodedLineSegments(tolReturnValueRange).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= DISPROPORTIONAL).ToList();
                  tolReturnValueRange = GeoFac.GroupParallelLines(tolReturnValueRange, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(default_fire_valve_width => GetCenterLine(default_fire_valve_width)).ToList();
                  precisePts.AddRange(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Center));
                  tolReturnValueRange = GeoFac.ToNodedLineSegments(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSHOUSING)).ToList()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSMORTUARY).Select(TolLightRangeSingleSideMax => new GLineSegment(new Point2d(FixValue(TolLightRangeSingleSideMax.StartPoint.X), FixValue(TolLightRangeSingleSideMax.StartPoint.Y)), new Point2d(FixValue(TolLightRangeSingleSideMax.EndPoint.X), FixValue(TolLightRangeSingleSideMax.EndPoint.Y)))).Distinct().ToList();
                  var linesf = GeoFac.CreateIntersectsSelector(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList());
                  {
                    var _pts = tolReturnValueRange.SelectMany(default_fire_valve_length => new Point2d[] { default_fire_valve_length.StartPoint, default_fire_valve_length.EndPoint }).GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).Distinct().Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => wlbufgf.Intersects(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint())).ToList();
                    var _tol_avg_column_dist = _pts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
                    djPts.AddRange(_tol_avg_column_dist);
                    _linesGroup.Add(tolReturnValueRange.ToHashSet());
                  }
                  foreach (var default_fire_valve_length in tolReturnValueRange)
                  {
                    if (default_fire_valve_length.Length < DINOFLAGELLATES) continue;
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = default_fire_valve_length.Center;
                    var TolLightRangeSingleSideMin = MLeaderInfo.Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, THESAURUSDEPLORE);
                    mlInfos.Add(TolLightRangeSingleSideMin);
                    var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint();
                    _raiseDistanceToStartDefault.UserData = TolLightRangeSingleSideMin;
                    addsankaku(_raiseDistanceToStartDefault);
                  }
                  max_balcony_to_balcony_distance.Enqueue(THESAURUSHOUSING, () =>
                                  {
                                    max_balcony_to_balcony_distance.Enqueue(THESAURUSPERMUTATION, () =>
                                                      {
                                                        foreach (var cp in REPEATED_POINT_DISTANCE.CondensePipes)
                                                        {
                                                          foreach (var TolUniformSideLenth in linesf(cp))
                                                          {
                                                            var MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondensePipeHorizontalDN;
                                                            draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.GetCenter().Expand(THESAURUSHOUSING).ToGRect().ToPolygon());
                                                          }
                                                        }
                                                      }
);
                                    max_balcony_to_balcony_distance.Enqueue(THESAURUSPERMUTATION, () =>
                                                    {
                                                      if (cpst(wlinesGeo))
                                                      {
                                                        var DEFAULT_FIRE_VALVE_WIDTH = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(MaxBalconybasinToBalcony(wlinesGeo))).ToGRect();
                                                        var MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondensePipeHorizontalDN;
                                                        after.Add(new(MAX_CONDENSEPIPE_TO_WASHMACHINE, DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()));
                                                      }
                                                      if (fdst(wlinesGeo))
                                                      {
                                                        var DEFAULT_FIRE_VALVE_WIDTH = GeoFac.CreateGeometry(ppsf(wlinesGeo).Concat(Commonradius(wlinesGeo))).ToGRect().Expand(-DISPENSABLENESS);
                                                        if (!vp2fdptst(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()))
                                                        {
                                                          string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                                          {
                                                            if (fl0st(wlinesGeo))
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WaterWellFloorDrainDN;
                                                            }
                                                            else if (nlst(wlinesGeo))
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondenseFloorDrainDN;
                                                            }
                                                            else
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BalconyFloorDrainDN;
                                                            }
                                                          }
                                                          after.Add(new(MAX_CONDENSEPIPE_TO_WASHMACHINE, DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()));
                                                        }
                                                      }
                                                    });
                                    max_balcony_to_balcony_distance.Enqueue(INTROPUNITIVENESS, () =>
                                                    {
                                                      foreach (var wlinesGeo in GeoFac.GroupGeometries(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSHOUSING)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList()).Select(GeoFac.CreateGeometry))
                                                      {
                                                        if (fldrst(wlinesGeo))
                                                        {
                                                          foreach (var default_fire_valve_length in GeoFac.GetLines(wlinesGeo))
                                                          {
                                                            string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                                            {
                                                              if (fl0st(wlinesGeo))
                                                              {
                                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WaterWellFloorDrainDN;
                                                              }
                                                              else if (nlst(wlinesGeo))
                                                              {
                                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondenseFloorDrainDN;
                                                              }
                                                              else
                                                              {
                                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BalconyFloorDrainDN;
                                                              }
                                                            }
                                                            draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, default_fire_valve_length.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                          }
                                                        }
                                                        else if (nlst(wlinesGeo))
                                                        {
                                                          foreach (var default_fire_valve_length in GeoFac.GetLines(wlinesGeo))
                                                          {
                                                            {
                                                              draw(vm.Params.CondensePipeVerticalDN, default_fire_valve_length.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                            }
                                                          }
                                                        }
                                                        else if (y1lst(wlinesGeo))
                                                        {
                                                          foreach (var default_fire_valve_length in GeoFac.GetLines(wlinesGeo))
                                                          {
                                                            {
                                                              draw(IRRESPONSIBLENESS, default_fire_valve_length.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                            }
                                                          }
                                                        }
                                                        else if (y2lst(wlinesGeo))
                                                        {
                                                          foreach (var default_fire_valve_length in GeoFac.GetLines(wlinesGeo))
                                                          {
                                                            {
                                                              draw(vm.Params.BalconyRainPipeDN, default_fire_valve_length.Center.Expand(THESAURUSCOMMUNICATION).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                                                            }
                                                          }
                                                        }
                                                      }
                                                    });
                                  });
                });
              }
              max_balcony_to_balcony_distance.Enqueue(ECCLESIASTICISM, () =>
              {
                foreach (var gpGeo in gpGeos)
                {
                  var pps = ppsf(gpGeo);
                  foreach (var ToiletWellsInterval in pps)
                  {
                    var MAX_CONDENSEPIPE_TO_WASHMACHINE = getPipeDn(maxCondensepipeToWashmachine(ToiletWellsInterval.GetCenter()));
                    if (MAX_CONDENSEPIPE_TO_WASHMACHINE is not null)
                    {
                      modify(GeoFac.GetLines(gpGeo, skipPolygon: THESAURUSOBSTINACY).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSHOUSING)).ToGeometry(), TolLightRangeSingleSideMin => TolLightRangeSingleSideMin.Text = MAX_CONDENSEPIPE_TO_WASHMACHINE);
                    }
                  }
                  if (pps.Count == THESAURUSHOUSING)
                  {
                    var ToiletWellsInterval = pps[THESAURUSSTAMPEDE];
                    if (y1ls.Contains(ToiletWellsInterval) || y2ls.Contains(ToiletWellsInterval))
                    {
                      var buf = gpGeo.Buffer(THESAURUSPERMUTATION);
                      var oksegs = new HashSet<GLineSegment>();
                      foreach (var default_fire_valve_length in wlsegs)
                      {
                        if (buf.Contains(default_fire_valve_length.Center.ToNTSPoint()))
                        {
                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(default_fire_valve_length.Buffer(THESAURUSHOUSING)))
                          {
                            ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = IRRESPONSIBLENESS;
                          }
                          oksegs.Add(default_fire_valve_length);
                        }
                      }
                      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < THESAURUSCOMMUNICATION; MAX_ANGEL_TOLLERANCE++)
                      {
                        var TolLane = verticesf(oksegs.YieldPoints().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGRect(THESAURUSPERMUTATION).ToPolygon()).ToGeometry()).Select(TolLightRangeSingleSideMax => (GLineSegment)TolLightRangeSingleSideMax.UserData).Except(oksegs).ToList();
                        foreach (var default_fire_valve_length in TolLane)
                        {
                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(default_fire_valve_length.Buffer(THESAURUSHOUSING)))
                          {
                            ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = IRRESPONSIBLENESS;
                          }
                          oksegs.Add(default_fire_valve_length);
                        }
                        if (TolLane.Count == THESAURUSSTAMPEDE) break;
                      }
                    }
                  }
                }
                {
                  var text_height = GeoFac.CreateEnvelopeSelector(wlsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList())(tolGroupBlkLane);
                  foreach (var DEFAULT_FIRE_VALVE_LENGTH in GeoFac.GroupLinesByConnPoints(text_height, THESAURUSPERMUTATION))
                  {
                    var buf = DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSHOUSING);
                    if (cpst(buf) && nlst(buf))
                    {
                      var _tol_avg_column_dist = sankakuptsf(buf);
                      if (_tol_avg_column_dist.All(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE or THESAURUSDISREPUTABLE or QUOTATIONBREWSTER or QUOTATIONDOPPLER))
                      {
                        foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
                        {
                          ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = THESAURUSDISREPUTABLE;
                        }
                      }
                    }
                  }
                  foreach (var wl in text_height)
                  {
                    if (y1lst(wl))
                    {
                      foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(wl.Buffer(THESAURUSPERMUTATION)))
                      {
                        ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = IRRESPONSIBLENESS;
                      }
                    }
                  }
                }
                foreach (var DEFAULT_FIRE_VALVE_LENGTH in GeoFac.GroupLinesByConnPoints(REPEATED_POINT_DISTANCE.WLines, THESAURUSCOMMUNICATION))
                {
                  var TolLane = GeoFac.GetLines(DEFAULT_FIRE_VALVE_LENGTH).ToList();
                  if (TolLane.Count == THESAURUSSTAMPEDE) continue;
                  if (TolLane.Count == THESAURUSHOUSING)
                  {
                    var default_fire_valve_length = TolLane[THESAURUSSTAMPEDE];
                    var buf = default_fire_valve_length.Buffer(THESAURUSPERMUTATION);
                    var _tol_avg_column_dist = sankakuptsf(buf);
                    if (_tol_avg_column_dist.Count == THESAURUSHOUSING)
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist[THESAURUSSTAMPEDE];
                      if (ppst(default_fire_valve_length.StartPoint.ToNTSPoint()) || ppst(default_fire_valve_length.EndPoint.ToNTSPoint()))
                      {
                        if (fdringst(default_fire_valve_length.StartPoint.ToNTSPoint()) || fdringst(default_fire_valve_length.EndPoint.ToNTSPoint()))
                        {
                          modify(default_fire_valve_length.Buffer(THESAURUSHOUSING), TolLightRangeSingleSideMin =>
                                        {
                                          if (TolLightRangeSingleSideMin.Text is THESAURUSDISREPUTABLE)
                                          {
                                            TolLightRangeSingleSideMin.Text = QUOTATIONBREWSTER;
                                          }
                                        });
                        }
                      }
                    }
                  }
                }
                {
                  var lnsGeos = _linesGroup.Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= THESAURUSINCOMPLETE).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()))).ToList();
                  var lnsGeosf = GeoFac.CreateIntersectsSelector(lnsGeos);
                  foreach (var _segs in _linesGroup)
                  {
                    var TolLane = _segs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= THESAURUSINCOMPLETE).ToList();
                    var DEFAULT_FIRE_VALVE_LENGTH = GeoFac.CreateGeometry(TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()));
                    var buf = DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSPERMUTATION);
                    if (TolLane.Count > THESAURUSSTAMPEDE)
                    {
                      var _tol_avg_column_dist = sankakuptsf(buf);
                      if (_tol_avg_column_dist.Count > THESAURUSSTAMPEDE)
                      {
                        if (_tol_avg_column_dist.Any(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE) && _tol_avg_column_dist.Any(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDISREPUTABLE) && _tol_avg_column_dist.All(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE or THESAURUSDISREPUTABLE))
                        {
                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
                          {
                            ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = THESAURUSDEPLORE;
                          }
                        }
                        if (_tol_avg_column_dist.All(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE))
                        {
                          void patch()
                          {
                            foreach (var gpgeo in gpGeos)
                            {
                              foreach (var toilet_wells_interval in MaxDownspoutToBalconywashingfloordrain)
                              {
                                if (toilet_wells_interval.Intersects(gpgeo))
                                {
                                  {
                                    var MAX_CONDENSEPIPE_TO_WASHMACHINE = getDN(toilet_wells_interval.GetCenter().ToNTSPoint());
                                    if (MAX_CONDENSEPIPE_TO_WASHMACHINE is null) continue;
                                    foreach (var DEFAULT_FIRE_VALVE_LENGTH in lnsGeosf(new MultiLineString(GeoFac.GetLines(gpgeo, skipPolygon: THESAURUSOBSTINACY).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToArray()).Buffer(THESAURUSPERMUTATION)))
                                    {
                                      draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSPERMUTATION), overWrite: INTRAVASCULARLY);
                                    }
                                  }
                                  break;
                                }
                              }
                            }
                          }
                          patch();
                        }
                      }
                    }
                  }
                }
              });
              max_balcony_to_balcony_distance.Enqueue(THESAURUSCOMMUNICATION, () =>
              {
                var points = djPts.ToList();
                var pointsf = GeoFac.CreateIntersectsSelector(points);
                var linesGeos = _linesGroup.Select(tolReturnValueRange => new MultiLineString(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToArray())).ToList();
                var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                foreach (var bufGeo in GroupGeometries(_linesGroup.Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()))).ToList(), MaxDownspoutToBalconywashingfloordrain.Concat(fldrs).ToList()).Select(GeoFac.CreateGeometry).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSPERMUTATION)))
                {
                  if (portst(bufGeo) || ditchest(bufGeo) || wellst(bufGeo) || swellst(bufGeo))
                  {
                    max_balcony_to_balcony_distance.Enqueue(SUPERLATIVENESS, () =>
                                  {
                                    var target = portsf(bufGeo).FirstOrDefault() ?? MaxTagXposition(bufGeo).FirstOrDefault() ?? MaxTagLength(bufGeo).FirstOrDefault() ?? swellsf(bufGeo).FirstOrDefault();
                                    if (target is null) return;
                                    var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                                    foreach (var edPt in edPts)
                                    {
                                      var max_basecircle_area = Commonradius(bufGeo);
                                      if (max_basecircle_area.Count == THESAURUSSTAMPEDE) return;
                                      var pps = ppsf(bufGeo);
                                      if (pps.Count == THESAURUSSTAMPEDE) return;
                                      var tolReturnValueRange = linesGeosk(bufGeo);
                                      if (pps.All(ToiletWellsInterval => !ToiletWellsInterval.Intersects(GeoFac.CreateGeometry(tolReturnValueRange)))) return;
                                      draw(THESAURUSDEPLORE, bufGeo, overWrite: THESAURUSOBSTINACY);
                                      {
                                        var lnsf = GeoFac.CreateIntersectsSelector(GeoFac.GetManyLineStrings(tolReturnValueRange).ToList());
                                        if (pps.Any(ToiletWellsInterval => fl0s.Contains(ToiletWellsInterval)))
                                        {
                                          foreach (var fd in max_basecircle_area)
                                          {
                                            foreach (var maxBalconyToBalconyDistance in lnsf(fd))
                                            {
                                              var MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WaterWellFloorDrainDN;
                                              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = maxBalconyToBalconyDistance.GetCenter();
                                              draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                            }
                                          }
                                        }
                                        else
                                        {
                                          foreach (var fd in max_basecircle_area)
                                          {
                                            var lns = lnsf(fd);
                                            if (lns.Count == THESAURUSHOUSING)
                                            {
                                              var maxBalconyToBalconyDistance = lns[THESAURUSSTAMPEDE];
                                              string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                              if (fl0st(maxBalconyToBalconyDistance))
                                              {
                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WaterWellFloorDrainDN;
                                              }
                                              else if (nlst(maxBalconyToBalconyDistance))
                                              {
                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondenseFloorDrainDN;
                                              }
                                              else
                                              {
                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BalconyFloorDrainDN;
                                              }
                                              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = maxBalconyToBalconyDistance.GetCenter();
                                              draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                            }
                                          }
                                        }
                                        foreach (var ToiletWellsInterval in pps)
                                        {
                                          if (nls.Contains(ToiletWellsInterval) || y1ls.Contains(ToiletWellsInterval) || y2ls.Contains(ToiletWellsInterval) || fl0s.Contains(ToiletWellsInterval))
                                          {
                                            var lns = lnsf(ToiletWellsInterval);
                                            if (lns.Count == THESAURUSHOUSING)
                                            {
                                              var maxBalconyToBalconyDistance = lns[THESAURUSSTAMPEDE];
                                              string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                              if (nls.Contains(ToiletWellsInterval))
                                              {
                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondensePipeVerticalDN;
                                              }
                                              else
                                              {
                                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BalconyRainPipeDN;
                                              }
                                              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = maxBalconyToBalconyDistance.GetCenter();
                                              draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToGRect(INTROPUNITIVENESS).ToPolygon());
                                            }
                                          }
                                        }
                                      }
                                      static Func<Geometry, bool> CreateContainsTester<T>(List<T> DEFAULT_VOLTAGE) where T : Geometry
                                      {
                                        if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) return DEFAULT_FIRE_VALVE_WIDTH => INTRAVASCULARLY;
                                        var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(DEFAULT_VOLTAGE.Count > THESAURUSACRIMONIOUS ? DEFAULT_VOLTAGE.Count : THESAURUSACRIMONIOUS);
                                        foreach (var DEFAULT_FIRE_VALVE_LENGTH in DEFAULT_VOLTAGE) engine.Insert(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal, DEFAULT_FIRE_VALVE_LENGTH);
                                        return DEFAULT_FIRE_VALVE_LENGTH =>
                                                      {
                                                        if (DEFAULT_FIRE_VALVE_LENGTH == null) throw new ArgumentNullException();
                                                        var default_voltage = GeoFac.PreparedGeometryFactory.Create(DEFAULT_FIRE_VALVE_LENGTH);
                                                        return engine.Query(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal).Any(default_fire_valve_width => default_voltage.Contains(default_fire_valve_width));
                                                      };
                                      }
                                      max_balcony_to_balcony_distance.Enqueue(THESAURUSSCARCE, () =>
                                                    {
                                                      var TolLightRangeMin = CreateContainsTester(after.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2).ToList());
                                                      var _lines = linesGeosk(bufGeo).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Distinct().ToList();
                                                      var _tol_avg_column_dist = pointsf(bufGeo);
                                                      var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
                                                      var stPts = new HashSet<Point>();
                                                      var addPts = new HashSet<Point>();
                                                      {
                                                        foreach (var maxBalconyrainpipeToFloordrainDistance in MaxDownspoutToBalconywashingfloordrain.Concat(fldrs).Distinct())
                                                        {
                                                          var _pts = ptsf(maxBalconyrainpipeToFloordrainDistance.Buffer(THESAURUSPERMUTATION));
                                                          if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                                          if (_pts.Count == THESAURUSHOUSING)
                                                          {
                                                            stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                                          }
                                                          else
                                                          {
                                                            var maxKitchenToBalconyDistance = GetBounds(_pts.ToArray());
                                                            var center = maxKitchenToBalconyDistance.Center.ToNTSPoint();
                                                            addPts.Add(center);
                                                            foreach (var default_fire_valve_length in _pts.Select(TolLightRangeSingleSideMax => new GLineSegment(TolLightRangeSingleSideMax.ToPoint2d(), center.ToPoint2d())))
                                                            {
                                                              _lines.Add(default_fire_valve_length);
                                                            }
                                                          }
                                                        }
                                                        stPts.Remove(edPt);
                                                      }
                                                      if (stPts.Count == THESAURUSSTAMPEDE)
                                                      {
                                                        _lines = _lines.Except(GeoFac.CreateIntersectsSelector(_lines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList())(GeoFac.CreateGeometry(addPts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPoint2d().ToGRect(UNCONSEQUENTIAL).ToPolygon()))).SelectMany(DEFAULT_FIRE_VALVE_LENGTH => GeoFac.GetLines(DEFAULT_FIRE_VALVE_LENGTH))).ToList();
                                                        addPts.Clear();
                                                        var tolReturnValueRange = _lines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
                                                        var linesf = GeoFac.CreateIntersectsSelector(tolReturnValueRange);
                                                        foreach (var ToiletWellsInterval in y1ls.Concat(y2ls).Concat(nls).Concat(fl0s))
                                                        {
                                                          stPts.AddRange(ptsf(ToiletWellsInterval));
                                                          var lns = linesf(ToiletWellsInterval).Where(maxBalconyToBalconyDistance => after.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2).All(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.Contains(maxBalconyToBalconyDistance))).ToList();
                                                          foreach (var maxBalconyToBalconyDistance in lns)
                                                          {
                                                            string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                                            if (nls.Contains(ToiletWellsInterval))
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondensePipeVerticalDN;
                                                            }
                                                            else
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = IRRESPONSIBLENESS;
                                                            }
                                                            draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, maxBalconyToBalconyDistance.GetCenter().ToNTSPoint(), overWrite: INTRAVASCULARLY);
                                                          }
                                                        }
                                                        foreach (var fd in fldrs)
                                                        {
                                                          stPts.AddRange(ptsf(fd));
                                                          var lns = linesf(fd).Where(maxBalconyToBalconyDistance => after.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2).All(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.Contains(maxBalconyToBalconyDistance))).ToList();
                                                          foreach (var maxBalconyToBalconyDistance in lns)
                                                          {
                                                            string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                                            var buf = fd.Buffer(MISAPPREHENSIVE);
                                                            if (fl0st(buf))
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WaterWellFloorDrainDN;
                                                            }
                                                            else if (nlst(buf))
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.CondenseFloorDrainDN;
                                                            }
                                                            else
                                                            {
                                                              MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BalconyFloorDrainDN;
                                                            }
                                                            draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, maxBalconyToBalconyDistance.GetCenter().ToNTSPoint(), overWrite: INTRAVASCULARLY);
                                                          }
                                                        }
                                                      }
                                                      _tol_avg_column_dist = _tol_avg_column_dist.Concat(addPts).Distinct().ToList();
                                                      var mdPts = _tol_avg_column_dist.Except(stPts).Except(edPts).ToHashSet();
                                                      var nodes = _tol_avg_column_dist.Select(TolLightRangeSingleSideMax => new GraphNode<Point>(TolLightRangeSingleSideMax)).ToList();
                                                      {
                                                        var kvs = new HashSet<KeyValuePair<int, int>>();
                                                        foreach (var default_fire_valve_length in _lines)
                                                        {
                                                          var MAX_ANGEL_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.StartPoint.ToNTSPoint());
                                                          if (MAX_ANGEL_TOLLERANCE < THESAURUSSTAMPEDE) continue;
                                                          var MAX_ANGLE_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.EndPoint.ToNTSPoint());
                                                          if (MAX_ANGLE_TOLLERANCE < THESAURUSSTAMPEDE) continue;
                                                          if (MAX_ANGEL_TOLLERANCE != MAX_ANGLE_TOLLERANCE)
                                                          {
                                                            if (MAX_ANGEL_TOLLERANCE > MAX_ANGLE_TOLLERANCE)
                                                            {
                                                              ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref MAX_ANGEL_TOLLERANCE, ref MAX_ANGLE_TOLLERANCE);
                                                            }
                                                            kvs.Add(new KeyValuePair<int, int>(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE));
                                                          }
                                                        }
                                                        foreach (var max_rainpipe_to_balconyfloordrain in kvs)
                                                        {
                                                          nodes[max_rainpipe_to_balconyfloordrain.Key].AddNeighbour(nodes[max_rainpipe_to_balconyfloordrain.Value], THESAURUSHOUSING);
                                                        }
                                                      }
                                                      var dijkstra = new Dijkstra<Point>(nodes);
                                                      {
                                                        var paths = new List<IList<GraphNode<Point>>>(stPts.Count);
                                                        var dnDict = new Dictionary<GLineSegment, int>();
                                                        foreach (var stPt in stPts)
                                                        {
                                                          var path = dijkstra.FindShortestPathBetween(nodes[_tol_avg_column_dist.IndexOf(stPt)], nodes[_tol_avg_column_dist.IndexOf(edPt)]);
                                                          paths.Add(path);
                                                        }
                                                        foreach (var path in paths)
                                                        {
                                                          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                                          {
                                                            dnDict[new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                                          }
                                                        }
                                                        foreach (var path in paths)
                                                        {
                                                          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                                          {
                                                            var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                                            var sel = default_fire_valve_length.Buffer(THESAURUSHOUSING);
                                                            foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                                            {
                                                              var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                                              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                                              {
                                                                var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                                                dnDict[default_fire_valve_length] = Math.Max(DEFAULT_FIRE_VALVE_WIDTH, dnDict[default_fire_valve_length]);
                                                              }
                                                            }
                                                          }
                                                        }
                                                        foreach (var path in paths)
                                                        {
                                                          int MAX_CONDENSEPIPE_TO_WASHMACHINE = THESAURUSSTAMPEDE;
                                                          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                                          {
                                                            var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                                            var sel = default_fire_valve_length.Buffer(THESAURUSHOUSING);
                                                            foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                                            {
                                                              var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                                              if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                                              {
                                                                var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                                                if (MAX_CONDENSEPIPE_TO_WASHMACHINE < DEFAULT_FIRE_VALVE_WIDTH) MAX_CONDENSEPIPE_TO_WASHMACHINE = DEFAULT_FIRE_VALVE_WIDTH;
                                                              }
                                                            }
                                                            if (dnDict[default_fire_valve_length] < MAX_CONDENSEPIPE_TO_WASHMACHINE) dnDict[default_fire_valve_length] = MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                                          }
                                                        }
                                                        foreach (var max_rainpipe_to_balconyfloordrain in dnDict)
                                                        {
                                                          var sel = max_rainpipe_to_balconyfloordrain.Key.Buffer(THESAURUSHOUSING);
                                                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                                          {
                                                            var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                                            TolLightRangeSingleSideMin.Text = getDn(max_rainpipe_to_balconyfloordrain.Value);
                                                          }
                                                        }
                                                      }
                                                    });
                                    }
                                  });
                  }
                }
              });
            }
          }
          max_balcony_to_balcony_distance.Enqueue(THESAURUSDESTITUTE, () =>
          {
            foreach (var tp in after.OrderByDescending(tp => tp.Item2.Area))
            {
              draw(tp.Item1, tp.Item2, INTRAVASCULARLY);
            }
          });
          static List<List<Geometry>> GroupGeometries(List<Geometry> tolReturnValueRange, List<Geometry> polys)
          {
            var geosGroup = new List<List<Geometry>>();
            GroupGeometries();
            return geosGroup;
            void GroupGeometries()
            {
              var DEFAULT_VOLTAGE = tolReturnValueRange.Concat(polys).Distinct().ToList();
              if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) return;
              tolReturnValueRange = tolReturnValueRange.Distinct().ToList();
              polys = polys.Distinct().ToList();
              if (tolReturnValueRange.Count + polys.Count != DEFAULT_VOLTAGE.Count) throw new ArgumentException();
              var lineshs = tolReturnValueRange.ToHashSet();
              var polyhs = polys.ToHashSet();
              var pairs = _GroupGeometriesToKVIndex(DEFAULT_VOLTAGE).Where(max_rainpipe_to_balconyfloordrain =>
              {
                if (lineshs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Key]) && lineshs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Value])) return INTRAVASCULARLY;
                if (polyhs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Key]) && polyhs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Value])) return INTRAVASCULARLY;
                return THESAURUSOBSTINACY;
              }).ToArray();
              var dict = new ListDict<int>();
              var h = new BFSHelper()
              {
                Pairs = pairs,
                TotalCount = DEFAULT_VOLTAGE.Count,
                Callback = (default_fire_valve_width, MAX_ANGEL_TOLLERANCE) => dict.Add(default_fire_valve_width.root, MAX_ANGEL_TOLLERANCE),
              };
              h.BFS();
              dict.ForEach((_i, l) =>
              {
                geosGroup.Add(l.Select(MAX_ANGEL_TOLLERANCE => DEFAULT_VOLTAGE[MAX_ANGEL_TOLLERANCE]).ToList());
              });
            }
            static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex<T>(List<T> DEFAULT_VOLTAGE) where T : Geometry
            {
              if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) yield break;
              var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(DEFAULT_VOLTAGE.Count > THESAURUSACRIMONIOUS ? DEFAULT_VOLTAGE.Count : THESAURUSACRIMONIOUS);
              foreach (var DEFAULT_FIRE_VALVE_LENGTH in DEFAULT_VOLTAGE) engine.Insert(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal, DEFAULT_FIRE_VALVE_LENGTH);
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < DEFAULT_VOLTAGE.Count; MAX_ANGEL_TOLLERANCE++)
              {
                var DEFAULT_FIRE_VALVE_LENGTH = DEFAULT_VOLTAGE[MAX_ANGEL_TOLLERANCE];
                var default_voltage = GeoFac.PreparedGeometryFactory.Create(DEFAULT_FIRE_VALVE_LENGTH);
                foreach (var MAX_ANGLE_TOLLERANCE in engine.Query(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal).Where(default_fire_valve_width => default_voltage.Intersects(default_fire_valve_width)).Select(default_fire_valve_width => DEFAULT_VOLTAGE.IndexOf(default_fire_valve_width)).Where(MAX_ANGLE_TOLLERANCE => MAX_ANGEL_TOLLERANCE < MAX_ANGLE_TOLLERANCE))
                {
                  yield return new KeyValuePair<int, int>(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                }
              }
            }
          }
          max_balcony_to_balcony_distance.Enqueue(THESAURUSACRIMONIOUS, () =>
          {
            foreach (var TolLightRangeSingleSideMin in mlInfos)
            {
              if (TolLightRangeSingleSideMin.Text == PHOTOFLUOROGRAM) TolLightRangeSingleSideMin.Text = THESAURUSDEPLORE;
            }
          });
          max_balcony_to_balcony_distance.Enqueue(THESAURUSOCCASIONALLY, () =>
          {
            var points = precisePts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
            var pointsf = GeoFac.CreateIntersectsSelector(points);
            foreach (var TolLightRangeSingleSideMin in mlInfos)
            {
              if (TolLightRangeSingleSideMin.Text is not null)
              {
                var _tol_avg_column_dist = pointsf(TolLightRangeSingleSideMin.BasePoint.ToGRect(THESAURUSHOUSING).ToPolygon());
                var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist.FirstOrDefault();
                if (_tol_avg_column_dist.Count > THESAURUSHOUSING)
                {
                  MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = GeoFac.NearestNeighbourGeometryF(_tol_avg_column_dist)(TolLightRangeSingleSideMin.BasePoint.ToNTSPoint());
                }
                if (MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN is not null)
                {
                  TolLightRangeSingleSideMin.BasePoint = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint3d();
                }
              }
            }
          });
        }
      }
      foreach (var TolLightRangeSingleSideMin in mlInfos)
      {
        if (TolLightRangeSingleSideMin.Text == THESAURUSDEPLORE) TolLightRangeSingleSideMin.Text = THESAURUSIMPETUOUS;
      }
      foreach (var TolLightRangeSingleSideMin in mlInfos)
      {
        if (!string.IsNullOrWhiteSpace(TolLightRangeSingleSideMin.Text)) DrawMLeader(TolLightRangeSingleSideMin.Text, TolLightRangeSingleSideMin.BasePoint, TolLightRangeSingleSideMin.BasePoint.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
      }
    }
    const string MLeaderLayer = ARGENTIMACULATUS;
    public static MLeader DrawMLeader(string content, Point2d _tolRegroupMainYRange, Point2d _tolConnectSecPtRange)
    {
      var tolReturnValue0Approx = new MLeader();
      tolReturnValue0Approx.MText = new MText() { Contents = content, TextHeight = HYPERDISYLLABLE, ColorIndex = DISPENSABLENESS, };
      tolReturnValue0Approx.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
      tolReturnValue0Approx.ArrowSize = THESAURUSENTREPRENEUR;
      tolReturnValue0Approx.DoglegLength = THESAURUSSTAMPEDE;
      tolReturnValue0Approx.LandingGap = THESAURUSSTAMPEDE;
      tolReturnValue0Approx.ExtendLeaderToText = INTRAVASCULARLY;
      tolReturnValue0Approx.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
      tolReturnValue0Approx.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
      tolReturnValue0Approx.AddLeaderLine(_tolRegroupMainYRange.ToPoint3d());
      var maxKitchenToBalconyDistance = tolReturnValue0Approx.MText.Bounds.ToGRect();
      var _tolConnectSecPrimAddValue = _tolConnectSecPtRange.OffsetY(maxKitchenToBalconyDistance.Height + HYPERDISYLLABLE).ToPoint3d();
      if (_tolConnectSecPtRange.X < _tolRegroupMainYRange.X)
      {
        _tolConnectSecPrimAddValue = _tolConnectSecPrimAddValue.OffsetX(-maxKitchenToBalconyDistance.Width);
      }
      tolReturnValue0Approx.TextLocation = _tolConnectSecPrimAddValue;
      tolReturnValue0Approx.Layer = MLeaderLayer;
      DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(tolReturnValue0Approx); });
      return tolReturnValue0Approx;
    }
    public static MLeader DrawMLeader(string content, Point3d _tolRegroupMainYRange, Point3d _tolConnectSecPtRange)
    {
      var tolReturnValue0Approx = new MLeader();
      tolReturnValue0Approx.MText = new MText() { Contents = content, TextHeight = HYPERDISYLLABLE, ColorIndex = DISPENSABLENESS, };
      tolReturnValue0Approx.TextStyleId = GetTextStyleId(CONTROVERSIALLY);
      tolReturnValue0Approx.ArrowSize = THESAURUSENTREPRENEUR;
      tolReturnValue0Approx.DoglegLength = THESAURUSSTAMPEDE;
      tolReturnValue0Approx.LandingGap = THESAURUSSTAMPEDE;
      tolReturnValue0Approx.ExtendLeaderToText = INTRAVASCULARLY;
      tolReturnValue0Approx.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
      tolReturnValue0Approx.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
      tolReturnValue0Approx.AddLeaderLine(_tolRegroupMainYRange);
      var maxKitchenToBalconyDistance = tolReturnValue0Approx.MText.Bounds.ToGRect();
      var _tolConnectSecPrimAddValue = _tolConnectSecPtRange.OffsetY(maxKitchenToBalconyDistance.Height + HYPERDISYLLABLE);
      if (_tolConnectSecPtRange.X < _tolRegroupMainYRange.X)
      {
        _tolConnectSecPrimAddValue = _tolConnectSecPrimAddValue.OffsetX(-maxKitchenToBalconyDistance.Width);
      }
      tolReturnValue0Approx.TextLocation = _tolConnectSecPrimAddValue;
      tolReturnValue0Approx.Layer = MLeaderLayer;
      DrawingQueue.Enqueue(adb => { adb.ModelSpace.Add(tolReturnValue0Approx); });
      return tolReturnValue0Approx;
    }
    private static void ClearMLeader()
    {
      var adb = _DrawingTransaction.Current.adb;
      LayerTools.AddLayer(adb.Database, MLeaderLayer);
      foreach (var tolReturnValue0Approx in adb.ModelSpace.OfType<MLeader>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer == MLeaderLayer))
      {
        adb.Element<Entity>(tolReturnValue0Approx.ObjectId, THESAURUSOBSTINACY).Erase();
      }
    }
    public static Geometry CreateXGeoRect(GRect DEFAULT_FIRE_VALVE_WIDTH)
    {
      return new MultiLineString(new LineString[] {
                DEFAULT_FIRE_VALVE_WIDTH.ToLinearRing(),
                new LineString(new Coordinate[] { DEFAULT_FIRE_VALVE_WIDTH.LeftTop.ToNTSCoordinate(), DEFAULT_FIRE_VALVE_WIDTH.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { DEFAULT_FIRE_VALVE_WIDTH.LeftButtom.ToNTSCoordinate(), DEFAULT_FIRE_VALVE_WIDTH.RightTop.ToNTSCoordinate() })
            });
    }
    public static GRect GetBounds(params Geometry[] DEFAULT_VOLTAGE) => new GeometryCollection(DEFAULT_VOLTAGE).ToGRect();
    public static void DrawBackToFlatDiagram(List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> _storeysItems, ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData, List<ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageDrawingData> drDatas, ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo, DrainageSystemDiagramViewModel vm)
    {
      static string getDn(double area)
      {
        return DnToString(Math.Sqrt(area));
      }
      static string DnToString(double MAX_CONDENSEPIPE_TO_WASHMACHINE)
      {
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSSYMMETRICAL) return PHOTOFLUOROGRAM;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSDRAGOON) return PHOTOFLUOROGRAM;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < PERICARDIOCENTESIS) return PHOTOFLUOROGRAM;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSMORTALITY) return QUOTATIONBREWSTER;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSNEGATE) return QUOTATIONDOPPLER;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE < THESAURUSINNOCUOUS) return QUOTATIONDOPPLER;
        return IRRESPONSIBLENESS;
      }
      static double parseDn(string MAX_CONDENSEPIPE_TO_WASHMACHINE)
      {
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE.StartsWith(THESAURUSVICTORIOUS)) return THESAURUSENTREPRENEUR;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is THESAURUSJOURNAL) return THESAURUSEVERLASTING;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is CHRISTIANIZATION or UNPREMEDITATEDNESS) return HYPERDISYLLABLE;
        if (MAX_CONDENSEPIPE_TO_WASHMACHINE is THESAURUSFINICKY) return THESAURUSENTREPRENEUR;
        var tolReturnValueMinRange = Regex.Match(MAX_CONDENSEPIPE_TO_WASHMACHINE, SUPERNATURALIZE);
        if (tolReturnValueMinRange.Success) return double.Parse(tolReturnValueMinRange.Value);
        return THESAURUSSTAMPEDE;
      }
      var mlInfos = new List<MLeaderInfo>(THESAURUSREPERCUSSION);
      var tk = DateTime.Now.Ticks % THESAURUSFINALITY;
      var cadDatas = exInfo.CadDatas;
      Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
      Func<List<Geometry>, Func<Geometry, bool>> T = GeoFac.CreateIntersectsTester;
      Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
      Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
      static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(MinWellToUrinalDistance => GeoFac.CreateGeometry(MinWellToUrinalDistance)).ToList();
      var (sankakuptsf, addsankaku) = GeoFac.CreateIntersectsSelectorEngine(mlInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.BasePoint.ToNTSPoint(TolLightRangeSingleSideMax)));
      var roomData = geoData.RoomData;
      var _kitchens = roomData.Where(TolLightRangeSingleSideMax => IsKitchen(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _toilets = roomData.Where(TolLightRangeSingleSideMax => IsToilet(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _nonames = roomData.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key is THESAURUSDEPLORE).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _balconies = roomData.Where(TolLightRangeSingleSideMax => IsBalcony(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var kitchenst = T(_kitchens);
      void draw(string repeated_point_distance, Geometry DEFAULT_FIRE_VALVE_LENGTH, bool autoCreate = THESAURUSOBSTINACY, bool overWrite = THESAURUSOBSTINACY, string note = null)
      {
        Point2d center;
        if (DEFAULT_FIRE_VALVE_LENGTH is Point point)
        {
          center = point.ToPoint2d();
          DEFAULT_FIRE_VALVE_LENGTH = GRect.Create(center, UNCONSEQUENTIAL).ToPolygon();
        }
        else
        {
          center = DEFAULT_FIRE_VALVE_LENGTH.GetCenter();
        }
        {
          var SidewaterbucketXIndent = INTRAVASCULARLY;
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(DEFAULT_FIRE_VALVE_LENGTH))
          {
            var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
            if (string.IsNullOrWhiteSpace(TolLightRangeSingleSideMin.Text) || overWrite)
            {
              TolLightRangeSingleSideMin.Text = repeated_point_distance;
            }
            if (!string.IsNullOrWhiteSpace(note)) TolLightRangeSingleSideMin.Note = note;
            SidewaterbucketXIndent = THESAURUSOBSTINACY;
          }
          if (!SidewaterbucketXIndent && autoCreate)
          {
            var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = center;
            var TolLightRangeSingleSideMin = MLeaderInfo.Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, repeated_point_distance);
            mlInfos.Add(TolLightRangeSingleSideMin);
            var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint();
            _raiseDistanceToStartDefault.UserData = TolLightRangeSingleSideMin;
            if (!string.IsNullOrWhiteSpace(note)) TolLightRangeSingleSideMin.Note = note;
            addsankaku(_raiseDistanceToStartDefault);
          }
        }
      }
      var zbqst = GeoFac.CreateIntersectsTester(geoData.zbqs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList());
      var xstst = GeoFac.CreateIntersectsTester(geoData.xsts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList());
      var dnInfectors = new List<KeyValuePair<Point2d, string>>();
      var vpInfectors = new HashSet<Point2d>();
      var flpts = new HashSet<Point2d>();
      var plpts = new HashSet<Point2d>();
      var vp2fdpts = new HashSet<Point2d>();
      var shooters = new HashSet<Point>();
      {
        var circleshooters = new HashSet<KeyValuePair<Point2d, Point2d>>();
        var storeysItems = ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos;
        var toCmp = new HashSet<KeyValuePair<int, int>>();
        {
          var REGION_BORDER_BUFFE_RDISTANCE = storeysItems.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Any()).ToList();
          var minlst = REGION_BORDER_BUFFE_RDISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Min()).ToList();
          var maxlst = REGION_BORDER_BUFFE_RDISTANCE.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Numbers.Max()).ToList();
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < maxlst.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var _tolReturnValueMinRange = maxlst[MAX_ANGEL_TOLLERANCE];
            for (int MAX_ANGLE_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGLE_TOLLERANCE < maxlst.Count; MAX_ANGLE_TOLLERANCE++)
            {
              if (MAX_ANGLE_TOLLERANCE == MAX_ANGEL_TOLLERANCE) continue;
              var _tolReturnValueMaxDistance = minlst[MAX_ANGLE_TOLLERANCE];
              if (_tolReturnValueMaxDistance + THESAURUSHOUSING == _tolReturnValueMinRange)
              {
                toCmp.Add(new KeyValuePair<int, int>(storeysItems.IndexOf(REGION_BORDER_BUFFE_RDISTANCE[MAX_ANGLE_TOLLERANCE]), storeysItems.IndexOf(REGION_BORDER_BUFFE_RDISTANCE[MAX_ANGEL_TOLLERANCE])));
              }
            }
          }
        }
        var _shooters = geoData.FloorDrainTypeShooter.SelectNotNull(max_rainpipe_to_balconyfloordrain =>
        {
          var toilet_buffer_distance = max_rainpipe_to_balconyfloordrain.Value;
          if (!string.IsNullOrWhiteSpace(toilet_buffer_distance))
          {
            if (toilet_buffer_distance.Contains(THESAURUSRESIGNED) || toilet_buffer_distance.Contains(PHOTOAUTOTROPHIC))
            {
              return max_rainpipe_to_balconyfloordrain.Key.ToNTSPoint();
            }
          }
          return null;
        }).ToList();
        shooters.AddRange(_shooters);
        var _shootersf = GeoFac.CreateIntersectsSelector(_shooters);
        foreach (var _kv in toCmp)
        {
          var lbdict = exInfo.Items[_kv.Value].LabelDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2);
          var low = storeysItems[_kv.Key];
          var high = storeysItems[_kv.Value];
          var highBound = high.Boundary.ToPolygon();
          var max_basecircle_area = geoData.FloorDrains.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS, THESAURUSOBSTINACY)).ToList();
          var Commonradius = GeoFac.CreateEnvelopeSelector(max_basecircle_area);
          max_basecircle_area = Commonradius(high.Boundary.ToPolygon());
          var pps = geoData.VerticalPipes.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList();
          var ppsf = GeoFac.CreateEnvelopeSelector(pps);
          pps = ppsf(low.Boundary.ToPolygon());
          var dps = geoData.DownWaterPorts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Center.ToGRect(HYPERDISYLLABLE).ToPolygon()).ToList();
          var dpsf = GeoFac.CreateEnvelopeSelector(dps);
          dps = dpsf(high.Boundary.ToPolygon());
          var circles = geoData.VerticalPipes.Concat(geoData.DownWaterPorts).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList();
          var circlesf = GeoFac.CreateEnvelopeSelector(circles);
          circles = circlesf(low.Boundary.ToPolygon());
          var vhigh = -high.ContraPoint.ToVector2d();
          var vlow = -low.ContraPoint.ToVector2d();
          var _dps = dps.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vhigh)).ToList();
          var _circles = circles.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vlow)).ToList();
          var _circlesf = GeoFac.CreateIntersectsSelector(_circles);
          var _fds = max_basecircle_area.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vhigh)).ToList();
          var _pps = pps.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(vlow)).ToList();
          var _ppsf = GeoFac.CreateIntersectsSelector(_pps);
          var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = low.ContraPoint - high.ContraPoint;
          {
            shooters.AddRange(_shootersf(highBound).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)));
          }
          {
            var text_indent = lbdict.Where(TolLightRangeSingleSideMax => IsDraiFL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            var max_tag_xposition = lbdict.Where(TolLightRangeSingleSideMax => IsPL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            var max_tag_length = lbdict.Where(TolLightRangeSingleSideMax => IsTL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
            foreach (var ToiletWellsInterval in text_indent.Concat(max_tag_xposition).Concat(max_tag_length))
            {
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = ToiletWellsInterval.GetCenter().Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              dnInfectors.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, IRRESPONSIBLENESS));
              vpInfectors.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
              if (text_indent.Contains(ToiletWellsInterval)) flpts.Add(ToiletWellsInterval.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              else if (max_tag_xposition.Contains(ToiletWellsInterval)) plpts.Add(ToiletWellsInterval.GetCenter() + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
            }
          }
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in dps.Where(dp => kitchenst(dp)).Select(dp => dp.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).GetCenter()))
          {
            dnInfectors.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, vm.Params.BasinDN));
          }
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in dps.Where(dp => zbqst(dp)).Select(dp => dp.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).GetCenter()))
          {
            dnInfectors.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, IRRESPONSIBLENESS));
          }
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in dps.Where(dp => xstst(dp)).Select(dp => dp.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).GetCenter()))
          {
            dnInfectors.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, QUOTATIONBREWSTER));
          }
          foreach (var fd in _fds)
          {
            var ToiletWellsInterval = _ppsf(fd.GetCenter().ToNTSPoint()).FirstOrDefault();
            if (ToiletWellsInterval != null)
            {
              vp2fdpts.Add(pps[_pps.IndexOf(ToiletWellsInterval)].GetCenter());
            }
          }
          foreach (var dp in _dps)
          {
            var circle = _circlesf(dp).FirstOrDefault();
            if (circle != null)
            {
              circleshooters.Add(new KeyValuePair<Point2d, Point2d>(dps[_dps.IndexOf(dp)].GetCenter(), circles[_circles.IndexOf(circle)].GetCenter()));
            }
          }
        }
      }
      var shooterst = GeoFac.CreateIntersectsTester(shooters.ToList());
      var vp2fdptst = GeoFac.CreateIntersectsTester(vp2fdpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList());
      var vp2fdptrgs = vp2fdpts.Select(TolLightRangeSingleSideMax => GRect.Create(TolLightRangeSingleSideMax, THESAURUSHESITANCY).ToPolygon()).ToList();
      var vp2fdptrgst = GeoFac.CreateIntersectsTester(vp2fdptrgs);
      {
        using var max_balcony_to_balcony_distance = new PriorityQueue(THESAURUSINCOMPLETE);
        var cleaningPortPtRgs = geoData.CleaningPortBasePoints.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGRect(THESAURUSINCOMPLETE).ToPolygon()).ToList();
        var cleaningPortPtsRgst = GeoFac.CreateIntersectsTester(cleaningPortPtRgs);
        foreach (var si in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count))
        {
          var REPEATED_POINT_DISTANCE = cadDatas[si];
          var lbdict = exInfo.Items[si].LabelDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2);
          var labelLinesGroup = GG(REPEATED_POINT_DISTANCE.LabelLines);
          var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
          var labellinesGeosf = F(labelLinesGeos);
          var tolGroupBlkLane = geoData.Storeys[si].ToPolygon();
          var gpGeos = GeoFac.CreateEnvelopeSelector(geoData.Groups.Select(GeoFac.CreateGeometry).ToList())(tolGroupBlkLane);
          var dlsegs = geoData.DLines;
          var vertices = GeoFac.CreateEnvelopeSelector(dlsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.StartPoint.ToNTSPoint().Tag(TolLightRangeSingleSideMax)).Concat(dlsegs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.EndPoint.ToNTSPoint().Tag(TolLightRangeSingleSideMax))).ToList())(tolGroupBlkLane);
          var verticesf = GeoFac.CreateIntersectsSelector(vertices);
          dlsegs = vertices.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData).Cast<GLineSegment>().Distinct().ToList();
          var killer = REPEATED_POINT_DISTANCE.VerticalPipes.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(-UNCONSEQUENTIAL)).Concat(REPEATED_POINT_DISTANCE.DownWaterPorts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(-UNCONSEQUENTIAL))).Concat(REPEATED_POINT_DISTANCE.FloorDrains.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(-UNCONSEQUENTIAL)));
          var dlinesGeos = GeoFac.GroupLinesByConnPoints(Substract(REPEATED_POINT_DISTANCE.DLines.SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).ToList(), killer).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList(), DINOFLAGELLATES).ToList();
          var precisePts = new HashSet<Point2d>();
          dlinesGeos = dlinesGeos.SelectMany(dlinesGeo =>
          {
            var tolReturnValueRange = Substract(GeoFac.GetLines(dlinesGeo).ToList(), killer);
            tolReturnValueRange = GeoFac.ToNodedLineSegments(tolReturnValueRange).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= DISPROPORTIONAL).ToList();
            tolReturnValueRange = GeoFac.GroupParallelLines(tolReturnValueRange, THESAURUSAPPARATUS, THESAURUSHOUSING).Select(default_fire_valve_width => GetCenterLine(default_fire_valve_width)).ToList();
            tolReturnValueRange = GeoFac.ToNodedLineSegments(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(DISPROPORTIONAL)).ToList()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSCOMMUNICATION).ToList();
            precisePts.AddRange(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Center));
            return GeoFac.GroupGeometries(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSPLAYGROUND).ToLineString()).ToList()).Select(TolLightRangeSingleSideMax =>
                      {
                        var lsArr = TolLightRangeSingleSideMax.Cast<LineString>().SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Distinct(new GLineSegment.EqualityComparer(ULTRASONOGRAPHY)).Select(TolLightRangeSingleSideMax => new GLineSegment(new Point2d(FixValue(TolLightRangeSingleSideMax.StartPoint.X), FixValue(TolLightRangeSingleSideMax.StartPoint.Y)), new Point2d(FixValue(TolLightRangeSingleSideMax.EndPoint.X), FixValue(TolLightRangeSingleSideMax.EndPoint.Y))).ToLineString()).ToArray();
                        return new MultiLineString(lsArr);
                      });
          }).Cast<Geometry>().ToList();
          var dlinesGeosf = F(dlinesGeos);
          var wrappingPipesf = F(REPEATED_POINT_DISTANCE.WrappingPipes);
          var dps = REPEATED_POINT_DISTANCE.DownWaterPorts;
          var ports = REPEATED_POINT_DISTANCE.WaterPorts;
          var portst = T(ports.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(MISAPPREHENSIVE)).ToList());
          var portsf = F(ports.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(MISAPPREHENSIVE)).ToList());
          var fldrs = REPEATED_POINT_DISTANCE.FloorDrains;
          var MaxDownspoutToBalconywashingfloordrain = REPEATED_POINT_DISTANCE.VerticalPipes;
          {
            var _vps = MaxDownspoutToBalconywashingfloordrain.ToHashSet();
            foreach (var toilet_wells_interval in _vps.ToList())
            {
              lbdict.TryGetValue(toilet_wells_interval, out string SidewaterbucketYIndent);
              if (!IsDraiLabel(SidewaterbucketYIndent))
              {
                if (kitchenst(toilet_wells_interval))
                {
                  dps.Add(toilet_wells_interval);
                  _vps.Remove(toilet_wells_interval);
                }
              }
            }
            dps = dps.Distinct().ToList();
            MaxDownspoutToBalconywashingfloordrain = _vps.ToList();
          }
          {
            var max_basecircle_area = MaxDownspoutToBalconywashingfloordrain.Where(vp2fdptst).ToList();
            fldrs = fldrs.Concat(max_basecircle_area).Distinct().ToList();
            MaxDownspoutToBalconywashingfloordrain = MaxDownspoutToBalconywashingfloordrain.Except(max_basecircle_area).ToList();
          }
          var Commonradius = F(fldrs);
          var fdst = T(fldrs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSPERMUTATION)).ToList());
          var ppsf = F(MaxDownspoutToBalconywashingfloordrain);
          var ppst = T(MaxDownspoutToBalconywashingfloordrain);
          var dpsf = F(dps);
          var dpst = T(dps);
          var text_indent = lbdict.Where(TolLightRangeSingleSideMax => IsDraiFL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
          var max_tag_xposition = lbdict.Where(TolLightRangeSingleSideMax => IsPL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
          var max_tag_length = lbdict.Where(TolLightRangeSingleSideMax => IsTL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
          {
            var MaxRainpipeToWashmachine = F(MaxDownspoutToBalconywashingfloordrain);
            var _fls = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((flpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
            var _pls = MaxRainpipeToWashmachine(GeoFac.CreateGeometry((plpts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()))));
            {
              var default_voltage = G(REPEATED_POINT_DISTANCE.LabelLines).ToIPreparedGeometry();
              text_indent = text_indent.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
              max_tag_xposition = max_tag_xposition.Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax)).ToList();
            }
            {
              text_indent = text_indent.Concat(_fls).Distinct().ToList();
              max_tag_xposition = max_tag_xposition.Concat(_pls).Distinct().ToList();
            }
          }
          if (cleaningPortPtRgs.Count > THESAURUSSTAMPEDE)
          {
            foreach (var ToiletWellsInterval in MaxDownspoutToBalconywashingfloordrain)
            {
              if (cleaningPortPtsRgst(ToiletWellsInterval))
              {
                max_tag_xposition.Add(ToiletWellsInterval);
                if (!lbdict.ContainsKey(ToiletWellsInterval))
                {
                  lbdict[ToiletWellsInterval] = THESAURUSBOTTOM + ++tk;
                }
              }
            }
            max_tag_xposition = max_tag_xposition.Distinct().ToList();
          }
          foreach (var default_fire_valve_length in REPEATED_POINT_DISTANCE.VLines.SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSCOMMUNICATION))
          {
            draw(IRRESPONSIBLENESS, default_fire_valve_length.ToLineString());
          }
          var djPts = new HashSet<Point>();
          var _linesGroup = new HashSet<HashSet<GLineSegment>>();
          max_balcony_to_balcony_distance.Enqueue(THESAURUSPERMUTATION, () =>
          {
            var dlscf = GeoFac.CreateContainsSelector(dlinesGeos);
            foreach (var sel in GeoFac.GroupGeometries(fldrs.Select(TolLightRangeSingleSideMax => CreateXGeoRect(TolLightRangeSingleSideMax.Buffer(THESAURUSPERMUTATION).ToGRect())).Concat(dlinesGeos).ToList()).Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax)))
            {
              var max_basecircle_area = Commonradius(sel);
              if (max_basecircle_area.Count == THESAURUSPERMUTATION)
              {
                var DEFAULT_FIRE_VALVE_WIDTH = GetBounds(max_basecircle_area.ToArray());
                var dls = dlscf(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon());
                if (dls.Count == THESAURUSHOUSING)
                {
                  max_balcony_to_balcony_distance.Enqueue(THESAURUSPERMUTATION, () =>
                            {
                              string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                              if (shooterst(GeoFac.CreateGeometryEx(max_basecircle_area)))
                              {
                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WashingMachineFloorDrainDN;
                              }
                              else
                              {
                                MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.OtherFloorDrainDN;
                              }
                              foreach (var dl in dlscf(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()))
                              {
                                draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, dl.Buffer(THESAURUSPERMUTATION));
                              }
                              var fdst = T(max_basecircle_area);
                              foreach (var dl in dlinesGeosf(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon()).Except(dls).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).Where(TolLightRangeSingleSideMax => fdst(TolLightRangeSingleSideMax)))
                              {
                                var _dn = MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                if (_dn == QUOTATIONBREWSTER) _dn = QUOTATIONDOPPLER;
                                draw(_dn, dl.Buffer(THESAURUSPERMUTATION));
                              }
                              {
                              }
                            });
                }
              }
            }
          });
          foreach (var dlinesGeo in dlinesGeos)
          {
            var tolReturnValueRange = GeoFac.GetLines(dlinesGeo);
            _linesGroup.Add(tolReturnValueRange.ToHashSet());
            max_balcony_to_balcony_distance.Enqueue(THESAURUSCENSURE, () =>
            {
              var flst = T(text_indent);
              var plst = T(max_tag_xposition);
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(dlinesGeo.Buffer(THESAURUSHOUSING)))
              {
                var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                if (TolLightRangeSingleSideMin.Text is THESAURUSDEPLORE)
                {
                  if (flst(dlinesGeo) || plst(dlinesGeo))
                  {
                    TolLightRangeSingleSideMin.Text = IRRESPONSIBLENESS;
                  }
                }
              }
            });
            var linesf = GeoFac.CreateIntersectsSelector(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList());
            foreach (var default_fire_valve_length in tolReturnValueRange)
            {
              if (default_fire_valve_length.Length < DINOFLAGELLATES) continue;
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = default_fire_valve_length.Center;
              var TolLightRangeSingleSideMin = MLeaderInfo.Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, THESAURUSDEPLORE);
              mlInfos.Add(TolLightRangeSingleSideMin);
              var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint();
              _raiseDistanceToStartDefault.UserData = TolLightRangeSingleSideMin;
              addsankaku(_raiseDistanceToStartDefault);
            }
            max_balcony_to_balcony_distance.Enqueue(INTROPUNITIVENESS, () =>
            {
              var dlinesGeoBuf = dlinesGeo.Buffer(THESAURUSACRIMONIOUS);
              var dlbufgf = dlinesGeoBuf.ToIPreparedGeometry();
              if (fdst(dlinesGeo) || ppst(dlinesGeo) || dpst(dlinesGeo))
              {
                foreach (var TolUniformSideLenth in tolReturnValueRange)
                {
                  foreach (var dp in dpsf(TolUniformSideLenth.ToLineString()))
                  {
                    {
                      var MAX_CONDENSEPIPE_TO_WASHMACHINE = dnInfectors.FirstOrDefault(max_rainpipe_to_balconyfloordrain => dp.Intersects(max_rainpipe_to_balconyfloordrain.Key.ToGRect(HYPERDISYLLABLE).ToPolygon())).Value;
                      if (!string.IsNullOrEmpty(MAX_CONDENSEPIPE_TO_WASHMACHINE))
                      {
                        draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                        continue;
                      }
                    }
                    if (zbqst(dp))
                    {
                      var MAX_CONDENSEPIPE_TO_WASHMACHINE = IRRESPONSIBLENESS;
                      draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                    }
                    else if (xstst(dp))
                    {
                      var MAX_CONDENSEPIPE_TO_WASHMACHINE = QUOTATIONBREWSTER;
                      draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                    }
                    else
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolUniformSideLenth.Center;
                      if (kitchenst(dp))
                      {
                        var MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BasinDN;
                        draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                      }
                      else
                      {
                        var MAX_CONDENSEPIPE_TO_WASHMACHINE = QUOTATIONBREWSTER;
                        draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon());
                      }
                    }
                  }
                  foreach (var fd in Commonradius(TolUniformSideLenth.ToLineString()))
                  {
                    string MAX_CONDENSEPIPE_TO_WASHMACHINE;
                    if (shooterst(fd))
                    {
                      MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.WashingMachineFloorDrainDN;
                    }
                    else
                    {
                      MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.OtherFloorDrainDN;
                    }
                    draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolUniformSideLenth.Center.Expand(THESAURUSACRIMONIOUS).ToGRect().ToPolygon(), overWrite: INTRAVASCULARLY);
                  }
                }
              }
              max_balcony_to_balcony_distance.Enqueue(QUOTATIONEDIBLE, () =>
                          {
                            var _pts = tolReturnValueRange.SelectMany(default_fire_valve_length => new Point2d[] { default_fire_valve_length.StartPoint, default_fire_valve_length.EndPoint }).GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).Distinct().Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => dlbufgf.Intersects(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint())).ToList();
                            var _tol_avg_column_dist = _pts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
                            djPts.AddRange(_tol_avg_column_dist);
                            _linesGroup.Add(tolReturnValueRange.ToHashSet());
                            return;
                            var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
                            var stPts = _tol_avg_column_dist.Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => fdst(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN) || dpst(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)).ToList();
                            var edPts = _tol_avg_column_dist.Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => portst(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)).ToList();
                            if (edPts.Count == THESAURUSSTAMPEDE)
                            {
                              var pps = ppsf(dlinesGeo);
                              if (pps.Count == THESAURUSHOUSING)
                              {
                                edPts = _tol_avg_column_dist.Where(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => ppst(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)).ToList();
                              }
                              else if (pps.Count > THESAURUSHOUSING)
                              {
                                if (!portst(dlinesGeo))
                                {
                                  foreach (var TolUniformSideLenth in tolReturnValueRange)
                                  {
                                    if (TolUniformSideLenth.Length >= THESAURUSINHERIT && TolUniformSideLenth.IsHorizontalOrVertical(THESAURUSCOMMUNICATION))
                                    {
                                      var defaultFireValveWidth = TolUniformSideLenth.ToLineString();
                                      foreach (var ToiletWellsInterval in pps)
                                      {
                                        if (defaultFireValveWidth.Intersects(ToiletWellsInterval))
                                        {
                                          edPts.AddRange(ptsf(ToiletWellsInterval));
                                        }
                                      }
                                    }
                                  }
                                }
                              }
                            }
                            max_balcony_to_balcony_distance.Enqueue(THESAURUSCOMMUNICATION, () =>
                                        {
                                          return;
                                        });
                            max_balcony_to_balcony_distance.Enqueue(SUPERLATIVENESS, () =>
                                        {
                                          return;
                                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
                                          {
                                          }
                                        });
                            max_balcony_to_balcony_distance.Enqueue(THESAURUSDESTITUTE, () =>
                                        {
                                          return;
                                          if (edPts.Count == THESAURUSHOUSING)
                                          {
                                            var edPt = edPts[THESAURUSSTAMPEDE];
                                            var mdPts = _tol_avg_column_dist.Except(stPts).Except(edPts).ToList();
                                          }
                                          else
                                          {
                                          }
                                        });
                          });
            });
          }
          static List<List<Geometry>> GroupGeometries(List<Geometry> tolReturnValueRange, List<Geometry> polys)
          {
            var geosGroup = new List<List<Geometry>>();
            GroupGeometries();
            return geosGroup;
            void GroupGeometries()
            {
              var DEFAULT_VOLTAGE = tolReturnValueRange.Concat(polys).Distinct().ToList();
              if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) return;
              tolReturnValueRange = tolReturnValueRange.Distinct().ToList();
              polys = polys.Distinct().ToList();
              if (tolReturnValueRange.Count + polys.Count != DEFAULT_VOLTAGE.Count) throw new ArgumentException();
              var lineshs = tolReturnValueRange.ToHashSet();
              var polyhs = polys.ToHashSet();
              var pairs = _GroupGeometriesToKVIndex(DEFAULT_VOLTAGE).Where(max_rainpipe_to_balconyfloordrain =>
              {
                if (lineshs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Key]) && lineshs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Value])) return INTRAVASCULARLY;
                if (polyhs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Key]) && polyhs.Contains(DEFAULT_VOLTAGE[max_rainpipe_to_balconyfloordrain.Value])) return INTRAVASCULARLY;
                return THESAURUSOBSTINACY;
              }).ToArray();
              var dict = new ListDict<int>();
              var h = new BFSHelper()
              {
                Pairs = pairs,
                TotalCount = DEFAULT_VOLTAGE.Count,
                Callback = (default_fire_valve_width, MAX_ANGEL_TOLLERANCE) => dict.Add(default_fire_valve_width.root, MAX_ANGEL_TOLLERANCE),
              };
              h.BFS();
              dict.ForEach((_i, l) =>
              {
                geosGroup.Add(l.Select(MAX_ANGEL_TOLLERANCE => DEFAULT_VOLTAGE[MAX_ANGEL_TOLLERANCE]).ToList());
              });
            }
            static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex<T>(List<T> DEFAULT_VOLTAGE) where T : Geometry
            {
              if (DEFAULT_VOLTAGE.Count == THESAURUSSTAMPEDE) yield break;
              var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(DEFAULT_VOLTAGE.Count > THESAURUSACRIMONIOUS ? DEFAULT_VOLTAGE.Count : THESAURUSACRIMONIOUS);
              foreach (var DEFAULT_FIRE_VALVE_LENGTH in DEFAULT_VOLTAGE) engine.Insert(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal, DEFAULT_FIRE_VALVE_LENGTH);
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < DEFAULT_VOLTAGE.Count; MAX_ANGEL_TOLLERANCE++)
              {
                var DEFAULT_FIRE_VALVE_LENGTH = DEFAULT_VOLTAGE[MAX_ANGEL_TOLLERANCE];
                var default_voltage = GeoFac.PreparedGeometryFactory.Create(DEFAULT_FIRE_VALVE_LENGTH);
                foreach (var MAX_ANGLE_TOLLERANCE in engine.Query(DEFAULT_FIRE_VALVE_LENGTH.EnvelopeInternal).Where(default_fire_valve_width => default_voltage.Intersects(default_fire_valve_width)).Select(default_fire_valve_width => DEFAULT_VOLTAGE.IndexOf(default_fire_valve_width)).Where(MAX_ANGLE_TOLLERANCE => MAX_ANGEL_TOLLERANCE < MAX_ANGLE_TOLLERANCE))
                {
                  yield return new KeyValuePair<int, int>(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                }
              }
            }
          }
          max_balcony_to_balcony_distance.Enqueue(THESAURUSCOMMUNICATION, () =>
          {
            var TolLightRangeMin = GeoFac.CreateIntersectsTester(vpInfectors.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList());
            var tolReturnValueRange = _linesGroup.SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
            var linesf = GeoFac.CreateIntersectsSelector(tolReturnValueRange);
            {
              foreach (var ToiletWellsInterval in Enumerable.Range(THESAURUSSTAMPEDE, cadDatas.Count).Select(si => exInfo.Items[si].LabelDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item1, TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Item2)).SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax)
                        .Where(TolLightRangeSingleSideMax => IsDraiFL(TolLightRangeSingleSideMax.Value) || IsPL(TolLightRangeSingleSideMax.Value) || IsTL(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).Concat(MaxDownspoutToBalconywashingfloordrain.Where(TolLightRangeMin)).Distinct())
              {
                var TolLane = linesf(ToiletWellsInterval.Buffer(THESAURUSHOUSING)).SelectMany(defaultFireValveWidth => GeoFac.GetLines(defaultFireValveWidth)).Distinct().ToList();
                if (TolLane.Count == THESAURUSHOUSING)
                {
                  var MAX_CONDENSEPIPE_TO_WASHMACHINE = IRRESPONSIBLENESS;
                  draw(MAX_CONDENSEPIPE_TO_WASHMACHINE, TolLane[THESAURUSSTAMPEDE].Center.ToGRect(INTROPUNITIVENESS).ToPolygon(), INTRAVASCULARLY, INTRAVASCULARLY, QUOTATIONPELVIC);
                }
              }
            }
          });
          max_balcony_to_balcony_distance.Enqueue(SUPERLATIVENESS, () =>
          {
            var points = djPts.ToList();
            var pointsf = GeoFac.CreateIntersectsSelector(points);
            var linesGeos = _linesGroup.Select(tolReturnValueRange => new MultiLineString(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToArray())).ToList();
            var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
            foreach (var bufGeo in GroupGeometries(_linesGroup.Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()))).ToList(), MaxDownspoutToBalconywashingfloordrain.Concat(fldrs).Concat(dps).ToList()).Select(GeoFac.CreateGeometry).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(THESAURUSPERMUTATION)))
            {
              if (portst(bufGeo))
              {
                max_balcony_to_balcony_distance.Enqueue(THESAURUSDESTITUTE, () =>
                          {
                            var target = portsf(bufGeo).FirstOrDefault();
                            if (target is null) return;
                            var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                            var edPt = edPts.FirstOrDefault();
                            if (edPt is null) return;
                            var _lines = linesGeosk(bufGeo).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Distinct().ToList();
                            var _tol_avg_column_dist = pointsf(bufGeo);
                            var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
                            var stPts = new HashSet<Point>();
                            var addPts = new HashSet<Point>();
                            {
                              foreach (var maxBalconyrainpipeToFloordrainDistance in MaxDownspoutToBalconywashingfloordrain.Concat(fldrs).Concat(dps).Distinct())
                              {
                                var _pts = ptsf(maxBalconyrainpipeToFloordrainDistance.Buffer(THESAURUSPERMUTATION));
                                if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                if (_pts.Count == THESAURUSHOUSING)
                                {
                                  stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                }
                                else
                                {
                                  var maxKitchenToBalconyDistance = GetBounds(_pts.ToArray());
                                  var center = maxKitchenToBalconyDistance.Center.ToNTSPoint();
                                  addPts.Add(center);
                                  foreach (var default_fire_valve_length in _pts.Select(TolLightRangeSingleSideMax => new GLineSegment(TolLightRangeSingleSideMax.ToPoint2d(), center.ToPoint2d())))
                                  {
                                    _lines.Add(default_fire_valve_length);
                                    if (text_indent.Contains(maxBalconyrainpipeToFloordrainDistance) || max_tag_xposition.Contains(maxBalconyrainpipeToFloordrainDistance))
                                    {
                                      draw(IRRESPONSIBLENESS, default_fire_valve_length.Center.ToNTSPoint());
                                      max_balcony_to_balcony_distance.Enqueue(THESAURUSBEATIFIC, () =>
                                                {
                                                  draw(null, default_fire_valve_length.Center.ToNTSPoint());
                                                });
                                    }
                                  }
                                }
                              }
                              stPts.Remove(edPt);
                            }
                            _tol_avg_column_dist = _tol_avg_column_dist.Concat(addPts).Distinct().ToList();
                            var mdPts = _tol_avg_column_dist.Except(stPts).Except(edPts).ToHashSet();
                            foreach (var dp in dpsf(bufGeo).Where(kitchenst))
                            {
                            }
                            var nodes = _tol_avg_column_dist.Select(TolLightRangeSingleSideMax => new GraphNode<Point>(TolLightRangeSingleSideMax)).ToList();
                            {
                              var kvs = new HashSet<KeyValuePair<int, int>>();
                              foreach (var default_fire_valve_length in _lines)
                              {
                                var MAX_ANGEL_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.StartPoint.ToNTSPoint());
                                var MAX_ANGLE_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.EndPoint.ToNTSPoint());
                                if (MAX_ANGEL_TOLLERANCE != MAX_ANGLE_TOLLERANCE)
                                {
                                  if (MAX_ANGEL_TOLLERANCE > MAX_ANGLE_TOLLERANCE)
                                  {
                                    ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref MAX_ANGEL_TOLLERANCE, ref MAX_ANGLE_TOLLERANCE);
                                  }
                                  kvs.Add(new KeyValuePair<int, int>(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE));
                                }
                              }
                              foreach (var max_rainpipe_to_balconyfloordrain in kvs)
                              {
                                nodes[max_rainpipe_to_balconyfloordrain.Key].AddNeighbour(nodes[max_rainpipe_to_balconyfloordrain.Value], THESAURUSHOUSING);
                              }
                            }
                            var dijkstra = new Dijkstra<Point>(nodes);
                            {
                              var paths = new List<IList<GraphNode<Point>>>(stPts.Count);
                              var areaDict = new Dictionary<GLineSegment, double>();
                              foreach (var stPt in stPts)
                              {
                                var path = dijkstra.FindShortestPathBetween(nodes[_tol_avg_column_dist.IndexOf(stPt)], nodes[_tol_avg_column_dist.IndexOf(edPt)]);
                                if (path.Count == THESAURUSSTAMPEDE) continue;
                                paths.Add(path);
                              }
                              foreach (var path in paths)
                              {
                                for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                {
                                  areaDict[new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                }
                              }
                              foreach (var path in paths)
                              {
                                for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                {
                                  var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                  var sel = default_fire_valve_length.Buffer(THESAURUSHOUSING);
                                  foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                  {
                                    var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                    if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                    {
                                      var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                      areaDict[default_fire_valve_length] += DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH;
                                    }
                                  }
                                }
                              }
                              foreach (var path in paths)
                              {
                                double area = THESAURUSSTAMPEDE;
                                for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                {
                                  var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                  var sel = default_fire_valve_length.Buffer(THESAURUSHOUSING);
                                  foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                  {
                                    var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                    if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                    {
                                      var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                      if (area < DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH) area = DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH;
                                    }
                                  }
                                  if (areaDict[default_fire_valve_length] < area) areaDict[default_fire_valve_length] = area;
                                }
                              }
                              {
                                var dict = areaDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key, TolLightRangeSingleSideMax => QUOTATIONTRANSFERABLE);
                                foreach (var path in paths)
                                {
                                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                  {
                                    var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                    dict[default_fire_valve_length] += areaDict[default_fire_valve_length];
                                  }
                                }
                                areaDict = dict;
                              }
                              foreach (var max_rainpipe_to_balconyfloordrain in areaDict)
                              {
                                var sel = max_rainpipe_to_balconyfloordrain.Key.Buffer(THESAURUSHOUSING);
                                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                {
                                  var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                  TolLightRangeSingleSideMin.Text = getDn(max_rainpipe_to_balconyfloordrain.Value);
                                }
                              }
                            }
                          });
              }
              else
              {
                max_balcony_to_balcony_distance.Enqueue(THESAURUSSCARCE, () =>
                          {
                            var max_basecircle_area = Commonradius(bufGeo);
                            var pps = ppsf(bufGeo);
                            if (pps.Count == THESAURUSHOUSING && max_basecircle_area.Count == THESAURUSPERMUTATION)
                            {
                              var target = pps[THESAURUSSTAMPEDE];
                              var edPts = GeoFac.CreateIntersectsSelector(pointsf(target))(bufGeo);
                              var edPt = edPts.FirstOrDefault();
                              if (edPt is null) return;
                              var _lines = linesGeosk(bufGeo).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Distinct().ToList();
                              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(bufGeo))
                              {
                                var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                if (TolLightRangeSingleSideMin.Note is QUOTATIONPELVIC)
                                {
                                  TolLightRangeSingleSideMin.Text = THESAURUSDEPLORE;
                                  TolLightRangeSingleSideMin.Note = null;
                                }
                              }
                              max_balcony_to_balcony_distance.Enqueue(THESAURUSACRIMONIOUS, () =>
                                        {
                                          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(bufGeo))
                                          {
                                            var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                            if (TolLightRangeSingleSideMin.Text == IRRESPONSIBLENESS)
                                            {
                                              TolLightRangeSingleSideMin.Text = QUOTATIONDOPPLER;
                                            }
                                          }
                                        });
                              var _tol_avg_column_dist = pointsf(bufGeo);
                              var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
                              var stPts = new HashSet<Point>();
                              foreach (var maxBalconyrainpipeToFloordrainDistance in fldrs)
                              {
                                var _pts = ptsf(maxBalconyrainpipeToFloordrainDistance.Buffer(THESAURUSPERMUTATION));
                                if (_pts.Count == THESAURUSSTAMPEDE) continue;
                                if (_pts.Count == THESAURUSHOUSING)
                                {
                                  stPts.Add(_pts[THESAURUSSTAMPEDE]);
                                }
                              }
                              var mdPts = _tol_avg_column_dist.Except(stPts).Except(edPts).ToHashSet();
                              var nodes = _tol_avg_column_dist.Select(TolLightRangeSingleSideMax => new GraphNode<Point>(TolLightRangeSingleSideMax)).ToList();
                              {
                                var kvs = new HashSet<KeyValuePair<int, int>>();
                                foreach (var default_fire_valve_length in _lines)
                                {
                                  var MAX_ANGEL_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.StartPoint.ToNTSPoint());
                                  var MAX_ANGLE_TOLLERANCE = _tol_avg_column_dist.IndexOf(default_fire_valve_length.EndPoint.ToNTSPoint());
                                  if (MAX_ANGEL_TOLLERANCE != MAX_ANGLE_TOLLERANCE)
                                  {
                                    if (MAX_ANGEL_TOLLERANCE > MAX_ANGLE_TOLLERANCE)
                                    {
                                      ThMEPWSS.ReleaseNs.RainSystemNs.RainDiagram.Swap(ref MAX_ANGEL_TOLLERANCE, ref MAX_ANGLE_TOLLERANCE);
                                    }
                                    kvs.Add(new KeyValuePair<int, int>(MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE));
                                  }
                                }
                                foreach (var max_rainpipe_to_balconyfloordrain in kvs)
                                {
                                  nodes[max_rainpipe_to_balconyfloordrain.Key].AddNeighbour(nodes[max_rainpipe_to_balconyfloordrain.Value], THESAURUSHOUSING);
                                }
                              }
                              var dijkstra = new Dijkstra<Point>(nodes);
                              {
                                var paths = new List<IList<GraphNode<Point>>>(stPts.Count);
                                var areaDict = new Dictionary<GLineSegment, double>();
                                foreach (var stPt in stPts)
                                {
                                  var path = dijkstra.FindShortestPathBetween(nodes[_tol_avg_column_dist.IndexOf(stPt)], nodes[_tol_avg_column_dist.IndexOf(edPt)]);
                                  paths.Add(path);
                                }
                                foreach (var path in paths)
                                {
                                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                  {
                                    areaDict[new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d())] = THESAURUSSTAMPEDE;
                                  }
                                }
                                foreach (var path in paths)
                                {
                                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                  {
                                    var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                    var sel = default_fire_valve_length.Buffer(VÖLKERWANDERUNG);
                                    foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                    {
                                      var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                      if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                      {
                                        var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                        areaDict[default_fire_valve_length] += DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH;
                                      }
                                    }
                                  }
                                }
                                foreach (var path in paths)
                                {
                                  double area = THESAURUSSTAMPEDE;
                                  for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                  {
                                    var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                    var sel = default_fire_valve_length.Buffer(VÖLKERWANDERUNG);
                                    foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                    {
                                      var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                      if (!string.IsNullOrEmpty(TolLightRangeSingleSideMin.Text))
                                      {
                                        var DEFAULT_FIRE_VALVE_WIDTH = parseDn(TolLightRangeSingleSideMin.Text);
                                        if (area < DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH) area = DEFAULT_FIRE_VALVE_WIDTH * DEFAULT_FIRE_VALVE_WIDTH;
                                      }
                                    }
                                    if (areaDict[default_fire_valve_length] < area) areaDict[default_fire_valve_length] = area;
                                  }
                                }
                                {
                                  var dict = areaDict.ToDictionary(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key, TolLightRangeSingleSideMax => QUOTATIONTRANSFERABLE);
                                  foreach (var path in paths)
                                  {
                                    for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < path.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
                                    {
                                      var default_fire_valve_length = new GLineSegment(path[MAX_ANGEL_TOLLERANCE].Value.ToPoint2d(), path[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].Value.ToPoint2d());
                                      dict[default_fire_valve_length] += areaDict[default_fire_valve_length];
                                    }
                                  }
                                  areaDict = dict;
                                }
                                foreach (var max_rainpipe_to_balconyfloordrain in areaDict)
                                {
                                  var sel = max_rainpipe_to_balconyfloordrain.Key.Buffer(THESAURUSHOUSING);
                                  foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in sankakuptsf(sel))
                                  {
                                    var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                                    TolLightRangeSingleSideMin.Text = getDn(max_rainpipe_to_balconyfloordrain.Value);
                                  }
                                }
                              }
                            }
                          });
              }
            }
            max_balcony_to_balcony_distance.Enqueue(ECCLESIASTICISM, () =>
                      {
                        foreach (var _segs in _linesGroup)
                        {
                          var TolLane = _segs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= THESAURUSINCOMPLETE).ToList();
                          if (TolLane.Count == THESAURUSSTAMPEDE) continue;
                          var DEFAULT_FIRE_VALVE_LENGTH = GeoFac.CreateGeometry(TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()));
                          var buf = DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSPERMUTATION);
                          void changeDn()
                          {
                            foreach (var cleaningPort in geoData.CleaningPorts)
                            {
                              if (buf.Intersects(cleaningPort))
                              {
                                var MAX_CONDENSEPIPE_TO_WASHMACHINE = IRRESPONSIBLENESS;
                                if (kitchenst(buf)) MAX_CONDENSEPIPE_TO_WASHMACHINE = vm.Params.BasinDN;
                                var _tol_avg_column_dist = sankakuptsf(buf);
                                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist.Where(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE))
                                {
                                  ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = MAX_CONDENSEPIPE_TO_WASHMACHINE;
                                }
                              }
                            }
                            if (TolLane.Count == THESAURUSPERMUTATION)
                            {
                              var _tol_avg_column_dist = sankakuptsf(buf);
                              if (_tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is IRRESPONSIBLENESS) == THESAURUSHOUSING && _tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is QUOTATIONBREWSTER or QUOTATIONDOPPLER) == THESAURUSHOUSING)
                              {
                                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist.Where(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is IRRESPONSIBLENESS))
                                {
                                  ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = (_tol_avg_column_dist.Where(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is QUOTATIONBREWSTER or QUOTATIONDOPPLER).First().UserData as MLeaderInfo).Text;
                                }
                              }
                              else if (_tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is QUOTATIONBREWSTER) == THESAURUSHOUSING && _tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE) == THESAURUSHOUSING)
                              {
                                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist.Where(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE))
                                {
                                  ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = QUOTATIONBREWSTER;
                                }
                              }
                              else if (_tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is QUOTATIONDOPPLER) == THESAURUSHOUSING && _tol_avg_column_dist.Count(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE) == THESAURUSHOUSING)
                              {
                                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist.Where(TolLightRangeSingleSideMax => ((MLeaderInfo)TolLightRangeSingleSideMax.UserData).Text is THESAURUSDEPLORE))
                                {
                                  ((MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).Text = QUOTATIONDOPPLER;
                                }
                              }
                              return;
                            }
                          }
                          changeDn();
                        }
                      });
            max_balcony_to_balcony_distance.Enqueue(ACANTHOCEPHALANS, () =>
                      {
                        foreach (var TolUniformSideLenth in _linesGroup.SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length < THESAURUSINCOMPLETE))
                        {
                          var _tol_avg_column_dist = sankakuptsf(TolUniformSideLenth.Buffer(ASSOCIATIONISTS));
                          if (_tol_avg_column_dist.Count == THESAURUSHOUSING)
                          {
                            var TolLightRangeSingleSideMin = (MLeaderInfo)_tol_avg_column_dist[THESAURUSSTAMPEDE].UserData;
                            if (TolLightRangeSingleSideMin.Text == THESAURUSDEPLORE)
                            {
                              TolLightRangeSingleSideMin.Text = null;
                            }
                          }
                        }
                      });
            max_balcony_to_balcony_distance.Enqueue(THESAURUSOCCASIONALLY, () =>
                      {
                        var linesGeos = _linesGroup.Select(tolReturnValueRange => new MultiLineString(tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToArray())).ToList();
                        var linesGeosk = GeoFac.CreateContainsSelector(linesGeos);
                        foreach (var tolReturnValueRange in _linesGroup)
                        {
                          var kvs = new List<KeyValuePair<GLineSegment, MLeaderInfo>>();
                          foreach (var TolUniformSideLenth in tolReturnValueRange.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > QUOTATIONLUCANIAN))
                          {
                            var _tol_avg_column_dist = sankakuptsf(TolUniformSideLenth.Buffer(THESAURUSPERMUTATION));
                            if (_tol_avg_column_dist.Count == THESAURUSHOUSING)
                            {
                              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist[THESAURUSSTAMPEDE];
                              var TolLightRangeSingleSideMin = (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData;
                              kvs.Add(new KeyValuePair<GLineSegment, MLeaderInfo>(TolUniformSideLenth, TolLightRangeSingleSideMin));
                            }
                          }
                          {
                            var TolLane = kvs.Select(max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Key).Distinct().ToList();
                            var lns = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
                            var vertexs = TolLane.YieldPoints().Distinct().ToList();
                            var lnsf = GeoFac.CreateIntersectsSelector(lns);
                            var lnscf = GeoFac.CreateContainsSelector(TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(-THESAURUSPERMUTATION).ToLineString()).ToList());
                            var opts = new List<Ref<RegexOptions>>();
                            foreach (var vertex in vertexs)
                            {
                              var MinWellToUrinalDistance = lnsf(GeoFac.CreateCirclePolygon(vertex, THESAURUSCOMMUNICATION, SUPERLATIVENESS));
                              RegexOptions opt;
                              if (MinWellToUrinalDistance.Count == THESAURUSHOUSING)
                              {
                                opt = RegexOptions.IgnoreCase;
                              }
                              else if (MinWellToUrinalDistance.Count == THESAURUSPERMUTATION)
                              {
                                opt = RegexOptions.Multiline;
                              }
                              else if (MinWellToUrinalDistance.Count > THESAURUSPERMUTATION)
                              {
                                opt = RegexOptions.ExplicitCapture;
                              }
                              else
                              {
                                opt = RegexOptions.None;
                              }
                              opts.Add(new Ref<RegexOptions>(opt));
                            }
                            foreach (var DEFAULT_FIRE_VALVE_LENGTH in GeoFac.GroupLinesByConnPoints(GeoFac.GetLines(GeoFac.CreateGeometry(lns).Difference(GeoFac.CreateGeometryEx(opts.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value == RegexOptions.ExplicitCapture).Select(opts).ToList(vertexs).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle(THESAURUSPERMUTATION).ToCirclePolygon(SUPERLATIVENESS)).ToList()))).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList(), UNCONSEQUENTIAL))
                            {
                              var bf = DEFAULT_FIRE_VALVE_LENGTH.Buffer(THESAURUSPERMUTATION);
                              var _tol_avg_column_dist = sankakuptsf(bf);
                              var _tol_light_range_single_side_max = _tol_avg_column_dist.Select(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN => (MLeaderInfo)MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData).ToList();
                              if (_tol_light_range_single_side_max.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Text).Distinct().Count() == THESAURUSHOUSING)
                              {
                                var repeated_point_distance = _tol_light_range_single_side_max[THESAURUSSTAMPEDE];
                                var _segs = lnscf(bf).SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).ToList();
                                if (_segs.Count > THESAURUSHOUSING)
                                {
                                  const double LEN = PHOTOCONDUCTION;
                                  if (_segs.Any(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length >= LEN))
                                  {
                                    foreach (var default_fire_valve_length in _segs)
                                    {
                                      if (default_fire_valve_length.Length < LEN)
                                      {
                                        draw(null, default_fire_valve_length.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                      }
                                    }
                                  }
                                  else
                                  {
                                    var _tolReturnValueMinRange = _segs.Max(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length);
                                    foreach (var default_fire_valve_length in _segs)
                                    {
                                      if (default_fire_valve_length.Length != _tolReturnValueMinRange)
                                      {
                                        draw(null, default_fire_valve_length.Center.ToNTSPoint(), overWrite: THESAURUSOBSTINACY);
                                      }
                                    }
                                  }
                                }
                                else
                                {
                                }
                              }
                            }
                          }
                        }
                      });
          });
          max_balcony_to_balcony_distance.Enqueue(THESAURUSOCCASIONALLY, () =>
          {
            var points = precisePts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
            var pointsf = GeoFac.CreateIntersectsSelector(points);
            foreach (var TolLightRangeSingleSideMin in mlInfos)
            {
              if (TolLightRangeSingleSideMin.Text is not null)
              {
                var _tol_avg_column_dist = pointsf(TolLightRangeSingleSideMin.BasePoint.ToGRect(THESAURUSHOUSING).ToPolygon());
                var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist.FirstOrDefault();
                if (_tol_avg_column_dist.Count > THESAURUSHOUSING)
                {
                  MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = GeoFac.NearestNeighbourGeometryF(_tol_avg_column_dist)(TolLightRangeSingleSideMin.BasePoint.ToNTSPoint());
                }
                if (MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN is not null)
                {
                  TolLightRangeSingleSideMin.BasePoint = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint3d();
                }
              }
            }
          });
        }
        max_balcony_to_balcony_distance.Enqueue(THESAURUSACRIMONIOUS, () =>
        {
          foreach (var TolLightRangeSingleSideMin in mlInfos)
          {
            if (TolLightRangeSingleSideMin.Text == PHOTOFLUOROGRAM) TolLightRangeSingleSideMin.Text = THESAURUSDEPLORE;
          }
        });
      }
      foreach (var TolLightRangeSingleSideMin in mlInfos)
      {
        if (TolLightRangeSingleSideMin.Text == THESAURUSDEPLORE) TolLightRangeSingleSideMin.Text = THESAURUSIMPETUOUS;
      }
      foreach (var TolLightRangeSingleSideMin in mlInfos)
      {
        if (!string.IsNullOrWhiteSpace(TolLightRangeSingleSideMin.Text)) DrawMLeader(TolLightRangeSingleSideMin.Text, TolLightRangeSingleSideMin.BasePoint, TolLightRangeSingleSideMin.BasePoint.OffsetXY(HYPERDISYLLABLE, HYPERDISYLLABLE));
      }
    }
    public static void DrawBackToFlatDiagram(ThMEPWSS.ReleaseNs.DrainageSystemNs.ExtraInfo exInfo)
    {
      if (exInfo is null) return;
      DrawBackToFlatDiagram(exInfo.storeysItems, exInfo.geoData, exInfo.drDatas, exInfo, exInfo.vm);
    }
    public static void FixStoreys(List<StoreyInfo> KitchenBufferDistance)
    {
      var lst1 = KitchenBufferDistance.Where(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers.Count == THESAURUSHOUSING).Select(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers[THESAURUSSTAMPEDE]).ToList();
      foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in KitchenBufferDistance.Where(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers.Count > THESAURUSHOUSING).ToList())
      {
        var defaultFireValveLength = new HashSet<int>(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers);
        foreach (var _s in lst1) defaultFireValveLength.Remove(_s);
        MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers.Clear();
        MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers.AddRange(defaultFireValveLength.OrderBy(MAX_ANGEL_TOLLERANCE => MAX_ANGEL_TOLLERANCE));
      }
    }
    public static StoreyInfo GetStoreyInfo(BlockReference tolReturnValueRangeTo)
    {
      var props = tolReturnValueRangeTo.DynamicBlockReferencePropertyCollection;
      return new StoreyInfo()
      {
        StoreyType = GetStoreyType((string)props.GetValue(ADSIGNIFICATION)),
        Numbers = ParseFloorNums(GetStoreyNumberString(tolReturnValueRangeTo)),
        ContraPoint = GetContraPoint(tolReturnValueRangeTo),
        Boundary = tolReturnValueRangeTo.Bounds.ToGRect(),
      };
    }
    public static string GetStoreyNumberString(BlockReference tolReturnValueRangeTo)
    {
      var d = tolReturnValueRangeTo.ObjectId.GetAttributesInBlockReference(THESAURUSOBSTINACY);
      d.TryGetValue(THESAURUSFLAGRANT, out string tolGroupEmgLightEvac);
      return tolGroupEmgLightEvac;
    }
    public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.BlockTableRecord.IsValid && TolLightRangeSingleSideMax.GetEffectiveName() is THESAURUSSTICKY).ToList();
    public static Point2d GetContraPoint(BlockReference tolReturnValueRangeTo)
    {
      double MAX_DEVICE_TO_DEVICE = double.NaN;
      double MAX_DEVICE_TO_BALCONY = double.NaN;
      Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
      foreach (DynamicBlockReferenceProperty _raiseDistanceToStartDefault in tolReturnValueRangeTo.DynamicBlockReferencePropertyCollection)
      {
        if (_raiseDistanceToStartDefault.PropertyName == QUOTATIONJUMPING)
        {
          MAX_DEVICE_TO_DEVICE = Convert.ToDouble(_raiseDistanceToStartDefault.Value);
        }
        else if (_raiseDistanceToStartDefault.PropertyName == THESAURUSEXPOSTULATE)
        {
          MAX_DEVICE_TO_BALCONY = Convert.ToDouble(_raiseDistanceToStartDefault.Value);
        }
      }
      if (!double.IsNaN(MAX_DEVICE_TO_DEVICE) && !double.IsNaN(MAX_DEVICE_TO_BALCONY))
      {
        MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = tolReturnValueRangeTo.Position.ToPoint2d() + new Vector2d(MAX_DEVICE_TO_DEVICE, MAX_DEVICE_TO_BALCONY);
      }
      else
      {
        throw new System.Exception(QUOTATIONAMNESTY);
      }
      return MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
    }
    public static string FixVerticalPipeLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return null;
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(THESAURUSSTUTTER))
      {
        return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Substring(INTROPUNITIVENESS);
      }
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(JUNGERMANNIALES))
      {
        return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Substring(THESAURUSPERMUTATION);
      }
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
    }
    public static bool IsNotedLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(THESAURUSACQUISITIVE) || MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(QUOTATIONCHILLI);
    }
    public static bool IsWantedLabelText(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsDL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
    }
    public static bool IsY1L(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(CHRISTIANIZATION);
    }
    public static bool IsY2L(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(UNPREMEDITATEDNESS);
    }
    public static bool IsNL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(THESAURUSFINICKY);
    }
    public static bool IsYL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      for (int MAX_ANGEL_TOLLERANCE = INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE < THESAURUSACRIMONIOUS; MAX_ANGEL_TOLLERANCE++)
      {
        if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(THESAURUSREGENERATE + MAX_ANGEL_TOLLERANCE + THESAURUSREALLY))
        {
          return THESAURUSOBSTINACY;
        }
      }
      return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.StartsWith(THESAURUSUNBEATABLE);
    }
    public static bool IsRainLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return IsY1L(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsY2L(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsNL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsYL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
    }
    public static bool IsDrainageLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      return IsRainLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsDraiLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
    }
    public static bool IsMaybeLabelText(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
      static bool maxDeviceplatformArea(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        return IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsDL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsWL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
      }
      return maxDeviceplatformArea(FixVerticalPipeLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
    }
    public const int THESAURUSHOUSING = 1;
        public const int THESAURUSCOMMUNICATION = 5;
        public const int THESAURUSPERMUTATION = 2;
        public const int THESAURUSSTAMPEDE = 0;
        public const double ELECTROLUMINESCENT = 10e5;
        public const int THESAURUSACRIMONIOUS = 10;
        public const double ASSOCIATIONISTS = .1;
        public const bool INTRAVASCULARLY = false;
        public const int THESAURUSDISINGENUOUS = 36;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int THESAURUSDICTATORIAL = 1500;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const string THESAURUSMARSHY = "空调内机--柜机";
        public const string THESAURUSPROFANITY = "空调内机--挂机";
        public const string DISORGANIZATION = "TCH_PIPE";
        public const string INSTRUMENTALITY = "W-RAIN-PIPE";
        public const string THESAURUSABJURE = "-RAIN-";
        public const string QUOTATIONSTANLEY = "W-BUSH-NOTE";
        public const string THESAURUSDEFAULTER = "W-BUSH";
        public const string THESAURUSINVOICE = "W-RAIN-DIMS";
        public const int HYPERDISYLLABLE = 100;
        public const string DENDROCHRONOLOGIST = "W-RAIN-EQPM";
        public const int THESAURUSINCOMPLETE = 20;
        public const string THESAURUSWINDFALL = "TCH_VPIPEDIM";
        public const string THESAURUSSPECIFICATION = "-";
        public const string THESAURUSDURESS = "TCH_TEXT";
        public const string QUOTATIONSWALLOW = "TCH_EQUIPMENT";
        public const string THESAURUSFACILITATE = "TCH_MTEXT";
        public const string THESAURUSINHARMONIOUS = "TCH_MULTILEADER";
        public const string THESAURUSDEPLORE = "";
        public const string THESAURUSELIGIBLE = "接";
        public const string VICISSITUDINOUS = "排水沟";
        public const string THESAURUSADVENT = "雨水口";
        public const int THESAURUSENTREPRENEUR = 50;
        public const char THESAURUSCONTEND = '|';
        public const string MULTIPROCESSING = "|";
        public const char SUPERREGENERATIVE = '$';
        public const string THESAURUSCOURIER = "$";
        public const string THESAURUSINDULGENT = "地漏";
        public const string INCORRESPONDENCE = "DL";
        public const string THESAURUSENTERPRISE = "可见性";
        public const string THESAURUSTHOROUGHBRED = "套管";
        public const int POLYOXYMETHYLENE = 1000;
        public const string SUPERINDUCEMENT = "带定位立管";
        public const string THESAURUSURBANITY = "立管编号";
        public const string THESAURUSSPECIMEN = "$LIGUAN";
        public const string THESAURUSMANIKIN = "A$C6BDE4816";
        public const string THESAURUSLANDMARK = "污废合流井编号";
        public const string THESAURUSCONTROVERSY = "W-DRAI-DOME-PIPE";
        public const string CIRCUMCONVOLUTION = "W-RAIN-NOTE";
        public const char CHROMATOGRAPHER = 'B';
        public const string THESAURUSARGUMENTATIVE = "RF";
        public const string ANTHROPOMORPHICALLY = "RF+1";
        public const string THESAURUSSCUFFLE = "RF+2";
        public const string THESAURUSASPIRATION = "F";
        public const int THESAURUSDOMESTIC = 400;
        public const string THESAURUSREGION = "1F";
        public const string THESAURUSTABLEAU = "-1F";
        public const string THESAURUSLEGACY = "-0.XX";
        public const string COSTERMONGERING = "W-NOTE";
        public const string THESAURUSJUBILEE = "W-DRAI-EQPM";
        public const string THESAURUSSTRIPED = "W-DRAI-NOTE";
        public const double QUOTATIONLETTERS = 2500.0;
        public const double BALANOPHORACEAE = 5500.0;
        public const double THESAURUSINCOMING = 1800.0;
        public const string THESAURUSPARTNER = "伸顶通气2000";
        public const string THESAURUSINEFFECTUAL = "伸顶通气500";
        public const string HYPERVENTILATION = "本层通气";
        public const string THESAURUSNARCOTIC = "通气帽系统-AI2";
        public const string QUINQUAGENARIAN = "可见性1";
        public const string THESAURUSDUBIETY = "1000";
        public const int THESAURUSNECESSITOUS = 580;
        public const string THESAURUSSUPERFICIAL = "标高";
        public const int QUOTATIONPITUITARY = 550;
        public const string THESAURUSSHADOWY = "建筑完成面";
        public const int QUOTATIONBASTARD = 1800;
        public const int THESAURUSPERVADE = 121;
        public const int THESAURUSUNEVEN = 1258;
        public const int THESAURUSUNCOMMITTED = 120;
        public const int CONTRADISTINGUISHED = 779;
        public const int COOPERATIVENESS = 1679;
        public const int THESAURUSERRAND = 658;
        public const int THESAURUSQUAGMIRE = 90;
        public const string QUOTATIONSTYLOGRAPHIC = @"^(\-?\d+)\-(\-?\d+)$";
        public const string TETRAIODOTHYRONINE = @"^\-?\d+$";
        public const string MULTINATIONALLY = "±0.00";
        public const double LAUTENKLAVIZIMBEL = 1000.0;
        public const string THESAURUSINFINITY = "0.00";
        public const int THESAURUSDISAGREEABLE = 800;
        public const int QUOTATIONWITTIG = 500;
        public const int THESAURUSMAIDENLY = 1879;
        public const int THESAURUSCAVERN = 180;
        public const int THESAURUSINTRACTABLE = 160;
        public const string ADENOHYPOPHYSIS = "普通地漏无存水弯";
        public const string THESAURUSDISREPUTABLE = "DN25";
        public const int THESAURUSATTACHMENT = 750;
        public const string IRRESPONSIBLENESS = "DN100";
        public const double UNDENOMINATIONAL = 0.0;
        public const int SUBCATEGORIZING = 780;
        public const int THESAURUSFORMULATE = 700;
        public const double QUOTATIONTRANSFERABLE = .0;
        public const int THESAURUSHYPNOTIC = 300;
        public const int THESAURUSREVERSE = 24;
        public const int THESAURUSACTUAL = 9;
        public const int QUOTATIONEDIBLE = 4;
        public const int INTROPUNITIVENESS = 3;
        public const int THESAURUSEXECRABLE = 3600;
        public const int THESAURUSENDANGER = 350;
        public const int THESAURUSSURPRISED = 150;
        public const string THESAURUSEXECUTIVE = "散排至";
        public const int PHOSPHORYLATION = 125;
        public const int THESAURUSMISUNDERSTANDING = 357;
        public const int THESAURUSBEHOVE = 83;
        public const int THESAURUSMAGNETIC = 220;
        public const int REPRESENTATIONAL = 1400;
        public const int THESAURUSBELLOW = 621;
        public const int DOCTRINARIANISM = 1200;
        public const int THESAURUSINHERIT = 2000;
        public const int THESAURUSDERELICTION = 600;
        public const int ACANTHORHYNCHUS = 479;
        public const int THESAURUSLOITER = 950;
        public const int PORTMANTOLOGISM = 387;
        public const int QUOTATIONCOLERIDGE = 1050;
        public const int THESAURUSEXPERIMENT = 269;
        public const int MISAPPREHENSIVE = 200;
        public const string QUOTATIONSECOND = "接至排水沟";
        public const int QUOTATIONETHIOPS = 187;
        public const int OTHERWORLDLINESS = 499;
        public const int THESAURUSPRETTY = 1600;
        public const int THESAURUSDIFFICULTY = 360;
        public const int THESAURUSDETERMINED = 130;
        public const int CONSCRIPTIONIST = 650;
        public const int VÖLKERWANDERUNG = 30;
        public const string INTERNALIZATION = "≥600";
        public const int THESAURUSGETAWAY = 450;
        public const int ACANTHOCEPHALANS = 18;
        public const int PHYSIOLOGICALLY = 250;
        public const double THESAURUSDISPASSIONATE = .7;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const string THESAURUSCAVALIER = ";";
        public const double UNCONSEQUENTIAL = .01;
        public const string PERSUADABLENESS = "地漏系统";
        public const int PROKELEUSMATIKOS = 745;
        public const string THESAURUSTENACIOUS = "乙字弯";
        public const string THESAURUSAGILITY = "立管检查口";
        public const string THESAURUSSTRINGENT = "套管系统";
        public const string THESAURUSGAUCHE = "重力流雨水井编号";
        public const string HELIOCENTRICISM = "666";
        public const string THESAURUSJAILER = @"^([^\-]*)\-([A-Za-z])$";
        public const string DEMATERIALISING = ",";
        public const string THESAURUSEXCREMENT = "~";
        public const string THESAURUSCAPRICIOUS = @"^([^\-]*\-[A-Za-z])(\d+)$";
        public const string UNIMPRESSIONABLE = @"^([^\-]*\-)([A-Za-z])(\d+)$";
        public const int THESAURUSDESTITUTE = 7;
        public const int SYNTHLIBORAMPHUS = 229;
        public const int THESAURUSPRIVATE = 230;
        public const int THESAURUSEXCEPTION = 8192;
        public const int THESAURUSEPICUREAN = 8000;
        public const int DINOFLAGELLATES = 15;
        public const int THESAURUSHESITANCY = 60;
        public const string THESAURUSJOBBER = "FromImagination";
        public const int THESAURUSLUMBERING = 55;
        public const string THESAURUSCROUCH = "X.XX";
        public const int THESAURUSITEMIZE = 666;
        public const string THESAURUSPRECOCIOUS = "屋面雨水斗";
        public const int THESAURUSEXCESS = 255;
        public const int THESAURUSDELIGHT = 0x91;
        public const int THESAURUSCRADLE = 0xc7;
        public const int HYPOSTASIZATION = 0xae;
        public const int THESAURUSDISCOLOUR = 211;
        public const string QUOTATIONDOPPLER = "DN75";
        public const string THESAURUSTOPICAL = "重力雨水斗";
        public const string THESAURUSBANDAGE = "侧入式雨水斗";
        public const string THESAURUSCONSERVATION = "87雨水斗";
        public const string THESAURUSSTUTTER = "73-";
        public const string JUNGERMANNIALES = "1-";
        public const string PERSPICACIOUSNESS = "SelectedRange";
        public const string UREDINIOMYCETES = @"DN\d+";
        public const string QUOTATION3BABOVE = @"[^\d\.\-]";
        public const string THESAURUSMISTRUST = @"\d+\-";
        public const string ADSIGNIFICATION = "楼层类型";
        public const char THESAURUSMETROPOLIS = '，';
        public const char THESAURUSPROMINENT = ',';
        public const char NATIONALDEMOKRATISCHE = '-';
        public const string STEREOPHOTOGRAMMETRY = "M";
        public const string THESAURUSPOLISH = " ";
        public const string THESAURUSAGITATION = @"(\-?\d+)-(\-?\d+)";
        public const string THESAURUSSANITY = @"\-?\d+";
        public const string THESAURUSFLAGRANT = "楼层编号";
        public const string THESAURUSSTICKY = "楼层框定";
        public const string QUOTATIONJUMPING = "基点 X";
        public const string THESAURUSEXPOSTULATE = "基点 Y";
        public const string QUOTATIONAMNESTY = "error occured while getting baseX and baseY";
        public const string THESAURUSLECHER = "雨水斗";
        public const string THESAURUSPLUMMET = "重力";
        public const string QUOTATIONSPENSERIAN = "87";
        public const string CIRCUMSTANTIARE = "卫生间";
        public const string THESAURUSCIPHER = "主卫";
        public const string PREMILLENNIALIST = "公卫";
        public const string THESAURUSINDIGENT = "次卫";
        public const string THESAURUSFRAMEWORK = "客卫";
        public const string THESAURUSHUMILIATION = "洗手间";
        public const string THESAURUSSTERLING = "卫";
        public const string THESAURUSSCREEN = @"^[卫]\d$";
        public const string THESAURUSPEDESTRIAN = "厨房";
        public const string THESAURUSRECLUSE = "西厨";
        public const string THESAURUSINCONTROVERTIBLE = "厨";
        public const string NATIONALIZATION = "阳台";
        public const string THESAURUSSPECIES = "连廊";
        public const string CHRISTIANIZATION = "Y1L";
        public const string UNPREMEDITATEDNESS = "Y2L";
        public const string THESAURUSFINICKY = "NL";
        public const string THESAURUSUNBEATABLE = "YL";
        public const string THESAURUSANTIDOTE = @"^W\d?L";
        public const string THESAURUSDISSOLVE = @"^F\d?L";
        public const string AUTOLITHOGRAPHIC = "-0";
        public const string THESAURUSABUNDANT = @"^P\d?L";
        public const string THESAURUSOPTIONAL = @"^T\d?L";
        public const string DIASTEREOISOMER = @"^D\d?L";
        public const double HYDROELECTRICITY = 383875.8169;
        public const double THESAURUSMARRIAGE = 250561.9571;
        public const string SUCCESSLESSNESS = "P型存水弯";
        public const string THESAURUSLINING = "板上P弯";
        public const int THESAURUSNAUGHT = 3500;
        public const int THESAURUSCOMATOSE = 1479;
        public const int THESAURUSFORESTALL = 2379;
        public const int THESAURUSLEARNER = 1779;
        public const int INVULNERABLENESS = 579;
        public const int THESAURUSOFFEND = 279;
        public const string PERIODONTOCLASIA = "双池S弯";
        public const string THUNDERSTRICKEN = "W-DRAI-VENT-PIPE";
        public const string QUOTATIONBREWSTER = "DN50";
        public const string THESAURUSADVERSITY = "W-DRAI-WAST-PIPE";
        public const int THESAURUSFLUTTER = 789;
        public const int THROMBOEMBOLISM = 1270;
        public const int THESAURUSJACKPOT = 1090;
        public const string THESAURUSDISCIPLINARIAN = "双池P弯";
        public const int THESAURUSPRIMARY = 171;
        public const int CONSPICUOUSNESS = 329;
        public const int QUOTATIONDENNIS = 5479;
        public const int THESAURUSBLESSING = 1079;
        public const int THESAURUSCLIMATE = 5600;
        public const int THESAURUSCOLOSSAL = 6079;
        public const int THESAURUSSCARCE = 8;
        public const int QUOTATIONAFGHAN = 1379;
        public const int INDIGESTIBLENESS = 569;
        public const int THESAURUSNECESSITY = 406;
        public const int QUOTATIONDEFLUVIUM = 404;
        public const int HYDROCOTYLACEAE = 3150;
        public const int THESAURUSMORTUARY = 12;
        public const int THESAURUSEUPHORIA = 1300;
        public const double THESAURUSRIBALD = .4;
        public const string THESAURUSMOLEST = "接阳台洗手盆排水";
        public const string THESAURUSSCOUNDREL = "DN50，余同";
        public const int THIGMOTACTICALLY = 1190;
        public const string QUOTATIONHUMPBACK = "接卫生间排水管";
        public const string DISCOMMODIOUSNESS = "DN100，余同";
        public const int THESAURUSOUTLANDISH = 490;
        public const int RETROGRESSIVELY = 170;
        public const int PROCRASTINATORY = 2830;
        public const int INCONSIDERABILIS = 900;
        public const int THESAURUSSHROUD = 330;
        public const int THESAURUSSOMETIMES = 895;
        public const int ELECTROMYOGRAPH = 285;
        public const int THESAURUSINTRENCH = 390;
        public const string ACCOMMODATINGLY = "普通地漏P弯";
        public const int THESAURUSSECLUSION = 1330;
        public const int PHOTOSYNTHETICALLY = 270;
        public const string THESAURUSPUGNACIOUS = "接厨房洗涤盆排水";
        public const int UNAPPREHENSIBLE = 156;
        public const int THESAURUSNOTABLE = 510;
        public const int THESAURUSMISSIONARY = 389;
        public const int ANTICONVULSANTS = 45;
        public const int THESAURUSPHANTOM = 669;
        public const int THESAURUSCONSIGNMENT = 590;
        public const int ACETYLSALICYLIC = 1700;
        public const int PREREGISTRATION = 520;
        public const int THESAURUSJOURNALIST = 919;
        public const int CONSTITUTIVENESS = 990;
        public const int THESAURUSALLEGIANCE = 129;
        public const int ALSOSESQUIALTERAL = 693;
        public const int THESAURUSCUSTOMARY = 1591;
        public const int APOLLINARIANISM = 511;
        public const int TRICHINELLIASIS = 289;
        public const string QUOTATIONBENJAMIN = "W-DRAI-DIMS";
        public const int THESAURUSSATIATE = 1391;
        public const int THESAURUSINFLEXIBLE = 667;
        public const int THESAURUSCAPITALISM = 1450;
        public const int QUINQUARTICULAR = 251;
        public const int THESAURUSINVADE = 660;
        public const int THESAURUSBISEXUAL = 110;
        public const int THESAURUSSPIRIT = 91;
        public const int THESAURUSCANDIDATE = 320;
        public const int ALUMINOSILICATES = 427;
        public const int THESAURUSEXHILARATION = 183;
        public const int POLIOENCEPHALITIS = 283;
        public const double THESAURUSADJUST = 250.0;
        public const int THESAURUSHALTER = 225;
        public const int THESAURUSMERITORIOUS = 1125;
        public const int THESAURUSRATION = 280;
        public const int THESAURUSNEGATE = 76;
        public const int QUOTATIONRHEUMATOID = 424;
        public const int THESAURUSSENSITIVE = 1900;
        public const string THESAURUSECHELON = "DN100乙字弯";
        public const string CONSECUTIVENESS = "1350";
        public const int THESAURUSBOMBARD = 275;
        public const int INTERNATIONALLY = 210;
        public const int THESAURUSEVIDENT = 151;
        public const int THESAURUSINDOMITABLE = 1109;
        public const int THESAURUSDISCERNIBLE = 420;
        public const int PRESBYTERIANIZE = 318;
        public const int THESAURUSDEGREE = 447;
        public const int THESAURUSSENTIMENTALITY = 43;
        public const int ULTRASONICATION = 237;
        public const string THESAURUSSYNTHETIC = "洗衣机地漏P弯";
        public const int THESAURUSMOTIONLESS = 1380;
        public const double THESAURUSFEELER = 200.0;
        public const double THESAURUSATTENDANCE = 780.0;
        public const double THESAURUSINACCURACY = 130.0;
        public const int THESAURUSEXCHANGE = 980;
        public const int UNPERISHABLENESS = 1358;
        public const int THESAURUSDENTIST = 172;
        public const int NOVAEHOLLANDIAE = 155;
        public const int THESAURUSMEDIATION = 1650;
        public const int THESAURUSFICTION = 71;
        public const int THESAURUSNEGLIGENCE = 221;
        public const int THESAURUSCREDITABLE = 29;
        public const int THESAURUSJINGLE = 1158;
        public const int ALSOMEGACEPHALOUS = 179;
        public const int THESAURUSINCARCERATE = 880;
        public const string METACOMMUNICATION = ">1500";
        public const int DEMONSTRATIONIST = 921;
        public const int THESAURUSMIRTHFUL = 2320;
        public const string QUOTATIONBARBADOS = "普通地漏S弯";
        public const int THESAURUSLINGER = 3090;
        public const int METALINGUISTICS = 371;
        public const int ORTHOPAEDICALLY = 2730;
        public const int THESAURUSREPRODUCTION = 888;
        public const int THESAURUSEQUATION = 629;
        public const int THESAURUSECLECTIC = 460;
        public const int THESAURUSINGLORIOUS = 2499;
        public const int QUOTATIONMASTOID = 1210;
        public const int THESAURUSISOLATION = 850;
        public const double THESAURUSFLIRTATIOUS = 270.0;
        public const int THESAURUSCHORUS = 2279;
        public const int THESAURUSCELESTIAL = 1239;
        public const int THESAURUSPRIVILEGE = 675;
        public const string MÖNCHENGLADBACH = "通气帽系统-AI";
        public const string THESAURUSDECLAIM = "PL";
        public const string THESAURUSCONFIRM = "TL";
        public const string QUOTATIONSTRETTO = "管底h+X.XX";
        public const string UNACCEPTABILITY = "双格洗涤盆排水-AI";
        public const string CARCINOGENICITY = "S型存水弯";
        public const string FISSIPAROUSNESS = "板上S弯";
        public const string THESAURUSCASCADE = "翻转状态";
        public const string THESAURUSDENOUNCE = "清扫口系统";
        public const int THESAURUSCOORDINATE = 21;
        public const string THESAURUSREMNANT = "-DRAI-";
        public const string THESAURUSINCENSE = "-PIPE";
        public const string THESAURUSDEVIANT = "VENT";
        public const string EXTRAORDINARINESS = "AI-空间框线";
        public const string THESAURUSUNSPEAKABLE = "AI-房间框线";
        public const string THESAURUSEMBOLDEN = "AI-空间名称";
        public const string QUOTATIONGOLDEN = "AI-房间名称";
        public const string THESAURUSSINCERE = "WP_KTN_LG";
        public const string THESAURUSNOTATION = "De";
        public const string PERPENDICULARITY = "wb";
        public const string THESAURUSIMPOSTER = "kd";
        public const string THESAURUSPREFERENCE = "C-XREF-EXT";
        public const string THESAURUSINTENTIONAL = "雨水井";
        public const string THESAURUSCONFRONTATION = "地漏平面";
        public const string REPRESENTATIVES = "A$C58B12E6E";
        public const string THESAURUSCORRELATION = "W-DRAI-PIEP-RISR";
        public const string THESAURUSUNINTERESTED = "A$C5E4A3C21";
        public const string QUOTATIONCORNISH = "PIPE-喷淋";
        public const string CONSUBSTANTIATUS = "A-Kitchen-3";
        public const string THESAURUSCHIVALROUS = "A-Kitchen-4";
        public const string THESAURUSGRUESOME = "A-Toilet-1";
        public const string THESAURUSDAPPER = "A-Toilet-2";
        public const string QUOTATIONPYRIFORM = "A-Toilet-3";
        public const string THESAURUSEXTORTIONATE = "A-Toilet-4";
        public const string THESAURUSDRASTIC = "-XiDiPen-";
        public const string THESAURUSHUMOUR = "A-Kitchen-9";
        public const string HYPERSENSITIZED = "0$座厕";
        public const string THESAURUSHOODLUM = "0$asdfghjgjhkl";
        public const string PHYTOGEOGRAPHER = "A-Toilet-";
        public const string THESAURUSABOUND = "A-Kitchen-";
        public const string THESAURUSALLURE = "|lp";
        public const string THESAURUSMACHINERY = "|lp1";
        public const string THESAURUSESCALATE = "|lp2";
        public const string THESAURUSCLINICAL = "A-Toilet-9";
        public const string THESAURUSBALEFUL = "$xiyiji";
        public const string THESAURUSARCHER = "feng_dbg_test_washing_machine";
        public const string THESAURUSRESIGNED = "多通道";
        public const string PHOTOAUTOTROPHIC = "洗衣机";
        public const string THESAURUSACQUISITIVE = "单排";
        public const string QUOTATIONCHILLI = "设置乙字弯";
        public const string THESAURUSPOWERLESS = "\n处理中断。未找到有效楼层。";
        public const string QUOTATIONISOPHANE = "已有的管径标注将被覆盖，是否继续？";
        public const string THESAURUSMISADVENTURE = "Quetion";
        public const double DISPROPORTIONAL = 5.01;
        public const double THESAURUSAPPARATUS = 10.01;
        public const string ARGENTIMACULATUS = "W-辅助-管径";
        public const int THESAURUSSYMMETRICAL = 26;
        public const int THESAURUSDRAGOON = 33;
        public const string ALLITERATIVENESS = "DN32";
        public const int THESAURUSMORTALITY = 51;
        public const string SUPERNATURALIZE = @"\d+";
        public const string QUOTATIONROBERT = "单盆洗手台";
        public const string THESAURUSDELIVER = "双盆洗手台";
        public const string CYLINDRICALNESS = "坐便器";
        public const int THESAURUSINNOCUOUS = 99;
        public const double THESAURUSPLAYGROUND = .00001;
        public const double ULTRASONOGRAPHY = .0001;
        public const string THESAURUSEMPHASIS = "$TwtSys$00000132";
        public const string THESAURUSVICTORIOUS = "FLDR";
        public const string THESAURUSJOURNAL = "CP";
        public const int THESAURUSEVERLASTING = 25;
        public const string QUOTATIONPELVIC = "PipeDN100";
        public const int THESAURUSFINALITY = 10000;
        public const string THESAURUSBOTTOM = "PL-";
        public const int THESAURUSCENSURE = 11;
        public const int THESAURUSBEATIFIC = 13;
        public const string PHOTOFLUOROGRAM = "DN0";
        public const int PERICARDIOCENTESIS = 49;
        public const double QUOTATIONLUCANIAN = 5.1;
        public const double PHOTOCONDUCTION = 400.0;
        public const string THESAURUSIMPETUOUS = "DNXXX";
        public const string INTELLECTUALNESS = "\n";
        public const string QUOTATIONCHROMIC = "TCH";
        public const string QUOTATIONMALTESE = "沟";
        public const string THESAURUSPROLONG = "侧";
        public const int THESAURUSASSURANCE = 505;
        public const int DETERMINATENESS = 239;
        public const string THESAURUSREALLY = "L";
        public const double ALSOMONOSIPHONIC = 3e4;
        public const int THESAURUSOCCASIONALLY = 19;
        public const int ECCLESIASTICISM = 17;
        public const string THESAURUSSTRAIGHTFORWARD = "废水立管";
        public const string THESAURUSEMPTINESS = "污水立管";
        public const string THESAURUSHYPOCRISY = "污废合流立管";
        public const string THESAURUSRECIPE = "沉箱立管";
        public const string THESAURUSRAFFLE = "通气立管";
        public const string HYDROCHLOROFLUOROCARBON = "2F";
        public const double THESAURUSCONFECTIONERY = .5;
        public const string PARATHYROIDECTOMY = "地漏-AI";
        public const string PARALINGUISTICALLY = "暂不支持污废分流，仅生成废水系统图";
        public const int THESAURUSSCINTILLATE = 1231;
        public const string THESAURUSBASELESS = "FL";
        public const string THESAURUSPOSSESSIVE = "WL";
        public const double THESAURUSLEGISLATION = 8000.0;
        public const int INTROJECTIONISM = 1075;
        public const int THESAURUSSEDATE = 1100;
        public const int PROGNOSTICATORY = 2400;
        public const int THESAURUSHALLUCINATE = 1571;
        public const int THESAURUSFAINTLY = 1171;
        public const int QUOTATIONELECTROMOTIVE = 1250;
        public const int THESAURUSHOMICIDAL = 1121;
        public const int THESAURUSDESULTORY = 1821;
        public const int THESAURUSPROSPEROUS = 1321;
        public const int THESAURUSUNGRATEFUL = 3400;
        public const int INCOMPREHENSIBILIS = 571;
        public const int THESAURUSSUPPOSITION = 3350;
        public const int QUOTATION1ASHANKS = 1271;
        public const string CHRISTADELPHIAN = "接至雨水口";
        public const int NEUROPSYCHIATRIST = 1120;
        public const int SEMICONSCIOUSNESS = 680;
        public const int PSEUDEPIGRAPHOS = 276;
        public const int THESAURUSMANIFESTATION = 468;
        public const int MAXILLOPALATINE = 2180;
        public const int THESAURUSATTENDANT = 910;
        public const int THESAURUSLAWYER = 1550;
        public const int THESAURUSINDECOROUS = 2059;
        public const int THESAURUSMISAPPREHEND = 1140;
        public const int SYNAESTHETICALLY = 1040;
        public const int THESAURUSDESTRUCTION = 790;
        public const int OLIGOMENORRHOEA = 840;
        public const int THESAURUSOBSERVANCE = 3042;
        public const int THESAURUSMAYHEM = 2050;
        public const int QUOTATIONELECTRICIAN = 3450;
        public const int THESAURUSVIGOROUS = 853;
        public const int QUOTATIONFOREGONE = 531;
        public const int RETROSPECTIVENESS = 1122;
        public const int REACTIONARINESS = 547;
        public const int THESAURUSCOMPOUND = 1073;
        public const string SEROEPIDEMIOLOGY = "EQPM";
        public const string INTELLECTUALISTS = "立管";
        public const string QUOTATIONBITTER = "井";
        public const string THESAURUSENCOMPASS = @"^(F\d?L|T\d?L|P\d?L|D\d?L|W\d?L|Y\d?L|N\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const string THESAURUSRESUSCITATE = @"\-?DN\d+";
        public const char THESAURUSHABITAT = '\n';
        public const string ALSOHEAVENWARDS = "屋面";
        public const string NEUROTRANSMITTER = @"DN(\d{1,5})";
        public const string THESAURUSPAGEANT = "污废合流井";
        public const string THESAURUSRECTIFY = "0.XX";
        public const int THESAURUSFRISKY = 1350;
        public const double THESAURUSUNDERSTANDING = .2446;
        public const string THESAURUSCURDLE = "侧排雨水斗系统";
        public const int CONSUMMATIVENESS = 215;
        public const int UNACCEPTABLENESS = 235;
        public const int THESAURUSDEPUTIZE = 567;
        public const int THESAURUSHEARTLESS = 587;
        public const int QUOTATIONWORSTED = 2052;
        public const int THESAURUSVISIBLE = 2002;
        public const int THESAURUSINSTITUTE = 482;
        public const int THESAURUSINCONCLUSIVE = 1597;
        public const int ALSOBIPINNATISECT = 1547;
        public const int CATECHOLAMINERGIC = 82;
        public const int CORYNOCARPACEAE = 1579;
        public const int PROBLEMATICALNESS = 1459;
        public const int THESAURUSSMOULDER = 2444;
        public const int THESAURUSBEAUTIFY = 1398;
        public const int THESAURUSEFFULGENT = 679;
        public const int THESAURUSINDISPENSABLE = 1292;
        public const int THESAURUSREPRESSIVE = 1020;
        public const int THESAURUSMADNESS = 899;
        public const int THESAURUSEFFICACY = 820;
        public const int PALAEOICHTHYOLOGY = 699;
        public const int THESAURUSSHALLOW = 1179;
        public const int IMMEASURABLENESS = 578;
        public const int ALSOWATERLANDIAN = 365;
        public const int THESAURUSWINDING = 1844;
        public const string THESAURUSPENNILESS = "DOME";
        public const int THESAURUSREFRACTORY = 1275;
        public const int ALSOMULTIFLORAL = 1178;
        public const int THESAURUSACQUIESCENT = 1155;
        public const int THESAURUSUNOCCUPIED = 1740;
        public const int PROMORPHOLOGIST = 149;
        public const int QUOTATIONQUICHE = 870;
        public const int THESAURUSTRAUMATIC = 421;
        public const int STENTOROPHŌNIKOS = 991;
        public const int THESAURUSASTUTE = 1348;
        public const int THESAURUSASSIMILATE = 914;
        public const int SPECTROFLUORIMETER = 1355;
        public const int THESAURUSRUINOUS = 1356;
        public const int DIFFERENTIATEDNESS = 3300;
        public const int THESAURUSIMPRACTICABLE = 1721;
        public const int THESAURUSRECONCILE = 2900;
        public const int METROPOLITANATE = 879;
        public const int IMAGINATIVENESS = 2150;
        public const int TRIGONOCEPHALIC = 620;
        public const int SCIENTIFICALNESS = 1099;
        public const int QUOTATIONBUBONIC = 1220;
        public const int THESAURUSHEADSTRONG = 291;
        public const int THESAURUSCONTEMPT = 1939;
        public const int THESAURUSMATTER = 1933;
        public const int THESAURUSABSORBENT = 1268;
        public const int OLIGOSACCHARIDES = 1950;
        public const int THESAURUSINTEGRITY = 2500;
        public const int THESAURUSDISTASTEFUL = 721;
        public const int THESAURUSPROGRESSIVE = 1750;
        public const int THESAURUSINSINCERE = 1021;
        public const double THESAURUSARRIVE = 100.0;
        public const string THESAURUSREGENERATE = "Y";
        public const string THESAURUSIMPOUND = "侧入式雨水斗\nDN100";
        public const int THESAURUSCIRCULAR = 1670;
        public const int THESAURUSBUSINESS = 1549;
        public const int THESAURUSSPRINGY = 1429;
        public const int QUOTATIONSHELLEY = 1170;
        public const int THESAURUSCOMPLAINT = 1487;
        public const int THESAURUSCONVOY = 1411;
        public const int ALSOPALIMBACCHIC = 1213;
        public const int THESAURUSPREFER = 388;
        public const int THESAURUSREPARATION = 1290;
        public const int THESAURUSINFAMOUS = 1187;
        public const int ANTHROPOPHAGINIAN = 399;
        public const int QUOTATIONNAMAQUA = 174;
        public const int THESAURUSLEGALIZE = 1066;
    public static bool IsToilet(string roomName)
    {
      var roomNameContains = new List<string>
            {
                CIRCUMSTANTIARE,THESAURUSCIPHER,PREMILLENNIALIST,
                THESAURUSINDIGENT,THESAURUSFRAMEWORK,THESAURUSHUMILIATION,
            };
      if (string.IsNullOrEmpty(roomName))
        return INTRAVASCULARLY;
      if (roomNameContains.Any(maxBalconyrainpipeToFloordrainDistance => roomName.Contains(maxBalconyrainpipeToFloordrainDistance)))
        return THESAURUSOBSTINACY;
      if (roomName.Equals(THESAURUSSTERLING))
        return THESAURUSOBSTINACY;
      return Regex.IsMatch(roomName, THESAURUSSCREEN);
    }
    public static bool IsKitchen(string roomName)
    {
      var roomNameContains = new List<string> { THESAURUSPEDESTRIAN, THESAURUSRECLUSE };
      if (string.IsNullOrEmpty(roomName))
        return INTRAVASCULARLY;
      if (roomNameContains.Any(maxBalconyrainpipeToFloordrainDistance => roomName.Contains(maxBalconyrainpipeToFloordrainDistance)))
        return THESAURUSOBSTINACY;
      if (roomName.Equals(THESAURUSINCONTROVERTIBLE))
        return THESAURUSOBSTINACY;
      return INTRAVASCULARLY;
    }
    public static bool IsBalcony(string roomName)
    {
      if (roomName == null) return INTRAVASCULARLY;
      var roomNameContains = new List<string> { NATIONALIZATION };
      if (string.IsNullOrEmpty(roomName))
        return INTRAVASCULARLY;
      if (roomNameContains.Any(maxBalconyrainpipeToFloordrainDistance => roomName.Contains(maxBalconyrainpipeToFloordrainDistance)))
        return THESAURUSOBSTINACY;
      return INTRAVASCULARLY;
    }
    public static bool IsCorridor(string roomName)
    {
      if (roomName == null) return INTRAVASCULARLY;
      var roomNameContains = new List<string> { THESAURUSSPECIES };
      if (string.IsNullOrEmpty(roomName))
        return INTRAVASCULARLY;
      if (roomNameContains.Any(maxBalconyrainpipeToFloordrainDistance => roomName.Contains(maxBalconyrainpipeToFloordrainDistance)))
        return THESAURUSOBSTINACY;
      return INTRAVASCULARLY;
    }
    public static bool IsWL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return Regex.IsMatch(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSANTIDOTE);
    }
    public static bool IsDraiFL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      return IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) && !IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
    }
    public static bool IsFL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return Regex.IsMatch(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSDISSOLVE);
    }
    public static bool IsFL0(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) && MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE.Contains(AUTOLITHOGRAPHIC);
    }
    public static bool IsPL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return Regex.IsMatch(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSABUNDANT);
    }
    public static bool IsTL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return Regex.IsMatch(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSOPTIONAL);
    }
    public static bool IsDL(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return Regex.IsMatch(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, DIASTEREOISOMER);
    }
    public static bool IsDraiLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return INTRAVASCULARLY;
      return (IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) && !IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) || IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsDL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || IsWL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
    }
  }
  public class DrainageLayoutManager
  {
    List<DBTextInfo> MAX_RAINPIPE_TO_WASHMACHINE;
    int EnlargeTolerance;
    int MinimumAreaTolerance;
    List<BlockInfo> COMMONRADIUS;
    int _fireHydrantOutRaisePipe;
    int TolLightRangeMax;
    List<LineInfo> MAX_BALCONYBASIN_TO_BALCONY;
    int _ringPipeSpace;
    int _raisePipeDistanceStartPoint;
    List<CircleInfo> BALCONY_BUFFER_DISTANCE;
    int _raisePipeSpace;
    int _ringPipeMaxSpace;
    List<DimInfo> MIN_BALCONYBASIN_TO_BALCONY;
    int _scaleRefugeFireH;
    int _scaleTopFloorTopLine;
    public DrainageLayoutManager(List<DBTextInfo> MAX_RAINPIPE_TO_WASHMACHINE, List<BlockInfo> COMMONRADIUS, List<LineInfo> MAX_BALCONYBASIN_TO_BALCONY, List<CircleInfo> BALCONY_BUFFER_DISTANCE, List<DimInfo> MIN_BALCONYBASIN_TO_BALCONY)
    {
      this.MAX_RAINPIPE_TO_WASHMACHINE = MAX_RAINPIPE_TO_WASHMACHINE;
      this.COMMONRADIUS = COMMONRADIUS;
      this.MAX_BALCONYBASIN_TO_BALCONY = MAX_BALCONYBASIN_TO_BALCONY;
      this.BALCONY_BUFFER_DISTANCE = BALCONY_BUFFER_DISTANCE;
      this.MIN_BALCONYBASIN_TO_BALCONY = MIN_BALCONYBASIN_TO_BALCONY;
      TakeStopSnap();
    }
    public void TakeStartSnap()
    {
      EnlargeTolerance = MAX_RAINPIPE_TO_WASHMACHINE.Count;
      _fireHydrantOutRaisePipe = COMMONRADIUS.Count;
      _ringPipeSpace = MAX_BALCONYBASIN_TO_BALCONY.Count;
      _raisePipeSpace = BALCONY_BUFFER_DISTANCE.Count;
      _scaleRefugeFireH = MIN_BALCONYBASIN_TO_BALCONY.Count;
    }
    public void TakeStopSnap()
    {
      MinimumAreaTolerance = MAX_RAINPIPE_TO_WASHMACHINE.Count;
      TolLightRangeMax = COMMONRADIUS.Count;
      _raisePipeDistanceStartPoint = MAX_BALCONYBASIN_TO_BALCONY.Count;
      _ringPipeMaxSpace = BALCONY_BUFFER_DISTANCE.Count;
      _scaleTopFloorTopLine = MIN_BALCONYBASIN_TO_BALCONY.Count;
    }
    public void MoveElements(Vector2d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN) => MoveElements(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.ToVector3d());
    public void MoveElements(Vector3d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)
    {
      var TolGroupEvcaEmg = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.ToVector2d();
      foreach (var TolLightRangeSingleSideMin in MAX_RAINPIPE_TO_WASHMACHINE)
      {
        TolLightRangeSingleSideMin.BasePoint += MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
      }
      foreach (var TolLightRangeSingleSideMin in COMMONRADIUS)
      {
        TolLightRangeSingleSideMin.BasePoint += MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
      }
      foreach (var TolLightRangeSingleSideMin in MAX_BALCONYBASIN_TO_BALCONY)
      {
        TolLightRangeSingleSideMin.Line = TolLightRangeSingleSideMin.Line.Offset(TolGroupEvcaEmg);
      }
      foreach (var TolLightRangeSingleSideMin in BALCONY_BUFFER_DISTANCE)
      {
        TolLightRangeSingleSideMin.Circle = TolLightRangeSingleSideMin.Circle.OffsetXY(TolGroupEvcaEmg.X, TolGroupEvcaEmg.Y);
      }
      foreach (var TolLightRangeSingleSideMin in MIN_BALCONYBASIN_TO_BALCONY)
      {
        TolLightRangeSingleSideMin.Point1 = TolLightRangeSingleSideMin.Point1.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
        TolLightRangeSingleSideMin.Point2 = TolLightRangeSingleSideMin.Point2.Offset(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
      }
    }
    public DrainageLayoutManager GetSnapshot()
    {
      return new(MAX_RAINPIPE_TO_WASHMACHINE.GetRange(EnlargeTolerance, MinimumAreaTolerance - EnlargeTolerance), COMMONRADIUS.GetRange(_fireHydrantOutRaisePipe, TolLightRangeMax - _fireHydrantOutRaisePipe), MAX_BALCONYBASIN_TO_BALCONY.GetRange(_ringPipeSpace, _raisePipeDistanceStartPoint - _ringPipeSpace), BALCONY_BUFFER_DISTANCE.GetRange(_raisePipeSpace, _ringPipeMaxSpace - _raisePipeSpace), MIN_BALCONYBASIN_TO_BALCONY.GetRange(_scaleRefugeFireH, _scaleTopFloorTopLine - _scaleRefugeFireH));
    }
    public IEnumerable<LineInfo> GetLineInfos()
    {
      for (int MAX_ANGEL_TOLLERANCE = _ringPipeSpace; MAX_ANGEL_TOLLERANCE < _raisePipeDistanceStartPoint; MAX_ANGEL_TOLLERANCE++)
      {
        yield return MAX_BALCONYBASIN_TO_BALCONY[MAX_ANGEL_TOLLERANCE];
      }
    }
    public IEnumerable<Point3d> GetBrBasePoints()
    {
      for (int MAX_ANGEL_TOLLERANCE = _fireHydrantOutRaisePipe; MAX_ANGEL_TOLLERANCE < TolLightRangeMax; MAX_ANGEL_TOLLERANCE++)
      {
        yield return COMMONRADIUS[MAX_ANGEL_TOLLERANCE].BasePoint;
      }
    }
    public IEnumerable<Point2d> GetLineVertices()
    {
      for (int MAX_ANGEL_TOLLERANCE = _ringPipeSpace; MAX_ANGEL_TOLLERANCE < _raisePipeDistanceStartPoint; MAX_ANGEL_TOLLERANCE++)
      {
        yield return MAX_BALCONYBASIN_TO_BALCONY[MAX_ANGEL_TOLLERANCE].Line.StartPoint;
        yield return MAX_BALCONYBASIN_TO_BALCONY[MAX_ANGEL_TOLLERANCE].Line.EndPoint;
      }
    }
  }
  public class TempGeoFac
  {
    public static IEnumerable<GLineSegment> GetMinConnSegs(List<GLineSegment> TolLane)
    {
      if (TolLane.Count <= THESAURUSHOUSING) yield break;
      var DEFAULT_VOLTAGE = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList();
      var TolLaneProtect = GeoFac.GroupGeometries(DEFAULT_VOLTAGE);
      if (TolLaneProtect.Count >= THESAURUSPERMUTATION)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < TolLaneProtect.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var default_fire_valve_length in GetMinConnSegs(TolLaneProtect[MAX_ANGEL_TOLLERANCE], TolLaneProtect[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING]))
          {
            yield return default_fire_valve_length;
          }
        }
      }
    }
    public static IEnumerable<GLineSegment> GetMinConnSegs(List<LineString> TolBrakeWall, List<LineString> TolInterFilter)
    {
      var BufferFrame = TolBrakeWall.SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).Distinct().ToList();
      var TolIntersect = TolInterFilter.SelectMany(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).Distinct().ToList();
      if (BufferFrame.Count > THESAURUSSTAMPEDE && TolIntersect.Count > THESAURUSSTAMPEDE)
      {
        var BlockScaleNum = TempGeoFac.GetMinDis(BufferFrame, TolIntersect, out LineString TolGroupBlkLane, out LineString TolGroupBlkLaneHead);
        if (BlockScaleNum > THESAURUSSTAMPEDE && TolGroupBlkLane != null && TolGroupBlkLaneHead != null)
        {
          foreach (var default_fire_valve_length in TempGeoFac.TryExtend(GeoFac.GetLines(TolGroupBlkLane).First(), GeoFac.GetLines(TolGroupBlkLaneHead).First(), ELECTROLUMINESCENT))
          {
            if (default_fire_valve_length.IsValid) yield return default_fire_valve_length;
          }
        }
      }
    }
    public static IEnumerable<GLineSegment> TryExtend(GLineSegment TolGroupEmgLightEvac, GLineSegment TolReturnValueDistCheck, double extend)
    {
      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolGroupEmgLightEvac.Extend(extend).ToLineString().Intersection(TolReturnValueDistCheck.Extend(extend).ToLineString()) as Point;
      if (MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN != null)
      {
        var TolReturnValueMinRange = TolGroupEmgLightEvac.ToLineString().Buffer(ASSOCIATIONISTS);
        var TolReturnValueMaxDistance = TolReturnValueDistCheck.ToLineString().Buffer(ASSOCIATIONISTS);
        if (!TolReturnValueMinRange.Intersects(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN))
        {
          var TolReturnValue0Approx = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint2d(), TolGroupEmgLightEvac.StartPoint);
          var TolReturnValueMax = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint2d(), TolGroupEmgLightEvac.EndPoint);
          yield return TolReturnValue0Approx.Length < TolReturnValueMax.Length ? TolReturnValue0Approx : TolReturnValueMax;
        }
        if (!TolReturnValueMaxDistance.Intersects(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN))
        {
          var TolReturnValue0Approx = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint2d(), TolReturnValueDistCheck.StartPoint);
          var TolReturnValueMax = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint2d(), TolReturnValueDistCheck.EndPoint);
          yield return TolReturnValue0Approx.Length < TolReturnValueMax.Length ? TolReturnValue0Approx : TolReturnValueMax;
        }
      }
    }
    public static double GetMinDis(List<LineString> TolReturnValueRange, List<LineString> TolReturnValueRangeTo, out LineString TolRegroupMainYRange, out LineString TolConnectSecPtRange)
    {
      var TolConnectSecPrimAddValue = double.MaxValue;
      TolRegroupMainYRange = null;
      TolConnectSecPtRange = null;
      foreach (var TolGroupBlkLane in TolReturnValueRange)
      {
        foreach (var TolGroupBlkLaneHead in TolReturnValueRangeTo)
        {
          var BlockScaleNum = TolGroupBlkLane.Distance(TolGroupBlkLaneHead);
          if (TolConnectSecPrimAddValue > BlockScaleNum)
          {
            TolConnectSecPrimAddValue = BlockScaleNum;
            TolRegroupMainYRange = TolGroupBlkLane;
            TolConnectSecPtRange = TolGroupBlkLaneHead;
          }
        }
      }
      return TolConnectSecPrimAddValue;
    }
  }
#pragma warning disable
  public enum FlFixType
  {
    NoFix,
    MiddleHigher,
    Lower,
    Higher,
  }
  public enum FlCaseEnum
  {
    Unknown,
    Case1,
    Case2,
    Case3,
  }
  public class FixingLogic1
  {
    public static FlCaseEnum GetFlCase(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane, bool existsXXRoomAtUpperStorey, int tolGroupBlkLaneHead)
    {
      FlCaseEnum maxDeviceplatformArea()
      {
        if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return FlCaseEnum.Unknown;
        if (tolGroupBlkLane == null) return FlCaseEnum.Unknown;
        if (existsXXRoomAtUpperStorey)
        {
          if (tolGroupBlkLaneHead > THESAURUSPERMUTATION)
          {
            return FlCaseEnum.Case1;
          }
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return FlCaseEnum.Case2;
          }
        }
        else
        {
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return FlCaseEnum.Case3;
          }
        }
        return FlCaseEnum.Unknown;
      }
      var tolGroupEmgLightEvac = maxDeviceplatformArea();
      return tolGroupEmgLightEvac;
    }
    public static FlFixType GetFlFixType(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane, bool existsXXRoomAtUpperStorey, int tolGroupBlkLaneHead)
    {
      FlFixType maxDeviceplatformArea()
      {
        if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return FlFixType.NoFix;
        if (tolGroupBlkLane == null) return FlFixType.NoFix;
        if (existsXXRoomAtUpperStorey)
        {
          if (tolGroupBlkLaneHead > THESAURUSPERMUTATION)
          {
            return FlFixType.MiddleHigher;
          }
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return FlFixType.Lower;
          }
        }
        else
        {
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return FlFixType.Higher;
          }
        }
        return FlFixType.NoFix;
      }
      var tolGroupEmgLightEvac = maxDeviceplatformArea();
      return tolGroupEmgLightEvac;
    }
  }
  public enum PlFixType
  {
    NoFix,
  }
  public enum PlCaseEnum
  {
    Unknown,
    Case1,
    Case3,
  }
  public class FixingLogic2
  {
    public static PlCaseEnum GetPlCase(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane, bool existsXXRoomAtUpperStorey, int tolGroupBlkLaneHead)
    {
      PlCaseEnum maxDeviceplatformArea()
      {
        if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return PlCaseEnum.Unknown;
        if (tolGroupBlkLane == null) return PlCaseEnum.Unknown;
        if (existsXXRoomAtUpperStorey)
        {
          if (tolGroupBlkLaneHead > THESAURUSPERMUTATION)
          {
            return PlCaseEnum.Case1;
          }
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return PlCaseEnum.Case1;
          }
        }
        else
        {
          if (tolGroupBlkLaneHead == THESAURUSPERMUTATION)
          {
            return PlCaseEnum.Case3;
          }
        }
        return PlCaseEnum.Unknown;
      }
      var tolGroupEmgLightEvac = maxDeviceplatformArea();
      return tolGroupEmgLightEvac;
    }
    public static PlFixType GetPlFixType(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane, bool existsXXRoomAtUpperStorey, int tolGroupBlkLaneHead)
    {
      return PlFixType.NoFix;
    }
  }
  public class DrainageLabelItem
  {
    public string Label;
    public string Prefix;
    public string D1S;
    public string D2S;
    public string Suffix;
    public int D2
    {
      get
      {
        int.TryParse(D2S, out int DEFAULT_FIRE_VALVE_WIDTH); return DEFAULT_FIRE_VALVE_WIDTH;
      }
    }
    static readonly Regex tolReturnValueDistCheck = new Regex(THESAURUSENCOMPASS);
    public static DrainageLabelItem Parse(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
    {
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == null) return null;
      var tolReturnValueMinRange = tolReturnValueDistCheck.Match(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
      if (!tolReturnValueMinRange.Success) return null;
      return new DrainageLabelItem()
      {
        Label = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE,
        Prefix = tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value,
        D1S = tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value,
        D2S = tolReturnValueMinRange.Groups[INTROPUNITIVENESS].Value,
        Suffix = tolReturnValueMinRange.Groups[QUOTATIONEDIBLE].Value,
      };
    }
  }
#pragma warning disable
  public class DrainageGroupingPipeItem : IEquatable<DrainageGroupingPipeItem>
  {
    public bool MoveTlLineUpper;
    public string Label;
    public bool HasWaterPort;
    public bool HasBasinInKitchenAt1F;
    public bool HasWrappingPipe;
    public bool IsSingleOutlet;
    public string WaterPortLabel;
    public List<ValueItem> Items;
    public List<Hanging> Hangings;
    public bool HasTL;
    public string TlLabel;
    public int MaxTl;
    public int MinTl;
    public int FloorDrainsCountAt1F;
    public bool CanHaveAring;
    public bool IsFL0;
    public bool MergeFloorDrainForFL0;
    public bool HasRainPortForFL0;
    public bool IsConnectedToFloorDrainForFL0;
    public PipeType PipeType;
    public string OutletWrappingPipeRadius;
    public bool Equals(DrainageGroupingPipeItem tolReturnValueMaxDistance)
    {
      return this.HasWaterPort == tolReturnValueMaxDistance.HasWaterPort
          && this.HasWrappingPipe == tolReturnValueMaxDistance.HasWrappingPipe
          && this.MoveTlLineUpper == tolReturnValueMaxDistance.MoveTlLineUpper
          && this.HasBasinInKitchenAt1F == tolReturnValueMaxDistance.HasBasinInKitchenAt1F
          && this.CanHaveAring == tolReturnValueMaxDistance.CanHaveAring
          && this.PipeType == tolReturnValueMaxDistance.PipeType
          && this.OutletWrappingPipeRadius == tolReturnValueMaxDistance.OutletWrappingPipeRadius
          && this.IsFL0 == tolReturnValueMaxDistance.IsFL0
          && this.MergeFloorDrainForFL0 == tolReturnValueMaxDistance.MergeFloorDrainForFL0
          && this.HasRainPortForFL0 == tolReturnValueMaxDistance.HasRainPortForFL0
          && this.IsConnectedToFloorDrainForFL0 == tolReturnValueMaxDistance.IsConnectedToFloorDrainForFL0
          && this.MaxTl == tolReturnValueMaxDistance.MaxTl
          && this.MinTl == tolReturnValueMaxDistance.MinTl
          && this.IsSingleOutlet == tolReturnValueMaxDistance.IsSingleOutlet
          && this.FloorDrainsCountAt1F == tolReturnValueMaxDistance.FloorDrainsCountAt1F
          && this.Items.SeqEqual(tolReturnValueMaxDistance.Items)
          && this.Hangings.SeqEqual(tolReturnValueMaxDistance.Hangings);
    }
    public class Hanging : IEquatable<Hanging>
    {
      public string Storey;
      public int FloorDrainsCount;
      public int WashingMachineFloorDrainsCount;
      public bool IsSeries;
      public bool HasSCurve;
      public bool HasDoubleSCurve;
      public bool HasCleaningPort;
      public bool HasCheckPoint;
      public bool HasDownBoardLine;
      public bool Is4Tune;
      public string RoomName;
      public FlFixType FlFixType;
      public FlCaseEnum FlCaseEnum;
      public PlFixType PlFixType;
      public PlCaseEnum PlCaseEnum;
      public override int GetHashCode()
      {
        return THESAURUSSTAMPEDE;
      }
      public bool Equals(Hanging tolReturnValueMaxDistance)
      {
        return this.FloorDrainsCount == tolReturnValueMaxDistance.FloorDrainsCount
            && this.WashingMachineFloorDrainsCount == tolReturnValueMaxDistance.WashingMachineFloorDrainsCount
            && this.IsSeries == tolReturnValueMaxDistance.IsSeries
            && this.HasSCurve == tolReturnValueMaxDistance.HasSCurve
            && this.HasDoubleSCurve == tolReturnValueMaxDistance.HasDoubleSCurve
            && this.HasCleaningPort == tolReturnValueMaxDistance.HasCleaningPort
            && this.HasCheckPoint == tolReturnValueMaxDistance.HasCheckPoint
            && this.HasDownBoardLine == tolReturnValueMaxDistance.HasDownBoardLine
            && this.Storey == tolReturnValueMaxDistance.Storey
            && this.Is4Tune == tolReturnValueMaxDistance.Is4Tune
            && this.FlFixType == tolReturnValueMaxDistance.FlFixType
            && this.FlCaseEnum == tolReturnValueMaxDistance.FlCaseEnum
            && this.PlFixType == tolReturnValueMaxDistance.PlFixType
            && this.PlCaseEnum == tolReturnValueMaxDistance.PlCaseEnum
            ;
      }
    }
    public struct ValueItem
    {
      public bool Exist;
      public bool HasLong;
      public bool DrawLongHLineHigher;
      public bool HasShort;
    }
    public override int GetHashCode()
    {
      return THESAURUSSTAMPEDE;
    }
  }
  public enum PipeType
  {
    Unknown, Y1L, Y2L, NL, YL, FL0, PLTL, WLTL, WLTLFL, PL, FL, WL, DL, TL,
  }
  public class DrainageGroupedPipeItem
  {
    public List<string> Labels;
    public List<string> WaterPortLabels;
    public bool HasWrappingPipe;
    public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > THESAURUSSTAMPEDE;
    public List<DrainageGroupingPipeItem.ValueItem> Items;
    public List<string> TlLabels;
    public int MinTl;
    public int MaxTl;
    public bool HasTl;
    public PipeType PipeType;
    public List<DrainageGroupingPipeItem.Hanging> Hangings;
    public bool IsSingleOutlet;
    public bool HasBasinInKitchenAt1F;
    public int FloorDrainsCountAt1F;
    public bool CanHaveAring;
    public bool IsFL0;
    public bool MergeFloorDrainForFL0;
    public bool MoveTlLineUpper;
    public bool HasRainPortForFL0;
    public bool IsConnectedToFloorDrainForFL0;
    public string OutletWrappingPipeRadius;
  }
  public class ThwPipeRun
  {
    public string Storey;
    public bool ShowStoreyLabel;
    public bool HasDownBoardLine;
    public bool DrawLongHLineHigher;
    public bool Is4Tune;
    public bool HasShortTranslator;
    public bool HasLongTranslator;
    public bool IsShortTranslatorToLeftOrRight;
    public bool IsLongTranslatorToLeftOrRight;
    public bool ShowShortTranslatorLabel;
    public bool HasCheckPoint;
    public bool IsFirstItem;
    public bool IsLastItem;
    public Hanging LeftHanging;
    public Hanging RightHanging;
    public bool HasCleaningPort;
    public BranchInfo BranchInfo;
  }
  public class BranchInfo
  {
    public bool FirstLeftRun;
    public bool MiddleLeftRun;
    public bool LastLeftRun;
    public bool FirstRightRun;
    public bool MiddleRightRun;
    public bool LastRightRun;
    public bool BlueToLeftFirst;
    public bool BlueToLeftMiddle;
    public bool BlueToLeftLast;
    public bool BlueToRightFirst;
    public bool BlueToRightMiddle;
    public bool BlueToRightLast;
    public bool HasLongTranslatorToLeft;
    public bool HasLongTranslatorToRight;
    public bool IsLast;
  }
  public class Hanging
  {
    public int FloorDrainsCount;
    public bool IsSeries;
    public bool HasSCurve;
    public bool HasDoubleSCurve;
    public bool HasUnderBoardLabel;
  }
  public class ThwOutput
  {
    public int LinesCount = THESAURUSHOUSING;
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
    public int HangingCount = THESAURUSSTAMPEDE;
    public Hanging Hanging1;
    public Hanging Hanging2;
  }
  public class ThwPipeLine
  {
    public List<string> Labels;
    public bool? IsLeftOrMiddleOrRight;
    public List<ThwPipeRun> PipeRuns;
    public ThwOutput Output;
  }
  public class PipeRun : IEquatable<PipeRun>
  {
    public int Index;
    public string Storey;
    public bool Exists;
    public bool HasLong;
    public bool HasShort;
    public bool HasCleaningPort;
    public bool HasBasin;
    public int FDSCount;
    public int CPSCount;
    public int WPSCount;
    public string WaterBucket;
    public static bool operator ==(PipeRun me, PipeRun tolReturnValueMaxDistance)
    {
      return me.Equals(tolReturnValueMaxDistance);
    }
    public static bool operator !=(PipeRun me, PipeRun tolReturnValueMaxDistance) => !(me == tolReturnValueMaxDistance);
    public bool Equals(PipeRun tolReturnValueMaxDistance)
    {
      if (this.Index != tolReturnValueMaxDistance.Index) return INTRAVASCULARLY;
      if (this.Storey != tolReturnValueMaxDistance.Storey) return INTRAVASCULARLY;
      if (this.Exists != tolReturnValueMaxDistance.Exists) return INTRAVASCULARLY;
      if (this.Exists == tolReturnValueMaxDistance.Exists == INTRAVASCULARLY) return THESAURUSOBSTINACY;
      if (this.HasLong != tolReturnValueMaxDistance.HasLong) return INTRAVASCULARLY;
      if (this.HasShort != tolReturnValueMaxDistance.HasShort) return INTRAVASCULARLY;
      if (this.HasCleaningPort != tolReturnValueMaxDistance.HasCleaningPort) return INTRAVASCULARLY;
      if (this.HasBasin != tolReturnValueMaxDistance.HasBasin) return INTRAVASCULARLY;
      if (this.FDSCount != tolReturnValueMaxDistance.FDSCount) return INTRAVASCULARLY;
      if (this.CPSCount != tolReturnValueMaxDistance.CPSCount) return INTRAVASCULARLY;
      if (this.WPSCount != tolReturnValueMaxDistance.WPSCount) return INTRAVASCULARLY;
      if (this.WaterBucket != tolReturnValueMaxDistance.WaterBucket) return INTRAVASCULARLY;
      return THESAURUSOBSTINACY;
    }
    public override int GetHashCode()
    {
      return THESAURUSSTAMPEDE;
    }
  }
  public class PipeLine : IEquatable<PipeLine>
  {
    public List<string> Labels = new();
    public readonly List<PipeRun> Runs = new();
    public PipeType PipeType;
    public string Outlet;
    public string WPRadius;
    public static bool operator ==(PipeLine me, PipeLine tolReturnValueMaxDistance)
    {
      return me.Equals(tolReturnValueMaxDistance);
    }
    public static bool operator !=(PipeLine me, PipeLine tolReturnValueMaxDistance) => !(me == tolReturnValueMaxDistance);
    public bool Equals(PipeLine tolReturnValueMaxDistance)
    {
      if (this.PipeType != tolReturnValueMaxDistance.PipeType) return INTRAVASCULARLY;
      if (this.Outlet != tolReturnValueMaxDistance.Outlet) return INTRAVASCULARLY;
      if (this.WPRadius != tolReturnValueMaxDistance.WPRadius) return INTRAVASCULARLY;
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < Runs.Count; MAX_ANGEL_TOLLERANCE++)
      {
        if (this.Runs[MAX_ANGEL_TOLLERANCE] != tolReturnValueMaxDistance.Runs[MAX_ANGEL_TOLLERANCE]) return INTRAVASCULARLY;
      }
      return THESAURUSOBSTINACY;
    }
    public override int GetHashCode()
    {
      return THESAURUSSTAMPEDE;
    }
  }
  public class StoreyItem
  {
    public List<int> Ints;
    public List<string> Labels;
    public void Init()
    {
      Ints ??= new List<int>();
      Labels ??= new List<string>();
    }
  }
  public class ThwPipeLineGroup
  {
    public ThwOutput Output;
    public ThwPipeLine TL;
    public ThwPipeLine DL;
    public ThwPipeLine PL;
    public ThwPipeLine FL;
    public int LinesCount
    {
      get
      {
        var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = THESAURUSSTAMPEDE;
        if (TL != null) ++MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        if (DL != null) ++MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        if (PL != null) ++MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        if (FL != null) ++MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
        return MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN;
      }
    }
    public ThwPipeLineGroup Clone()
    {
      return this.ToCadJson().FromCadJson<ThwPipeLineGroup>();
    }
  }
  class FloorDrainCbItem
  {
    public Point2d BasePt;
    public string Name;
    public bool LeftOrRight;
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
  public class CommandContext
  {
    public Point3dCollection range;
    public StoreyContext StoreyContext;
    public DrainageSystemDiagramViewModel ViewModel;
    public System.Windows.Window window;
  }
  public class DrainageSystemDiagram
  {
    public static void SetLabelStylesForRainNote(params Entity[] MAX_TAG_YPOSITION)
    {
      foreach (var tolReturnValue0Approx in MAX_TAG_YPOSITION)
      {
        tolReturnValue0Approx.Layer = CIRCUMCONVOLUTION;
        ByLayer(tolReturnValue0Approx);
        if (tolReturnValue0Approx is DBText TolLightRangeMin)
        {
          TolLightRangeMin.WidthFactor = THESAURUSDISPASSIONATE;
          SetTextStyleLazy(TolLightRangeMin, CONTROVERSIALLY);
        }
      }
    }
    public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
    {
      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSDOMESTIC, -QUOTATIONWITTIG), new Vector2d(PROKELEUSMATIKOS, THESAURUSSTAMPEDE) };
      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt);
      var wordPt = isLeftOrRight ? TolLane[THESAURUSHOUSING].EndPoint : TolLane[THESAURUSHOUSING].StartPoint;
      var repeated_point_distance = THESAURUSTENACIOUS;
      var line_coincide_tolerance = THESAURUSENDANGER;
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane);
      SetLabelStylesForRainNote(tolReturnValueRange.ToArray());
      var TolLightRangeMin = DrawTextLazy(repeated_point_distance, line_coincide_tolerance, wordPt);
      SetLabelStylesForRainNote(TolLightRangeMin);
    }
    public static void DrawWashingMachineRaisingSymbol(Point2d tolReturnValueMax, bool isLeftOrRight)
    {
      if (isLeftOrRight)
      {
        var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(HYDROELECTRICITY, -THESAURUSMARRIAGE);
        DrawBlockReference(SUCCESSLESSNESS, (tolReturnValueMax - MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).ToPoint3d(), tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          tolReturnValueRangeTo.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
          if (tolReturnValueRangeTo.IsDynamicBlock)
          {
            tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, THESAURUSLINING);
          }
        });
      }
      else
      {
        var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(-HYDROELECTRICITY, -THESAURUSMARRIAGE);
        DrawBlockReference(SUCCESSLESSNESS, (tolReturnValueMax - MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).ToPoint3d(), tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          tolReturnValueRangeTo.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
          if (tolReturnValueRangeTo.IsDynamicBlock)
          {
            tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, THESAURUSLINING);
          }
        });
      }
    }
    public static double LONG_TRANSLATOR_HEIGHT1 = SUBCATEGORIZING;
    public static double CHECKPOINT_OFFSET_Y = THESAURUSNECESSITOUS;
    public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, Point2d SIDEWATERBUCKET_X_INDENT, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> tolRegroupMainYRange, List<string> tolConnectSecPtRange, DrainageSystemDiagramViewModel viewModel, ExtraInfo exInfo)
    {
      var allNumStoreyLabels = tolRegroupMainYRange.Select(MAX_ANGEL_TOLLERANCE => MAX_ANGEL_TOLLERANCE + THESAURUSASPIRATION).ToList();
      var SIDEWATERBUCKET_Y_INDENT = allNumStoreyLabels.Concat(tolConnectSecPtRange).ToList();
      var MAX_TAG_LENGTH = SIDEWATERBUCKET_Y_INDENT.Count - THESAURUSHOUSING;
      var TEXT_INDENT = THESAURUSSTAMPEDE;
      var OFFSET_X = QUOTATIONLETTERS;
      var SPAN_X = BALANOPHORACEAE + QUOTATIONWITTIG + THESAURUSNAUGHT;
      var HEIGHT = THESAURUSINCOMING;
      {
        if (viewModel?.Params?.StoreySpan is double MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)
        {
          HEIGHT = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
        }
      }
      var COUNT = pipeGroupItems.Count;
      var MAX_DEVICE_TO_BALCONY = HEIGHT - THESAURUSINCOMING;
      var __dy = THESAURUSHYPNOTIC;
      DrawDrainageSystemDiagram(SIDEWATERBUCKET_X_INDENT, pipeGroupItems, allNumStoreyLabels, SIDEWATERBUCKET_Y_INDENT, MAX_TAG_LENGTH, TEXT_INDENT, OFFSET_X, SPAN_X, HEIGHT, COUNT, MAX_DEVICE_TO_BALCONY, __dy, viewModel, exInfo);
    }
    public class Opt
    {
      double MAX_TAG_XPOSITION;
      double _dy;
      public List<Vector2d> tolConnectSecPrimAddValue => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONBASTARD - MAX_DEVICE_TO_BALCONY) };
      public List<Vector2d> _tolGroupBlkLane => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + MAX_TAG_XPOSITION), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - MAX_DEVICE_TO_BALCONY - MAX_TAG_XPOSITION) };
      public List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - MAX_DEVICE_TO_BALCONY + __dy) };
      public List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - MAX_DEVICE_TO_BALCONY - __dy) };
      public List<Vector2d> _tolGroupBlkLaneHead => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -COOPERATIVENESS - MAX_DEVICE_TO_BALCONY) };
      public List<Vector2d> _tolGroupEmgLightEvac => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - MAX_DEVICE_TO_BALCONY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
      public List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - MAX_DEVICE_TO_BALCONY + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
      public List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + MAX_DEVICE_TO_BALCONY), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - MAX_DEVICE_TO_BALCONY - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
      public List<Vector2d> vecs4 => _tolGroupBlkLane.GetYAxisMirror();
      public List<Vector2d> vecs5 => _tolGroupBlkLaneHead.GetYAxisMirror();
      public List<Vector2d> vecs6 => _tolGroupEmgLightEvac.GetYAxisMirror();
      public List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
      public List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
      public List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
      public Vector2d vec7 => new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE);
      public Point2d SIDEWATERBUCKET_X_INDENT;
      public List<DrainageGroupedPipeItem> pipeGroupItems;
      public List<string> allNumStoreyLabels;
      public List<string> SIDEWATERBUCKET_Y_INDENT;
      public int MAX_TAG_LENGTH;
      public int TEXT_INDENT;
      public double OFFSET_X;
      public double SPAN_X;
      public double HEIGHT;
      public int COUNT;
      public double MAX_DEVICE_TO_BALCONY;
      public int __dy;
      public DrainageSystemDiagramViewModel viewModel;
      public ExtraInfo exInfo;
      public void Draw()
      {
        {
          var _tolReturnValueDistCheck = new List<int>(SIDEWATERBUCKET_Y_INDENT.Count);
          var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = THESAURUSSTAMPEDE;
          var _vm = FloorHeightsViewModel.Instance;
          static bool test(string TolLightRangeSingleSideMax, int TolLightRangeMin)
          {
            var tolReturnValueMinRange = Regex.Match(TolLightRangeSingleSideMax, QUOTATIONSTYLOGRAPHIC);
            if (tolReturnValueMinRange.Success)
            {
              if (int.TryParse(tolReturnValueMinRange.Groups[THESAURUSHOUSING].Value, out int _tolReturnValue0Approx) && int.TryParse(tolReturnValueMinRange.Groups[THESAURUSPERMUTATION].Value, out int TolGroupEvcaEmg))
              {
                var _tolReturnValueMaxDistance = Math.Min(_tolReturnValue0Approx, TolGroupEvcaEmg);
                var _tolReturnValueMinRange = Math.Max(_tolReturnValue0Approx, TolGroupEvcaEmg);
                for (int MAX_ANGEL_TOLLERANCE = _tolReturnValueMaxDistance; MAX_ANGEL_TOLLERANCE <= _tolReturnValueMinRange; MAX_ANGEL_TOLLERANCE++)
                {
                  if (MAX_ANGEL_TOLLERANCE == TolLightRangeMin) return THESAURUSOBSTINACY;
                }
              }
              else
              {
                return INTRAVASCULARLY;
              }
            }
            tolReturnValueMinRange = Regex.Match(TolLightRangeSingleSideMax, TETRAIODOTHYRONINE);
            if (tolReturnValueMinRange.Success)
            {
              if (int.TryParse(TolLightRangeSingleSideMax, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN))
              {
                if (MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN == TolLightRangeMin) return THESAURUSOBSTINACY;
              }
            }
            return INTRAVASCULARLY;
          }
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < SIDEWATERBUCKET_Y_INDENT.Count; MAX_ANGEL_TOLLERANCE++)
          {
            _tolReturnValueDistCheck.Add(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
            var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _vm.GeneralFloor;
            if (_vm.ExistsSpecialFloor) MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = _vm.Items.FirstOrDefault(tolReturnValueMinRange => test(tolReturnValueMinRange.Floor, GetStoreyScore(SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE])))?.Height ?? MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
            MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN += MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
          }
          var WELL_TO_WALL_OFFSET = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < SIDEWATERBUCKET_Y_INDENT.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var tolGroupBlkLane = SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE];
            string textIndent()
            {
              if (tolGroupBlkLane is THESAURUSREGION) return MULTINATIONALLY;
              var tolGroupEmgLightEvac = (_tolReturnValueDistCheck[MAX_ANGEL_TOLLERANCE] / LAUTENKLAVIZIMBEL).ToString(THESAURUSINFINITY); ;
              if (tolGroupEmgLightEvac == THESAURUSINFINITY) return MULTINATIONALLY;
              return tolGroupEmgLightEvac;
            }
            var _tolReturnValueMax = SIDEWATERBUCKET_X_INDENT.OffsetY(HEIGHT * MAX_ANGEL_TOLLERANCE);
            textHeight(tolGroupBlkLane, _tolReturnValueMax.ToPoint3d(), WELL_TO_WALL_OFFSET, textIndent());
          }
        }
        void _DrawWrappingPipe(Point2d basePt)
        {
          DrawBlockReference(THESAURUSSTRINGENT, basePt.ToPoint3d(), tolReturnValueRangeTo =>
          {
            tolReturnValueRangeTo.Layer = THESAURUSDEFAULTER;
            ByLayer(tolReturnValueRangeTo);
          });
        }
        void DrawOutlets5(Point2d SIDEWATERBUCKET_X_INDENT, ThwOutput output, DrainageGroupedPipeItem _tolReturnValueRange)
        {
          var values = output.DirtyWaterWellValues;
          var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL - THESAURUSDOMESTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
          var TolLane = LineCoincideTolerance.ToGLineSegments(SIDEWATERBUCKET_X_INDENT);
          TolLane.RemoveAt(INTROPUNITIVENESS);
          DrawDiryWaterWells1(TolLane[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
          if (output.HasWrappingPipe1) _DrawWrappingPipe(TolLane[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
          if (output.HasWrappingPipe2) _DrawWrappingPipe(TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
          DrawNoteText(output.DN1, TolLane[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSATTACHMENT));
          DrawNoteText(output.DN2, TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSATTACHMENT));
          if (output.HasCleaningPort1) DrawCleaningPort(TolLane[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
          if (output.HasCleaningPort2) DrawCleaningPort(TolLane[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
          var _raiseDistanceToStartDefault = TolLane[THESAURUSCOMMUNICATION].EndPoint;
          DrawFloorDrain((_raiseDistanceToStartDefault.OffsetX(-THESAURUSPERVADE) + new Vector2d(-THESAURUSOFFEND + THESAURUSDOMESTIC, THESAURUSSTAMPEDE)).ToPoint3d(), THESAURUSOBSTINACY);
        }
        string getDSCurveValue()
        {
          return viewModel?.Params?.Basin ?? PERIODONTOCLASIA;
        }
        bool getShouldToggleBlueMiddleLine()
        {
          return viewModel?.Params?.H ?? INTRAVASCULARLY;
        }
        for (int MAX_ANGLE_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGLE_TOLLERANCE < COUNT; MAX_ANGLE_TOLLERANCE++)
        {
          var dome_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
          var vent_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
          var dome_layer = THESAURUSCONTROVERSY;
          var vent_layer = THUNDERSTRICKEN;
          void drawDomePipe(GLineSegment default_fire_valve_length)
          {
            if (default_fire_valve_length.IsValid) dome_lines.Add(default_fire_valve_length);
          }
          void drawDomePipes(IEnumerable<GLineSegment> TolLane)
          {
            var SidewaterbucketXIndent = INTRAVASCULARLY;
            foreach (var default_fire_valve_length in TolLane.Where(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.IsValid))
            {
              if (!SidewaterbucketXIndent)
              {
                SidewaterbucketXIndent = THESAURUSOBSTINACY;
              }
              dome_lines.Add(default_fire_valve_length);
            }
          }
          void drawVentPipe(GLineSegment default_fire_valve_length)
          {
            if (default_fire_valve_length.IsValid) vent_lines.Add(default_fire_valve_length);
          }
          void drawVentPipes(IEnumerable<GLineSegment> TolLane)
          {
            var SidewaterbucketXIndent = INTRAVASCULARLY;
            foreach (var default_fire_valve_length in TolLane.Where(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN => MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.IsValid))
            {
              if (!SidewaterbucketXIndent)
              {
                SidewaterbucketXIndent = THESAURUSOBSTINACY;
              }
              vent_lines.Add(default_fire_valve_length);
            }
          }
          string getWashingMachineFloorDrainDN()
          {
            return viewModel?.Params?.WashingMachineFloorDrainDN ?? QUOTATIONBREWSTER;
          }
          string getBasinDN()
          {
            return viewModel?.Params?.BasinDN ?? QUOTATIONBREWSTER;
          }
          string getOtherFloorDrainDN()
          {
            return viewModel?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER;
          }
          void Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg)
          {
            _tolReturnValue0Approx = viewModel?.Params?.WashingMachineFloorDrainDN ?? QUOTATIONBREWSTER;
            TolGroupEvcaEmg = _tolReturnValue0Approx;
            if (TolGroupEvcaEmg == QUOTATIONBREWSTER) TolGroupEvcaEmg = QUOTATIONDOPPLER;
          }
          bool getCouldHavePeopleOnRoof()
          {
            return viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSOBSTINACY;
          }
          var _tolReturnValueRange = pipeGroupItems[MAX_ANGLE_TOLLERANCE];
          var thwPipeLine = new ThwPipeLine();
          thwPipeLine.Labels = _tolReturnValueRange.Labels.Concat(_tolReturnValueRange.TlLabels.Yield()).ToList();
          var _tolReturnValueRangeTo = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
          if (_tolReturnValueRange.PipeType == PipeType.FL)
          {
            dome_layer = THESAURUSADVERSITY;
          }
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var tolGroupBlkLane = allNumStoreyLabels[MAX_ANGEL_TOLLERANCE];
            var run = _tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].Exist ? new ThwPipeRun()
            {
              HasLongTranslator = _tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].HasLong,
              HasShortTranslator = _tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].HasShort,
              HasCleaningPort = _tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING)?.HasCleaningPort ?? INTRAVASCULARLY,
              HasCheckPoint = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].HasCheckPoint,
              HasDownBoardLine = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].HasDownBoardLine,
              DrawLongHLineHigher = _tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].DrawLongHLineHigher,
              Is4Tune = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].Is4Tune,
            } : null;
            _tolReturnValueRangeTo.Add(run);
          }
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var floorDrainsCount = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].FloorDrainsCount;
            var hasSCurve = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].HasSCurve;
            var hasDoubleSCurve = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].HasDoubleSCurve;
            if (hasDoubleSCurve)
            {
              var run = _tolReturnValueRangeTo.TryGet(MAX_ANGEL_TOLLERANCE);
              if (run != null)
              {
                var hanging = run.LeftHanging ??= new Hanging();
                hanging.HasDoubleSCurve = hasDoubleSCurve;
              }
            }
            if (floorDrainsCount > THESAURUSSTAMPEDE || hasSCurve)
            {
              var run = _tolReturnValueRangeTo.TryGet(MAX_ANGEL_TOLLERANCE - THESAURUSHOUSING);
              if (run != null)
              {
                var hanging = run.LeftHanging ??= new Hanging();
                hanging.FloorDrainsCount = floorDrainsCount;
                hanging.HasSCurve = hasSCurve;
              }
            }
          }
          for (int MAX_ANGEL_TOLLERANCE = _tolReturnValueRangeTo.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE--)
          {
            var DEFAULT_FIRE_VALVE_WIDTH = _tolReturnValueRangeTo[MAX_ANGEL_TOLLERANCE];
            if (DEFAULT_FIRE_VALVE_WIDTH == null) continue;
            if (DEFAULT_FIRE_VALVE_WIDTH.HasLongTranslator)
            {
              DEFAULT_FIRE_VALVE_WIDTH.IsLongTranslatorToLeftOrRight = THESAURUSOBSTINACY;
            }
          }
          {
            foreach (var DEFAULT_FIRE_VALVE_WIDTH in _tolReturnValueRangeTo)
            {
              if (DEFAULT_FIRE_VALVE_WIDTH?.HasShortTranslator == THESAURUSOBSTINACY)
              {
                DEFAULT_FIRE_VALVE_WIDTH.IsShortTranslatorToLeftOrRight = INTRAVASCULARLY;
                if (DEFAULT_FIRE_VALVE_WIDTH.HasLongTranslator && DEFAULT_FIRE_VALVE_WIDTH.IsLongTranslatorToLeftOrRight)
                {
                  DEFAULT_FIRE_VALVE_WIDTH.IsShortTranslatorToLeftOrRight = THESAURUSOBSTINACY;
                }
              }
            }
          }
          Point2d drawHanging(Point2d MAX_TAG_LENGTH, Hanging hanging)
          {
            var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE), new Vector2d(THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
            var TolLane = LineCoincideTolerance.ToGLineSegments(MAX_TAG_LENGTH);
            {
              var _segs = TolLane.ToList();
              if (hanging.FloorDrainsCount == THESAURUSHOUSING)
              {
                _segs.RemoveAt(INTROPUNITIVENESS);
              }
              _segs.RemoveAt(THESAURUSPERMUTATION);
              DrawDomePipes(_segs);
            }
            {
              var _tol_avg_column_dist = LineCoincideTolerance.ToPoint2ds(MAX_TAG_LENGTH);
              {
                var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_avg_column_dist[THESAURUSHOUSING];
                var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(THESAURUSQUAGMIRE, THESAURUSQUAGMIRE);
                if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                {
                  MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = default;
                }
                var _raiseDistanceToStartDefault = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                if (hanging.HasSCurve)
                {
                  DrawSCurve(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, INTRAVASCULARLY);
                }
                if (hanging.HasDoubleSCurve)
                {
                  if (!_raiseDistanceToStartDefault.Equals(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN))
                  {
                    dome_lines.Add(new GLineSegment(_raiseDistanceToStartDefault, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN));
                  }
                  DrawDSCurve(_raiseDistanceToStartDefault, INTRAVASCULARLY, getDSCurveValue());
                }
              }
              if (hanging.FloorDrainsCount >= THESAURUSHOUSING)
              {
                DrawFloorDrain(_tol_avg_column_dist[THESAURUSPERMUTATION].ToPoint3d(), INTRAVASCULARLY);
              }
              if (hanging.FloorDrainsCount >= THESAURUSPERMUTATION)
              {
                DrawFloorDrain(_tol_avg_column_dist[QUOTATIONEDIBLE].ToPoint3d(), INTRAVASCULARLY);
              }
            }
            MAX_TAG_LENGTH = TolLane.Last().EndPoint;
            return MAX_TAG_LENGTH;
          }
          void DrawOutlets1(Point2d basePoint1, double BranchPortToMainDistance, ThwOutput output, bool isRainWaterWell = INTRAVASCULARLY, Vector2d? _tol_light_range_min = null)
          {
            Point2d _tol_lane, _tol_uniform_side_lenth;
            if (output.DirtyWaterWellValues != null)
            {
              var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(-THESAURUSINHERIT - THESAURUSDOMESTIC, -THESAURUSDERELICTION - QUOTATIONPITUITARY);
              var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = basePoint1 + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
              if (_tol_light_range_min.HasValue)
              {
                MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN += _tol_light_range_min.Value;
              }
              var values = output.DirtyWaterWellValues;
              DrawDiryWaterWells1(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, values, isRainWaterWell);
            }
            {
              var MAX_DEVICE_TO_DEVICE = BranchPortToMainDistance - THESAURUSEXECRABLE;
              var MAX_TAG_XPOSITION = QUOTATIONTRANSFERABLE;
              if (output.LinesCount == THESAURUSHOUSING)
              {
                MAX_TAG_XPOSITION = -THESAURUSPRIMARY;
              }
              var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY - CONSPICUOUSNESS + MAX_TAG_XPOSITION), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDERELICTION), new Vector2d(QUOTATIONDENNIS + MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSBLESSING), new Vector2d(-THESAURUSCLIMATE - MAX_DEVICE_TO_DEVICE, -QUOTATIONBASTARD), new Vector2d(THESAURUSCOLOSSAL + MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) };
              {
                var TolLane = LineCoincideTolerance.ToGLineSegments(basePoint1);
                if (output.LinesCount == THESAURUSHOUSING)
                {
                  drawDomePipes(TolLane.Take(INTROPUNITIVENESS));
                }
                else if (output.LinesCount > THESAURUSHOUSING)
                {
                  TolLane.RemoveAt(THESAURUSDESTITUTE);
                  if (!output.HasVerticalLine2) TolLane.RemoveAt(SUPERLATIVENESS);
                  TolLane.RemoveAt(INTROPUNITIVENESS);
                  drawDomePipes(TolLane);
                }
              }
              var _tol_avg_column_dist = LineCoincideTolerance.ToPoint2ds(basePoint1);
              if (output.HasWrappingPipe1) _DrawWrappingPipe(_tol_avg_column_dist[INTROPUNITIVENESS].OffsetX(THESAURUSHYPNOTIC));
              if (output.HasWrappingPipe2) _DrawWrappingPipe(_tol_avg_column_dist[QUOTATIONEDIBLE].OffsetX(THESAURUSHYPNOTIC));
              if (output.HasWrappingPipe3) _DrawWrappingPipe(_tol_avg_column_dist[THESAURUSSCARCE].OffsetX(THESAURUSHYPNOTIC));
              if (output.HasWrappingPipe1 && !output.HasWrappingPipe2 && !output.HasWrappingPipe3)
              {
                if (_tolReturnValueRange.OutletWrappingPipeRadius != null)
                {
                  static void DrawLine(string layer, params GLineSegment[] TolLane)
                  {
                    var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
                    foreach (var TolUniformSideLenth in tolReturnValueRange)
                    {
                      TolUniformSideLenth.Layer = layer;
                      ByLayer(TolUniformSideLenth);
                    }
                  }
                  static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = HELIOCENTRICISM)
                  {
                    DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE } }, cb: tolReturnValueRangeTo => { ByLayer(tolReturnValueRangeTo); });
                  }
                  var _tolRegroupMainYRange = _tol_avg_column_dist[INTROPUNITIVENESS].OffsetX(THESAURUSDOMESTIC);
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(-DOCTRINARIANISM);
                  var _tolConnectSecPrimAddValue = _tolConnectSecPtRange.OffsetX(QUOTATIONWITTIG);
                  var layer = THESAURUSSTRIPED;
                  DrawLine(layer, new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                  DrawLine(layer, new GLineSegment(_tolConnectSecPrimAddValue, _tolConnectSecPtRange));
                  DrawStoreyHeightSymbol(_tolConnectSecPrimAddValue, THESAURUSSTRIPED, _tolReturnValueRange.OutletWrappingPipeRadius);
                }
              }
              var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR);
              DrawNoteText(output.DN1, _tol_avg_column_dist[INTROPUNITIVENESS] + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              DrawNoteText(output.DN2, _tol_avg_column_dist[QUOTATIONEDIBLE] + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              DrawNoteText(output.DN3, _tol_avg_column_dist[THESAURUSSCARCE] + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              if (output.HasCleaningPort1) DrawCleaningPort(_tol_avg_column_dist[THESAURUSPERMUTATION].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
              if (output.HasCleaningPort2) DrawCleaningPort(_tol_avg_column_dist[THESAURUSCOMMUNICATION].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
              if (output.HasCleaningPort3) DrawCleaningPort(_tol_avg_column_dist[THESAURUSACTUAL].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
              _tol_lane = _tol_avg_column_dist[SUPERLATIVENESS];
              _tol_uniform_side_lenth = _tol_avg_column_dist.Last();
            }
            if (output.HasLargeCleaningPort)
            {
              var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONAFGHAN) };
              var TolLane = LineCoincideTolerance.ToGLineSegments(_tol_uniform_side_lenth);
              drawDomePipes(TolLane);
              DrawCleaningPort(TolLane.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSPERMUTATION);
            }
            if (output.HangingCount == THESAURUSHOUSING)
            {
              var hang = output.Hanging1;
              Point2d lastPt = _tol_lane;
              {
                var TolLane = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, INDIGESTIBLENESS), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) }.ToGLineSegments(lastPt);
                drawDomePipes(TolLane);
                lastPt = TolLane.Last().EndPoint;
              }
              {
                lastPt = drawHanging(lastPt, output.Hanging1);
              }
            }
            else if (output.HangingCount == THESAURUSPERMUTATION)
            {
              var vs1 = new List<Vector2d> { new Vector2d(THESAURUSNECESSITY, THESAURUSNECESSITY), new Vector2d(QUOTATIONDEFLUVIUM, QUOTATIONDEFLUVIUM) };
              var _tol_avg_column_dist = vs1.ToPoint2ds(_tol_uniform_side_lenth);
              drawDomePipes(vs1.ToGLineSegments(_tol_uniform_side_lenth));
              drawHanging(_tol_avg_column_dist.Last(), output.Hanging1);
              var MAX_DEVICE_TO_DEVICE = output.Hanging1.FloorDrainsCount == THESAURUSPERMUTATION ? POLYOXYMETHYLENE : THESAURUSSTAMPEDE;
              var vs2 = new List<Vector2d> { new Vector2d(HYDROCOTYLACEAE + MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), new Vector2d(QUOTATIONDEFLUVIUM, QUOTATIONDEFLUVIUM) };
              drawDomePipes(vs2.ToGLineSegments(_tol_avg_column_dist[THESAURUSHOUSING]));
              drawHanging(vs2.ToPoint2ds(_tol_avg_column_dist[THESAURUSHOUSING]).Last(), output.Hanging2);
            }
          }
          void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] well_to_wall_offset)
          {
            {
            }
            {
              foreach (var TolLightRangeSingleSideMin in well_to_wall_offset)
              {
                if (TolLightRangeSingleSideMin?.Storey == THESAURUSARGUMENTATIVE)
                {
                  if (_tolReturnValueRange.CanHaveAring)
                  {
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLightRangeSingleSideMin.BasePoint;
                    var default_fire_valve_length = new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                    drawDomePipe(default_fire_valve_length);
                    DrawAiringSymbol(default_fire_valve_length.EndPoint, getCouldHavePeopleOnRoof());
                  }
                }
              }
            }
            int counterPipeButtomHeightSymbol = THESAURUSSTAMPEDE;
            bool hasDrawedSCurveLabel = INTRAVASCULARLY;
            bool hasDrawedDSCurveLabel = INTRAVASCULARLY;
            bool hasDrawedCleaningPort = INTRAVASCULARLY;
            void _DrawLabel(string text1, string text2, Point2d basePt, bool leftOrRight, double line_coincide_tolerance)
            {
              var w = THESAURUSEXECRABLE - THESAURUSDICTATORIAL;
              var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, line_coincide_tolerance), new Vector2d(leftOrRight ? -w : w, THESAURUSSTAMPEDE) };
              var TolLane = LineCoincideTolerance.ToGLineSegments(basePt);
              var tolReturnValueRange = DrawLineSegmentsLazy(TolLane);
              Dr.SetLabelStylesForDraiNote(tolReturnValueRange.ToArray());
              var _raiseDistanceToStartDefault = TolLane.Last().EndPoint.OffsetY(THESAURUSENTREPRENEUR);
              if (!string.IsNullOrEmpty(text1))
              {
                var TolLightRangeMin = DrawTextLazy(text1, THESAURUSENDANGER, _raiseDistanceToStartDefault);
                Dr.SetLabelStylesForDraiNote(TolLightRangeMin);
              }
              if (!string.IsNullOrEmpty(text2))
              {
                var TolLightRangeMin = DrawTextLazy(text2, THESAURUSENDANGER, _raiseDistanceToStartDefault.OffsetXY(THESAURUSMORTUARY, -THESAURUSDOMESTIC));
                Dr.SetLabelStylesForDraiNote(TolLightRangeMin);
              }
            }
            void _DrawHorizontalLineOnPipeRun(Point3d basePt)
            {
              if (_tolReturnValueRange.Labels.Any(TolLightRangeSingleSideMax => IsFL(TolLightRangeSingleSideMax)))
              {
                ++counterPipeButtomHeightSymbol;
                if (counterPipeButtomHeightSymbol == THESAURUSPERMUTATION)
                {
                  var _raiseDistanceToStartDefault = basePt.ToPoint2d();
                  var h = HEIGHT * THESAURUSDISPASSIONATE;
                  if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                  {
                    h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSSURPRISED;
                  }
                  _raiseDistanceToStartDefault = _raiseDistanceToStartDefault.OffsetY(h);
                  DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, _raiseDistanceToStartDefault);
                }
              }
              DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
            }
            void _DrawSCurve(Vector2d vec7, Point2d _tolRegroupMainYRange, bool leftOrRight)
            {
              if (!hasDrawedSCurveLabel)
              {
                hasDrawedSCurveLabel = THESAURUSOBSTINACY;
                _DrawLabel(THESAURUSMOLEST, THESAURUSSCOUNDREL, _tolRegroupMainYRange + new Vector2d(-THESAURUSGETAWAY, THIGMOTACTICALLY), THESAURUSOBSTINACY, QUOTATIONBASTARD);
              }
              DrawSCurve(vec7, _tolRegroupMainYRange, leftOrRight);
            }
            void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
            {
              if (_tolReturnValueRange.Labels.Any(TolLightRangeSingleSideMax => IsPL(TolLightRangeSingleSideMax)))
              {
                ++counterPipeButtomHeightSymbol;
                if (counterPipeButtomHeightSymbol == THESAURUSPERMUTATION)
                {
                  var _raiseDistanceToStartDefault = basePt.ToPoint2d();
                  DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, _raiseDistanceToStartDefault);
                }
              }
              var _tolRegroupMainYRange = basePt.ToPoint2d();
              if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax)))
              {
                hasDrawedCleaningPort = THESAURUSOBSTINACY;
                _DrawLabel(QUOTATIONHUMPBACK, DISCOMMODIOUSNESS, _tolRegroupMainYRange + new Vector2d(-THESAURUSOUTLANDISH, RETROGRESSIVELY), THESAURUSOBSTINACY, PROCRASTINATORY);
              }
              DrawCleaningPort(basePt, leftOrRight, scale);
            }
            void _DrawCheckPoint(Point2d basePt, bool leftOrRight)
            {
              DrawCheckPoint(basePt.ToPoint3d(), leftOrRight);
            }
            var fdBasePoints = new Dictionary<int, List<Point2d>>();
            var floorDrainCbs = new Dictionary<Geometry, FloorDrainCbItem>();
            var washingMachineFloorDrainShooters = new List<Geometry>();
            for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
            {
              var fdBsPts = new List<Point2d>();
              fdBasePoints[MAX_ANGEL_TOLLERANCE] = fdBsPts;
              var tolGroupBlkLane = allNumStoreyLabels.TryGet(MAX_ANGEL_TOLLERANCE);
              if (tolGroupBlkLane == null) continue;
              var run = thwPipeLine.PipeRuns.TryGet(MAX_ANGEL_TOLLERANCE);
              if (run == null) continue;
              var TolLightRangeSingleSideMin = well_to_wall_offset[MAX_ANGEL_TOLLERANCE];
              if (TolLightRangeSingleSideMin == null) continue;
              var output = thwPipeLine.Output;
              {
                if (tolGroupBlkLane == THESAURUSREGION)
                {
                  var basePt = TolLightRangeSingleSideMin.EndPoint;
                  if (output != null)
                  {
                    DrawOutlets1(basePt, THESAURUSEXECRABLE, output);
                  }
                }
              }
              bool shouldRaiseWashingMachine()
              {
                return viewModel?.Params?.ShouldRaiseWashingMachine ?? INTRAVASCULARLY;
              }
              bool _shouldDrawRaiseWashingMachineSymbol()
              {
                return INTRAVASCULARLY;
              }
              bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
              {
                return INTRAVASCULARLY;
              }
              void handleHanging(Hanging hanging, bool isLeftOrRight)
              {
                var linesDfferencers = new List<Polygon>();
                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, int MAX_ANGEL_TOLLERANCE, int MAX_ANGLE_TOLLERANCE)
                {
                  var _tolRegroupMainYRange = basePt.ToPoint2d();
                  {
                    if (_shouldDrawRaiseWashingMachineSymbol())
                    {
                      var fixVec = new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE);
                      var _raiseDistanceToStartDefault = _tolRegroupMainYRange + new Vector2d(THESAURUSSTAMPEDE, INCONSIDERABILIS) + new Vector2d(-THESAURUSCAVERN, THESAURUSSHROUD) + fixVec;
                      fdBsPts.Add(_raiseDistanceToStartDefault);
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-THESAURUSSOMETIMES, THESAURUSSTAMPEDE), fixVec, new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFORMULATE), new Vector2d(-ELECTROMYOGRAPH, THESAURUSSTAMPEDE) };
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE));
                      drawDomePipes(TolLane);
                      DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(TolLane.Last().EndPoint, THESAURUSOBSTINACY);
                      return;
                    }
                  }
                  {
                    var _raiseDistanceToStartDefault = _tolRegroupMainYRange + new Vector2d(-THESAURUSCAVERN + (leftOrRight ? THESAURUSSTAMPEDE : THESAURUSDIFFICULTY), THESAURUSINTRENCH);
                    fdBsPts.Add(_raiseDistanceToStartDefault);
                    floorDrainCbs[new GRect(basePt, basePt.OffsetXY(leftOrRight ? -THESAURUSDOMESTIC : THESAURUSDOMESTIC, QUOTATIONWITTIG)).ToPolygon()] = new FloorDrainCbItem()
                    {
                      BasePt = basePt.ToPoint2D(),
                      Name = ACCOMMODATINGLY,
                      LeftOrRight = leftOrRight,
                    };
                    return;
                  }
                }
                void _DrawDSCurve(Vector2d vec7, Point2d _tolRegroupMainYRange, bool leftOrRight, int MAX_ANGEL_TOLLERANCE, int MAX_ANGLE_TOLLERANCE)
                {
                  if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax)))
                  {
                    hasDrawedDSCurveLabel = THESAURUSOBSTINACY;
                    var _tolConnectSecPtRange = _tolRegroupMainYRange + new Vector2d(-THESAURUSLOITER, THESAURUSSECLUSION - THESAURUSHYPNOTIC);
                    if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                    {
                      _tolConnectSecPtRange += new Vector2d(PHOTOSYNTHETICALLY, -THESAURUSINTRENCH);
                    }
                    _DrawLabel(THESAURUSPUGNACIOUS, THESAURUSSCOUNDREL, _tolConnectSecPtRange, THESAURUSOBSTINACY, QUOTATIONBASTARD);
                  }
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = vec7;
                    if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                    {
                      MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = default;
                      _tolRegroupMainYRange = _tolRegroupMainYRange.OffsetY(HYPERDISYLLABLE);
                    }
                    var _tolConnectSecPtRange = _tolRegroupMainYRange + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                    if (!_tolRegroupMainYRange.Equals(_tolConnectSecPtRange))
                    {
                      dome_lines.Add(new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                    }
                    DrawDSCurve(_tolConnectSecPtRange, leftOrRight, getDSCurveValue());
                  }
                }
                ++counterPipeButtomHeightSymbol;
                if (counterPipeButtomHeightSymbol == THESAURUSHOUSING && thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL(TolLightRangeSingleSideMax)))
                {
                  if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax)))
                  {
                    DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, TolLightRangeSingleSideMin.StartPoint.OffsetY(-THESAURUSINTRENCH - UNAPPREHENSIBLE));
                  }
                  else
                  {
                    var maxBalconyrainpipeToFloordrainDistance = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE]?.FloorDrainsCount ?? THESAURUSSTAMPEDE;
                    if (maxBalconyrainpipeToFloordrainDistance > THESAURUSSTAMPEDE)
                    {
                      if (maxBalconyrainpipeToFloordrainDistance == THESAURUSPERMUTATION && !_tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].IsSeries)
                      {
                        DrawPipeButtomHeightSymbol(THESAURUSHYPNOTIC, HEIGHT * THESAURUSRIBALD, TolLightRangeSingleSideMin.StartPoint.OffsetXY(THESAURUSJACKPOT, -THESAURUSINTRENCH));
                        var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDERELICTION), new Vector2d(-THESAURUSINHERIT, THESAURUSSTAMPEDE) };
                        var TolLane = LineCoincideTolerance.ToGLineSegments(new List<Vector2d> { new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINTRENCH) }.GetLastPoint(TolLightRangeSingleSideMin.StartPoint));
                        DrawPipeButtomHeightSymbol(TolLane.Last().EndPoint, TolLane);
                      }
                      else
                      {
                        DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, TolLightRangeSingleSideMin.StartPoint.OffsetY(-THESAURUSINTRENCH));
                      }
                    }
                    else
                    {
                      double MAX_TAG_XPOSITION = -THESAURUSPERVADE - THESAURUSQUAGMIRE;
                      if (_tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].HasLong)
                      {
                        MAX_TAG_XPOSITION += THESAURUSQUAGMIRE;
                      }
                      DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, THESAURUSSTAMPEDE, TolLightRangeSingleSideMin.EndPoint.OffsetY(HEIGHT / THESAURUSCOMMUNICATION + MAX_TAG_XPOSITION));
                    }
                  }
                }
                var w = THROMBOEMBOLISM;
                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve)
                {
                  w = THESAURUSSTAMPEDE;
                }
                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve && !hanging.IsSeries)
                {
                  var startPt = TolLightRangeSingleSideMin.StartPoint.OffsetY(-THESAURUSNOTABLE - THESAURUSHOUSING);
                  var delta = run.Is4Tune ? THESAURUSSTAMPEDE : HYPERDISYLLABLE + THESAURUSENTREPRENEUR;
                  var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSFLUTTER, THESAURUSSTAMPEDE), };
                  var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSPERVADE + delta, THESAURUSPERVADE + delta), new Vector2d(THESAURUSFLUTTER - delta, THESAURUSSTAMPEDE), };
                  var segs1 = _vecs1.ToGLineSegments(startPt);
                  var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                  DrawDomePipes(segs1);
                  DrawDomePipes(segs2);
                  _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                  _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                  if (run.Is4Tune)
                  {
                    var sidewaterbucket_y_indent = TolLightRangeSingleSideMin.StartPoint;
                    var _tolRegroupMainYRange = new List<Vector2d> { new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(sidewaterbucket_y_indent);
                    var _tolConnectSecPtRange = new List<Vector2d> { new Vector2d(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(sidewaterbucket_y_indent);
                    _DrawWrappingPipe(_tolRegroupMainYRange);
                    _DrawWrappingPipe(_tolConnectSecPtRange);
                  }
                }
                else if (run.HasLongTranslator)
                {
                  if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE && hanging.HasDoubleSCurve)
                  {
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(ANTICONVULSANTS, -ANTICONVULSANTS), new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) };
                    var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.GetLastPoint(Point2d.Origin).X;
                    var startPt = TolLightRangeSingleSideMin.EndPoint.OffsetXY(-MAX_DEVICE_TO_DEVICE, HEIGHT / THESAURUSCOMMUNICATION + ANTICONVULSANTS);
                    var TolLane = LineCoincideTolerance.ToGLineSegments(startPt);
                    var _tolRegroupMainYRange = TolLane.Last(INTROPUNITIVENESS).StartPoint;
                    drawDomePipes(TolLane);
                    _DrawDSCurve(vec7, _tolRegroupMainYRange, isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                    var MAX_CONDENSEPIPE_TO_WASHMACHINE = getBasinDN();
                    DrawNoteText(MAX_CONDENSEPIPE_TO_WASHMACHINE, _tolRegroupMainYRange - new Vector2d(-ANTICONVULSANTS, -THESAURUSASSURANCE));
                  }
                  else
                  {
                    var beShort = hanging.FloorDrainsCount == THESAURUSHOUSING && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-MISAPPREHENSIVE, MISAPPREHENSIVE), new Vector2d(THESAURUSSTAMPEDE, ACANTHORHYNCHUS), new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(beShort ? THESAURUSSTAMPEDE : -THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(-w, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
                    if (isLeftOrRight == INTRAVASCULARLY)
                    {
                      LineCoincideTolerance = LineCoincideTolerance.GetYAxisMirror();
                    }
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLightRangeSingleSideMin.Segs[QUOTATIONEDIBLE].StartPoint.OffsetY(-THESAURUSPHANTOM).OffsetY(THESAURUSCONSIGNMENT - THESAURUSQUAGMIRE);
                    if (isLeftOrRight == INTRAVASCULARLY && run.IsLongTranslatorToLeftOrRight == THESAURUSOBSTINACY)
                    {
                      var _tolRegroupMainYRange = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                      var _tolConnectSecPtRange = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(ACETYLSALICYLIC);
                      drawDomePipe(new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                      MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tolConnectSecPtRange;
                    }
                    if (isLeftOrRight == THESAURUSOBSTINACY && run.IsLongTranslatorToLeftOrRight == INTRAVASCULARLY)
                    {
                      var _tolRegroupMainYRange = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN;
                      var _tolConnectSecPtRange = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-ACETYLSALICYLIC);
                      drawDomePipe(new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                      MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tolConnectSecPtRange;
                    }
                    var isFDHigher = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].FlFixType == FlFixType.Higher && hanging.FloorDrainsCount > THESAURUSSTAMPEDE && run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight;
                    if (isFDHigher)
                    {
                      MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-PREREGISTRATION);
                    }
                    Action maxDeviceplatformArea;
                    var TolLane = LineCoincideTolerance.ToGLineSegments(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
                    {
                      var _segs = TolLane.ToList();
                      if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                      {
                        if (hanging.IsSeries)
                        {
                          _segs.RemoveAt(THESAURUSCOMMUNICATION);
                        }
                      }
                      else if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                      {
                        _segs = TolLane.Take(THESAURUSCOMMUNICATION).ToList();
                      }
                      else if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
                      {
                        _segs = TolLane.Take(QUOTATIONEDIBLE).ToList();
                      }
                      if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSPERMUTATION); }
                      maxDeviceplatformArea = () => { drawDomePipes(_segs); };
                    }
                    if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                    {
                      var _raiseDistanceToStartDefault = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                      _DrawFloorDrain(_raiseDistanceToStartDefault.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                      Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                      DrawNoteText(_tolReturnValue0Approx, _raiseDistanceToStartDefault + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                    }
                    if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                    {
                      var _tolConnectSecPtRange = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                      var _tolRegroupMainYRange = TolLane.Last(THESAURUSHOUSING).EndPoint;
                      _DrawFloorDrain(_tolRegroupMainYRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                      _DrawFloorDrain(_tolConnectSecPtRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                      Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                      DrawNoteText(_tolReturnValue0Approx, _tolRegroupMainYRange + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                      DrawNoteText(TolGroupEvcaEmg, _tolConnectSecPtRange + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                      if (!hanging.IsSeries)
                      {
                        drawDomePipes(new GLineSegment[] { TolLane.Last(THESAURUSPERMUTATION) });
                      }
                      {
                        var MAX_TAG_XPOSITION = QUOTATIONTRANSFERABLE;
                        if (isFDHigher)
                        {
                          MAX_TAG_XPOSITION = PREREGISTRATION;
                        }
                        var _segs = new List<Vector2d> { new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSJOURNALIST, THESAURUSSTAMPEDE), new Vector2d(HYPERDISYLLABLE, THESAURUSSTAMPEDE), new Vector2d(CONSTITUTIVENESS, THESAURUSSTAMPEDE), new Vector2d(THESAURUSALLEGIANCE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -ALSOSESQUIALTERAL + MAX_TAG_XPOSITION), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(_tolRegroupMainYRange);
                        _segs.RemoveAt(THESAURUSPERMUTATION);
                        var default_fire_valve_length = new List<Vector2d> { new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) }.ToGLineSegments(_tolRegroupMainYRange)[THESAURUSHOUSING];
                        maxDeviceplatformArea = () =>
                        {
                          drawDomePipes(_segs);
                          drawDomePipes(new GLineSegment[] { default_fire_valve_length });
                        };
                      }
                    }
                    {
                      var _raiseDistanceToStartDefault = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                      var default_fire_valve_length = new List<Vector2d> { new Vector2d(THESAURUSCUSTOMARY, -APOLLINARIANISM), new Vector2d(THESAURUSSTAMPEDE, -TRICHINELLIASIS) }.ToGLineSegments(_raiseDistanceToStartDefault)[THESAURUSHOUSING];
                      var pt1 = TolLane.First().StartPoint;
                      var _tol_lane = pt1.OffsetY(TRICHINELLIASIS);
                      var MAX_TAG_XPOSITION = QUOTATIONTRANSFERABLE;
                      if (isFDHigher)
                      {
                        MAX_TAG_XPOSITION = PREREGISTRATION;
                      }
                      pt1 = pt1.OffsetY(MAX_TAG_XPOSITION);
                      _tol_lane = _tol_lane.OffsetY(MAX_TAG_XPOSITION);
                      var dim = DrawDimLabel(pt1, _tol_lane, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                    }
                    if (hanging.HasSCurve)
                    {
                      var _tolRegroupMainYRange = TolLane.Last(INTROPUNITIVENESS).StartPoint;
                      _DrawSCurve(vec7, _tolRegroupMainYRange, isLeftOrRight);
                    }
                    if (hanging.HasDoubleSCurve)
                    {
                      var _tolRegroupMainYRange = TolLane.Last(INTROPUNITIVENESS).StartPoint;
                      _DrawDSCurve(vec7, _tolRegroupMainYRange, isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                    }
                    maxDeviceplatformArea?.Invoke();
                  }
                }
                else
                {
                  if (_tolReturnValueRange.IsFL0)
                  {
                    DrawFloorDrain((TolLightRangeSingleSideMin.StartPoint + new Vector2d(-THESAURUSSATIATE, -THESAURUSINTRENCH)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINFLEXIBLE), new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSCAPITALISM, THESAURUSSTAMPEDE) };
                    var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint).Skip(THESAURUSHOUSING).ToList();
                    drawDomePipes(TolLane);
                  }
                  else
                  {
                    var beShort = hanging.FloorDrainsCount == THESAURUSHOUSING && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(beShort ? THESAURUSSTAMPEDE : -THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(-w, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
                    if (isLeftOrRight == INTRAVASCULARLY)
                    {
                      LineCoincideTolerance = LineCoincideTolerance.GetYAxisMirror();
                    }
                    var startPt = TolLightRangeSingleSideMin.StartPoint.OffsetY(-THESAURUSNOTABLE - THESAURUSHOUSING);
                    if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE && hanging.HasDoubleSCurve)
                    {
                      startPt = TolLightRangeSingleSideMin.EndPoint.OffsetY(-THESAURUSDISCOLOUR + HEIGHT / THESAURUSCOMMUNICATION);
                    }
                    var SidewaterbucketXIndent = INTRAVASCULARLY;
                    if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve)
                    {
                      if (hanging.IsSeries)
                      {
                        var TolLane = LineCoincideTolerance.ToGLineSegments(startPt);
                        var _segs = TolLane.ToList();
                        linesDfferencers.Add(GRect.Create(_segs[INTROPUNITIVENESS].EndPoint, THESAURUSENTREPRENEUR).ToPolygon());
                        var _tolConnectSecPtRange = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                        var _tolRegroupMainYRange = TolLane.Last(THESAURUSHOUSING).EndPoint;
                        _DrawFloorDrain(_tolRegroupMainYRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                        _DrawFloorDrain(_tolConnectSecPtRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                        Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                        DrawNoteText(_tolReturnValue0Approx, _tolRegroupMainYRange + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                        DrawNoteText(TolGroupEvcaEmg, _tolConnectSecPtRange + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                        TolLane = new List<Vector2d> { new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSJOURNALIST, THESAURUSSTAMPEDE), new Vector2d(HYPERDISYLLABLE, THESAURUSSTAMPEDE), new Vector2d(QUINQUARTICULAR, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(_tolRegroupMainYRange);
                        var _raiseDistanceToStartDefault = TolLane[QUOTATIONEDIBLE].StartPoint;
                        TolLane.RemoveAt(THESAURUSPERMUTATION);
                        dome_lines.AddRange(TolLane);
                        dome_lines.AddRange(new List<Vector2d> { new Vector2d(THESAURUSINVADE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSALLEGIANCE, -THESAURUSPERVADE) }.ToGLineSegments(_raiseDistanceToStartDefault));
                      }
                      else
                      {
                        var delta = HYPERDISYLLABLE;
                        var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSFLUTTER, THESAURUSSTAMPEDE), };
                        var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSPERVADE + delta, THESAURUSPERVADE + delta), new Vector2d(THESAURUSFLUTTER - delta, THESAURUSSTAMPEDE), };
                        var segs1 = _vecs1.ToGLineSegments(startPt);
                        var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                        dome_lines.AddRange(segs1);
                        dome_lines.AddRange(segs2);
                        _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                        _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                      }
                      SidewaterbucketXIndent = THESAURUSOBSTINACY;
                    }
                    Action maxDeviceplatformArea = null;
                    if (!SidewaterbucketXIndent)
                    {
                      if (_tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].FlCaseEnum != FlCaseEnum.Case1)
                      {
                        var TolLane = LineCoincideTolerance.ToGLineSegments(startPt);
                        var _segs = TolLane.ToList();
                        {
                          if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                          {
                            if (hanging.IsSeries)
                            {
                              _segs.RemoveAt(INTROPUNITIVENESS);
                            }
                          }
                          if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                          {
                            _segs.RemoveAt(QUOTATIONEDIBLE);
                            _segs.RemoveAt(INTROPUNITIVENESS);
                          }
                          if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
                          {
                            _segs = _segs.Take(THESAURUSPERMUTATION).ToList();
                          }
                          if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSPERMUTATION); }
                        }
                        if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                        {
                          var _raiseDistanceToStartDefault = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                          _DrawFloorDrain(_raiseDistanceToStartDefault.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                          Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                          DrawNoteText(_tolReturnValue0Approx, _raiseDistanceToStartDefault + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                        }
                        if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                        {
                          var _tolConnectSecPtRange = TolLane.Last(INTROPUNITIVENESS).EndPoint;
                          var _tolRegroupMainYRange = TolLane.Last(THESAURUSHOUSING).EndPoint;
                          _DrawFloorDrain(_tolRegroupMainYRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                          _DrawFloorDrain(_tolConnectSecPtRange.ToPoint3d(), isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                          Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                          DrawNoteText(_tolReturnValue0Approx, _tolRegroupMainYRange + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                          DrawNoteText(TolGroupEvcaEmg, _tolConnectSecPtRange + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                        }
                        maxDeviceplatformArea = () => drawDomePipes(_segs);
                      }
                    }
                    {
                      var TolLane = LineCoincideTolerance.ToGLineSegments(startPt);
                      if (hanging.HasSCurve)
                      {
                        var _tolRegroupMainYRange = TolLane.Last(INTROPUNITIVENESS).StartPoint;
                        _DrawSCurve(vec7, _tolRegroupMainYRange, isLeftOrRight);
                      }
                      if (hanging.HasDoubleSCurve)
                      {
                        var _tolRegroupMainYRange = TolLane.Last(INTROPUNITIVENESS).StartPoint;
                        if (_tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].FlCaseEnum == FlCaseEnum.Case1)
                        {
                          var _tolConnectSecPtRange = _tolRegroupMainYRange + vec7;
                          var segs1 = new List<Vector2d> { new Vector2d(-INCONSIDERABILIS + THESAURUSBISEXUAL + THESAURUSQUAGMIRE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE - THESAURUSSPIRIT - THESAURUSQUAGMIRE), new Vector2d(MISAPPREHENSIVE, -MISAPPREHENSIVE) }.ToGLineSegments(_tolConnectSecPtRange);
                          drawDomePipes(segs1);
                          {
                            Vector2d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = default;
                            var b = isLeftOrRight;
                            if (b && getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                            {
                              b = INTRAVASCULARLY;
                              MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(-THESAURUSCAVERN, -QUOTATIONWITTIG);
                            }
                            _DrawDSCurve(default(Vector2d), _tolConnectSecPtRange + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, b, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                          }
                          var _tolConnectSecPrimAddValue = segs1.Last().EndPoint;
                          var p4 = _tolConnectSecPrimAddValue.OffsetY(THESAURUSCANDIDATE);
                          DrawDimLabel(_tolConnectSecPrimAddValue, p4, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                          var MAX_CONDENSEPIPE_TO_WASHMACHINE = getBasinDN();
                          Dr.DrawDN_2(segs1.Last().StartPoint + new Vector2d(ALUMINOSILICATES + THESAURUSEXHILARATION - HYPERDISYLLABLE - POLIOENCEPHALITIS, -MISAPPREHENSIVE), THESAURUSSTRIPED, MAX_CONDENSEPIPE_TO_WASHMACHINE);
                        }
                        else
                        {
                          var MAX_TAG_XPOSITION = THESAURUSADJUST + THESAURUSHYPNOTIC;
                          _DrawDSCurve(vec7, _tolRegroupMainYRange, isLeftOrRight, MAX_ANGEL_TOLLERANCE, MAX_ANGLE_TOLLERANCE);
                          if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                          {
                            var segs1 = new List<Vector2d> { new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(_tolRegroupMainYRange.OffsetY(HYPERDISYLLABLE));
                            maxDeviceplatformArea = () => { drawDomePipes(segs1); };
                          }
                          var MAX_CONDENSEPIPE_TO_WASHMACHINE = getBasinDN();
                          DrawNoteText(MAX_CONDENSEPIPE_TO_WASHMACHINE, _tolRegroupMainYRange.OffsetY(MAX_TAG_XPOSITION));
                        }
                      }
                    }
                    maxDeviceplatformArea?.Invoke();
                  }
                }
                if (linesDfferencers.Count > THESAURUSSTAMPEDE)
                {
                  var killer = GeoFac.CreateGeometryEx(linesDfferencers);
                  dome_lines = GeoFac.GetLines(GeoFac.CreateGeometry(dome_lines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString())).Difference(killer)).ToList();
                  linesDfferencers.Clear();
                }
              }
              void handleBranchInfo(ThwPipeRun run, PipeRunLocationInfo TolLightRangeSingleSideMin)
              {
                var bi = run.BranchInfo;
                if (bi.FirstLeftRun)
                {
                  var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, THESAURUSPERMUTATION));
                  var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                  var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(THESAURUSPERMUTATION, INTROPUNITIVENESS));
                  TolLightRangeSingleSideMin.DisplaySegs = new List<GLineSegment>() { new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange), new GLineSegment(_tolConnectSecPtRange, p4) };
                }
                if (bi.FirstRightRun)
                {
                  var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                  var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                  var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
                  TolLightRangeSingleSideMin.DisplaySegs = new List<GLineSegment>() { new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange), new GLineSegment(_tolConnectSecPtRange, p4) };
                }
                if (bi.LastLeftRun)
                {
                }
                if (bi.LastRightRun)
                {
                }
                if (bi.MiddleLeftRun)
                {
                }
                if (bi.MiddleRightRun)
                {
                }
                if (bi.BlueToLeftFirst)
                {
                  var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                  var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                  var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
                  TolLightRangeSingleSideMin.DisplaySegs = new List<GLineSegment>() { new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange), new GLineSegment(_tolConnectSecPtRange, p4) };
                }
                if (bi.BlueToRightFirst)
                {
                  var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                  var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                  var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
                  TolLightRangeSingleSideMin.DisplaySegs = new List<GLineSegment>() { new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange), new GLineSegment(_tolConnectSecPtRange, p4) };
                }
                if (bi.BlueToLeftLast)
                {
                  if (run.HasLongTranslator)
                  {
                    if (run.IsLongTranslatorToLeftOrRight)
                    {
                      var _dy = THESAURUSHYPNOTIC;
                      var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING - _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - MAX_DEVICE_TO_BALCONY + _dy + THESAURUSDOMESTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      var TolLane = TolLightRangeSingleSideMin.DisplaySegs = vs1.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                      var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(vs2.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint).Skip(THESAURUSHOUSING).ToList());
                    }
                    else
                    {
                      var _dy = THESAURUSHYPNOTIC;
                      var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - MAX_DEVICE_TO_BALCONY - _dy + THESAURUSDOMESTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      var TolLane = TolLightRangeSingleSideMin.DisplaySegs = vs1.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                      var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(vs2.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint).Skip(THESAURUSHOUSING).ToList());
                    }
                  }
                  else if (!run.HasLongTranslator)
                  {
                    var vs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMERITORIOUS), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                    TolLightRangeSingleSideMin.DisplaySegs = vs.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                  }
                }
                if (bi.BlueToRightLast)
                {
                  if (!run.HasLongTranslator && !run.HasShortTranslator)
                  {
                    var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                    var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                    var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                    var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                    var _tol_light_range_max = _tolRegroupMainYRange.OffsetY(HEIGHT);
                    TolLightRangeSingleSideMin.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, _tolConnectSecPtRange), new GLineSegment(_tolConnectSecPtRange, _tol_light_range_max) };
                  }
                }
                if (bi.BlueToLeftMiddle)
                {
                  if (!run.HasLongTranslator && !run.HasShortTranslator)
                  {
                    var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                    var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                    var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                    var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                    var TolLane = TolLightRangeSingleSideMin.Segs.ToList();
                    TolLane.Add(new GLineSegment(_tolConnectSecPtRange, p4));
                    TolLightRangeSingleSideMin.DisplaySegs = TolLane;
                  }
                  else if (run.HasLongTranslator)
                  {
                    if (run.IsLongTranslatorToLeftOrRight)
                    {
                      var _dy = THESAURUSHYPNOTIC;
                      var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING - _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - MAX_DEVICE_TO_BALCONY + _dy) };
                      var TolLane = TolLightRangeSingleSideMin.DisplaySegs = vs1.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                      var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(vs2.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint).Skip(THESAURUSHOUSING).ToList());
                    }
                    else
                    {
                      var _dy = THESAURUSHYPNOTIC;
                      var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - MAX_DEVICE_TO_BALCONY - _dy) };
                      var TolLane = TolLightRangeSingleSideMin.DisplaySegs = vs1.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                      var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(vs2.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint).Skip(THESAURUSHOUSING).ToList());
                    }
                  }
                }
                if (bi.BlueToRightMiddle)
                {
                  var _tolRegroupMainYRange = TolLightRangeSingleSideMin.EndPoint;
                  var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                  var _tolConnectSecPrimAddValue = TolLightRangeSingleSideMin.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                  var p4 = _tolConnectSecPrimAddValue.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                  var TolLane = TolLightRangeSingleSideMin.Segs.ToList();
                  TolLane.Add(new GLineSegment(_tolConnectSecPtRange, p4));
                  TolLightRangeSingleSideMin.DisplaySegs = TolLane;
                }
                {
                  var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -INCONSIDERABILIS), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSCOMATOSE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -OTHERWORLDLINESS), new Vector2d(-MISAPPREHENSIVE, -MISAPPREHENSIVE) };
                  if (bi.HasLongTranslatorToLeft)
                  {
                    var vs = LineCoincideTolerance;
                    TolLightRangeSingleSideMin.DisplaySegs = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                    if (!bi.IsLast)
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = vs.Take(vs.Count - THESAURUSHOUSING).GetLastPoint(TolLightRangeSingleSideMin.StartPoint);
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSRATION) }.ToGLineSegments(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN));
                    }
                  }
                  if (bi.HasLongTranslatorToRight)
                  {
                    var vs = LineCoincideTolerance.GetYAxisMirror();
                    TolLightRangeSingleSideMin.DisplaySegs = vs.ToGLineSegments(TolLightRangeSingleSideMin.StartPoint);
                    if (!bi.IsLast)
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = vs.Take(vs.Count - THESAURUSHOUSING).GetLastPoint(TolLightRangeSingleSideMin.StartPoint);
                      TolLightRangeSingleSideMin.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSRATION) }.ToGLineSegments(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN));
                    }
                  }
                }
              }
              if (run.LeftHanging != null)
              {
                run.LeftHanging.IsSeries = _tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING)?.IsSeries ?? THESAURUSOBSTINACY;
                handleHanging(run.LeftHanging, THESAURUSOBSTINACY);
              }
              if (run.BranchInfo != null)
              {
                handleBranchInfo(run, TolLightRangeSingleSideMin);
              }
              if (run.ShowShortTranslatorLabel)
              {
                var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSNEGATE, THESAURUSNEGATE), new Vector2d(-QUOTATIONRHEUMATOID, QUOTATIONRHEUMATOID), new Vector2d(-THESAURUSSENSITIVE, THESAURUSSTAMPEDE) };
                var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.EndPoint).Skip(THESAURUSHOUSING).ToList();
                DrawDraiNoteLines(TolLane);
                DrawDraiNoteLines(TolLane);
                var repeated_point_distance = THESAURUSECHELON;
                var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane.Last().EndPoint;
                DrawNoteText(repeated_point_distance, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
              }
              if (run.HasCheckPoint)
              {
                var h = THESAURUSDOMESTIC;
                Point2d pt1, _tol_lane;
                {
                  pt1 = TolLightRangeSingleSideMin.EndPoint.OffsetY(h);
                  _tol_lane = TolLightRangeSingleSideMin.EndPoint;
                  if (run.HasLongTranslator)
                  {
                    pt1 = TolLightRangeSingleSideMin.EndPoint.OffsetY(DETERMINATENESS + QUINQUARTICULAR);
                  }
                }
                _DrawCheckPoint(pt1.OffsetY(PHYSIOLOGICALLY - CONSCRIPTIONIST + THESAURUSDOMESTIC), THESAURUSOBSTINACY);
                if (tolGroupBlkLane == THESAURUSREGION)
                {
                  var MAX_DEVICE_TO_DEVICE = -POLYOXYMETHYLENE;
                  if (_tolReturnValueRange.HasBasinInKitchenAt1F)
                  {
                    MAX_DEVICE_TO_DEVICE = POLYOXYMETHYLENE;
                  }
                  {
                    var dim = DrawDimLabel(pt1, _tol_lane, new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), _tolReturnValueRange.PipeType == PipeType.PL ? CONSECUTIVENESS : THESAURUSDUBIETY, QUOTATIONBENJAMIN);
                    if (MAX_DEVICE_TO_DEVICE < THESAURUSSTAMPEDE)
                    {
                      dim.TextPosition = (pt1 + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE) + new Vector2d(-THESAURUSBOMBARD, -MISAPPREHENSIVE) + new Vector2d(THESAURUSSTAMPEDE, HYPERDISYLLABLE)).ToPoint3d();
                    }
                  }
                  if (_tolReturnValueRange.HasTl && SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE] == _tolReturnValueRange.MinTl + THESAURUSASPIRATION)
                  {
                    var RADIAN_TOLERANCE = THESAURUSINCOMING / HEIGHT;
                    pt1 = TolLightRangeSingleSideMin.EndPoint;
                    _tol_lane = pt1.OffsetY(INTERNATIONALLY * RADIAN_TOLERANCE);
                    if (run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight)
                    {
                      _tol_lane = pt1.OffsetY(THESAURUSEVIDENT);
                    }
                    var dim = DrawDimLabel(pt1, _tol_lane, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN);
                  }
                }
              }
              if (run.HasDownBoardLine)
              {
                _DrawHorizontalLineOnPipeRun(TolLightRangeSingleSideMin.BasePoint.ToPoint3d());
              }
              if (run.HasCleaningPort)
              {
                if (run.HasLongTranslator)
                {
                  var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-MISAPPREHENSIVE, MISAPPREHENSIVE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSINDOMITABLE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSOFFEND) };
                  if (run.IsLongTranslatorToLeftOrRight == INTRAVASCULARLY)
                  {
                    LineCoincideTolerance = LineCoincideTolerance.GetYAxisMirror();
                  }
                  if (run.HasShortTranslator)
                  {
                    var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.Segs.Last(THESAURUSPERMUTATION).StartPoint.OffsetY(-THESAURUSHYPNOTIC));
                    drawDomePipes(TolLane);
                    _DrawCleaningPort(TolLane.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSPERMUTATION);
                  }
                  else
                  {
                    var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.Segs.Last().StartPoint.OffsetY(-THESAURUSHYPNOTIC));
                    drawDomePipes(TolLane);
                    _DrawCleaningPort(TolLane.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSPERMUTATION);
                    if (run.IsLongTranslatorToLeftOrRight)
                    {
                      var pt1 = TolLane.First().StartPoint;
                      var _tol_lane = pt1.OffsetY(THESAURUSDISCERNIBLE);
                      var dim = DrawDimLabel(pt1, _tol_lane, new Vector2d(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                      dim.TextPosition = (pt1 + new Vector2d(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE) + new Vector2d(-PRESBYTERIANIZE, THESAURUSDEGREE) + new Vector2d(THESAURUSSENTIMENTALITY, MISAPPREHENSIVE - ULTRASONICATION)).ToPoint3d();
                    }
                  }
                }
                else
                {
                  _DrawCleaningPort(TolLightRangeSingleSideMin.StartPoint.OffsetY(-THESAURUSHYPNOTIC).ToPoint3d(), THESAURUSOBSTINACY, THESAURUSPERMUTATION);
                }
              }
              if (run.HasShortTranslator)
              {
                DrawShortTranslatorLabel(TolLightRangeSingleSideMin.Segs.First().Center, run.IsShortTranslatorToLeftOrRight);
              }
            }
            var showAllFloorDrainLabel = INTRAVASCULARLY;
            for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
            {
              if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax))) continue;
              var (SidewaterbucketXIndent, REPEATED_POINT_DISTANCE) = _tolReturnValueRange.Items.TryGetValue(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
              if (!SidewaterbucketXIndent) continue;
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in fdBasePoints[MAX_ANGEL_TOLLERANCE].OrderBy(_raiseDistanceToStartDefault => _raiseDistanceToStartDefault.X))
              {
              }
            }
            for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
            {
              if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax))) continue;
              var hanging = _tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
              if (hanging == null) continue;
              var fdsCount = hanging.FloorDrainsCount;
              if (fdsCount == THESAURUSSTAMPEDE) continue;
              var wfdsCount = hanging.WashingMachineFloorDrainsCount;
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in fdBasePoints[MAX_ANGEL_TOLLERANCE].OrderBy(_raiseDistanceToStartDefault => _raiseDistanceToStartDefault.X))
              {
                if (fdsCount > THESAURUSSTAMPEDE)
                {
                  if (wfdsCount > THESAURUSSTAMPEDE)
                  {
                    wfdsCount--;
                    washingMachineFloorDrainShooters.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint());
                  }
                  fdsCount--;
                }
              }
            }
            for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
            {
              if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax))) continue;
              var hanging = _tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
              if (hanging == null) continue;
              var fdsCount = hanging.FloorDrainsCount;
              if (fdsCount == THESAURUSSTAMPEDE) continue;
              var wfdsCount = hanging.WashingMachineFloorDrainsCount;
              var h = QUOTATIONTRANSFERABLE;
              var ok_texts = new HashSet<string>();
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in fdBasePoints[MAX_ANGEL_TOLLERANCE].OrderBy(_raiseDistanceToStartDefault => _raiseDistanceToStartDefault.X))
              {
                if (fdsCount > THESAURUSSTAMPEDE)
                {
                  if (wfdsCount > THESAURUSSTAMPEDE)
                  {
                    wfdsCount--;
                    h += INCONSIDERABILIS;
                    if (hanging.RoomName != null)
                    {
                      var repeated_point_distance = $"接{hanging.RoomName}洗衣机地漏";
                      if (!ok_texts.Contains(repeated_point_distance))
                      {
                        _DrawLabel(repeated_point_distance, $"{getWashingMachineFloorDrainDN()}，余同", MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, THESAURUSOBSTINACY, h);
                        ok_texts.Add(repeated_point_distance);
                      }
                    }
                  }
                  else
                  {
                    h += INCONSIDERABILIS;
                    if (hanging.RoomName != null)
                    {
                      _DrawLabel($"接{hanging.RoomName}地漏", $"{getWashingMachineFloorDrainDN()}，余同", MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, THESAURUSOBSTINACY, h);
                    }
                  }
                  fdsCount--;
                }
              }
              break;
            }
            for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
            {
              if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax))) continue;
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in fdBasePoints[MAX_ANGEL_TOLLERANCE].OrderBy(_raiseDistanceToStartDefault => _raiseDistanceToStartDefault.X))
              {
                if (showAllFloorDrainLabel)
                {
                }
                else
                {
                }
              }
            }
            {
              var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(washingMachineFloorDrainShooters);
              foreach (var max_rainpipe_to_balconyfloordrain in floorDrainCbs)
              {
                var o = max_rainpipe_to_balconyfloordrain.Value;
                if (maxDeviceplatformArea(max_rainpipe_to_balconyfloordrain.Key).Any())
                {
                  o.Name = THESAURUSSYNTHETIC;
                }
                DrawFloorDrain(o.BasePt.ToPoint3d(), o.LeftOrRight, o.Name);
              }
            }
          }
          PipeRunLocationInfo[] getPipeRunLocationInfos()
          {
            var _tol_light_range_single_side_max = new PipeRunLocationInfo[SIDEWATERBUCKET_Y_INDENT.Count];
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < SIDEWATERBUCKET_Y_INDENT.Count; MAX_ANGEL_TOLLERANCE++)
            {
              _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE] = new PipeRunLocationInfo() { Visible = THESAURUSOBSTINACY, Storey = SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE], };
            }
            {
              var tdx = UNDENOMINATIONAL;
              for (int MAX_ANGEL_TOLLERANCE = MAX_TAG_LENGTH; MAX_ANGEL_TOLLERANCE >= TEXT_INDENT; MAX_ANGEL_TOLLERANCE--)
              {
                var _tolReturnValueMax = SIDEWATERBUCKET_X_INDENT.OffsetY(HEIGHT * MAX_ANGEL_TOLLERANCE);
                var basePt = _tolReturnValueMax.OffsetX(OFFSET_X + (MAX_ANGLE_TOLLERANCE + THESAURUSHOUSING) * SPAN_X) + new Vector2d(tdx, THESAURUSSTAMPEDE);
                var run = thwPipeLine.PipeRuns.TryGet(MAX_ANGEL_TOLLERANCE);
                MAX_TAG_XPOSITION = THESAURUSSTAMPEDE;
                PipeRunLocationInfo drawNormal()
                {
                  {
                    var LineCoincideTolerance = tolConnectSecPrimAddValue;
                    var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                    var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                    tdx += MAX_DEVICE_TO_DEVICE;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].HangingEndPoint = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                  }
                  {
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                  }
                  {
                    var TolLightRangeSingleSideMin = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE];
                    var RADIAN_TOLERANCE = HEIGHT / THESAURUSINCOMING;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMOTIONLESS * RADIAN_TOLERANCE), new Vector2d(-THESAURUSHYPNOTIC, -INTERNATIONALLY * RADIAN_TOLERANCE) };
                    var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.EndPoint.OffsetY(HEIGHT)).Skip(THESAURUSHOUSING).ToList();
                    TolLightRangeSingleSideMin.RightSegsLast = TolLane;
                  }
                  {
                    var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs.First().StartPoint.OffsetX(THESAURUSHYPNOTIC);
                    var TolLane = new List<GLineSegment>() { new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE))) };
                    _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                  }
                  return _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE];
                }
                if (MAX_ANGEL_TOLLERANCE == MAX_TAG_LENGTH)
                {
                  drawNormal().Visible = INTRAVASCULARLY;
                  continue;
                }
                if (run == null)
                {
                  drawNormal().Visible = INTRAVASCULARLY;
                  continue;
                }
                _dy = run.DrawLongHLineHigher ? CONSCRIPTIONIST : THESAURUSSTAMPEDE;
                if (run.HasLongTranslator && run.HasShortTranslator)
                {
                  if (run.IsLongTranslatorToLeftOrRight)
                  {
                    {
                      var LineCoincideTolerance = _tolGroupEmgLightEvac;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      TolLane.RemoveAt(THESAURUSCOMMUNICATION);
                      TolLane.RemoveAt(QUOTATIONEDIBLE);
                      TolLane.Add(new GLineSegment(TolLane[INTROPUNITIVENESS].EndPoint, TolLane[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC)));
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSPERMUTATION].EndPoint, new Point2d(TolLane[THESAURUSCOMMUNICATION].EndPoint.X, TolLane[THESAURUSPERMUTATION].EndPoint.Y)));
                      TolLane.RemoveAt(THESAURUSCOMMUNICATION);
                      TolLane.RemoveAt(INTROPUNITIVENESS);
                      TolLane = new List<GLineSegment>() { TolLane[INTROPUNITIVENESS], new GLineSegment(TolLane[INTROPUNITIVENESS].StartPoint, TolLane[THESAURUSSTAMPEDE].StartPoint) };
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = TolLane;
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane[QUOTATIONEDIBLE].EndPoint;
                      TolLane = new List<GLineSegment>() { new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].EndPoint, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  else
                  {
                    {
                      var LineCoincideTolerance = vecs6;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      TolLane.RemoveAt(THESAURUSCOMMUNICATION);
                      TolLane.RemoveAt(QUOTATIONEDIBLE);
                      TolLane.Add(new GLineSegment(TolLane[INTROPUNITIVENESS].EndPoint, TolLane[QUOTATIONEDIBLE].StartPoint));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = TolLane;
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane[QUOTATIONEDIBLE].EndPoint;
                      TolLane = new List<GLineSegment>() { new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].EndPoint, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].HangingEndPoint = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs[QUOTATIONEDIBLE].EndPoint;
                }
                else if (run.HasLongTranslator)
                {
                  switch (_tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE].FlFixType)
                  {
                    case FlFixType.NoFix:
                      break;
                    case FlFixType.MiddleHigher:
                      MAX_TAG_XPOSITION = THESAURUSFEELER / QUOTATIONBASTARD * HEIGHT;
                      break;
                    case FlFixType.Lower:
                      MAX_TAG_XPOSITION = -THESAURUSATTENDANCE / THESAURUSPERMUTATION / QUOTATIONBASTARD * HEIGHT;
                      break;
                    case FlFixType.Higher:
                      MAX_TAG_XPOSITION = THESAURUSATTENDANCE / THESAURUSPERMUTATION / QUOTATIONBASTARD * HEIGHT + THESAURUSINACCURACY / QUOTATIONBASTARD * HEIGHT;
                      break;
                    default:
                      break;
                  }
                  if (run.IsLongTranslatorToLeftOrRight)
                  {
                    {
                      var LineCoincideTolerance = _tolGroupBlkLane;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      TolLane = TolLane.Take(QUOTATIONEDIBLE).YieldAfter(TolLane.Last()).YieldAfter(new GLineSegment(TolLane[INTROPUNITIVENESS].EndPoint, TolLane[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC))).ToList();
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSPERMUTATION].EndPoint, new Point2d(TolLane[THESAURUSCOMMUNICATION].EndPoint.X, TolLane[THESAURUSPERMUTATION].EndPoint.Y)));
                      TolLane.RemoveAt(THESAURUSCOMMUNICATION);
                      TolLane.RemoveAt(INTROPUNITIVENESS);
                      TolLane = new List<GLineSegment>() { TolLane[INTROPUNITIVENESS], new GLineSegment(TolLane[INTROPUNITIVENESS].StartPoint, TolLane[THESAURUSSTAMPEDE].StartPoint) };
                      var h = HEIGHT - QUOTATIONBASTARD;
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSEXCHANGE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-UNPERISHABLENESS, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDENTIST - HYPERDISYLLABLE - h), new Vector2d(-MISAPPREHENSIVE, -NOVAEHOLLANDIAE) };
                      TolLane = LineCoincideTolerance.ToGLineSegments(_tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint.OffsetXY(THESAURUSHYPNOTIC, HEIGHT));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = TolLane;
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane[QUOTATIONEDIBLE].EndPoint;
                      TolLane = new List<GLineSegment>() { new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].EndPoint, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  else
                  {
                    {
                      var LineCoincideTolerance = vecs4;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = TolLane.Take(QUOTATIONEDIBLE).YieldAfter(new GLineSegment(TolLane[INTROPUNITIVENESS].EndPoint, TolLane[THESAURUSCOMMUNICATION].StartPoint)).YieldAfter(TolLane[THESAURUSCOMMUNICATION]).ToList();
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane[QUOTATIONEDIBLE].EndPoint;
                      TolLane = new List<GLineSegment>() { new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].EndPoint, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].HangingEndPoint = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint;
                }
                else if (run.HasShortTranslator)
                {
                  if (run.IsShortTranslatorToLeftOrRight)
                  {
                    {
                      var LineCoincideTolerance = _tolGroupBlkLaneHead;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = new List<GLineSegment>() { new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, TolLane[THESAURUSPERMUTATION].StartPoint), TolLane[THESAURUSPERMUTATION] };
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var DEFAULT_FIRE_VALVE_WIDTH = new GRect(TolLane[THESAURUSPERMUTATION].StartPoint, TolLane[THESAURUSPERMUTATION].EndPoint);
                      TolLane[THESAURUSPERMUTATION] = new GLineSegment(DEFAULT_FIRE_VALVE_WIDTH.LeftTop, DEFAULT_FIRE_VALVE_WIDTH.RightButtom);
                      TolLane.RemoveAt(THESAURUSSTAMPEDE);
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, DEFAULT_FIRE_VALVE_WIDTH.RightButtom));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  else
                  {
                    {
                      var LineCoincideTolerance = vecs5;
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                      var MAX_DEVICE_TO_DEVICE = LineCoincideTolerance.Sum(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN => MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.X);
                      tdx += MAX_DEVICE_TO_DEVICE;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].BasePoint = basePt;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint = basePt + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE);
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Vector2ds = LineCoincideTolerance;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs = TolLane;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle = TolLane.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].PlBasePt = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                    }
                    {
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.First().StartPoint;
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.Add(new GLineSegment(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsLast = new List<GLineSegment>() { new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, TolLane[THESAURUSPERMUTATION].StartPoint), TolLane[THESAURUSPERMUTATION] };
                    }
                    {
                      var TolLane = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsMiddle.ToList();
                      var DEFAULT_FIRE_VALVE_WIDTH = new GRect(TolLane[THESAURUSPERMUTATION].StartPoint, TolLane[THESAURUSPERMUTATION].EndPoint);
                      TolLane[THESAURUSPERMUTATION] = new GLineSegment(DEFAULT_FIRE_VALVE_WIDTH.LeftTop, DEFAULT_FIRE_VALVE_WIDTH.RightButtom);
                      TolLane.RemoveAt(THESAURUSSTAMPEDE);
                      TolLane.Add(new GLineSegment(TolLane[THESAURUSSTAMPEDE].StartPoint, DEFAULT_FIRE_VALVE_WIDTH.RightButtom));
                      _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].RightSegsFirst = TolLane;
                    }
                  }
                  _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].HangingEndPoint = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE].Segs[THESAURUSSTAMPEDE].EndPoint;
                }
                else
                {
                  drawNormal();
                }
              }
            }
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
            {
              var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
              if (TolLightRangeSingleSideMin != null)
              {
                TolLightRangeSingleSideMin.StartPoint = TolLightRangeSingleSideMin.BasePoint.OffsetY(HEIGHT);
              }
            }
            return _tol_light_range_single_side_max;
          }
          var _tol_light_range_single_side_max = getPipeRunLocationInfos();
          handlePipeLine(thwPipeLine, _tol_light_range_single_side_max);
          static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight)
          {
            var RegionBorderBuffeRdistance = THESAURUSENTREPRENEUR;
            var RepeatedPointDistance = THESAURUSDISPASSIONATE;
            double line_coincide_tolerance = THESAURUSENDANGER;
            var (_tol_group_evca_emg, _) = GetDBTextSize(text1, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var (_tol_light_range_single_side_min, _) = GetDBTextSize(text2, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
            var BranchPortToMainDistance = Math.Max(_tol_group_evca_emg, _tol_light_range_single_side_min);
            var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSDOMESTIC, THESAURUSDOMESTIC), new Vector2d(BranchPortToMainDistance, THESAURUSSTAMPEDE) };
            if (isLeftOrRight == THESAURUSOBSTINACY)
            {
              LineCoincideTolerance = LineCoincideTolerance.GetYAxisMirror();
            }
            var TolLane = LineCoincideTolerance.ToGLineSegments(basePt).ToList();
            foreach (var default_fire_valve_length in TolLane)
            {
              var TolUniformSideLenth = DrawLineSegmentLazy(default_fire_valve_length);
              Dr.SetLabelStylesForDraiNote(TolUniformSideLenth);
            }
            var TolAvgColumnDist = isLeftOrRight ? TolLane[THESAURUSHOUSING].EndPoint : TolLane[THESAURUSHOUSING].StartPoint;
            TolAvgColumnDist = TolAvgColumnDist.OffsetY(THESAURUSENTREPRENEUR);
            if (text1 != null)
            {
              var TolLightRangeMin = DrawTextLazy(text1, line_coincide_tolerance, TolAvgColumnDist);
              Dr.SetLabelStylesForDraiNote(TolLightRangeMin);
            }
            if (text2 != null)
            {
              var TolLightRangeMin = DrawTextLazy(text2, line_coincide_tolerance, TolAvgColumnDist.OffsetY(-line_coincide_tolerance - RegionBorderBuffeRdistance));
              Dr.SetLabelStylesForDraiNote(TolLightRangeMin);
            }
          }
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
            if (TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible)
            {
              var TolLane = TolLightRangeSingleSideMin.DisplaySegs ?? TolLightRangeSingleSideMin.Segs;
              if (TolLane != null)
              {
                drawDomePipes(TolLane);
              }
            }
          }
          {
            var hasPipeLabelStoreys = new HashSet<string>();
            {
              var _allSmoothStoreys = new List<string>();
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
              {
                var run = _tolReturnValueRangeTo.TryGet(MAX_ANGEL_TOLLERANCE);
                if (run != null)
                {
                  if (!run.HasLongTranslator && !run.HasShortTranslator)
                  {
                    _allSmoothStoreys.Add(allNumStoreyLabels[MAX_ANGEL_TOLLERANCE]);
                  }
                }
              }
              var _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSPERMUTATION), _allSmoothStoreys.GetLastOrDefault(INTROPUNITIVENESS) }.SelectNotNull().Distinct().ToList();
              if (_storeys.Count == THESAURUSSTAMPEDE)
              {
                _storeys = new string[] { _allSmoothStoreys.FirstOrDefault(), _allSmoothStoreys.LastOrDefault() }.SelectNotNull().Distinct().ToList();
              }
              _storeys = _storeys.Where(tolGroupBlkLane =>
              {
                var MAX_ANGEL_TOLLERANCE = allNumStoreyLabels.IndexOf(tolGroupBlkLane);
                var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
                return TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible;
              }).ToList();
              if (_storeys.Count == THESAURUSSTAMPEDE)
              {
                _storeys = allNumStoreyLabels.Where(tolGroupBlkLane =>
                {
                  var MAX_ANGEL_TOLLERANCE = allNumStoreyLabels.IndexOf(tolGroupBlkLane);
                  var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
                  return TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible;
                }).Take(THESAURUSHOUSING).ToList();
              }
              hasPipeLabelStoreys.AddRange(_storeys);
              foreach (var tolGroupBlkLane in _storeys)
              {
                var MAX_ANGEL_TOLLERANCE = allNumStoreyLabels.IndexOf(tolGroupBlkLane);
                var TolLightRangeSingleSideMin = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE];
                {
                  string wellsMaxArea, minWellToUrinalDistance;
                  var isLeftOrRight = !thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL(TolLightRangeSingleSideMax));
                  var max_kitchen_to_balcony_distance = ConvertLabelStrings(thwPipeLine.Labels.Where(TolLightRangeSingleSideMax => !IsTL(TolLightRangeSingleSideMax))).Distinct().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
                  if (max_kitchen_to_balcony_distance.Count == THESAURUSPERMUTATION)
                  {
                    wellsMaxArea = max_kitchen_to_balcony_distance[THESAURUSSTAMPEDE];
                    minWellToUrinalDistance = max_kitchen_to_balcony_distance[THESAURUSHOUSING];
                  }
                  else
                  {
                    wellsMaxArea = max_kitchen_to_balcony_distance.JoinWith(THESAURUSCAVALIER);
                    minWellToUrinalDistance = null;
                  }
                  drawLabel(TolLightRangeSingleSideMin.PlBasePt, wellsMaxArea, minWellToUrinalDistance, isLeftOrRight);
                }
                if (_tolReturnValueRange.HasTl)
                {
                  string wellsMaxArea, minWellToUrinalDistance;
                  var max_kitchen_to_balcony_distance = ConvertLabelStrings(thwPipeLine.Labels.Where(TolLightRangeSingleSideMax => IsTL(TolLightRangeSingleSideMax))).Distinct().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
                  if (max_kitchen_to_balcony_distance.Count == THESAURUSPERMUTATION)
                  {
                    wellsMaxArea = max_kitchen_to_balcony_distance[THESAURUSSTAMPEDE];
                    minWellToUrinalDistance = max_kitchen_to_balcony_distance[THESAURUSHOUSING];
                  }
                  else
                  {
                    wellsMaxArea = max_kitchen_to_balcony_distance.JoinWith(THESAURUSCAVALIER);
                    minWellToUrinalDistance = null;
                  }
                  drawLabel(TolLightRangeSingleSideMin.PlBasePt.OffsetX(THESAURUSHYPNOTIC), wellsMaxArea, minWellToUrinalDistance, INTRAVASCULARLY);
                }
              }
            }
            {
              List<string> _storeys;
              var _allSmoothStoreys = new List<string>();
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
              {
                var run = _tolReturnValueRangeTo.TryGet(MAX_ANGEL_TOLLERANCE);
                if (run != null)
                {
                  var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = allNumStoreyLabels[MAX_ANGEL_TOLLERANCE];
                  if (!run.HasLongTranslator && !run.HasShortTranslator && !hasPipeLabelStoreys.Contains(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN))
                  {
                    _allSmoothStoreys.Add(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
                  }
                }
              }
              _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSHOUSING), _allSmoothStoreys.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
              if (_storeys.Count == THESAURUSSTAMPEDE)
              {
                _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSHOUSING), allNumStoreyLabels.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
              }
              if (_storeys.Count == THESAURUSSTAMPEDE)
              {
                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
              }
              foreach (var tolGroupBlkLane in _storeys)
              {
                var MAX_ANGEL_TOLLERANCE = allNumStoreyLabels.IndexOf(tolGroupBlkLane);
                var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
                if (TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible)
                {
                  var run = _tolReturnValueRangeTo.TryGet(MAX_ANGEL_TOLLERANCE);
                  if (run != null)
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = default(Vector2d);
                    if (((_tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING)?.FloorDrainsCount ?? THESAURUSSTAMPEDE) > THESAURUSSTAMPEDE)
                        || (_tolReturnValueRange.Hangings.TryGet(MAX_ANGEL_TOLLERANCE)?.HasDoubleSCurve ?? INTRAVASCULARLY))
                    {
                      MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE);
                    }
                    if (_tolReturnValueRange.IsFL0)
                    {
                      Dr.DrawDN_2(TolLightRangeSingleSideMin.EndPoint + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, THESAURUSSTRIPED, viewModel?.Params?.DirtyWaterWellDN ?? IRRESPONSIBLENESS);
                    }
                    else
                    {
                      Dr.DrawDN_2(TolLightRangeSingleSideMin.EndPoint + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, THESAURUSSTRIPED);
                    }
                    if (_tolReturnValueRange.HasTl)
                    {
                      Dr.DrawDN_3(TolLightRangeSingleSideMin.EndPoint.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), THESAURUSSTRIPED);
                    }
                  }
                }
              }
            }
          }
          var b = INTRAVASCULARLY;
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < allNumStoreyLabels.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.TryGet(MAX_ANGEL_TOLLERANCE);
            if (TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible)
            {
              void TestRightSegsMiddle()
              {
                var TolLane = TolLightRangeSingleSideMin.RightSegsMiddle;
                if (TolLane != null)
                {
                  drawVentPipes(TolLane);
                }
              }
              void TestRightSegsLast()
              {
                var TolLane = TolLightRangeSingleSideMin.RightSegsLast;
                if (TolLane != null)
                {
                  drawVentPipes(TolLane);
                }
              }
              void TestRightSegsFirst()
              {
                var TolLane = TolLightRangeSingleSideMin.RightSegsFirst;
                if (TolLane != null)
                {
                  drawVentPipes(TolLane);
                }
              }
              void Run()
              {
                var tolGroupBlkLane = allNumStoreyLabels[MAX_ANGEL_TOLLERANCE];
                var maxStorey = allNumStoreyLabels.Last();
                if (_tolReturnValueRange.HasTl)
                {
                  bool isFirstTl()
                  {
                    return (_tolReturnValueRange.MaxTl == GetStoreyScore(tolGroupBlkLane));
                  }
                  if (isFirstTl())
                  {
                    var TolLane = TolLightRangeSingleSideMin.RightSegsFirst;
                    if (TolLane != null)
                    {
                      drawVentPipes(TolLane);
                    }
                  }
                  else if (_tolReturnValueRange.MinTl + THESAURUSASPIRATION == tolGroupBlkLane)
                  {
                    var TolLane = TolLightRangeSingleSideMin.RightSegsLast;
                    if (TolLane != null)
                    {
                      drawVentPipes(TolLane);
                    }
                  }
                  else if (GetStoreyScore(tolGroupBlkLane).InRange(_tolReturnValueRange.MinTl, _tolReturnValueRange.MaxTl))
                  {
                    var TolLane = TolLightRangeSingleSideMin.RightSegsMiddle;
                    if (TolLane != null)
                    {
                      if (getShouldToggleBlueMiddleLine())
                      {
                        b = !b;
                        if (b) TolLane = TolLane.Take(THESAURUSHOUSING).ToList();
                      }
                      drawVentPipes(TolLane);
                    }
                  }
                }
              }
              Run();
            }
          }
          {
            var MAX_ANGEL_TOLLERANCE = allNumStoreyLabels.IndexOf(THESAURUSREGION);
            if (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE)
            {
              var tolGroupBlkLane = allNumStoreyLabels[MAX_ANGEL_TOLLERANCE];
              var TolLightRangeSingleSideMin = _tol_light_range_single_side_max.First();
              if (TolLightRangeSingleSideMin != null && TolLightRangeSingleSideMin.Visible)
              {
                var output = new ThwOutput()
                {
                  DirtyWaterWellValues = _tolReturnValueRange.WaterPortLabels.OrderBy(TolLightRangeSingleSideMax =>
                  {
                    long.TryParse(TolLightRangeSingleSideMax, out long MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                  }).ToList(),
                  HasWrappingPipe1 = _tolReturnValueRange.HasWrappingPipe,
                  DN1 = IRRESPONSIBLENESS,
                };
                if (thwPipeLine.Labels.Any(TolLightRangeSingleSideMax => IsFL0(TolLightRangeSingleSideMax)))
                {
                  var basePt = TolLightRangeSingleSideMin.EndPoint;
                  if (_tolReturnValueRange.HasRainPortForFL0)
                  {
                    {
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                      var TolLane = LineCoincideTolerance.ToGLineSegments(basePt);
                      drawDomePipes(TolLane);
                      var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = TolLane.Last().EndPoint.ToPoint3d();
                      {
                        Dr.DrawRainPort(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(THESAURUSDOMESTIC));
                        Dr.DrawRainPortLabel(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-THESAURUSENTREPRENEUR));
                        Dr.DrawStarterPipeHeightLabel(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-THESAURUSENTREPRENEUR + THESAURUSMEDIATION));
                      }
                    }
                    if (_tolReturnValueRange.IsConnectedToFloorDrainForFL0)
                    {
                      var _raiseDistanceToStartDefault = basePt + new Vector2d(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH);
                      DrawFloorDrain(_raiseDistanceToStartDefault.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSFICTION), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC), new Vector2d(-INCONSIDERABILIS, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSNEGLIGENCE, THESAURUSNEGLIGENCE) };
                      var TolLane = LineCoincideTolerance.ToGLineSegments(_raiseDistanceToStartDefault + new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE));
                      drawDomePipes(TolLane);
                    }
                  }
                  else
                  {
                    var _raiseDistanceToStartDefault = basePt + new Vector2d(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH);
                    if (_tolReturnValueRange.IsFL0)
                    {
                      if (_tolReturnValueRange.IsConnectedToFloorDrainForFL0)
                      {
                        if (_tolReturnValueRange.MergeFloorDrainForFL0)
                        {
                          DrawFloorDrain(_raiseDistanceToStartDefault.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                          {
                            var LineCoincideTolerance = new List<Vector2d>() { new Vector2d(THESAURUSSTAMPEDE, -HYPERDISYLLABLE + THESAURUSCREDITABLE), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC), new Vector2d(-HYPERDISYLLABLE - THESAURUSJINGLE + ALSOMEGACEPHALOUS * THESAURUSPERMUTATION, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSHYPNOTIC, THESAURUSHYPNOTIC) };
                            var TolLane = LineCoincideTolerance.ToGLineSegments(_raiseDistanceToStartDefault + new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE));
                            drawDomePipes(TolLane);
                            var default_fire_valve_length = new List<Vector2d> { new Vector2d(-THESAURUSDICTATORIAL, -THESAURUSFICTION), new Vector2d(THESAURUSINCARCERATE, THESAURUSSTAMPEDE) }.ToGLineSegments(TolLane.First().StartPoint)[THESAURUSHOUSING];
                            DrawDimLabel(default_fire_valve_length.StartPoint, default_fire_valve_length.EndPoint, new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE), METACOMMUNICATION, QUOTATIONBENJAMIN);
                          }
                        }
                        else
                        {
                          var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-THESAURUSINHERIT, -DEMONSTRATIONIST), new Vector2d(THESAURUSMIRTHFUL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                          var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.EndPoint).Skip(THESAURUSHOUSING).ToList();
                          drawDomePipes(TolLane);
                          DrawFloorDrain((TolLane.Last().EndPoint + new Vector2d(THESAURUSCAVERN, THESAURUSINTRACTABLE)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                        }
                      }
                    }
                    DrawOutlets1(basePt, THESAURUSEXECRABLE, output, isRainWaterWell: THESAURUSOBSTINACY);
                  }
                }
                else if (_tolReturnValueRange.IsSingleOutlet)
                {
                  void DrawOutlets3(Point2d SIDEWATERBUCKET_X_INDENT)
                  {
                    var values = output.DirtyWaterWellValues;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                    var TolLane = LineCoincideTolerance.ToGLineSegments(SIDEWATERBUCKET_X_INDENT);
                    TolLane.RemoveAt(INTROPUNITIVENESS);
                    drawDomePipes(TolLane);
                    DrawDiryWaterWells1(TolLane[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(TolLane[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
                    if (output.HasWrappingPipe2)
                    {
                      if (_tolReturnValueRange.OutletWrappingPipeRadius != null)
                      {
                        static void DrawLine(string layer, params GLineSegment[] TolLane)
                        {
                          var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
                          foreach (var TolUniformSideLenth in tolReturnValueRange)
                          {
                            TolUniformSideLenth.Layer = layer;
                            ByLayer(TolUniformSideLenth);
                          }
                        }
                        static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = HELIOCENTRICISM)
                        {
                          DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE } }, cb: tolReturnValueRangeTo => { ByLayer(tolReturnValueRangeTo); });
                        }
                        var _tolRegroupMainYRange = TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSDOMESTIC);
                        var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(-DOCTRINARIANISM);
                        var _tolConnectSecPrimAddValue = _tolConnectSecPtRange.OffsetX(QUOTATIONWITTIG);
                        var layer = THESAURUSSTRIPED;
                        DrawLine(layer, new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                        DrawLine(layer, new GLineSegment(_tolConnectSecPrimAddValue, _tolConnectSecPtRange));
                        DrawStoreyHeightSymbol(_tolConnectSecPrimAddValue, THESAURUSSTRIPED, _tolReturnValueRange.OutletWrappingPipeRadius);
                      }
                    }
                    DrawNoteText(output.DN1, TolLane[INTROPUNITIVENESS].StartPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                    DrawNoteText(output.DN2, TolLane[THESAURUSPERMUTATION].EndPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                    if (output.HasCleaningPort1) DrawCleaningPort(TolLane[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    if (output.HasCleaningPort2) DrawCleaningPort(TolLane[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    DrawCleaningPort(TolLane[THESAURUSCOMMUNICATION].EndPoint.ToPoint3d(), THESAURUSOBSTINACY, THESAURUSPERMUTATION);
                  }
                  output.HasWrappingPipe2 = output.HasWrappingPipe1 = _tolReturnValueRange.HasWrappingPipe;
                  output.DN2 = IRRESPONSIBLENESS;
                  DrawOutlets3(TolLightRangeSingleSideMin.EndPoint);
                }
                else if (_tolReturnValueRange.FloorDrainsCountAt1F > THESAURUSSTAMPEDE)
                {
                  for (int RADIAN_TOLERANCE = THESAURUSSTAMPEDE; RADIAN_TOLERANCE < _tolReturnValueRange.FloorDrainsCountAt1F; RADIAN_TOLERANCE++)
                  {
                    var _raiseDistanceToStartDefault = TolLightRangeSingleSideMin.EndPoint + new Vector2d(THESAURUSDISAGREEABLE + RADIAN_TOLERANCE * INCONSIDERABILIS, -THESAURUSINTRENCH);
                    DrawFloorDrain(_raiseDistanceToStartDefault.ToPoint3d(), INTRAVASCULARLY, QUOTATIONBARBADOS);
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = new Vector2d(THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                    Get2FloorDrainDN(out string _tolReturnValue0Approx, out string TolGroupEvcaEmg);
                    if (RADIAN_TOLERANCE == THESAURUSSTAMPEDE)
                    {
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-THESAURUSLINGER + THESAURUSBISEXUAL, -METALINGUISTICS), new Vector2d(ORTHOPAEDICALLY - THESAURUSBISEXUAL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                      var TolLane = LineCoincideTolerance.ToGLineSegments(_raiseDistanceToStartDefault + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).Skip(THESAURUSHOUSING).ToList();
                      var v3 = _tolReturnValueRange.FloorDrainsCountAt1F == THESAURUSHOUSING ? _tolReturnValue0Approx : TolGroupEvcaEmg;
                      var _tolRegroupMainYRange = TolLane[THESAURUSSTAMPEDE].EndPoint;
                      DrawNoteText(v3, _tolRegroupMainYRange.OffsetXY(-POLYOXYMETHYLENE - THESAURUSREPRODUCTION, -THESAURUSDOMESTIC).OffsetY(-THESAURUSEQUATION));
                      TolLane = new List<Vector2d> { new Vector2d(-THESAURUSCAVERN, -THESAURUSECLECTIC), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSINGLORIOUS, THESAURUSSTAMPEDE) }.ToGLineSegments(_raiseDistanceToStartDefault.OffsetY(-THESAURUSEQUATION)).Skip(THESAURUSHOUSING).ToList();
                      drawDomePipes(TolLane);
                      var _tolConnectSecPrimAddValue = TolLane.First().StartPoint;
                      drawDomePipe(new GLineSegment(_tolConnectSecPrimAddValue, _tolConnectSecPrimAddValue.OffsetY(THESAURUSEQUATION)));
                    }
                    else
                    {
                      var _tolConnectSecPtRange = _raiseDistanceToStartDefault + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(-QUOTATIONMASTOID, -METALINGUISTICS), new Vector2d(THESAURUSISOLATION, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                      var TolLane = LineCoincideTolerance.ToGLineSegments(_tolConnectSecPtRange).Skip(THESAURUSHOUSING).ToList();
                      var _tolRegroupMainYRange = TolLane[THESAURUSSTAMPEDE].StartPoint;
                      DrawNoteText(_tolReturnValue0Approx, _tolRegroupMainYRange.OffsetXY(HYPERDISYLLABLE, -THESAURUSDOMESTIC).OffsetY(-THESAURUSEQUATION));
                      TolLane = new List<Vector2d> { new Vector2d(-THESAURUSCAVERN, -THESAURUSECLECTIC), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSISOLATION, THESAURUSSTAMPEDE) }.ToGLineSegments(_raiseDistanceToStartDefault.OffsetY(-THESAURUSEQUATION)).Skip(THESAURUSHOUSING).ToList();
                      drawDomePipes(TolLane);
                      var _tolConnectSecPrimAddValue = TolLane.First().StartPoint;
                      drawDomePipe(new GLineSegment(_tolConnectSecPrimAddValue, _tolConnectSecPrimAddValue.OffsetY(THESAURUSEQUATION)));
                    }
                  }
                  DrawOutlets1(TolLightRangeSingleSideMin.EndPoint, THESAURUSEXECRABLE, output, _tol_light_range_min: new Vector2d(THESAURUSSTAMPEDE, -THESAURUSSURPRISED));
                }
                else if (_tolReturnValueRange.HasBasinInKitchenAt1F)
                {
                  output.HasWrappingPipe2 = output.HasWrappingPipe1;
                  output.DN1 = getBasinDN();
                  output.DN2 = IRRESPONSIBLENESS;
                  void DrawOutlets4(Point2d SIDEWATERBUCKET_X_INDENT, double HEIGHT)
                  {
                    var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = PERIODONTOCLASIA;
                    var MAX_DEVICE_TO_DEVICE = QUOTATIONTRANSFERABLE;
                    if (getDSCurveValue() == THESAURUSDISCIPLINARIAN && MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN == PERIODONTOCLASIA)
                    {
                      MAX_DEVICE_TO_DEVICE = THESAURUSFLIRTATIOUS;
                    }
                    var values = output.DirtyWaterWellValues;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL - THESAURUSDOMESTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER + MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                    var TolLane = LineCoincideTolerance.ToGLineSegments(SIDEWATERBUCKET_X_INDENT);
                    TolLane.RemoveAt(INTROPUNITIVENESS);
                    DrawDiryWaterWells1(TolLane[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(TolLane[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
                    if (output.HasWrappingPipe2)
                    {
                      if (_tolReturnValueRange.OutletWrappingPipeRadius != null)
                      {
                        static void DrawLine(string layer, params GLineSegment[] TolLane)
                        {
                          var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
                          foreach (var TolUniformSideLenth in tolReturnValueRange)
                          {
                            TolUniformSideLenth.Layer = layer;
                            ByLayer(TolUniformSideLenth);
                          }
                        }
                        static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                        {
                          DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE } }, cb: tolReturnValueRangeTo => { ByLayer(tolReturnValueRangeTo); });
                        }
                        var p10 = TolLane[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSDOMESTIC);
                        var p20 = p10.OffsetY(-DOCTRINARIANISM);
                        var p30 = p20.OffsetX(QUOTATIONWITTIG);
                        var layer = THESAURUSSTRIPED;
                        DrawLine(layer, new GLineSegment(p10, p20));
                        DrawLine(layer, new GLineSegment(p30, p20));
                        DrawStoreyHeightSymbol(p30, THESAURUSSTRIPED, _tolReturnValueRange.OutletWrappingPipeRadius);
                      }
                    }
                    DrawNoteText(output.DN1, TolLane[INTROPUNITIVENESS].StartPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                    DrawNoteText(output.DN2, TolLane[THESAURUSPERMUTATION].EndPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                    if (output.HasCleaningPort1) DrawCleaningPort(TolLane[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    if (output.HasCleaningPort2) DrawCleaningPort(TolLane[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    var _raiseDistanceToStartDefault = TolLane[THESAURUSCOMMUNICATION].EndPoint;
                    var MAX_TAG_XPOSITION = THESAURUSHYPNOTIC + HEIGHT / THESAURUSCOMMUNICATION;
                    var _tolRegroupMainYRange = _raiseDistanceToStartDefault.OffsetX(-THESAURUSPERVADE) + new Vector2d(-THESAURUSOFFEND + THESAURUSDOMESTIC, MAX_TAG_XPOSITION);
                    DrawDSCurve(_tolRegroupMainYRange, THESAURUSOBSTINACY, MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetY(-MAX_TAG_XPOSITION);
                    TolLane.Add(new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
                    if (MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN == THESAURUSDISCIPLINARIAN)
                    {
                      var _tol_light_range_max = TolLane[INTROPUNITIVENESS].StartPoint;
                      var _segs = new List<Vector2d> { new Vector2d(THESAURUSCHORUS, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSCELESTIAL), new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE) }.ToGLineSegments(_tol_light_range_max);
                      TolLane = TolLane.Take(INTROPUNITIVENESS).ToList();
                      TolLane.AddRange(_segs);
                    }
                    drawDomePipes(TolLane);
                  }
                  DrawOutlets4(TolLightRangeSingleSideMin.EndPoint, HEIGHT);
                }
                else
                {
                  DrawOutlets1(TolLightRangeSingleSideMin.EndPoint, THESAURUSEXECRABLE, output);
                }
              }
            }
          }
          {
            var linesKillers = new HashSet<Geometry>();
            if (_tolReturnValueRange.IsFL0)
            {
              for (int MAX_ANGEL_TOLLERANCE = _tolReturnValueRange.Items.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE; --MAX_ANGEL_TOLLERANCE)
              {
                if (_tolReturnValueRange.Items[MAX_ANGEL_TOLLERANCE].Exist)
                {
                  var TolLightRangeSingleSideMin = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE];
                  DrawAiringSymbol(TolLightRangeSingleSideMin.StartPoint, getCouldHavePeopleOnRoof(), INSTRUMENTALITY);
                  break;
                }
              }
            }
            dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
            var DEFAULT_VOLTAGE = dome_lines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
            dome_lines = DEFAULT_VOLTAGE.Except(GeoFac.CreateIntersectsSelector(DEFAULT_VOLTAGE)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGLineSegments()).ToList();
          }
          {
            if (_tolReturnValueRange.HasTl)
            {
              var tolReturnValueRange = new HashSet<GLineSegment>(vent_lines);
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < _tolReturnValueRange.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
              {
                var hanging = _tolReturnValueRange.Hangings[MAX_ANGEL_TOLLERANCE];
                if (SIDEWATERBUCKET_Y_INDENT[MAX_ANGEL_TOLLERANCE] == _tolReturnValueRange.MaxTl + THESAURUSASPIRATION)
                {
                  var TolLightRangeSingleSideMin = _tol_light_range_single_side_max[MAX_ANGEL_TOLLERANCE];
                  if (TolLightRangeSingleSideMin != null)
                  {
                    foreach (var default_fire_valve_length in TolLightRangeSingleSideMin.RightSegsFirst)
                    {
                      tolReturnValueRange.Remove(default_fire_valve_length);
                    }
                    var RADIAN_TOLERANCE = HEIGHT / THESAURUSINCOMING;
                    var LineCoincideTolerance = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, (INCONSIDERABILIS + THESAURUSDOMESTIC) * RADIAN_TOLERANCE), new Vector2d(THESAURUSHYPNOTIC, (-THESAURUSHALTER) * RADIAN_TOLERANCE), new Vector2d(THESAURUSSTAMPEDE, (-THESAURUSPRIVILEGE - THESAURUSDOMESTIC) * RADIAN_TOLERANCE) };
                    var TolLane = LineCoincideTolerance.ToGLineSegments(TolLightRangeSingleSideMin.EndPoint).Skip(THESAURUSHOUSING).ToList();
                    tolReturnValueRange.AddRange(TolLane);
                  }
                  break;
                }
              }
              vent_lines = tolReturnValueRange.ToList();
            }
          }
          {
            var auto_conn = INTRAVASCULARLY;
            var layer = _tolReturnValueRange.Labels.Any(IsFL0) ? INSTRUMENTALITY : dome_layer;
            if (auto_conn)
            {
              foreach (var default_fire_valve_width in GeoFac.GroupParallelLines(dome_lines, THESAURUSHOUSING, UNCONSEQUENTIAL))
              {
                var TolUniformSideLenth = DrawLineSegmentLazy(GeoFac.GetCenterLine(default_fire_valve_width, work_around: ELECTROLUMINESCENT));
                TolUniformSideLenth.Layer = layer;
                ByLayer(TolUniformSideLenth);
              }
              foreach (var default_fire_valve_width in GeoFac.GroupParallelLines(vent_lines, THESAURUSHOUSING, UNCONSEQUENTIAL))
              {
                var TolUniformSideLenth = DrawLineSegmentLazy(GeoFac.GetCenterLine(default_fire_valve_width, work_around: ELECTROLUMINESCENT));
                TolUniformSideLenth.Layer = vent_layer;
                ByLayer(TolUniformSideLenth);
              }
            }
            else
            {
              foreach (var dome_line in dome_lines)
              {
                var TolUniformSideLenth = DrawLineSegmentLazy(dome_line);
                TolUniformSideLenth.Layer = layer;
                ByLayer(TolUniformSideLenth);
              }
              foreach (var _line in vent_lines)
              {
                var TolUniformSideLenth = DrawLineSegmentLazy(_line);
                TolUniformSideLenth.Layer = vent_layer;
                ByLayer(TolUniformSideLenth);
              }
            }
          }
        }
      }
    }
    public static void DrawDrainageSystemDiagram(Point2d SIDEWATERBUCKET_X_INDENT, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> SIDEWATERBUCKET_Y_INDENT, int MAX_TAG_LENGTH, int TEXT_INDENT, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double MAX_DEVICE_TO_BALCONY, int __dy, DrainageSystemDiagramViewModel viewModel, ExtraInfo exInfo)
    {
      exInfo.vm = viewModel;
      var o = new Opt()
      {
        SIDEWATERBUCKET_X_INDENT = SIDEWATERBUCKET_X_INDENT,
        pipeGroupItems = pipeGroupItems,
        allNumStoreyLabels = allNumStoreyLabels,
        SIDEWATERBUCKET_Y_INDENT = SIDEWATERBUCKET_Y_INDENT,
        MAX_TAG_LENGTH = MAX_TAG_LENGTH,
        TEXT_INDENT = TEXT_INDENT,
        OFFSET_X = OFFSET_X,
        SPAN_X = SPAN_X,
        HEIGHT = HEIGHT,
        COUNT = COUNT,
        MAX_DEVICE_TO_BALCONY = MAX_DEVICE_TO_BALCONY,
        __dy = __dy,
        viewModel = viewModel,
        exInfo = exInfo,
      };
      o.Draw();
    }
    public static void DrawAiringSymbol(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, bool canPeopleBeOnRoof, string layer = THESAURUSCONTROVERSY)
    {
      DrawAiringSymbol(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, canPeopleBeOnRoof ? THESAURUSPARTNER : THESAURUSINEFFECTUAL, layer);
    }
    public static void DrawAiringSymbol(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, string toilet_buffer_distance, string layer)
    {
      DrawBlockReference(blkName: MÖNCHENGLADBACH, basePt: MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint3d(), layer: layer, cb: tolReturnValueRangeTo =>
      {
        tolReturnValueRangeTo.ObjectId.SetDynBlockValue(QUINQUAGENARIAN, toilet_buffer_distance);
      });
    }
    public static CommandContext commandContext;
    public static void CollectFloorListDatasEx(bool maxBasecircleArea)
    {
      if (maxBasecircleArea) FocusMainWindow();
      ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out _, () =>
      {
        using (DocLock)
        {
          var range = TrySelectRangeEx();
          TryUpdateByRange(range, INTRAVASCULARLY);
        }
      });
    }
    public static void TryUpdateByRange(Point3dCollection range, bool _lock)
    {
      void maxDeviceplatformArea()
      {
        if (range == null) return;
        using var adb = AcadDatabase.Active();
        var (ctx, max_device_to_device) = GetStoreyContext(range, adb);
        commandContext.StoreyContext = ctx;
        InitFloorListDatas(adb, max_device_to_device);
        CadCache.SetCache(CadCache.CurrentFile, PERSPICACIOUSNESS, range);
        CadCache.UpdateByRange(range);
      }
      if (_lock)
      {
        using (DocLock)
        {
          maxDeviceplatformArea();
        }
      }
      else
      {
        maxDeviceplatformArea();
      }
    }
    public static (StoreyContext, List<BlockReference>) GetStoreyContext(Point3dCollection range, AcadDatabase adb)
    {
      var ctx = new StoreyContext();
      var DEFAULT_FIRE_VALVE_LENGTH = range?.ToGRect().ToPolygon();
      var max_device_to_device = GetStoreyBlockReferences(adb);
      var max_device_to_balcony = new List<BlockReference>();
      var KitchenBufferDistance = new List<StoreyInfo>();
      foreach (var tolReturnValueRangeTo in max_device_to_device)
      {
        var TolLightRangeSingleSideMin = GetStoreyInfo(tolReturnValueRangeTo);
        if (DEFAULT_FIRE_VALVE_LENGTH?.Contains(TolLightRangeSingleSideMin.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY)
        {
          max_device_to_balcony.Add(tolReturnValueRangeTo);
          KitchenBufferDistance.Add(TolLightRangeSingleSideMin);
        }
      }
      FixStoreys(KitchenBufferDistance);
      ctx.StoreyInfos = KitchenBufferDistance;
      return (ctx, max_device_to_balcony);
    }
    public static void InitFloorListDatas(AcadDatabase adb, List<BlockReference> max_device_to_device)
    {
      var ctx = commandContext.StoreyContext;
      var KitchenBufferDistance = max_device_to_device.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ObjectId).ToObjectIdCollection();
      var service = new ThReadStoreyInformationService();
      service.Read(KitchenBufferDistance);
      commandContext.ViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
    }
    public static (List<DrainageDrawingData>, ExtraInfo, bool) CreateDrainageDrawingData(out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
    {
      geoData.ODLines.AddRange(geoData.DLines);
      ThDrainageService.PreFixGeoData(geoData);
      if (noWL && geoData.Labels.Any(TolLightRangeSingleSideMax => IsWL(TolLightRangeSingleSideMax.Text)))
      {
        MessageBox.Show(PARALINGUISTICALLY);
      }
      var (_drDatas, exInfo) = _CreateDrainageDrawingData(geoData, THESAURUSOBSTINACY);
      drDatas = _drDatas;
      return (drDatas, exInfo, THESAURUSOBSTINACY);
    }
    public static (List<DrainageDrawingData>, ExtraInfo) CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
    {
      geoData.ODLines.AddRange(geoData.DLines);
      ThDrainageService.PreFixGeoData(geoData);
      return _CreateDrainageDrawingData(geoData, noDraw);
    }
    private static (List<DrainageDrawingData>, ExtraInfo) _CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
    {
      List<DrainageDrawingData> drDatas;
      ThDrainageService.ConnectLabelToLabelLine(geoData);
      geoData.FixData();
      var cadDataMain = DrainageCadData.Create(geoData);
      var cadDatas = cadDataMain.SplitByStorey();
      var exInfo = DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out drDatas);
      if (noDraw) Dispose();
      return (drDatas, exInfo);
    }
    public static ExtraInfo CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = INTRAVASCULARLY)
    {
      CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
      var tolReturnValueMinRange = CreateDrainageDrawingData(out drDatas, noWL, geoData);
      tolReturnValueMinRange.Item2.drDatas = tolReturnValueMinRange.Item1;
      tolReturnValueMinRange.Item2.OK = tolReturnValueMinRange.Item3;
      return tolReturnValueMinRange.Item2;
    }
    public static ExtraInfo CollectDrainageData(AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = INTRAVASCULARLY)
    {
      CollectDrainageGeoData(adb, out storeysItems, out DrainageGeoData geoData, ctx);
      var tolReturnValueMinRange = CreateDrainageDrawingData(out drDatas, noWL, geoData);
      tolReturnValueMinRange.Item2.drDatas = tolReturnValueMinRange.Item1;
      tolReturnValueMinRange.Item2.OK = tolReturnValueMinRange.Item3;
      return tolReturnValueMinRange.Item2;
    }
    public static List<StoreyItem> GetStoreysItem(List<StoreyInfo> KitchenBufferDistance)
    {
      var storeysItems = new List<StoreyItem>();
      foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in KitchenBufferDistance)
      {
        var REPEATED_POINT_DISTANCE = new StoreyItem();
        storeysItems.Add(REPEATED_POINT_DISTANCE);
        switch (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.StoreyType)
        {
          case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
            break;
          case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
            {
              REPEATED_POINT_DISTANCE.Labels = new List<string>() { THESAURUSARGUMENTATIVE };
            }
            break;
          case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
            break;
          case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
          case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
            {
              REPEATED_POINT_DISTANCE.Ints = MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN.Numbers.OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
              REPEATED_POINT_DISTANCE.Labels = REPEATED_POINT_DISTANCE.Ints.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax + THESAURUSASPIRATION).ToList();
            }
            break;
          default:
            break;
        }
      }
      return storeysItems;
    }
    public static List<StoreyInfo> GetStoreys(AcadDatabase adb, CommandContext ctx)
    {
      return ctx.StoreyContext.StoreyInfos;
    }
    public static void CollectDrainageGeoData(AcadDatabase adb, out List<StoreyItem> storeysItems, out DrainageGeoData geoData, CommandContext ctx)
    {
      var KitchenBufferDistance = GetStoreys(adb, ctx);
      FixStoreys(KitchenBufferDistance);
      storeysItems = GetStoreysItem(KitchenBufferDistance);
      geoData = new DrainageGeoData();
      geoData.Init();
      geoData.StoreyInfos.AddRange(KitchenBufferDistance);
      geoData.StoreyItems.AddRange(storeysItems);
      DrainageService.CollectGeoData(adb, geoData, ctx);
      geoData.Flush();
    }
    public static List<StoreyInfo> GetStoreys(Point3dCollection range, AcadDatabase adb)
    {
      var DEFAULT_FIRE_VALVE_LENGTH = range?.ToGRect().ToPolygon();
      var KitchenBufferDistance = GetStoreyBlockReferences(adb).Select(TolLightRangeSingleSideMax => GetStoreyInfo(TolLightRangeSingleSideMax)).Where(TolLightRangeSingleSideMin => DEFAULT_FIRE_VALVE_LENGTH?.Contains(TolLightRangeSingleSideMin.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY).ToList();
      FixStoreys(KitchenBufferDistance);
      return KitchenBufferDistance;
    }
    public static void CollectDrainageGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out DrainageGeoData geoData)
    {
      var KitchenBufferDistance = GetStoreys(range, adb);
      FixStoreys(KitchenBufferDistance);
      storeysItems = GetStoreysItem(KitchenBufferDistance);
      geoData = new DrainageGeoData();
      geoData.Init();
      geoData.StoreyInfos.AddRange(KitchenBufferDistance);
      DrainageService.CollectGeoData(range, adb, geoData);
      geoData.StoreyItems.AddRange(storeysItems);
      geoData.Flush();
    }
    public static void DrawDrainageSystemDiagram(DrainageSystemDiagramViewModel viewModel, bool maxBasecircleArea)
    {
      if (DrawWLPipeSystem()) return;
      if (TOILET_WELLS_INTERVAL is null) return;
      if (maxBasecircleArea) FocusMainWindow();
      if (commandContext == null) return;
      if (commandContext.StoreyContext == null) return;
      if (commandContext.StoreyContext.StoreyInfos == null) return;
      if (!TrySelectPoint(out Point3d basePt)) return;
      if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
      using (DocLock)
      using (var adb = AcadDatabase.Active())
      using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
      {
        Dispose();
        LayerThreeAxes(new List<string>() { COSTERMONGERING, THESAURUSCONTROVERSY, THESAURUSSTRIPED, THESAURUSJUBILEE, THESAURUSDEFAULTER, CIRCUMCONVOLUTION, THUNDERSTRICKEN });
        var KitchenBufferDistance = commandContext.StoreyContext.StoreyInfos;
        List<StoreyItem> storeysItems;
        List<DrainageDrawingData> drDatas;
        var range = commandContext.range;
        ExtraInfo exInfo;
        if (range != null)
        {
          exInfo = CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSOBSTINACY);
          if (!exInfo.OK) return;
        }
        else
        {
          exInfo = CollectDrainageData(adb, out storeysItems, out drDatas, commandContext, noWL: THESAURUSOBSTINACY);
          if (!exInfo.OK) return;
        }
        var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, DrainageSystemDiagram.commandContext?.ViewModel, out List<int> tolRegroupMainYRange, out List<string> tolConnectSecPtRange);
        var allNumStoreyLabels = tolRegroupMainYRange.Select(MAX_ANGEL_TOLLERANCE => MAX_ANGEL_TOLLERANCE + THESAURUSASPIRATION).ToList();
        var SIDEWATERBUCKET_Y_INDENT = allNumStoreyLabels.Concat(tolConnectSecPtRange).ToList();
        var MAX_TAG_LENGTH = SIDEWATERBUCKET_Y_INDENT.Count - THESAURUSHOUSING;
        var TEXT_INDENT = THESAURUSSTAMPEDE;
        var OFFSET_X = QUOTATIONLETTERS;
        var SPAN_X = BALANOPHORACEAE + QUOTATIONWITTIG + THESAURUSNAUGHT;
        var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSINCOMING;
        var COUNT = pipeGroupItems.Count;
        var MAX_DEVICE_TO_BALCONY = HEIGHT - THESAURUSINCOMING;
        var __dy = THESAURUSHYPNOTIC;
        Dispose();
        DrawDrainageSystemDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, SIDEWATERBUCKET_Y_INDENT, MAX_TAG_LENGTH, TEXT_INDENT, OFFSET_X, SPAN_X, HEIGHT, COUNT, MAX_DEVICE_TO_BALCONY, __dy, viewModel, exInfo);
        FlushDQ(adb);
      }
    }
    public static void DrawDrainageSystemDiagram()
    {
      FocusMainWindow();
      var range = TrySelectRangeEx();
      if (range == null) return;
      if (!TrySelectPoint(out Point3d point3D)) return;
      var SIDEWATERBUCKET_X_INDENT = point3D.ToPoint2d();
      if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
      using (DocLock)
      using (var adb = AcadDatabase.Active())
      using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
      {
        List<StoreyItem> storeysItems;
        List<DrainageDrawingData> drDatas;
        var exInfo = CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSOBSTINACY);
        if (!exInfo.OK) return;
        var vm = DrainageSystemDiagram.commandContext?.ViewModel;
        var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, vm, out List<int> tolRegroupMainYRange, out List<string> tolConnectSecPtRange);
        Dispose();
        DrawDrainageSystemDiagram(drDatas, storeysItems, SIDEWATERBUCKET_X_INDENT, pipeGroupItems, tolRegroupMainYRange, tolConnectSecPtRange, vm, exInfo);
        FlushDQ(adb);
      }
    }
    public static List<DrainageGroupedPipeItem> GetDrainageGroupedPipeItems(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, DrainageSystemDiagramViewModel vm, out List<int> tolRegroupMainYRange, out List<string> tolConnectSecPtRange)
    {
      var _storeys = new List<string>();
      foreach (var REPEATED_POINT_DISTANCE in storeysItems)
      {
        REPEATED_POINT_DISTANCE.Init();
        _storeys.AddRange(REPEATED_POINT_DISTANCE.Labels);
      }
      _storeys = _storeys.Distinct().OrderBy(GetStoreyScore).ToList();
      var minS = _storeys.Where(IsNumStorey).Select(TolLightRangeSingleSideMax => GetStoreyScore(TolLightRangeSingleSideMax)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax > THESAURUSSTAMPEDE).Min();
      var maxS = _storeys.Where(IsNumStorey).Select(TolLightRangeSingleSideMax => GetStoreyScore(TolLightRangeSingleSideMax)).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax > THESAURUSSTAMPEDE).Max();
      var countS = maxS - minS + THESAURUSHOUSING;
      tolRegroupMainYRange = new List<int>();
      for (int tolGroupBlkLane = minS; tolGroupBlkLane <= maxS; tolGroupBlkLane++)
      {
        tolRegroupMainYRange.Add(tolGroupBlkLane);
      }
      tolConnectSecPtRange = _storeys.Where(TolLightRangeSingleSideMax => !IsNumStorey(TolLightRangeSingleSideMax)).ToList();
      var allNumStoreyLabels = tolRegroupMainYRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax + THESAURUSASPIRATION).ToList();
      bool getCanHaveDownboard()
      {
        return vm?.Params?.CanHaveDownboard ?? THESAURUSOBSTINACY;
      }
      bool testExist(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData.VerticalPipeLabels.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      bool hasLong(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.LongTranslatorLabels.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
              {
                var tmp = storeysItems[MAX_ANGEL_TOLLERANCE].Labels.Where(IsNumStorey).ToList();
                if (tmp.Count > THESAURUSHOUSING)
                {
                  var floor = tmp.Select(GetStoreyScore).Max() + THESAURUSASPIRATION;
                  if (tolGroupBlkLane != floor) return INTRAVASCULARLY;
                }
                return THESAURUSOBSTINACY;
              }
            }
          }
        }
        return INTRAVASCULARLY;
      }
      if (vm?.Params is null)
      {
        ThMEPWSS.FlatDiagramNs.FlatDiagramService.DrawDrainageFlatDiagram(vm);
      }
      bool hasShort(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.ShortTranslatorLabels.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
              {
                {
                  var tmp = storeysItems[MAX_ANGEL_TOLLERANCE].Labels.Where(IsNumStorey).ToList();
                  if (tmp.Count > THESAURUSHOUSING)
                  {
                    var floor = tmp.Select(GetStoreyScore).Max() + THESAURUSASPIRATION;
                    if (tolGroupBlkLane != floor) return INTRAVASCULARLY;
                  }
                }
                return THESAURUSOBSTINACY;
              }
            }
          }
        }
        return INTRAVASCULARLY;
      }
      string getWaterPortLabel(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        foreach (var drData in drDatas)
        {
          if (drData.Outlets.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out string value))
          {
            return value;
          }
        }
        return THESAURUSSPECIFICATION;
      }
      bool hasWaterPort(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        return getWaterPortLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) != null;
      }
      int getMinTl(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        var scores = new List<int>();
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            var drData = drDatas[MAX_ANGEL_TOLLERANCE];
            var score = GetStoreyScore(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
            if (score < ushort.MaxValue)
            {
              if (drData.VerticalPipeLabels.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) scores.Add(score);
            }
          }
        }
        if (scores.Count == THESAURUSSTAMPEDE) return THESAURUSSTAMPEDE;
        var tolGroupEmgLightEvac = scores.Min() - THESAURUSHOUSING;
        if (tolGroupEmgLightEvac <= THESAURUSSTAMPEDE) return THESAURUSHOUSING;
        return tolGroupEmgLightEvac;
      }
      int getMaxTl(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        var scores = new List<int>();
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            var drData = drDatas[MAX_ANGEL_TOLLERANCE];
            var score = GetStoreyScore(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN);
            if (score < ushort.MaxValue)
            {
              if (drData.VerticalPipeLabels.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) scores.Add(score);
            }
          }
        }
        return scores.Count == THESAURUSSTAMPEDE ? THESAURUSSTAMPEDE : scores.Max();
      }
      bool is4Tune(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData._4tunes.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      bool getIsShunt(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData.Shunts.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      int getSingleOutletFDCount(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              drData.SingleOutletFloorDrains.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
            }
          }
        }
        return THESAURUSSTAMPEDE;
      }
      int getFDCount(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              drData.FloorDrains.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              if (tolGroupBlkLane == HYDROCHLOROFLUOROCARBON)
              {
                var TolGroupEvcaEmg = drDatas.Select(TolLightRangeSingleSideMax => { TolLightRangeSingleSideMax.FdsCountAt2F.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int vv); return vv; }).MaxOrZero();
                return Math.Max(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, TolGroupEvcaEmg);
              }
              return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
            }
          }
        }
        return THESAURUSSTAMPEDE;
      }
      int getCirclesCount(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.tolGroupBlkLaneHead.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN))
              {
                return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
              }
            }
          }
        }
        return THESAURUSSTAMPEDE;
      }
      bool isKitchen(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData.KitchenFls.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      bool isBalcony(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData.BalconyFls.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      bool getIsConnectedToFloorDrainForFL0(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        if (!IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        bool maxDeviceplatformArea(string tolGroupBlkLane)
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
          {
            foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
            {
              if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
              {
                var drData = drDatas[MAX_ANGEL_TOLLERANCE];
                return drData.IsConnectedToFloorDrainForFL0.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              }
            }
          }
          return INTRAVASCULARLY;
        }
        return maxDeviceplatformArea(THESAURUSREGION) || maxDeviceplatformArea(THESAURUSTABLEAU);
      }
      bool getHasRainPort(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        if (!IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        bool maxDeviceplatformArea(string tolGroupBlkLane)
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
          {
            foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
            {
              if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
              {
                var drData = drDatas[MAX_ANGEL_TOLLERANCE];
                return drData.HasRainPortSymbolsForFL0.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              }
            }
          }
          return INTRAVASCULARLY;
        }
        return maxDeviceplatformArea(THESAURUSREGION) || maxDeviceplatformArea(THESAURUSTABLEAU);
      }
      bool isToilet(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              return drData.ToiletPls.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      int getWashingMachineFloorDrainsCount(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              drData.WashingMachineFloorDrains.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
              return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
            }
          }
        }
        return THESAURUSSTAMPEDE;
      }
      bool IsSeries(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == tolGroupBlkLane)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.Shunts.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
              {
                return INTRAVASCULARLY;
              }
            }
          }
        }
        return THESAURUSOBSTINACY;
      }
      bool hasOutletlWrappingPipe(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == THESAURUSREGION)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.HasOutletWrappingPipe.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return THESAURUSOBSTINACY;
              return drData.OutletWrappingPipeDict.ContainsValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            }
          }
        }
        return INTRAVASCULARLY;
      }
      string getOutletWrappingPipeRadius(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        if (!hasOutletlWrappingPipe(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return null;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == THESAURUSREGION)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              foreach (var max_rainpipe_to_balconyfloordrain in drData.OutletWrappingPipeDict)
              {
                if (max_rainpipe_to_balconyfloordrain.Value == MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
                {
                  var id = max_rainpipe_to_balconyfloordrain.Key;
                  drData.OutletWrappingPipeRadiusStringDict.TryGetValue(id, out string MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                  return MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                }
              }
            }
          }
        }
        return null;
      }
      int getFloorDrainsCountAt1F(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        if (!IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return THESAURUSSTAMPEDE;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == THESAURUSREGION)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              drData.FloorDrains.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int DEFAULT_FIRE_VALVE_WIDTH);
              return DEFAULT_FIRE_VALVE_WIDTH;
            }
          }
        }
        return THESAURUSSTAMPEDE;
      }
      bool getIsMerge(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)
      {
        if (!IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) return INTRAVASCULARLY;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < storeysItems.Count; MAX_ANGEL_TOLLERANCE++)
        {
          foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in storeysItems[MAX_ANGEL_TOLLERANCE].Labels)
          {
            if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == THESAURUSREGION)
            {
              var drData = drDatas[MAX_ANGEL_TOLLERANCE];
              if (drData.Merges.Contains(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
              {
                return THESAURUSOBSTINACY;
              }
            }
          }
        }
        return INTRAVASCULARLY;
      }
      bool HasKitchenWashingMachine(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, string tolGroupBlkLane)
      {
        return INTRAVASCULARLY;
      }
      var pipeInfoDict = new Dictionary<string, DrainageGroupingPipeItem>();
      var alllabels = new HashSet<string>(drDatas.SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.VerticalPipeLabels));
      var allFls = alllabels.Where(TolLightRangeSingleSideMax => IsFL(TolLightRangeSingleSideMax)).ToList();
      var allPls = alllabels.Where(TolLightRangeSingleSideMax => IsPL(TolLightRangeSingleSideMax)).ToList();
      var FlGroupingItems = new List<DrainageGroupingPipeItem>();
      var PlGroupingItems = new List<DrainageGroupingPipeItem>();
      var SIDEWATERBUCKET_Y_INDENT = allNumStoreyLabels.Concat(tolConnectSecPtRange).OrderBy(GetStoreyScore).ToList();
      foreach (var fl in allFls)
      {
        var REPEATED_POINT_DISTANCE = new DrainageGroupingPipeItem()
        {
          Label = fl,
          PipeType = PipeType.FL,
        };
        REPEATED_POINT_DISTANCE.HasWaterPort = hasWaterPort(fl);
        REPEATED_POINT_DISTANCE.WaterPortLabel = getWaterPortLabel(fl);
        REPEATED_POINT_DISTANCE.HasWrappingPipe = hasOutletlWrappingPipe(fl);
        REPEATED_POINT_DISTANCE.FloorDrainsCountAt1F = getFloorDrainsCountAt1F(fl);
        REPEATED_POINT_DISTANCE.OutletWrappingPipeRadius = getOutletWrappingPipeRadius(fl);
        REPEATED_POINT_DISTANCE.Items = new List<DrainageGroupingPipeItem.ValueItem>();
        REPEATED_POINT_DISTANCE.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
        foreach (var tolGroupBlkLane in allNumStoreyLabels)
        {
          var _hasLong = hasLong(fl, tolGroupBlkLane);
          REPEATED_POINT_DISTANCE.Items.Add(new DrainageGroupingPipeItem.ValueItem()
          {
            Exist = testExist(fl, tolGroupBlkLane),
            HasLong = _hasLong,
            HasShort = hasShort(fl, tolGroupBlkLane),
          });
          REPEATED_POINT_DISTANCE.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
          {
            Is4Tune = is4Tune(fl, tolGroupBlkLane),
            FloorDrainsCount = getFDCount(fl, tolGroupBlkLane),
            WashingMachineFloorDrainsCount = getWashingMachineFloorDrainsCount(fl, tolGroupBlkLane),
            Storey = tolGroupBlkLane,
            IsSeries = !getIsShunt(fl, tolGroupBlkLane),
          });
        }
        FlGroupingItems.Add(REPEATED_POINT_DISTANCE);
        pipeInfoDict[fl] = REPEATED_POINT_DISTANCE;
      }
      foreach (var maxToiletToFloordrainDistance1 in allPls)
      {
        var REPEATED_POINT_DISTANCE = new DrainageGroupingPipeItem()
        {
          Label = maxToiletToFloordrainDistance1,
          PipeType = PipeType.PL,
        };
        REPEATED_POINT_DISTANCE.HasWaterPort = hasWaterPort(maxToiletToFloordrainDistance1);
        REPEATED_POINT_DISTANCE.WaterPortLabel = getWaterPortLabel(maxToiletToFloordrainDistance1);
        REPEATED_POINT_DISTANCE.HasWrappingPipe = hasOutletlWrappingPipe(maxToiletToFloordrainDistance1);
        REPEATED_POINT_DISTANCE.OutletWrappingPipeRadius = getOutletWrappingPipeRadius(maxToiletToFloordrainDistance1);
        {
          REPEATED_POINT_DISTANCE.TlLabel = maxToiletToFloordrainDistance1.Replace(THESAURUSDECLAIM, THESAURUSCONFIRM);
          REPEATED_POINT_DISTANCE.MinTl = getMinTl(REPEATED_POINT_DISTANCE.TlLabel);
          REPEATED_POINT_DISTANCE.MaxTl = getMaxTl(REPEATED_POINT_DISTANCE.TlLabel);
          REPEATED_POINT_DISTANCE.HasTL = THESAURUSOBSTINACY;
          if (REPEATED_POINT_DISTANCE.MinTl <= THESAURUSSTAMPEDE || REPEATED_POINT_DISTANCE.MaxTl <= THESAURUSHOUSING || REPEATED_POINT_DISTANCE.MinTl >= REPEATED_POINT_DISTANCE.MaxTl)
          {
            REPEATED_POINT_DISTANCE.HasTL = INTRAVASCULARLY;
            REPEATED_POINT_DISTANCE.MinTl = REPEATED_POINT_DISTANCE.MaxTl = THESAURUSSTAMPEDE;
          }
          if (REPEATED_POINT_DISTANCE.HasTL && REPEATED_POINT_DISTANCE.MaxTl == maxS)
          {
            REPEATED_POINT_DISTANCE.MoveTlLineUpper = THESAURUSOBSTINACY;
          }
        }
        REPEATED_POINT_DISTANCE.Items = new List<DrainageGroupingPipeItem.ValueItem>();
        REPEATED_POINT_DISTANCE.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
        foreach (var tolGroupBlkLane in allNumStoreyLabels)
        {
          var _hasLong = hasLong(maxToiletToFloordrainDistance1, tolGroupBlkLane);
          REPEATED_POINT_DISTANCE.Items.Add(new DrainageGroupingPipeItem.ValueItem()
          {
            Exist = testExist(maxToiletToFloordrainDistance1, tolGroupBlkLane),
            HasLong = _hasLong,
            HasShort = hasShort(maxToiletToFloordrainDistance1, tolGroupBlkLane),
          });
          REPEATED_POINT_DISTANCE.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
          {
            FloorDrainsCount = getFDCount(maxToiletToFloordrainDistance1, tolGroupBlkLane),
            Storey = tolGroupBlkLane,
          });
        }
        PlGroupingItems.Add(REPEATED_POINT_DISTANCE);
        pipeInfoDict[maxToiletToFloordrainDistance1] = REPEATED_POINT_DISTANCE;
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        REPEATED_POINT_DISTANCE.OutletWrappingPipeRadius ??= THESAURUSLEGACY;
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
        {
          var hanging = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
          if (hanging.Storey is THESAURUSREGION)
          {
            if (REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].HasShort)
            {
              var tolReturnValueMinRange = REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE];
              tolReturnValueMinRange.HasShort = INTRAVASCULARLY;
              REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE] = tolReturnValueMinRange;
            }
          }
        }
        REPEATED_POINT_DISTANCE.FloorDrainsCountAt1F = Math.Max(REPEATED_POINT_DISTANCE.FloorDrainsCountAt1F, getSingleOutletFDCount(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSREGION));
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        foreach (var hanging in REPEATED_POINT_DISTANCE.Hangings)
        {
          if (hanging.FloorDrainsCount > THESAURUSPERMUTATION)
          {
            hanging.FloorDrainsCount = THESAURUSPERMUTATION;
          }
          if (isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, hanging.Storey))
          {
            if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
            {
              hanging.HasDoubleSCurve = THESAURUSOBSTINACY;
            }
          }
          if (isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, hanging.Storey))
          {
            hanging.RoomName = THESAURUSPEDESTRIAN;
          }
          else if (isBalcony(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, hanging.Storey))
          {
            hanging.RoomName = NATIONALIZATION;
          }
          if (hanging.WashingMachineFloorDrainsCount > hanging.FloorDrainsCount)
          {
            hanging.WashingMachineFloorDrainsCount = hanging.FloorDrainsCount;
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        if (!IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) continue;
        if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)) continue;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        {
          foreach (var hanging in REPEATED_POINT_DISTANCE.Hangings)
          {
            if (isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, hanging.Storey))
            {
              hanging.HasDoubleSCurve = THESAURUSOBSTINACY;
            }
            if (hanging.Storey == THESAURUSREGION)
            {
              if (isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, hanging.Storey))
              {
                hanging.HasDoubleSCurve = INTRAVASCULARLY;
                REPEATED_POINT_DISTANCE.HasBasinInKitchenAt1F = THESAURUSOBSTINACY;
              }
            }
          }
        }
      }
      {
        foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
        {
          if (IsPL(max_rainpipe_to_balconyfloordrain.Key))
          {
            var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
            foreach (var hanging in REPEATED_POINT_DISTANCE.Hangings)
            {
              hanging.FloorDrainsCount = THESAURUSSTAMPEDE;
            }
          }
        }
      }
      {
        foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
        {
          if (!IsFL(max_rainpipe_to_balconyfloordrain.Key)) continue;
          var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
          if (IsFL0(REPEATED_POINT_DISTANCE.Label))
          {
            REPEATED_POINT_DISTANCE.IsFL0 = THESAURUSOBSTINACY;
            REPEATED_POINT_DISTANCE.HasRainPortForFL0 = getHasRainPort(REPEATED_POINT_DISTANCE.Label);
            REPEATED_POINT_DISTANCE.IsConnectedToFloorDrainForFL0 = getIsConnectedToFloorDrainForFL0(REPEATED_POINT_DISTANCE.Label);
            foreach (var hanging in REPEATED_POINT_DISTANCE.Hangings)
            {
              hanging.FloorDrainsCount = THESAURUSHOUSING;
              hanging.HasSCurve = INTRAVASCULARLY;
              hanging.HasDoubleSCurve = INTRAVASCULARLY;
              hanging.HasCleaningPort = INTRAVASCULARLY;
              if (hanging.Storey == THESAURUSREGION)
              {
                hanging.FloorDrainsCount = getSingleOutletFDCount(max_rainpipe_to_balconyfloordrain.Key, THESAURUSREGION);
              }
            }
            if (REPEATED_POINT_DISTANCE.IsConnectedToFloorDrainForFL0) REPEATED_POINT_DISTANCE.MergeFloorDrainForFL0 = getIsMerge(max_rainpipe_to_balconyfloordrain.Key);
          }
        }
      }
      {
        foreach (var REPEATED_POINT_DISTANCE in pipeInfoDict.Values)
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
          {
            if (!REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].Exist) continue;
            var hanging = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
            var tolGroupBlkLane = allNumStoreyLabels[MAX_ANGEL_TOLLERANCE];
            hanging.HasCleaningPort = IsPL(REPEATED_POINT_DISTANCE.Label) || IsDL(REPEATED_POINT_DISTANCE.Label);
            hanging.HasDownBoardLine = IsPL(REPEATED_POINT_DISTANCE.Label) || IsDL(REPEATED_POINT_DISTANCE.Label);
            {
              var tolReturnValueMinRange = REPEATED_POINT_DISTANCE.Items.TryGet(MAX_ANGEL_TOLLERANCE - THESAURUSHOUSING);
              if ((tolReturnValueMinRange.Exist && tolReturnValueMinRange.HasLong) || tolGroupBlkLane == THESAURUSREGION)
              {
                hanging.HasCheckPoint = THESAURUSOBSTINACY;
              }
            }
            if (hanging.HasCleaningPort)
            {
              hanging.HasCheckPoint = THESAURUSOBSTINACY;
            }
            if (hanging.HasDoubleSCurve)
            {
              hanging.HasCheckPoint = THESAURUSOBSTINACY;
            }
            if (hanging.WashingMachineFloorDrainsCount > THESAURUSSTAMPEDE)
            {
              hanging.HasCheckPoint = THESAURUSOBSTINACY;
            }
            if (GetStoreyScore(tolGroupBlkLane) == maxS)
            {
              hanging.HasCleaningPort = INTRAVASCULARLY;
              hanging.HasDownBoardLine = INTRAVASCULARLY;
            }
          }
        }
      }
      {
        foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
        {
          var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
          var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
          if (tolConnectSecPtRange.Any(tolGroupBlkLane => testExist(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, tolGroupBlkLane)))
          {
            REPEATED_POINT_DISTANCE.CanHaveAring = THESAURUSOBSTINACY;
          }
          if (testExist(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, maxS + THESAURUSASPIRATION))
          {
            REPEATED_POINT_DISTANCE.CanHaveAring = THESAURUSOBSTINACY;
          }
          if (IsFL0(REPEATED_POINT_DISTANCE.Label))
          {
            REPEATED_POINT_DISTANCE.CanHaveAring = INTRAVASCULARLY;
          }
        }
      }
      {
        if (tolRegroupMainYRange.Max() < THESAURUSDESTITUTE)
        {
          foreach (var REPEATED_POINT_DISTANCE in pipeInfoDict.Values)
          {
            REPEATED_POINT_DISTANCE.HasTL = INTRAVASCULARLY;
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        if (IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
        {
          foreach (var hanging in REPEATED_POINT_DISTANCE.Hangings)
          {
            if (hanging.Storey == THESAURUSREGION)
            {
              if (isToilet(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, THESAURUSREGION))
              {
                REPEATED_POINT_DISTANCE.IsSingleOutlet = THESAURUSOBSTINACY;
              }
              break;
            }
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        if (IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
        {
          for (int MAX_ANGEL_TOLLERANCE = REPEATED_POINT_DISTANCE.Items.Count - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE; --MAX_ANGEL_TOLLERANCE)
          {
            if (REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].Exist)
            {
              REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE] = default;
              break;
            }
          }
        }
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
        {
          var hanging = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
          if (hanging == null) continue;
          if (hanging.Storey == maxS + THESAURUSASPIRATION)
          {
            if (REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].HasShort || REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].HasLong)
            {
              var tolReturnValueMinRange = REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE];
              tolReturnValueMinRange.HasShort = INTRAVASCULARLY;
              tolReturnValueMinRange.HasLong = THESAURUSOBSTINACY;
              tolReturnValueMinRange.DrawLongHLineHigher = THESAURUSOBSTINACY;
              REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE] = tolReturnValueMinRange;
              hanging.HasDownBoardLine = INTRAVASCULARLY;
            }
            break;
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        if (IsPL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var h1 = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
            h1.HasCleaningPort = isToilet(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h1.Storey);
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        if (IsFL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) && !IsFL0(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
          {
            var h1 = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
            var h2 = REPEATED_POINT_DISTANCE.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
            if (REPEATED_POINT_DISTANCE.Items[MAX_ANGEL_TOLLERANCE].HasLong && REPEATED_POINT_DISTANCE.Items.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING).Exist && h2 != null)
            {
              h1.FlFixType = FixingLogic1.GetFlFixType(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h1.Storey, isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h2.Storey), getCirclesCount(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h1.Storey));
              h2.FlCaseEnum = FixingLogic1.GetFlCase(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h1.Storey, isKitchen(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h2.Storey), getCirclesCount(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, h1.Storey));
            }
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < REPEATED_POINT_DISTANCE.Hangings.Count; MAX_ANGEL_TOLLERANCE++)
        {
          var h1 = REPEATED_POINT_DISTANCE.Hangings[MAX_ANGEL_TOLLERANCE];
          var h2 = REPEATED_POINT_DISTANCE.Hangings.TryGet(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
          if (h2 == null) continue;
          if (!h2.HasCleaningPort)
          {
            h1.HasDownBoardLine = INTRAVASCULARLY;
          }
        }
      }
      foreach (var max_rainpipe_to_balconyfloordrain in pipeInfoDict)
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = max_rainpipe_to_balconyfloordrain.Key;
        var REPEATED_POINT_DISTANCE = max_rainpipe_to_balconyfloordrain.Value;
        if (!getCanHaveDownboard())
        {
          foreach (var h in REPEATED_POINT_DISTANCE.Hangings)
          {
            h.HasDownBoardLine = INTRAVASCULARLY;
          }
        }
      }
      var pipeGroupedItems = new List<DrainageGroupedPipeItem>();
      var TolLaneProtect = pipeInfoDict.Values.GroupBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
      foreach (var default_fire_valve_width in TolLaneProtect)
      {
        if (!IsFL(default_fire_valve_width.Key.Label)) continue;
        var outlets = default_fire_valve_width.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.HasWaterPort).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.WaterPortLabel).Distinct().ToList();
        var max_kitchen_to_balcony_distance = default_fire_valve_width.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Label).Distinct().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
        var REPEATED_POINT_DISTANCE = new DrainageGroupedPipeItem()
        {
          Labels = max_kitchen_to_balcony_distance,
          HasWrappingPipe = default_fire_valve_width.Key.HasWrappingPipe,
          WaterPortLabels = outlets,
          Items = default_fire_valve_width.Key.Items.ToList(),
          PipeType = PipeType.FL,
          Hangings = default_fire_valve_width.Key.Hangings.ToList(),
          HasBasinInKitchenAt1F = default_fire_valve_width.Key.HasBasinInKitchenAt1F,
          FloorDrainsCountAt1F = default_fire_valve_width.Key.FloorDrainsCountAt1F,
          CanHaveAring = default_fire_valve_width.Key.CanHaveAring,
          IsFL0 = default_fire_valve_width.Key.IsFL0,
          HasRainPortForFL0 = default_fire_valve_width.Key.HasRainPortForFL0,
          IsConnectedToFloorDrainForFL0 = default_fire_valve_width.Key.IsConnectedToFloorDrainForFL0,
          MergeFloorDrainForFL0 = default_fire_valve_width.Key.MergeFloorDrainForFL0,
          OutletWrappingPipeRadius = default_fire_valve_width.Key.OutletWrappingPipeRadius,
        };
        pipeGroupedItems.Add(REPEATED_POINT_DISTANCE);
      }
      foreach (var default_fire_valve_width in TolLaneProtect)
      {
        if (!IsPL(default_fire_valve_width.Key.Label)) continue;
        var outlets = default_fire_valve_width.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.HasWaterPort).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.WaterPortLabel).Distinct().ToList();
        var max_kitchen_to_balcony_distance = default_fire_valve_width.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Label).Distinct().OrderBy(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).ToList();
        var REPEATED_POINT_DISTANCE = new DrainageGroupedPipeItem()
        {
          Labels = max_kitchen_to_balcony_distance,
          HasWrappingPipe = default_fire_valve_width.Key.HasWrappingPipe,
          WaterPortLabels = outlets,
          Items = default_fire_valve_width.Key.Items.ToList(),
          PipeType = PipeType.PL,
          MinTl = default_fire_valve_width.Key.MinTl,
          MaxTl = default_fire_valve_width.Key.MaxTl,
          HasTl = default_fire_valve_width.Key.HasTL,
          TlLabels = default_fire_valve_width.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.TlLabel).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax != null).ToList(),
          Hangings = default_fire_valve_width.Key.Hangings.ToList(),
          IsSingleOutlet = default_fire_valve_width.Key.IsSingleOutlet,
          CanHaveAring = default_fire_valve_width.Key.CanHaveAring,
          IsFL0 = default_fire_valve_width.Key.IsFL0,
          MoveTlLineUpper = default_fire_valve_width.Key.MoveTlLineUpper,
          OutletWrappingPipeRadius = default_fire_valve_width.Key.OutletWrappingPipeRadius,
        };
        pipeGroupedItems.Add(REPEATED_POINT_DISTANCE);
      }
      pipeGroupedItems = pipeGroupedItems.OrderBy(TolLightRangeSingleSideMax =>
      {
        var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = TolLightRangeSingleSideMax.Labels.FirstOrDefault();
        if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE is null) return THESAURUSSTAMPEDE;
        if (IsPL((MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))) return THESAURUSHOUSING;
        if (IsFL0((MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))) return THESAURUSPERMUTATION;
        if (IsFL((MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))) return INTROPUNITIVENESS;
        return int.MaxValue;
      }).ThenBy(TolLightRangeSingleSideMax =>
      {
        return TolLightRangeSingleSideMax.Labels.FirstOrDefault();
      }).ToList();
      return pipeGroupedItems;
    }
    public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
    {
      var h = HEIGHT * THESAURUSDISPASSIONATE;
      if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
      {
        h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSSURPRISED;
      }
      var _tolRegroupMainYRange = basePt.OffsetY(h);
      var _tolConnectSecPtRange = _tolRegroupMainYRange.OffsetX(-MISAPPREHENSIVE);
      var _tolConnectSecPrimAddValue = _tolRegroupMainYRange.OffsetX(MISAPPREHENSIVE);
      var TolUniformSideLenth = DrawLineLazy(_tolConnectSecPtRange, _tolConnectSecPrimAddValue);
      TolUniformSideLenth.Layer = THESAURUSSTRIPED;
      ByLayer(TolUniformSideLenth);
    }
    public static void DrawPipeButtomHeightSymbol(Point2d _raiseDistanceToStartDefault, List<GLineSegment> TolLane)
    {
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane);
      Dr.SetLabelStylesForDraiNote(tolReturnValueRange.ToArray());
      DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: _raiseDistanceToStartDefault.ToPoint3d(),
props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, QUOTATIONSTRETTO } },
cb: tolReturnValueRangeTo =>
{
  tolReturnValueRangeTo.Layer = THESAURUSSTRIPED;
});
    }
    public static void DrawPipeButtomHeightSymbol(double w, double h, Point2d _raiseDistanceToStartDefault)
    {
      var LineCoincideTolerance = new List<Vector2d> { new Vector2d(w, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, h) };
      var TolLane = LineCoincideTolerance.ToGLineSegments(_raiseDistanceToStartDefault);
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane);
      Dr.SetLabelStylesForDraiNote(tolReturnValueRange.ToArray());
      DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: TolLane.Last().EndPoint.OffsetX(PHYSIOLOGICALLY).ToPoint3d(),
props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, QUOTATIONSTRETTO } },
cb: tolReturnValueRangeTo =>
{
  tolReturnValueRangeTo.Layer = THESAURUSSTRIPED;
});
    }
    public static void textHeight(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, Point2d basePt, double WELL_TO_WALL_OFFSET, string repeated_point_distance)
    {
      textHeight(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, basePt.ToPoint3d(), WELL_TO_WALL_OFFSET, repeated_point_distance);
    }
    public static void textHeight(string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, Point3d basePt, double WELL_TO_WALL_OFFSET, string repeated_point_distance)
    {
      {
        var TolUniformSideLenth = DrawLineLazy(basePt.X, basePt.Y, basePt.X + WELL_TO_WALL_OFFSET, basePt.Y);
        var maxBalconyToDeviceplatformDistance = DrawTextLazy(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
        Dr.SetLabelStylesForWNote(TolUniformSideLenth, maxBalconyToDeviceplatformDistance);
        DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.OffsetX(QUOTATIONPITUITARY), layer: COSTERMONGERING, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, repeated_point_distance } });
      }
      if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE == THESAURUSARGUMENTATIVE)
      {
        var TolUniformSideLenth = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, THESAURUSSTAMPEDE), new Point3d(basePt.X + WELL_TO_WALL_OFFSET, basePt.Y + ThWSDStorey.RF_OFFSET_Y, THESAURUSSTAMPEDE));
        var maxBalconyToDeviceplatformDistance = DrawTextLazy(THESAURUSSHADOWY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
        Dr.SetLabelStylesForWNote(TolUniformSideLenth, maxBalconyToDeviceplatformDistance);
      }
    }
    public static void SortStoreys(List<string> KitchenBufferDistance)
    {
      KitchenBufferDistance.Sort((TolLightRangeSingleSideMax, y) => GetStoreyScore(TolLightRangeSingleSideMax) - GetStoreyScore(y));
    }
    public static void SetLabelStylesForDraiNote(params Entity[] MAX_TAG_YPOSITION)
    {
      foreach (var tolReturnValue0Approx in MAX_TAG_YPOSITION)
      {
        tolReturnValue0Approx.Layer = THESAURUSSTRIPED;
        ByLayer(tolReturnValue0Approx);
        if (tolReturnValue0Approx is DBText TolLightRangeMin)
        {
          TolLightRangeMin.WidthFactor = THESAURUSDISPASSIONATE;
          SetTextStyleLazy(TolLightRangeMin, CONTROVERSIALLY);
        }
      }
    }
    public static void DrawDomePipes(params GLineSegment[] TolLane)
    {
      DrawDomePipes((IEnumerable<GLineSegment>)TolLane);
    }
    public static void DrawDomePipes(IEnumerable<GLineSegment> TolLane)
    {
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
      tolReturnValueRange.ForEach(TolUniformSideLenth => SetDomePipeLineStyle(TolUniformSideLenth));
    }
    public static void SetDomePipeLineStyle(Line TolUniformSideLenth)
    {
      TolUniformSideLenth.Layer = THESAURUSCONTROVERSY;
      ByLayer(TolUniformSideLenth);
    }
    public static void DrawBluePipes(IEnumerable<GLineSegment> TolLane)
    {
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
      tolReturnValueRange.ForEach(TolUniformSideLenth =>
      {
        TolUniformSideLenth.Layer = INSTRUMENTALITY;
        ByLayer(TolUniformSideLenth);
      });
    }
    public static void DrawDraiNoteLines(IEnumerable<GLineSegment> TolLane)
    {
      var tolReturnValueRange = DrawLineSegmentsLazy(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid));
      tolReturnValueRange.ForEach(TolUniformSideLenth =>
      {
        TolUniformSideLenth.Layer = THESAURUSSTRIPED;
        ByLayer(TolUniformSideLenth);
      });
    }
    public static void DrawNoteText(string repeated_point_distance, Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)
    {
      if (string.IsNullOrWhiteSpace(repeated_point_distance)) return;
      var TolLightRangeMin = DrawTextLazy(repeated_point_distance, THESAURUSENDANGER, MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
      SetLabelStylesForDraiNote(TolLightRangeMin);
    }
    public static void DrawSCurve(Vector2d vec7, Point2d _tolRegroupMainYRange, bool leftOrRight)
    {
      var _tolConnectSecPtRange = _tolRegroupMainYRange + vec7;
      DrawDomePipes(new GLineSegment(_tolRegroupMainYRange, _tolConnectSecPtRange));
      if (!Testing) DrawSWaterStoringCurve(_tolConnectSecPtRange.ToPoint3d(), leftOrRight);
    }
    public static void DrawDSCurve(Point2d _tolConnectSecPtRange, bool leftOrRight, string value)
    {
      if (!Testing) DrawDoubleWashBasins(_tolConnectSecPtRange.ToPoint3d(), leftOrRight, value);
    }
    public static bool Testing;
    public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight, string value)
    {
      if (leftOrRight)
      {
        if (value is THESAURUSDISCIPLINARIAN)
        {
          basePt += new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC).ToVector3d();
        }
        DrawBlockReference(UNACCEPTABILITY, basePt,
          tolReturnValueRangeTo =>
          {
            tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
            tolReturnValueRangeTo.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
            if (tolReturnValueRangeTo.IsDynamicBlock)
            {
              tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
            }
          });
      }
      else
      {
        DrawBlockReference(UNACCEPTABILITY, basePt,
          tolReturnValueRangeTo =>
          {
            tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
            tolReturnValueRangeTo.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
            if (tolReturnValueRangeTo.IsDynamicBlock)
            {
              tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
            }
          });
      }
    }
    public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = ACCOMMODATINGLY)
    {
      if (Testing) return;
      if (leftOrRight)
      {
        DrawBlockReference(PERSUADABLENESS, basePt, tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          ByLayer(tolReturnValueRangeTo);
          tolReturnValueRangeTo.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
          if (tolReturnValueRangeTo.IsDynamicBlock)
          {
            tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
          }
        });
      }
      else
      {
        DrawBlockReference(PERSUADABLENESS, basePt,
       tolReturnValueRangeTo =>
       {
         tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
         ByLayer(tolReturnValueRangeTo);
         tolReturnValueRangeTo.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
         if (tolReturnValueRangeTo.IsDynamicBlock)
         {
           tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
         }
       });
      }
    }
    public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
    {
      if (leftOrRight)
      {
        DrawBlockReference(CARCINOGENICITY, basePt, tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          ByLayer(tolReturnValueRangeTo);
          tolReturnValueRangeTo.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
          if (tolReturnValueRangeTo.IsDynamicBlock)
          {
            tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, FISSIPAROUSNESS);
            tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSCASCADE, (short)THESAURUSHOUSING);
          }
        });
      }
      else
      {
        DrawBlockReference(CARCINOGENICITY, basePt,
           tolReturnValueRangeTo =>
           {
             tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
             ByLayer(tolReturnValueRangeTo);
             tolReturnValueRangeTo.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
             if (tolReturnValueRangeTo.IsDynamicBlock)
             {
               tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, FISSIPAROUSNESS);
               tolReturnValueRangeTo.ObjectId.SetDynBlockValue(THESAURUSCASCADE, (short)THESAURUSHOUSING);
             }
           });
      }
    }
    public static AlignedDimension DrawDimLabel(Point2d pt1, Point2d _tol_lane, Vector2d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, string repeated_point_distance, string layer)
    {
      var dim = new AlignedDimension();
      dim.XLine1Point = pt1.ToPoint3d();
      dim.XLine2Point = _tol_lane.ToPoint3d();
      dim.DimLinePoint = (GeoAlgorithm.MidPoint(pt1, _tol_lane) + MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN).ToPoint3d();
      dim.DimensionText = repeated_point_distance;
      dim.Layer = layer;
      ByLayer(dim);
      DrawEntityLazy(dim);
      return dim;
    }
    public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
    {
      if (leftOrRight)
      {
        DrawBlockReference(THESAURUSDENOUNCE, basePt, scale: scale, cb: tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          ByLayer(tolReturnValueRangeTo);
          tolReturnValueRangeTo.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSQUAGMIRE);
        });
      }
      else
      {
        DrawBlockReference(THESAURUSDENOUNCE, basePt, scale: scale, cb: tolReturnValueRangeTo =>
        {
          tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
          ByLayer(tolReturnValueRangeTo);
          tolReturnValueRangeTo.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSQUAGMIRE + THESAURUSCAVERN);
        });
      }
    }
    public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
    {
      DrawBlockReference(blkName: THESAURUSAGILITY, basePt: basePt,
cb: tolReturnValueRangeTo =>
{
  if (leftOrRight)
  {
    tolReturnValueRangeTo.ScaleFactors = new Scale3d(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING);
  }
  ByLayer(tolReturnValueRangeTo);
  tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
});
    }
    public static void DrawDiryWaterWells2(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, List<string> values)
    {
      var MAX_DEVICE_TO_DEVICE = THESAURUSSTAMPEDE;
      foreach (var value in values)
      {
        DrawDirtyWaterWell(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(THESAURUSDOMESTIC) + new Vector2d(MAX_DEVICE_TO_DEVICE, THESAURUSSTAMPEDE), value);
        MAX_DEVICE_TO_DEVICE += THESAURUSDISAGREEABLE;
      }
    }
    public static void DrawRainWaterWell(Point3d basePt, string value)
    {
      DrawBlockReference(blkName: THESAURUSGAUCHE, basePt: basePt.OffsetY(-THESAURUSDOMESTIC),
    props: new Dictionary<string, string>() { { THESAURUSSPECIFICATION, value } },
    cb: tolReturnValueRangeTo =>
    {
      tolReturnValueRangeTo.Layer = DENDROCHRONOLOGIST;
      ByLayer(tolReturnValueRangeTo);
    });
    }
    public static void DrawRainWaterWell(Point2d basePt, string value)
    {
      DrawRainWaterWell(basePt.ToPoint3d(), value);
    }
    public static void DrawDirtyWaterWell(Point2d basePt, string value)
    {
      DrawDirtyWaterWell(basePt.ToPoint3d(), value);
    }
    public static void DrawDirtyWaterWell(Point3d basePt, string value)
    {
      DrawBlockReference(blkName: THESAURUSLANDMARK, basePt: basePt.OffsetY(-THESAURUSDOMESTIC),
      props: new Dictionary<string, string>() { { THESAURUSSPECIFICATION, value } },
      cb: tolReturnValueRangeTo =>
      {
        tolReturnValueRangeTo.Layer = THESAURUSJUBILEE;
        ByLayer(tolReturnValueRangeTo);
      });
    }
    public static void DrawDiryWaterWells1(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, List<string> values, bool isRainWaterWell = INTRAVASCULARLY)
    {
      if (values == null) return;
      if (values.Count == THESAURUSHOUSING)
      {
        var MAX_DEVICE_TO_BALCONY = -THESAURUSCOORDINATE;
        if (!isRainWaterWell)
        {
          DrawDirtyWaterWell(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(MAX_DEVICE_TO_BALCONY), values[THESAURUSSTAMPEDE]);
        }
        else
        {
          DrawRainWaterWell(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetY(MAX_DEVICE_TO_BALCONY), values[THESAURUSSTAMPEDE]);
        }
      }
      else if (values.Count >= THESAURUSPERMUTATION)
      {
        var _tol_avg_column_dist = GetBasePoints(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.OffsetX(-THESAURUSDISAGREEABLE), THESAURUSPERMUTATION, values.Count, THESAURUSDISAGREEABLE, THESAURUSDISAGREEABLE).ToList();
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < values.Count; MAX_ANGEL_TOLLERANCE++)
        {
          if (!isRainWaterWell)
          {
            DrawDirtyWaterWell(_tol_avg_column_dist[MAX_ANGEL_TOLLERANCE], values[MAX_ANGEL_TOLLERANCE]);
          }
          else
          {
            DrawRainWaterWell(_tol_avg_column_dist[MAX_ANGEL_TOLLERANCE], values[MAX_ANGEL_TOLLERANCE]);
          }
        }
      }
    }
    public static IEnumerable<Point2d> GetBasePoints(Point2d SIDEWATERBUCKET_X_INDENT, int maxCol, int num, double BranchPortToMainDistance, double line_coincide_tolerance)
    {
      int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE, MAX_ANGLE_TOLLERANCE = THESAURUSSTAMPEDE;
      for (int RADIAN_TOLERANCE = THESAURUSSTAMPEDE; RADIAN_TOLERANCE < num; RADIAN_TOLERANCE++)
      {
        yield return new Point2d(SIDEWATERBUCKET_X_INDENT.X + MAX_ANGEL_TOLLERANCE * BranchPortToMainDistance, SIDEWATERBUCKET_X_INDENT.Y - MAX_ANGLE_TOLLERANCE * line_coincide_tolerance);
        MAX_ANGEL_TOLLERANCE++;
        if (MAX_ANGEL_TOLLERANCE >= maxCol)
        {
          MAX_ANGLE_TOLLERANCE++;
          MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE;
        }
      }
    }
    public static IEnumerable<Point3d> GetBasePoints(Point3d SIDEWATERBUCKET_X_INDENT, int maxCol, int num, double BranchPortToMainDistance, double line_coincide_tolerance)
    {
      int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE, MAX_ANGLE_TOLLERANCE = THESAURUSSTAMPEDE;
      for (int RADIAN_TOLERANCE = THESAURUSSTAMPEDE; RADIAN_TOLERANCE < num; RADIAN_TOLERANCE++)
      {
        yield return new Point3d(SIDEWATERBUCKET_X_INDENT.X + MAX_ANGEL_TOLLERANCE * BranchPortToMainDistance, SIDEWATERBUCKET_X_INDENT.Y - MAX_ANGLE_TOLLERANCE * line_coincide_tolerance, THESAURUSSTAMPEDE);
        MAX_ANGEL_TOLLERANCE++;
        if (MAX_ANGEL_TOLLERANCE >= maxCol)
        {
          MAX_ANGLE_TOLLERANCE++;
          MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE;
        }
      }
    }
  }
  public class ThDrainageService
  {
    public static void PreFixGeoData(DrainageGeoData geoData)
    {
      foreach (var ct in geoData.Labels)
      {
        ct.Text = FixVerticalPipeLabel(ct.Text);
        ct.Boundary = ct.Boundary.Expand(-DISPENSABLENESS);
      }
      geoData.FixData();
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < geoData.LabelLines.Count; MAX_ANGEL_TOLLERANCE++)
      {
        var default_fire_valve_length = geoData.LabelLines[MAX_ANGEL_TOLLERANCE];
        if (default_fire_valve_length.IsHorizontal(THESAURUSCOMMUNICATION))
        {
          geoData.LabelLines[MAX_ANGEL_TOLLERANCE] = default_fire_valve_length.Extend(SUPERLATIVENESS);
        }
        else if (default_fire_valve_length.IsVertical(THESAURUSCOMMUNICATION))
        {
          geoData.LabelLines[MAX_ANGEL_TOLLERANCE] = default_fire_valve_length.Extend(THESAURUSHOUSING);
        }
      }
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < geoData.DLines.Count; MAX_ANGEL_TOLLERANCE++)
      {
        geoData.DLines[MAX_ANGEL_TOLLERANCE] = geoData.DLines[MAX_ANGEL_TOLLERANCE].Extend(THESAURUSCOMMUNICATION);
      }
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < geoData.VLines.Count; MAX_ANGEL_TOLLERANCE++)
      {
        geoData.VLines[MAX_ANGEL_TOLLERANCE] = geoData.VLines[MAX_ANGEL_TOLLERANCE].Extend(THESAURUSCOMMUNICATION);
      }
      {
        geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSACRIMONIOUS)).ToList();
      }
      {
        geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).Cast<Geometry>().ToList()).Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometryEx(TolLightRangeSingleSideMax).Envelope.ToGRect()).ToList();
        geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).Cast<Geometry>().ToList()).Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometryEx(TolLightRangeSingleSideMax).Envelope.ToGRect()).ToList();
        geoData.FloorDrains = geoData.FloorDrains.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Width < THESAURUSFORMULATE && TolLightRangeSingleSideMax.Height < THESAURUSFORMULATE).ToList();
      }
      {
        geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((TolLightRangeSingleSideMax, y) => TolLightRangeSingleSideMax.Text == y.Text && TolLightRangeSingleSideMax.Boundary.EqualsTo(y.Boundary, THESAURUSACRIMONIOUS))).ToList();
      }
      {
        var cmp = new GRect.EqualityComparer(ASSOCIATIONISTS);
        geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
        geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
      }
      {
        var okPts = new HashSet<Point2d>(geoData.WrappingPipeRadius.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key));
        var lbs = geoData.WrappingPipeLabels.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList();
        var lbsf = GeoFac.CreateIntersectsSelector(lbs);
        var tolReturnValueRange = geoData.WrappingPipeLabelLines.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
        var TolLaneProtect = GeoFac.GroupLinesByConnPoints(tolReturnValueRange, DINOFLAGELLATES);
        foreach (var DEFAULT_FIRE_VALVE_LENGTH in TolLaneProtect)
        {
          var TolLane = GeoFac.GetLines(DEFAULT_FIRE_VALVE_LENGTH).ToList();
          var buf = TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSCOMMUNICATION)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(HYPERDISYLLABLE)).FirstOrDefault();
          if (buf == null) continue;
          var _tol_avg_column_dist = GeoFac.GetLabelLineEndPoints(TolLane, Geometry.DefaultFactory.CreatePolygon());
          foreach (var SidewaterbucketYIndent in lbsf(buf))
          {
            var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = TryParseWrappingPipeRadiusText(SidewaterbucketYIndent.UserData as string);
            if (!string.IsNullOrWhiteSpace(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
            {
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
              {
                if (okPts.Contains(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN)) continue;
                geoData.WrappingPipeRadius.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE));
                okPts.Add(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
              }
            }
          }
        }
      }
      {
        var kvs = new HashSet<KeyValuePair<Point2d, string>>(geoData.WrappingPipeRadius);
        var okKvs = new HashSet<KeyValuePair<Point2d, string>>();
        geoData.WrappingPipeRadius.Clear();
        foreach (var wp in geoData.WrappingPipes)
        {
          var default_voltage = wp.ToPolygon().ToIPreparedGeometry();
          var _kvs = kvs.Except(okKvs).Where(TolLightRangeSingleSideMax => default_voltage.Intersects(TolLightRangeSingleSideMax.Key.ToNTSPoint())).ToList();
          var strs = _kvs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
          var nums = strs.Select(TolLightRangeSingleSideMax => double.TryParse(TolLightRangeSingleSideMax, out double MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN) ? MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN : double.NaN).Where(TolLightRangeSingleSideMax => !double.IsNaN(TolLightRangeSingleSideMax)).ToList();
          if (nums.Count > THESAURUSHOUSING)
          {
            var _tolReturnValueMaxDistance = nums.Min();
            var WELLS_MAX_AREA = strs.First(TolLightRangeSingleSideMax => double.Parse(TolLightRangeSingleSideMax) == _tolReturnValueMaxDistance);
            foreach (var max_rainpipe_to_balconyfloordrain in _kvs)
            {
              kvs.Remove(max_rainpipe_to_balconyfloordrain);
            }
            foreach (var max_rainpipe_to_balconyfloordrain in _kvs)
            {
              if (max_rainpipe_to_balconyfloordrain.Value == WELLS_MAX_AREA)
              {
                kvs.Add(max_rainpipe_to_balconyfloordrain);
                okKvs.Add(max_rainpipe_to_balconyfloordrain);
                break;
              }
            }
          }
        }
        geoData.WrappingPipeRadius.AddRange(kvs);
      }
      {
        var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = THESAURUSENTREPRENEUR;
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < geoData.WrappingPipes.Count; MAX_ANGEL_TOLLERANCE++)
        {
          var wp = geoData.WrappingPipes[MAX_ANGEL_TOLLERANCE];
          if (wp.Width > MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN * THESAURUSPERMUTATION)
          {
            geoData.WrappingPipes[MAX_ANGEL_TOLLERANCE] = wp.Expand(-MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
          }
        }
      }
      {
        var _pipes = geoData.VerticalPipes.Distinct().ToList();
        var pipes = _pipes.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()).ToList();
        var defaultVoltage = new object();
        var sidewaterbucket_x_indent = GeoFac.CreateIntersectsSelector(pipes);
        foreach (var _killer in geoData.PipeKillers)
        {
          foreach (var pipe in sidewaterbucket_x_indent(_killer.ToPolygon()))
          {
            pipe.UserData = defaultVoltage;
          }
        }
        geoData.VerticalPipes = pipes.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData == null).Select(pipes).ToList(_pipes);
      }
    }
    public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
    {
      static bool maxDeviceplatformArea(string MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN)
      {
        if (MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN == null) return INTRAVASCULARLY;
        if (IsMaybeLabelText(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN)) return THESAURUSOBSTINACY;
        return INTRAVASCULARLY;
      }
      var tolReturnValueRange = geoData.LabelLines.Distinct().ToList();
      var bds = geoData.Labels.Where(TolLightRangeSingleSideMax => maxDeviceplatformArea(TolLightRangeSingleSideMax.Text)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary).ToList();
      var lineHs = tolReturnValueRange.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSACRIMONIOUS)).ToList();
      var lineHGs = lineHs.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
      var _tol_brake_wall = GeoFac.CreateContainsSelector(lineHGs);
      foreach (var maxKitchenToBalconyDistance in bds)
      {
        var default_fire_valve_width = GRect.Create(maxKitchenToBalconyDistance.Center.OffsetY(-THESAURUSACRIMONIOUS).OffsetY(-PHYSIOLOGICALLY), THESAURUSDICTATORIAL, PHYSIOLOGICALLY);
        var _lineHGs = _tol_brake_wall(default_fire_valve_width.ToPolygon());
        var DEFAULT_FIRE_VALVE_LENGTH = GeoFac.NearestNeighbourGeometryF(_lineHGs)(maxKitchenToBalconyDistance.Center.ToNTSPoint());
        if (DEFAULT_FIRE_VALVE_LENGTH == null) continue;
        {
          var TolUniformSideLenth = lineHs[lineHGs.IndexOf(DEFAULT_FIRE_VALVE_LENGTH)];
          var BlockScaleNum = TolUniformSideLenth.Center.GetDistanceTo(maxKitchenToBalconyDistance.Center);
          if (BlockScaleNum.InRange(HYPERDISYLLABLE, THESAURUSDOMESTIC) || Math.Abs(TolUniformSideLenth.Center.Y - maxKitchenToBalconyDistance.Center.Y).InRange(ASSOCIATIONISTS, THESAURUSDOMESTIC))
          {
            geoData.LabelLines.Add(new GLineSegment(maxKitchenToBalconyDistance.Center, TolUniformSideLenth.Center).Extend(ASSOCIATIONISTS));
          }
        }
      }
    }
  }
  public static class GeometryExtensions
  {
    public static Geometry Clone(this Geometry DEFAULT_FIRE_VALVE_LENGTH)
    {
      if (DEFAULT_FIRE_VALVE_LENGTH is null) return null;
      if (DEFAULT_FIRE_VALVE_LENGTH is Point MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN) return Clone(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN);
      if (DEFAULT_FIRE_VALVE_LENGTH is LineString defaultFireValveWidth) return Clone(defaultFireValveWidth);
      if (DEFAULT_FIRE_VALVE_LENGTH is Polygon maxToiletToFloordrainDistance1) return Clone(maxToiletToFloordrainDistance1);
      if (DEFAULT_FIRE_VALVE_LENGTH is MultiPoint mpt) return new MultiPoint(mpt.Geometries.Cast<Point>().Select(Clone).ToArray());
      if (DEFAULT_FIRE_VALVE_LENGTH is MultiLineString mls) return new MultiLineString(mls.Geometries.Cast<LineString>().Select(Clone).ToArray());
      if (DEFAULT_FIRE_VALVE_LENGTH is MultiPolygon mpl) return new MultiPolygon(mpl.Geometries.Cast<Polygon>().Select(Clone).ToArray());
      throw new NotSupportedException();
    }
    public static Coordinate Clone(Coordinate o) => new(o.X, o.Y);
    public static Point Clone(Point o) => new(o.X, o.Y);
    public static LineString Clone(LineString o) => new(o.Coordinates);
    public static Polygon Clone(Polygon o) => new(o.Shell);
    public static IEnumerable<Point> Clone(this IEnumerable<Point> DEFAULT_VOLTAGE) => DEFAULT_VOLTAGE.Select(Clone);
    public static IEnumerable<LineString> Clone(this IEnumerable<LineString> DEFAULT_VOLTAGE) => DEFAULT_VOLTAGE.Select(Clone);
    public static IEnumerable<Polygon> Clone(this IEnumerable<Polygon> DEFAULT_VOLTAGE) => DEFAULT_VOLTAGE.Select(Clone);
    public static IEnumerable<Geometry> ToBaseGeometries(this Geometry geometry)
    {
      if (geometry is Point or LineString or Polygon)
      {
        yield return geometry;
      }
      else if (geometry is GeometryCollection colle)
      {
        foreach (var DEFAULT_FIRE_VALVE_LENGTH in colle.Geometries)
        {
          foreach (var DEFAULT_FIRE_VALVE_WIDTH in ToBaseGeometries(DEFAULT_FIRE_VALVE_LENGTH))
          {
            yield return DEFAULT_FIRE_VALVE_WIDTH;
          }
        }
      }
    }
  }
  public class ThDrainageSystemServiceGeoCollector3
  {
    public AcadDatabase adb;
    public DrainageGeoData geoData;
    List<Polygon> roomPolygons;
    List<CText> roomNames;
    List<KeyValuePair<string, Geometry>> roomData => geoData.RoomData;
    List<GLineSegment> labelLines => geoData.LabelLines;
    List<CText> cts => geoData.Labels;
    List<GLineSegment> dlines => geoData.DLines;
    List<GLineSegment> vlines => geoData.VLines;
    List<GLineSegment> wlines => geoData.WLines;
    List<GRect> pipes => geoData.VerticalPipes;
    List<GRect> downwaterPorts => geoData.DownWaterPorts;
    List<GRect> wrappingPipes => geoData.WrappingPipes;
    List<GRect> floorDrains => geoData.FloorDrains;
    List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
    List<GRect> waterPorts => geoData.WaterPorts;
    List<GRect> waterWells => geoData.WaterWells;
    List<string> waterPortLabels => geoData.WaterPortLabels;
    List<GRect> KitchenBufferDistance => geoData.Storeys;
    List<GRect> washingMachines => geoData.WashingMachines;
    List<GRect> mopPools => geoData.MopPools;
    List<GRect> basins => geoData.Basins;
    List<GRect> pipeKillers => geoData.PipeKillers;
    List<GRect> rainPortSymbols => geoData.RainPortSymbols;
    List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
    public void CollectStoreys(CommandContext ctx)
    {
      KitchenBufferDistance.AddRange(ctx.StoreyContext.StoreyInfos.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary));
    }
    public void CollectStoreys(Point3dCollection range)
    {
      var DEFAULT_FIRE_VALVE_LENGTH = range?.ToGRect().ToPolygon();
      foreach (var tolReturnValueRangeTo in GetStoreyBlockReferences(adb))
      {
        var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect();
        if (DEFAULT_FIRE_VALVE_LENGTH != null)
        {
          if (!DEFAULT_FIRE_VALVE_LENGTH.Contains(maxKitchenToBalconyDistance.ToPolygon()))
          {
            continue;
          }
        }
        KitchenBufferDistance.Add(maxKitchenToBalconyDistance);
      }
    }
    public static IEnumerable<Entity> EnumerateVisibleEntites(AcadDatabase adb, BlockReference tolReturnValueRangeTo)
    {
      var q = tolReturnValueRangeTo.ExplodeToDBObjectCollection().OfType<Entity>().Where(tolReturnValue0Approx => tolReturnValue0Approx.Visible && tolReturnValue0Approx.Bounds.HasValue)
          .Where(tolReturnValue0Approx =>
          {
            if (tolReturnValue0Approx.LayerId.IsNull) return INTRAVASCULARLY;
            var layer = adb.Element<LayerTableRecord>(tolReturnValue0Approx.LayerId);
            return !layer.IsFrozen && !layer.IsHidden && !layer.IsOff;
          });
      var xclip = tolReturnValueRangeTo.XClipInfo();
      if (xclip.IsValid)
      {
        var default_voltage = xclip.PreparedPolygon;
        return q.Where(tolReturnValue0Approx => default_voltage.Intersects(tolReturnValue0Approx.Bounds.ToGRect().ToPolygon()));
      }
      else
      {
        return q;
      }
    }
    const int distinguishDiameter = VÖLKERWANDERUNG;
    public static string GetEffectiveLayer(string TOILET_BUFFER_DISTANCE)
    {
      return GetEffectiveName(TOILET_BUFFER_DISTANCE);
    }
    public static string GetEffectiveName(string WELLS_MAX_AREA)
    {
      WELLS_MAX_AREA ??= THESAURUSDEPLORE;
      var MAX_ANGEL_TOLLERANCE = WELLS_MAX_AREA.LastIndexOf(THESAURUSCONTEND);
      if (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE && !WELLS_MAX_AREA.EndsWith(MULTIPROCESSING))
      {
        WELLS_MAX_AREA = WELLS_MAX_AREA.Substring(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
      }
      MAX_ANGEL_TOLLERANCE = WELLS_MAX_AREA.LastIndexOf(SUPERREGENERATIVE);
      if (MAX_ANGEL_TOLLERANCE >= THESAURUSSTAMPEDE && !WELLS_MAX_AREA.EndsWith(THESAURUSCOURIER))
      {
        WELLS_MAX_AREA = WELLS_MAX_AREA.Substring(MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING);
      }
      return WELLS_MAX_AREA;
    }
    public static string GetEffectiveBRName(string MIN_WELL_TO_URINAL_DISTANCE)
    {
      return GetEffectiveName(MIN_WELL_TO_URINAL_DISTANCE);
    }
    static bool isDrainageLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSREMNANT); 
    HashSet<Handle> ok_group_handles;
    private void handleEntity(Entity maxToiletToCondensepipeDistance, Matrix3d MAX_RAINPIPE_TO_BALCONYFLOORDRAIN, List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance)
    {
      if (!IsLayerVisible(maxToiletToCondensepipeDistance)) return;
      if (MAX_ROOM_INTERVAL)
      {
        return;
      }
      var maxToiletToFloordrainDistance2 = maxToiletToCondensepipeDistance.GetRXClass().DxfName.ToUpper();
      var TOILET_BUFFER_DISTANCE = maxToiletToCondensepipeDistance.Layer;
      TOILET_BUFFER_DISTANCE = GetEffectiveLayer(TOILET_BUFFER_DISTANCE);
      static bool isDLineLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && !layer.Contains(THESAURUSDEVIANT);
      static bool isVentLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && layer.Contains(THESAURUSDEVIANT);
      if (!HandleGroupAtCurrentModelSpaceOnly)
      {
        ok_group_handles ??= new HashSet<Handle>();
        var groups = maxToiletToCondensepipeDistance.GetPersistentReactorIds().OfType<ObjectId>()
            .SelectNotNull(id => adb.ElementOrDefault<DBObject>(id)).OfType<Autodesk.AutoCAD.DatabaseServices.Group>().ToList();
        foreach (var default_fire_valve_width in groups)
        {
          if (ok_group_handles.Contains(default_fire_valve_width.Handle)) continue;
          ok_group_handles.Add(default_fire_valve_width.Handle);
          var MinWellToUrinalDistance = new List<GLineSegment>();
          foreach (var id in default_fire_valve_width.GetAllEntityIds())
          {
            var tolReturnValue0Approx = adb.Element<Entity>(id);
            var _dxfName = tolReturnValue0Approx.GetRXClass().DxfName.ToUpper();
            if (_dxfName is DISORGANIZATION && isDLineLayer(GetEffectiveLayer(tolReturnValue0Approx.Layer)))
            {
              dynamic o = tolReturnValue0Approx;
              var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
              MinWellToUrinalDistance.Add(default_fire_valve_length);
            }
          }
          geoData.DLines.AddRange(TempGeoFac.GetMinConnSegs(MinWellToUrinalDistance.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList()));
        }
      }
      if (!CollectRoomDataAtCurrentModelSpaceOnly)
      {
        if (TOILET_BUFFER_DISTANCE.ToUpper() is EXTRAORDINARINESS or THESAURUSUNSPEAKABLE)
        {
          if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
          {
            try
            {
              maxToiletToFloordrainDistance1 = maxToiletToFloordrainDistance1.Clone() as Polyline;
              maxToiletToFloordrainDistance1.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            }
            catch
            {
            }
            var poly = ConvertToPolygon(maxToiletToFloordrainDistance1);
            if (poly != null)
            {
              roomPolygons.Add(poly);
            }
          }
          return;
        }
        if (TOILET_BUFFER_DISTANCE.ToUpper() is THESAURUSEMBOLDEN or QUOTATIONGOLDEN)
        {
          CText ct = null;
          if (maxToiletToCondensepipeDistance is MText mtx)
          {
            ct = new CText() { Text = mtx.Text, Boundary = mtx.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN) };
          }
          if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance)
          {
            ct = new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN) };
          }
          if (maxToiletToFloordrainDistance2 is THESAURUSDURESS)
          {
            dynamic o = maxToiletToCondensepipeDistance.AcadObject;
            string repeated_point_distance = o.Text;
            if (!string.IsNullOrWhiteSpace(repeated_point_distance))
            {
              ct = new CText() { Text = repeated_point_distance, Boundary = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN) };
            }
          }
          if (ct != null)
          {
            roomNames.Add(ct);
          }
          return;
        }
      }
      {
        if (TOILET_BUFFER_DISTANCE is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
        {
          if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth)
          {
            if (TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, geoData.WrappingPipeLabelLines);
            }
            return;
          }
          else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
          {
            foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
            {
              if (maxBalconyToBalconyDistance.Length > THESAURUSSTAMPEDE)
              {
                var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
                _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, geoData.WrappingPipeLabelLines);
              }
            }
            return;
          }
          else if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance)
          {
            var repeated_point_distance = maxBalconyToDeviceplatformDistance.TextString;
            if (!string.IsNullOrWhiteSpace(repeated_point_distance))
            {
              var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              var ct = new CText() { Text = repeated_point_distance, Boundary = maxKitchenToBalconyDistance };
              _tol_lane_protect(maxToiletToFloordrainDistance, ct, geoData.WrappingPipeLabels);
            }
            return;
          }
        }
      }
      {
        if (TOILET_BUFFER_DISTANCE is THESAURUSINVOICE)
        {
          if (maxToiletToCondensepipeDistance is Spline)
          {
            var maxKitchenToBalconyDistance = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, rainPortSymbols);
            return;
          }
        }
      }
      {
        if (maxToiletToFloordrainDistance2 == QUOTATIONSWALLOW && TOILET_BUFFER_DISTANCE is THESAURUSINVOICE)
        {
          var DEFAULT_FIRE_VALVE_WIDTH = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, rainPortSymbols);
        }
      }
      {
        if (maxToiletToCondensepipeDistance is Circle maxBalconyrainpipeToFloordrainDistance && isDrainageLayer(TOILET_BUFFER_DISTANCE))
        {
          if (distinguishDiameter < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= HYPERDISYLLABLE)
          {
            var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
          if (THESAURUSCOMMUNICATION < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= distinguishDiameter && GetEffectiveLayer(maxBalconyrainpipeToFloordrainDistance.Layer) is THESAURUSJUBILEE or QUOTATIONBENJAMIN)
          {
            var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, downwaterPorts);
            return;
          }
        }
      }
      {
        if (maxToiletToCondensepipeDistance is Circle maxBalconyrainpipeToFloordrainDistance)
        {
          if (distinguishDiameter < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= HYPERDISYLLABLE)
          {
            if (isDrainageLayer(maxBalconyrainpipeToFloordrainDistance.Layer))
            {
              var DEFAULT_FIRE_VALVE_WIDTH = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, pipes);
              return;
            }
          }
        }
      }
      if (TOILET_BUFFER_DISTANCE is INSTRUMENTALITY)
      {
        if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
        {
          var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, wlines);
          return;
        }
        else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
        {
          foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
          {
            var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, wlines);
          }
          return;
        }
        if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
        {
          dynamic o = maxToiletToCondensepipeDistance;
          var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, wlines);
          return;
        }
      }
      if (isDLineLayer(TOILET_BUFFER_DISTANCE))
      {
        if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
        {
          var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, dlines);
          return;
        }
        else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
        {
          foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
          {
            var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, dlines);
          }
          return;
        }
        if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
        {
          dynamic o = maxToiletToCondensepipeDistance;
          var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, dlines);
          return;
        }
      }
      if (isVentLayer(TOILET_BUFFER_DISTANCE))
      {
        if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
        {
          var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, vlines);
          return;
        }
        else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
        {
          foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
          {
            var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, vlines);
          }
          return;
        }
        if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
        {
          dynamic o = maxToiletToCondensepipeDistance;
          var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, vlines);
          return;
        }
      }
      if (maxToiletToFloordrainDistance2 is DISORGANIZATION)
      {
        if (TOILET_BUFFER_DISTANCE is THESAURUSSINCERE or THESAURUSJUBILEE)
        {
          foreach (var maxBalconyrainpipeToFloordrainDistance in maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
          {
            if (maxBalconyrainpipeToFloordrainDistance.Radius > distinguishDiameter)
            {
              var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            }
            else if (THESAURUSCOMMUNICATION < maxBalconyrainpipeToFloordrainDistance.Radius && maxBalconyrainpipeToFloordrainDistance.Radius <= distinguishDiameter && GetEffectiveLayer(maxBalconyrainpipeToFloordrainDistance.Layer) is THESAURUSJUBILEE or QUOTATIONBENJAMIN)
            {
              var maxKitchenToBalconyDistance = maxBalconyrainpipeToFloordrainDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, downwaterPorts);
            }
          }
        }
      }
      {
        if (isDrainageLayer(TOILET_BUFFER_DISTANCE) && maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
        {
          var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, labelLines);
          return;
        }
      }
      if (maxToiletToFloordrainDistance2 == THESAURUSWINDFALL)
      {
        dynamic o = maxToiletToCondensepipeDistance.AcadObject;
        var repeated_point_distance = (string)o.DimStyleText + THESAURUSSPECIFICATION + (string)o.VPipeNum;
        var colle = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
        var ts = new List<DBText>();
        foreach (var tolReturnValue0Approx in colle.OfType<Entity>().Where(IsLayerVisible))
        {
          if (tolReturnValue0Approx is Line TolUniformSideLenth && isDrainageLayer(TolUniformSideLenth.Layer))
          {
            if (TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, labelLines);
              continue;
            }
          }
          else if (tolReturnValue0Approx.GetRXClass().DxfName.ToUpper() == THESAURUSDURESS)
          {
            ts.AddRange(tolReturnValue0Approx.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
            continue;
          }
        }
        if (ts.Count > THESAURUSSTAMPEDE)
        {
          GRect maxKitchenToBalconyDistance;
          if (ts.Count == THESAURUSHOUSING) maxKitchenToBalconyDistance = ts[THESAURUSSTAMPEDE].Bounds.ToGRect();
          else
          {
            maxKitchenToBalconyDistance = GeoFac.CreateGeometry(ts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Bounds.ToGRect()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon())).EnvelopeInternal.ToGRect();
          }
          maxKitchenToBalconyDistance = maxKitchenToBalconyDistance.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          var ct = new CText() { Text = repeated_point_distance, Boundary = maxKitchenToBalconyDistance };
          if (IsMaybeLabelText(ct.Text)) _tol_lane_protect(maxToiletToFloordrainDistance, ct, cts);
        }
        return;
      }
      {
        static bool default_fire_valve_width(string TolLightRangeMin) => !TolLightRangeMin.StartsWith(THESAURUSNOTATION) && !TolLightRangeMin.ToLower().Contains(PERPENDICULARITY) && !TolLightRangeMin.ToUpper().Contains(THESAURUSIMPOSTER);
        if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance && isDrainageLayer(TOILET_BUFFER_DISTANCE) && default_fire_valve_width(maxBalconyToDeviceplatformDistance.TextString))
        {
          var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          var ct = new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxKitchenToBalconyDistance };
          if (IsMaybeLabelText(ct.Text)) _tol_lane_protect(maxToiletToFloordrainDistance, ct, cts);
          return;
        }
      }
      if (maxToiletToFloordrainDistance2 == THESAURUSDURESS)
      {
        dynamic o = maxToiletToCondensepipeDistance.AcadObject;
        string repeated_point_distance = o.Text;
        if (!string.IsNullOrWhiteSpace(repeated_point_distance))
        {
          var ct = new CText() { Text = repeated_point_distance, Boundary = maxToiletToCondensepipeDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN) };
          if (IsMaybeLabelText(ct.Text)) _tol_lane_protect(maxToiletToFloordrainDistance, ct, cts);
        }
        return;
      }
      if (maxToiletToFloordrainDistance2 == THESAURUSINHARMONIOUS)
      {
        if (TOILET_BUFFER_DISTANCE is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
        {
          dynamic o = maxToiletToCondensepipeDistance.AcadObject;
          string UpText = o.UpText;
          string DownText = o.DownText;
          var MAX_TAG_YPOSITION = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
          var TolLane = MAX_TAG_YPOSITION.OfType<Line>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGLineSegment()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
          var points = GeoFac.GetAlivePoints(TolLane, THESAURUSHOUSING);
          var _tol_avg_column_dist = points.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
          points = points.Except(GeoFac.CreateIntersectsSelector(_tol_avg_column_dist)(GeoFac.CreateGeometryEx(TolLane.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsHorizontal(THESAURUSHOUSING)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(_tol_avg_column_dist).ToList(points)).ToList();
          foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in points)
          {
            var TolLightRangeMin = TryParseWrappingPipeRadiusText(DownText);
            if (!string.IsNullOrWhiteSpace(TolLightRangeMin))
            {
              wrappingPipeRadius.Add(new KeyValuePair<Point2d, string>(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, TolLightRangeMin));
            }
          }
          return;
        }
        var colle = maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection();
        {
          foreach (var tolReturnValue0Approx in colle.OfType<Entity>().Where(tolReturnValue0Approx => tolReturnValue0Approx.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE).Where(TolLightRangeSingleSideMax => isDrainageLayer(TolLightRangeSingleSideMax.Layer)).Where(IsLayerVisible))
          {
            foreach (var maxBalconyToDeviceplatformDistance in tolReturnValue0Approx.ExplodeToDBObjectCollection().OfType<DBText>().Where(TolLightRangeSingleSideMax => !string.IsNullOrWhiteSpace(TolLightRangeSingleSideMax.TextString)).Where(IsLayerVisible))
            {
              var maxKitchenToBalconyDistance = maxBalconyToDeviceplatformDistance.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              var ct = new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxKitchenToBalconyDistance };
              if (IsMaybeLabelText(ct.Text)) _tol_lane_protect(maxToiletToFloordrainDistance, ct, cts);
            }
          }
          foreach (var default_fire_valve_length in colle.OfType<Line>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSSTAMPEDE).Where(TolLightRangeSingleSideMax => isDrainageLayer(TolLightRangeSingleSideMax.Layer)).Where(IsLayerVisible).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGLineSegment().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN)))
          {
            _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, labelLines);
          }
        }
        return;
      }
    }
    const string XREF_LAYER = THESAURUSPREFERENCE;
    private void handleBlockReference(BlockReference tolReturnValueRangeTo, Matrix3d MAX_RAINPIPE_TO_BALCONYFLOORDRAIN, List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance)
    {
      if (!tolReturnValueRangeTo.ObjectId.IsValid || !tolReturnValueRangeTo.BlockTableRecord.IsValid) return;
      if (!tolReturnValueRangeTo.Visible) return;
      if (IsLayerVisible(tolReturnValueRangeTo))
      {
        var _name = tolReturnValueRangeTo.GetEffectiveName() ?? THESAURUSDEPLORE;
        var toilet_buffer_distance = GetEffectiveBRName(_name);
        if (toilet_buffer_distance is THESAURUSDENOUNCE)
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          var maxToiletToFloordrainDistance1 = new Polygon(tolReturnValueRangeTo.Bounds.ToGRect().ToLinearRing(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN));
          maxToiletToFloordrainDistance.Add(new KeyValuePair<Geometry, Action>(maxKitchenToBalconyDistance.ToPolygon(), () =>
          {
            geoData.CleaningPorts.Add(maxToiletToFloordrainDistance1);
            geoData.CleaningPortBasePoints.Add(tolReturnValueRangeTo.Position.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToPoint2d());
          }));
          return;
        }
        if (xstNames.Contains(toilet_buffer_distance))
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, geoData.xsts);
          return;
        }
        if (zbqNames.Contains(toilet_buffer_distance))
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, geoData.zbqs);
          return;
        }
        if (toilet_buffer_distance is THESAURUSLANDMARK)
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          var SidewaterbucketYIndent = tolReturnValueRangeTo.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE;
          _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, () =>
          {
            waterPorts.Add(maxKitchenToBalconyDistance);
            waterPortLabels.Add(SidewaterbucketYIndent);
          });
          return;
        }
        if (toilet_buffer_distance.Contains(THESAURUSINTENTIONAL))
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          var SidewaterbucketYIndent = tolReturnValueRangeTo.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE;
          _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, () =>
          {
            waterWells.Add(maxKitchenToBalconyDistance);
          });
          return;
        }
        if (!MAX_ROOM_INTERVAL)
        {
          if (toilet_buffer_distance.Contains(THESAURUSCONFRONTATION) && _name.Count(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax == SUPERREGENERATIVE) < THESAURUSPERMUTATION)
          {
            if (tolReturnValueRangeTo.IsDynamicBlock)
            {
              var maxKitchenToBalconyDistance = GRect.Combine(tolReturnValueRangeTo.ExplodeToDBObjectCollection().OfType<Circle>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Visible && TolLightRangeSingleSideMax.Bounds.HasValue).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Bounds.ToGRect()));
              if (!maxKitchenToBalconyDistance.IsValid)
              {
                maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect();
              }
              maxKitchenToBalconyDistance = maxKitchenToBalconyDistance.TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, () =>
              {
                floorDrains.Add(maxKitchenToBalconyDistance);
                geoData.UpdateFloorDrainTypeDict(maxKitchenToBalconyDistance.Center, tolReturnValueRangeTo.ObjectId.GetDynBlockValue(THESAURUSENTERPRISE) ?? THESAURUSDEPLORE);
              });
              DrawRectLazy(maxKitchenToBalconyDistance, THESAURUSACRIMONIOUS);
              return;
            }
            else
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, () =>
              {
                floorDrains.Add(maxKitchenToBalconyDistance);
              });
              return;
            }
          }
        }
        {
          if (toilet_buffer_distance.Contains(THESAURUSSTRAIGHTFORWARD) || toilet_buffer_distance.Contains(THESAURUSEMPTINESS) || toilet_buffer_distance.Contains(THESAURUSHYPOCRISY) || toilet_buffer_distance.Contains(THESAURUSRECIPE) || toilet_buffer_distance.Contains(THESAURUSRAFFLE))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSENTREPRENEUR);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
          if (isDrainageLayer(tolReturnValueRangeTo.Layer))
          {
            if (toilet_buffer_distance is SUPERINDUCEMENT or THESAURUSURBANITY)
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSENTREPRENEUR);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
              return;
            }
            if (toilet_buffer_distance.Contains(THESAURUSSPECIMEN))
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSHESITANCY);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
              return;
            }
            if (toilet_buffer_distance is THESAURUSMANIKIN)
            {
              var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSLUMBERING);
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
              return;
            }
          }
          if (toilet_buffer_distance is REPRESENTATIVES && GetEffectiveLayer(tolReturnValueRangeTo.Layer) is THESAURUSCORRELATION or THESAURUSJUBILEE or THESAURUSCONTROVERSY)
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSLUMBERING);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
          if (toilet_buffer_distance is THESAURUSUNINTERESTED && GetEffectiveLayer(tolReturnValueRangeTo.Layer) is QUOTATIONCORNISH)
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN).ToGRect(THESAURUSLUMBERING);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
          if (toilet_buffer_distance is SUPERINDUCEMENT or THESAURUSURBANITY)
          {
            var maxKitchenToBalconyDistance = GRect.Create(tolReturnValueRangeTo.Bounds.ToGRect().Center.ToPoint3d().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN), THESAURUSENTREPRENEUR);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
          if (toilet_buffer_distance.Contains(THESAURUSSPECIMEN))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipes);
            return;
          }
        }
        if (toilet_buffer_distance.Contains(THESAURUSTHOROUGHBRED))
        {
          var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
          if (maxKitchenToBalconyDistance.IsValid)
          {
            if (maxKitchenToBalconyDistance.Width < POLYOXYMETHYLENE && maxKitchenToBalconyDistance.Height < POLYOXYMETHYLENE)
            {
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, wrappingPipes);
            }
          }
          return;
        }
        {
          var SidewaterbucketXIndent = INTRAVASCULARLY;
          if (killerNames.Any(TolLightRangeSingleSideMax => toilet_buffer_distance.Contains(TolLightRangeSingleSideMax)))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            if (!washingMachinesNames.Any(TolLightRangeSingleSideMax => toilet_buffer_distance.Contains(TolLightRangeSingleSideMax)))
            {
              _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, pipeKillers);
            }
            SidewaterbucketXIndent = THESAURUSOBSTINACY;
          }
          if (basinNames.Any(TolLightRangeSingleSideMax => toilet_buffer_distance.Contains(TolLightRangeSingleSideMax)))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, basins);
            SidewaterbucketXIndent = THESAURUSOBSTINACY;
          }
          if (washingMachinesNames.Any(TolLightRangeSingleSideMax => toilet_buffer_distance.Contains(TolLightRangeSingleSideMax)))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, washingMachines);
            SidewaterbucketXIndent = THESAURUSOBSTINACY;
          }
          if (mopPoolNames.Any(TolLightRangeSingleSideMax => toilet_buffer_distance.Contains(TolLightRangeSingleSideMax)))
          {
            var maxKitchenToBalconyDistance = tolReturnValueRangeTo.Bounds.ToGRect().TransformBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN);
            _tol_lane_protect(maxToiletToFloordrainDistance, maxKitchenToBalconyDistance, mopPools);
            SidewaterbucketXIndent = THESAURUSOBSTINACY;
          }
          if (SidewaterbucketXIndent) return;
        }
      }
      var minDeviceplatformArea = adb.Element<BlockTableRecord>(tolReturnValueRangeTo.BlockTableRecord);
      if (!IsWantedBlock(minDeviceplatformArea)) return;
      var _fs = new List<KeyValuePair<Geometry, Action>>();
      foreach (var objId in minDeviceplatformArea)
      {
        var dbObj = adb.Element<Entity>(objId);
        if (dbObj is BlockReference b)
        {
          handleBlockReference(b, tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN), _fs);
        }
        else
        {
          handleEntity(dbObj, tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN), _fs);
        }
      }
      {
        var MinWellToUrinalDistance = new List<KeyValuePair<Geometry, Action>>();
        var TolLightRangeSingleSideMin = tolReturnValueRangeTo.XClipInfo();
        if (TolLightRangeSingleSideMin.IsValid)
        {
          TolLightRangeSingleSideMin.TransformBy(tolReturnValueRangeTo.BlockTransform.PreMultiplyBy(MAX_RAINPIPE_TO_BALCONYFLOORDRAIN));
          var default_voltage = TolLightRangeSingleSideMin.PreparedPolygon;
          foreach (var max_rainpipe_to_balconyfloordrain in _fs)
          {
            if (default_voltage.Intersects(max_rainpipe_to_balconyfloordrain.Key))
            {
              MinWellToUrinalDistance.Add(max_rainpipe_to_balconyfloordrain);
            }
          }
        }
        else
        {
          foreach (var max_rainpipe_to_balconyfloordrain in _fs)
          {
            MinWellToUrinalDistance.Add(max_rainpipe_to_balconyfloordrain);
          }
        }
        maxToiletToFloordrainDistance.AddRange(MinWellToUrinalDistance);
      }
    }
    readonly List<string> basinNames = new List<string>() { CONSUBSTANTIATUS, THESAURUSCHIVALROUS, THESAURUSGRUESOME, THESAURUSDAPPER, QUOTATIONPYRIFORM, THESAURUSEXTORTIONATE, THESAURUSDRASTIC };
    readonly List<string> mopPoolNames = new List<string>() { THESAURUSHUMOUR, };
    readonly List<string> killerNames = new List<string>() { THESAURUSDRASTIC, HYPERSENSITIZED, THESAURUSHOODLUM, PHYTOGEOGRAPHER, THESAURUSABOUND, THESAURUSALLURE, THESAURUSMACHINERY, THESAURUSESCALATE };
    readonly List<string> washingMachinesNames = new List<string>() { THESAURUSCLINICAL, THESAURUSBALEFUL };
    bool MAX_ROOM_INTERVAL;
    static bool HandleGroupAtCurrentModelSpaceOnly = INTRAVASCULARLY;
    HashSet<string> VerticalAiringMachineNames;
    HashSet<string> HangingAiringMachineNames;
    HashSet<string> xstNames;
    HashSet<string> zbqNames;
    public void CollectEntities()
    {
      {
        var dict = ThMEPWSS.ViewModel.BlockConfigService.GetBlockNameListDict();
        dict.TryGetValue(THESAURUSMARSHY, out List<string> lstVertical);
        if (lstVertical != null) this.VerticalAiringMachineNames = new HashSet<string>(lstVertical);
        dict.TryGetValue(THESAURUSPROFANITY, out List<string> lstHanging);
        if (lstHanging != null) this.HangingAiringMachineNames = new HashSet<string>(lstHanging);
        HashSet<string> hs1 = null;
        dict.TryGetValue(QUOTATIONROBERT, out List<string> lst1);
        if (lst1 != null) hs1 = new HashSet<string>(lst1);
        HashSet<string> hs2 = null;
        dict.TryGetValue(THESAURUSDELIVER, out List<string> lst2);
        if (lst2 != null) hs2 = new HashSet<string>(lst2);
        HashSet<string> hs3 = null;
        dict.TryGetValue(CYLINDRICALNESS, out List<string> lst3);
        if (lst3 != null) hs3 = new HashSet<string>(lst3);
        hs1 ??= new HashSet<string>();
        hs2 ??= new HashSet<string>();
        hs3 ??= new HashSet<string>();
        this.xstNames = new HashSet<string>(hs1.Concat(hs2));
        this.zbqNames = new HashSet<string>(hs3);
        this.VerticalAiringMachineNames ??= new HashSet<string>();
        this.HangingAiringMachineNames ??= new HashSet<string>();
      }
      if (!CollectRoomDataAtCurrentModelSpaceOnly)
      {
        roomNames ??= new List<CText>();
        roomPolygons ??= new List<Polygon>();
      }
      if (HandleGroupAtCurrentModelSpaceOnly)
      {
        foreach (var default_fire_valve_width in adb.Groups)
        {
          var MinWellToUrinalDistance = new List<GLineSegment>();
          foreach (var id in default_fire_valve_width.GetAllEntityIds())
          {
            var maxToiletToCondensepipeDistance = adb.Element<Entity>(id);
            var maxToiletToFloordrainDistance2 = maxToiletToCondensepipeDistance.GetRXClass().DxfName.ToUpper();
            if (maxToiletToFloordrainDistance2 is DISORGANIZATION && GetEffectiveLayer(maxToiletToCondensepipeDistance.Layer) is THESAURUSCONTROVERSY)
            {
              dynamic o = maxToiletToCondensepipeDistance;
              var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
              MinWellToUrinalDistance.Add(default_fire_valve_length);
            }
          }
          MinWellToUrinalDistance = MinWellToUrinalDistance.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
          geoData.DLines.AddRange(TempGeoFac.GetMinConnSegs(MinWellToUrinalDistance.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList()));
        }
      }
      foreach (var group in adb.Groups)
      {
        var MinWellToUrinalDistance = new List<Geometry>();
        foreach (var id in group.GetAllEntityIds())
        {
          var maxToiletToCondensepipeDistance = adb.Element<Entity>(id);
          var maxToiletToFloordrainDistance2 = maxToiletToCondensepipeDistance.GetRXClass().DxfName.ToUpper();
          if (maxToiletToCondensepipeDistance.Layer is THESAURUSCONTROVERSY)
          {
            if (maxToiletToCondensepipeDistance is Line TolUniformSideLenth && TolUniformSideLenth.Length > THESAURUSSTAMPEDE)
            {
              var default_fire_valve_length = TolUniformSideLenth.ToGLineSegment();
              if (default_fire_valve_length.IsValid) MinWellToUrinalDistance.Add(default_fire_valve_length.ToLineString());
              continue;
            }
            else if (maxToiletToCondensepipeDistance is Polyline maxToiletToFloordrainDistance1)
            {
              foreach (var maxBalconyToBalconyDistance in maxToiletToFloordrainDistance1.ExplodeToDBObjectCollection().OfType<Line>())
              {
                var default_fire_valve_length = maxBalconyToBalconyDistance.ToGLineSegment();
                if (default_fire_valve_length.IsValid) MinWellToUrinalDistance.Add(default_fire_valve_length.ToLineString());
              }
              continue;
            }
          }
          if (maxToiletToFloordrainDistance2 is DISORGANIZATION && GetEffectiveLayer(maxToiletToCondensepipeDistance.Layer) is THESAURUSCONTROVERSY)
          {
            dynamic o = maxToiletToCondensepipeDistance;
            var default_fire_valve_length = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
            if (default_fire_valve_length.IsValid) MinWellToUrinalDistance.Add(default_fire_valve_length.ToLineString());
            continue;
          }
          if (!maxToiletToCondensepipeDistance.Visible || !isDrainageLayer(maxToiletToCondensepipeDistance.Layer)) continue;
          var maxKitchenToBalconyDistance = maxToiletToCondensepipeDistance.Bounds.ToGRect();
          if (maxKitchenToBalconyDistance.IsValid)
          {
            MinWellToUrinalDistance.Add(maxKitchenToBalconyDistance.ToPolygon());
            continue;
          }
          else
          {
            var ext = new Extents3d();
            try
            {
              foreach (var tolReturnValue0Approx in maxToiletToCondensepipeDistance.ExplodeToDBObjectCollection().OfType<Entity>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Visible))
              {
                if (tolReturnValue0Approx.Bounds.HasValue)
                {
                  var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = tolReturnValue0Approx.Bounds.Value;
                  var DEFAULT_FIRE_VALVE_WIDTH = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.ToGRect();
                  if (DEFAULT_FIRE_VALVE_WIDTH.IsValid)
                  {
                    ext.AddExtents(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                  }
                }
              }
            }
            catch { }
            maxKitchenToBalconyDistance = ext.ToGRect();
            if (maxKitchenToBalconyDistance.IsValid && maxKitchenToBalconyDistance.Width < ALSOMONOSIPHONIC && maxKitchenToBalconyDistance.Height < ALSOMONOSIPHONIC)
            {
              MinWellToUrinalDistance.Add(maxKitchenToBalconyDistance.ToPolygon());
              continue;
            }
          }
        }
        if (MinWellToUrinalDistance.Count > THESAURUSSTAMPEDE) geoData.Groups.Add(MinWellToUrinalDistance);
      }
      foreach (var maxToiletToCondensepipeDistance in adb.ModelSpace.OfType<Entity>())
      {
        {
          {
            if (maxToiletToCondensepipeDistance is Circle maxBalconyrainpipeToFloordrainDistance && isDrainageLayer(maxBalconyrainpipeToFloordrainDistance.Layer) && maxBalconyrainpipeToFloordrainDistance.Radius > THESAURUSSTAMPEDE)
            {
              geoData.circles.Add(maxBalconyrainpipeToFloordrainDistance.ToGCircle());
            }
          }
          {
            if (maxToiletToCondensepipeDistance is BlockReference tolReturnValueRangeTo && tolReturnValueRangeTo.BlockTableRecord.IsValid && tolReturnValueRangeTo.GetEffectiveName() is THESAURUSCONFRONTATION or PARATHYROIDECTOMY)
            {
              var maxBalconyrainpipeToFloordrainDistance = EntityTool.GetCircles(tolReturnValueRangeTo).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Visible).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle()).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Radius > THESAURUSINCOMPLETE).FirstOrDefault();
              if (maxBalconyrainpipeToFloordrainDistance.IsValid)
              {
                geoData.FloorDrains.Add(maxBalconyrainpipeToFloordrainDistance.ToCirclePolygon(SUPERLATIVENESS).ToGRect());
                geoData.FloorDrainRings.Add(maxBalconyrainpipeToFloordrainDistance);
              }
            }
          }
        }
        {
          if (maxToiletToCondensepipeDistance.Layer is THESAURUSARCHER)
          {
            var maxKitchenToBalconyDistance = maxToiletToCondensepipeDistance.Bounds.ToGRect();
            if (maxKitchenToBalconyDistance.IsValid)
            {
              washingMachines.Add(maxKitchenToBalconyDistance);
            }
            return;
          }
        }
        {
          if (maxToiletToCondensepipeDistance is BlockReference tolReturnValueRangeTo)
          {
            if (!tolReturnValueRangeTo.BlockTableRecord.IsValid) continue;
            var minDeviceplatformArea = adb.Blocks.Element(tolReturnValueRangeTo.BlockTableRecord);
            var _fs = new List<KeyValuePair<Geometry, Action>>();
            Action maxDeviceplatformArea = null;
            try
            {
              MAX_ROOM_INTERVAL = minDeviceplatformArea.XrefStatus != XrefStatus.NotAnXref;
              handleBlockReference(tolReturnValueRangeTo, Matrix3d.Identity, _fs);
            }
            finally
            {
              MAX_ROOM_INTERVAL = INTRAVASCULARLY;
            }
            {
              var TolLightRangeSingleSideMin = tolReturnValueRangeTo.XClipInfo();
              if (TolLightRangeSingleSideMin.IsValid)
              {
                TolLightRangeSingleSideMin.TransformBy(tolReturnValueRangeTo.BlockTransform);
                var default_voltage = TolLightRangeSingleSideMin.PreparedPolygon;
                foreach (var max_rainpipe_to_balconyfloordrain in _fs)
                {
                  if (default_voltage.Intersects(max_rainpipe_to_balconyfloordrain.Key))
                  {
                    maxDeviceplatformArea += max_rainpipe_to_balconyfloordrain.Value;
                  }
                }
              }
              else
              {
                foreach (var max_rainpipe_to_balconyfloordrain in _fs)
                {
                  maxDeviceplatformArea += max_rainpipe_to_balconyfloordrain.Value;
                }
              }
              maxDeviceplatformArea?.Invoke();
            }
          }
          else
          {
            var _fs = new List<KeyValuePair<Geometry, Action>>();
            handleEntity(maxToiletToCondensepipeDistance, Matrix3d.Identity, _fs);
            foreach (var max_rainpipe_to_balconyfloordrain in _fs)
            {
              max_rainpipe_to_balconyfloordrain.Value();
            }
          }
        }
      }
    }
    private static bool IsWantedBlock(BlockTableRecord blockTableRecord)
    {
      if (blockTableRecord.IsDynamicBlock)
      {
        return INTRAVASCULARLY;
      }
      if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
      {
        return INTRAVASCULARLY;
      }
      if (!blockTableRecord.Explodable)
      {
        return INTRAVASCULARLY;
      }
      return THESAURUSOBSTINACY;
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GLineSegment default_fire_valve_length, Action maxDeviceplatformArea)
    {
      if (default_fire_valve_length.IsValid) maxToiletToFloordrainDistance.Add(new KeyValuePair<Geometry, Action>(default_fire_valve_length.ToLineString(), maxDeviceplatformArea));
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GLineSegment default_fire_valve_length, List<GLineSegment> MinWellToUrinalDistance)
    {
      if (default_fire_valve_length.IsValid) _tol_lane_protect(maxToiletToFloordrainDistance, default_fire_valve_length, () => { MinWellToUrinalDistance.Add(default_fire_valve_length); });
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GRect DEFAULT_FIRE_VALVE_WIDTH, Action maxDeviceplatformArea)
    {
      if (DEFAULT_FIRE_VALVE_WIDTH.IsValid) maxToiletToFloordrainDistance.Add(new KeyValuePair<Geometry, Action>(DEFAULT_FIRE_VALVE_WIDTH.ToPolygon(), maxDeviceplatformArea));
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, GRect DEFAULT_FIRE_VALVE_WIDTH, List<GRect> MinWellToUrinalDistance)
    {
      if (DEFAULT_FIRE_VALVE_WIDTH.IsValid) _tol_lane_protect(maxToiletToFloordrainDistance, DEFAULT_FIRE_VALVE_WIDTH, () => { MinWellToUrinalDistance.Add(DEFAULT_FIRE_VALVE_WIDTH); });
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, CText ct, Action maxDeviceplatformArea)
    {
      _tol_lane_protect(maxToiletToFloordrainDistance, ct.Boundary, maxDeviceplatformArea);
    }
    private static void _tol_lane_protect(List<KeyValuePair<Geometry, Action>> maxToiletToFloordrainDistance, CText ct, List<CText> MinWellToUrinalDistance)
    {
      _tol_lane_protect(maxToiletToFloordrainDistance, ct, () => { MinWellToUrinalDistance.Add(ct); });
    }
    public void CollectRoomData()
    {
      if (CollectRoomDataAtCurrentModelSpaceOnly)
      {
        roomData.AddRange(DrainageService.CollectRoomData(adb));
      }
      else
      {
        var ranges = roomPolygons;
        var names = roomNames;
        var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(ranges);
        var list = roomData;
        foreach (var toilet_buffer_distance in names)
        {
          if (toilet_buffer_distance.Boundary.IsValid)
          {
            var l = maxDeviceplatformArea(toilet_buffer_distance.Boundary.ToPolygon());
            if (l.Count == THESAURUSHOUSING)
            {
              list.Add(new KeyValuePair<string, Geometry>(toilet_buffer_distance.Text.Trim(), l[THESAURUSSTAMPEDE]));
            }
            else
            {
              foreach (var DEFAULT_FIRE_VALVE_LENGTH in l)
              {
                DrawGeometryLazy(DEFAULT_FIRE_VALVE_LENGTH);
                ranges.Remove(DEFAULT_FIRE_VALVE_LENGTH);
              }
            }
          }
        }
        foreach (var range in ranges.Except(list.Select(max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Value)))
        {
          list.Add(new KeyValuePair<string, Geometry>(THESAURUSDEPLORE, range));
        }
      }
    }
    static bool CollectRoomDataAtCurrentModelSpaceOnly = THESAURUSOBSTINACY;
  }
  public class DrainageService
  {
    public static void CollectGeoData(AcadDatabase adb, DrainageGeoData geoData, CommandContext ctx)
    {
      var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
      cl.CollectStoreys(ctx);
      cl.CollectEntities();
      cl.CollectRoomData();
    }
    public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
    {
      var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
      cl.CollectStoreys(range);
      cl.CollectEntities();
      cl.CollectRoomData();
    }
    static bool AllNotEmpty(params List<Geometry>[] plss)
    {
      foreach (var max_tag_xposition in plss)
      {
        if (max_tag_xposition.Count == THESAURUSSTAMPEDE) return INTRAVASCULARLY;
      }
      return THESAURUSOBSTINACY;
    }
    public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
    {
      var ranges = new HashSet<Geometry>(adb.ModelSpace.OfType<Polyline>().Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer?.ToUpper() is EXTRAORDINARINESS or THESAURUSUNSPEAKABLE)
          .SelectNotNull(ConvertToPolygon).ToList());
      var names = adb.ModelSpace.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Layer?.ToUpper() is THESAURUSEMBOLDEN or QUOTATIONGOLDEN).SelectNotNull(maxToiletToCondensepipeDistance =>
      {
        if (maxToiletToCondensepipeDistance is MText mtx)
        {
          return new CText() { Text = mtx.Text, Boundary = mtx.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect() };
        }
        if (maxToiletToCondensepipeDistance is DBText maxBalconyToDeviceplatformDistance)
        {
          return new CText() { Text = maxBalconyToDeviceplatformDistance.TextString, Boundary = maxBalconyToDeviceplatformDistance.Bounds.ToGRect() };
        }
        var maxToiletToFloordrainDistance2 = maxToiletToCondensepipeDistance.GetRXClass().DxfName.ToUpper();
        if (maxToiletToFloordrainDistance2 == THESAURUSDURESS)
        {
          dynamic o = maxToiletToCondensepipeDistance.AcadObject;
          string repeated_point_distance = o.Text;
          if (!string.IsNullOrWhiteSpace(repeated_point_distance))
          {
            return new CText() { Text = repeated_point_distance, Boundary = maxToiletToCondensepipeDistance.Bounds.ToGRect() };
          }
        }
        return null;
      }).ToList();
      var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(ranges.ToList());
      var list = new List<KeyValuePair<string, Geometry>>(names.Count);
      foreach (var toilet_buffer_distance in names)
      {
        if (toilet_buffer_distance.Boundary.IsValid)
        {
          var l = maxDeviceplatformArea(toilet_buffer_distance.Boundary.ToPolygon());
          if (l.Count == THESAURUSHOUSING)
          {
            list.Add(new KeyValuePair<string, Geometry>(toilet_buffer_distance.Text.Trim(), l[THESAURUSSTAMPEDE]));
          }
          else if (l.Count > THESAURUSSTAMPEDE)
          {
            var tmp = l.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Area).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax > THESAURUSSTAMPEDE).ToList();
            if (tmp.Count > THESAURUSSTAMPEDE)
            {
              var _tolReturnValueMaxDistance = tmp.Min();
              foreach (var DEFAULT_FIRE_VALVE_LENGTH in l)
              {
                if (DEFAULT_FIRE_VALVE_LENGTH.Area == _tolReturnValueMaxDistance)
                {
                  list.Add(new KeyValuePair<string, Geometry>(toilet_buffer_distance.Text.Trim(), DEFAULT_FIRE_VALVE_LENGTH));
                  foreach (var TolLightRangeSingleSideMax in l) ranges.Remove(TolLightRangeSingleSideMax);
                  break;
                }
              }
            }
          }
        }
      }
      foreach (var range in ranges.Except(list.Select(max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Value)))
      {
        list.Add(new KeyValuePair<string, Geometry>(THESAURUSDEPLORE, range));
      }
      return list;
    }
    public static ExtraInfo CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out string logString, out List<DrainageDrawingData> drDatas)
    {
      var extraInfo = new ExtraInfo() { Items = new List<ExtraInfo.Item>(), CadDatas = cadDatas, geoData = geoData, };
      var roomData = geoData.RoomData;
      Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
      Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
      Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
      static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(MinWellToUrinalDistance => GeoFac.CreateGeometry(MinWellToUrinalDistance)).ToList();
      foreach (var MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN in geoData.Storeys)
      {
        var tolReturnValue0Approx = DrawRectLazy(MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN).ColorIndex = THESAURUSHOUSING;
      }
      var sb = new StringBuilder(THESAURUSEXCEPTION);
      drDatas = new List<DrainageDrawingData>();
      var _kitchens = roomData.Where(TolLightRangeSingleSideMax => IsKitchen(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _toilets = roomData.Where(TolLightRangeSingleSideMax => IsToilet(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _nonames = roomData.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key is THESAURUSDEPLORE).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _balconies = roomData.Where(TolLightRangeSingleSideMax => IsBalcony(TolLightRangeSingleSideMax.Key)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value).ToList();
      var _kitchensf = F(_kitchens);
      var _toiletsf = F(_toilets);
      var _nonamesf = F(_nonames);
      var _balconiesf = F(_balconies);
      var circlePts = geoData.circles.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(SUPERLATIVENESS).Tag(TolLightRangeSingleSideMax)).ToList();
      var circlePtsf = GeoFac.CreateIntersectsSelector(circlePts);
      for (int si = THESAURUSSTAMPEDE; si < cadDatas.Count; si++)
      {
        var drData = new DrainageDrawingData();
        drData.Init();
        var REPEATED_POINT_DISTANCE = cadDatas[si];
        var exItem = new ExtraInfo.Item();
        extraInfo.Items.Add(exItem);
        var storeyGeo = geoData.Storeys[si].ToPolygon();
        var kitchens = _kitchensf(storeyGeo);
        var toilets = _toiletsf(storeyGeo);
        var nonames = _nonamesf(storeyGeo);
        var balconies = _balconiesf(storeyGeo);
        var kitchensf = F(kitchens);
        var toiletsf = F(toilets);
        var nonamesf = F(nonames);
        var balconiesf = F(balconies);
        {
          var maxDis = THESAURUSEPICUREAN;
          var angleTolleranceDegree = THESAURUSHOUSING;
          var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
          var tolReturnValueRange = GeoFac.AutoConn(REPEATED_POINT_DISTANCE.DLines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Length > THESAURUSSTAMPEDE).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
              GeoFac.CreateGeometryEx(REPEATED_POINT_DISTANCE.VerticalPipes.Concat(REPEATED_POINT_DISTANCE.DownWaterPorts).Concat(REPEATED_POINT_DISTANCE.FloorDrains).Concat(REPEATED_POINT_DISTANCE.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
              maxDis, angleTolleranceDegree).ToList();
          geoData.DLines.AddRange(tolReturnValueRange);
          var dlineCvt = DrainageCadData.ConvertDLinesF();
          var _lines = tolReturnValueRange.Select(dlineCvt).ToList();
          cadDataMain.DLines.AddRange(_lines);
          REPEATED_POINT_DISTANCE.DLines.AddRange(_lines);
        }
        var lbDict = new Dictionary<Geometry, string>();
        var notedPipesDict = new Dictionary<Geometry, string>();
        var labelLinesGroup = GG(REPEATED_POINT_DISTANCE.LabelLines);
        var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
        var labellinesGeosf = F(labelLinesGeos);
        var shortTranslatorLabels = new HashSet<string>();
        var longTranslatorLabels = new HashSet<string>();
        var dlinesGroups = GG(REPEATED_POINT_DISTANCE.DLines);
        var dlinesGeos = GeoFac.GroupLinesByConnPoints(REPEATED_POINT_DISTANCE.DLines, DINOFLAGELLATES).ToList();
        var dlinesGeosf = F(dlinesGeos);
        var washingMachinesf = F(cadDataMain.WashingMachines);
        var wrappingPipesf = F(REPEATED_POINT_DISTANCE.WrappingPipes);
        {
          var labelsf = F(REPEATED_POINT_DISTANCE.Labels);
          var sidewaterbucket_x_indent = F(REPEATED_POINT_DISTANCE.VerticalPipes);
          foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in REPEATED_POINT_DISTANCE.Labels)
          {
            var repeated_point_distance = geoData.Labels[cadDataMain.Labels.IndexOf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)].Text;
            if (!IsDraiLabel(repeated_point_distance)) continue;
            var MinWellToUrinalDistance = labellinesGeosf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            if (MinWellToUrinalDistance.Count == THESAURUSHOUSING)
            {
              var labelline = MinWellToUrinalDistance[THESAURUSSTAMPEDE];
              var pipes = sidewaterbucket_x_indent(GeoFac.CreateGeometry(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, labelline));
              if (pipes.Count == THESAURUSSTAMPEDE)
              {
                var tolReturnValueRange = ExplodeGLineSegments(labelline);
                var points = GeoFac.GetLabelLineEndPoints(tolReturnValueRange.Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList(), MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, radius: THESAURUSCOMMUNICATION);
                if (points.Count == THESAURUSHOUSING)
                {
                  var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = points[THESAURUSSTAMPEDE];
                  if (!labelsf(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToNTSPoint()).Any())
                  {
                    var DEFAULT_FIRE_VALVE_WIDTH = GRect.Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, THESAURUSLUMBERING);
                    geoData.VerticalPipes.Add(DEFAULT_FIRE_VALVE_WIDTH);
                    var maxToiletToFloordrainDistance1 = DEFAULT_FIRE_VALVE_WIDTH.ToPolygon();
                    cadDataMain.VerticalPipes.Add(maxToiletToFloordrainDistance1);
                    REPEATED_POINT_DISTANCE.VerticalPipes.Add(maxToiletToFloordrainDistance1);
                    DrawTextLazy(THESAURUSJOBBER, maxToiletToFloordrainDistance1.GetCenter());
                  }
                }
              }
            }
          }
        }
        {
          var labelsf = F(REPEATED_POINT_DISTANCE.Labels);
          var sidewaterbucket_x_indent = F(REPEATED_POINT_DISTANCE.VerticalPipes);
          foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in REPEATED_POINT_DISTANCE.Labels)
          {
            var repeated_point_distance = geoData.Labels[cadDataMain.Labels.IndexOf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)].Text;
            if (!IsDraiLabel(repeated_point_distance)) continue;
            var MinWellToUrinalDistance = labellinesGeosf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
            if (MinWellToUrinalDistance.Count == THESAURUSHOUSING)
            {
              var labellinesGeo = MinWellToUrinalDistance[THESAURUSSTAMPEDE];
              if (labelsf(labellinesGeo).Count != THESAURUSHOUSING) continue;
              var tolReturnValueRange = ExplodeGLineSegments(labellinesGeo).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
              var DEFAULT_VOLTAGE = tolReturnValueRange.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).Cast<Geometry>().ToList();
              var maxDeviceplatformArea = F(DEFAULT_VOLTAGE);
              var tmp = maxDeviceplatformArea(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE).ToList();
              if (tmp.Count == THESAURUSHOUSING)
              {
                var l1 = tmp[THESAURUSSTAMPEDE];
                tmp = maxDeviceplatformArea(l1).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax != l1).ToList();
                if (tmp.Count == THESAURUSHOUSING)
                {
                  var l2 = tmp[THESAURUSSTAMPEDE];
                  if (tolReturnValueRange[DEFAULT_VOLTAGE.IndexOf(l2)].IsHorizontal(THESAURUSCOMMUNICATION))
                  {
                    tmp = maxDeviceplatformArea(l2).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax != l1 && TolLightRangeSingleSideMax != l2).ToList();
                    if (tmp.Count == THESAURUSHOUSING)
                    {
                      var l3 = tmp[THESAURUSSTAMPEDE];
                      var default_fire_valve_length = tolReturnValueRange[DEFAULT_VOLTAGE.IndexOf(l3)];
                      var _tol_avg_column_dist = new List<Point>() { default_fire_valve_length.StartPoint.ToNTSPoint(), default_fire_valve_length.EndPoint.ToNTSPoint() };
                      var _tmp = _tol_avg_column_dist.Except(GeoFac.CreateIntersectsSelector(_tol_avg_column_dist)(l2.Buffer(THESAURUSACRIMONIOUS, EndCapStyle.Square))).ToList();
                      if (_tmp.Count == THESAURUSHOUSING)
                      {
                        var ptGeo = _tmp[THESAURUSSTAMPEDE];
                        var pipes = sidewaterbucket_x_indent(ptGeo);
                        if (pipes.Count == THESAURUSHOUSING)
                        {
                          var pipe = pipes[THESAURUSSTAMPEDE];
                          if (!lbDict.ContainsKey(pipe))
                          {
                            lbDict[pipe] = repeated_point_distance;
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        foreach (var o in REPEATED_POINT_DISTANCE.LabelLines)
        {
          DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = THESAURUSHOUSING;
        }
        foreach (var maxToiletToFloordrainDistance1 in REPEATED_POINT_DISTANCE.Labels)
        {
          var tolReturnValueMinRange = geoData.Labels[cadDataMain.Labels.IndexOf(maxToiletToFloordrainDistance1)];
          var tolReturnValue0Approx = DrawTextLazy(tolReturnValueMinRange.Text, tolReturnValueMinRange.Boundary.LeftButtom.ToPoint3d());
          tolReturnValue0Approx.ColorIndex = THESAURUSPERMUTATION;
          var _pl = DrawRectLazy(tolReturnValueMinRange.Boundary);
          _pl.ColorIndex = THESAURUSPERMUTATION;
        }
        foreach (var o in REPEATED_POINT_DISTANCE.PipeKillers)
        {
          DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(THESAURUSEXCESS, THESAURUSEXCESS, THESAURUSLUMBERING);
        }
        foreach (var o in REPEATED_POINT_DISTANCE.WashingMachines)
        {
          DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], THESAURUSACRIMONIOUS);
        }
        foreach (var o in REPEATED_POINT_DISTANCE.VerticalPipes)
        {
          DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = INTROPUNITIVENESS;
        }
        foreach (var o in REPEATED_POINT_DISTANCE.FloorDrains)
        {
          DrawGeometryLazy(o, MAX_TAG_YPOSITION => MAX_TAG_YPOSITION.ForEach(tolReturnValue0Approx => tolReturnValue0Approx.ColorIndex = SUPERLATIVENESS));
        }
        foreach (var o in REPEATED_POINT_DISTANCE.WaterPorts)
        {
          DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = THESAURUSDESTITUTE;
          DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
        }
        foreach (var o in REPEATED_POINT_DISTANCE.WashingMachines)
        {
          var tolReturnValue0Approx = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = THESAURUSHOUSING;
        }
        foreach (var o in REPEATED_POINT_DISTANCE.DownWaterPorts)
        {
          DrawRectLazy(geoData.DownWaterPorts[cadDataMain.DownWaterPorts.IndexOf(o)]);
        }
        {
          var cl = Color.FromRgb(THESAURUSDELIGHT, THESAURUSCRADLE, HYPOSTASIZATION);
          foreach (var o in REPEATED_POINT_DISTANCE.WrappingPipes)
          {
            var tolReturnValue0Approx = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
            tolReturnValue0Approx.Color = cl;
          }
        }
        {
          var cl = Color.FromRgb(QUOTATIONEDIBLE, SYNTHLIBORAMPHUS, THESAURUSPRIVATE);
          foreach (var o in REPEATED_POINT_DISTANCE.DLines)
          {
            var tolReturnValue0Approx = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
          }
        }
        {
          {
            var ok_ents = new HashSet<Geometry>();
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE++)
            {
              var SidewaterbucketXIndent = INTRAVASCULARLY;
              var labelsf = F(REPEATED_POINT_DISTANCE.Labels.Except(ok_ents).ToList());
              var sidewaterbucket_x_indent = F(REPEATED_POINT_DISTANCE.VerticalPipes.Except(ok_ents).ToList());
              foreach (var labelLinesGeo in labelLinesGeos)
              {
                var max_kitchen_to_balcony_distance = labelsf(labelLinesGeo);
                var pipes = sidewaterbucket_x_indent(labelLinesGeo);
                if (max_kitchen_to_balcony_distance.Count == THESAURUSHOUSING && pipes.Count == THESAURUSHOUSING)
                {
                  var SidewaterbucketYIndent = max_kitchen_to_balcony_distance[THESAURUSSTAMPEDE];
                  var ToiletWellsInterval = pipes[THESAURUSSTAMPEDE];
                  var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = geoData.Labels[cadDataMain.Labels.IndexOf(SidewaterbucketYIndent)].Text ?? THESAURUSDEPLORE;
                  if (IsMaybeLabelText(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                  {
                    lbDict[ToiletWellsInterval] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                    ok_ents.Add(ToiletWellsInterval);
                    ok_ents.Add(SidewaterbucketYIndent);
                    SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  }
                  else if (IsNotedLabel(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                  {
                    notedPipesDict[ToiletWellsInterval] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                    ok_ents.Add(SidewaterbucketYIndent);
                    SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  }
                }
              }
              if (!SidewaterbucketXIndent) break;
            }
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE++)
            {
              var SidewaterbucketXIndent = INTRAVASCULARLY;
              var labelsf = F(REPEATED_POINT_DISTANCE.Labels.Except(ok_ents).ToList());
              var sidewaterbucket_x_indent = F(REPEATED_POINT_DISTANCE.VerticalPipes.Except(ok_ents).ToList());
              foreach (var labelLinesGeo in labelLinesGeos)
              {
                var max_kitchen_to_balcony_distance = labelsf(labelLinesGeo);
                var pipes = sidewaterbucket_x_indent(labelLinesGeo);
                if (max_kitchen_to_balcony_distance.Count == pipes.Count && max_kitchen_to_balcony_distance.Count > THESAURUSSTAMPEDE)
                {
                  var labelsTxts = max_kitchen_to_balcony_distance.Select(SidewaterbucketYIndent => geoData.Labels[cadDataMain.Labels.IndexOf(SidewaterbucketYIndent)].Text ?? THESAURUSDEPLORE).ToList();
                  if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                  {
                    pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                    max_kitchen_to_balcony_distance = ThRainSystemService.SortGeometrysBy2DSpacePosition(max_kitchen_to_balcony_distance).ToList();
                    for (int RADIAN_TOLERANCE = THESAURUSSTAMPEDE; RADIAN_TOLERANCE < pipes.Count; RADIAN_TOLERANCE++)
                    {
                      var ToiletWellsInterval = pipes[RADIAN_TOLERANCE];
                      var SidewaterbucketYIndent = max_kitchen_to_balcony_distance[RADIAN_TOLERANCE];
                      var MAX_ANGLE_TOLLERANCE = cadDataMain.Labels.IndexOf(SidewaterbucketYIndent);
                      var tolReturnValueMinRange = geoData.Labels[MAX_ANGLE_TOLLERANCE];
                      var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = tolReturnValueMinRange.Text;
                      lbDict[ToiletWellsInterval] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                    }
                    ok_ents.AddRange(pipes);
                    ok_ents.AddRange(max_kitchen_to_balcony_distance);
                    SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  }
                }
              }
              if (!SidewaterbucketXIndent) break;
            }
            {
              foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in REPEATED_POINT_DISTANCE.Labels.Except(ok_ents).ToList())
              {
                var SidewaterbucketYIndent = geoData.Labels[cadDataMain.Labels.IndexOf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE)].Text ?? THESAURUSDEPLORE;
                if (!IsMaybeLabelText(SidewaterbucketYIndent)) continue;
                var MinWellToUrinalDistance = labellinesGeosf(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                if (MinWellToUrinalDistance.Count == THESAURUSHOUSING)
                {
                  var labelline = MinWellToUrinalDistance[THESAURUSSTAMPEDE];
                  var tolReturnValueRange = ExplodeGLineSegments(labelline);
                  var points = GeoFac.GetLabelLineEndPoints(tolReturnValueRange, MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                  if (points.Count == THESAURUSHOUSING)
                  {
                    var pipes = F(REPEATED_POINT_DISTANCE.VerticalPipes.Except(lbDict.Keys).ToList())(points[THESAURUSSTAMPEDE].ToNTSPoint());
                    if (pipes.Count == THESAURUSHOUSING)
                    {
                      var ToiletWellsInterval = pipes[THESAURUSSTAMPEDE];
                      lbDict[ToiletWellsInterval] = SidewaterbucketYIndent;
                      ok_ents.Add(ToiletWellsInterval);
                      ok_ents.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
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
            foreach (var pipe in REPEATED_POINT_DISTANCE.VerticalPipes) if (lbDict.ContainsKey(pipe)) pipes1.Add(pipe); else pipes2.Add(pipe);
            return (pipes1, pipes2);
          }
          {
            bool recognise1()
            {
              var SidewaterbucketXIndent = INTRAVASCULARLY;
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE++)
              {
                var (pipes1, pipes2) = getPipes();
                var pipes1f = F(pipes1);
                var pipes2f = F(pipes2);
                foreach (var dlinesGeo in dlinesGeos)
                {
                  var lst1 = pipes1f(dlinesGeo);
                  var lst2 = pipes2f(dlinesGeo);
                  if (lst1.Count == THESAURUSHOUSING && lst2.Count > THESAURUSSTAMPEDE)
                  {
                    var pp1 = lst1[THESAURUSSTAMPEDE];
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = lbDict[pp1];
                    var maxBalconyrainpipeToFloordrainDistance = pp1.GetCenter();
                    foreach (var pp2 in lst2)
                    {
                      var BlockScaleNum = maxBalconyrainpipeToFloordrainDistance.GetDistanceTo(pp2.GetCenter());
                      if (THESAURUSACRIMONIOUS < BlockScaleNum && BlockScaleNum <= MAX_SHORTTRANSLATOR_DISTANCE)
                      {
                        if (!IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                        {
                          shortTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                          lbDict[pp2] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                          SidewaterbucketXIndent = THESAURUSOBSTINACY;
                        }
                      }
                      else if (BlockScaleNum > MAX_SHORTTRANSLATOR_DISTANCE)
                      {
                        longTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                        lbDict[pp2] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                        SidewaterbucketXIndent = THESAURUSOBSTINACY;
                      }
                    }
                  }
                }
                if (!SidewaterbucketXIndent) break;
              }
              return SidewaterbucketXIndent;
            }
            bool recognise2()
            {
              var SidewaterbucketXIndent = INTRAVASCULARLY;
              for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE++)
              {
                var (pipes1, pipes2) = getPipes();
                var pipes1f = F(pipes1);
                foreach (var pp2 in pipes2)
                {
                  var pps1 = pipes1f(pp2.ToGRect().Expand(THESAURUSCOMMUNICATION).ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS));
                  var maxToiletToFloordrainDistance = new List<Action>();
                  foreach (var pp1 in pps1)
                  {
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = lbDict[pp1];
                    if (!IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > THESAURUSHOUSING)
                      {
                        maxToiletToFloordrainDistance.Add(() =>
                        {
                          shortTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                          lbDict[pp2] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                          SidewaterbucketXIndent = THESAURUSOBSTINACY;
                        });
                      }
                    }
                  }
                  if (maxToiletToFloordrainDistance.Count == THESAURUSHOUSING) maxToiletToFloordrainDistance[THESAURUSSTAMPEDE]();
                }
                if (!SidewaterbucketXIndent) break;
              }
              return SidewaterbucketXIndent;
            }
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < INTROPUNITIVENESS; MAX_ANGEL_TOLLERANCE++)
            {
              if (!(recognise1() && recognise2())) break;
            }
          }
        }
        string getLabel(Geometry pipe)
        {
          lbDict.TryGetValue(pipe, out string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
          return MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
        }
        {
          var maxDeviceplatformArea = F(REPEATED_POINT_DISTANCE.VerticalPipes);
          foreach (var dlinesGeo in dlinesGeos)
          {
            var pipes = maxDeviceplatformArea(dlinesGeo);
            var d = pipes.Select(getLabel).Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax != null).ToCountDict();
            foreach (var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE in d.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Value > THESAURUSHOUSING).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key))
            {
              var pps = pipes.Where(_raiseDistanceToStartDefault => getLabel(_raiseDistanceToStartDefault) == MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE).ToList();
              if (pps.Count == THESAURUSPERMUTATION)
              {
                var BlockScaleNum = pps[THESAURUSSTAMPEDE].GetCenter().GetDistanceTo(pps[THESAURUSHOUSING].GetCenter());
                if (THESAURUSACRIMONIOUS < BlockScaleNum && BlockScaleNum <= MAX_SHORTTRANSLATOR_DISTANCE)
                {
                  if (!IsTL(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                  {
                    shortTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                  }
                }
                else if (BlockScaleNum > MAX_SHORTTRANSLATOR_DISTANCE)
                {
                  longTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
                }
              }
              else
              {
                longTranslatorLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
              }
            }
          }
        }
        {
          var ok_ents = new HashSet<Geometry>();
          var outletd = new Dictionary<string, string>();
          var outletWrappingPipe = new Dictionary<int, string>();
          var portd = new Dictionary<Geometry, string>();
          {
            void collect(Func<Geometry, List<Geometry>> waterPortsf, Func<Geometry, string> getWaterPortLabel)
            {
              var _tol_inter_filter = F(REPEATED_POINT_DISTANCE.VerticalPipes.Except(ok_ents).ToList());
              foreach (var dlinesGeo in dlinesGeos)
              {
                var waterPorts = waterPortsf(dlinesGeo);
                if (waterPorts.Count == THESAURUSHOUSING)
                {
                  var waterPort = waterPorts[THESAURUSSTAMPEDE];
                  var waterPortLabel = getWaterPortLabel(waterPort);
                  portd[dlinesGeo] = waterPortLabel;
                  var pipes = _tol_inter_filter(dlinesGeo);
                  ok_ents.AddRange(pipes);
                  foreach (var pipe in pipes)
                  {
                    if (lbDict.TryGetValue(pipe, out string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      outletd[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = waterPortLabel;
                      var wrappingpipes = wrappingPipesf(dlinesGeo);
                      if (wrappingpipes.Count > THESAURUSSTAMPEDE)
                      {
                      }
                      foreach (var wp in wrappingpipes)
                      {
                        outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                        portd[wp] = waterPortLabel;
                        DrawTextLazy(waterPortLabel, wp.GetCenter());
                      }
                    }
                  }
                }
              }
            }
            collect(F(REPEATED_POINT_DISTANCE.WaterPorts), waterPort => geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)]);
            {
              var spacialIndex = REPEATED_POINT_DISTANCE.WaterPorts.Select(cadDataMain.WaterPorts).ToList();
              var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Expand(THESAURUSDOMESTIC).ToPolygon()).Cast<Geometry>().ToList();
              collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
            }
          }
          {
            var _tol_inter_filter = F(REPEATED_POINT_DISTANCE.VerticalPipes.Except(ok_ents).ToList());
            var radius = THESAURUSACRIMONIOUS;
            var _block_scale_num = GeoFac.NearestNeighbourPoint3dF(REPEATED_POINT_DISTANCE.WaterPorts);
            foreach (var dlinesGeo in dlinesGeos)
            {
              var TolLane = ExplodeGLineSegments(dlinesGeo);
              var _tol_avg_column_dist = GeoFac.GetAlivePoints(TolLane.Distinct().ToList(), radius: radius);
              {
                var _pts = _tol_avg_column_dist.Select(TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax, radius).ToCirclePolygon(SUPERLATIVENESS, INTRAVASCULARLY)).ToGeometryList();
                var killer = GeoFac.CreateGeometryEx(REPEATED_POINT_DISTANCE.VerticalPipes.Concat(REPEATED_POINT_DISTANCE.WaterPorts).Concat(REPEATED_POINT_DISTANCE.FloorDrains).Distinct().ToList());
                _tol_avg_column_dist = _tol_avg_column_dist.Except(F(_pts)(killer).Select(_pts).ToList(_tol_avg_column_dist)).ToList();
              }
              foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in _tol_avg_column_dist)
              {
                var waterPort = _block_scale_num(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint3d());
                if (waterPort != null)
                {
                  if (waterPort.GetCenter().GetDistanceTo(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN) <= THESAURUSDICTATORIAL)
                  {
                    var waterPortLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                    portd[dlinesGeo] = waterPortLabel;
                    foreach (var pipe in _tol_inter_filter(dlinesGeo))
                    {
                      if (lbDict.TryGetValue(pipe, out string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                      {
                        outletd[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = waterPortLabel;
                        ok_ents.Add(pipe);
                        var wrappingpipes = wrappingPipesf(dlinesGeo);
                        if (wrappingpipes.Any())
                        {
                        }
                        foreach (var wp in wrappingpipes)
                        {
                          outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
                          portd[wp] = waterPortLabel;
                          DrawTextLazy(waterPortLabel, wp.GetCenter());
                        }
                      }
                    }
                  }
                }
              }
            }
          }
          {
            var wpf = F(REPEATED_POINT_DISTANCE.WrappingPipes.Where(wp => !portd.ContainsKey(wp)).ToList());
            foreach (var dlinesGeo in dlinesGeos)
            {
              if (!portd.TryGetValue(dlinesGeo, out string MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)) continue;
              foreach (var wp in wpf(dlinesGeo))
              {
                if (!portd.ContainsKey(wp))
                {
                  portd[wp] = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                }
              }
            }
            {
              var points = geoData.WrappingPipeRadius.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key).ToList();
              var _tol_avg_column_dist = points.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()).ToList();
              var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
              foreach (var wp in REPEATED_POINT_DISTANCE.WrappingPipes)
              {
                var _pts = ptsf(wp.Buffer(THESAURUSENTREPRENEUR));
                if (_pts.Count > THESAURUSSTAMPEDE)
                {
                  var max_rainpipe_to_balconyfloordrain = geoData.WrappingPipeRadius[_tol_avg_column_dist.IndexOf(_pts[THESAURUSSTAMPEDE])];
                  var radiusText = max_rainpipe_to_balconyfloordrain.Value;
                  if (string.IsNullOrWhiteSpace(radiusText)) radiusText = THESAURUSCROUCH;
                  drData.OutletWrappingPipeRadiusStringDict[cadDataMain.WrappingPipes.IndexOf(wp)] = radiusText;
                }
              }
            }
          }
          {
            var sidewaterbucket_x_indent = F(REPEATED_POINT_DISTANCE.VerticalPipes);
            foreach (var wp in REPEATED_POINT_DISTANCE.WrappingPipes)
            {
              if (portd.TryGetValue(wp, out string MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN))
              {
                var pipes = sidewaterbucket_x_indent(wp);
                foreach (var pipe in pipes)
                {
                  if (lbDict.TryGetValue(pipe, out string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                  {
                    if (!outletd.ContainsKey(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE))
                    {
                      outletd[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                    }
                  }
                }
              }
            }
          }
          {
            drData.ShortTranslatorLabels.AddRange(shortTranslatorLabels);
            drData.LongTranslatorLabels.AddRange(longTranslatorLabels);
            drData.Outlets = outletd;
            outletd.Join(lbDict, max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Key, max_rainpipe_to_balconyfloordrain => max_rainpipe_to_balconyfloordrain.Value, (kv1, kv2) =>
            {
              var num = kv1.Value;
              var pipe = kv2.Key;
              DrawTextLazy(num, pipe.ToGRect().RightButtom);
              return THESAURUSITEMIZE;
            }).Count();
          }
          {
            var _tol_avg_column_dist = lbDict.Where(TolLightRangeSingleSideMax => IsDraiLabel(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key.GetCenter().ToNTSPoint().Tag(TolLightRangeSingleSideMax.Key)).ToList();
            var ptsf = GeoFac.CreateIntersectsSelector(_tol_avg_column_dist);
            foreach (var default_fire_valve_width in geoData.Groups)
            {
              var DEFAULT_FIRE_VALVE_LENGTH = default_fire_valve_width.ToGeometry();
              if (wrappingPipesf(DEFAULT_FIRE_VALVE_LENGTH).Any())
              {
                foreach (var MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN in ptsf(DEFAULT_FIRE_VALVE_LENGTH))
                {
                  drData.HasOutletWrappingPipe.Add(lbDict[MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.UserData as Geometry]);
                }
              }
            }
          }
          {
            drData.OutletWrappingPipeDict = outletWrappingPipe;
          }
        }
        {
          var Commonradius = F(REPEATED_POINT_DISTANCE.FloorDrains);
          var MaxDownspoutToBalconywashingfloordrain = new List<Geometry>();
          foreach (var max_rainpipe_to_balconyfloordrain in lbDict)
          {
            if (IsFL0(max_rainpipe_to_balconyfloordrain.Value))
            {
              MaxDownspoutToBalconywashingfloordrain.Add(max_rainpipe_to_balconyfloordrain.Key);
            }
          }
          var ok_vpipes = new HashSet<Geometry>();
          var outletd = new Dictionary<string, string>();
          var waterWellIdDict = new Dictionary<string, int>();
          var rainPortIdDict = new Dictionary<string, int>();
          var waterWellsIdDict = new Dictionary<Geometry, int>();
          var waterWellsLabelDict = new Dictionary<Geometry, string>();
          var outletWrappingPipe = new Dictionary<int, string>();
          var hasRainPortSymbols = new HashSet<string>();
        }
        {
          foreach (var ToiletWellsInterval in REPEATED_POINT_DISTANCE.VerticalPipes)
          {
            if (!lbDict.ContainsKey(ToiletWellsInterval))
            {
              lbDict[ToiletWellsInterval] = null;
            }
          }
        }
        {
          var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(REPEATED_POINT_DISTANCE.VerticalPipes);
          foreach (var ToiletWellsInterval in REPEATED_POINT_DISTANCE.VerticalPipes)
          {
            if (string.IsNullOrEmpty(lbDict[ToiletWellsInterval]))
            {
              var pps = maxDeviceplatformArea(ToiletWellsInterval.ToGRect().Expand(THESAURUSENTREPRENEUR).ToPolygon()).Where(TolLightRangeSingleSideMax => IsWantedLabelText(lbDict[TolLightRangeSingleSideMax])).ToList();
              if (pps.Count == THESAURUSHOUSING)
              {
                lbDict[ToiletWellsInterval] = lbDict[pps[THESAURUSSTAMPEDE]];
                drData.ShortTranslatorLabels.Add(lbDict[pps[THESAURUSSTAMPEDE]]);
              }
            }
          }
        }
        foreach (var ToiletWellsInterval in REPEATED_POINT_DISTANCE.VerticalPipes)
        {
          lbDict.TryGetValue(ToiletWellsInterval, out string MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
          if (MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE != null)
          {
            DrawTextLazy(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, ToiletWellsInterval.ToGRect().LeftTop.ToPoint3d());
            drData.VerticalPipeLabels.Add(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE);
          }
        }
        exItem.LabelDict = lbDict.Select(TolLightRangeSingleSideMax => new Tuple<Geometry, string>(TolLightRangeSingleSideMax.Key, TolLightRangeSingleSideMax.Value)).ToList();
        if (geoData.StoreyItems[si].Labels?.Contains(THESAURUSREGION) ?? INTRAVASCULARLY)
        {
          for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < geoData.StoreyItems.Count; MAX_ANGEL_TOLLERANCE++)
          {
            if ((geoData.StoreyItems[MAX_ANGEL_TOLLERANCE].Labels?.Contains(HYDROCHLOROFLUOROCARBON) ?? INTRAVASCULARLY) && MAX_ANGEL_TOLLERANCE != si)
            {
              var MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = geoData.StoreyInfos[MAX_ANGEL_TOLLERANCE].ContraPoint - geoData.StoreyInfos[si].ContraPoint;
              var r1 = geoData.StoreyInfos[si].Boundary.ToPolygon();
              var r2 = geoData.StoreyInfos[MAX_ANGEL_TOLLERANCE].Boundary.ToPolygon();
              var cPts = circlePtsf(r1);
              var ccs = cPts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.UserData).Cast<GCircle>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(SUPERLATIVENESS).Tag(TolLightRangeSingleSideMax)).ToList();
              var max_basecircle_area = GeoFac.CreateContainsSelector(geoData.FloorDrainRings.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToCirclePolygon(SUPERLATIVENESS).Tag(TolLightRangeSingleSideMax)).ToList())(r2).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Offset(-MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN));
              var fdPtsf = GeoFac.CreateIntersectsSelector(max_basecircle_area.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GetCenter().ToNTSPoint().Tag(TolLightRangeSingleSideMax)).ToList());
              var lbPtsf = GeoFac.CreateIntersectsSelector(lbDict.Where(TolLightRangeSingleSideMax => IsFL(TolLightRangeSingleSideMax.Value) && !IsFL0(TolLightRangeSingleSideMax.Value)).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Key.GetCenter().ToNTSPoint().Tag(TolLightRangeSingleSideMax.Value)).ToList());
              var bufs = GeoFac.GroupGeometries(REPEATED_POINT_DISTANCE.DLines.Concat(ccs).Concat(max_basecircle_area).ToList()).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGeometry().Buffer(THESAURUSHOUSING)).ToList();
              foreach (var buf in bufs)
              {
                var lbPt = lbPtsf(buf).FirstOrDefault();
                if (lbPt is not null)
                {
                  var SidewaterbucketYIndent = lbPt.UserData as string;
                  var fdPts = fdPtsf(buf).Where(TolLightRangeSingleSideMax => ccs.Any(maxBalconyrainpipeToFloordrainDistance => maxBalconyrainpipeToFloordrainDistance.Intersects(TolLightRangeSingleSideMax))).ToList();
                  if (fdPts.Count > THESAURUSSTAMPEDE)
                  {
                    drData.FdsCountAt2F[SidewaterbucketYIndent] = fdPts.Count;
                  }
                }
              }
            }
          }
        }
        var FLs = new List<Geometry>();
        var FL0s = new List<Geometry>();
        var max_tag_xposition = new List<Geometry>();
        foreach (var max_rainpipe_to_balconyfloordrain in lbDict)
        {
          if (IsFL(max_rainpipe_to_balconyfloordrain.Value) && !IsFL0(max_rainpipe_to_balconyfloordrain.Value))
          {
            FLs.Add(max_rainpipe_to_balconyfloordrain.Key);
          }
          else if (IsFL0(max_rainpipe_to_balconyfloordrain.Value))
          {
            FL0s.Add(max_rainpipe_to_balconyfloordrain.Key);
          }
          else if (IsPL(max_rainpipe_to_balconyfloordrain.Value))
          {
            max_tag_xposition.Add(max_rainpipe_to_balconyfloordrain.Key);
          }
        }
        {
          var toiletPls = new HashSet<string>();
          var plsf = F(max_tag_xposition);
          foreach (var _toilet in toilets)
          {
            var toilet = _toilet.Buffer(THESAURUSDERELICTION);
            foreach (var maxToiletToFloordrainDistance1 in plsf(toilet))
            {
              toiletPls.Add(lbDict[maxToiletToFloordrainDistance1]);
            }
          }
          drData.ToiletPls.AddRange(toiletPls);
        }
        {
          var kitchenFls = new HashSet<string>();
          var balconyFls = new HashSet<string>();
          var ok_fls = new HashSet<Geometry>();
          var ok_rooms = new HashSet<Geometry>();
          var flsf = F(FLs);
          foreach (var kitchen in kitchens)
          {
            if (ok_rooms.Contains(kitchen)) continue;
            foreach (var fl in flsf(kitchen))
            {
              if (ok_fls.Contains(fl)) continue;
              ok_fls.Add(fl);
              kitchenFls.Add(lbDict[fl]);
              ok_rooms.Add(kitchen);
            }
          }
          foreach (var bal in balconies)
          {
            if (ok_rooms.Contains(bal)) continue;
            foreach (var fl in flsf(bal))
            {
              if (ok_fls.Contains(fl)) continue;
              ok_fls.Add(fl);
              balconyFls.Add(lbDict[fl]);
              ok_rooms.Add(bal);
            }
          }
          for (double buf = HYPERDISYLLABLE; buf <= THESAURUSDERELICTION; buf += HYPERDISYLLABLE)
          {
            foreach (var kitchen in kitchens)
            {
              if (ok_rooms.Contains(kitchen)) continue;
              var SidewaterbucketXIndent = INTRAVASCULARLY;
              foreach (var toilet in toiletsf(kitchen.Buffer(buf)))
              {
                if (ok_rooms.Contains(toilet))
                {
                  SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  break;
                }
                foreach (var fl in flsf(toilet))
                {
                  SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  ok_fls.Add(fl);
                  ok_rooms.Add(toilet);
                  kitchenFls.Add(lbDict[fl]);
                }
              }
              if (SidewaterbucketXIndent)
              {
                ok_rooms.Add(kitchen);
                continue;
              }
              foreach (var fl in flsf(kitchen.Buffer(buf)))
              {
                if (ok_fls.Contains(fl)) continue;
                ok_fls.Add(fl);
                kitchenFls.Add(lbDict[fl]);
                ok_rooms.Add(kitchen);
              }
            }
            foreach (var bal in balconies)
            {
              if (ok_rooms.Contains(bal)) continue;
              foreach (var fl in flsf(bal.Buffer(buf)))
              {
                if (ok_fls.Contains(fl)) continue;
                ok_fls.Add(fl);
                balconyFls.Add(lbDict[fl]);
                ok_rooms.Add(bal);
              }
            }
          }
          drData.KitchenFls.AddRange(kitchenFls);
          drData.BalconyFls.AddRange(balconyFls);
        }
        {
          var flsf = F(FLs);
          var filtedFds = REPEATED_POINT_DISTANCE.FloorDrains.Where(fd => toilets.All(toilet => !toilet.Intersects(fd))).ToList();
          var Commonradius = F(filtedFds);
          foreach (var lineEx in GeoFac.GroupGeometries(REPEATED_POINT_DISTANCE.FloorDrains.OfType<Polygon>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Shell)
              .Concat(FLs.OfType<Polygon>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Shell))
              .Concat(dlinesGeos).Distinct().ToList()).Select(TolLightRangeSingleSideMax => GeoFac.CreateGeometry(TolLightRangeSingleSideMax)))
          {
            var text_indent = flsf(lineEx);
            foreach (var fl in text_indent)
            {
              var max_basecircle_area = Commonradius(lineEx);
              drData.FloorDrains[lbDict[fl]] = max_basecircle_area.Count;
              var washingMachineFds = new List<Geometry>();
              var shooters = geoData.FloorDrainTypeShooter.Select(max_rainpipe_to_balconyfloordrain =>
              {
                var DEFAULT_FIRE_VALVE_LENGTH = GRect.Create(max_rainpipe_to_balconyfloordrain.Key, HYPERDISYLLABLE).ToPolygon();
                DEFAULT_FIRE_VALVE_LENGTH.UserData = max_rainpipe_to_balconyfloordrain.Value;
                return DEFAULT_FIRE_VALVE_LENGTH;
              }).ToList();
              var WellsMaxArea = GeoFac.CreateIntersectsSelector(shooters);
              foreach (var fd in max_basecircle_area)
              {
                var SidewaterbucketXIndent = INTRAVASCULARLY;
                foreach (var DEFAULT_FIRE_VALVE_LENGTH in WellsMaxArea(fd))
                {
                  var toilet_buffer_distance = (string)DEFAULT_FIRE_VALVE_LENGTH.UserData;
                  if (!string.IsNullOrWhiteSpace(toilet_buffer_distance))
                  {
                    if (toilet_buffer_distance.Contains(THESAURUSRESIGNED) || toilet_buffer_distance.Contains(PHOTOAUTOTROPHIC))
                    {
                      SidewaterbucketXIndent = THESAURUSOBSTINACY;
                      break;
                    }
                  }
                }
                if (!SidewaterbucketXIndent)
                {
                  if (washingMachinesf(fd).Any())
                  {
                    SidewaterbucketXIndent = THESAURUSOBSTINACY;
                  }
                }
                if (SidewaterbucketXIndent)
                {
                  washingMachineFds.Add(fd);
                }
              }
              drData.WashingMachineFloorDrains[lbDict[fl]] = washingMachineFds.Count;
              if (max_basecircle_area.Count == THESAURUSPERMUTATION)
              {
                bool is4tune;
                bool isShunt()
                {
                  is4tune = INTRAVASCULARLY;
                  var _dlines = dlinesGeosf(fl);
                  if (_dlines.Count == THESAURUSSTAMPEDE) return INTRAVASCULARLY;
                  if (max_basecircle_area.Count == THESAURUSPERMUTATION)
                    {
                      try
                      {
                        var aaa = max_basecircle_area.YieldAfter(fl).Distinct().ToList();
                        var jjj = GeoFac.CreateGeometry(aaa);
                        var bbb = dlinesGeosf(jjj).Select(TolLightRangeSingleSideMax => GeoFac.GetLines(TolLightRangeSingleSideMax)).SelectMany(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax).Distinct().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString());
                        var ccc = GeoFac.CreateGeometry(bbb).Difference(jjj);
                        var ddd = GeoFac.GetLines(ccc).ToList();
                        var xxx = GeoFac.ToNodedLineSegments(ddd).Distinct().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString()).ToList();
                        var yyy = GeoFac.GroupGeometries(xxx).Select(DEFAULT_VOLTAGE => GeoFac.CreateGeometry(DEFAULT_VOLTAGE)).ToList();
                        if (yyy.Count == THESAURUSHOUSING)
                        {
                          var dlines = yyy[THESAURUSSTAMPEDE];
                          if (dlines.Intersects(max_basecircle_area[THESAURUSSTAMPEDE].Buffer(THESAURUSCOMMUNICATION)) && dlines.Intersects(max_basecircle_area[THESAURUSHOUSING].Buffer(THESAURUSCOMMUNICATION)) && dlines.Intersects(fl.Buffer(THESAURUSCOMMUNICATION)))
                          {
                            if (wrappingPipesf(dlines).Count >= THESAURUSPERMUTATION)
                            {
                              is4tune = THESAURUSOBSTINACY;
                            }
                            return INTRAVASCULARLY;
                          }
                        }
                        else if (yyy.Count == THESAURUSPERMUTATION)
                        {
                          var dl1 = yyy[THESAURUSSTAMPEDE];
                          var dl2 = yyy[THESAURUSHOUSING];
                          var fd1 = max_basecircle_area[THESAURUSSTAMPEDE].Buffer(THESAURUSCOMMUNICATION);
                          var fd2 = max_basecircle_area[THESAURUSHOUSING].Buffer(THESAURUSCOMMUNICATION);
                          var toilet_wells_interval = fl.Buffer(THESAURUSCOMMUNICATION);
                          var DEFAULT_VOLTAGE = new List<Geometry>() { fd1, fd2, toilet_wells_interval };
                          var maxDeviceplatformArea = F(DEFAULT_VOLTAGE);
                          var l1 = maxDeviceplatformArea(dl1);
                          var l2 = maxDeviceplatformArea(dl2);
                          if (l1.Count == THESAURUSPERMUTATION && l2.Count == THESAURUSPERMUTATION && l1.Contains(toilet_wells_interval) && l2.Contains(toilet_wells_interval))
                          {
                            return THESAURUSOBSTINACY;
                          }
                          return INTRAVASCULARLY;
                        }
                      }
                      catch
                      {
                        return INTRAVASCULARLY;
                      }
                    }
                  return INTRAVASCULARLY;
                }
                if (isShunt())
                {
                  drData.Shunts.Add(lbDict[fl]);
                  if (is4tune)
                  {
                    drData._4tunes.Add(lbDict[fl]);
                  }
                }
              }
            }
          }
          {
            {
              var supterDLineGeos = GeoFac.GroupGeometries(dlinesGeos.Concat(filtedFds.OfType<Polygon>().Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Shell)).ToList()).Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Count == THESAURUSHOUSING ? TolLightRangeSingleSideMax[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(TolLightRangeSingleSideMax)).ToList();
              var maxDeviceplatformArea = F(supterDLineGeos);
              foreach (var wp in REPEATED_POINT_DISTANCE.WaterPorts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGRect().Expand(HYPERDISYLLABLE).ToPolygon().Shell))
              {
                var dls = maxDeviceplatformArea(wp);
                var dlgeo = GeoFac.CreateGeometry(dls);
                var max_basecircle_area = Commonradius(dlgeo);
                var maxBalconyrainpipeToFloordrainDistance = max_basecircle_area.Count;
                {
                  var _fls = flsf(dlgeo).Where(fl => toilets.All(toilet => !toilet.Intersects(fl))).ToList();
                  foreach (var fl in _fls)
                  {
                    var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = lbDict[fl];
                    drData.SingleOutletFloorDrains.TryGetValue(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                    MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = Math.Max(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, maxBalconyrainpipeToFloordrainDistance);
                    drData.SingleOutletFloorDrains[MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE] = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
                  }
                }
              }
            }
            {
            }
          }
        }
        {
        }
        {
          {
            var xls = FLs;
            var xlsf = F(xls);
            var TolLaneProtect = GeoFac.GroupGeometries(xls.Concat(REPEATED_POINT_DISTANCE.DownWaterPorts).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(dlinesGeos).ToList())
                .Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Count == THESAURUSHOUSING ? TolLightRangeSingleSideMax[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(TolLightRangeSingleSideMax)).ToList();
            foreach (var default_fire_valve_width in TolLaneProtect)
            {
              var MaxDownspoutToBalconywashingfloordrain = xlsf(default_fire_valve_width);
              if (MaxDownspoutToBalconywashingfloordrain.Count > THESAURUSSTAMPEDE)
              {
                var SidewaterbucketYIndent = lbDict[MaxDownspoutToBalconywashingfloordrain.First()];
                drData.tolGroupBlkLaneHead.TryGetValue(SidewaterbucketYIndent, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                drData.tolGroupBlkLaneHead[SidewaterbucketYIndent] = Math.Max(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, MaxDownspoutToBalconywashingfloordrain.Count);
              }
            }
          }
          {
            var xls = max_tag_xposition;
            var xlsf = F(xls);
            var TolLaneProtect = GeoFac.GroupGeometries(xls.Concat(REPEATED_POINT_DISTANCE.DownWaterPorts).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(dlinesGeos).ToList())
                .Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Count == THESAURUSHOUSING ? TolLightRangeSingleSideMax[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(TolLightRangeSingleSideMax)).ToList();
            foreach (var default_fire_valve_width in TolLaneProtect)
            {
              var MaxDownspoutToBalconywashingfloordrain = xlsf(default_fire_valve_width);
              if (MaxDownspoutToBalconywashingfloordrain.Count > THESAURUSSTAMPEDE)
              {
                var SidewaterbucketYIndent = lbDict[MaxDownspoutToBalconywashingfloordrain.First()];
                drData.tolGroupBlkLaneHead.TryGetValue(SidewaterbucketYIndent, out int MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN);
                drData.tolGroupBlkLaneHead[SidewaterbucketYIndent] = Math.Max(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, MaxDownspoutToBalconywashingfloordrain.Count);
              }
            }
          }
        }
        drDatas.Add(drData);
      }
      logString = sb.ToString();
      extraInfo.drDatas = drDatas;
      return extraInfo;
    }
    public static Geometry CreateXGeoRect(GRect DEFAULT_FIRE_VALVE_WIDTH)
    {
      return new MultiLineString(new LineString[] {
                DEFAULT_FIRE_VALVE_WIDTH.ToLinearRing(),
                new LineString(new Coordinate[] { DEFAULT_FIRE_VALVE_WIDTH.LeftTop.ToNTSCoordinate(), DEFAULT_FIRE_VALVE_WIDTH.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { DEFAULT_FIRE_VALVE_WIDTH.LeftButtom.ToNTSCoordinate(), DEFAULT_FIRE_VALVE_WIDTH.RightTop.ToNTSCoordinate() })
            });
    }
    const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSHYPNOTIC;
    public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
    {
      return source1.Concat(source2).ToList();
    }
    public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
    {
      return source1.Concat(source2).Concat(source3).ToList();
    }
    public static HashSet<Geometry> GetFLsWhereSupportingFloorDrainUnderWaterPoint(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> floorDrains, List<Geometry> washMachines)
    {
      var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(ToList(floorDrains, washMachines));
      var defaultFireValveLength = new HashSet<Geometry>();
      {
        var flsf = GeoFac.CreateIntersectsSelector(FLs);
        foreach (var kitchen in kitchens)
        {
          var MinWellToUrinalDistance = flsf(kitchen);
          if (MinWellToUrinalDistance.Count > THESAURUSSTAMPEDE)
          {
            if (maxDeviceplatformArea(kitchen).Count > THESAURUSSTAMPEDE)
            {
              defaultFireValveLength.AddRange(MinWellToUrinalDistance);
            }
          }
        }
      }
      return defaultFireValveLength;
    }
    static List<GLineSegment> ExplodeGLineSegments(Geometry DEFAULT_FIRE_VALVE_LENGTH)
    {
      static IEnumerable<GLineSegment> enumerate(Geometry DEFAULT_FIRE_VALVE_LENGTH)
      {
        if (DEFAULT_FIRE_VALVE_LENGTH is LineString defaultFireValveWidth)
        {
          if (defaultFireValveWidth.NumPoints == THESAURUSPERMUTATION) yield return new GLineSegment(defaultFireValveWidth[THESAURUSSTAMPEDE].ToPoint2d(), defaultFireValveWidth[THESAURUSHOUSING].ToPoint2d());
          else if (defaultFireValveWidth.NumPoints > THESAURUSPERMUTATION)
          {
            for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < defaultFireValveWidth.NumPoints - THESAURUSHOUSING; MAX_ANGEL_TOLLERANCE++)
            {
              yield return new GLineSegment(defaultFireValveWidth[MAX_ANGEL_TOLLERANCE].ToPoint2d(), defaultFireValveWidth[MAX_ANGEL_TOLLERANCE + THESAURUSHOUSING].ToPoint2d());
            }
          }
        }
        else if (DEFAULT_FIRE_VALVE_LENGTH is GeometryCollection colle)
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
      return enumerate(DEFAULT_FIRE_VALVE_LENGTH).ToList();
    }
    static List<Point2d> GetEndPoints(Geometry MAX_TAG_LENGTH, List<Point2d> points, List<Geometry> dlines)
    {
      points = points.Distinct().ToList();
      var _tol_avg_column_dist = points.Select(TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax, THESAURUSCOMMUNICATION).ToCirclePolygon(SUPERLATIVENESS)).ToList();
      var dlinesGeo = GeoFac.CreateGeometry(GeoFac.CreateIntersectsSelector(dlines)(MAX_TAG_LENGTH));
      return GeoFac.CreateIntersectsSelector(_tol_avg_column_dist)(dlinesGeo).Where(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.Intersects(MAX_TAG_LENGTH)).Select(_tol_avg_column_dist).ToList(points);
    }
    public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> _tol_avg_column_dist, List<Geometry> dlines)
    {
      var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
      var list = new List<Geometry>(FLs.Count);
      {
        return list;
      }
      foreach (var fl in FLs)
      {
        List<Point2d> endpoints = null;
        Geometry endpointsGeo = null;
        List<Point2d> _GetEndPoints()
        {
          return GetEndPoints(fl, _tol_avg_column_dist, dlines);
        }
        bool test1()
        {
          return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), QUOTATIONWITTIG, THESAURUSDISINGENUOUS).Intersects(kitchensGeo);
        }
        bool test2()
        {
          endpoints ??= _GetEndPoints();
          endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()));
          return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
        }
        if (test1() && test2())
        {
          list.Add(fl);
        }
      }
      return list;
    }
    public static List<Geometry> GetKitchenOnlyFLs(List<Geometry> FLs,
        List<Geometry> kitchens,
        List<Geometry> nonames,
        List<Geometry> balconies,
        List<Point2d> _tol_avg_column_dist,
        List<Geometry> dlines,
        List<string> max_kitchen_to_balcony_distance,
        List<Geometry> basins,
        List<Geometry> floorDrains,
        List<Geometry> washingMachines,
        List<bool> hasBasinList,
        List<bool> hasKitchenFloorDrainList,
        List<bool> hasKitchenWashingMachineList
        )
    {
      var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
      var list = new List<Geometry>(FLs.Count);
      var basinsf = GeoFac.CreateIntersectsSelector(basins);
      {
        var ok_fls = new HashSet<Geometry>();
        var floorDrainsf = GeoFac.CreateIntersectsSelector(floorDrains);
        var washingMachinesf = GeoFac.CreateIntersectsSelector(washingMachines);
        foreach (var kitchen in kitchens)
        {
          var flsf = GeoFac.CreateIntersectsSelector(FLs.Except(ok_fls).ToList());
          var text_indent = flsf(kitchen);
          if (text_indent.Count > THESAURUSSTAMPEDE)
          {
            var hasBasin = basinsf(kitchen).Any();
            foreach (var fl in text_indent)
            {
              list.Add(fl);
              hasBasinList.Add(hasBasin);
              hasKitchenFloorDrainList.Add(floorDrainsf(kitchen).Any());
              hasKitchenWashingMachineList.Add(washingMachinesf(kitchen).Any());
              ok_fls.Add(fl);
            }
          }
          else
          {
            text_indent = flsf(kitchen.Buffer(QUOTATIONWITTIG));
            if (text_indent.Count > THESAURUSSTAMPEDE)
            {
              var hasBasin = basinsf(kitchen).Any();
              var fl = GeoFac.NearestNeighbourGeometryF(text_indent)(kitchen);
              list.Add(fl);
              hasBasinList.Add(hasBasin);
              hasKitchenFloorDrainList.Add(floorDrainsf(kitchen).Any());
              hasKitchenWashingMachineList.Add(washingMachinesf(kitchen).Any());
              ok_fls.Add(fl);
            }
          }
        }
        return list;
      }
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < FLs.Count; MAX_ANGEL_TOLLERANCE++)
      {
        var fl = FLs[MAX_ANGEL_TOLLERANCE];
        var SidewaterbucketYIndent = max_kitchen_to_balcony_distance[MAX_ANGEL_TOLLERANCE];
        {
          foreach (var kitchen in kitchens)
          {
            if (kitchen.Envelope.ToGRect().ToPolygon().Intersects(fl))
            {
              list.Add(fl);
              hasBasinList.Add(basinsf(kitchen).Any());
            }
          }
          continue;
        }
        List<Point2d> endpoints = null;
        Geometry endpointsGeo = null;
        List<Point2d> _GetEndPoints()
        {
          return GetEndPoints(fl, _tol_avg_column_dist, dlines);
        }
        bool test1()
        {
          return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), QUOTATIONWITTIG, THESAURUSDISINGENUOUS));
        }
        bool test2()
        {
          endpoints ??= _GetEndPoints();
          if (endpoints.Count == THESAURUSSTAMPEDE) return THESAURUSOBSTINACY;
          endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()));
          return kitchensGeo.Intersects(endpointsGeo);
        }
        bool test3()
        {
          endpoints ??= _GetEndPoints();
          endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint()));
          return !endpointsGeo.Intersects(GeoFac.CreateGeometryEx(nonames)) || !endpointsGeo.Intersects(GeoFac.CreateGeometryEx(balconies));
        }
        if ((test1() || test2()) && test3())
        {
          list.Add(fl);
        }
      }
      return list;
    }
  }
  public class DrainageDrawingData
  {
    public HashSet<string> VerticalPipeLabels;
    public HashSet<string> LongTranslatorLabels;
    public HashSet<string> ShortTranslatorLabels;
    public Dictionary<string, int> FloorDrains;
    public Dictionary<string, int> SingleOutletFloorDrains;
    public Dictionary<string, int> WashingMachineFloorDrains;
    public Dictionary<string, int> tolGroupBlkLaneHead;
    public Dictionary<string, string> Outlets;
    public HashSet<string> KitchenFls;
    public HashSet<string> BalconyFls;
    public HashSet<string> ToiletPls;
    public HashSet<string> Merges;
    public Dictionary<int, string> OutletWrappingPipeDict;
    public Dictionary<int, string> OutletWrappingPipeRadiusStringDict;
    public HashSet<string> HasOutletWrappingPipe;
    public HashSet<string> Shunts;
    public HashSet<string> _4tunes;
    public HashSet<string> HasRainPortSymbolsForFL0;
    public HashSet<string> IsConnectedToFloorDrainForFL0;
    public Dictionary<string, int> FdsCountAt2F;
    public void Init()
    {
      FdsCountAt2F ??= new Dictionary<string, int>();
      VerticalPipeLabels ??= new HashSet<string>();
      LongTranslatorLabels ??= new HashSet<string>();
      ShortTranslatorLabels ??= new HashSet<string>();
      FloorDrains ??= new Dictionary<string, int>();
      SingleOutletFloorDrains ??= new Dictionary<string, int>();
      WashingMachineFloorDrains ??= new Dictionary<string, int>();
      tolGroupBlkLaneHead ??= new Dictionary<string, int>();
      Outlets ??= new Dictionary<string, string>();
      Shunts ??= new HashSet<string>();
      _4tunes ??= new HashSet<string>();
      KitchenFls ??= new HashSet<string>();
      BalconyFls ??= new HashSet<string>();
      ToiletPls ??= new HashSet<string>();
      HasRainPortSymbolsForFL0 ??= new HashSet<string>();
      IsConnectedToFloorDrainForFL0 ??= new HashSet<string>();
      Merges ??= new HashSet<string>();
      OutletWrappingPipeDict ??= new Dictionary<int, string>();
      OutletWrappingPipeRadiusStringDict ??= new Dictionary<int, string>();
      HasOutletWrappingPipe ??= new HashSet<string>();
    }
  }
  public class DrainageGeoData
  {
    public List<StoreyItem> StoreyItems;
    public List<GRect> Storeys;
    public List<KeyValuePair<string, Geometry>> RoomData;
    public List<CText> Labels;
    public List<GLineSegment> LabelLines;
    public HashSet<GLineSegment> ODLines;
    public List<GLineSegment> DLines;
    public List<GLineSegment> VLines;
    public List<GLineSegment> WLines;
    public List<GRect> VerticalPipes;
    public List<GRect> WrappingPipes;
    public List<GRect> FloorDrains;
    public List<GRect> WaterPorts;
    public List<GRect> WaterWells;
    public List<string> WaterPortLabels;
    public List<GRect> WashingMachines;
    public List<GRect> Basins;
    public List<GRect> MopPools;
    public List<Geometry> CleaningPorts;
    public HashSet<Point2d> CleaningPortBasePoints;
    public List<Point2d> SideFloorDrains;
    public List<GRect> PipeKillers;
    public List<GRect> DownWaterPorts;
    public List<GRect> RainPortSymbols;
    public List<KeyValuePair<Point2d, string>> FloorDrainTypeShooter;
    public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
    public List<GLineSegment> WrappingPipeLabelLines;
    public List<CText> WrappingPipeLabels;
    public List<StoreyInfo> StoreyInfos;
    public List<GRect> zbqs;
    public List<GRect> xsts;
    public List<List<Geometry>> Groups;
    public HashSet<GCircle> circles;
    public List<GCircle> FloorDrainRings;
    public void Init()
    {
      Storeys ??= new List<GRect>();
      RoomData ??= new List<KeyValuePair<string, Geometry>>();
      Labels ??= new List<CText>();
      LabelLines ??= new List<GLineSegment>();
      DLines ??= new List<GLineSegment>();
      VLines ??= new List<GLineSegment>();
      WLines ??= new List<GLineSegment>();
      zbqs ??= new List<GRect>();
      xsts ??= new List<GRect>();
      VerticalPipes ??= new List<GRect>();
      WrappingPipes ??= new List<GRect>();
      FloorDrains ??= new List<GRect>();
      WaterPorts ??= new List<GRect>();
      WaterWells ??= new List<GRect>();
      WaterPortLabels ??= new List<string>();
      WashingMachines ??= new List<GRect>();
      Basins ??= new List<GRect>();
      CleaningPorts ??= new List<Geometry>();
      CleaningPortBasePoints ??= new HashSet<Point2d>();
      SideFloorDrains ??= new List<Point2d>();
      PipeKillers ??= new List<GRect>();
      MopPools ??= new List<GRect>();
      DownWaterPorts ??= new List<GRect>();
      RainPortSymbols ??= new List<GRect>();
      FloorDrainTypeShooter ??= new List<KeyValuePair<Point2d, string>>();
      WrappingPipeRadius ??= new List<KeyValuePair<Point2d, string>>();
      WrappingPipeLabelLines ??= new List<GLineSegment>();
      WrappingPipeLabels ??= new List<CText>();
      StoreyInfos ??= new List<StoreyInfo>();
      Groups ??= new List<List<Geometry>>();
      StoreyItems ??= new List<StoreyItem>();
      ODLines ??= new HashSet<GLineSegment>();
      circles ??= new HashSet<GCircle>();
      FloorDrainRings ??= new List<GCircle>();
    }
    Dictionary<Point2d, string> floorDrainTypeDict;
    public void UpdateFloorDrainTypeDict(Point2d maxKitchenToBalconyDistance, string MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN)
    {
      floorDrainTypeDict ??= new Dictionary<Point2d, string>();
      if (!string.IsNullOrWhiteSpace(MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN))
      {
        floorDrainTypeDict[maxKitchenToBalconyDistance] = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
      }
    }
    public void Flush()
    {
      if (floorDrainTypeDict != null)
      {
        FloorDrainTypeShooter.AddRange(floorDrainTypeDict);
        floorDrainTypeDict = null;
      }
    }
    public void FixData()
    {
      Init();
      Storeys = Storeys.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      Labels = Labels.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary.IsValid).Distinct().ToList();
      LabelLines = LabelLines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      DLines = DLines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      VLines = VLines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      WLines = WLines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      VerticalPipes = VerticalPipes.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      WrappingPipes = WrappingPipes.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      FloorDrains = FloorDrains.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      zbqs = zbqs.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      xsts = xsts.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      DownWaterPorts = DownWaterPorts.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      RainPortSymbols = RainPortSymbols.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      {
        var d = new Dictionary<GRect, string>();
        for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < WaterPorts.Count; MAX_ANGEL_TOLLERANCE++)
        {
          var well = WaterPorts[MAX_ANGEL_TOLLERANCE];
          var MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = WaterPortLabels[MAX_ANGEL_TOLLERANCE];
          if (!string.IsNullOrWhiteSpace(MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE) || !d.ContainsKey(well))
          {
            d[well] = MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE;
          }
        }
        WaterPorts.Clear();
        WaterPortLabels.Clear();
        foreach (var max_rainpipe_to_balconyfloordrain in d)
        {
          WaterPorts.Add(max_rainpipe_to_balconyfloordrain.Key);
          WaterPortLabels.Add(max_rainpipe_to_balconyfloordrain.Value);
        }
      }
      WaterWells = WaterWells.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      WashingMachines = WashingMachines.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      Basins = Basins.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      MopPools = MopPools.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      PipeKillers = PipeKillers.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.IsValid).Distinct().ToList();
      WrappingPipeRadius = WrappingPipeRadius.Distinct().ToList();
      SideFloorDrains = SideFloorDrains.Distinct().ToList();
      WrappingPipeLabelLines = WrappingPipeLabelLines.Distinct().ToList();
    }
    public DrainageGeoData Clone()
    {
      return (DrainageGeoData)MemberwiseClone();
    }
    public DrainageGeoData DeepClone()
    {
      return this.ToCadJson().FromCadJson<DrainageGeoData>();
    }
  }
  public class DrainageCadData
  {
    public List<Geometry> Storeys;
    public List<Geometry> Labels;
    public List<Geometry> LabelLines;
    public List<Geometry> DLines;
    public List<Geometry> VLines;
    public List<Geometry> VerticalPipes;
    public List<Geometry> WrappingPipes;
    public List<Geometry> FloorDrains;
    public List<Geometry> WaterPorts;
    public List<Geometry> WaterWells;
    public List<Geometry> WashingMachines;
    public List<Geometry> SideFloorDrains;
    public List<Geometry> PipeKillers;
    public List<Geometry> Basins;
    public List<Geometry> MopPools;
    public List<Geometry> DownWaterPorts;
    public List<Geometry> RainPortSymbols;
    public void Init()
    {
      Storeys ??= new List<Geometry>();
      Labels ??= new List<Geometry>();
      LabelLines ??= new List<Geometry>();
      DLines ??= new List<Geometry>();
      VLines ??= new List<Geometry>();
      VerticalPipes ??= new List<Geometry>();
      WrappingPipes ??= new List<Geometry>();
      FloorDrains ??= new List<Geometry>();
      WaterPorts ??= new List<Geometry>();
      WaterWells ??= new List<Geometry>();
      WashingMachines ??= new List<Geometry>();
      SideFloorDrains ??= new List<Geometry>();
      PipeKillers ??= new List<Geometry>();
      Basins ??= new List<Geometry>();
      MopPools ??= new List<Geometry>();
      DownWaterPorts ??= new List<Geometry>();
      RainPortSymbols ??= new List<Geometry>();
    }
    public static DrainageCadData Create(DrainageGeoData data)
    {
      var bfSize = THESAURUSACRIMONIOUS;
      var o = new DrainageCadData();
      o.Init();
      o.Storeys.AddRange(data.Storeys.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()));
      o.Labels.AddRange(data.Labels.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Boundary.ToPolygon()));
      if (INTRAVASCULARLY) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
      else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
      o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
      o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
      o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
      if (INTRAVASCULARLY) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
      else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
      o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
      o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
      o.WaterWells.AddRange(data.WaterWells.Select(ConvertWaterPortsF()));
      o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
      o.Basins.AddRange(data.Basins.Select(ConvertWashingMachinesF()));
      o.MopPools.AddRange(data.MopPools.Select(ConvertWashingMachinesF()));
      o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
      o.PipeKillers.AddRange(data.PipeKillers.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()));
      o.DownWaterPorts.AddRange(data.DownWaterPorts.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()));
      o.RainPortSymbols.AddRange(data.RainPortSymbols.Select(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon()));
      return o;
    }
    private static Func<GRect, Polygon> ConvertWrappingPipesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon();
    }
    private static Func<GLineSegment, Geometry> NewMethod(int bfSize)
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Buffer(bfSize);
    }
    public static Func<Point2d, Point> ConvertSideFloorDrains()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToNTSPoint();
    }
    public static Func<Point2d, Polygon> ConvertCleaningPortsF()
    {
      return TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax, DISPENSABLENESS).ToCirclePolygon(THESAURUSDISINGENUOUS);
    }
    public static Func<GRect, Polygon> ConvertWashingMachinesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon();
    }
    public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Center.ToGCircle(THESAURUSDICTATORIAL).ToCirclePolygon(SUPERLATIVENESS);
    }
    private static Func<GRect, Polygon> ConvertWaterPortsF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon();
    }
    public static Func<GRect, Polygon> ConvertFloorDrainsF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToGCircle(THESAURUSOBSTINACY).ToCirclePolygon(THESAURUSDISINGENUOUS);
    }
    public static Func<GRect, Polygon> ConvertVerticalPipesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToPolygon();
    }
    private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
    {
      return TolLightRangeSingleSideMax => new GCircle(TolLightRangeSingleSideMax.Center, TolLightRangeSingleSideMax.InnerRadius).ToCirclePolygon(THESAURUSDISINGENUOUS);
    }
    public static Func<GLineSegment, LineString> ConvertVLinesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString();
    }
    public static Func<GLineSegment, LineString> ConvertDLinesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.ToLineString();
    }
    public static Func<GLineSegment, LineString> ConvertLabelLinesF()
    {
      return TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.Extend(ASSOCIATIONISTS).ToLineString();
    }
    public List<Geometry> GetAllEntities()
    {
      var tolGroupEmgLightEvac = new List<Geometry>(THESAURUSREPERCUSSION);
      tolGroupEmgLightEvac.AddRange(Storeys);
      tolGroupEmgLightEvac.AddRange(Labels);
      tolGroupEmgLightEvac.AddRange(LabelLines);
      tolGroupEmgLightEvac.AddRange(DLines);
      tolGroupEmgLightEvac.AddRange(VLines);
      tolGroupEmgLightEvac.AddRange(VerticalPipes);
      tolGroupEmgLightEvac.AddRange(WrappingPipes);
      tolGroupEmgLightEvac.AddRange(FloorDrains);
      tolGroupEmgLightEvac.AddRange(WaterPorts);
      tolGroupEmgLightEvac.AddRange(WaterWells);
      tolGroupEmgLightEvac.AddRange(WashingMachines);
      tolGroupEmgLightEvac.AddRange(SideFloorDrains);
      tolGroupEmgLightEvac.AddRange(PipeKillers);
      tolGroupEmgLightEvac.AddRange(Basins);
      tolGroupEmgLightEvac.AddRange(MopPools);
      tolGroupEmgLightEvac.AddRange(DownWaterPorts);
      tolGroupEmgLightEvac.AddRange(RainPortSymbols);
      return tolGroupEmgLightEvac;
    }
    public List<DrainageCadData> SplitByStorey()
    {
      var MinWellToUrinalDistance = new List<DrainageCadData>(this.Storeys.Count);
      if (this.Storeys.Count == THESAURUSSTAMPEDE) return MinWellToUrinalDistance;
      var maxDeviceplatformArea = GeoFac.CreateIntersectsSelector(GetAllEntities());
      foreach (var tolGroupBlkLane in this.Storeys)
      {
        var objs = maxDeviceplatformArea(tolGroupBlkLane);
        var o = new DrainageCadData();
        o.Init();
        o.Labels.AddRange(objs.Where(TolLightRangeSingleSideMax => this.Labels.Contains(TolLightRangeSingleSideMax)));
        o.LabelLines.AddRange(objs.Where(TolLightRangeSingleSideMax => this.LabelLines.Contains(TolLightRangeSingleSideMax)));
        o.DLines.AddRange(objs.Where(TolLightRangeSingleSideMax => this.DLines.Contains(TolLightRangeSingleSideMax)));
        o.VLines.AddRange(objs.Where(TolLightRangeSingleSideMax => this.VLines.Contains(TolLightRangeSingleSideMax)));
        o.VerticalPipes.AddRange(objs.Where(TolLightRangeSingleSideMax => this.VerticalPipes.Contains(TolLightRangeSingleSideMax)));
        o.WrappingPipes.AddRange(objs.Where(TolLightRangeSingleSideMax => this.WrappingPipes.Contains(TolLightRangeSingleSideMax)));
        o.FloorDrains.AddRange(objs.Where(TolLightRangeSingleSideMax => this.FloorDrains.Contains(TolLightRangeSingleSideMax)));
        o.WaterPorts.AddRange(objs.Where(TolLightRangeSingleSideMax => this.WaterPorts.Contains(TolLightRangeSingleSideMax)));
        o.WaterWells.AddRange(objs.Where(TolLightRangeSingleSideMax => this.WaterWells.Contains(TolLightRangeSingleSideMax)));
        o.WashingMachines.AddRange(objs.Where(TolLightRangeSingleSideMax => this.WashingMachines.Contains(TolLightRangeSingleSideMax)));
        o.SideFloorDrains.AddRange(objs.Where(TolLightRangeSingleSideMax => this.SideFloorDrains.Contains(TolLightRangeSingleSideMax)));
        o.PipeKillers.AddRange(objs.Where(TolLightRangeSingleSideMax => this.PipeKillers.Contains(TolLightRangeSingleSideMax)));
        o.Basins.AddRange(objs.Where(TolLightRangeSingleSideMax => this.Basins.Contains(TolLightRangeSingleSideMax)));
        o.MopPools.AddRange(objs.Where(TolLightRangeSingleSideMax => this.MopPools.Contains(TolLightRangeSingleSideMax)));
        o.DownWaterPorts.AddRange(objs.Where(TolLightRangeSingleSideMax => this.DownWaterPorts.Contains(TolLightRangeSingleSideMax)));
        o.RainPortSymbols.AddRange(objs.Where(TolLightRangeSingleSideMax => this.RainPortSymbols.Contains(TolLightRangeSingleSideMax)));
        MinWellToUrinalDistance.Add(o);
      }
      return MinWellToUrinalDistance;
    }
    public DrainageCadData Clone()
    {
      return (DrainageCadData)MemberwiseClone();
    }
  }
  public class BlockInfo
  {
    public string LayerName;
    public string BlockName;
    public Point3d BasePoint;
    public double Rotate;
    public double Scale;
    public Scale3d? ScaleEx;
    public Dictionary<string, string> PropDict;
    public Dictionary<string, object> DynaDict;
    public BlockInfo(string blockName, string layerName, Point3d SIDEWATERBUCKET_X_INDENT)
    {
      this.LayerName = layerName;
      this.BlockName = blockName;
      this.BasePoint = SIDEWATERBUCKET_X_INDENT;
      this.PropDict = new Dictionary<string, string>();
      this.DynaDict = new Dictionary<string, object>();
      this.Rotate = THESAURUSSTAMPEDE;
      this.Scale = THESAURUSHOUSING;
    }
  }
  public class LineInfo
  {
    public GLineSegment Line;
    public string LayerName;
    public LineInfo(GLineSegment TolUniformSideLenth, string layerName)
    {
      this.Line = TolUniformSideLenth;
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
    public DBTextInfo(Point3d point, string repeated_point_distance, string layerName, string textStyle)
    {
      repeated_point_distance ??= THESAURUSDEPLORE;
      this.LayerName = layerName;
      this.TextStyle = textStyle;
      this.BasePoint = point;
      this.Text = repeated_point_distance;
    }
  }
  public class DimInfo
  {
    public string Layer;
    public string Text;
    public Point3d Point1;
    public Point3d Point2;
    public Vector3d Vector;
    public DimInfo(Point2d pt1, Point2d _tol_lane, Vector2d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, string repeated_point_distance, string layer) : this(pt1.ToPoint3d(), _tol_lane.ToPoint3d(), MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN.ToVector3d(), repeated_point_distance, layer) { }
    public DimInfo(Point3d pt1, Point3d _tol_lane, Vector3d MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN, string repeated_point_distance, string layer)
    {
      this.Layer = layer;
      this.Point1 = pt1;
      this.Point2 = _tol_lane;
      this.Text = repeated_point_distance ?? THESAURUSDEPLORE;
      this.Vector = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN;
    }
  }
  public class CircleInfo
  {
    public GCircle Circle;
    public string LayerName;
    public CircleInfo(Point3d center, double radius, string layerName) : this(new GCircle(center.X, center.Y, radius), layerName)
    {
    }
    public CircleInfo(GCircle circle, string layerName)
    {
      this.Circle = circle;
      this.LayerName = layerName;
    }
  }
  public class PriorityQueue : IDisposable
  {
    Queue<Action>[] queues;
    public PriorityQueue(int queuesCount)
    {
      queues = new Queue<Action>[queuesCount];
      for (int MAX_ANGEL_TOLLERANCE = THESAURUSSTAMPEDE; MAX_ANGEL_TOLLERANCE < queuesCount; MAX_ANGEL_TOLLERANCE++)
      {
        queues[MAX_ANGEL_TOLLERANCE] = new Queue<Action>();
      }
    }
    public void Dispose()
    {
      Execute();
    }
    public void Enqueue(int priority, Action maxDeviceplatformArea)
    {
      queues[priority].Enqueue(maxDeviceplatformArea);
    }
    public void Execute()
    {
      while (queues.Any(queue => queue.Count > THESAURUSSTAMPEDE))
      {
        foreach (var queue in queues)
        {
          if (queue.Count > THESAURUSSTAMPEDE)
          {
            queue.Dequeue()();
            break;
          }
        }
      }
    }
  }
  public class Point3dComparer : IEqualityComparer<Point3d>
  {
    Tolerance tol;
    public Point3dComparer(double tol)
    {
      this.tol = new Tolerance(tol, tol);
    }
    public bool Equals(Point3d TolLightRangeSingleSideMax, Point3d y)
    {
      return TolLightRangeSingleSideMax.IsEqualTo(y, tol);
    }
    public int GetHashCode(Point3d obj)
    {
      return THESAURUSSTAMPEDE;
    }
  }
  public class ExtraInfo
  {
    public bool OK;
    public class Item
    {
      public int Index;
      public List<Tuple<Geometry, string>> LabelDict;
    }
    public List<DrainageCadData> CadDatas;
    public List<Item> Items;
    public List<DrainageDrawingData> drDatas;
    public List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems;
    public ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData;
    public DrainageSystemDiagramViewModel vm;
  }
  public enum LayoutState
  {
    CheckPoint, Basin, FloorDrain, CondensePipe, SanPaiMark, Outlet, PipeLabel, FixPipeVLines, FixAirBlock, Finished,
  }
  public enum GeoCalState
  {
    MarkTranslator, GroupPipe, MarkCompsToPipe, MarkWaterBucketToPipe, Finished,
  }
  public class Ref<T>
  {
    public T Value;
    public Ref() { }
    public Ref(T MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN) { Value = MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN; }
  }
  public class MLeaderInfo
  {
    public Point3d BasePoint;
    public string Text;
    public string Note;
    public static MLeaderInfo Create(Point2d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, string repeated_point_distance) => Create(MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN.ToPoint3d(), repeated_point_distance);
    public static MLeaderInfo Create(Point3d MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, string repeated_point_distance)
    {
      return new MLeaderInfo() { BasePoint = MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN, Text = repeated_point_distance };
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
    public IList<GraphNode<T>> FindShortestPathBetween(GraphNode<T> MAX_TAG_LENGTH, GraphNode<T> finish)
    {
      PrepareGraphForDijkstra();
      MAX_TAG_LENGTH.TentativeDistance = THESAURUSSTAMPEDE;
      var current = MAX_TAG_LENGTH;
      while (THESAURUSOBSTINACY)
      {
        foreach (var neighbour in current.Neighbours.Where(TolLightRangeSingleSideMax => !TolLightRangeSingleSideMax.GraphNode.Visited))
        {
          var newTentativeDistance = current.TentativeDistance + neighbour.Distance;
          if (newTentativeDistance < neighbour.GraphNode.TentativeDistance)
          {
            neighbour.GraphNode.TentativeDistance = newTentativeDistance;
          }
        }
        current.Visited = THESAURUSOBSTINACY;
        var next = _unvistedNodes.Pop();
        if (next == null || next.TentativeDistance == int.MaxValue)
        {
          if (finish.TentativeDistance == int.MaxValue)
          {
            return new List<GraphNode<T>>();
          }
          finish.Visited = THESAURUSOBSTINACY;
          break;
        }
        var smallest = next;
        current = smallest;
      }
      return DeterminePathFromWeightedGraph(MAX_TAG_LENGTH, finish);
    }
    private static List<GraphNode<T>> DeterminePathFromWeightedGraph(GraphNode<T> MAX_TAG_LENGTH, GraphNode<T> finish)
    {
      var current = finish;
      var path = new List<GraphNode<T>> { current };
      var currentTentativeDistance = finish.TentativeDistance;
      while (THESAURUSOBSTINACY)
      {
        if (current == MAX_TAG_LENGTH)
        {
          break;
        }
        foreach (var neighbour in current.Neighbours.Where(TolLightRangeSingleSideMax => TolLightRangeSingleSideMax.GraphNode.Visited))
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
      _graph.ForEach(TolLightRangeSingleSideMax =>
      {
        TolLightRangeSingleSideMax.Visited = INTRAVASCULARLY;
        TolLightRangeSingleSideMax.TentativeDistance = int.MaxValue;
        _unvistedNodes.Push(TolLightRangeSingleSideMax);
      });
    }
  }
  internal class CompareNeighbour<T> : IComparer<GraphNode<T>>
  {
    public int Compare(GraphNode<T> TolLightRangeSingleSideMax, GraphNode<T> y)
    {
      if (TolLightRangeSingleSideMax.TentativeDistance > y.TentativeDistance)
      {
        return THESAURUSHOUSING;
      }
      if (TolLightRangeSingleSideMax.TentativeDistance < y.TentativeDistance)
      {
        return -THESAURUSHOUSING;
      }
      return THESAURUSSTAMPEDE;
    }
  }
  public class GraphNode<T>
  {
    public readonly List<Neighbour> Neighbours;
    public bool Visited = INTRAVASCULARLY;
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
    void Push(T REPEATED_POINT_DISTANCE);
    T Pop();
    bool Contains(T REPEATED_POINT_DISTANCE);
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
    public void Push(T REPEATED_POINT_DISTANCE)
    {
      _innerList.Add(REPEATED_POINT_DISTANCE);
    }
    public T Pop()
    {
      if (_innerList.Count <= THESAURUSSTAMPEDE)
      {
        return default(T);
      }
      Sort();
      var REPEATED_POINT_DISTANCE = _innerList[THESAURUSSTAMPEDE];
      _innerList.RemoveAt(THESAURUSSTAMPEDE);
      return REPEATED_POINT_DISTANCE;
    }
    public bool Contains(T REPEATED_POINT_DISTANCE)
    {
      return _innerList.Contains(REPEATED_POINT_DISTANCE);
    }
    private void Sort()
    {
      _innerList.Sort(_comparer);
    }
  }
}