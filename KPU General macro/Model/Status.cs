using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace KPU_General_macro.Model
{
    public class Status
    {
        public struct Component
        {
            public Sprite sprite;
            public bool requirement;

            public Component(Sprite sprite, bool requirement)
            {
                this.sprite = sprite;
                this.requirement = requirement;
            }
        }

        public string Name { get; set; }
        public string Script { get; set; }

        public List<Component> Components { get; private set; }

        public Status(string name, string script)
        {
            this.Name = name;
            this.Script = script;
            this.Components = new List<Component>();
        }

        public bool Contains(Sprite sprite)
        {
            foreach (var component in this.Components)
            {
                if (component.requirement && component.sprite.Equals(sprite.Name))
                    return true;
            }

            return false;
        }
    }

    public class StatusContainer : Dictionary<string, Status>, IDisposable
    {
        private SpriteContainer SpriteContainer { get; set; }

        public StatusContainer(SpriteContainer spriteContainer)
        {
            this.SpriteContainer = spriteContainer;
        }

        public bool Load(string filename)
        {
            try
            {
                using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    var statusSize = reader.ReadInt32();
                    for (var i = 0; i < statusSize; i++)
                    {
                        var name = reader.ReadString();
                        var script = reader.ReadString();

                        var status = new Status(name, script);
                        var bindedSpriteSize = reader.ReadInt32();
                        for (var i2 = 0; i2 < bindedSpriteSize; i2++)
                            status.Components.Add(new Status.Component(this.SpriteContainer[reader.ReadString()], reader.ReadBoolean()));

                        this.Add(name, status);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Save(string filename)
        {
            try
            {
                using (var writer = new BinaryWriter(File.Open(filename, FileMode.CreateNew)))
                {
                    writer.Write(this.Count);                               // write status count : 4bytes
                    foreach (var status in this.Values)
                    {
                        writer.Write(status.Name);                          // write status name : length + value
                        writer.Write(status.Script);                        // write status script : length + value
                        writer.Write(status.Components.Count);
                        foreach (var component in status.Components)
                        {
                            writer.Write(component.sprite.Name);
                            writer.Write(component.requirement);
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public PythonDictionary ToDict()
        {
            var pythonDict = new PythonDictionary();
            foreach (var pair in this)
                pythonDict.Add(pair.Key, pair.Value);

            return pythonDict;
        }

        public void Dispose()
        {
            base.Clear();
        }
    }
}
