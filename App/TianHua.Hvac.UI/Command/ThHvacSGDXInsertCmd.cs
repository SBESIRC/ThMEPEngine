using System;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Command;
using ThMEPHVAC;
using ThMEPHVAC.Service;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacSGDXInsertCmd : ThMEPBaseCommand, IDisposable
    {
        private string SGDXBlkName = "";
        private string AttributeDefinitionName = "冷/热水量";
        private ThQueryRoomAirVolumeService RoomAirVolumeQuery { get; set; }
        public ThHvacSGDXInsertCmd()
        {
            ActionName = "插水管断线";
            CommandName = "THSGDX";
            SGDXBlkName = ThMEPHAVCDataManager.SGDXBlkName;
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
                var rooms = GetRooms();
                if(rooms.Count>0)
                {
                    CreateLayer();
                    ImportBlocks();
                    UserInteract(rooms);
                }
            }
        }

        private List<ThIfcRoom> GetRooms()
        {
            var roomSelector = new ThRoomSelector();
            roomSelector.Select();
            return roomSelector.Rooms;
        }

        private void UserInteract(List<ThIfcRoom> rooms)
        {
            ThMEPHAVCCommon.FocusToCAD();
            while(true)
            {
                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if(ppo.Status==PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    var withInRooms = Contains(rooms, wcsPt);
                    if(withInRooms.Count==1)
                    {
                        var volume = RoomAirVolumeQuery.Query(withInRooms[0], AttributeDefinitionName);
                        var value = RoomAirVolumeQuery.ConvertToString(volume);
                        if (!string.IsNullOrEmpty(value))
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
                acadDb.Database.CreateAINoteLayer();  
            }
        }

        private void ImportBlocks()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(SGDXBlkName), true);
            }
        }

        private void InsertBlock(Point3d pos, double angle,string volume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> { { AttributeDefinitionName, Format(volume)} };
                db.ModelSpace.ObjectId.InsertBlockReference(ThMEPEngineCoreLayerUtils.Note, SGDXBlkName, 
                    pos, new Scale3d(1.0), angle, attNameValues);
            }
        }

        private List<ThIfcRoom> Contains(List<ThIfcRoom> rooms,Point3d wcsPt)
        {
            var transformer = new ThMEPOriginTransformer(wcsPt);
            var toOriginPt = transformer.Transform(wcsPt);
            rooms.ForEach(o => transformer.Transform(o.Boundary));
            var results = toOriginPt.FindRooms(rooms);
            rooms.ForEach(o => transformer.Reset(o.Boundary));
            return results;
        }

        private string Format(string volume)
        {
            var strs = volume.Split(';');
            var firstStr = "";
            var secondStr = "";
            if (strs.Length>0)
            {
                var last = strs[strs.Length - 1];
                var values = last.Split('/');
                if(values.Length==2)
                {
                    firstStr = Convert(values[0]);
                    secondStr = Convert(values[1]);
                }
                else if (values.Length == 1)
                {
                    firstStr = Convert(values[0]);
                    secondStr = firstStr;
                }
                else
                {
                    //
                }
            }
            return "冷/热水量(m3/h)："+ firstStr+"/"+ secondStr;
        }

        private string Convert(string content)
        {
            if (content.Contains("-"))
            {
                return "-";
            }
            else
            {
                double outValue = 0.0;
                if(double.TryParse(content,out outValue))
                {
                    return outValue.ToString("#0.0");
                }
                else
                {
                    return content;
                }
            }
        }
    }
}
