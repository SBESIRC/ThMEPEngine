using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPTCH.Model;
using ThMEPTCH.TCHDrawServices;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG;
using ThMEPWSS.DrainageSystemAG.Bussiness;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.DrainageSystemAG.Services;
using ThMEPWSS.Engine;
using ThMEPWSS.Model;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public static class TangentSymbMultiLeaderConvertion
    {
        public static void ConvertToTCHSymbMultiLeader(ref List<CreateBasicElement> createBasicElems,ref List<CreateDBTextElement> createTextElems, ref List<ThTCHSymbMultiLeader> symbMultiLeaders)
        {
            var texts = createTextElems.Where(e => e.ConvertToTCHElement || e.dbText.TextString.Contains("雨水斗"));
            createTextElems = createTextElems.Except(texts).ToList();
            var texts_up = texts.Where(e => e.dbText.TextString.Contains("雨水斗"));
            texts = texts.Except(texts_up);
            GenerateTCHSymMultiLeader(ref createBasicElems, ref symbMultiLeaders, texts_up, texts, "W-RAIN-NOTE");
            texts = createTextElems.Where(e => e.layerName.Equals("W-BUSH-NOTE"));
            createTextElems = createTextElems.Except(texts).ToList();
            texts_up = texts.Where(e => e.dbText.TextString.Contains("DN"));
            texts = texts.Except(texts_up);
            GenerateTCHSymMultiLeader(ref createBasicElems, ref symbMultiLeaders, texts_up, texts, "W-BUSH-NOTE");
        }

        private static void GenerateTCHSymMultiLeader(ref List<CreateBasicElement> createBasicElems, ref List<ThTCHSymbMultiLeader> symbMultiLeaders,IEnumerable<CreateDBTextElement> texts_up, IEnumerable<CreateDBTextElement> texts,
            string layerName)
        {
            foreach (var text in texts_up)
            {
                var corresponding_text = "";
                corresponding_text = texts.OrderBy(e => e.textPoint.DistanceTo(text.textPoint)).Count() > 0 ? texts.OrderBy(e => e.textPoint.DistanceTo(text.textPoint)).First().dbText.TextString : corresponding_text;
                var line = new Line();
                createBasicElems = createBasicElems.OrderBy(e => e.baseCurce.GetClosestPointTo(text.textPoint, false).DistanceTo(text.textPoint)).ToList();
                var horizontalBasicElems = createBasicElems.Where(e => e.baseCurce is Line ln && IsHorizontalLine(ln));
                if (horizontalBasicElems.Count() > 0 && horizontalBasicElems.First().baseCurce is Line ln)
                    line = ln;
                if (line.Length > 0)
                {
                    var elements = createBasicElems.Where(e => e.baseCurce is Line ln && IsConnected(ln, line));
                    if (elements.Count() == 1)
                    {
                        var firstelement = elements.First();
                        createBasicElems.Remove(horizontalBasicElems.First());
                        createBasicElems.Remove(elements.First());
                        var basepoint = firstelement.baseCurce.StartPoint;
                        var locpoint = firstelement.baseCurce.EndPoint;
                        if (line.GetClosestPointTo(locpoint, false).DistanceTo(locpoint) > 10)
                        {
                            locpoint = firstelement.baseCurce.StartPoint;
                            basepoint = firstelement.baseCurce.EndPoint;
                        }
                        ThTCHSymbMultiLeader symbMultiLeader = new ThTCHSymbMultiLeader(basepoint, locpoint, line.Length / 100, text.dbText.TextString, corresponding_text, layerName);
                        symbMultiLeaders.Add(symbMultiLeader);
                    }
                }

            }
        }
        private static bool IsConnected(Line a, Line b)
        {
            double tol = 10;
            if (a.GetClosestPointTo(b.StartPoint, false).DistanceTo(b.StartPoint) < 1 && a.GetClosestPointTo(b.EndPoint, false).DistanceTo(b.EndPoint) < 1)
                return false;
            if (a.StartPoint.DistanceTo(b.StartPoint) <= tol) return true;
            else if (a.StartPoint.DistanceTo(b.EndPoint) <= tol) return true;
            else if (a.EndPoint.DistanceTo(b.StartPoint) <= tol) return true;
            else if (a.EndPoint.DistanceTo(b.EndPoint) <= tol) return true;
            return false;
        }
        private static bool IsHorizontalLine(Line line, double degreetol = 1)
        {
            double angle = CreateVector(line).GetAngleTo(Vector3d.YAxis);
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }
        private static Vector3d CreateVector(Line line)
        {
            return CreateVector(line.StartPoint, line.EndPoint);
        }
        private static Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }
    }
}
