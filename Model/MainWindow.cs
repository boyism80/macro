using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace macro.Model
{
    public class MainWindow
    {
        public Model.Option Option { get; set; } = new Option();

        [JsonIgnore] public Dictionary<string, Model.Sprite> Sprites { get; set; } = new Dictionary<string, Sprite>();

        [JsonIgnore] public BitmapImage Bitmap { get; set; }

        [JsonIgnore] public Model.App App { get; private set; }

        public MainWindow()
        {

        }

        public static MainWindow Load(string path)
        {
            try
            {
                var stringify = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<MainWindow>(stringify, new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });
            }
            catch
            {
                return null;
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        public void LoadSpriteList()
        {
            var path = Option.ResourceFilePath;
            if (File.Exists(path) == false)
                throw new Exception($"{path} 파일을 찾을 수 없습니다.");

            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var size = reader.ReadInt32();
                var bytes = reader.ReadBytes(size);
                var threshold = reader.ReadSingle();
                var isActiveExt = reader.ReadBoolean();
                var detectColor = reader.ReadBoolean();
                var pivot = Color.FromArgb(reader.ReadInt32());
                var factor = reader.ReadSingle();
                var sprite = new Model.Sprite
                {
                    Name = name,
                    Source = Cv2.ImDecode(bytes, ImreadModes.AnyColor),
                    Threshold = threshold,
                    Extension = new Model.SpriteExtension
                    {
                        Activated = isActiveExt,
                        DetectColor = detectColor,
                        Pivot = pivot,
                        Factor = factor
                    }
                };

                Sprites.Add(name, sprite);
            }
        }

        public void SaveSpriteList()
        {
            var path = Option.ResourceFilePath;
            using var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate));
            writer.Write(Sprites.Count);
            foreach (var sprite in Sprites.Values)
            {
                var bytes = sprite.Source.ToBytes();

                writer.Write(sprite.Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
                writer.Write(sprite.Threshold);
                writer.Write(sprite.Extension.Activated);
                writer.Write(sprite.Extension.DetectColor);
                writer.Write(sprite.Extension.Pivot.ToArgb());
                writer.Write(sprite.Extension.Factor);
            }
        }
    }
}
