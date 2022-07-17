using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
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
using ThMEPWSS.Command;
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
namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public static class CoordinateTransformation
    {
        public static void ConvertCoordinateToUCS(ref List<FloorFramed> floorFrameds,
            ref List<FloorFramed> roofFloors,
            ref FloorFramed livingHighestFloor,
            ref List<Polyline> _allWalls,
            ref List<Polyline> _allColumns,
            ref List<Polyline> _allRailings,
            ref List<Polyline> _allBeams,
            ref List<EquipmentBlcokModel> _floorBlockEqums,
            Matrix3d matrix
            )
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                for (int i = 0; i < floorFrameds.Count; i++)
                {
                    floorFrameds[i] = ConvertFloorFramedToUSC(floorFrameds[i], matrix);
                }
                for (int i = 0; i < roofFloors.Count; i++)
                {
                    roofFloors[i] = ConvertFloorFramedToUSC(roofFloors[i], matrix);
                }
                livingHighestFloor = ConvertFloorFramedToUSC(livingHighestFloor, matrix);
                for (int i = 0; i < _allWalls.Count; i++)
                {
                    _allWalls[i].TransformBy(matrix);
                }
                for (int i = 0; i < _allColumns.Count; i++)
                {
                    _allColumns[i].TransformBy(matrix);
                }
                for (int i = 0; i < _allRailings.Count; i++)
                {
                    _allRailings[i].TransformBy(matrix);
                }
                for (int i = 0; i < _allBeams.Count; i++)
                {
                    _allBeams[i].TransformBy(matrix);
                }
                for (int i = 0; i < _floorBlockEqums.Count; i++)
                {
                    _floorBlockEqums[i] = ConvertEquipmentBlcokModelToUSC(_floorBlockEqums[i], matrix);
                }
            }
               


        }
        private static FloorFramed ConvertFloorFramedToUSC(FloorFramed floorFramed,Matrix3d matrix)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                floorFramed.outPolyline.TransformBy(matrix);
                floorFramed.blockOutPointCollection = floorFramed.blockOutPointCollection.Cast<Point3d>().Select(e =>
                {
                    var p = e.TransformBy(matrix);
                    return p;
                }).ToCollection();
                var br = floorFramed.floorBlock.Clone() as BlockReference;
                br.TransformBy(matrix);
                floorFramed.floorBlock = br;
                floorFramed.datumPoint = floorFramed.datumPoint.TransformBy(matrix);
                return floorFramed;
            }


        }
        private static EquipmentBlcokModel ConvertEquipmentBlcokModelToUSC(EquipmentBlcokModel equipmentBlcokModel,Matrix3d matrix)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                for (int i = 0; i < equipmentBlcokModel.blockReferences.Count; i++)
                {
                    var br = equipmentBlcokModel.blockReferences[i].Clone() as BlockReference;
                    br.TransformBy(matrix);
                    equipmentBlcokModel.blockReferences[i]=br;
                }
                return equipmentBlcokModel;
            }

        }
        public static void ConvertCoordinateToWCS(ref List<CreateBlockInfo> createBlocks, ref List<CreateBasicElement> createElems,
            ref List<CreateDBTextElement> createTexts,Matrix3d matrix)
        {
            for (int i = 0; i < createBlocks.Count; i++)
            {
                var data= createBlocks[i];
                data.createPoint = data.createPoint.TransformBy(matrix);
                createBlocks[i] = data;
            }
            for (int i = 0; i < createElems.Count; i++)
            {
                var data = createElems[i];
                var curve = data.baseCurce;
                curve.TransformBy(matrix);
                data.baseCurce = curve;
                createElems[i] = data;
            }
            for (int i = 0; i < createTexts.Count; i++)
            {
                var data = createTexts[i];
                data.textPoint= data.textPoint.TransformBy(matrix);
                var text = data.dbText;
                text.TransformBy(matrix);
                data.dbText = text;
                createTexts[i] = data;
            }
        }
        public static void ConvertTCHPipeToWCS(ref List<ThTCHVerticalPipe>pipes,Matrix3d matrix)
        {
            for (int i = 0; i < pipes.Count; i++)
            {
                var pipe = pipes[i];
                pipe.PipeBottomPoint = pipe.PipeBottomPoint.TransformBy(matrix);
                pipe.TurnPoint = pipe.TurnPoint.TransformBy(matrix);
                pipe.PipeTopPoint = pipe.PipeTopPoint.TransformBy(matrix);
                pipe.TextDirection = pipe.TextDirection.TransformBy(matrix);
                pipes[i] = pipe;
            }
        }
        public static void ConvertSymbMultiLeadersToWCS(ref List<ThTCHSymbMultiLeader> leaders, Matrix3d matrix)
        {
            for (int i = 0; i < leaders.Count; i++)
            {
                var leader = leaders[i];
                leader.BasePoint= leader.BasePoint.TransformBy(matrix);
                leader.TextLineLocPoint = leader.TextLineLocPoint.TransformBy(matrix);
                leaders[i] = leader;
            }
        }

    }
}
