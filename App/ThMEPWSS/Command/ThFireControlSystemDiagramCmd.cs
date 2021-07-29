using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Common;
using ThMEPWSS.FireProtectionSystemDiagram.Bussiness;
using ThMEPWSS.FireProtectionSystemDiagram.Models;
using ThMEPWSS.FireProtectionSystemDiagram.Services;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.Command
{
    public class ThFireControlSystemDiagramCmd : IAcadCommand, IDisposable
    {
        FireControlSystemDiagramViewModel _vm;
        public ThFireControlSystemDiagramCmd(FireControlSystemDiagramViewModel vm)
        {
            _vm = vm;
        }
        List<CreateDBTextElement> _createTextElements;
        List<CreateBasicElement> _createBasicElements;
        List<CreateBlockInfo> _createBlockInfos;
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            _createTextElements = new List<CreateDBTextElement>();
            _createBlockInfos = new List<CreateBlockInfo>();
            _createBasicElements = new List<CreateBasicElement>();
            
            //选择开始点后开始放置
            Active.Document.LockDocument();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                PromptPointOptions pPtOpts = new PromptPointOptions("请选择消火栓系统图排布的起点");
                var result = Active.Editor.GetPoint(pPtOpts);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                acdb.Database.LoadBlockLayerToDocument();
                var startPoint = result.Value;
                var floorGroupData = InputDataConvert.SplitFloor(_vm);
                var floorDatas = InputDataConvert.FloorDataModels(floorGroupData);

                //创建消火栓
                var fireHTexts = new List<CreateDBTextElement>();
                var fireHBlocks = new List<CreateBlockInfo>();
                var fireHydrantLayout = new FireHydrantLayout(floorGroupData, floorDatas, _vm);
                var fireHLines = fireHydrantLayout.FireHydrantBlocks(startPoint, out fireHTexts, out fireHBlocks);
                if (null != fireHLines && fireHLines.Count > 0)
                    _createBasicElements.AddRange(fireHLines);
                if (null != fireHTexts && fireHTexts.Count > 0)
                    _createTextElements.AddRange(fireHTexts);
                if (null != fireHBlocks && fireHBlocks.Count > 0)
                    _createBlockInfos.AddRange(fireHBlocks);

                //区域线、标注绘制
                var fireHydrant = new FireHydrantSystem(floorGroupData, floorDatas,_vm);
                var fireLines = fireHydrant.LayoutPipeFireHydrant(startPoint, out fireHTexts, out fireHBlocks);
                if (null != fireLines && fireLines.Count > 0)
                    _createBasicElements.AddRange(fireLines);
                if (null != fireHTexts && fireHTexts.Count > 0)
                    _createTextElements.AddRange(fireHTexts);
                if (null != fireHBlocks && fireHBlocks.Count > 0)
                    _createBlockInfos.AddRange(fireHBlocks);

                var floorWidth = fireHydrant.GetAllWidth() + fireHydrant.GetRaisePipeStart();

                //楼层信息绘制
                LevelFloorUtil levelFloor = new LevelFloorUtil(floorWidth, _vm.FaucetFloor);
                foreach (var item in floorDatas)
                {
                    levelFloor.AddFloorLevel(new LevelFloor(item.floorNum, item.floorLevel, string.Format("{0}F", item.floorNum)));
                }
                var last = floorDatas.Last();
                levelFloor.AddFloorLevel(new LevelFloor(last.floorNum + 1, last.floorLevel, string.Format("RF", last.floorNum)));
                levelFloor.CreateFloorLines(acdb.Database, startPoint);

                //最后生成数据
                CreateBlockService.CreateBasicElement(acdb.Database, _createBasicElements);
                CreateBlockService.CreateTextElement(acdb.Database, _createTextElements);
                CreateBlockService.CreateBlocks(acdb.Database, _createBlockInfos);
            }
                
        }
    }
}
