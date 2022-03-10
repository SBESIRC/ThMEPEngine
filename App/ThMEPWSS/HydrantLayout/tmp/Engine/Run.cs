using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Data;

using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;
using Linq2Acad;

namespace ThMEPWSS.HydrantLayout.tmp.Engine
{
    class Run
    {
        public RawData rawData;
        public static ThHydrantLayoutDataQueryService dataQueryService;

        public Run(ThHydrantLayoutDataQueryService dataQuery) 
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


            //处理输入数据
            this.rawData = new RawData(dataQuery);
            dataQueryService = dataQuery;
            InputDataProcess inputDataProcess0 = new InputDataProcess(rawData);
            ProcessedData processedData0 = inputDataProcess0.Output();

            //进行寻找
            //修正每一个消防栓
            for (int i = 0; i < processedData0.FireHydrant.Count; i++)
            {
                SingleFireHydrant singleFireHydrant0 = new SingleFireHydrant(processedData0.FireHydrant[i], 0);
            }

        }
    }
}
