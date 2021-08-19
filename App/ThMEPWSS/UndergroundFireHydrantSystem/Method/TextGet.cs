﻿using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class TextGet
    {
        public static void GetText(Point3dEx tpt, FireHydrantSystemIn fireHydrantSysIn, ref FireHydrantSystemOut fireHydrantSysOut, Point3d pt4, Point3d pt6)
        {
            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber2;//立管标号
            }

            if (pipeNumber1[0].Equals('X') || pipeNumber1[0].Equals('B') || pipeNumber1.StartsWith("DX"))
            {
                var textPt1 = new Point3d(pt4.X - textWidth, pt4.Y - 1700, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - 1700, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text = ThTextSet.ThText(textPt1, pipeNumber1.Trim());
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber12.Equals(""))
                {
                    text = ThTextSet.ThText(new Point3d(textPt1.X, textPt1.Y - 400, 0), pipeNumber12.Trim());
                    fireHydrantSysOut.TextList.Add(text);
                }
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);
            var strDN = "DN65";
            if(fireHydrantSysIn.TermDnDic.ContainsKey(tpt))
            {
                strDN = fireHydrantSysIn.TermDnDic[tpt];
            }
            var DN = ThTextSet.ThText(new Point3d(pt4.X + 400, pt4.Y - 2800, 0), Math.PI / 2, strDN);
            fireHydrantSysOut.DNList.Add(DN);
        }
    }
}
