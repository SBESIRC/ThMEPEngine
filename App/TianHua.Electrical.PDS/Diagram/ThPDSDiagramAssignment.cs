using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSDiagramAssignment
    {
        public void TableTitleAssign(AcadDatabase activeDb, BlockReference title, ThPDSProjectGraphNode node)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, title);
            var text = objs.OfType<DBText>().ToList();

            // 配电箱编号
            var loadId = text.Where(t => t.TextString == ThPDSCommon.DISTRIBUTION_BOX_ID).First();
            loadId.TextString = node.Load.ID.LoadID;

            // 设备用途
            var application = text.Where(t => t.TextString == ThPDSCommon.APPLICATION).First();
            application.TextString = node.Load.ID.Description;

            // 消防负荷
            var fireLoad = text.Where(t => t.TextString == ThPDSCommon.FIRE_LOAD).First();
            fireLoad.TextString = node.Load.FireLoad ? ThPDSCommon.FIRE_POWER_SUPPLY : ThPDSCommon.NON_FIRE_POWER_SUPPLY;

            // 参考尺寸
            var overallDimensions = text.Where(t => t.TextString == ThPDSCommon.OVERALL_DIMENSIONS).First();
            overallDimensions.TextString = "";

            // 安装位置
            var location = text.Where(t => t.TextString == ThPDSCommon.LOCATION).First();
            location.TextString = "";

            // 安装方式
            var installMethod = text.Where(t => t.TextString == ThPDSCommon.INSTALLMETHOD).First();
            installMethod.TextString = "";
        }

        public void EnterCircuitAssign(AcadDatabase activeDb, BlockReference circuit, ThPDSProjectGraphNode node)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuit);
            var text = objs.OfType<DBText>().ToList();

            // 进线回路编号
            var circuitId = text.Where(t => t.TextString.Contains(ThPDSCommon.ENTER_CIRCUIT_ID));
            //circuitId.TextString = node.Load.ID.LoadID;
        }
    }
}
