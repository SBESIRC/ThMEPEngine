using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Command;

using ThMEPWSS.Common;
using ThMEPWSS.ThSprinklerDim.Data;
using ThMEPWSS.ThSprinklerDim.Engine;

namespace ThMEPWSS.ThSprinklerDim.Cmd
{
    public class ThSprinklerDimCmd : ThMEPBaseCommand, IDisposable
    {
        public ThSprinklerDimCmd()
        {
            InitialCmdInfo();
            InitialSetting();

        }
        private void InitialCmdInfo()
        {
            ActionName = "标注";
            CommandName = "THPLBZ";
        }
        private void InitialSetting()
        {

        }

        public override void SubExecute()
        {
            SprinklerDimExecute();
        }
        public void Dispose()
        {
        }

        public void SprinklerDimExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frames = ThSelectFrameUtil.GetRoomFrame();
                if (frames.Count == 0)
                {
                    return;
                }

                //转换器
                //var transformer = ThMEPWSSUtils.GetTransformer(frames);
                var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));


                //提取数据
                var dataFactory = new ThSprinklerDimDataFactory()
                {
                    Transformer = transformer,
                };

                dataFactory.GetElements(acadDatabase.Database, frames);

                //处理数据
                var dataProcess = new ThSprinklerDimDataProcessService()
                {
                    TchPipeData = dataFactory.TchPipeData,
                    SprinklerPt = dataFactory.SprinklerPtData,

                };

                // dataQuery.Transform(transformer);
                dataProcess.CreateTchPipe();
                dataProcess.ProjectOntoXYPlane();
                dataProcess.Print();

                ThSprinklerDimEngine.GetSprinklerPtNetwork(dataProcess.SprinklerPt, out var DTTol);


            }
        }
    }
}
