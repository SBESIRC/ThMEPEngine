using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class PipeData
    {
        // input
        Polyline region;
        double buffer { get; set; } = 500;
        double room_buffer { get; set; } = 200;
        List<PipeInput> pipe_inputs { get; set; } = new List<PipeInput>();
        int main_index { get; set; }

        List<List<PipeSegment>> pipe_segments { get; set; }
        List<List<BufferPoly>> equispaced_segments { get; set; }
        List<BufferPoly> shortest_way { get; set; }
    }
}
