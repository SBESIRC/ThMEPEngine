using Autodesk.AutoCAD.Runtime;
using ThPlatform3D.Command;

namespace ThPlatform3D
{
    public class ThPlatform3DDrawCmds
    {
        [CommandMethod("TIANHUACAD", "THBPI", CommandFlags.Modal)]
        public void THBPI()
        {
            using (var cmd = new ThInsertBasePointCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLGHZ", CommandFlags.Modal)]
        public void THLGHZ()
        {
            using (var cmd = new ThRailDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THQDHZ", CommandFlags.Modal)]
        public void THQDHZ()
        {
            using (var cmd = new ThWallHoleDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLBHZ", CommandFlags.Modal)]
        public void THLBHZ()
        {
            using (var cmd = new ThSlabDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THJBHZ", CommandFlags.Modal)]
        public void THJBHZ()
        {
            using (var cmd = new ThDropSlabDrawCmd())
            {
                cmd.Execute();
            }
        }
    }
}
