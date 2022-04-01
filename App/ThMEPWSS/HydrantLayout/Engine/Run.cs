using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPEngineCore.Model.Hvac;

using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;
using Linq2Acad;

namespace ThMEPWSS.HydrantLayout.Engine
{
    class Run
    {
        public RawData rawData;
        public static ThHydrantLayoutDataQueryService dataQueryService;

        //输出数据
        public List<OutPutModel> outPutModels = new List<OutPutModel>();
        public List<ThIfcVirticalPipe> VerticalPipeOut = new List<ThIfcVirticalPipe>();

        public Run(ThHydrantLayoutDataQueryService dataQuery, DataPass dataPass0)
        {
            //var room = dataQuery.Room;
            //var wall = dataQuery.Wall;
            //var column = dataQuery.Column;

            //var obj = new DBObjectCollection();
            //wall.ForEach(x => obj.Add(x));
            //column.ForEach(x => obj.Add(x));
            //Polyline pl = dataQuery.Car[0].Clone() as Polyline ; 
            //var mroom = room.OfType<MPolygon>().ToList();
            //var differ0 = mroom[0].DifferenceMP(obj);
            //differ0.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l0mroom"));

            //全局变量设定
            ParameterSetting(dataPass0);

            //处理输入数据
            this.rawData = new RawData(dataQuery);
            dataQueryService = dataQuery;
            InputDataProcess inputDataProcess0 = new InputDataProcess(rawData);
            ProcessedData processedData0 = inputDataProcess0.Output();
            VerticalPipeOut = processedData0.VerticalPipeOut;

            //进行寻找
            //修正每一个消防栓
            if (Info.Type != 1)
            {
                for (int i = 0; i < processedData0.FireHydrant.Count; i++)
                {
                    SingleFireHydrant singleFireHydrant0 = new SingleFireHydrant(processedData0.FireHydrant[i]);
                    outPutModels.AddRange(singleFireHydrant0.OutPutSingleModel());

                }
            }

            if (Info.Type != 0)
            {
                for (int i = 0; i < processedData0.FireExtinguisher.Count; i++)
                {
                    SingleFireExtinguisher singleFireExtinguisher0 = new SingleFireExtinguisher(processedData0.FireExtinguisher[i]);
                    outPutModels.Add(singleFireExtinguisher0.OutPutSingleModel());
                }
            }
        }



        private void ParameterSetting(DataPass dataPass0)
        {
            //全局变量设定
            Info.Type = dataPass0.LayoutObject;
            Info.Mode = dataPass0.LayoutMode;
            Info.OriginRadius = dataPass0.SearchRadius;
            Info.AllowDoorInPaking = dataPass0.AvoidParking;

            //根据全局参数修改数据
            Info.Radius = Info.OriginRadius + 500;
            Info.SearchRadius = Info.OriginRadius - 300;
        }
    }


    class DataPass
    {
        public int LayoutObject = 2;//消火栓（0）灭火器（1）两者都考虑（2）
        public int SearchRadius = 3500;
        public int LayoutMode = 2; //一字（0） L字（1） 两者都考虑（2）
        public bool AvoidParking = false;  //开门是否避让车位 T:避让 F:不用避让
        public DataPass(int radius, int layoutObj, int layoutMode, bool avoidPaking)
        {
            this.SearchRadius = radius;
            this.LayoutObject = layoutObj;
            this.LayoutMode = layoutMode;
            this.AvoidParking = avoidPaking;
        }
    }

    class OutPutModel
    {
        //
        public bool IfFind = false;

        //
        public int Type = -1;    // 0:立柱 1:消防栓 2：灭火器
        public Point3d CenterPoint = new Point3d();
        public Vector3d Dir = new Vector3d();
        public int DoorOpenDir = -1;  //

        //
        public ThHydrantModel OriginModel = new ThHydrantModel();


        public OutPutModel(bool flag, int type, Point3d centerpoint, Vector3d dir, int doorOpenDir, ThHydrantModel originmodel)
        {
            this.IfFind = flag;
            this.Type = type;
            this.CenterPoint = centerpoint;
            this.Dir = dir;
            this.DoorOpenDir = doorOpenDir;
            this.OriginModel = originmodel;
        }

        public OutPutModel() { }

        public void Reset(ThMEPOriginTransformer transformer)
        {
            if (CenterPoint !=Point3d.Origin )
            {
                transformer.Reset(ref CenterPoint);
            }
        }
    }
}
