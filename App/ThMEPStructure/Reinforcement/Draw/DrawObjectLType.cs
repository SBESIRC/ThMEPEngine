using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectLType:DrawObjectBase
    {
        /// <summary>
        /// 计算纵筋位置
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        ThLTypeEdgeComponent thLTypeEdgeComponent;
        public override void DrawOutline(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            var pts = new Point3dCollection
            {
                TableStartPt + new Vector3d(450, -1000, 0) * scale,
                TableStartPt + new Vector3d(450, -1000 - thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -1000 - thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -1000 - thLTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf, -1000 - thLTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf, -1000, 0) * scale
            };
            Outline = pts.CreatePolyline();
        }

        void CalReinforcePosition(int pointNum,List<Point3d> points)
        {

        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }


        void CalLinkPosition()
        {

        }

        void CalStirrupPosition()
        {

        }

        public void CalGangjinPosition()
        {
            foreach(var gangJin in GangJinBases)
            {
                //如果是纵筋
                if(gangJin.GangjinType==0)
                {
                    //更新gangjin的值
                    //CalReinforcePosition();
                    

                }
                else if()
                {

                }
            }
        }
    }
}
