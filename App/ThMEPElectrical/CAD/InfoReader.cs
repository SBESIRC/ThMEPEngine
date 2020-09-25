using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Business;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.CAD
{
    /// <summary>
    /// Io 数据读取提取器
    /// </summary>
    public class InfoReader
    {
        public List<Polyline> RecognizeMainBeamColumnWalls
        {
            get;
            set;
        } = new List<Polyline>(); // 主梁,柱子，剪力墙

        public List<SecondBeamProfileInfo> RecognizeSecondBeams
        {
            get;
            set;
        } = new List<SecondBeamProfileInfo>(); // 次梁

        public InfoReader()
        {

        }

        public void Do()
        {
            var preWindow = SelectPreWindow();
            ComponentPicker(preWindow);

        }
        /// <summary>
        /// 用户选择预选框
        /// </summary>
        /// <returns></returns>
        private Point3dCollection SelectPreWindow()
        {
            var preWindPoints = PreWindowSelector.GetSelectRectPoints();
            return preWindPoints;
        }

        /// <summary>
        /// Io modol 前置数据提取
        /// </summary>
        /// <param name="ptCollection"></param>
        private void ComponentPicker(Point3dCollection ptCollection)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                // 接口，提取主梁，次梁，柱子，剪力墙等数据
                var beamConnectEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(acadDatabase.Database, ptCollection);
                // 主梁
                beamConnectEngine.PrimaryBeamLinks.ForEach(mainBeamInfo =>
                {
                    mainBeamInfo.Beams.ForEach(beamInfo =>
                    {
                        if (beamInfo.Outline is Polyline mainBeamPoly)
                            RecognizeMainBeamColumnWalls.Add(mainBeamPoly);
                    });
                });
                // 柱子
                beamConnectEngine.ColumnEngine.Elements.ForEach(columnElement =>
                {
                    if (columnElement.Outline is Polyline columnPoly)
                        RecognizeMainBeamColumnWalls.Add(columnPoly);
                });
                // 剪力墙
                beamConnectEngine.ShearWallEngine.Elements.ForEach(shearWallElement =>
                {
                    if (shearWallElement.Outline is Polyline shearWallPoly)
                        RecognizeMainBeamColumnWalls.Add(shearWallPoly);
                });
                // 次梁
                beamConnectEngine.SecondaryBeamLinks.ForEach(secondBeamInfo =>
                {
                    secondBeamInfo.Beams.ForEach(singleBeamInfo =>
                    {
                        if (singleBeamInfo.Outline is Polyline secondBeamPoly)
                            RecognizeSecondBeams.Add(new SecondBeamProfileInfo(secondBeamPoly, singleBeamInfo.Height));
                    });
                });
            }
        }
    }
}
