using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public class OuterBrder
    {
        public DBObjectCollection OuterLineObjs = new DBObjectCollection();
        public DBObjectCollection BuildingObjs = new DBObjectCollection();

        public List<Line> SegLines = new List<Line>();//分割线
        public List<BlockReference> Buildings = new List<BlockReference>();//建筑物block
        public Polyline WallLine = new Polyline();//外框线
        public List<BlockReference> Ramps = new List<BlockReference>();//坡道块
        public List<BlockReference> LonelyRamps = new List<BlockReference>();//未合并的坡道

        public ThCADCoreNTSSpatialIndex BuildingSpatialIndex = null;//建筑物SpatialIndex
        public ThCADCoreNTSSpatialIndex LonelyRampSpatialIndex = null;//孤立在地库中间的坡道
        public ThCADCoreNTSSpatialIndex AttachedRampSpatialIndex = null;//依附在地库边界上的坡道
        public List<Ramps> RampLists = new List<Ramps>();//孤立坡道块
        public bool Extract(BlockReference basement)
        {
            Explode(basement);
            BuildingSpatialIndex = new ThCADCoreNTSSpatialIndex(BuildingObjs);
            foreach (var obj in OuterLineObjs)
            {
                var pline = obj as Polyline;
                if (pline.Length > 0.0)
                {
                    pline = pline.DPSimplify(1.0);
                    pline = pline.MakeValid().OfType<Polyline>().OrderByDescending(p => p.Area).First(); // 处理自交
                }
                WallLine = pline;
                break;
            }

            if (WallLine.GetPoints().Count() == 0)//外框线不存在
            {
                Active.Editor.WriteMessage("地库边界不存在！");
                return false;
            }

            if (Buildings.Count == 0)
            {
                Active.Editor.WriteMessage("障碍物不存在！");
                return false;
            }

            if (Ramps.Count == 0)
            {
                return true;
            }

            if (Ramps.Count > 0)//合并坡道至墙线
            {
                Ramps.ForEach(ramp => LonelyRamps.Add(ramp));
                var rampSpatialIndex = new ThCADCoreNTSSpatialIndex(Ramps.ToCollection());
                var rstRamps = rampSpatialIndex.SelectFence(WallLine);
                AttachedRampSpatialIndex = new ThCADCoreNTSSpatialIndex(rstRamps);
                foreach (var ramp in rstRamps)
                {
                    LonelyRamps.Remove(ramp as BlockReference);
                }
                LonelyRampSpatialIndex = new ThCADCoreNTSSpatialIndex(LonelyRamps.ToCollection());
                var splitArea = WallLine.SplitByRamp(rstRamps);
                splitArea = splitArea.DPSimplify(1.0);
                splitArea = splitArea.MakeValid().OfType<Polyline>().OrderByDescending(p => p.Area).First(); // 处理自交
                WallLine = splitArea;
            }

            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                var rst = LonelyRampSpatialIndex.SelectFence(segLine);
                if (rst.Count > 0)
                {
                    var blk = rst[0] as BlockReference;
                    var rect = blk.GetRect();
                    RampLists.Add(new Ramps(segLine.Intersect(rect, 0).First(), blk));

                    SegLines.RemoveAt(i);
                }
            }
            return true;
        }

        private bool IsOuterLayer(string layer)
        {
            return layer.ToUpper() == "地库边界";
        }

        private bool IsBuildingLayer(string layer)
        {
            return layer.ToUpper().Contains("障碍物");
        }

        private bool IsRampLayer(string layer)
        {
            return layer.ToUpper() == "坡道";
        }

        private bool IsSegLayer(string layer)
        {
            return layer.ToUpper() == "分割线";
        }

        private void Explode(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);

            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                AddObjs(ent);
            }
        }

        private void AddObjs(Entity ent)
        {
            if (IsOuterLayer(ent.Layer))
            {
                if (ent is Polyline pline)
                {
                    OuterLineObjs.Add(pline);
                }
            }
            if (IsBuildingLayer(ent.Layer))
            {
                if (ent is BlockReference br)
                {
                    Buildings.Add(br);
                    BuildingObjs.Add(br);
                }
            }
            if (IsRampLayer(ent.Layer))
            {
                if (ent is BlockReference br)
                {
                    Ramps.Add(br);
                    Buildings.Add(br);
                    BuildingObjs.Add(br);
                }
            }
            if (IsSegLayer(ent.Layer))
            {
                if (ent is Line line)
                {
                    SegLines.Add(line);
                }
            }
        }
    }
}
