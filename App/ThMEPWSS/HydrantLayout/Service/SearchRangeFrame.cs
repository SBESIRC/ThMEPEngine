using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using Linq2Acad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

using NFox.Cad;
using ThMEPEngineCore.Diagnostics;


namespace ThMEPWSS.HydrantLayout.Service
{
    class SearchRangeFrame
    {

        //输入    
        Point3d center;

        //中间变量
        public bool IfFind = false; 

        //输出
        MPolygon LeanWall;
        Polyline Shell = new Polyline();
        List<Curve> Holes = new List<Curve>();

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

            var overlapArea = new DBObjectCollection();
            //搜索所在房间
            if (selectRooms.Count > 0)
            {
                overlapArea = cpolyline.IntersectionMP(selectRooms);
            }
            else 
            {
                return;
            }
            //overlapArea.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1overlap", 2));
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
                    if (pl.Contains(center))
                    {
                        //var plNew = ThMPolygonTool.CreateMPolygon(pl);
                        Shell = pl;
                        IfFind = true;
                        break;
                    }
                }
            }

            if (Shell.Contains(center))
            {
                foreach (Polyline a in overlapArea)
                {
                    if (a == Shell) continue;
                    if (Shell.Contains(a))
                    {
                        Holes.Add(a);
                    }
                }
                var mpl = ThMPolygonTool.CreateMPolygon(Shell, Holes);
                LeanWall = mpl;
            }

        }

        public MPolygon output()
        {
            DrawUtils.ShowGeometry(LeanWall, "l1env", 6);
            return LeanWall;
        }
    }
}
