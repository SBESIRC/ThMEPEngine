using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;


using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Model;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.HydrantLayout.tmp.Service
{
    class SearchRangeFrame
    {

        //输入    
        Point3d center;

        //中间变量
        public bool IfFind = false; 

        //输出
        MPolygon LeanWall;

        public SearchRangeFrame(Point3d center)
        {
            this.center = center;
            FindFeasibleArea();
        }

       
        //寻找可行面
        public void FindFeasibleArea()
        {
            //画圆
            var c = new Circle(center,Vector3d.ZAxis,Info.Radius);
            var cpolyline = c.Tessellate(1000);
            DrawUtils.ShowGeometry(cpolyline, "l1c");

            //进行空间索引
            var selectRooms = ProcessedData.LeanWallIndex.SelectCrossingPolygon(cpolyline);

            //搜索所在房间
            var overlapArea = cpolyline.Intersection(selectRooms);
            foreach (var a in overlapArea) 
            {
                if (a is MPolygon mpl)
                {
                    if (mpl.Shell().Contains(center))
                    {
                        LeanWall = mpl;
                        IfFind = true;
                        break;
                    }
                }
                else if (a is Polyline pl) 
                {
                    var mpl2 = ThMPolygonTool.CreateMPolygon(pl);
                    if (mpl2.Shell().Contains(center))
                    {
                        LeanWall = mpl2;
                        IfFind = true;
                        break;
                    }
                }
            }


        }

        public MPolygon output()
        {
            DrawUtils.ShowGeometry(LeanWall, "l1env", 6);
            return LeanWall;
        }



    }
}
