using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.AlarmSensorLayout.Sensorlayout
{
    /// <summary>
    /// 计算UCS及其方向
    /// input: 可布置区域轮廓,房间框线
    /// output: 区域的ucs及其方向  
    /// </summary>
    public class SpaceDivider
    {
        private List<Layout> layouts; 
        public Dictionary<Polyline, Vector3d> UCSs = new Dictionary<Polyline, Vector3d>();//UCS轮廓及其方向 
        
        public SpaceDivider()
        {

        }
        
        /// <summary>
        /// 计算UCS
        /// </summary>
        /// <param name="polylines">可布置区域轮廓</param>
        /// <param name="frame">房间框线</param>
        public void Compute(Polyline frame, List<Polyline> polylines)
        {
            layouts = new List<Layout>();
            for(int i=0;i<polylines.Count;i++)
                layouts.Add(new Layout(polylines[i], i));
            //按照方向初步分组
            var TmpGroups=new List<LayoutGroup>();
            foreach(Layout layout in layouts)
            {
                var group = TmpGroups.Find(o => Math.Abs(o.angle - layout.angle) < 10);
                if (group.IsNull())
                {
                    TmpGroups.Add(new LayoutGroup(layout));
                    layout.GroupID = TmpGroups.Count - 1;
                }
                else
                {
                    group.insert(layout);
                    layout.GroupID=TmpGroups.FindIndex(o=>o==group);
                }
            }
            //个数少的组合并到大组中
            var rest = TmpGroups.Where(o => 1.0 * o.layouts.Count / layouts.Count <= 0.25).ToList();
            foreach (var group in rest)
            {
                foreach (var index in group.layouts)
                {
                    var buffer = layouts[index].ent.Buffer(2500).Cast<Polyline>().First();
                    var nearLayouts = layouts.Where(o => o.ent.Intersects(buffer) && 1.0 * TmpGroups[o.GroupID].layouts.Count / layouts.Count > 0.25).ToList();
                    Dictionary<int, int> map = new Dictionary<int, int>();
                    int maxCount = -1;
                    int NewGroupID = layouts[index].GroupID;
                    foreach (var layout in nearLayouts)
                    {
                        if (map.ContainsKey(layout.GroupID) == false)
                            map.Add(layout.GroupID, 1);
                        else map[layout.GroupID]++;
                        if (map[layout.GroupID] > maxCount)
                        {
                            maxCount = map[layout.GroupID];
                            NewGroupID = layout.GroupID;
                        }
                    }
                    layouts[index].GroupID = NewGroupID;
                }
            }
            //处理合并错的元素
            for (int i = 0; i < 2; i++)
            {
                foreach (var layout in layouts)
                {
                    var buffer = layout.ent.Buffer(2500).Cast<Polyline>().First();
                    var nearLayouts = layouts.Where(o => o.ent.Intersects(buffer)).ToList();
                    Dictionary<int, int> map = new Dictionary<int, int>();
                    int maxCount = -1;
                    int NewGroupID = 0;
                    foreach (var near in nearLayouts)
                    {
                        if (map.ContainsKey(near.GroupID) == false)
                            map.Add(near.GroupID, 1);
                        else map[near.GroupID]++;
                        if (map[near.GroupID] > maxCount)
                        {
                            maxCount = map[near.GroupID];
                            NewGroupID = near.GroupID;
                        }
                    }
                    if (!map.ContainsKey(layout.GroupID) || map[layout.GroupID] < 3)
                        layout.GroupID = NewGroupID;
                }
            }
            //计算分组后的UCS,record为（角度，可布置区域的组）
            var groups = new Dictionary<double, DBObjectCollection>();
            foreach(var layout in layouts)
            {
                if (groups.ContainsKey(TmpGroups[layout.GroupID].angle) == false)
                    groups.Add(TmpGroups[layout.GroupID].angle, layout.ent.Buffer(1000));
                else 
                    groups[TmpGroups[layout.GroupID].angle].Add(layout.ent.Buffer(1000).Cast<Polyline>().First());
            }
            //转化为需要的UCS字典
            foreach(var group in groups)
            {
                var UCSs = group.Value.UnionPolygons().Cast<Polyline>().ToList();
                foreach(var UCS in UCSs)
                {
                    if (frame.ToNTSPolygon().Contains(UCS.ToNTSPolygon()))
                        continue;
                    var regions = UCS.GeometryIntersection(frame).Cast<Polyline>().ToList();
                    foreach(var region in regions)
                    {

                        var dbline = new Line(region.GetCentroidPoint(), region.GetCentroidPoint() + new Vector3d(3000, 0, 0));
                        dbline.Rotate(dbline.StartPoint, group.Key / 180 * Math.PI);
                        this.UCSs.Add(region, dbline.EndPoint-dbline.StartPoint);
                    }

                }
            }
        }
    }
    public class Layout
    {
        public Polyline ent;
        public double angle;
        public int ID;
        public int GroupID;

        public Layout(Polyline poly,int index)
        {
            ent = poly;
            //var minRect = ent.OBB();
            //var dir = minRect.GetPoint3dAt(1) - minRect.GetPoint3dAt(0);
            var angle_list = new List<double>();
            var len_list = new List<double>();

            for (int i=0;i<poly.NumberOfVertices-1;i++)
            {
                var dir = poly.GetPoint3dAt(i + 1) - poly.GetPoint3dAt(i);
                var tmpAngle = GetAngle(dir.X, dir.Y);
                int maxIndex = -1;
                for (int j=0;j<angle_list.Count;j++)
                    if(Math.Abs(angle_list[j]-tmpAngle)<5)
                    {
                        maxIndex = j;
                        break;
                    }
                if (maxIndex >= 0)
                    len_list[maxIndex] += dir.Length;

                else
                {
                    angle_list.Add(tmpAngle);
                    len_list.Add(dir.Length);
                }
            }

            double maxLength = -1;
            for (int i=0;i<angle_list.Count;i++)
                if(len_list[i]>maxLength)
                {
                    maxLength = len_list[i];
                    angle = angle_list[i];
                }
            ID = index;
        }

        private double GetAngle(double x, double y)
        {
            var angle = Math.Atan2(y, x) * 180 / Math.PI;
            if (angle > 45 && angle <= 135)
                angle -= 90;
            else if (angle > 135 && angle <= 180)
                angle -= 180;
            else if (angle >= -180 && angle <= -135)
                angle += 180;
            else if (angle > -135 && angle <= -45)
                angle += 90;
            return angle;
        }
    }
    public class LayoutGroup
    {
        public List<int> layouts;
        public double angle;

        public LayoutGroup(Layout layout)
        {
            layouts = new List<int>();
            layouts.Add(layout.ID);
            angle = layout.angle;
        }
        public void insert(Layout layout)
        {
            angle = (angle * layouts.Count + layout.angle) / (layouts.Count + 1);
            layouts.Add(layout.ID);
        }
    }
}
