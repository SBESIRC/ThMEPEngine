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
using ThMEPHVAC.FloorHeatingCoil;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using Dreambuild.AutoCAD;

namespace ThMEPHVAC
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
                Polyline room = acadDatabase.Element<Polyline>(room_result.ObjectId).Clone() as Polyline;
                // input pipe in
                var pipein_result = Active.Editor.GetEntity("get pipein point");
                if (pipein_result.Status != PromptStatus.OK) return;
                var circle = acadDatabase.Element<Circle>(pipein_result.ObjectId);
                List<DrawPipeData> pipe_in = new List<DrawPipeData>();
                pipe_in.Add(new DrawPipeData(circle.Center, circle.Radius, 0, 0));
                // input room_buffer
                double buffer = 500;
                double room_buffer = 100;
                // core process
                RoomPipeGenerator1 roomPipeGenerator = new RoomPipeGenerator1(room, pipe_in, buffer, room_buffer);
                roomPipeGenerator.CalculatePipeline();
                // show result
                var pipe = roomPipeGenerator.output;
                if (pipe.shape != null)
                    acadDatabase.ModelSpace.Add(pipe.shape);
                foreach (var sk in pipe.skeleton)
                    acadDatabase.ModelSpace.Add(sk);
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
                // input in pipe num
                var in_pipe_num_result = Active.Editor.GetInteger("input in pipe num");
                if (in_pipe_num_result.Status != PromptStatus.OK) return;
                int in_pipe_num = in_pipe_num_result.Value;
                // input out pipe num
                var out_pipe_num_result = Active.Editor.GetInteger("input out pipe num");
                if (out_pipe_num_result.Status != PromptStatus.OK) return;
                int out_pipe_num = out_pipe_num_result.Value;
                // input main index
                var main_index_result = Active.Editor.GetInteger("input main index");
                if (main_index_result.Status != PromptStatus.OK) return;
                int main_index = main_index_result.Value;
                // input freedom
                var pouts_freedom = new List<int>(out_pipe_num);
                for (int i = 0; i < out_pipe_num; i++)
                    pouts_freedom.Add(0);
                // input pipe ins
                List<DrawPipeData> pipe_in_list = new List<DrawPipeData>();
                for (int i = 0; i < in_pipe_num; i++)
                {
                    string str = "select in[" + i.ToString() + "]";
                    var pin_result = Active.Editor.GetEntity(str);
                    if (pin_result.Status != PromptStatus.OK)
                        return;
                    var circle = acadDatabase.Element<Circle>(pin_result.ObjectId);
                    pipe_in_list.Add(new DrawPipeData(circle.Center, circle.Radius, 0,i));
                }
                // input pipe outs
                List<DrawPipeData> pipe_out_list = new List<DrawPipeData>();
                for (int i = 0; i < out_pipe_num; i++)
                {
                    string str = "select out[" + i.ToString() + "]";
                    var pout_result = Active.Editor.GetEntity(str);
                    if (pout_result.Status != PromptStatus.OK)
                        return;
                    var circle = acadDatabase.Element<Circle>(pout_result.ObjectId);
                    pipe_out_list.Add(new DrawPipeData(circle.Center, circle.Radius, pouts_freedom[i], i));
                }
                //// input pipe outs freedom
                //for (int i = 0; i < out_pipe_num; i++)
                //{
                //    string str = "select out[" + i.ToString() + "]";
                //    var pout_result = Active.Editor.GetEntity(str);
                //    if (pout_result.Status != PromptStatus.OK)
                //        return;
                //    var line = acadDatabase.Element<Line>(pout_result.ObjectId);
                //    pipe_out_list[i].DoorLeft = line.StartPoint;
                //    pipe_out_list[i].DoorRight = line.EndPoint;
                //}

                double buffer = 400;
                double room_buffer = 100;
                // core process
                PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(room, pipe_in_list, pipe_out_list, main_index, buffer, room_buffer, 0);
                passagePipeGenerator.CalculatePipeline();
                // show result
                var show = passagePipeGenerator.outputs;
                foreach (var pipe in show) 
                {
                    acadDatabase.ModelSpace.Add(pipe.shape);
                    foreach (var sk in pipe.skeleton)
                        acadDatabase.ModelSpace.Add(sk);
                }
                // dispose
                passagePipeGenerator.Dispose();
            }
        }

        [CommandMethod("TIANHUACAD", "THPIPETEST", CommandFlags.Modal)]
        public void THPIPETEST()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polyline
                var result = Active.Editor.GetEntity("select shell");
                if (result.Status != PromptStatus.OK) return;
                Polyline shell = acadDatabase.Element<Polyline>(result.ObjectId).Clone() as Polyline;
                var polys = CenterLineUtils.GetCenterLine(shell);
                foreach (var poly in polys)
                    PassageShowUtils.ShowEntity(poly);
            }
        }

        [CommandMethod("TIANHUACAD", "THPIPECLEAR", CommandFlags.Modal)]
        public void THPIPECLEAR()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polygon
                var roomSelect = Active.Editor.GetEntity("select room");
                if (roomSelect.Status != PromptStatus.OK) return;
                Polyline roomPl = acadDatabase.Element<Polyline>(roomSelect.ObjectId);

                var pipeSelect = Active.Editor.GetEntity("select pipe");
                if (pipeSelect.Status != PromptStatus.OK) return;
                Polyline pipePl = acadDatabase.Element<Polyline>(pipeSelect.ObjectId);

                Polyline newPl = new Polyline();
                //PolylineProcessService.ClearBendsTest(pipePl, roomPl, 400,out newPl);
                newPl = ClearSinglePolyline.ClearBendsLongFirstClosed(pipePl, roomPl, 400);
                DrawUtils.ShowGeometry(newPl, "l8PipeClear", 170, lineWeightNum: 30);
            }
        }

        public void THPIPECLEAR2()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polygon
                var roomSelect = Active.Editor.GetEntity("select room");
                if (roomSelect.Status != PromptStatus.OK) return;
                Polyline roomPl = acadDatabase.Element<Polyline>(roomSelect.ObjectId);

                var pipeSelect = Active.Editor.GetEntity("select pipe");
                if (pipeSelect.Status != PromptStatus.OK) return;
                Polyline pipePl = acadDatabase.Element<Polyline>(pipeSelect.ObjectId);

                Polyline newPl = new Polyline();
                //PolylineProcessService.ClearBendsTest(pipePl, roomPl, 400,out newPl);
                //newPl = ClearSinglePolyline.ClearBendsLongFirstUnClosed(pipePl, roomPl, 400);
                DrawUtils.ShowGeometry(newPl, "l8PipeClear", 170, lineWeightNum: 30);
            }
        }


        [CommandMethod("TIANHUACAD", "MAINPIPE", CommandFlags.Modal)]
        public void MAINPIPE()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polygon
                var roomSelect = Active.Editor.GetEntity("select room");
                if (roomSelect.Status != PromptStatus.OK) return;
                Polyline roomPl = acadDatabase.Element<Polyline>(roomSelect.ObjectId);


                // input pipe num
                var pipe_num_result = Active.Editor.GetInteger("input pipe num");
                if (pipe_num_result.Status != PromptStatus.OK) return;
                int pipe_num = pipe_num_result.Value;

                var main_index_result = Active.Editor.GetInteger("input main index");
                if (main_index_result.Status != PromptStatus.OK) return;
                int main_index = main_index_result.Value;


                var result = Active.Editor.GetEntity("get pipe in");
                if (result.Status != PromptStatus.OK) return;
                var circle = acadDatabase.Element<Circle>(result.ObjectId);
                Point3d pipe_in = circle.Center;

                List<Polyline> PipePlList = new List<Polyline>();
                for (int i = 0; i < pipe_num; i++)
                {
                    string str = "select in[" + i.ToString() + "]";
                    var pin_result = Active.Editor.GetEntity(str);
                    if (pin_result.Status != PromptStatus.OK)
                        continue;
                    var pipePl = acadDatabase.Element<Polyline>(pin_result.ObjectId);
                    PipePlList.Add(pipePl);
                }

                PipePlList.Insert(main_index, new Polyline());

                //
                //Polyline mainPipeArea = GetMainPipeArea(roomPl, PipePlList, main_index);
                //DrawUtils.ShowGeometry(newPl, "l2PipeClear", 170, lineWeightNum: 30);

                //MainPipeGet mainPipeGet = new MainPipeGet(pipe_in, mainPipeArea);
                //mainPipeGet.Pipeline();
            }
        }

        public void MAINPIPE2()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // input polygon
                var roomSelect = Active.Editor.GetEntity("select room");
                if (roomSelect.Status != PromptStatus.OK) return;
                Polyline roomPl = acadDatabase.Element<Polyline>(roomSelect.ObjectId);


                // input pipe num
                var pipe_num_result = Active.Editor.GetInteger("input pipe num");
                if (pipe_num_result.Status != PromptStatus.OK) return;
                int pipe_num = pipe_num_result.Value;

                var main_index_result = Active.Editor.GetInteger("input main index");
                if (main_index_result.Status != PromptStatus.OK) return;
                int main_index = main_index_result.Value;


                //var result = Active.Editor.GetEntity("get pipe in");
                //if (result.Status != PromptStatus.OK) return;
                //var circle = acadDatabase.Element<Circle>(result.ObjectId);
                //Point3d pipe_in = circle.Center;

                List<Polyline> PipePlList = new List<Polyline>();
                for (int i = 0; i < pipe_num; i++)
                {
                    string str = "select in[" + i.ToString() + "]";
                    var pin_result = Active.Editor.GetEntity(str);
                    if (pin_result.Status != PromptStatus.OK)
                        continue;
                    var pipePl = acadDatabase.Element<Polyline>(pin_result.ObjectId);
                    PipePlList.Add(pipePl);
                }

                //PipePlList.Insert(main_index, new Polyline());

                //Polyline mainPipeArea = GetMainPipeArea(roomPl, PipePlList, main_index);
                //DrawUtils.ShowGeometry(newPl, "l2PipeClear", 170, lineWeightNum: 30);

                //MainPipeGet mainPipeGet = new MainPipeGet(PipePlList[main_index], mainPipeArea);
                //mainPipeGet.Pipeline();
            }
        }
    }
}
