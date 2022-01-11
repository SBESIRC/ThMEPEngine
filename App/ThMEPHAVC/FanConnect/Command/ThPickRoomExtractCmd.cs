using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThPickRoomExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public ThPickRoomExtractCmd()
        {
            CommandName = "THSPM";
            ActionName = "选择";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    PromptSelectionOptions options = new PromptSelectionOptions()
                    {
                        AllowDuplicates = false,
                        MessageForAdding = "选择房间框线",
                        RejectObjectsOnLockedLayers = true,
                    };
                    var result = Active.Editor.GetSelection(options);
                    if (result.Status == PromptStatus.OK)
                    {
                        foreach (var obj in result.Value.GetObjectIds())
                        {
                            var entity = database.Element<Entity>(obj);
                            if (entity.Layer.Contains("AI-房间框线"))
                            {
                                ConfigInfo.WaterValveConfigInfo.RoomObb.Add(entity);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

        }
    }
}
