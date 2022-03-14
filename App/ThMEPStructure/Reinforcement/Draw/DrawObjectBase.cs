using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
namespace ThMEPStructure.Reinforcement.Draw
{
    abstract class DrawObjectBase
    {
        public void DrawGangJin()
        {
            foreach(var gangJin in GangJinBases)
            {
                gangJin.Draw();
            }
        }

        /// <summary>
        /// 绘制表格
        /// </summary>
        public void  DrawTable()
        {

        }

        /// <summary>
        /// 绘制轮廓
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawOutline(string drawingScale);

        public Point3d TableStartPt;
        public Polyline Outline;
        public List<GangJinBase> GangJinBases;
        

        //初始化钢筋列表
        public void Init(ThLTypeEdgeComponent thLTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThRectangleEdgeComponent thRectangleEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThTTypeEdgeComponent thTTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }
    }
}
