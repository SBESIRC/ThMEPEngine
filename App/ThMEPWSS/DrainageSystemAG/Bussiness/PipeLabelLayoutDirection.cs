using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class PipeLabelLayoutDirection
    {
        private double _pipeLabelNearDistance = 500;//文字距离立管最短距离
        private double _pipeLabelMaxDistance = 3500;//文字距离立管最大距离
        private double _pipeLavelDirectionMoveStep = 300;//文字沿着线方向移动步长
        List<PointLabelInfo> _areaAllPipe;
        double _minX;
        double _maxX;
        double _midY;
        Vector3d xAxis = Vector3d.XAxis;
        Vector3d yAxis = Vector3d.YAxis;
        Vector3d xy13 = Vector3d.XAxis;
        Vector3d xy24 = Vector3d.YAxis;
        public PipeLabelLayoutDirection(List<PointLabelInfo> areaAllPipe, double minX, double maxX, double midY) 
        {
            xy13 = (xAxis + yAxis).GetNormal();
            xy24 = (xAxis.Negate() + yAxis).GetNormal();
            _areaAllPipe = new List<PointLabelInfo>();
            _minX = minX;
            _maxX = maxX;
            _midY = midY;
            if (null != areaAllPipe && areaAllPipe.Count > 0) 
                areaAllPipe.ForEach(c => _areaAllPipe.Add(c));
        }
        public void InitData(double pipeLabelNearDistance,double pipeLabelMaxDistance,double pipeLavelDirectionMoveStep) 
        {
            _pipeLabelMaxDistance = pipeLabelMaxDistance;
            _pipeLabelNearDistance = pipeLabelNearDistance;
            _pipeLavelDirectionMoveStep = pipeLavelDirectionMoveStep;
        }
        public List<CheckDirection> GetLayoutDirections(List<PointLabelInfo> thisLinePipes, Point3d centerPoint, double textWidth, double textHeight,double checkDistance) 
        {
            var layoutDirs = new List<CheckDirection>();
            bool noRight = centerPoint.X + textWidth > _maxX;
            bool noLeft = centerPoint.X - textWidth < _minX;
            var startHeight = _pipeLabelNearDistance + textHeight;
            if (noLeft && noRight)
            {
                //左右两侧都不可以布置
                layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                return layoutDirs;
            }
            var otherPipes = _areaAllPipe.Where(c => !thisLinePipes.Any(x => x.BasePoint.DistanceTo(c.BasePoint) < 10)).ToList();
            var nearPipes = otherPipes.Where(c => c.BasePoint.DistanceTo(centerPoint) > 10 && c.BasePoint.DistanceTo(centerPoint) < checkDistance).ToList();
            bool leftHavePipe = nearPipes.Any(c => c.BasePoint.X < centerPoint.X);
            bool rightHavePipe = nearPipes.Any(c => c.BasePoint.X > centerPoint.X);
            bool inUp = centerPoint.Y >= _midY;
            if (thisLinePipes.Count > 1)
                layoutDirs = MiultPipeLayoutDir(noLeft, noRight, leftHavePipe, rightHavePipe, inUp, startHeight);
            else
                layoutDirs = SinglePipeLayoutDir(noLeft, noRight, leftHavePipe, rightHavePipe, inUp, startHeight);
            return layoutDirs;
        }
        List<CheckDirection> MiultPipeLayoutDir(bool noLeft,bool noRight,bool leftHavePipe,bool rightHavePipe,bool inUp,double startHeight) 
        {
            var layoutDirs = new List<CheckDirection>();
            if (!noLeft && !noRight)
            {
                //左右两侧都可以布置
                if (leftHavePipe && rightHavePipe)
                {
                    //两侧都有立管，优先左侧布置
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else 
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        
                    }
                }
                else if (!leftHavePipe && !rightHavePipe)
                {
                    //左右两侧都没有立管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else 
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else if (leftHavePipe)
                {
                    //左侧有立管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else 
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else
                {
                    //右侧有立管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else 
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
            }
            else if (noRight)
            {
                //右侧不可布置
                layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                if (!inUp)
                    layoutDirs.Reverse();
            }
            else
            {
                //左侧不可布置
                layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                if (!inUp)
                    layoutDirs.Reverse();
            }
            return layoutDirs;
        }

        List<CheckDirection> SinglePipeLayoutDir(bool noLeft, bool noRight, bool leftHavePipe, bool rightHavePipe, bool inUp, double startHeight) 
        {
            var layoutDirs = new List<CheckDirection>();
            if (!noLeft && !noRight)
            {
                //两侧都可以布置
                if (leftHavePipe && rightHavePipe)
                {
                    //两侧都有管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else if (!leftHavePipe && !rightHavePipe)
                {
                    //左右都没有管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else if (leftHavePipe)
                {
                    //左侧有管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
                else
                {
                    //右侧有管
                    if (inUp)
                    {
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                    else
                    {
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy13, xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                        layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    }
                }
            }
            else if (noLeft)
            {
                //左侧不可布置，右侧可以布置
                if (inUp)
                {
                    layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy13, xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                else
                {
                    layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(yAxis, xAxis, _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy24.Negate(), xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy13, xAxis, startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
            }
            else
            {
                //右侧不可布置，左侧可以布置
                if (inUp)
                {
                    layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                }
                else
                {
                    layoutDirs.Add(new CheckDirection(yAxis.Negate(), xAxis.Negate(), startHeight, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(yAxis, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy13.Negate(), xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));
                    layoutDirs.Add(new CheckDirection(xy24, xAxis.Negate(), _pipeLabelNearDistance, _pipeLabelMaxDistance, _pipeLavelDirectionMoveStep));

                }
            }
            return layoutDirs;
        }
    }
}
