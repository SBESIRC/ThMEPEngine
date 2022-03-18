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
                    Link2 = "1C8@100",
                    Link3 = "1C8@100",
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
                    Link2 = "1C8@120",
                    Link3 = "1C8@120",
                    Link4 = "1C8@120",
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
                    Link2 = "1C8@115",
                    Link3 = "2C8@115",
                    Link4 = "1C8@115",
                    Stirrup = "C10@120",
                    Reinforce = "10C18+6C16",
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
                    Link2 = "1C8@115",
                    Link3 = "2C8@115",
                    Link4 = "1C8@115",
                    Stirrup = "C10@120",
                    Reinforce = "10C18+6C16",
                    AntiSeismicGrade = "二级",
                    Type = "A",
                    IsCalculation = true,
                    LinkWallPos = "1",
                    ConcreteStrengthGrade = "C60",
                    PointReinforceLineWeight = 50,
                    StirrupLineWeight = 30,
                    EnhancedReinforce = "2C20+8C18+6C16",
                    Number = "GBZ6",
                };
            }
        }
    }
}
