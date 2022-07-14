using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OInterProcess
{
    public class OSubArea
    {
        public Polygon OutBound { get { return OInterParameter.TotalArea; } }//原始边界
        public readonly Polygon Area;//该区域的面域
        public readonly List<LineString> Walls;
        public readonly List<LineSegment> VaildLanes;//该区域全部车道线(目前是分区线）
        //public readonly List<LineString> SegLines;//该区域全部分区线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        //public readonly List<Ramp> Ramps;//该区域全部的坡道
        //public readonly List<Polygon> BoundingBoxes;//该区域所有建筑物的bounding box
        public int Count = -3;//车位总数

        public OSubArea(Polygon area, List<LineSegment> vaildLanes, List<LineString> walls,List<Polygon> buildings)
        {
            Area = area;
            VaildLanes = vaildLanes;
            Walls = walls;
            Buildings = buildings;
        }
    }
}
