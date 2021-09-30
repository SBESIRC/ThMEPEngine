using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class PipeConnectPipe
    {
        string _layerName = "W-DRAI-VENT-PIPE";
        double _findPipeDistance = 300.0;
        List<CreateBlockInfo> _thisFloorPLPipes;
        List<CreateBlockInfo> _thisFloorTLPipes;
        public PipeConnectPipe(List<CreateBlockInfo> plPipes,List<CreateBlockInfo> tlPipes) 
        {
            _thisFloorPLPipes = new List<CreateBlockInfo>();
            _thisFloorTLPipes =new List<CreateBlockInfo>();
            if (null != plPipes && plPipes.Count > 0) 
                _thisFloorPLPipes.AddRange(plPipes);
            if (null != tlPipes && tlPipes.Count > 0)
                _thisFloorTLPipes.AddRange(tlPipes);
        }
        public List<CreateBasicElement> GetConnectLines() 
        {
            var createBasicElements = new List<CreateBasicElement>();
            if (null == _thisFloorPLPipes || _thisFloorPLPipes.Count < 1 || null == _thisFloorTLPipes || _thisFloorTLPipes.Count < 1)
                return createBasicElements;
            foreach (var plPipe in _thisFloorPLPipes) 
            {
                var nearTL = _thisFloorTLPipes.OrderBy(c => c.createPoint.DistanceTo(plPipe.createPoint)).FirstOrDefault();
                if (null == nearTL || nearTL.createPoint.DistanceTo(plPipe.createPoint) > _findPipeDistance)
                    continue;
                var connect = GetConnectLine(plPipe, true, nearTL, true);
                if (null != connect)
                    createBasicElements.Add(connect);
            }
            return createBasicElements;
        }
        public CreateBasicElement GetConnectLine(CreateBlockInfo start,bool startOutCircle,CreateBlockInfo end,bool endOutCircle) 
        {
            var lineDir = (end.createPoint - start.createPoint).GetNormal();
            var lineSp = start.createPoint + lineDir.MultiplyBy(startOutCircle ? DrainSysAGCommon.GetBlockCircleRadius(start, "可见性1"):0);
            var lineEp = end.createPoint - lineDir.MultiplyBy(endOutCircle ? DrainSysAGCommon.GetBlockCircleRadius(end, "可见性1"):0);
            if (lineSp.DistanceTo(lineEp) < 5)
                return null;
            var ret = new CreateBasicElement(start.floorId, new Line(lineSp, lineEp), _layerName, "","");
            return ret;
        }
    }
}
