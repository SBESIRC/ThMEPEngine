using System.Drawing;

namespace TianHua.FanSelection
{
    public partial class FanDataModel
    {
        public Bitmap ImgRemark
        {
            get
            {
                return this.GetImgRemark();
            }

        }

        private Bitmap GetImgRemark()
        {
            if (PID == "0")
            {
                return Properties.Resources.备注32;
            }
            else
            {
                return Properties.Resources.无;
            }
        }

        public Bitmap InsertMap
        {
            get
            {
                return this.GetInsertMap();
            }

        }

        private Bitmap GetInsertMap()
        {
            if (PID == "0" && FanModelName != string.Empty)
            {
                return Properties.Resources.插入32;
            }
            else
            {
                return Properties.Resources.无;
            }


        }

        public Bitmap AddAuxiliary
        {
            get
            {
                return this.GetAddAuxiliary();
            }

        }

        private Bitmap GetAddAuxiliary()
        {
            if (PID == "0")
            {
                return Properties.Resources.向下加一行;
            }
            else
            {
                return Properties.Resources.皇帝的新图16x16;
            }
        }
    }
}
