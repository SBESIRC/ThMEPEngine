using System;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPEngineCore;
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
                ImportLayers();
                ImportBlocks();
                OpenLayer();
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
                            InsertBlock(wcsPt,value);
                        }
                    }                    
                }
                else
                {
                    break;
                }
            }
        }

        private void ImportLayers()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(FGDXLayer), true);
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

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(FGDXLayer);
            }
        }

        private void InsertBlock(Point3d position, double portAirVolume)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> { { "风量", portAirVolume.ToString() + "m3/h" } };
                var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                    FGDXLayer, FGDXBlkName, Point3d.Origin, new Scale3d(1.0), 0.0, attNameValues);
                var blk = acadDb.Element<BlockReference>(blkId);
                blk.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var mt = Matrix3d.Displacement(blk.Position.GetVectorTo(position));
                blk.TransformBy(mt);
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
