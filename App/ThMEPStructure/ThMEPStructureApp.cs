using Autodesk.AutoCAD.Runtime;
using ThMEPStructure.GirderConnect.Command;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Command;

namespace ThMEPStructure
{
    public class ThMEPStructureApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        /// <summary>
        /// 生成主梁
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSCZL", CommandFlags.Modal)]
        public void THZLSC()
        {
            using (var cmd = new ThBeamConnectorCommand())
            {
                cmd.SubExecute();
            }
        }

        /// <summary>
        /// 生成次梁
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSCCL", CommandFlags.Modal)]
        public void THCLSC()
        {
            using (var cmd = new SecondaryBeamConnectCmd())
            {
                cmd.Execute();
            }
        }
    }
}
