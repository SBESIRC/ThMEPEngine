using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchitecturePlane.Service
{
    internal abstract class ThNumberCreator
    {
        protected string PreFix { get; set; } = "";
        protected double TextHeight { get; set; } = 300.0;
        public ThNumberCreator()
        {
            
        }
        public abstract List<MarkInfo> Create(List<ThComponentInfo> components);
        public abstract List<MarkInfo> CreateElevationMarks(List<ThComponentInfo> components);
        public void SetTextHeight(double height)
        {
            TextHeight = height;
        }
        protected double StringToDouble(string size)
        {
            var values = size.GetDoubles();
            if(values.Count==1)
            {
                return values[0];
            }
            else
            {
                return 0.0;
            }
        }  
        protected Vector3d GetTextMoveDirection(Point3d sp,Point3d ep)
        {
            if (Math.Abs(sp.Y - ep.Y) <= 1.0)
            {
                return Vector3d.YAxis; // 水平
            }
            else if (Math.Abs(sp.X - ep.X) <= 1.0)
            {
                return Vector3d.XAxis; // 垂直
            }
            else
            {
                var dir = sp.GetVectorTo(ep).GetPerpendicularVector();
                if(dir.DotProduct(Vector3d.YAxis)>0)
                {
                    return dir;
                }
                else
                {
                    return dir.Negate();
                }
            }
        }
        protected double GetTextAngle(Point3d sp, Point3d ep)
        {
            if(Math.Abs(sp.Y-ep.Y)<=1.0)
            {
                return 0.0; // 水平
            }
            else if(Math.Abs(sp.X - ep.X) <= 1.0)
            {
                return Math.PI/2.0; // 垂直
            }
            else
            {
                var dir = sp.GetVectorTo(ep);
                if ((dir.X > 0 && dir.Y > 0.0) || (dir.X < 0 && dir.Y < 0.0))
                {
                    // 一、三象限，锐角
                    if (sp.X < ep.X)
                    {
                        return Vector3d.XAxis.GetAngleTo(dir);
                    }
                    else
                    {
                        return Vector3d.XAxis.GetAngleTo(dir.Negate());
                    }
                }
                else
                {
                    // 二、四象限, 钝角
                    return Vector3d.XAxis.GetAngleTo(dir) % Math.PI + Math.PI;
                }
            }
        }
        protected DBText CreateText(Point3d position, double rotation,
            string textString, double height, double widthFactor = 1.0)
        {
            return new DBText()
            {
                Height = height,
                WidthFactor = widthFactor,
                Position = position,
                Rotation = rotation,
                TextString = textString,                
                HorizontalMode = TextHorizontalMode.TextMid,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                AlignmentPoint = position,
            };
        }
        protected virtual string BuildMark(double width, double height)
        {
            string widthSuffix = "";
            string heightSuffix = "";
            int wValue = (int)Math.Floor(width / 100);
            if (Math.Abs(width % 100 - 50.0) <= 5.0)
            {
                widthSuffix = "a";
            }
            int hValue = (int)Math.Floor(height / 100);
            if (Math.Abs(height % 100 - 50.0) <= 5.0)
            {
                heightSuffix = "b";
            }
            return PreFix + wValue.ToString().PadLeft(2, '0') +
                hValue.ToString().PadLeft(2, '0') +
                widthSuffix + heightSuffix;
        }
        #region ---------- 偏移文字 -----------
        protected void Move(DBText mark, double offsetH, Vector3d moveVec)
        {
            var geoH = CalculateTextHeight(mark);
            var mt = Matrix3d.Displacement(moveVec.GetNormal().MultiplyBy(offsetH + geoH / 2.0));
            mark.TransformBy(mt);
        }
        private double CalculateTextHeight(DBText text)
        {
            double geoH = text.Height;
            var clone = text.Clone() as DBText;
            var mt = Matrix3d.Rotation(clone.Rotation * -1.0, clone.Normal, clone.Position);
            clone.TransformBy(mt);
            if (clone.Bounds.HasValue)
            {
                geoH = clone.GeometricExtents.MaxPoint.Y - clone.GeometricExtents.MinPoint.Y;
            }
            clone.Dispose();
            return geoH;
        }
        #endregion
    }
    internal class MarkInfo
    {
        public DBText Mark { get; set; }
        public Point3d BelongedLineSp { get; set; }
        public Point3d BelongedLineEp { get; set; }
        public Vector3d MoveDir { get; set; }
    }
}
