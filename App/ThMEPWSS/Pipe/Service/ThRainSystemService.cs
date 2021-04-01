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


namespace ThMEPWSS.Pipe.Service
{
  using Autodesk.AutoCAD.Geometry;
  using Dreambuild.AutoCAD;
  using ThMEPWSS.Pipe.Model;
  using ThUtilExtensionsNs;
  public class ThRainSystemService
  {
    public AcadDatabase adb;
    public List<Entity> Lines;
    public List<DBText> Texts;
    public List<BlockReference> Pipes;
    public Dictionary<Entity, ThWGRect> BoundaryDict = new Dictionary<Entity, ThWGRect>();
    public Dictionary<string, string> LabelToDNDict = new Dictionary<string, string>();
    public Dictionary<Entity, string> EntityToLabelDict = new Dictionary<Entity, string>();
    public List<Tuple<Entity, Entity>> ShortConverters = new List<Tuple<Entity, Entity>>();
    
    public Point3d GetCenterOfVerticalPipe(Point3dCollection range, string verticalPipeID)
    {
      foreach (var pipe in Pipes)
      {
        EntityToLabelDict.TryGetValue(pipe, out string id);
        if (id == verticalPipeID)
        {
          var bd = BoundaryDict[pipe];
          if (range.ToRect().ContainsRect(bd))
          {
            return bd.Center.ToPoint3d();
          }
        }
      }
      return default;
    }
    public TranslatorTypeEnum GetTranslatorType(Point3dCollection range, string verticalPipeID)
    {
      var type = TranslatorTypeEnum.None;

      return type;
    }


    public void CollectData()
    {
      InitCache();
      var used = new HashSet<Entity>();
      foreach (var pipe in Pipes)
      {
        if (used.Contains(pipe)) continue;
        var group = new HashSet<Entity> { pipe };
        Entity curLine = null;
        {
          var r = BoundaryDict[pipe];
          foreach (var line in Lines)
          {
            if (!used.Contains(line) && GeoAlgorithm.IsRectCross(r, BoundaryDict[line]))
            {
              group.Add(line);
              used.Add(line);
              curLine = line;
              break;
            }
          }
        }
        if (curLine != null)
        {
          var r = BoundaryDict[curLine];
          foreach (var line in Lines)
          {
            if (!used.Contains(line) && GeoAlgorithm.IsRectCross(r, BoundaryDict[line]))
            {
              group.Add(line);
              used.Add(line);
            }
          }
          foreach (var e in Pipes)
          {
            if (!used.Contains(e) && GeoAlgorithm.IsRectCross(r, BoundaryDict[e]))
            {
              group.Add(e);
              used.Add(e);
            }
          }
          var rect = GeoAlgorithm.GetBoundaryRect(group.ToArray());
          foreach (var e in Texts)
          {
            if (!used.Contains(e))
            {
              var ok = GeoAlgorithm.IsRectCross(rect, BoundaryDict[e]);
              if (!ok)
              {
                var r3 = BoundaryDict[e];
                r3 = GeoAlgorithm.ExpandRect(r3, 100);
                ok = GeoAlgorithm.IsRectCross(rect, r3);
              }
              if (ok)
              {
                group.Add(e);
                used.Add(e);
              }
            }
          }
          var _targetPipes = group.OfType<BlockReference>().OrderBy(e => BoundaryDict[e].LeftTop.X).ThenByDescending(e => BoundaryDict[e].LeftTop.Y).ToList();
          var _targetTexts = group.OfType<DBText>().OrderBy(e => BoundaryDict[e].LeftTop.X).ThenByDescending(e => BoundaryDict[e].LeftTop.Y).ToList();
          if (_targetTexts.Count == 0)
          {
            var boundary = GeoAlgorithm.GetBoundaryRect(group.ToArray());
            foreach (var t in Texts)
            {
              var bd = BoundaryDict[t];
              if (Math.Abs(bd.MinY - boundary.MaxY) < 100 && boundary.MinX <= bd.MaxX && boundary.MaxX >= bd.MaxX)
              {
                if (!_targetTexts.Contains(t))
                {
                  _targetTexts.Add(t);
                }
              }
            }
          }
          if (_targetPipes.Count > 0 && _targetPipes.Count == _targetTexts.Count)
          {
            var targetPipes = (from e in _targetPipes
                               let bd = BoundaryDict[e]
                               orderby bd.MinX ascending
                               orderby bd.MaxY descending
                               select e).ToList();
            var targetTexts = (from e in _targetTexts
                               let bd = BoundaryDict[e]
                               orderby bd.MinX ascending
                               orderby bd.MaxY descending
                               select e).ToList();
            var dnProp = "可见性1";
            for (int i = 0; i < targetPipes.Count; i++)
            {
              var pipeEnt = targetPipes[i];
              var dbText = targetTexts[i];
              var label = dbText.TextString;
              var dnText = pipeEnt.GetStrValueFromDict(dnProp);
              if (label != null)
              {
                LabelToDNDict[label] = dnText;
                EntityToLabelDict[pipeEnt] = label;
              }
            }
          }
        }
        used.Add(pipe);
      }
      var pipeBoundaries = (from pipe in Pipes
                            let boundary = BoundaryDict[pipe]
                            where !Equals(boundary, default(ThWGRect))
                            select new { pipe, boundary }).ToList();
      for (int i = 0; i < pipeBoundaries.Count; i++)
      {
        for (int j = i + 1; j < pipeBoundaries.Count; j++)
        {
          var bd1 = pipeBoundaries[i].boundary;
          var bd2 = pipeBoundaries[j].boundary;
          if (GeoAlgorithm.Distance(bd1.Center, bd2.Center) <= 5)
          {
            ShortConverters.Add(new Tuple<Entity, Entity>(pipeBoundaries[i].pipe, pipeBoundaries[j].pipe));
          }
        }
      }
    }
    bool inited;
    public void InitCache()
    {
      if (inited) return;
      Lines = adb.ModelSpace.OfType<Line>().Cast<Entity>()
       .Union(adb.ModelSpace.OfType<Polyline>().Cast<Entity>())
       .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE)
       .ToList();
      Texts = adb.ModelSpace.OfType<DBText>()
       .Where(x => x.Layer == ThWPipeCommon.W_RAIN_NOTE)
       .ToList();
      var blockNameOfVerticalPipe = "带定位立管";
      Pipes = adb.ModelSpace.OfType<BlockReference>()
       .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
       .Where(x => x.ToDataItem().EffectiveName == blockNameOfVerticalPipe)
       .ToList();

