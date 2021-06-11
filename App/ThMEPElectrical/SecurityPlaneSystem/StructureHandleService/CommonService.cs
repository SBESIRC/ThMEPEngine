using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.StructureHandleService
{
    public static class CommonService
    {
        /// <summary>
        /// 获取块内信息
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static List<Entity> GetBlockInfo(BlockReference blockReference)
        {
            var matrix = blockReference.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var results = new List<Entity>();
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId).Clone() as Entity;
                    dbObj.TransformBy(matrix);
                    results.Add(dbObj);
                }
                return results;
            }
        }
    }
}
