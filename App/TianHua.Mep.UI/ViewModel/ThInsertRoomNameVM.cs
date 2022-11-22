using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using CommunityToolkit.Mvvm.Input;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThInsertRoomNameVM:NotifyPropertyChangedBase
    {
        private string _id = "";
        public string Id => _id;
        public ThInsertRoomNameVM()
        {
            _id= System.Guid.NewGuid().ToString();
        }

        private string _roomName = "";
        public string RoomName
        {
            get { return _roomName; }
            set 
            { 
                _roomName = value;
                base.RaisePropertyChanged("RoomName");
            }
        }
        public ICommand InsertRoomNameCmd
        {
            get
            {
                return new RelayCommand(ManualInsert);
            }
        }
        public void Extract()
        {
            using (var acadDb= AcadDatabase.Active())
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                var winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.CurrentUserCoordinateSystem);

                var elements = ExtractRoomNames(acadDb.Database, frame.Vertices());
                Print(acadDb, elements);
            }
        }

        private void ManualInsert()
        {
            var roomName = _roomName.Trim();
            if (!string.IsNullOrEmpty(roomName))
            {
                using (var docLock = Active.Document.LockDocument())
                {
                    SetFocusToDwgView();
                    var markLayerId = Active.Database.CreateAIRoomMarkLayer();
                    var textStyleId = Active.Database.ImportTextStyle("TH-STYLE3");
                    if(markLayerId ==ObjectId.Null || textStyleId == ObjectId.Null)
                    {
                        return;
                    }
                    while (true)
                    {
                        var ppo = Active.Editor.GetPoint("\n请选择插入点");
                        if(ppo.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                        {
                            var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                            Print(roomName,wcsPt,textStyleId,markLayerId);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                Active.Editor.WriteLine("\n房间名称不能为空！");
            }
        }

        private void Print(string roomName,Point3d pt,ObjectId textStyleId, ObjectId markLayerId)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var dbText = new DBText
                {
                    TextString = roomName,
                    TextStyleId = textStyleId,
                    Height = 300,
                    WidthFactor = 0.7,
                    Justify = AttachmentPoint.MiddleCenter,
                    LayerId = markLayerId,
                };
                dbText.AlignmentPoint = pt;
                acadDb.ModelSpace.Add(dbText);
            }
        }

        private void Print(AcadDatabase acadDb, List<ThIfcAnnotation> annotations)
        {
            var markLayerId = acadDb.Database.CreateAIRoomMarkLayer();
            var textStyleId = acadDb.Database.ImportTextStyle("TH-STYLE3");
            annotations.OfType<ThIfcTextNote>().ForEach(o =>
            {
                var dbText = new DBText
                {
                    TextString = o.Text,
                    TextStyleId = textStyleId,
                    Height = 300,
                    WidthFactor = 0.7,
                    Justify = AttachmentPoint.MiddleCenter,
                    LayerId = markLayerId,
                };
                dbText.AlignmentPoint = o.Geometry.GetMaximumInscribedCircleCenter();
                acadDb.ModelSpace.Add(dbText);
            });
        }

        private List<ThIfcAnnotation> ExtractRoomNames(Database database, Point3dCollection pts)
        {
            var engine = new ThDB3RoomMarkRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements;
        }

        
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
