﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class CreatLoadCalculationTableCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public CreatLoadCalculationTableCmd()
        {
            this.CommandName = "THSCFH";
            this.ActionName = "生成负荷通风计算表";
        }

        public override void SubExecute()
        {
            using (var database = AcadDatabase.Active())
            {
                // 获取房间框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { LoadCalculationParameterFromConfig.Room_Layer_Name });
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                ObjectIdCollection dBObject = new ObjectIdCollection();
                foreach (ObjectId objid in result.Value.GetObjectIds())
                {
                    dBObject.Add(objid);
                }
                var roomEngine = new ThDB3RoomOutlineRecognitionEngine();
                roomEngine.RecognizeMS(database.Database, dBObject);
                var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markEngine = new ThDB3RoomMarkRecognitionEngine();
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var roomBuilder = new ThRoomBuilderEngine();
                roomBuilder.Build(rooms, marks);

                if(rooms.Count==0)
                {
                    return;
                }

                var objs = rooms.Select(o => o.Boundary).ToCollection();
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(objs);

                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                var roomFunctionBlocks = getPrimitivesService.GetRoomFunctionBlocks();

                LogicService logicService = new LogicService();
                var tables=logicService.InsertLoadCalculationTable(rooms.Select(o => o.Boundary).ToList(), roomFunctionBlocks);

                using(AcadDatabase acad=AcadDatabase.Active())
                {
                    foreach (var table in tables)
                    {
                        acad.ModelSpace.Add(table);
                    }
                }
            }
        }
    }
}
