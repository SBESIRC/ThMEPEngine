using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.BuildBeam
{
    public class BuildBeamLayoutConfig
    {
        public static int EstimateSelection = 1;
        public static int FormulaEstimateSelection = 1;
        public static int TableEstimateSelection = 1;
        public static int BeamCheckSelection = 1;
        public static FormulaFule FormulaTop = new FormulaFule() { LDividesH = 10, HDividesB = 2, Hmin = 500, Bmin = 300 };
        public static FormulaFule FormulaMiddleA = new FormulaFule() { LDividesH = 10, HDividesB = 2, Hmin = 500, Bmin = 250 };
        public static FormulaFule FormulaMiddleB = new FormulaFule() { LDividesH = 15, HDividesB = 3, Hmin = 500, Bmin = 200 };
        public static FormulaFule FormulaMiddleSecondary = new FormulaFule() { LDividesH = 15, HDividesB = 3, Hmin = 500, Bmin = 200 };

        public static TableFule TableTop1 = new TableFule() { H = 500, B = 300 };
        public static TableFule TableTop2 = new TableFule() { H = 600, B = 300 };
        public static TableFule TableTop3 = new TableFule() { H = 700, B = 300 };
        public static TableFule TableTop4 = new TableFule() { H = 700, B = 350 };
        public static TableFule TableTop5 = new TableFule() { H = 800, B = 400 };
        public static TableFule TableTop6 = new TableFule() { H = 900, B = 450 };
        public static TableFule TableTop7 = new TableFule() { H = 1000, B = 500 };

        public static TableFule TableMiddleA1 = new TableFule() { H = 500, B = 250 };
        public static TableFule TableMiddleA2 = new TableFule() { H = 550, B = 250 };
        public static TableFule TableMiddleA3 = new TableFule() { H = 600, B = 250 };
        public static TableFule TableMiddleA4 = new TableFule() { H = 700, B = 300 };
        public static TableFule TableMiddleA5 = new TableFule() { H = 800, B = 400 };
        public static TableFule TableMiddleA6 = new TableFule() { H = 900, B = 450 };

        public static TableFule TableMiddleB1 = new TableFule() { H = 500, B = 200 };
        public static TableFule TableMiddleB2 = new TableFule() { H = 500, B = 200 };
        public static TableFule TableMiddleB3 = new TableFule() { H = 550, B = 200 };
        public static TableFule TableMiddleB4 = new TableFule() { H = 550, B = 250 };
        public static TableFule TableMiddleB5 = new TableFule() { H = 600, B = 300 };
        public static TableFule TableMiddleB6 = new TableFule() { H = 700, B = 300 };
        public static int BeamCheck = 50;
        public static int RegionSelection = 1;
    }

    public class BuildBeamLayoutConfigFromFile
    {
        public static int EstimateSelection = 1;
        public static int FormulaEstimateSelection = 1;
        public static int TableEstimateSelection = 1;
        public static int BeamCheckSelection = 1;
        public static FormulaFule FormulaTop = new FormulaFule() { LDividesH = 10, HDividesB = 2, Hmin = 500, Bmin = 300 };
        public static FormulaFule FormulaMiddleA = new FormulaFule() { LDividesH = 10, HDividesB = 2, Hmin = 500, Bmin = 250 };
        public static FormulaFule FormulaMiddleB = new FormulaFule() { LDividesH = 15, HDividesB = 3, Hmin = 500, Bmin = 200 };
        public static FormulaFule FormulaMiddleSecondary = new FormulaFule() { LDividesH = 15, HDividesB = 3, Hmin = 500, Bmin = 200 };

        public static TableFule TableTop1 = new TableFule() { H = 500, B = 300 };
        public static TableFule TableTop2 = new TableFule() { H = 600, B = 300 };
        public static TableFule TableTop3 = new TableFule() { H = 700, B = 300 };
        public static TableFule TableTop4 = new TableFule() { H = 700, B = 350 };
        public static TableFule TableTop5 = new TableFule() { H = 800, B = 400 };
        public static TableFule TableTop6 = new TableFule() { H = 900, B = 450 };
        public static TableFule TableTop7 = new TableFule() { H = 1000, B = 500 };

        public static TableFule TableMiddleA1 = new TableFule() { H = 500, B = 250 };
        public static TableFule TableMiddleA2 = new TableFule() { H = 550, B = 250 };
        public static TableFule TableMiddleA3 = new TableFule() { H = 600, B = 250 };
        public static TableFule TableMiddleA4 = new TableFule() { H = 700, B = 300 };
        public static TableFule TableMiddleA5 = new TableFule() { H = 800, B = 400 };
        public static TableFule TableMiddleA6 = new TableFule() { H = 900, B = 450 };

        public static TableFule TableMiddleB1 = new TableFule() { H = 500, B = 200 };
        public static TableFule TableMiddleB2 = new TableFule() { H = 500, B = 200 };
        public static TableFule TableMiddleB3 = new TableFule() { H = 550, B = 200 };
        public static TableFule TableMiddleB4 = new TableFule() { H = 550, B = 250 };
        public static TableFule TableMiddleB5 = new TableFule() { H = 600, B = 300 };
        public static TableFule TableMiddleB6 = new TableFule() { H = 700, B = 300 };
        public static int BeamCheck = 50;
        public static int RegionSelection = 1;
    }

    public class FormulaFule
    {
        public int LDividesH { get; set; }
        public int HDividesB { get; set; }
        public int Hmin { get; set; }
        public int Bmin { get; set; }
    }

    public class TableFule
    {
        public int H { get; set; }
        public int B { get; set; }
    }
}
