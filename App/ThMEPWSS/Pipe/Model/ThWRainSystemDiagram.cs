using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.DebugNs;
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
    public void InitCacheData(AcadDatabase acadDatabase)
    {
      thRainSystemService = new ThMEPWSS.Pipe.Service.ThRainSystemService
      {
        adb = acadDatabase
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
                  LargeRoofVPTexts = SystemDiagramUtils.GetVerticalPipeNotes(pts);

                  var vps1 = new List<ThWSDPipe>();
                  LargeRoofVPTexts.ForEach(pt =>
                  {
                    thRainSystemService.LabelToDNDict.TryGetValue(pt, out string nd);
                    vps1.Add(new ThWSDPipe() { Label = pt, ND = nd });
                  });

                  WSDStoreys.Add(new ThWSDStorey() { Label = $"RF", Range = pts, VerticalPipes = vps1 });
                  break;
                }
              case StoreyType.SmallRoof:
                {
                  var smallRoofVPTexts = SystemDiagramUtils.GetVerticalPipeNotes(pts);
                  var rf1Storey = new ThWSDStorey() { Label = $"RF+1", Range = pts };
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
                        thRainSystemService.LabelToDNDict.TryGetValue(pt, out string nd);
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
                        thRainSystemService.LabelToDNDict.TryGetValue(pt, out string nd);
                        rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, ND = nd });
                      });
                      rf1Storey.VerticalPipes = rf1VerticalPipeObjects;

                      var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                      rf2VerticalPipeText.ForEach(pt =>
                      {
                        thRainSystemService.LabelToDNDict.TryGetValue(pt, out string nd);
                        rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt, ND = nd });
                      });

                      WSDStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Range = pts, VerticalPipes = rf2VerticalPipeObjects });
                    }
                  }
                  break;
                }
              case StoreyType.StandardStorey:
              case StoreyType.NonStandardStorey:
                sobj.Storeys.ForEach(i => WSDStoreys.Add(new ThWSDStorey() { Label = $"{i}F", Range = pts }));
                break;
              case StoreyType.Unknown:
              default:
                break;
            }
          }
        }
      }
    }

    public void InitVerticalPipeSystems(Database db, Point3dCollection range)
    {
      //Init Roof Pipe Systems
      InitRoofPipeSystems(db, range);
      //todo: Init Balcony Pipe Systems
      InitBalconyPipeSystems();
      //todo: Init Condense Pipe Systems
      InitCondensePipeSystems();
    }
    void InitBalconyPipeSystems()
    {

    }
    void InitCondensePipeSystems()
    {

    }
    private void InitRoofPipeSystems(Database db, Point3dCollection range)
    {
      //1. get all notes of roof vertical pipes
      var allRoofPipeNotes = SystemDiagramUtils.GetRoofVerticalPipeNotes(range);
      var distinctRoofPipeNotes = allRoofPipeNotes.Distinct();

      foreach (var roofPipeNote in distinctRoofPipeNotes)
      {
        var rainPipeSystem = new ThWRoofRainPipeSystem();
        var PipeRuns = new List<ThWRainPipeRun>();
        bool bSetWaterBucket = false;
        foreach (var s in WSDStoreys)
        {

          thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote);
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
            var pipes = SystemDiagramUtils.GetRoofVerticalPipeNotes(s.Range);
            if (pipes.Contains(roofPipeNote))
            {
              thRainSystemService.LabelToDNDict.TryGetValue(roofPipeNote, out string nd);
              PipeRuns.Add(new ThWRainPipeRun()
              {
                Storey = s,
                MainRainPipe = new ThWSDPipe()
                {
                  Label = roofPipeNote,
                  PipeType = VerticalPipeType.RoofVerticalPipe,
                  ND = nd,
                }
                //todo: set translator type
                //todo: set check point
              });
            }
          }

          if (bSetWaterBucket == false)
          {
            //water bucket, too slow
            //var center = thRainSystemService.GetCenterOfVerticalPipe(s.Range, roofPipeNote);
            //var waterBucketType = SystemDiagramUtils.GetRelatedWaterBucket(db, range, center);
            //if (!waterBucketType.Equals(WaterBucketEnum.None))
            //{
            //  rainPipeSystem.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType };
            //  bSetWaterBucket = true;
            //}
          }
        }
        rainPipeSystem.PipeRuns = PipeRuns;
        //todo:
        //rainPipeSystem.OutputType
        this.RoofVerticalRainPipes.Add(rainPipeSystem);
      }
    }
    const double VERTICAL_STOREY_SPAN = 2000;
    const double HORIZONTAL_STOREY_SPAN = 5500;
    public override void Draw(Point3d basePt)
    {
      //draw horizental storey lines
      {
        for (int i = 0; i < WSDStoreys.Count; i++)
        {
          ThWSDStorey s = WSDStoreys[i];
          var storeyBasePt = new Point3d(basePt.X, basePt.Y + i * VERTICAL_STOREY_SPAN, basePt.Z);
          s.StoreyBasePoint = storeyBasePt;
          s.Draw(storeyBasePt);
          //NoDraw.Circle(storeyBasePt.OffsetX(i * HORIZONTAL_STOREY_SPAN+ HORIZONTAL_STOREY_SPAN), 150).AddToCurrentSpace();
          //NoDraw.Circle(basePt.OffsetX(i * HORIZONTAL_STOREY_SPAN+ HORIZONTAL_STOREY_SPAN), 100).AddToCurrentSpace();
          //NoDraw.Circle(basePt, 100).AddToCurrentSpace();
          //NoDraw.Circle(storeyBasePt, 100).AddToCurrentSpace();
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
          roofRainSystem.Draw(basePt.OffsetX(i * HORIZONTAL_STOREY_SPAN + HORIZONTAL_STOREY_SPAN));

          //todo: draw outputs
          var outputs = g.Select(p => p.OutputType);

          //todo: draw vertical pipe id
          var pipeIds = g.Select(p => p.VerticalPipeId);
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
