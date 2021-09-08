using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;

namespace ThMEPElectrical.AlarmSensorLayout.Data
{
    public class InputArea
    {
        public Polyline room { get; private set; }//房间区域
        public List<Polyline> layout_area { get; private set; }//可布置区域
        public List<Polyline> detect_area { get; private set; }//探测区域
        public List<Polyline> holes { get; private set; }//洞
        public List<Polyline> walls { get; private set; }//墙
        public List<Polyline> columns { get; private set; }//柱
        public List<Polyline> prioritys { get; private set; }//更高级别的点位
        public Dictionary<Polyline, Vector3d> UCS { get; private set; }//UCS
        public InputArea(Polyline room,
                         List<Polyline> layout_area,
                         List<Polyline> holes = null, List<Polyline> walls = null, List<Polyline> columns = null, List<Polyline> prioritys = null,
                         List<Polyline> detect_area = null,
                         Dictionary<Polyline, Vector3d> UCS = null)
        {
            this.room = room;
            this.layout_area = layout_area;//可能为空

            if (holes == null)
                this.holes = new List<Polyline>();
            else this.holes = holes;

            if (walls == null)
                this.walls = new List<Polyline>();
            else this.walls = walls;

            if (columns == null)
                this.columns = new List<Polyline>();
            else this.columns = columns;

            if (prioritys == null)
                this.prioritys = new List<Polyline>();
            else this.prioritys = prioritys;

            if (detect_area == null)
                this.detect_area = new List<Polyline>();
            else this.detect_area = detect_area;

            if(UCS==null)
                this.UCS = new Dictionary<Polyline, Vector3d>();
            else
            {
                this.UCS = UCS;
                if (this.UCS.Count == 0)
                    this.UCS.Add(room, new Vector3d(1, 0, 0));
                else
                {
                    foreach(var record in UCS)
                    {
                        if (record.Value == null)
                        {
                            UCS.Add(record.Key, new Vector3d(1, 0, 0));
                            UCS.Remove(record.Key);
                        }
                    }
                }
            }
        }
    }
}