      foreach (var e in Lines)
      {
        BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
      }
      foreach (var e in Texts)
      {
        BoundaryDict[e] = GeoAlgorithm.GetBoundaryRect(e);
      }
      ThWGRect getRealBoundaryForPipe(Entity ent)
      {
        var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
        var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
        if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
        return default;
      }
      foreach (var e in Pipes)
      {
        BoundaryDict[e] = getRealBoundaryForPipe(e);
      }
      inited = true;
    }
  }
}
namespace ThUtilExtensionsNs
{
  public static class ThDataItemExtensions
  {
    public static ThWGRect ToRect(this Point3dCollection colle)
    {
      if (colle.Count == 0) return default;
      var arr = colle.Cast<Point3d>().ToArray();
      var x1 = arr.Select(p => p.X).Min();
      var x2 = arr.Select(p => p.X).Max();
      var y1 = arr.Select(p => p.Y).Max();
      var y2 = arr.Select(p => p.Y).Min();
      return new ThWGRect(x1, y1, x2, y2);
    }
    public static ThBlockReferenceData ToDataItem(this Entity ent)
    {
      return new ThBlockReferenceData(ent.ObjectId);
    }
    public static DBObjectCollection ExplodeToDBObjectCollection(this Entity ent)
    {
      var entitySet = new DBObjectCollection();
      ent.Explode(entitySet);
      return entitySet;
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
    public static string GetStrValueFromDict(this Entity e, string key)
    {
      var d = e.ToDataItem().CustomProperties.ToDict();
      d.TryGetValue(key, out object o);
      return o?.ToString();
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
