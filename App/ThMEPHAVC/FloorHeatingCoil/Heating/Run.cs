using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class Run
    {
        //输入数据
        public List<RawData>  rawDataList = new List<RawData>();

        public Run(ThFloorHeatingDataProcessService dataQuery)
        {

            //全局变量设定
            ParameterSetting();

            //处理输入数据
            List<ThRoomSetModel> roomSet = dataQuery.RoomSet;
            for (int i = 0; i < roomSet.Count; i++) 
            {
                RawData singleRawdata = new RawData(roomSet[i]);
                this.rawDataList.Add(singleRawdata);
            }
        }

        public void Pipeline()
        {

            for (int i = 0; i < rawDataList.Count; i++)
            {
                //数据处理
                RawData rawData0 = rawDataList[i]; 
                DataPreprocess dataPreprocess = new DataPreprocess(rawData0);
                dataPreprocess.Pipeline();

                //提取处理后的数据
                DistributionService distributionService = new DistributionService();
                distributionService.Pipeline();

                //
                FindPointService findPointService = new FindPointService();
                findPointService.Pipeline();

                //
                DrawPipe drawPipe = new DrawPipe();
                drawPipe.Pipeline();
            }
        }


        private void ParameterSetting()
        {
            ////全局变量设定
            //Info.Type = dataPass0.LayoutObject;
            //Info.Mode = dataPass0.LayoutMode;
            //Info.OriginRadius = dataPass0.SearchRadius;
            //Info.AllowDoorInPaking = !dataPass0.AvoidParking;

            ////根据全局参数修改数据
            Parameter.TotalLength = 120000;
        }

        class DataPass
        {
            //public int LayoutObject = 2;//消火栓（0）灭火器（1）两者都考虑（2）
            //public int SearchRadius = 3500;
            //public int LayoutMode = 2; //一字（0） L字（1） 两者都考虑（2）
            //public bool AvoidParking = false;  //开门是否避让车位 T:避让 F:不用避让
            //public DataPass(int radius, int layoutObj, int layoutMode, bool avoidPaking)
            //{
            //    this.SearchRadius = radius;
            //    this.LayoutObject = layoutObj;
            //    this.LayoutMode = layoutMode;
            //    this.AvoidParking = avoidPaking;
            //}
        }
    }
}
