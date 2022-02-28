using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Diagnostics;


namespace ThMEPWSS.HydrantLayoutImprove.Data
{
    internal class ThHydrantLayoutDataQueryService
    {
        //----input
        //public List<ThExtractorBase> InputExtractors { get; set; }

        //----output
        public List<ThIfcTchVPipe> THCVerticalPipe { get;  set; } = new List<ThIfcTchVPipe>();
        public List<ThIfcTchVPipe> BlkVerticalPipe { get;  set; } = new List<ThIfcTchVPipe>();
        public List<ThIfcTchVPipe> CVerticalPipe { get; set; } = new List<ThIfcTchVPipe>();

        public ThHydrantLayoutDataQueryService()
        {

        }

        public void ExtractData()
        {
            //var verticalPipeEx = InputExtractors.Where(o => o is ThTchVerticalPipeExtractService).First() as ThTchVerticalPipeExtractService;
            //var verticalPipeBlkEx = InputExtractors.Where(o => o is ThBlkVerticalPipeExtractor).First() as ThBlkVerticalPipeExtractor;

           
            //THCVerticalPipe.AddRange(verticalPipeEx.TCHVerticalPipe);
            //BlkVerticalPipe.AddRange(verticalPipeBlkEx.VerticalPipe.Select(x => x.Value).ToList());

        }

        public void Print()
        {
            THCVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0THCVerticalPipe"));
            BlkVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0blkVerticalPipe"));
            CVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0cVerticalPipe"));



        }


    }
}
