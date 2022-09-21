using OpenCvSharp;
using System.Drawing;

namespace KPUGeneralMacro.Model
{
    public class ExtensionColor
    {
        public bool Activated { get; set; }
        public Color Pivot { get; set; }
        public float Factor { get; set; }
    }

    public class Sprite
    {
        public string Name { get; set; }
        public Mat Mat { get; set; }
        public ExtensionColor ExtensionColor { get; set; }
    }
}
