using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Model;

namespace ThMEPWSS.Service
{
    public class CalHolesService
    {
        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        public Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 删除框线中的洞口
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        public List<Polyline> RemoveHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            List<Polyline> resFrames = new List<Polyline>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                resFrames.Add(firFrame);
            }

            return resFrames;
        }

        /// <summary>
        /// 清除洞口内的喷淋
        /// </summary>
        /// <param name="holes"></param>
        /// <param name="sprays"></param>
        public void ClearHoleSpray(List<Polyline> holes, List<SprayLayoutData> sprays)
        {
            foreach (var hole in holes)
            {
                var tempHole = hole.Buffer(1)[0] as Polyline;
                sprays.RemoveAll(x => tempHole.Contains(x.Position));
            }
        }
    }
}
