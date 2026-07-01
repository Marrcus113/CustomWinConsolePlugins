using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CustomWinConsole;

namespace BashWsl2Plugin;

public class BashWsl2Plugin : ICwcPlugin
{
    public string Name => "bash_WSL2";
    public string Version => "1.0.0";
    public string Description => "Интеграция с WSL2: запуск bash-команд, конвертация путей, управление дистрибутивами";

    private IPluginHost? _host;

    public void Register(IPluginHost host)
    {
        _host = host;
        host.RegisterCommand("bash", RunBash);
        host.RegisterCommand("wsl2", Wsl2Info);
        host.RegisterCommand("wsl2-path", Wsl2Path);
        host.RegisterCommand("wsl2-to-win", Wsl2ToWindows);
        host.RegisterCommand("wsl2-to-linux", Wsl2ToLinux);
        host.RegisterCommand("wsl2-distro", Wsl2Distro);
        host.RegisterCommand("wsl2-exec", RunBash);
    }

    private bool CheckWsl()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) return false;
            proc.WaitForExit(3000);
            return proc.ExitCode == 0;
        }
        catch { return false; }
    }

    private string RunWslCommand(string command, int timeoutSec = 10)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-- bash -lc \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            });
            if (proc == null) return "Ошибка: не удалось запустить wsl.exe";
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(timeoutSec * 1000);
            if (proc.ExitCode != 0 && !string.IsNullOrEmpty(stderr))
                return $"Ошибка: {stderr.Trim()}";
            return stdout.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Ошибка: {ex.Message}";
        }
    }

    private void RunBash(string[] args)
    {
        if (!CheckWsl())
        {
            _host?.WriteError("WSL2 не найден! Установи его: wsl --install");
            return;
        }

        if (args.Length == 0)
        {
            _host?.WriteLine("Использование: bash <команда>");
            _host?.WriteLine("  Пример: bash ls -la");
            _host?.WriteLine("  Пример: bash 'echo Привет из Linux!'");
            _host?.WriteLine("  Пример: bash 'apt update && apt upgrade -y'");
            return;
        }

        var command = string.Join(" ", args);
        _host?.WriteLine($"  Выполняю: bash -c \"{command}\"");
        _host?.WriteLine();

        var result = RunWslCommand(command, 30);
        if (result.StartsWith("Ошибка:"))
        {
            _host?.WriteError(result);
        }
        else
        {
            _host?.Write(result, ConsoleColor.Gray);
        }
    }

    private void Wsl2Info(string[] args)
    {
        if (!CheckWsl())
        {
            _host?.WriteError("WSL2 не найден.");
            return;
        }

        _host?.WriteLine("  WSL2 статус:");
        _host?.WriteLine();
        _host?.Write(RunWslCommand("uname -a"));
        _host?.WriteLine();
        _host?.WriteLine();
        _host?.Write(RunWslCommand("cat /etc/os-release | head -5"));
    }

    private void Wsl2Path(string[] args)
    {
        if (!CheckWsl())
        {
            _host?.WriteError("WSL2 не найден.");
            return;
        }

        _host?.Write("  Текущая WSL директория: ");
        _host?.WriteLine(RunWslCommand("pwd"));
    }

    private void Wsl2ToWindows(string[] args)
    {
        if (args.Length == 0)
        {
            _host?.WriteError("Использование: wsl2-to-win /home/user/file.txt");
            return;
        }

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e wslpath -w \"{args[0]}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) { _host?.WriteError("Ошибка запуска wsl.exe"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            _host?.Write(result);
        }
        catch (Exception ex)
        {
            _host?.WriteError(ex.Message);
        }
    }

    private void Wsl2ToLinux(string[] args)
    {
        if (args.Length == 0)
        {
            _host?.WriteError("Использование: wsl2-to-linux C:\\Users\\file.txt");
            return;
        }

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e wslpath -u \"{args[0]}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) { _host?.WriteError("Ошибка запуска wsl.exe"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            _host?.Write(result);
        }
        catch (Exception ex)
        {
            _host?.WriteError(ex.Message);
        }
    }

    private void Wsl2Distro(string[] args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-l -v",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) { _host?.WriteError("Ошибка запуска wsl.exe"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();

            _host?.WriteLine("  Установленные WSL дистрибутивы:");
            _host?.WriteLine();
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (i == 0)
                    _host?.WriteLine($"  {line}");
                else
                {
                    var parts = Regex.Split(line.Trim(), @"\s+");
                    if (parts.Length >= 4)
                    {
                        var isRunning = parts[2] == "Running";
                        _host?.Write($"    {parts[0],-20} ");
                        _host?.Write(parts[1], isRunning ? ConsoleColor.Green : ConsoleColor.DarkGray);
                        _host?.Write("  ");
                        _host?.Write(parts[2], isRunning ? ConsoleColor.Green : ConsoleColor.Red);
                        _host?.WriteLine($"  {parts[3]}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _host?.WriteError(ex.Message);
        }
    }
}
