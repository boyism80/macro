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
            foreach (var sprite in Model.Sprite.Load(Option.ResourceFilePath))
            {
                Sprites.Add(sprite.Name, sprite);
            }
        }

        public void SaveSpriteList()
        {
            Model.Sprite.Save(Option.ResourceFilePath, Sprites.Values);
        }
    }
}
