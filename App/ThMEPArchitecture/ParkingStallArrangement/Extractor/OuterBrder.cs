using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public class OuterBrder
    {
        public DBObjectCollection OuterLines = new DBObjectCollection();//外框线
        public DBObjectCollection BuildingLines = new DBObjectCollection();//建筑物block
        public DBObjectCollection EquipmentLines = new DBObjectCollection();//机房设备线
        public DBObjectCollection SegmentLines = new DBObjectCollection();//分割线
        public List<Line> SegLines = new List<Line>();//分割线
        public List<BlockReference> Building = new List<BlockReference>();//建筑物block
        public Polyline WallLine = new Polyline();//外框线
        public List<List<Polyline>> buildingPlines = new List<List<Polyline>>();//建筑物hatch提取得到的多段线
        public bool Extract(BlockReference basement)
        {
            var objs = new DBObjectCollection();

            Explode(basement);
            foreach (var db in SegmentLines)
            {
                var spt = (db as Polyline).StartPoint;
                var ept = (db as Polyline).EndPoint;
                SegLines.Add(new Line(spt, ept));
            }

            foreach(var obj in OuterLines)
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

            var buildingPlines = new List<List<Polyline>>();
            foreach(var block in Building)
            {
                buildingPlines.Add(ExplodeBlock(block));
            }
            return true;
        }


        private List<Polyline> ExplodeBlock(BlockReference block)
        {
            var plines = new List<Polyline>();
            var dbObjs = new DBObjectCollection();
            block.Explode(dbObjs);
            foreach(var obj in dbObjs)
            {
                if (obj is Polyline pline)
                {
                    plines.Add(pline);
                }
            }
            return plines;
        }
        private bool IsOuterLayer(string layer)
        {
            return layer.ToUpper() == "地库边界";
        }
        private bool IsBuildingLayer(string layer)
        {
            return layer.ToUpper().Contains("障碍物");
        }
        private bool IsEquipmentLayer(string layer)
        {
            return layer.ToUpper() == "机房";
        }
        private bool IsSegLayer(string layer)
        {
            return layer.ToUpper() == "分割线";
        }

        private void Explode(Entity entity)
        {
            if(entity is BlockReference)
            {
                var dbObjs = new DBObjectCollection();
                entity.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    var ent = obj as Entity;
                    AddObjs(ent);
                }
            }
            if(IsCurve(entity))
            {
                AddObjs(entity);
            }
        }
        private void AddObjs(Entity ent)
        {
            if (IsOuterLayer(ent.Layer))
            {
                AddObjs(ent, OuterLines);
            }
            if (IsBuildingLayer(ent.Layer))
            {
                AddObjs2(ent, BuildingLines);
            }
            if (IsEquipmentLayer(ent.Layer))
            {
                AddObjs(ent, EquipmentLines);
            }
            if (IsSegLayer(ent.Layer))
            {
                AddObjs(ent, SegmentLines);
            }
        }
        private void AddObjs(Entity entity, DBObjectCollection dbObjs)
        {
            if(entity is Polyline)
            {
                dbObjs.Add(entity);
            }
            if(entity is Line line)
            {
                dbObjs.Add(line.ToPolyline());
            }
        }
        private void AddObjs2(Entity entity, DBObjectCollection dbObjs)
        {
            if (entity is BlockReference br)
            {
                Building.Add(br);
                dbObjs.Add(br);
            }
        }
        private bool IsCurve(Entity ent)
        {
            return ent is Polyline ||
                   ent is Line;
        }
    }

    

}
