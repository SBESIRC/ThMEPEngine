using System.Linq;
using System.Collections.Generic;

using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSTextHandleService
    {
        // 文字偏移矩阵
        private static readonly Matrix3d TextMatrix = Matrix3d.Displacement(new Vector3d(0, 300, 0));
        // 行偏移矩阵
        private static readonly Matrix3d RowMatrix = Matrix3d.Displacement(new Vector3d(0, -1000, 0));

        public static void Handle(AcadDatabase activeDb, AcadDatabase configDb, List<Entity> tableObjs, Scale3d scale, DBText dbText, string textString,
            int switchLength, MeterLocation meterLocation, MeterLocation lastMeterLocation, ref Point3d basePoint)
        {
            if (Handle(activeDb, tableObjs, dbText, textString, switchLength) 
                && !meterLocation.Equals(MeterLocation.None) && meterLocation.Equals(lastMeterLocation))
            {
                tableObjs.ForEach(t => t.TransformBy(RowMatrix));

                var insertEngine = new ThPDSBlockInsertEngine();
                insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                basePoint = new Point3d(basePoint.X, basePoint.Y - 1000, 0);
                
                // 暂时忽略单位因子
                // basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
            }
        }

        public static bool Handle(AcadDatabase activeDb, List<Entity> tableObjs, DBText dbText, string textString, int switchLength)
        {
            if (textString.Count() <= switchLength)
            {
                dbText.TextString = textString;
                return false;
            }

            var spaceIndex = textString.LastIndexOf(' ', switchLength);
            var slashIndex = textString.LastIndexOf('/', switchLength);
            var stubIndex = textString.LastIndexOf('-', switchLength);
            var index = -1;
            if (spaceIndex != -1
                && (slashIndex == -1 || spaceIndex >= slashIndex)
                && (stubIndex == -1 || spaceIndex >= stubIndex))
            {
                index = spaceIndex;
            }
            else if (slashIndex != -1
                && (spaceIndex == -1 || slashIndex >= spaceIndex)
                && (stubIndex == -1 || slashIndex >= stubIndex))
            {
                index = slashIndex;
            }
            else if (stubIndex != -1)
            {
                index = stubIndex;
            }
            if (index == -1)
            {
                return false;
            }

            var firstLine = textString.Substring(0, index);
            var secondLine = textString.Substring(index + 1);

            var newText = dbText.Clone() as DBText;
            newText.TransformBy(TextMatrix);
            activeDb.ModelSpace.Add(newText);
            newText.TextString = firstLine;
            dbText.TextString = secondLine;
            tableObjs.Add(newText);
            return true;
        }
    }
}
