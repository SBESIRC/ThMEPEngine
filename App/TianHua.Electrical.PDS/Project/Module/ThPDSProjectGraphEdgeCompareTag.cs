﻿using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public abstract class ThPDSProjectGraphEdgeCompareTag : ThPDSProjectGraphEdgeTag
    {
        //
    }

    [Serializable]
    public class ThPDSProjectGraphEdgeIdChangeTag : ThPDSProjectGraphEdgeCompareTag
    {
        public bool ChangeFrom { get; set; }
        public string ChangedLastCircuitID { get; set; }

        public ThPDSProjectGraphEdgeIdChangeTag()
        {
            ChangeFrom = false;
            ChangedLastCircuitID = null;
        }
    }

    public class ThPDSProjectGraphEdgeMoveTag : ThPDSProjectGraphEdgeCompareTag
    {
        public bool MoveFrom { get; set; }

        public ThPDSProjectGraphEdgeMoveTag()
        {
            MoveFrom = false;
        }
    }

    public class ThPDSProjectGraphEdgeAddTag : ThPDSProjectGraphEdgeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphEdgeDeleteTag : ThPDSProjectGraphEdgeCompareTag
    {
        //
    }
}
