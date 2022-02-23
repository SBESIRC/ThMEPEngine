using System;
using ThMEPStructure.GirderConnect.BuildBeam;

namespace TianHua.Structure.WPF.UI.BeamStructure.BuildBeam
{
    public class BuildBeamConfigModel
    {
        public BuildBeamConfigModel()
        {
            FormulaTop = new FormulaFule();
            FormulaMiddleA = new FormulaFule();
            FormulaMiddleB = new FormulaFule();
            FormulaMiddleSecondary = new FormulaFule();
            TableTop1 = new TableFule();
            TableTop2 = new TableFule();
            TableTop3 = new TableFule();
            TableTop4 = new TableFule();
            TableTop5 = new TableFule();
            TableTop6 = new TableFule();
            TableTop7 = new TableFule();

            TableMiddleA1 = new TableFule();
            TableMiddleA2 = new TableFule();
            TableMiddleA3 = new TableFule();
            TableMiddleA4 = new TableFule();
            TableMiddleA5 = new TableFule();
            TableMiddleA6 = new TableFule();

            TableMiddleB1 = new TableFule();
            TableMiddleB2 = new TableFule();
            TableMiddleB3 = new TableFule();
            TableMiddleB4 = new TableFule();
            TableMiddleB5 = new TableFule();
            TableMiddleB6 = new TableFule();
        }
        public int EstimateSelection { get; set; }
        public int FormulaEstimateSelection { get; set; }
        public int TableEstimateSelection { get; set; }
        public int BeamCheckSelection { get; set; }
        public FormulaFule FormulaTop { get; set; }
        public FormulaFule FormulaMiddleA { get; set; }
        public FormulaFule FormulaMiddleB { get; set; }
        public FormulaFule FormulaMiddleSecondary { get; set; }
        public TableFule TableTop1 { get; set; }
        public TableFule TableTop2 { get; set; }
        public TableFule TableTop3 { get; set; }
        public TableFule TableTop4 { get; set; }
        public TableFule TableTop5 { get; set; }
        public TableFule TableTop6 { get; set; }
        public TableFule TableTop7 { get; set; }

        public TableFule TableMiddleA1 { get; set; }
        public TableFule TableMiddleA2 { get; set; }
        public TableFule TableMiddleA3 { get; set; }
        public TableFule TableMiddleA4 { get; set; }
        public TableFule TableMiddleA5 { get; set; }
        public TableFule TableMiddleA6 { get; set; }

        public TableFule TableMiddleB1 { get; set; }
        public TableFule TableMiddleB2 { get; set; }
        public TableFule TableMiddleB3 { get; set; }
        public TableFule TableMiddleB4 { get; set; }
        public TableFule TableMiddleB5 { get; set; }
        public TableFule TableMiddleB6 { get; set; }
        public int BeamCheck { get; set; }
        public int RegionSelection { get; set; }
    }
}
