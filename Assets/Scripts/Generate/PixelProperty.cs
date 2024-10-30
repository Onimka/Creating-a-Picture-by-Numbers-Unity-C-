using UnityEngine;

namespace PictureGenerator
{
    public class PixelProperty
    {
        public readonly int Index;
        public readonly Color OriginalColor;
        public readonly Color32 color32;

        public Color ChangedColor { get; set; }

        public PixelProperty(int index, Color color)
        {
            Index = index;
            OriginalColor = color;
        }

        public void ChangeColor(Color color)
        {
            //var gray = 0.299 * OriginalColor.ScR + 0.587 * OriginalColor.ScB + 0.114 * OriginalColor.ScB;

            //byte newRed = (byte)Math.Max(OriginalColor.R + gray, 0);
            //byte newGreen = (byte)Math.Max(OriginalColor.G + gray, 0);
            //byte newBlue = (byte)Math.Max(OriginalColor.B + gray, 0); 



            //ChangedColor = Color.FromArgb(OriginalColor.A, newRed, newGreen, newBlue);

            //var gray = (byte)(0.299 * OriginalColor.R + 0.587 * OriginalColor.G + 0.114 * OriginalColor.B);
            //ChangedColor = Color.FromArgb(OriginalColor.A, gray, gray, gray);
            ChangedColor = color;
        }
    }
}
