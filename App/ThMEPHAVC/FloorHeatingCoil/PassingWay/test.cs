using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using System.Diagnostics;
using NetTopologySuite.Operation.Buffer;
using ThMEPEngineCore.Diagnostics;
using GeometryExtensions;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class test
    {
        [CommandMethod("TIANHUACAD", "THPIPEROOM", CommandFlags.Modal)]
        public void THPIPEROOM()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // select room
                var room_result = Active.Editor.GetEntity("get room region");
                if (room_result.Status != PromptStatus.OK) return;
                Polyline room = acadDatabase.Element<Polyline>(room_result.ObjectId);
                // select pipe in
                var pipein_result = Active.Editor.GetEntity("get pipein point");
                if (pipein_result.Status != PromptStatus.OK) return;
                var circle = acadDatabase.Element<Circle>(pipein_result.ObjectId);
                // init generator
                PipeInput pipeInput = new PipeInput(circle.Center, circle.Radius);
                RoomPipeGenerator roomPipeGenerator = new RoomPipeGenerator(room, pipeInput, -200);
                // calculate pipeline
                roomPipeGenerator.CalculatePipeline();
                // show result
                var show = roomPipeGenerator.skeleton;
                foreach (Polyline poly in show)
                {
                    acadDatabase.ModelSpace.Add(poly);
                }
                roomPipeGenerator.Dispose();
            }
        }
        [CommandMethod("TIANHUACAD", "THPIPEPASSAGE", CommandFlags.Modal)]
        public void THPIPEPASSAGE()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input region
                var room_result = Active.Editor.GetEntity("select room region");
                if (room_result.Status != PromptStatus.OK) return;
                Polyline room = acadDatabase.Element<Polyline>(room_result.ObjectId);
                // input pipe num
                var pipe_num_result = Active.Editor.GetInteger("input pipe num");
                if (pipe_num_result.Status != PromptStatus.OK) return;
                int pipe_num = pipe_num_result.Value;
                //// input start index
                //var start_index_result = Active.Editor.GetInteger("input start_index");
                //if (start_index_result.Status != PromptStatus.OK) return;
                //int start_index = start_index_result.Value;
                // input main index
                var main_index_result = Active.Editor.GetInteger("input main index");
                if (main_index_result.Status != PromptStatus.OK) return;
                int main_index = main_index_result.Value;
                // input pipe ins
                var pins = new List<Point3d>();
                var pins_buffer = new List<double>();
                for (int i = 0; i < pipe_num; i++)
                {
                    string str = "select in[" + i.ToString() + "]";
                    var pin_result = Active.Editor.GetEntity(str);
                    if (pin_result.Status != PromptStatus.OK)
                        continue;
                    var circle = acadDatabase.Element<Circle>(pin_result.ObjectId);
                    pins.Add(circle.Center);
                    pins_buffer.Add(circle.Radius);
                }
                // input pipe outs
                var pouts = new List<Point3d>();
                var pouts_buffer = new List<double>();
                for (int i = 0; i < pipe_num; i++)
                {
                    string str = "select out[" + i.ToString() + "]";
                    var pout_result = Active.Editor.GetEntity(str);
                    if (pout_result.Status != PromptStatus.OK)
                        continue;
                    var circle = acadDatabase.Element<Circle>(pout_result.ObjectId);
                    pouts.Add(circle.Center);
                    pouts_buffer.Add(circle.Radius);
                }

                for(int i = 0; i < pipe_num; i++)
                {
                    pins[i] = room.GetClosePoint(pins[i]);
                    pouts[i] = room.GetClosePoint(pouts[i]);
                }

                double buffer_dis = -500.0;
                PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(room, pins, pouts, pins_buffer, pouts_buffer, main_index, buffer_dis);
                passagePipeGenerator.CalculatePipeline();
                var show = passagePipeGenerator.skeleton;
                foreach (Polyline poly in show)
                {
                    //poly.ColorIndex = (poly.ColorIndex - 1 + start_index) % 7 + 1;
                    acadDatabase.ModelSpace.Add(poly);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPIPETEST", CommandFlags.Modal)]
        public void THPIPETEST()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polygon
                var result = Active.Editor.GetEntity("select polyline");
                if (result.Status != PromptStatus.OK) return;
                Polyline polyline = acadDatabase.Element<Polyline>(result.ObjectId);
                var points = PassageWayUtils.GetPolyPoints(polyline);
                var poly = PassageWayUtils.BuildPolyline(points);
                poly.Closed = true;
                PassageShowUtils.ShowPoints(points);
                poly.FilletAll(500);
                PassageShowUtils.ShowEntity(poly);
            }
        }
    }
}
