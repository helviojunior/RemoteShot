using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CamControl
{

    public class RGB
    {
        Color rgb = Color.Black;

        public Byte R { get { return rgb.R; } }
        public Byte G { get { return rgb.G; } }
        public Byte B { get { return rgb.B; } }

        public RGB(Color color)
            : this(color.R, color.G, color.B) { }


        public RGB(Int32 R, Int32 G, Int32 B)
        {
            Int32 R1 = R;
            Int32 G1 = G;
            Int32 B1 = B;
            if (R1 < 0) R1 = 0;
            if (R1 > 255) R1 = 255;

            if (G1 < 0) G1 = 0;
            if (G1 > 255) G1 = 255;

            if (B1 < 0) B1 = 0;
            if (B1 > 255) B1 = 255;

            rgb = Color.FromArgb(R1, G1, B1);

        }

        public Boolean Equal(Color color)
        {
            return color.Equals(rgb);
        }


        public Boolean Equal(Color color, Int32 tolerance)
        {
            RGB c1 = new RGB(color.R - tolerance, color.G - tolerance, color.B - tolerance);
            RGB c2 = new RGB(color.R + tolerance, color.G + tolerance, color.B + tolerance);

            if (((this.R >= c1.R) && (this.R <= c2.R)) && ((this.G >= c1.G) && (this.G <= c2.G)) && ((this.B >= c1.B) && (this.B <= c2.B)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
