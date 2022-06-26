using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.IO.SVG;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.ArchitecturePlane.Service
{
    internal class ThDoorNumberCreator : ThNumberCreator
    {
        public ThDoorNumberCreator()
        {
            PreFix = "M";
        }
        /// <summary>
        /// 创建门编号
        /// </summary>
        /// <param name="doors"></param>
        /// <returns>(文字、文字基点、移动方向)的集合</returns>
        public override List<MarkInfo> Create(List<ThComponentInfo> doors)
        {
            var results = new List<MarkInfo>();
            doors.ForEach(d =>
            {
                var textInfo = Create(d);
                if(textInfo!=null)
                {
                    results.Add(textInfo);
                }
            });
            return results;
        }

        public override List<MarkInfo> CreateElevationMarks(List<ThComponentInfo> doors)
        {
            var results = new List<MarkInfo>();
            doors.ForEach(d =>
            {
                var textInfo = CreateElevation(d);
                if (textInfo != null)
                {
                    results.Add(textInfo);
                }
            });
            return results;
        }

        private MarkInfo Create(ThComponentInfo door)
        {
            var width = StringToDouble(door.HoleWidth);
            var height = StringToDouble(door.HoleHeight);
            if(width <= 0 || height <= 0)
            {
                return null;
            }
            var mark = BuildMark(width, height);
            var startCord = door.Start.ToPoint3d();
            var endCord = door.End.ToPoint3d();
            if(!startCord.HasValue && !endCord.HasValue)
            {
                return null;
            }          
            var textMovDir = GetTextMoveDirection(startCord.Value, endCord.Value);            
            double textRad = GetTextAngle(startCord.Value, endCord.Value);
            var midPt = startCord.Value.GetMidPt(endCord.Value);
            var doorCode = CreateText(midPt, textRad, mark,TextHeight);

            // 偏移
            var wallThick = door.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
            Move(doorCode, wallThick / 2.0, textMovDir);

            return new MarkInfo()
            {
                Mark = doorCode,
                BelongedLineSp = startCord.Value,
                BelongedLineEp = endCord.Value,
                MoveDir = textMovDir,
            };
        }

        private MarkInfo CreateElevation(ThComponentInfo door)
        {
            var width = StringToDouble(door.HoleWidth);
            var height = StringToDouble(door.HoleHeight);
            if (width <= 0 || height <= 0)
            {
                return null;
            }
            var mark = BuildMark(width, height);
            var centeCoord = door.CenterPoint.ToPoint3d();
            if (!centeCoord.HasValue)
            {
                return null;
            }           
            var doorCode = CreateText(centeCoord.Value, 0.0, mark, TextHeight);

            // 偏移
            //var wallThick = door.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
            //Move(doorCode, wallThick / 2.0, textMovDir);

            return new MarkInfo()
            {
                Mark = doorCode,
                //BelongedLineSp = startCord.Value,
                //BelongedLineEp = endCord.Value,
                MoveDir = Vector3d.YAxis,
            };
        }
    }
}
