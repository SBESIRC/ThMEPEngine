using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    public class ThCreateWithdrawalFormCmd : IAcadCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo = new WaterWellPumpConfigInfo();//配置信息
        public void Dispose()
        {
            
        }

        public void Execute()
        {
            //获取选择区域
            var input = ThWGeUtils.SelectPoints();
            //获取集水井
            var water_well_entity_list = ThWGeUtils.GetWaterWellEntityList(input, configInfo.WaterWellInfo.identifyInfo);
            if (water_well_entity_list.IsNull())
            {
                //命令栏提示“未选中集水井”
                //退出本次布置动作
                return;
            }

            foreach(Entity entity in water_well_entity_list)
            {
                //if(集水井包含潜水泵,提取数据)
                //整理数据，合并，统计等操作
                //生成提资表格
            }
        }
    }
}
