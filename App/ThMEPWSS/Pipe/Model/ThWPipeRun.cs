using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Pipe.Model
{
  public class ThWPipeRun : IThWDraw
  {
    virtual public void Draw(Point3d basePt, Matrix3d mat)
    {
      throw new NotImplementedException();
    }

    virtual public void Draw(Point3d basePt)
    {
      throw new NotImplementedException();
    }
  }

  public class ThWRainPipeRun : ThWPipeRun, IEquatable<ThWRainPipeRun>
  {
    /// <summary>
    /// 楼层
    /// </summary>
    public ThWSDStorey Storey { get; set; } = new ThWSDStorey();

    /// <summary>
    /// 主雨水管
    /// </summary>
    public ThWSDPipe MainRainPipe { get; set; } = new ThWSDPipe();

    /// <summary>
    /// 地漏
    /// </summary>
    public List<ThWSDDrain> FloorDrains { get; set; } = new List<ThWSDDrain>();

    /// <summary>
    /// 冷凝管
    /// </summary>
    public List<ThWSDCondensePipe> CondensePipes { get; set; } = new List<ThWSDCondensePipe>();

    /// <summary>
    /// 转管
    /// </summary>
    public ThWSDTranslatorPipe TranslatorPipe { get; set; } = new ThWSDTranslatorPipe();

    /// <summary>
    /// 检查口
    /// </summary>
    public ThWSDCheckPoint CheckPoint { get; set; } = new ThWSDCheckPoint();

    public ThWRainPipeRun()
    {
      //FloorDrains = new List<ThWFloorDrain>();
      //CondensePipes = new List<ThWCondensePipe>();
    }

    override public void Draw(Point3d basePt)
    {
      NoDraw.Text("ThWRainPipeRun", 100, basePt).AddToCurrentSpace();
      //MainRainPipe.Draw(basePt);
      //todo
      FloorDrains.ForEach(o => o.Draw(basePt));
      CondensePipes.ForEach(o => o.Draw(basePt));
      TranslatorPipe.Draw(basePt);
      CheckPoint.Draw(basePt);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public bool Equals(ThWRainPipeRun other)
    {
      return this.Storey.Equals(other.Storey)
          && this.MainRainPipe.Equals(other.MainRainPipe)
          && FloorDrains.Count.Equals(other.FloorDrains.Count)
          && CondensePipes.Count.Equals(other.CondensePipes.Count)
          && TranslatorPipe.Equals(other.TranslatorPipe)
          && CheckPoint.Equals(other.CheckPoint);
    }
  }
}
