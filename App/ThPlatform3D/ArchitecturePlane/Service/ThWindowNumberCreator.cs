using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal class ThWindowNumberCreator : ThNumberCreator
    {
        public ThWindowNumberCreator()
        {
            PreFix = "C";
        }
        /// <summary>
        /// 创建窗编号
        /// </summary>
        /// <param name="windows"></param>
        /// <returns>(文字、文字基点、移动方向)的集合</returns>
        public override List<MarkInfo> Create(List<ThComponentInfo> windows)
        {
            var results = new List<MarkInfo>();
            windows.ForEach(d =>
            {
                var textInfo = Create(d);
                if (textInfo != null)
                {
                    results.Add(textInfo);
                }
            });
            return results;
        }

        public override List<MarkInfo> CreateElevationMarks(List<ThComponentInfo> windows)
        {
            var results = new List<MarkInfo>();
            windows.ForEach(d =>
            {
                var textInfo = CreateElevation(d);
                if (textInfo != null)
                {
                    results.Add(textInfo);
                }
            });
            return results;
        }

        private MarkInfo Create(ThComponentInfo window)
        {
            var width = StringToDouble(window.HoleWidth);
            var height = StringToDouble(window.HoleHeight);
            if(width<=0.0 || height<=0.0)
            {
                return null;
            }
            var mark = BuildMark(width, height);
            var startCord = window.Start.ToPoint3d();
            var endCord = window.End.ToPoint3d();
            if (!startCord.HasValue && !endCord.HasValue)
            {
                return null;
            }            
            var textMovDir = GetTextMoveDirection(startCord.Value, endCord.Value);
            double textRad = GetTextAngle(startCord.Value, endCord.Value);
            var midPt = startCord.Value.GetMidPt(endCord.Value);       
            var windowCode = CreateText(midPt, textRad, mark, TextHeight);

            // 偏移
            var wallThick = window.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
            Move(windowCode, wallThick / 2.0, textMovDir);

            return new MarkInfo()
            {
                Mark = windowCode,
                BelongedLineSp = startCord.Value,
                BelongedLineEp = endCord.Value,
                MoveDir = textMovDir,
            };
        }
        private MarkInfo CreateElevation(ThComponentInfo window)
        {
            var width = StringToDouble(window.HoleWidth);
            var height = StringToDouble(window.HoleHeight);
            if (width <= 0 || height <= 0)
            {
                return null;
            }
            var mark = BuildMark(width, height);
            var centeCoord = window.CenterPoint.ToPoint3d();
            if (!centeCoord.HasValue)
            {
                return null;
            }
            var windowCode = CreateText(centeCoord.Value, 0.0, mark, TextHeight);

            // 偏移
            //var wallThick = door.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
            //Move(doorCode, wallThick / 2.0, textMovDir);

            return new MarkInfo()
            {
                Mark = windowCode,
                //BelongedLineSp = startCord.Value,
                //BelongedLineEp = endCord.Value,
                MoveDir = Vector3d.YAxis,
            };
        }
    }
}
