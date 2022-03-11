using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectLType:DrawObjectBase
    {
        /// <summary>
        /// 计算纵筋位置
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
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
