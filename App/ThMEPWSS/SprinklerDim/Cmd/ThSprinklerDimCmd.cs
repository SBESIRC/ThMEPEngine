using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

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
using ThMEPWSS.SprinklerDim.Data;
using ThMEPWSS.SprinklerDim.Engine;
using ThMEPWSS.SprinklerDim.Service;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Cmd
{
    public class ThSprinklerDimCmd : ThMEPBaseCommand, IDisposable
    {
        ThSprinklerDimViewModel ViewModel;
        public ThSprinklerDimCmd(ThSprinklerDimViewModel vm)
        {
            InitialCmdInfo();
            ViewModel = vm;
        }
        private void InitialCmdInfo()
        {
            ActionName = "标注";
            CommandName = "THPLBZ";
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
                var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);

                var frames = new Point3dCollection();
                if (debugSwitch)
                {
                    frames = ThSelectFrameUtil.GetRoomFrame();
                }
                else
                {
                    frames = ThSelectFrameUtil.GetFrame();
                }

                if (frames.Count == 0)
                {
                    return;
                }

                var printTag = "";

                if (debugSwitch)
                {
                    printTag = ThMEPWSSUtils.SettingString("");
                }

                var layer = new List<string>() { ThSprinklerDimCommon.Layer_Dim, ThSprinklerDimCommon.Layer_UnTagX, ThSprinklerDimCommon.Layer_UnTagY };
                var dimST = new List<string>() { ThSprinklerDimCommon.Style_DimCAD };
                ThSprinklerDimInsertService.LoadBlockLayerToDocument(acadDatabase.Database, new List<string>(), layer, dimST);
                ThSprinklerDimInsertService.SetCurrentLayer(ThSprinklerDimCommon.Layer_Dim);

                //转换器
                var transformer = ThMEPWSSUtils.GetTransformer(frames);
                //var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                //提取数据
                var dataFactory = new ThSprinklerDimDataFactory()
                {
                    Transformer = transformer,
                };

                dataFactory.GetElements(acadDatabase.Database, frames);

                //处理数据
                var dataProcess = new ThSprinklerDimDataProcessService()
                {
                    InputExtractors = dataFactory.Extractors,
                    RoomIFCData = dataFactory.RoomsData,
                    TchPipeData = dataFactory.TchPipeData,
                    LinePipeData = dataFactory.LinePipeData,
                    LinePipeTextData = dataFactory.LinePipeTextData,
                    SprinklerPt = dataFactory.SprinklerPtData,
                    AxisCurvesData = dataFactory.AxisCurves,
                    PreviousData = dataFactory.PreviousData,
                    Transformer = transformer,
                };

                dataProcess.ProcessData();
                dataProcess.Print();

                var dims = ThSprinklerDimEngine.LayoutDimEngine(dataProcess, printTag, out var xUnDimedPtsAll, out var yUnDimedPtsAll);

                //清除前序结果
                dataProcess.CleanPreviousData();

                //生成结果
                if (debugSwitch)
                {
                    ThSprinklerDimInsertService.ToDebugDim(dims, printTag);
                }

                dims.ForEach(x => x.Reset(transformer));
                xUnDimedPtsAll = xUnDimedPtsAll.Select(x => transformer.Reset(x)).ToList();
                yUnDimedPtsAll = yUnDimedPtsAll.Select(x => transformer.Reset(x)).ToList();

                if (ViewModel.UseTCHDim == 1)
                {
                    ThSprinklerDimInsertService.ToTCHDim(dims, ViewModel.Scale, ViewModel.TCHDBPath);
                }
                else if (ViewModel.UseTCHDim == 0)
                {
                    ThSprinklerDimInsertService.ToCADDim(dims, ViewModel.Scale);
                }

                ThSprinklerDimInsertService.InsertUnTagPt(xUnDimedPtsAll, true);
                ThSprinklerDimInsertService.InsertUnTagPt(yUnDimedPtsAll, false);
            }
        }
    }
}
