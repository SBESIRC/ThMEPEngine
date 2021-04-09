using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.SplitMPolygon.Interface;
using ThMEPEngineCore.SplitMPolygon.Model;

namespace ThMEPEngineCore.SplitMPolygon.Service
{
    public class ThSplitMPolygonService :ISplitMPolygon
    {
        public void Dispose()
        {           
        }
        private Polyline Shell { get; set; }
        private List<Polyline> Holes { get; set; }

        private List<List<ThRectangle>> Grids { get; set; }
        private List<List<bool>> Marks { get; set; }
        public List<Polyline> Split(Polyline shell, List<Polyline> holes)
        {
            Shell = shell;
            Holes = holes;
            /*
             * 获取Shell的BoundBox,用1m*1m的格子对BoundBox分割
             * 获取矩阵标记
             * 去掉不在Shell内，也不在Holes内的格子
             */
            Grids = SplitGrid(shell, 1.0, 1.0);
            Marks = CreateMarks(Grids.Count, Grids[0].Count);
            UpdateMarks(); //对于不在区域内的Rectangle,标记已使用
            return Split();
        }

        private List<Polyline> Split()
        {
            var results = new List<Polyline>();
            int row = Marks.Count;
            int column = Marks[0].Count;
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    if(Marks[i][j])
                    {
                        continue;
                    }
                    var recNumber = FindRecZone(i, j);
                    results.Add(CreateSplitZone(i, j, recNumber.Item1, recNumber.Item2));
                    SetMarks(i, j, recNumber.Item1, recNumber.Item2);
                }
            }
            return results;
        }

        private Polyline CreateSplitZone(int startX,int startY,int endX,int endY)
        {
            int xCount = endX - startX + 1;
            int yCount = endY - startY + 1;
            var startRec = Grids[startX][startY];
            var leftDownPt = new Point2d(startRec.MinX, startRec.MinY);
            var rightUpPt = new Point2d(startRec.MinX+ yCount * startRec.Width, startRec.MinY+ xCount* startRec.Height);
            var rec = new Polyline() { Closed=true};
            rec.AddVertexAt(0, leftDownPt, 0, 0, 0);
            rec.AddVertexAt(1, new Point2d(rightUpPt.X, leftDownPt.Y), 0, 0, 0);
            rec.AddVertexAt(2, rightUpPt, 0, 0, 0);
            rec.AddVertexAt(3, new Point2d(leftDownPt.X, rightUpPt.Y), 0, 0, 0);
            return rec;
        }

        private void SetMarks(int startX, int startY, int endX, int endY)
        {
            for (int i = startX ; i <= endX; i++)
            {
                for (int j = startY; j <= endY; j++)
                {
                    Marks[i][j] = true;
                }
            }
        }

        private Tuple<int,int> FindRecZone(int startRow,int startColumn)
        {
            int endRow = FindEndRow(startRow, startColumn);
            for(int i=startRow;i<= endRow;i++)
            {
                for()
                {

                }
            }


            int endColumn = FindEndColumn(startRow, startColumn);
            
            return Tuple.Create(endRow, endColumn);
        }

        private int FindRowHole(int rowIndex,int columnIndex)
        {
            var rowGrid = Grids[rowIndex];
            var 
            for (int startIndex=columnIndex+1; startIndex< rowGrid.Count; startIndex++)
            {

            }
        }

        private int FindEndRow(int startRow,int startColumn)
        {
            int endRow = startRow;
            for (int i = startRow; i < Grids.Count; i++)
            {
                if (RowHasHole(i, startColumn))
                {
                    break;
                }
                endRow = i;
            }
            return endRow;
        }
        private int FindEndColumn(int startRow, int startColumn)
        {
            int endColumn = startColumn;
            for (int i = startColumn; i < Grids[0].Count; i++)
            {
                if (ColumnHasHole(startRow, i))
                {
                    break;
                }
                endColumn = i;
            }
            return endColumn;
        }

        private bool RowHasHole(int rowIndex,int columnIndex)
        {
            var rowGrids = Grids[rowIndex];
            for(int i = columnIndex;i< rowGrids.Count;i++)
            {
                if(rowGrids[i].InHole)
                {
                    return true;
                }
            }
            return false;
        }
        private bool ColumnHasHole(int rowIndex, int columnIndex)
        {
            for(int i = rowIndex; i<Grids.Count;i++)
            {
                if(Grids[i][columnIndex].InHole)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsInArea(ThRectangle rect)
        {
            var center = rect.Center;
            if (Shell.Contains(center))
            {
                var count = Holes.Where(o => o.Contains(center)).Count();
                rect.InHole = count > 0 ? true : false;
                return count == 0 ? true : false;
            }
            else
            {
                return false;
            }
        }
        private void UpdateMarks()
        {
            int row = Marks.Count;
            int column = Marks[0].Count;
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    if (!IsInArea(Grids[i][j]))
                    {
                        Marks[i][j] = true;
                    }
                }
            }
        }

        private List<List<ThRectangle>> SplitGrid(Polyline shell,double gridX=1, double gridY = 1)
        {
           var grids = new List<List<ThRectangle>>();
           var minPt = shell.GeometricExtents.MinPoint;
           var maxPt = shell.GeometricExtents.MaxPoint;
            double xDis = maxPt.X - minPt.X;
            double yDis = maxPt.Y - minPt.Y;
            int xCount = (int)Math.Ceiling(xDis / gridX);
            int yCount = (int)Math.Ceiling(yDis / gridY);
            for(int i=0;i<yCount;i++)
            {
                var rowGrids = new List<ThRectangle>();
                var rowPt = new Point3d(minPt.X,minPt.Y+i*gridY,0);
                for (int j = 0; j < xCount; j++)
                {
                    var columnPt = new Point3d(rowPt.X+j* gridX, rowPt.Y, 0);
                    rowGrids.Add(CreateRectangle(columnPt, gridX, gridY));
                }
                grids.Add(rowGrids);
            }
            return grids;
        }
        private ThRectangle CreateRectangle(Point3d leftDownPt, double x, double y)
        {
            return new ThRectangle()
            {
                MinX = leftDownPt.X,
                MinY = leftDownPt.Y,
                MaxX = leftDownPt.X + x,
                MaxY = leftDownPt.Y + y
            };
        }
        private List<List<bool>> CreateMarks(int row,int column)
        {
            var results = new List<List<bool>>();
            for (int i =0;i<row;i++)
            {
                var rowMarks = new List<bool>();
                for (int j = 0; j < column; j++)
                {
                    rowMarks.Add(false);
                }
                results.Add(rowMarks);
            }
            return results;
        }
    }
}
