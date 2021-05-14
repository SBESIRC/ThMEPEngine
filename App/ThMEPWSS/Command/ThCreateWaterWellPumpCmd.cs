using System;
using Linq2Acad;
using AcHelper.Commands;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Model;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe;

namespace ThMEPWSS.Command
{
    public class ThCreateWaterWellPumpCmd : IAcadCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo = new WaterWellPumpConfigInfo();//配置信息

        public List<Entity> GetWaterWellEntityList(Tuple<Point3d, Point3d> input)
        {
            List<Entity> waterWellEntityList = ThWGeUtils.GetWaterWellEntityList(input, configInfo.WaterWellInfo.identifyInfo);
            using (var database = AcadDatabase.Active())
            {
                foreach (Entity intity in waterWellEntityList)
                {
                    double area = 1.0;
                    if (configInfo.WaterWellInfo.isWaterWellSizeFilter && (area < configInfo.WaterWellInfo.fMinacreage))
                    {
                        waterWellEntityList.Remove(intity);
                        continue;
                    }
                    if (configInfo.PumpInfo.isCoveredWaterWell)
                    {
                        //if(存在水泵,删除该水泵)
                        //if(存在立管,删除该立管)
                    }
                }
            }
            return waterWellEntityList;
        }

        public bool InsertPumpToWaterWell(Entity WaterWell,int index, int number)
        {
            //通过indx查找到边L
            switch(number)
            {
                case 1:
                    {

                    }
                    break;
                case 2:
                    {

                    }
                    break;
                case 3:
                    {

                    }
                    break;
                case 4:
                    {

                    }
                    break;
                default:
                    break;
            }
            return true;
        }
        //集水井靠墙数量为0的情况
        public bool InsertPumpToWaterWell0(Entity WaterWell, int number)
        {
            bool isSquare = true;

            int index = 0;
            if(isSquare)
            {
                //找到距离墙最近的边L
            }
            else
            {
                //找长边中距墙最近的边L
            }
            //在边L上布置泵
            return InsertPumpToWaterWell(WaterWell, index, number);
        }
        //集水井靠墙数量为1的情况
        public bool InsertPumpToWaterWell1(Entity WaterWell, int number)
        {
            bool isSquare = true;
            //需要和产品沟通
            int index = 0;
            if (isSquare)
            {
                //靠墙边布置泵
            }
            else
            {
                if(1 == number)
                {
                    //靠墙边布置泵
                }
                else
                {
                    //if(靠墙边是长边,取靠墙边L)
                    //else(找2条长边中距离其他墙最近的边L)
                }
                
            }
            //在边L上布置泵
            return InsertPumpToWaterWell(WaterWell, index, number);
        }
        //集水井靠墙数量为2的情况
        public bool InsertPumpToWaterWell2(Entity WaterWell, int number)
        {
            bool isSquare = true;
            bool isParallel = true;//靠墙边平行
            int index = 0;
            if (isSquare)
            {
                if(isParallel)
                {
                    //取第一个靠墙边L
                }
                else
                {
                    //找距离车位最大的靠墙边
                    //找不到车位，就取第一个靠墙边L
                }
            }
            else
            {
                if (isParallel)
                {
                    //取第一个靠墙边L
                }
                else
                {
                    //取靠墙边中的长边L
                }
            }
            //在边L上布置泵
            return InsertPumpToWaterWell(WaterWell, index, number);
        }
        //集水井靠墙数量为3的情况
        public bool InsertPumpToWaterWell3(Entity WaterWell, int number)
        {
            bool isSquare = true;
            int index = 0;
            if (isSquare)
            {
                //取靠中间墙的边L
            }
            else
            {
                //if(中间墙是长边L，取边L)
                //else 取第一个长边
            }
            //在边L上布置泵
            return InsertPumpToWaterWell(WaterWell, index, number);
        }

        public bool AddPumpToWaterWell(Entity WaterWell,int number)
        {
            bool isOk = true;
            using (var database = AcadDatabase.Active())
            {
                //检测集水井靠墙数量
                int wallCount = 0;
                switch (wallCount)
                {
                    case 0:
                    {
                        isOk = InsertPumpToWaterWell0(WaterWell,number);
                    }
                        break;
                    case 1:
                    {
                        isOk = InsertPumpToWaterWell1(WaterWell, number);
                    }
                        break;
                    case 2:
                    {
                        isOk = InsertPumpToWaterWell2(WaterWell, number);
                    }
                        break;
                    case 3:
                    {
                        isOk = InsertPumpToWaterWell3(WaterWell, number);
                    }
                        break;
                    default:
                        break;
                }
            }
            return isOk;
        }

        public void Dispose()
        {
            //
        }
        
        public void Execute()
        {
            //获取配置信息
            if(configInfo.PumpInfo.PumpLyoutType == LAYOUTTYPE.DOTCHOICE)
            {
                //获取选择数据 
                //获取集水井区域 
            }
            else if(configInfo.PumpInfo.PumpLyoutType == LAYOUTTYPE.BOXCHOICE)
            {
                //获取选择区域
                var input_points = ThWGeUtils.SelectPoints();

                //获取集水井
                var water_well_entity_list = GetWaterWellEntityList(input_points);
                if(water_well_entity_list.IsNull())
                {
                    //命令栏提示“未选中集水井”
                    //退出本次布置动作
                    return;
                }

                //添加排水泵
                foreach (Entity entity in water_well_entity_list)
                {
                    if(!AddPumpToWaterWell(entity, configInfo.PumpInfo.PumpsNumber))
                    {
                        //提示或写入日志表当前集水井添加泵失败
                    }
                }
            }
        }
    }
}
