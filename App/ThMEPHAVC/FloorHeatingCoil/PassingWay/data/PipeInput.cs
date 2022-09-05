using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class PipeInput
    {
        public Point3d pin;
        public Point3d pout = new Point3d(0, 0, 0);
        public double in_buffer;
        public double out_buffer = -1;
        public int start_dir = -1;
        public int end_dir = -1;

        public int passage_index { get; set; } = -1;      // 当前过道区域索引
        public int pipe_id = -1;                          // 套房管道索引
        public int door_id = -1;                          // 出口门的索引

        public int end_offset = -1;                       // 出口偏移条数

        public bool in_near_wall = false;                 // 入口是否靠墙
        public bool out_near_wall = false;                // 出口是否靠墙

        public bool is_in_free = false;                   // 入口是否自由移动（未使用）
        public bool is_out_free = false;                  // 出口是否自由移动

        public Point3d door_left;
        public Point3d door_right;
        PipeInput() { }
        public PipeInput(Point3d pin, double in_buffer)
        {
            this.pin = pin;
            this.in_buffer = in_buffer;
        }
        public PipeInput(DrawPipeData pin_data)
        {
            // id
            this.pipe_id = pin_data.PipeId;
            // center
            this.pin = new Point3d((int)pin_data.CenterPoint.X, (int)pin_data.CenterPoint.Y, 0);
            // buffer
            this.in_buffer = pin_data.HalfPipeWidth;
            // freedom
            this.is_in_free = pin_data.Freedom != 0;
            // dir
            this.start_dir = PassageWayUtils.GetDirBetweenTwoPoint(Point3d.Origin, Point3d.Origin + pin_data.OutDir);
        }
        public PipeInput(DrawPipeData pin_data, DrawPipeData pout_data)
        {
            // id
            this.pipe_id = pin_data.PipeId;
            this.door_id = pout_data.DoorId;
            // center
            this.pin = new Point3d((int)pin_data.CenterPoint.X, (int)pin_data.CenterPoint.Y, 0);
            this.pout = new Point3d((int)pout_data.CenterPoint.X, (int)pout_data.CenterPoint.Y, 0);
            // out door
            door_left = new Point3d((int)pout_data.DoorLeft.X, (int)pout_data.DoorLeft.Y, 0);
            door_right = new Point3d((int)pout_data.DoorRight.X, (int)pout_data.DoorRight.Y, 0);
            // buffer
            this.in_buffer = pin_data.HalfPipeWidth;
            this.out_buffer = pout_data.HalfPipeWidth;
            // freedom
            this.is_in_free = pin_data.Freedom != 0;
            this.is_out_free = pout_data.Freedom != 0;
            // dir
            this.start_dir = PassageWayUtils.GetDirBetweenTwoPoint(Point3d.Origin, Point3d.Origin + pin_data.OutDir);
            this.end_dir = PassageWayUtils.GetDirBetweenTwoPoint(Point3d.Origin, Point3d.Origin + pout_data.OutDir);
        }

        public bool check(Point3d p, double hpw, double rb)
        {
            if (!PassageWayUtils.PointOnSegment(p, door_left, door_right))
                return false;
            return p.DistanceTo(door_left) > hpw + rb && p.DistanceTo(door_right) > hpw + rb;
        }
    }
}
