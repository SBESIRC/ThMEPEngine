﻿using System;
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
using ThMEPWSS.SprinklerDim.Data;
using ThMEPWSS.SprinklerDim.Engine;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Cmd
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

                var printTag = ThMEPWSSUtils.SettingString("");

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
                    InputExtractors = dataFactory.Extractors,
                    TchPipeData = dataFactory.TchPipeData,
                    SprinklerPt = dataFactory.SprinklerPtData,
                    AxisCurvesData  = dataFactory.AxisCurves,
                };

                // dataQuery.Transform(transformer);
                //dataProcess.ProcessArchitechData();
                //dataProcess.RemoveDuplicateSprinklerPt();
                //dataProcess.CreateTchPipe();
                //dataProcess.ProjectOntoXYPlane();
                dataProcess.ProcessData();
                dataProcess.Print();

                // 给喷淋点分区
                var netList = ThSprinklerDimEngine.GetSprinklerPtNetwork(dataProcess.SprinklerPt,dataProcess.TchPipe, printTag, out var step);
                netList = ThSprinklerNetGroupListService.ReGroupByRoom(netList, dataProcess.Room, printTag);
                var transNetList = ThOptimizeGroupService.GetSprinklerPtOptimizedNet(netList, step, printTag);
                List<Polyline> mix = new List<Polyline>();
                mix.AddRange(dataProcess.Room);
                mix.AddRange(dataProcess.Wall);

                ThSprinklerNetGroupListService.CutOffLinesCrossWall(transNetList, mix, printTag);
                ThSprinklerNetGroupListService.GenerateCollineation(ref transNetList, step, printTag);

                // 区域标注喷淋点
                ThSprinklerDimensionService.GenerateDimension(transNetList, step, printTag, mix);
                mix.AddRange(dataProcess.Column);
                ThSprinklerDimExtensionService.GenerateRealDimension(transNetList, mix, dataProcess.AxisCurves, printTag, step);



            }
        }
    }
}
