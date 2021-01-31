using IronPython.Runtime;
using System;
using System.Collections.Generic;

namespace KPU_General_macro.Model
{
    public class Status
    {
        public class Component
        {
            public Sprite Sprite { get; private set; }
            public bool Requirement { get; set; }

            public Component(Sprite sprite, bool requirement)
            {
                this.Sprite = sprite;
                this.Requirement = requirement;
            }
        }

        public string Name { get; set; }

        public List<Component> Components { get; private set; }

        public Status(string name)
        {
            this.Name = name;
            this.Components = new List<Component>();
        }

        public bool Contains(Sprite sprite)
        {
            foreach (var component in this.Components)
            {
                if (component.Requirement && component.Sprite.Equals(sprite.Name))
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
