using Autodesk.AutoCAD.Geometry;


namespace ThMEPWSS.BlockNameConfig
{
    public class PicInfo
    {
        public double HeightImg;
        public Point2d Origin1;
        public int PaperIndex;
        public int ImgFileNum;

        public PicInfo()
        {
            HeightImg = 0;
            Origin1 = new Point2d();
            PaperIndex = 0;
            ImgFileNum = 0;
        }
    }
}
