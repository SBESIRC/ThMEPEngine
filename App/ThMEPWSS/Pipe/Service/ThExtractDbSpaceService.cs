using AcHelper;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Tools;

namespace ThMEPWSS.Pipe.Service
{
    public class ThExtractDbSpaceService
    {
       private double SimilarityTolerance = 0.99;
       private double TagPositionTolerance= 1000;
       public List<Polyline> polylines { get; set; }
       public List<DBText> tags { get; set; }
        public ThExtractDbSpaceService()
        {
            polylines=new List<Polyline> ();
            tags = new List<DBText>();
        }          
        public void Extract(Database db,Point3dCollection pts)
        {
            SelectFrames();         
            var roomEngine = new ThWRoomRecognitionEngine();
            roomEngine.Recognize(db, pts);
            var spaceNames = new Dictionary<Polyline, string>();
            roomEngine.Elements.Cast<ThIfcRoom>().ToList().ForEach(o =>
            {
                spaceNames.Add(o.Boundary as Polyline,o.Name);
            });
            var samePositionSpaces = DisposePolylines(spaceNames.Select(o=>o.Key).ToList(), polylines);
            var samePositionTags= DisposeTags(spaceNames, tags);           
            ErasePolylines(samePositionSpaces);
            EraseTags(samePositionTags);
            CreateFrames(spaceNames);
        }
        private void CreateFrames(Dictionary<Polyline, string> dictionary)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var style = db.TextStyles.ElementOrDefault("TH-STYLE3");
                var polylines = dictionary.Select(o => o.Key).ToList();
                foreach(Polyline polyline in polylines)
                {               
                    polyline.LayerId = CreateLayer("AI-空间框线", 30, LineWeight.LineWeight020);
                    polyline.Closed = true;
                    db.ModelSpace.Add(polyline);
                    DBText text= new DBText() {
                        TextString = dictionary[polyline],
                        LayerId = CreateLayer("AI-空间名称", 130),
                        Position= polyline.GetCenter(),
                        TextStyleId = style.ObjectId,
                        Height=200,
                        WidthFactor=0.7
                    } ;
                    db.ModelSpace.Add(text);
                }               
            }
        }
        public static ObjectId CreateLayer(string aimLayer, short colorIndex, LineWeight lineWeight= LineWeight.ByLineWeightDefault)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color =Color.FromColorIndex(
                        Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex);
                    layerRecord.IsPlottable = false;
                    layerRecord.IsFrozen = false;
                    layerRecord.IsLocked = false;
                    layerRecord.LineWeight = lineWeight;
                }
                else
                {
                    if (!layerRecord.Color.Equals(Color.FromColorIndex(
                        Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex)))
                    {
                        layerRecord.UpgradeOpen();
                        layerRecord.Color = Color.FromColorIndex(
                        Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex);
                        layerRecord.IsPlottable = false;                     
                        layerRecord.IsFrozen = false;
                        layerRecord.IsLocked = false;
                        layerRecord.LineWeight = lineWeight;
                        layerRecord.DowngradeOpen();
                    }
                }
            }

            return layerRecord.ObjectId;
        }
        private void EraseTags(List<DBText> spaces)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                spaces.ForEach(o =>
                {
                    o.UpgradeOpen();
                    o.Erase();
                    o.DowngradeOpen();
                });
            }
        }
        private void ErasePolylines(List<Polyline> spaces)
        {
            using (AcadDatabase db= AcadDatabase.Active())
            {
                spaces.ForEach(o =>
                {
                    o.UpgradeOpen();
                    o.Erase();
                    o.DowngradeOpen();
                });
            }
        }
        private List<DBText> DisposeTags(Dictionary<Polyline, string> dictionary, List<DBText> tags)
        {
            var result = new List<DBText>();
            var tags_ref = dictionary.Select(o => o.Key).ToList();
            foreach (DBText tag in tags)
            {
                foreach(Polyline polyline in tags_ref)
                {                   
                  if (polyline.GetCenter().DistanceTo( tag.Position)< TagPositionTolerance && dictionary[polyline] != "")
                  {
                            result.Add(tag);
                            break;
                  }              
                }
            }
            return result;
        }
        private List<Polyline> DisposePolylines(List<Polyline> polylines, List<Polyline> polylines_r)
        {
            var result = new List<Polyline>();
            foreach (Polyline polyline in polylines)
            {
                foreach(Polyline polyline_r in polylines_r)
                {
                    if(polyline.SimilarityMeasure(polyline_r)>= SimilarityTolerance)
                    {
                        result.Add(polyline_r);
                    }
                }
            }
            return result;
        }
        private void SelectFrames()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var points = new Point3dCollection();
                var results = new List<Polyline>();
                var pso = new PromptSelectionOptions();
                pso.MessageForAdding = "请您框选范围\n";
                var tvs = new TypedValue[]
                {
              new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Polyline)).DxfName+","+RXClass.GetClass(typeof(DBText)).DxfName)
                };                
                var sf = new SelectionFilter(tvs);
                var psr = Active.Editor.GetSelection(pso, sf);
                if (psr.Status == PromptStatus.OK)
                {
                    var ents = psr.Value
                        .GetObjectIds()
                        .Select(o => acadDatabase.Element<Entity>(o));
                    polylines = ents
                        .Where(o => o is Polyline)
                        .Cast<Polyline>()
                        .ToList();
                    tags = ents
                        .Where(o => o is DBText)
                        .Cast<DBText>()
                        .ToList();
                }   
            }
        }
    
    }
}
