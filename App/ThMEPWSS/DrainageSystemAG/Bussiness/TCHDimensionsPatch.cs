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
using static ThMEPWSS.DrainageSystemAG.Bussiness.TangentPipeConvertion;
using static ThMEPWSS.DrainageSystemAG.Bussiness.TangentSymbMultiLeaderConvertion;
using static ThMEPWSS.DrainageSystemAG.Bussiness.CoordinateTransformation;
using GeometryExtensions;
using ThMEPWSS.Uitl;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public static class TCHDimensionsPatch
    {

        public static void DrawTCHPipeDimensions(List<ThTCHVerticalPipe> verPipes)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
               foreach (var pipe in verPipes)
                {
                    var text = pipe.DimTypeText + pipe.FloorType + "-" + pipe.PipeNum;
                    MLeader mLeader = drawMLeader(text, "", pipe.PipeTopPoint.ToPoint2D().ToPoint3d(), pipe.TurnPoint.ToPoint2D().ToPoint3d());
                    mLeader.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    mLeader.AddToCurrentSpace();
                }
            }
        }
        public static void DrawTCHSymbMultiLeader(List<ThTCHSymbMultiLeader> symbMultiLeaders)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                foreach (var symbMultiLeader in symbMultiLeaders)
                {
                    MLeader mLeader = drawMLeader(symbMultiLeader.UpText, symbMultiLeader.DownText, symbMultiLeader.BasePoint.ToPoint2D().ToPoint3d(), symbMultiLeader.TextLineLocPoint.ToPoint2D().ToPoint3d());
                    mLeader.Layer = symbMultiLeader.Layer;
                    mLeader.TextStyleId= DbHelper.GetTextStyleId("TH-STYLE3");
                    mLeader.AddToCurrentSpace();
                }
            }
        }
        static MLeader drawMLeader(string uptext, string down, Point3d p1, Point3d p2)
        {
            var mLeader = new MLeader();
            int ln = mLeader.AddLeaderLine(p1);
            mLeader.AddFirstVertex(ln, p1);
            mLeader.SetFirstVertex(ln, p1);
            mLeader.SetLastVertex(ln, p2);
            mLeader.ColorIndex = 0;
            mLeader.MText = new MText() { Contents = uptext + MText.ParagraphBreak + down, TextHeight = 350, ColorIndex = 0, };
            mLeader.MText.Location = p2;
            mLeader.ArrowSize = 0;
            mLeader.DoglegLength = 0;
            mLeader.LandingGap = 0;
            mLeader.ExtendLeaderToText = false;
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            return mLeader;
        }
    }
}