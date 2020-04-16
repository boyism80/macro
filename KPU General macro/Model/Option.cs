using SimpleJSON;
using System;
using System.IO;

namespace KPU_General_macro.Model
{
    public class Option
    {
        public string ClassName { get; set; } = "LOSTARK";

        public string ResourceFile { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "templates", "resource.kpu");
        public string PythonDirectory { get; set; } = Path.Combine("D:\\", "Program Files (x86)", "Python", "Python27");


        public string InitializeScriptName { get; set; } = "init.py";
        public string FrameScriptName { get; set; } = "frame.py";
        public string RenderScriptName { get; set; } = "render.py";
        public string DisposeScriptName { get; set; } = "dipose.py";

        public void Save(string filename = "config.dat")
        {
            var config = new JSONClass();
            config["class"] = this.ClassName;
            config["software operatable"] = new JSONData(DestinationApp.Instance.OperationType == OperationType.Software);

            config["sprites"] = this.ResourceFile;
            config["python directory"] = this.PythonDirectory;

            config["initialize script"] = this.InitializeScriptName;
            config["frame script"] = this.FrameScriptName;
            config["render script"] = this.RenderScriptName;
            config["dispose script"] = this.DisposeScriptName;

            config.SaveToCompressedFile(filename);
        }

        public bool Load(string filename = "config.dat")
        {
            try
            {
                var config = JSONClass.LoadFromCompressedFile(filename);

                this.ClassName = config["class"].Value;
                DestinationApp.Instance.OperationType = config["software operatable"].AsBool ? OperationType.Software : OperationType.Hardware;

                this.ResourceFile = config["sprites"].Value;
                this.PythonDirectory = config["python directory"].Value;

                this.InitializeScriptName = config["initialize script"].Value;
                this.FrameScriptName = config["frame script"].Value;
                this.RenderScriptName = config["render script"].Value;
                this.DisposeScriptName = config["dispose script"].Value;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
