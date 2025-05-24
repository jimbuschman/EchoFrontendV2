using System.Diagnostics;

public class OllamaProcessManager
{
    private Process? _ollamaProcess;

    public void StartOllamaCpuServer()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/C set OLLAMA_PORT=11435 && ollama serve",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _ollamaProcess = Process.Start(startInfo);
    }

    public void StopOllamaCpuServer()
    {
        if (_ollamaProcess != null && !_ollamaProcess.HasExited)
        {
            _ollamaProcess.Kill(true);
            _ollamaProcess.Dispose();
        }
    }
}
