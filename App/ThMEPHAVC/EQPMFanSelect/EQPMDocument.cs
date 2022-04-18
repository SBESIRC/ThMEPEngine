using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.EQPMFanSelect
{
    public class EQPMDocument
    {
        public void CheckAndUpdataCopyBlock() 
        {
            var allBlocks = GetDocumentAllFanBlocks(null);
            if (allBlocks.Count < 1)
                return;
            using (var acdb = AcadDatabase.Active())
            {
                // 获取原模型对象
                foreach (var item in allBlocks)
                {
                    var pModel = FanDataModelExtension.ReadBlockAllFanData(item, out FanDataModel cModel, out bool isCopy);
                    if (null == pModel || !isCopy)
                        continue;
                    pModel.ID = Guid.NewGuid().ToString();
                    if (null != cModel)
                    {
                        cModel.ID = Guid.NewGuid().ToString();
                        cModel.PID = pModel.ID;
                    }
                    int.TryParse(pModel.InstallFloor, out int number);
                    item.Id.SetModelIdentifier(pModel.XDataValueList(number, cModel, item.Id.Handle.ToString()), ThHvacCommon.RegAppName_FanSelectionEx);
                }
            }
        }
        public List<FanDataModel> DocumentAreaFanToFanModels(Polyline selectArea)
        {
            var allFanModels = new List<FanDataModel>();
            var allBlocks = GetDocumentAllFanBlocks(selectArea);
            if (allBlocks.Count < 1)
                return allFanModels;
            
            return DocumentFanToFanModels(allBlocks);
        }
        public List<FanDataModel> DocumentFanToFanModels(List<BlockReference> targetBlocks)
        {
            VentSNCalculator ventSN = new VentSNCalculator();
            var allFanModels = new List<FanDataModel>();
            if (targetBlocks.Count < 1)
                return allFanModels;
            using (var acdb = AcadDatabase.Active())
            {
                // 获取原模型对象
                foreach (var item in targetBlocks)
                {
                    var pModel = FanDataModelExtension.ReadBlockAllFanData(item, out FanDataModel cModel, out bool isCopy);
                    if (null == pModel)
                        continue;
                    bool haveChildFan = cModel != null;
                    allFanModels.Add(pModel);
                    if (haveChildFan)
                        allFanModels.Add(cModel);
                }
            }
            var mergerFanModels = new List<FanDataModel>();
            var allChilds = new List<FanDataModel>();
            while (allFanModels.Count > 0)
            {
                List<int> ventQu = new List<int>();
                var first = allFanModels.First();
                allFanModels.Remove(first);
                int.TryParse(first.VentNum, out int num);
                ventQu.Add(num);
                if (first.IsChildFan)
                {
                    if (!allChilds.Any(c => c.PID == first.PID))
                        allChilds.Add(first);
                    continue;
                }
                var thisIdFans = allFanModels.Where(c => !c.IsChildFan && c.ID == first.ID).ToList();
                foreach (var item in thisIdFans)
                {
                    int.TryParse(item.VentNum, out num);
                    ventQu.Add(num);
                    allFanModels.Remove(item);
                }
                ventQu = ventQu.Distinct().ToList();
                first.VentNum = ventSN.VentQuanToString(ventQu);
                first.ListVentQuan = ventQu;
                mergerFanModels.Add(first);
            }
            mergerFanModels.AddRange(allChilds);
            return mergerFanModels;
        }
        public List<BlockReference> GetDocumentAllFanBlocks(Polyline selectArea)
        {
            using (var acdb = AcadDatabase.Active())
            {
                // 获取原模型对象
                var models = acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(ThHvacCommon.RegAppName_FanSelectionEx))
                    .ToList();
                if (null == selectArea)
                    return models;
                var retBlocks = new List<BlockReference>();
                foreach (var item in models) 
                {
                    var pt = item.Position;
                    pt = new Autodesk.AutoCAD.Geometry.Point3d(pt.X, pt.Y, 0);
                    if (selectArea.Contains(pt))
                        retBlocks.Add(item);
                }
                return retBlocks;
            }
        }
        public List<BlockReference> GetDocumentFanBlocks(FanDataModel dataModel)
        {
            using (var acdb = AcadDatabase.Active())
            {
                // 获取原模型对象
                var identifier = dataModel.ID;
                var models = acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(identifier, ThHvacCommon.RegAppName_FanSelectionEx))
                    .ToList();
                return models;
            }
        }
    }
}
