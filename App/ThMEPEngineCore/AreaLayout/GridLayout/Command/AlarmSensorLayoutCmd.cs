using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NetTopologySuite.Geometries;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.GridLayout.Sensorlayout;


namespace ThMEPEngineCore.AreaLayout.GridLayout.Command
{
  public  class AlarmSensorLayoutCmd : ThMEPBaseCommand
    {
        private BeamSensorOpt sensorOpt;
        private List<ObjectId> pointId_list { get; set; } = new List<ObjectId>();
        private List<ObjectId> blind_List { get; set; } = new List<ObjectId>();
        private List<ObjectId> detect_List { get; set; } = new List<ObjectId>();
        private List<ObjectId> UCS_list { get; set; } = new List<ObjectId>();
        //inputs
        public Polyline frame { get; set; }//房间外框线
        public List<Polyline> holeList { get; set; }//洞
        public List<MPolygon> layoutList { get; set; }//可布置区域
        public List<Polyline> wallList { get; set; } //墙
        public List<Polyline> columns { get; set; }//柱子
        public List<Polyline> prioritys { get; set; }//优先级更高点位，比如要躲避已布置好的区域
        public List<Polyline> detectArea { get; set; }//探测区域
        public Dictionary<Polyline,Vector3d> ucs { get; set; }//UCS， maybe input or output

        public double protectRadius { get; set; }//保护半径
        public BlindType equipmentType { get; set; }//盲区类型

        //outputs
        public List<Point3d> layoutPoints { get; set; }//布置点位
        public List<Polyline> blinds { get; set; }//盲区
        public override void SubExecute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    //输入区域
                    InputArea input_Area = null;
                    if (ucs != null && ucs.Count > 0)
                    {
                        //use input areas
                        input_Area = new InputArea(frame, layoutList, holeList, wallList, columns, prioritys, detectArea, ucs);
                    }
                    else
                    {
                        //区域分割
                        SpaceDivider spaceDivider = new SpaceDivider();
                        spaceDivider.Compute(frame, layoutList);
                        ucs = spaceDivider.UCSs;
                        input_Area = new InputArea(frame, layoutList, holeList, wallList, columns, prioritys, detectArea, spaceDivider.UCSs);
                    }
                    //输入参数
                    var equipmentParameter = new EquipmentParameter(protectRadius, equipmentType);
                    //初始化布点引擎
                    sensorOpt = new BeamSensorOpt(input_Area, equipmentParameter);
                    sensorOpt.Calculate();
                    //输出参数
                    blinds = sensorOpt.Blinds;
                    layoutPoints = sensorOpt.PlacePoints;
                    ShowPoints();
                    ShowBlind();
                    //ShowDetect();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

              
            }
        }


        private void ShowPoints()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var id in pointId_list)
                {
                    id.Erase();
                }
                pointId_list.Clear();
                foreach (var p in layoutPoints)
                {
                    var circle = new Circle(p, Vector3d.ZAxis, 100);
                    circle.ColorIndex = 4;
                    var id = acadDatabase.ModelSpace.Add(circle);
                    pointId_list.Add(id);
                }
                //if (sensorOpt.validPoints[i][j])
                //{
                //    var mpoly = sensorOpt.Detect[i][j].ToDbMPolygon();
                //    mpoly.ColorIndex = 6;
                //    var id = acadDatabase.ModelSpace.Add(mpoly);
                //    id.Rotate(center, -angle);
                //    pointId_list.Add(id);
                //}
            }
        }
        private void ShowDetect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var d in sensorOpt.Detect)
                {
                    var dbDetect = d.ToDbMPolygon();
                    dbDetect.ColorIndex = 4;
                    acadDatabase.ModelSpace.Add(dbDetect);
                }
            }
        }
        private void ShowBlind()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach(var blind in blinds)
                {
                    blind.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(blind);
                }
            }
        }
    }
}
