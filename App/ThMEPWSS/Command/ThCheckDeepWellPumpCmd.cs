using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    public class ThCheckDeepWellPumpCmd : IAcadCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo = new WaterWellPumpConfigInfo();//配置信息

        Dictionary<int, List<ObjectId>> CheckProblems()
        {
            Dictionary<int, List<ObjectId>> problems = new Dictionary<int, List<ObjectId>>();
            //获取选择区域
            var input = ThWGeUtils.SelectPoints();
            //获取集水井
            var water_well_entity_list = ThWGeUtils.GetWaterWellEntityList(input, configInfo.WaterWellInfo.identifyInfo);
            //遍历集水井
            foreach (Entity intity in water_well_entity_list)
            {
                double area = 1.0;
                if (configInfo.WaterWellInfo.isWaterWellSizeFilter && (area < configInfo.WaterWellInfo.fMinacreage))//过滤集水井
                {
                    water_well_entity_list.Remove(intity);
                    continue;
                }

                //判断集水井内范围内，是否有潜水泵（包含交集部分）
                bool isHavePump = true;
                if(isHavePump)
                {
                    //判断，是否超出集水井范围,如果是-->问题3，continue
                    //判断，角度是否一致(设定容差)，如果否-->问题4，continue
                }
                else
                {
                    //集水井没有潜水泵-->问题1，continue
                }
            }
            //获取所有潜水泵
            var deep_well_entity_list = ThWGeUtils.GetEntityFromDatabase(input, "潜水泵-AI");

            //遍历潜水泵
            foreach (Entity intity in water_well_entity_list)
            {
                //判断潜水泵和集水井，是否有交集
                bool isWellPumpCloss = true;
                if(!isWellPumpCloss)//如果没有交集
                {
                    //潜水泵不在集水井内 -->问题2，continue
                }
                //判断是否有编号
                bool isHaveNumber = true;
                if (!isHaveNumber)
                {
                    //问题6，continue
                }
                //判断编号是否合法
                bool isLegalNumber = true;
                if(!isLegalNumber)
                {
                    //问题5，continue
                }
            }

            return problems;
        }
        public void Dispose()
        {
            
        }

        public void Execute()
        {
            //检测问题
            Dictionary<int, List<ObjectId>> problems = CheckProblems();
            //todo: 将问题数据，发送到界面
        }
    }
}
