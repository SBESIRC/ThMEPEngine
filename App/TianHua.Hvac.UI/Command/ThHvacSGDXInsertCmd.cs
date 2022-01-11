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
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacSGDXInsertCmd : ThMEPBaseCommand, IDisposable
    {
        private string SGDXBlkName = "";
        private string Attribute1DefinitionName = "冷/热水量"; //Table中查询KeyWord
        private string Attribute2DefinitionName = "冷/热负荷"; //Table中查询KeyWord

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
                    ImportLayers(); // 优先用导入的图层
                    CreateLayer();  // 如果没有，则创建
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
                        var attribute1 = RoomAirVolumeQuery.Query(withInRooms[0], Attribute1DefinitionName); // "冷/热水量"
                        var attribute2 = RoomAirVolumeQuery.Query(withInRooms[0], Attribute2DefinitionName); // "冷/热负荷"
                        var attribute1Str = RoomAirVolumeQuery.ConvertToString(attribute1);
                        var attribute2Str = RoomAirVolumeQuery.ConvertToString(attribute2);
                        InsertBlock(wcsPt, attribute1Str, attribute2Str);
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
                acadDb.Database.OpenAILayer("0");
                if (!acadDb.Layers.Contains(ThMEPEngineCoreLayerUtils.Note))
                {
                    acadDb.Database.CreateAINoteLayer();
                }
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

        private void ImportLayers()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPEngineCoreLayerUtils.Note), true);
            }
        }

        private void InsertBlock(Point3d position, string attribute1Value,string attribute2Value)
        {
            // attribute1Value->"冷/热水量",attribute2Value->"制冷量/制热量"
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> 
                { 
                    { Attribute1DefinitionName, FormatAttribute1(attribute1Value)},
                    { "制冷量/制热量", FormatAttribute2(attribute2Value)}
                };
                var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(ThMEPEngineCoreLayerUtils.Note, SGDXBlkName, 
                    Point3d.Origin, new Scale3d(1.0), 0.0, attNameValues);
                var blk = acadDb.Element<BlockReference>(blkId);
                blk.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var mt = Matrix3d.Displacement(blk.Position.GetVectorTo(position));
                blk.TransformBy(mt);
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

        private string FormatAttribute1(string value)
        {
            string linkStr = "(" + "m3/h" + ")";
            if (string.IsNullOrEmpty(value))
            {
                return "-/-"+ linkStr;
            }
            string firstStr = "-";
            string secondStr = "-";
            string[] values = value.Split('/');
            if(values.Length==1)
            {
                firstStr = GetDataStr(values[0]);
            }
            else if(values.Length == 2)
            {
                firstStr = GetDataStr(values[0]);
                secondStr = GetDataStr(values[1]);
            }
            else
            {
                //
            }
            return firstStr + "/" + secondStr+ linkStr;
        }

        private string GetDataStr(string number)
        {
            double firstValue = 0.0;
            if (double.TryParse(number, out firstValue))
            {
                return number;
            }
            else
            {
                return "-";
            }
        }

        private string FormatAttribute2(string value)
        {
            // value->"制冷量/制热量"
            var firstStr = "-";
            var secondStr = "-";
            var values = value.Split('/');
            if (values.Length == 1)
            {
                firstStr = GetDataStr(values[0]);
            }
            else if (values.Length == 2)
            {
                firstStr = GetDataStr(values[0]);
                secondStr = GetDataStr(values[1]);
            }
            else
            {
                //
            }
            if (!string.IsNullOrEmpty(firstStr) && !firstStr.ToUpper().Contains("KW"))
            {
                firstStr += "kW";
            }
            if (!string.IsNullOrEmpty(secondStr) && !secondStr.ToUpper().Contains("KW"))
            {
                secondStr += "kW";
            }
            return firstStr + "/" + secondStr;
        }
    }
}
