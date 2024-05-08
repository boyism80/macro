using IronPython.Hosting;
using macro.Extension;
using Microsoft.Scripting.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace macro.ViewModel
{
    public partial class MainWindow
    {
        private ScriptRuntime _pythonRuntime;
        public bool IsExecutePython { get; private set; }

        private void LoadPythonModules(string path)
        {
            if (path.Length == 0)
                throw new Exception("You must set a valid python path.");

            if (Directory.Exists(path) == false)
                throw new Exception($"{path} path does not exist.");

            if (File.Exists(Path.Combine(path, "python.exe")) == false)
                throw new Exception($"Cannot find python.exe file in '{path}'.");

            if (_pythonRuntime != null)
                _pythonRuntime.Shutdown();

            _pythonRuntime = Python.CreateRuntime();
            var engine = _pythonRuntime.GetEngine("IronPython");
            var paths = engine.GetSearchPaths();
            paths.Add(path);
            paths.Add(Path.Combine(path, @"DLLs"));
            paths.Add(Path.Combine(path, @"lib"));
            paths.Add(Path.Combine(path, @"lib\site-packages"));
            paths.Add(Path.Combine(Option.ScriptDirectoryPath));
            engine.SetSearchPaths(paths);

            ExecPython("init.py");
        }

        private void ReleasePythonModule()
        {
            _pythonRuntime?.Shutdown();
            _pythonRuntime = null;
        }

        private Task<object> ExecPython(string path)
        {
            if (IsExecutePython)
                return null;

            if (_pythonRuntime == null)
                return null;

            var tcs = new TaskCompletionSource<object>();
            Task.Run(() =>
            {
                try
                {
                    dynamic scope = _pythonRuntime.UseFile(Path.Combine(Option.ScriptDirectoryPath, path));
                    IsExecutePython = true;
                    var ret = scope.callback(this);

                    if (ret is IronPython.Runtime.PythonGenerator generator)
                    {
                        foreach (var value in generator)
                        {
                            if (Running == false)
                                break;

                            if (ret != null)
                                Logs.Add($"{path} return : {value}");
                        }

                        tcs.SetResult(generator);
                    }
                    else
                    {
                        if (ret != null)
                            Logs.Add($"{path} return : {ret}");

                        tcs.SetResult(ret);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logs.Add($"{path} does not exists");
                    tcs.SetException(e);
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        Logs.Add($"{path} return : {e.Message}");
                        Logs.Add(e.StackTrace);
                    }
                    tcs.SetException(e);
                }
                finally
                {
                    IsExecutePython = false;
                }
            });

            return tcs.Task;
        }
    }
}
