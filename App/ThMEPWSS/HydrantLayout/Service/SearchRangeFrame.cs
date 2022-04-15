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
       

        public SearchRangeFrame(Point3d center )
        {
            this.center = center;
        }


        public void Pipeline() 
        {
            FindFeasibleArea();
        }


        //寻找可安放区域
        /*
        逻辑如下：
        【1】如果圈内有房间。
        【1.1】如果设备在圈内的某一个房间内，则走正常流程。
        【1.2】如果设备不在这些房间内，把圆当成房间，把其他房间和实体挖除。
        【2】如果圈内没有房间，把圆当成房间，把其他实体挖除。
        */
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

                //如果找到的是polyline，有可能会没读到hole，手动把所有hole都放进去。
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

                //如果不在这个重合区域内，则把自己造的圈当作房间
                if (!IfFind) 
                {
                    var differobj0 = ProcessedData.EntityAggregationIndex.SelectCrossingPolygon(cpolyline);
                    foreach (MPolygon mp in selectRooms) 
                    {
                        differobj0.Add(mp.Shell());
                    }
                    var differedArea = cpolyline.DifferenceMP(differobj0);


                    foreach (var a in differedArea)
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

                    //如果找到的是polyline，有可能会没读到hole，手动把所有hole都放进去。
                    if (Shell.Contains(center))
                    {
                        foreach (Polyline a in differedArea)
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
            }
            else   //周围直接没有房间
            {
                var differobj0 = ProcessedData.EntityAggregationIndex.SelectCrossingPolygon(cpolyline);
                var differedArea = cpolyline.DifferenceMP(differobj0);

                foreach (var a in differedArea)
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

                //如果找到的是polyline，有可能会没读到hole，手动把所有hole都放进去。
                if (Shell.Contains(center))
                {
                    foreach (Polyline a in differedArea)
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
            //overlapArea.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1overlap", 2));
        }

        public MPolygon output()
        {
            DrawUtils.ShowGeometry(LeanWall, "l1env", 6);
            return LeanWall;
        }
    }
}
