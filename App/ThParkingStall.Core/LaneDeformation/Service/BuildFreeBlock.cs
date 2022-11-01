using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;

namespace ThParkingStall.Core.LaneDeformation
{
    public class BuildFreeBlock
    {
        
        //类公共变量
        public Vector2D MoveDir = new Vector2D();
        public List<Polygon> OriginalFreeAreaList = new List<Polygon>();

        public List<FreeAreaRec> FreeBlocks;


        //单个Area临时变量
        double maxY = 0;
        double minY = 0;

        public BuildFreeBlock(List<Polygon> originalFreeAreaList,Vector2D dir) 
        {
            MoveDir = dir;
            OriginalFreeAreaList = originalFreeAreaList;
        }

        public void Pipeline() 
        {
            for (int i = 0; i < OriginalFreeAreaList.Count; i++) 
            {
                FromSingleFreeArea(OriginalFreeAreaList[i]);
            }
        }

        public void FromSingleFreeArea(Polygon singleFreeArea) 
        {
            var PointList = singleFreeArea.Coordinates.ToList();

            Polygon tmpObb = singleFreeArea.Boundary as Polygon;
            var obbPointList = tmpObb.Boundary.Coordinates.ToList();

            List<double> yList = GetYList(obbPointList);
            maxY = yList.Last();
            minY = yList.First();

            List<double> xList = GetXList(obbPointList);

            //清楚部分重合点
            xList = IgnoreSmall(xList);
            List<List<double>> XYListMap = GetXYMap(singleFreeArea,xList);



        }

        public List<double> GetXList(List<Coordinate> pointList) 
        {
            List<double> xList = new List<double>();
            for (int i = 0;i< pointList.Count; i++) 
            {
                xList.Add(pointList[i].X);
            }
            return xList.OrderBy(x => x).ToList();
        }

        public List<double> GetYList(List<Coordinate> pointList)
        {
            List<double> yList = new List<double>();
            for (int i = 0; i < pointList.Count; i++)
            {
                yList.Add(pointList[i].Y);
            }
            return yList.OrderBy(x => x).ToList();
        }

        public List<double> IgnoreSmall(List<double> numberList,double threshold= 50) 
        {
            double nowX = numberList[0];
            List<int> deleteIndexList = new List<int>();
            for (int i = 0; i < numberList.Count-1;i++) 
            {
                if (numberList[i+1] - nowX < threshold) 
                {
                    deleteIndexList.Add(i + 1);
                }else nowX = numberList[i+1]; 
            }

            List<double> newList = new List<double>();
            for (int i = 0; i < numberList.Count - 1; i++)
            {
                if (!deleteIndexList.Contains(i))
                {
                    newList.Add(numberList[i]);
                }
            }

            return newList;

        }

        public List<List<double>> GetXYMap(Polygon oPl,List<double> xList) 
        {
            List<List<double>> xyMap = new List<List<double>>();
            for (int i = 0; i < xList.Count; i++) 
            {
                LineSegment line = new LineSegment()
            }
        }
    }
}
