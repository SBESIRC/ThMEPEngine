using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Command;

namespace ThMEPWSS
{
  public class ThSystemDiagramCmds
  {
    [CommandMethod("TIANHUACAD", "THCRSD", CommandFlags.Modal)]
    public void ThCreateRainSystemDiagram()
    {
      using (var cmd = new ThRainSystemDiagramCmd())
      {
        cmd.Execute();
      }
    }
  }
}
