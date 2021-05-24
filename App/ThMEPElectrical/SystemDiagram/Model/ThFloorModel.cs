using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 楼层信息
    /// </summary>
    public class ThFloorModel
    {
        /// <summary>
        /// 楼层名称
        /// 可能会用不到
        /// </summary>
        public string FloorName { get; set; }

        /// <summary>
        /// 楼层数
        /// </summary>
        public int? FloorNumber { get; set; }

        /// <summary>
        /// 防火分区信息
        /// </summary>
        public List<ThFireDistrictModel> FireDistricts { get; set; }

        public ThFloorModel()
        {
            FireDistricts = new List<ThFireDistrictModel>();
        }

        // <summary>
        /// 初始化楼层
        /// </summary>
        /// <param name="storeys"></param>
        public void InitFloors(AcadDatabase acadDatabase, BlockReference FloofBlockReference, ThCADCore.NTS.ThCADCoreNTSSpatialIndex spatialIndex)
        {
            List<Entity> FindFireCompartments = new List<Entity>();
            Polyline obb = GetBlockOBB(acadDatabase.Database, FloofBlockReference, FloofBlockReference.BlockTransform);
            FindFireCompartments = spatialIndex.SelectWindowPolygon(obb).Cast<Entity>().ToList();
            foreach (var item in FindFireCompartments)
            {
                item.ColorIndex = 2;
                acadDatabase.ModelSpace.Add(item);
            }
            if (FindFireCompartments.Count > 0)
            {
                int FireDistrictNo = 1;
                FindFireCompartments.ForEach(o =>
                {
                    ThFireDistrictModel NewFireDistrict = new ThFireDistrictModel()
                    {
                        FireDistrictName = this.FloorName + "-" + FireDistrictNo++,
                        DrawFireDistrictNameText = true
                    };
                    NewFireDistrict.InitFireDistrict(o);
                    this.FireDistricts.Add(NewFireDistrict);
                });
            }
            else
            {
                ThFireDistrictModel NewFireDistrict = new ThFireDistrictModel()
                {
                    FireDistrictName = this.FloorName
                };
                NewFireDistrict.InitFireDistrict(FloofBlockReference);
                this.FireDistricts.Add(NewFireDistrict);
            }
        }

        private Polyline GetBlockOBB(Database database, BlockReference blockObj, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var btr = acadDatabase.Blocks.Element(blockObj.BlockTableRecord);
                var polyline = btr.GeometricExtents().ToRectangle().GetTransformedCopy(matrix) as Polyline;
                return polyline;
            }
        }
    }
}
