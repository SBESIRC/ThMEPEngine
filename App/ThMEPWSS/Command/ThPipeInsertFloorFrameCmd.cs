using System;
using Linq2Acad;
using AcHelper;
using AcHelper.Commands;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Command
{
  public  class ThPipeInsertFloorFrameCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }
        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptPointResult result;
                var tpipe = new List<Point3d>();
                do
                {
                    result = Active.Editor.GetPoint("\n选择要插入的基点位置");
                    if (result.Status == PromptStatus.OK)
                    {
                        tpipe.Add(result.Value);
                    }
                } while (result.Status == PromptStatus.OK);
                ThInsertStoreyFrameService.Insert(tpipe);
            }
        }
    }
}
