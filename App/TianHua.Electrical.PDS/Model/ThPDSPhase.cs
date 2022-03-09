using System.ComponentModel;

namespace TianHua.Electrical.PDS.Model
{
    public enum ThPDSPhase
    {
        [Description("一相")]
        一相,
        [Description("三相")]
        三相,
        [Description("None")]
        None,
    }
}
