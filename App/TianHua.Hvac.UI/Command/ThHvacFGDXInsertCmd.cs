using System;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPHVAC;
using ThMEPHVAC.Model;
using ThMEPHVAC.Service;


namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFGDXInsertCmd : ThMEPBaseCommand, IDisposable
    {
        private string FGDXLayer = "";
        private string FGDXBlkName = "";
        private List<ThIfcRoom> Rooms { get; set; }
        private ThFGDXParameter Parameter { get; set; }       
        private ThQueryRoomAirVolumeService RoomAirVolumeQuery { get; set; }
        public ThHvacFGDXInsertCmd(ThFGDXParameter parameter,List<ThIfcRoom> rooms)
        {
            ActionName = "";
            CommandName = "";
            FGDXLayer = ThMEPHAVCDataManager.FGDXLayer;
            FGDXBlkName = ThMEPHAVCDataManager.FGDXBlkName;
            Rooms = rooms;
            Parameter = parameter;
            RoomAirVolumeQuery = new ThQueryRoomAirVolumeService();
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var docLock= Active.Document.LockDocument())
            {
                CreateLayer();
                ImportBlocks();
                UserInteract();
            }
        }

        private List<ThIfcRoom> GetRooms()
        {
            var roomSelector = new ThRoomSelector();
            roomSelector.Select();
            return roomSelector.Rooms;
        }

        private void UserInteract()
        {
            ThMEPHAVCCommon.FocusToCAD();
            while(true)
            {
                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if(ppo.Status==PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    var withInRooms = Contains(Rooms,wcsPt);
                    if(withInRooms.Count==1)
                    {
                        var keyWord = ThMEPHAVCDataManager.GetAirVolumeQueryKeyword(Parameter.SystemType);
                        var volume = RoomAirVolumeQuery.Query(withInRooms[0], keyWord);
                        var value = RoomAirVolumeQuery.ConvertToDouble(volume);
                        if (value > 0.0)
                        {
                            InsertBlock(wcsPt, 0.0, value);
                        }
                    }                    
                }
                else
                {
                    break;
                }
            }
        }

        private void CreateLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.CreateLayer(FGDXLayer);  
            }
        }

        private void ImportBlocks()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(FGDXBlkName), true);
            }
        }

        private void InsertBlock(Point3d pos, double angle,double portAirVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> { { "风量", portAirVolume.ToString() + "m3/h" } };
                db.ModelSpace.ObjectId.InsertBlockReference(FGDXLayer, FGDXBlkName, pos, new Scale3d(1.0), angle, attNameValues);
            }
        }

        private List<ThIfcRoom> Contains(List<ThIfcRoom> rooms, Point3d wcsPt)
        {
            var transformer = new ThMEPOriginTransformer(wcsPt);
            var toOriginPt = transformer.Transform(wcsPt);
            rooms.ForEach(o => transformer.Transform(o.Boundary));
            var results = toOriginPt.FindRooms(rooms);
            rooms.ForEach(o => transformer.Reset(o.Boundary));
            return results;
        }
    }
}
