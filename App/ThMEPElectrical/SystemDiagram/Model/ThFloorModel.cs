using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

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
        public int FloorNumber { get; set; }

        /// <summary>
        /// 是否是多楼层
        /// </summary>
        public bool IsMultiFloor { get; set; } = false;

        /// <summary>
        /// 多楼层名称
        /// </summary>
        public List<int> MulitFloorName { get; set; }

        /// <summary>
        /// 防火分区信息
        /// </summary>
        public List<ThFireDistrictModel> FireDistricts { get; set; }

        /// <summary>
        /// 楼层PolyLine
        /// </summary>
        public Polyline FloorBoundary { get; set; }

        public ThFloorModel()
        {
            FireDistricts = new List<ThFireDistrictModel>();
        }

        /// <summary>
        /// 初始化楼层
        /// </summary>
        /// <param name="storeys"></param>
        public void InitFloors(Database database, BlockReference FloofBlockReference, List<ThMEPEngineCore.Model.Electrical.ThFireCompartment> fireCompartments, ThCADCore.NTS.ThCADCoreNTSSpatialIndex spatialIndex)
        {
            FloorBoundary = GetBlockOBB(database, FloofBlockReference, FloofBlockReference.BlockTransform);
            var FindFireCompartmentsEntity = spatialIndex.SelectWindowPolygon(FloorBoundary);
            var FindFireCompartments = fireCompartments.Where(e => FindFireCompartmentsEntity.Contains(e.Boundary));
            if (FindFireCompartmentsEntity.Count > 0)
            {
                FindFireCompartments.ForEach(o =>
                {
                    ThFireDistrictModel NewFireDistrict = new ThFireDistrictModel();
                    NewFireDistrict.InitFireDistrict(this.FloorNumber, o);
                    this.FireDistricts.Add(NewFireDistrict);
                });
            }
            else
            {
                ThFireDistrictModel NewFireDistrict = new ThFireDistrictModel()
                {
                    FireDistrictName = this.FloorName
                };
                NewFireDistrict.InitFireDistrict(this.FloorNumber, FloofBlockReference);
                this.FireDistricts.Add(NewFireDistrict);
            }
        }

        /// <summary>
        /// 初始化楼层
        /// </summary>
        /// <param name="storeys"></param>
        public void InitFloors(Polyline floorBoundary, List<ThMEPEngineCore.Model.Electrical.ThFireCompartment> fireCompartments, ThCADCore.NTS.ThCADCoreNTSSpatialIndex spatialIndex)
        {
            FloorBoundary = floorBoundary;
            var FindFireCompartmentsEntity = spatialIndex.SelectCrossingPolygon(FloorBoundary);
            var FindFireCompartments = fireCompartments.Where(e => FindFireCompartmentsEntity.Contains(e.Boundary));
            if (FindFireCompartmentsEntity.Count == fireCompartments.Count)
            {
                FindFireCompartments.ForEach(o =>
                {
                    ThFireDistrictModel NewFireDistrict = new ThFireDistrictModel();
                    NewFireDistrict.InitFireDistrict(this.FloorNumber, o);
                    this.FireDistricts.Add(NewFireDistrict);
                });
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
