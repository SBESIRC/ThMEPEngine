﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.Pipe.Model
{

  public class ThWRainPipeSystem : IThWDraw, IEquatable<ThWRainPipeSystem>
  {
    public string VerticalPipeId { get; set; } = string.Empty;
    public List<ThWRainPipeRun> PipeRuns { get; set; }
    protected ThWRainPipeSystem()
    {
      PipeRuns = new List<ThWRainPipeRun>();
    }

    public void SortPipeRuns()
    {
      //todo:
    }

    virtual public void Draw(Point3d basePt, Matrix3d mat)
    {
      //todo:
    }

    virtual public void Draw(Point3d basePt)
    {
      foreach (var r in PipeRuns)
      {
        //draw pipe
      }

      //todo: draw other device
    }

    public override int GetHashCode()
    {
      var hashCode = 1;
      foreach (var r in PipeRuns)
      {
        hashCode ^= r.GetHashCode();
      }

      return hashCode;
    }

    public bool Equals(ThWRainPipeSystem other)
    {
      if (other == null) return false;

      if (this.PipeRuns.Count != other.PipeRuns.Count) return false;

      for (int i = 0; i < this.PipeRuns.Count; ++i)
      {
        if (!PipeRuns[i].Equals(other.PipeRuns[i]))
          return false;
      }

      return true;
    }
  }

  /// <summary>
  /// 屋顶雨水管系统
  /// </summary>
  public class ThWRoofRainPipeSystem : ThWRainPipeSystem, IEquatable<ThWRoofRainPipeSystem>
  {
    /// <summary>
    /// 雨水斗
    /// </summary>
    public ThWSDWaterBucket WaterBucket { get; set; } = new ThWSDWaterBucket();

    public ThWSDOutputType OutputType { get; set; } = new ThWSDOutputType();

    public ThWRoofRainPipeSystem()
    {

    }

    override public void Draw(Point3d basePt)
    {
      if (WaterBucket != null)
      {
        var waterBucketStorey = WaterBucket.Storey;
        if (waterBucketStorey != null)
        {
          //Dbg.PrintLine(waterBucketStorey.Label);
          var waterBucketBasePt = new Point2d(basePt.X, waterBucketStorey.StoreyBasePoint.Y).ToPoint3d();
          WaterBucket?.Draw(waterBucketBasePt);
        }
      }
      for (int i = 0; i < PipeRuns.Count; i++)
      {
        ThWRainPipeRun r = PipeRuns[i];
        //draw piperun
        r.Draw(basePt.OffsetY(i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
        if (r.TranslatorPipe.PipeType.Equals(TranslatorTypeEnum.None))
        {

        }

      }
      //new Circle() { Center = basePt, ColorIndex = 3, Radius = 200, Thickness = 5, }.AddToCurrentSpace();
      //NoDraw.Text("ThWRoofRainPipeSystem: " + VerticalPipeId, 100, basePt.OffsetY(-1000)).AddToCurrentSpace();
      //todo: draw other device
    }

    public override int GetHashCode()
    {
        var hashCode = base.GetHashCode() ^ WaterBucket.GetHashCode() ^ OutputType.GetHashCode();
        return hashCode;
    }

    public bool Equals(ThWRoofRainPipeSystem other)
    {
      if (other == null) return false;

      return WaterBucket.Equals(other.WaterBucket) && OutputType.Equals(other.OutputType) && base.Equals(other);
    }
  }

  /// <summary>
  /// 阳台雨水管系统
  /// </summary>
  public class ThWBalconyRainPipeSystem : ThWRainPipeSystem, IEquatable<ThWRoofRainPipeSystem>
  {
    /// <summary>
    /// 雨水斗
    /// </summary>
    public ThWSDWaterBucket WaterBucket { get; set; }

    public ThWSDOutputType OutputType { get; set; }

    public ThWBalconyRainPipeSystem()
    {

    }

    override public void Draw(Point3d basePt)
    {
      foreach (var r in PipeRuns)
      {
        //draw pipe
      }

      //todo: draw other device
    }

    public override int GetHashCode()
    {
      return base.GetHashCode() ^ WaterBucket.GetHashCode() ^ OutputType.GetHashCode();
    }

    public bool Equals(ThWRoofRainPipeSystem other)
    {
      if (other == null) return false;

      return WaterBucket.Equals(other.WaterBucket) && OutputType.Equals(other.OutputType) && base.Equals(other);
    }
  }

  /// <summary>
  /// 冷凝管系统
  /// </summary>
  public class ThWCondensePipeSystem : ThWRainPipeSystem, IEquatable<ThWCondensePipeSystem>
  {
    public ThWSDOutputType OutputType { get; set; }

    public ThWCondensePipeSystem()
    {

    }

    override public void Draw(Point3d basePt)
    {
      foreach (var r in PipeRuns)
      {
        //draw pipe
      }

      //todo: draw other device
    }

    public override int GetHashCode()
    {
      return OutputType.GetHashCode();
    }

    public bool Equals(ThWCondensePipeSystem other)
    {
      if (other == null) return false;

      return OutputType.Equals(other.OutputType) && base.Equals(other);
    }
  }

}
