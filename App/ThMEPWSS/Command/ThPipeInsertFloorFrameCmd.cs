using System;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Command
{
    public class ThPipeInsertFloorFrameCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }
        public void Execute()
        {
            ThInsertStoreyFrameService.ImportBlock();
            PromptPointResult result;
            do
            {
                result = Active.Editor.GetPoint("\n选择要插入的基点位置");
                if (result.Status == PromptStatus.OK)
                {                    
                    ThInsertStoreyFrameService.Insert(result.Value);
                }
            } while (result.Status == PromptStatus.OK);
        }
    }
}
