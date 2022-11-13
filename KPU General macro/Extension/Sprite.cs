using KPUGeneralMacro.Model;
using OpenCvSharp;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KPUGeneralmacro.Extension
{
    public static class SpriteExt
    {
        public static void Write(this BinaryWriter writer, Sprite sprite)
        {
            var bytes = sprite.Mat.ToBytes();

            writer.Write(sprite.Name);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            writer.Write(sprite.Threshold);
            writer.Write(sprite.ExtensionColor);
        }

        public static void Write(this BinaryWriter writer, ExtensionColor extColor)
        {
            writer.Write(extColor.Activated);
            writer.Write(extColor.DetectColor);
            writer.Write(extColor.Pivot.ToArgb());
            writer.Write(extColor.Factor);
        }

        public static void Write(this BinaryWriter writer, Dictionary<string, Sprite> sprites)
        {
            writer.Write(sprites.Count);
            foreach (var sprite in sprites.Values)
            {
                writer.Write(sprite);
            }
        }

        public static Sprite ReadSprite(this BinaryReader reader)
        {
            return new Sprite
            {
                Name = reader.ReadString(),
                Mat = Cv2.ImDecode(reader.ReadBytes(reader.ReadInt32()), ImreadModes.AnyColor),
                Threshold = reader.ReadSingle(),
                ExtensionColor = reader.ReadExtColor()
            };
        }

        public static ExtensionColor ReadExtColor(this BinaryReader reader)
        {
            return new ExtensionColor
            {
                Activated = reader.ReadBoolean(),
                DetectColor = reader.ReadBoolean(),
                Pivot = Color.FromArgb(reader.ReadInt32()),
                Factor = reader.ReadSingle()
            };
        }

        public static Dictionary<string, Sprite> ReadSprites(this BinaryReader reader)
        {
            var count = reader.ReadInt32();
            return Enumerable.Range(0, count)
                .Select(x => reader.ReadSprite())
                .ToDictionary(x => x.Name, x => x);
        }
    }
}
