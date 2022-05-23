using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm.FrameComparer.Model
{
    public class ThFrameChangeItem
    {
        public ThFrameChangeItem(ThFrameChangedCommon.CompareFrameType frameType, string changeType, Polyline focusPoly)
        {
            FrameTypeView = ThFrameChangedCommon.FrameTypeName[frameType];
            FrameType = frameType;
            ChangeType = changeType;
            FocusPoly = focusPoly;
            FocusPolyHash = focusPoly.GetHashCode();
        }

        public string FrameTypeView { get; set; }
        public ThFrameChangedCommon.CompareFrameType FrameType { get; set; }
        public string ChangeType { get; set; }
        public Polyline FocusPoly { get; set; }
        public int FocusPolyHash { get; set; }

        public List<DBText> RemoveText { get; set; } = new List<DBText>();
        public List<DBText> AddText { get; set; } = new List<DBText>();
        public Polyline RemovePoly { get; set; }
        public Polyline AddPoly { get; set; }

    }
}
