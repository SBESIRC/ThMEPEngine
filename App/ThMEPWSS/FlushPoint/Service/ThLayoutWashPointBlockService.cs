using System;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Command;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThLayoutWashPointBlockService
    {
        private WashPointLayoutData LayoutData { get; set; }
        /// <summary>
        /// 墙、柱索引
        /// </summary>
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        /// <summary>
        /// 房间索引
        /// </summary>
        private ThCADCoreNTSSpatialIndex RoomSpatialIndex { get; set; }
        public ThLayoutWashPointBlockService(WashPointLayoutData layOutData)
        {
            LayoutData = layOutData;        
        }
        public Dictionary<Point3d, BlockReference> Layout()
        {
            var results = new Dictionary<Point3d, BlockReference>();
            if (LayoutData==null || !LayoutData.IsValid)
            {
                return results;
            }
            BuildSpatialIndex();
            //var blk1Dic = new Dictionary<Point3d, Vector3d>(); // 给水角阀平面(暂时不要)
            var blk2Dic = new Dictionary<Point3d, Vector3d>(); // 给水角阀带定位
            LayoutData.WashPoints.ForEach(o =>
            {
                bool isOnObj = false;
                var vec = CalculateDirection(o,out isOnObj);
                if(isOnObj)
                {
                    //将图块沿垂直柱边方向向外平移100距离
                    var pt = o + vec.GetNormal().MultiplyBy(100.0);
                    if (!blk2Dic.ContainsKey(pt))
                    {
                        blk2Dic.Add(pt, vec);
                    }
                }
                else
                {
                    if (!blk2Dic.ContainsKey(o))
                    {
                        blk2Dic.Add(o, vec);
                    }
                }
            });
            SetDatabaseDefaults();
            //var blk1Res = InsertBlock(blk1Dic, LayoutData.WashPointLayerName, LayoutData.WashPointBlkName);
            //blk1Res.ForEach(b => results.Add(b.Key, b.Value));
            var blk2Res = InsertBlock(blk2Dic, LayoutData.WashPointLayerName, LayoutData.LocatedVerticalPipeBlkName);
            blk2Res.ForEach(b => results.Add(b.Key, b.Value));
            return results;
        }
        private void SetDatabaseDefaults()
        {
            using (var currentDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                //currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(LayoutData.WashPointBlkName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(LayoutData.LocatedVerticalPipeBlkName), true);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(LayoutData.WashPointLayerName), true);
                SetLayerDefaults(LayoutData.WashPointLayerName);
            }
        }
        private void SetLayerDefaults(string name)
        {
            using (var currentDb = AcadDatabase.Active())
            {
                currentDb.Database.UnOffLayer(name);
                currentDb.Database.UnLockLayer(name);
                currentDb.Database.UnPrintLayer(name);
                currentDb.Database.UnFrozenLayer(name);
            }
        }
        private void BuildSpatialIndex()
        {
            var objs = new DBObjectCollection();
            LayoutData.Walls.ForEach(o => objs.Add(o));
            LayoutData.Columns.ForEach(o => objs.Add(o));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var roomObjs = new DBObjectCollection();
            LayoutData.Rooms.ForEach(o=>roomObjs.Add(o));
            RoomSpatialIndex = new ThCADCoreNTSSpatialIndex(roomObjs);
        }
        private Vector3d CalculateDirection(Point3d pt,out bool isOnObj)
        {
            isOnObj = false;
            var square = ThDrawTool.CreateSquare(pt, LayoutData.PtRange);
            var objs = SpatialIndex.SelectCrossingPolygon(square);
            if(objs.Count==0)
            {
                var rooms = RoomSpatialIndex.SelectCrossingPolygon(square);
                if(rooms.Count>0)
                {
                    isOnObj = true;
                    var obj = GetClosestObj(rooms, pt);
                    var edge = GetPropertyEdge(obj, pt);
                    var leftVec = edge.LineDirection().GetPerpendicularVector().GetNormal();
                    var rightVec = leftVec.Negate();
                    var pt1 = pt + leftVec.MultiplyBy(LayoutData.PtRange + 1);
                    return IsContains(obj, pt1) ? leftVec : rightVec;
                }
                else
                {
                    return Vector3d.XAxis;
                }                
            }
            else
            {
                var obj = GetClosestObj(objs, pt);
                isOnObj = true;
                var edge = GetPropertyEdge(obj, pt);
                var leftVec = edge.LineDirection().GetPerpendicularVector().GetNormal();
                var rightVec = leftVec.Negate();
                var pt1 = pt + leftVec.MultiplyBy(LayoutData.PtRange + 1);
                return IsContains(obj, pt1) ? rightVec : leftVec;
            }
        }
        private DBObject GetClosestObj(DBObjectCollection objs, Point3d pt)
        {
            var disDic = new Dictionary<DBObject, double>();
            objs.Cast<DBObject>().ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    var closePt = polyline.GetClosestPointTo(pt, false);
                    disDic.Add(o, pt.DistanceTo(closePt));
                }
                else if(o is MPolygon mPolygon)
                {
                    var dis = mPolygon.Loops().Select(e =>
                    {
                        var closePt = e.GetClosestPointTo(pt, false);
                        return pt.DistanceTo(closePt);
                    }).OrderBy(d=>d).First();
                    disDic.Add(o, dis);
                }
            });
            return disDic.OrderBy(o => o.Value).First().Key;
        }
        private List<Line> GetLines(DBObject dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                return GetLines(polyline);
            }
            else if (dbObj is MPolygon mPolygon)
            {
                return GetLines(mPolygon);
            }
            else
            {
                return new List<Line>();
            }
        }
        private List<Line> GetLines(Polyline poly)
        {
            return poly.ExplodeLines();
        }
        private List<Line> GetLines(MPolygon polygon)
        {
            return polygon.Loops().SelectMany(o=> GetLines(o)).ToList();
        }
        private Line GetPropertyEdge(DBObject obj,Point3d pt)
        {
            var lines = new List<Line>();
            var edges = GetLines(obj);
            //优先找点在线段内的线
            lines = edges
                .Select(o => o.ExtendLine(-1.0))
                .Where(o => ThGeometryTool.IsPointOnLine(o.StartPoint, o.EndPoint, pt, 1.0))
                .ToList();

            //找点在端点的
            if (lines.Count==0)
            {
                lines = edges
                    .Where(o => o.StartPoint.DistanceTo(pt) <= 1.0 || o.EndPoint.DistanceTo(pt) <= 1.0)
                    .ToList();
            }

            //找端点距离边最近的
            if (lines.Count == 0)
            {
                var disDic = new Dictionary<Line, double>();
                edges.ForEach(o=>
                {
                    var closePt = o.GetClosestPointTo(pt, false);
                    disDic.Add(o, closePt.DistanceTo(pt));
                });
                var dis = disDic.OrderBy(o => o.Value).First().Value;
                lines = disDic.Where(o => o.Value == dis).Select(o => o.Key).ToList();
            }
            //后续根据规则对线过滤,找出合适的边
            return lines.First(); 
        }
        private bool IsContains(DBObject obj, Point3d pt)
        {
            if (obj is Entity entity)
            {
                return entity.EntityContains(pt);
            }
            return false;
        }
        private Dictionary<Point3d,BlockReference> InsertBlock(Dictionary<Point3d,Vector3d> ptDic,string layer,string blkName)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var results = new Dictionary<Point3d, BlockReference>();
                ptDic.ForEach(o =>
                {
                    var rad = Vector3d.XAxis.GetAngleTo(o.Value, Vector3d.ZAxis);
                    var objId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                        layer, blkName,
                        o.Key, new Scale3d(1.0, 1.0, 1.0), rad);
                    var br = acadDb.Element<BlockReference>(objId);
                    br.ColorIndex = (int)ColorIndex.BYLAYER;
                    br.Linetype = "ByLayer";
                    br.LineWeight = LineWeight.ByLayer;
                    results.Add(o.Key, acadDb.Element<BlockReference>(objId));
                });
                return results;
            }
        }
    }
    public class WashPointLayoutData
    {
        public List<Entity> Walls { get; set; }
        public List<Entity> Columns { get; set; }
        public List<Entity> Rooms { get; set; }
        public string WashPointBlkName { get; set; }
        public string LocatedVerticalPipeBlkName { get; set; }
        /// <summary>
        /// 冲洗点位块的图层名称
        /// </summary>
        public string WashPointLayerName { get; set; }
        /// <summary>
        /// 冲洗点位标注的图层名称
        /// </summary>
        public string WaterSupplyMarkLayerName { get; set; }
        /// <summary>
        /// 冲洗点位标注文字的样式名称
        /// </summary>
        public string WaterSupplyMarkStyle { get; set; }
        /// <summary>
        /// 冲洗点位标注文字的宽度因子
        /// </summary>
        public double WaterSupplyMarkWidthFactor { get; set; }

        public List<Point3d> WashPoints { get; set; }
        /// <summary>
        /// 冲洗点扩大搜索的范围(找到相邻的墙、柱子等)
        /// </summary>
        public double PtRange { get; set; }
        /// <summary>
        /// 引线高度
        /// </summary>
        public double LeaderHeight { get; set; }
        /// <summary>
        /// 引线角度
        /// </summary>
        public double LeaderAngle { get; set; }
        /// <summary>
        /// 文字高度
        /// </summary>
        public double TextHeight { get; set; }
        /// <summary>
        /// 楼层标识
        /// </summary>
        public string FloorSign { get; set; }
        /// <summary>
        /// 图纸比例
        /// </summary>
        public string PlotScale { get; set; }
        public Database Db { get; set; }
        public WashPointLayoutData()
        {
            PtRange = 5.0;
            LeaderAngle = 45;
            TextHeight = 3.5;
            PlotScale = "1:1";
            WaterSupplyMarkWidthFactor = 0.7;
            WashPointBlkName = "给水角阀平面";
            WashPointLayerName = "W-WSUP-EQPM"; 
            WaterSupplyMarkStyle = "TH-STYLE3";
            WaterSupplyMarkLayerName = "W-WSUP-NOTE";
            LocatedVerticalPipeBlkName = "给水角阀带立管";
            Walls = new List<Entity>();            
            Rooms = new List<Entity>();
            Columns = new List<Entity>();
            WashPoints = new List<Point3d>();
            FloorSign = THLayoutFlushPointCmd.FlushPointVM.Parameter.FloorSign;
            PlotScale = THLayoutFlushPointCmd.FlushPointVM.Parameter.PlotScale;
        }
        public bool IsValid
        {
            get
            {
                return Check();
            }
        }
        private bool Check()
        {
            if (WashPoints.Count == 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(WashPointBlkName))
            {
                return false;
            }
            if(string.IsNullOrEmpty(WashPointLayerName))
            {
                return false;
            }
            if (Db == null)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 文字高度乘以比例后的高度
        /// </summary>
        /// <returns></returns>
        public double GetMarkTextSize()
        {
            string pattern = @"\d+";
            var rg = new Regex(pattern);
            var nums =rg.Matches(PlotScale).Cast<Match>().Select(o=>double.Parse(o.Value)).ToList();
            if (nums.Count == 2 && nums[1] > 0)
            {
                return TextHeight * nums[1];
            }
            else
            {
                return TextHeight;
            }
        }
        public double GetLeaderYForwardLength(double ratio = 3.0)
        {
            if(LeaderHeight>0)
            {
                return LeaderHeight;
            }
            else
            {
                var textSize = GetMarkTextSize();
                return textSize * ratio;
            }
        }
        public double GetLeaderXForwardLength()
        {
            var height = GetLeaderYForwardLength();
            return height / Math.Tan(ThAuxiliaryUtils.AngToRad(LeaderAngle));
        }
    }
}
