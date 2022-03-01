using System;
using ThMEPEngineCore;

namespace ThMEPStructure.GirderConnect.Data
{
    public class ThBeamConnectorExpandColumnOverride : IDisposable
    {
        private bool IsExpanded { get; set; }

        public ThBeamConnectorExpandColumnOverride(bool expand)
        {
            IsExpanded = ThMEPEngineCoreService.Instance.ExpandColumn;
            ThMEPEngineCoreService.Instance.ExpandColumn = expand;
        }

        public void Dispose()
        {
            ThMEPEngineCoreService.Instance.ExpandColumn = IsExpanded;
        }
    }
}
