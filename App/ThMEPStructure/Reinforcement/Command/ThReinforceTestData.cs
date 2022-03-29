using System.Collections.Generic;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Command
{
    internal class ThReinforceTestData
    {
        public static ThRectangleEdgeComponent RectangleEdgeComponent
        {
            get
            {
                return new ThRectangleEdgeComponent()
                {
                    Bw = 350,
                    Hc = 400,
                    C = 20,
                    Link2 = "1C8@80",
                    Link3 = "1C8@80",
                    Stirrup = "C8@100",
                    Reinforce = "6C16+2C12",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = false,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    Number = "GBZ1",
                };
            }
        }
        public static ThRectangleEdgeComponent RectangleCalEdgeComponent
        {
            get
            {
                return  new ThRectangleEdgeComponent()
                {
                    Bw = 350,
                    Hc = 400,
                    C = 20,
                    Link2 = "1C8@100",
                    Link3 = "1C8@100",
                    Stirrup = "C8@100",
                    Reinforce = "6C16+2C12",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = true,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    X = 1,
                    EnhancedReinforce = "2C18+4C16+2C12",
                    Number = "GBZ2",
                };
            }
        }
        public static ThLTypeEdgeComponent LTypeEdgeComponent
        {
            get
            {
                return new ThLTypeEdgeComponent()
                {
                    Bw = 350,
                    Hc1 = 350,
                    Bf = 350,
                    Hc2 = 450,
                    C = 20,
                    Link2 = "1C8@100",
                    Link3 = "1C8@100",
                    Link4 = "1C8@100",
                    Stirrup = "C10@120",
                    Reinforce = "10C18+6C16",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = false,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    Number = "GBZ3",
                };
            }
        }
        public static ThLTypeEdgeComponent LTypeCalEdgeComponent
        {
            get
            {
                return new ThLTypeEdgeComponent()
                {
                    Bw = 350,
                    Hc1 = 350,
                    Bf = 350,
                    Hc2 = 450,
                    C = 20,
                    Link2 = "1C8@120",
                    Link3 = "1C8@120",
                    Link4 = "1C8@120",
                    Stirrup = "C10@120",
                    Reinforce = "10C18+6C16",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = true,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    EnhancedReinforce = "2C20+8C24+6C16",
                    Number = "GBZ4",
                };
            }
        }
        public static ThTTypeEdgeComponent TTypeEdgeComponent
        {
            get
            {
                return new ThTTypeEdgeComponent()
                {
                    Bw = 350,
                    Hc1 = 350,
                    Bf = 350,
                    Hc2l = 450,
                    Hc2s = 450,
                    C = 20,
                    Link2 = "1C8@80",
                    Link3 = "2C8@80",
                    Link4 = "1C8@80",
                    Stirrup = "C8@85",
                    Reinforce = "8C22+12C20",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = false,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    Number = "GBZ5",
                };
            }
        }
        public static ThTTypeEdgeComponent TTypeCalEdgeComponent
        {
            get
            {
                return new ThTTypeEdgeComponent()
                {
                    Bw = 350,
                    Hc1 = 350,
                    Bf = 350,
                    Hc2l = 450,
                    Hc2s = 450,
                    C = 20,
                    Link2 = "1C8@85",
                    Link3 = "2C8@85",
                    Link4 = "1C8@85",
                    Stirrup = "C8@85",
                    Reinforce = "8C22+12C20",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = true,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    EnhancedReinforce = "2C24+6C22+12C20",
                    Number = "GBZ6",
                };
            }
        }
        public static List<ThEdgeComponent> StandardTestDatas
        {
            get
            {
                return CreateStdTestDatas();
            }
        }
        private static List<ThEdgeComponent> CreateStdTestDatas()
        {
            var results = new List<ThEdgeComponent>();
            // 一字型 标准型
            var rec1 = new ThRectangleEdgeComponent()
            {
                Bw = 200,
                Hc = 400,
                C = 20,
                Link2 ="1C10@110",
                Link3 = "",
                Stirrup ="C10@110",
                Reinforce = "6C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ8",
            };
            var rec2 = new ThRectangleEdgeComponent()
            {
                Bw = 200,
                Hc = 400,
                C = 20,
                Link2 ="1C10@100",
                Link3 = "",
                Stirrup ="C10@110",
                Reinforce ="6C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ9",
            };
            var rec3 = new ThRectangleEdgeComponent()
            {
                Bw = 350,
                Hc = 400,
                C = 20,
                Link2 = "1C8@100",
                Link3 = "1C8@100",
                Stirrup ="C8@100",
                Reinforce ="6C16+2C12",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ1",
            };
            results.Add(rec1);
            results.Add(rec2);
            results.Add(rec3);
            // 一字型 标准Cal型
            var rec4 = new ThRectangleEdgeComponent()
            {
                Hc = 400,
                Bw = 200,                
                C = 20,
                Link2 = "1C10@110",
                Link3 = "",
                Stirrup ="C10@110",
                Reinforce ="6C14",
                EnhancedReinforce="2C16+4C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = true,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ8",
            };
            results.Add(rec4);

            // L型 标准型
            var ltype1 = new ThLTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2 = 400,
                C = 20,
                Link2 = "",
                Link3 ="1C8@105",
                Link4 = "",
                Stirrup ="C8@105",
                Reinforce ="6C16+6C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ19",
            };
            var ltype2 = new ThLTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2 = 400,
                C = 20,
                Link2 = "",
                Link3 ="1C8@105",
                Link4 = "",
                Stirrup ="C8@105",
                Reinforce ="6C16+6C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ20",
            };
            var ltype3 = new ThLTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2 = 400,
                C = 20,
                Link2 = "",
                Link3 ="1C8@105",
                Link4 = "",
                Stirrup ="C8@105",
                Reinforce ="6C16+6C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ19",
            };
            var ltype4 = new ThLTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2 = 450,
                C = 20,
                Link2 = "1C8@120",
                Link3 = "1C8@120",
                Link4 = "1C8@120",
                Stirrup = "C10@120",
                Reinforce ="16C18",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ3",
            };
            var ltype5 = new ThLTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2 = 450,
                C = 20,
                Link2 ="1C8@120",
                Link3 = "1C8@120",
                Link4 = "1C8@120",
                Stirrup ="C10@120",
                Reinforce ="16C18",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ3",
            };
            results.Add(ltype1);
            results.Add(ltype2);
            results.Add(ltype3);
            results.Add(ltype4);
            results.Add(ltype5);

            // L型 标准Cal型
            var ltype6 = new ThLTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2 = 400,
                C = 20,
                Link2 = "",
                Link3 ="1C8@105",
                Link4 = "",
                Stirrup ="C8@105",
                Reinforce ="6C16+6C14",
                EnhancedReinforce ="4C18+2C16+6C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = true,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ19",
            };
            var ltype7 = new ThLTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2 = 400,
                C = 20,
                Link2 = "",
                Link3 ="1C8@105",
                Link4 = "",
                Stirrup ="C8@105",
                Reinforce ="6C16+6C14",
                EnhancedReinforce ="2C18+4C16+6C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = true,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ19",
            };
            results.Add(ltype6);
            results.Add(ltype7);

            // T型 标准型
            var ttype1 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l= 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ12",
            };
            var ttype2 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2S",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ13",
            };
            var ttype3 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2L",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ14",
            };
            var ttype4 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "3",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ15",
            };
            results.Add(ttype1);
            results.Add(ttype2);
            results.Add(ttype3);
            results.Add(ttype4);
            var ttype5 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 ="2C8@85",
                Link4 ="1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ5",
            };
            var ttype6 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 ="2C8@85",
                Link4 ="1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2L",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ6",
            };
            var ttype7 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 ="2C8@85",
                Link4 ="1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "2S",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ7",
            };
            var ttype8 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 ="2C8@85",
                Link4 ="1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = false,
                LinkWallPos = "3",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ7",
            };
            results.Add(ttype5);
            results.Add(ttype6);
            results.Add(ttype7);
            results.Add(ttype8);

            var ttype9 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "2",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ12",
            };
            var ttype10 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1S",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ13",
            };
            var ttype11 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1L",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ14",
            };
            results.Add(ttype9);
            results.Add(ttype10);
            results.Add(ttype11);

            var ttype12 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 = "2C8@85",
                Link4 = "1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "2",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ5",
            };
            var ttype13 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 = "2C8@85",
                Link4 = "1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1S",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ6",
            };
            var ttype14 = new ThTTypeEdgeComponent()
            {
                Bw = 350,
                Hc1 = 350,
                Bf = 350,
                Hc2l = 450,
                Hc2s = 450,
                C = 20,
                Link2 ="1C8@85",
                Link3 = "2C8@85",
                Link4 = "1C8@85",
                Stirrup ="C8@85",
                Reinforce ="8C22+12C20",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = false,
                LinkWallPos = "1L",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "GBZ7",
            };
            results.Add(ttype12);
            results.Add(ttype13);
            results.Add(ttype14);

            // T型 标准Cal型
            var ttype15 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                EnhancedReinforce ="4C18+2C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "A",
                IsCalculation = true,
                LinkWallPos = "1",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ12",
            };
            var ttype16 = new ThTTypeEdgeComponent()
            {
                Bw = 200,
                Hc1 = 300,
                Bf = 200,
                Hc2s = 300,
                Hc2l = 500,
                C = 20,
                Link2 = "",
                Link3 ="2C8@110",
                Link4 = "",
                Stirrup ="C8@110",
                Reinforce ="6C16+10C14",
                EnhancedReinforce ="2C18+4C16+10C14",
                AntiSeismicGrade = "二级",
                Type = "B",
                IsCalculation = true,
                LinkWallPos = "2",
                ConcreteStrengthGrade = "C60",
                PointReinforceLineWeight = 50,
                StirrupLineWeight = 30,
                Number = "YBZ12",
            };
            results.Add(ttype15);
            results.Add(ttype16);
            return results;
        }
    }
}
