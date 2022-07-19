using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPHVAC.SmokeProofSystem.Data;

namespace ThMEPHVAC.Command
{
    public class SmokeProofSystemCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var frameDic = CalStructrueService.GetFrame(acad);
                if (frameDic.Count <= 0)
                {
                    return;
                }
                var pt = frameDic.First().Key.StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                foreach (var dic in frameDic)
                {
                    var frame = dic.Key.Clone() as Polyline;
                    originTransformer.Transform(frame);
                    var thRooms = CalStructrueService.GetRoomInfo(acad, originTransformer);
                    var rooms = CalAllRoomPolylines(thRooms);
                    var blocks = CalStructrueService.GetSmkBlock(frame, acad, originTransformer);
                }
            }   
        }

        /// <summary>
        /// 获取房间所有的polyline
        /// </summary>
        /// <param name="thRooms"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<string>> CalAllRoomPolylines(List<ThIfcRoom> thRooms)
        {
            var polyDic = new Dictionary<Polyline, List<string>>();
            foreach (var room in thRooms)
            {
                if (room.Boundary is Polyline polyline)
                {
                    polyline.ProjectOntoXYPlane();
                    polyDic.Add(polyline, room.Tags);
                }
                else if (room.Boundary is MPolygon mPolygon)
                {
                    mPolygon.ProjectOntoXYPlane();
                    polyDic.Add(mPolygon.Shell(), room.Tags);
                }
            }

            return polyDic;
        }
    }
}
