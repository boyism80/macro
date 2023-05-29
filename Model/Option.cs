using System.IO;

namespace macro.Model
{
    public class Option
    {
        public OperationType OperationType { get; set; } = OperationType.Software;
        public string Class { get; set; } = "LOSTARK";
        public int RenderFrame { get; set; } = 30;
        public int DetectFrame { get; set; } = 30;

        public string ResourceFilePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "sprite.dat");
        public string ScriptDirectoryPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "sprite");
        public string PythonDirectoryPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Python");
        public string InitializeScriptName { get; set; } = "init.py";
        public string FrameScriptName { get; set; } = "frame.py";
        public string RenderScriptName { get; set; } = "render.py";
        public string DisposeScriptName { get; set; } = "dispose.py";

        public Option()
        {

        }
    }
}
