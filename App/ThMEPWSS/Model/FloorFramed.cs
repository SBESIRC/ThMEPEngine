using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPWSS.Model
{
    public class FloorFramed
    {
        public string floorUid { get; set; }
        /// <summary>
        /// 楼层名称（参数数据）（当楼层为屋面时，数据为空字符串）
        /// </summary>
        public string floorName { get; set; }
        /// <summary>
        /// 楼层类型（参数数据）（大屋面、小屋面、标准层、非标层）
        /// </summary>
        public string floorType { get; set; }
        /// <summary>
        /// 楼层框定块的Id
        /// </summary>
        public ObjectId blockId { get; set; }
        /// <summary>
        /// 楼层框线的Block，
        /// 注意：这里的BlockReference可能只是是复制出来的数据，不能直接通过该块的ID去找块的信息
        /// </summary>
        public BlockReference floorBlock { get; set; }
        /// <summary>
        /// 楼层框的外轮廓
        /// </summary>
        public Polyline outPolyline { get; set; }
        /// <summary>
        /// 楼层框的开始楼层编号(屋面或没有找到时 -999)
        /// </summary>
        public int startFloorOrder { get; set; }
        /// <summary>
        /// 楼层框的结束楼层编号(屋面或没有找到时 -999)
        /// （可能和开始楼层编号一样）
        /// </summary>
        public int endFloorOrder { get; set; }
        /// <summary>
        /// 楼层框的楼层（最大、最小）
        /// （3-10）->（3,10）   (3-10,9,8)->(3,8,9,10)
        /// </summary>
        public List<int> allFloorOrder { get; set; }
        /// <summary>
        /// 楼层框的基准Id
        /// </summary>
        public Point3d datumPoint { get; set; }
        /// <summary>
        /// 显示的楼层名称
        /// </summary>
        public string floorShowName { get; set; }
        /// <summary>
        /// 宽度(X轴方向)
        /// </summary>
        public double width { get; set; }
        /// <summary>
        /// 高度(Y轴方向)
        /// </summary>
        public double height { get; set; }
        /// <summary>
        /// 楼层的框线角点
        /// </summary>
        public Point3dCollection blockOutPointCollection { get; set; }
        public FloorFramed Clone()
        {
            var newFloor = new FloorFramed();
            newFloor.floorUid = floorUid;
            newFloor.floorName = floorName;
            newFloor.floorType = floorType;
            newFloor.blockId = blockId;
            newFloor.floorBlock = floorBlock.Clone() as BlockReference;
            newFloor.outPolyline = outPolyline.Clone() as Polyline;
            newFloor.startFloorOrder = startFloorOrder;
            newFloor.endFloorOrder = endFloorOrder;
            newFloor.allFloorOrder = new List<int>();
            newFloor.allFloorOrder = allFloorOrder;
            newFloor.datumPoint = new Point3d(datumPoint.X, datumPoint.Y, datumPoint.Z);
            newFloor.floorShowName = floorShowName;
            newFloor.width = width;
            newFloor.height = height;
            newFloor.blockOutPointCollection = new Point3dCollection();
            newFloor.blockOutPointCollection = blockOutPointCollection;
            return newFloor;
        }
        FloorFramed()
        {

        }
        public FloorFramed(BlockReference floorBlock,ObjectId blockId)
        {
            this.floorUid = Guid.NewGuid().ToString();
            this.floorBlock = floorBlock;
            this.blockId = blockId;
            this.allFloorOrder = new List<int>();
            this.outPolyline = floorBlock.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
            var firstPt = outPolyline.GetPoint3dAt(0);
            var pts = new List<Point3d>();
            pts.Add(firstPt);
            for (int i = 1; i < outPolyline.NumberOfVertices; i++)
            {
                var pt = outPolyline.GetPoint3dAt(i);
                if (firstPt.DistanceTo(pt) < 1)
                    continue;
                pts.Add(pt);
            }
            pts.Add(firstPt);
            this.blockOutPointCollection = new Point3dCollection(pts.ToArray());

            this.floorType = BlockTools.GetDynBlockValue(blockId, "楼层类型");
            this.width = Convert.ToDouble(BlockTools.GetDynBlockValue(blockId, "宽度"));
            this.height = Convert.ToDouble(BlockTools.GetDynBlockValue(blockId, "高度"));
            //屋面时楼层编号无效
            var visAttrs = BlockTools.GetAttributesInBlockReference(blockId, true);
            this.floorName = "";
            foreach (var attr in visAttrs)
            {
                if (attr.Key.Equals("楼层编号"))
                {
                    this.floorName = attr.Value;
                    break;
                }
            }
            this.floorShowName = string.Format("{0}{1}", this.floorType, this.floorName);

            startFloorOrder = -999;
            endFloorOrder = -999;
            if (!string.IsNullOrEmpty(this.floorName)) 
            {
                //获取开始楼层编号
                var chars = this.floorName.ToCharArray();
                string num = "";
                for (int i = 0; i < chars.Count(); i++) 
                {
                    if (chars[i] >= 48 && chars[i] <= 57)
                    {
                        num += chars[i];
                    }
                    else if(!string.IsNullOrEmpty(num))
                    {
                        var intNum = Convert.ToInt32(num);
                        if (!this.allFloorOrder.Any(c => c == intNum))
                            this.allFloorOrder.Add(intNum);
                        num = "";
                    }
                }
                if (!string.IsNullOrEmpty(num))
                {
                    var intNum = Convert.ToInt32(num);
                    if (!this.allFloorOrder.Any(c => c == intNum))
                        this.allFloorOrder.Add(intNum);
                    num = "";
                }
            }
            if (this.allFloorOrder.Count > 0) 
            {
                this.allFloorOrder = this.allFloorOrder.OrderBy(c => c).ToList();
                this.startFloorOrder = this.allFloorOrder.FirstOrDefault();
                this.endFloorOrder = this.allFloorOrder.LastOrDefault();
            }
            //楼层框中的定位基点计算 框的Poistion 在左上角落
            var posison = floorBlock.Position;
            var xAxis = Vector3d.XAxis;
            var yAxis = Vector3d.YAxis;
            var angle = floorBlock.Rotation;
            xAxis = xAxis.RotateBy(angle, Vector3d.ZAxis);
            yAxis = yAxis.RotateBy(angle, Vector3d.ZAxis);
            var doubleX = Convert.ToDouble(BlockTools.GetDynBlockValue(blockId, "基点 X"));
            var doubleY = Convert.ToDouble(BlockTools.GetDynBlockValue(blockId, "基点 Y"));
            var realDatumPoint = posison;
            realDatumPoint = realDatumPoint + xAxis.MultiplyBy(doubleX);
            realDatumPoint = realDatumPoint + yAxis.MultiplyBy(doubleY);
            this.datumPoint = realDatumPoint;
        }
    }
}
