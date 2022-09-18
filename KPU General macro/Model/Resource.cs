using System;
using System.Drawing;
using System.IO;

namespace KPUGeneralMacro.Model
{
    public class Resource
    {
        public SpriteContainer Sprites { get; private set; }
        public StatusContainer Statuses { get; private set; }

        public bool Load(string filename)
        {
            try
            {
                using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    this.Sprites = new SpriteContainer();
                    var spriteSize = reader.ReadInt32();
                    for (var i = 0; i < spriteSize; i++)
                    {
                        var name = reader.ReadString();
                        var threshold = reader.ReadSingle();
                        var templateSize = reader.ReadInt32();
                        var template = reader.ReadBytes(templateSize);
                        var usedColor = reader.ReadBoolean();
                        if (usedColor)
                            this.Sprites.Add(name, new LegacySprite(name, template, threshold, Color.FromArgb(reader.ReadInt32()), reader.ReadSingle()));
                        else
                            this.Sprites.Add(name, new LegacySprite(name, template, threshold));
                    }

                    this.Statuses = new StatusContainer(Sprites);
                    var statusSize = reader.ReadInt32();
                    for (var i = 0; i < statusSize; i++)
                    {
                        var name = reader.ReadString();
                        var script = reader.ReadString();

                        var status = new Status(name, script);
                        var bindedSpriteSize = reader.ReadInt32();
                        for (var i2 = 0; i2 < bindedSpriteSize; i2++)
                            status.Components.Add(new Status.Component(this.Sprites[reader.ReadString()], reader.ReadBoolean()));

                        this.Statuses.Add(name, status);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public void Save(string filename)
        {
            try
            {
                using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
                {
                    writer.Write(this.Sprites.Count);                       // write script count : 4 bytes
                    foreach (var sprite in this.Sprites.Values)
                    {
                        writer.Write(sprite.Name);                          // write sprite name : length + value
                        writer.Write(sprite.Threshold);                     // write threshold : 4bytes
                        writer.Write(sprite.Bytes.Length);                  // write bytes length : 4bytes
                        writer.Write(sprite.Bytes);                         // write bytes : bytes length
                        writer.Write(sprite.Color != null);
                        if (sprite.Color != null)
                        {
                            writer.Write(sprite.Color.Value.ToArgb());
                            writer.Write(sprite.ErrorFactor);
                        }
                    }

                    writer.Write(this.Statuses.Count);                      // write status count : 4bytes
                    foreach (var status in this.Statuses.Values)
                    {
                        writer.Write(status.Name);                          // write status name : length + value
                        writer.Write(status.Script);                        // write status script : length + value
                        writer.Write(status.Components.Count);
                        foreach (var component in status.Components)
                        {
                            writer.Write(component.Sprite.Name);
                            writer.Write(component.Requirement);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Clear()
        {
            if (this.Statuses != null)
                this.Statuses.Dispose();
            this.Statuses = null;

            if (this.Sprites != null)
                this.Sprites.Dispose();
            this.Sprites = null;
        }
    }
}
