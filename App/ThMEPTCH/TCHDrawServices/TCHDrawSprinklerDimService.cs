using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ThCADExtension;
using ThMEPTCH.TCHDrawServices;
using ThMEPTCH.Data;
using ThMEPTCH.Model;
using ThMEPTCH.TCHTables;


namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawSprinklerDimService : TCHDrawServiceBase
    {
        protected override string CmdName => "TH2T20";
        List<ThTCHSprinklerDim> ThDims;

        public TCHDrawSprinklerDimService(string tchDBPath)
        {
            if (string.IsNullOrEmpty(tchDBPath) || !File.Exists(tchDBPath))
            {
                TCHDBPath = Path.GetTempPath() + "TG20.db";
            }
            else
            {
                TCHDBPath = tchDBPath;
            }

            ClearDataTables.Add("TwtPoint");
            ClearDataTables.Add("TwtDimensionDim");
            ClearDataTables.Add("TwtPublicList");
        }

        public void Init(List<ThTCHSprinklerDim> thDims)
        {
            ThDims = new List<ThTCHSprinklerDim>();
            ThDims.AddRange(thDims);
        }

        protected override void WriteModelToTCHDatabase()
        {
            if (null == ThDims || ThDims.Count < 1)
                return;

            ulong id = 2000001;

            foreach (var item in ThDims)
            {
                var twtBtPoint = ThSQLHelper.PointToTwtPointModel(id, item.FirstPoint);
                WriteModelToTCH(twtBtPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref id);

                ulong segStartId = id;
                for (int i = 0; i < item.SegmentValues.Count; i++)
                {
                    var nextID = (int)(id + 1);
                    if (i == item.SegmentValues.Count - 1)
                    {
                        nextID = -1;
                    }

                    var seg = item.SegmentValues[i];
                    var segModel = ThSQLHelper.ConvertToTCHTwtPublicList(id, -1, -1, seg.ToString(), nextID);
                    WriteModelToTCH(segModel, ThMEPTCHCommon.TCHTableName_TwtPublicList, ref id);
                }

                var twtDimensionDim = GetTCHTwtDimensionDim(id, twtBtPoint.ID, segStartId, item);
                WriteModelToTCH(twtDimensionDim, ThMEPTCHCommon.TCHTableName_TwtDimensionDim, ref id);
            }
        }

        TCHTwtDimensionDim GetTCHTwtDimensionDim(ulong Id, ulong firstPointID, ulong segmentStartID, ThTCHSprinklerDim item)
        {
            TCHTwtDimensionDim tchTwtPublicList = new TCHTwtDimensionDim
            {
                ID = Id,
                LocationID = firstPointID,
                System = item.System,
                Rotation = item.Rotation,
                Dist2DimLine = item.Dist2DimLine,
                DocScale = item.Scale,
                DimStyle = "TH-STYLE3",
                LayoutRotation = item.LayoutRotation,
                SegmentStartID = segmentStartID,
            };

            return tchTwtPublicList;
        }

    }
}
