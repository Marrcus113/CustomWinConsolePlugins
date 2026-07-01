using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CustomWinConsole;

namespace BashWsl2Plugin;

public class BashWsl2Plugin : ICwcPlugin
{
    public string Name => "bash_WSL2";
    public string Version => "1.1.0";
    public string Description => "WSL2 + apt-эмуляция через winget. Интеграция Linux и Windows";

    private IPluginHost? _host;
    private string _primaryDistro = "";

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
        host.RegisterCommand("apt", Apt);
        host.RegisterCommand("winget", WingetCommand);
    }

    private string GetPrimaryDistro()
    {
        if (!string.IsNullOrEmpty(_primaryDistro)) return _primaryDistro;

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-l -v",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            });
            if (proc == null) return "";
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("NAME")) continue;

                var parts = Regex.Split(trimmed, @"\s+");
                if (parts.Length >= 3)
                {
                    var name = parts[0].Trim();
                    var state = parts[2].Trim();

                    if (state == "Running" && !name.Contains("docker", StringComparison.OrdinalIgnoreCase))
                    {
                        _primaryDistro = name;
                        return _primaryDistro;
                    }
                }
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("NAME")) continue;
                var parts = Regex.Split(trimmed, @"\s+");
                if (parts.Length >= 3)
                {
                    var name = parts[0].Trim();
                    if (!name.Contains("docker", StringComparison.OrdinalIgnoreCase))
                    {
                        _primaryDistro = name;
                        return _primaryDistro;
                    }
                }
            }
        }
        catch { }
        return "";
    }

    private bool CheckWsl()
    {
        var distro = GetPrimaryDistro();
        return !string.IsNullOrEmpty(distro);
    }

    private string RunWslCommand(string command, int timeoutSec = 10)
    {
        var distro = GetPrimaryDistro();
        if (string.IsNullOrEmpty(distro))
            return "Ошибка: WSL2 дистрибутив не найден (docker-desktop не считается)";

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d \"{distro}\" -- bash -lc \"{command.Replace("\"", "\\\"")}\"",
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

    private string RunWinget(string args, int timeoutSec = 30)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "winget.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) return "Ошибка: winget не найден";
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
            _host?.WriteError("WSL2 дистрибутив не найден. Установи: wsl --install -d Ubuntu");
            return;
        }

        if (args.Length == 0)
        {
            _host?.WriteLine($"  Использование: bash <команда>");
            _host?.WriteLine($"  Дистрибутив: {GetPrimaryDistro()}");
            _host?.WriteLine($"  Пример: bash ls -la");
            _host?.WriteLine($"  Пример: bash 'echo Привет из Linux!'");
            return;
        }

        var command = string.Join(" ", args);
        _host?.WriteLine($"  [{GetPrimaryDistro()}] $ {command}");
        _host?.WriteLine();

        var result = RunWslCommand(command, 30);
        if (result.StartsWith("Ошибка:"))
            _host?.WriteError(result);
        else
            _host?.WriteLine(result);
    }

    private void Wsl2Info(string[] args)
    {
        if (!CheckWsl())
        {
            _host?.WriteError("WSL2 дистрибутив не найден.");
            return;
        }

        _host?.WriteLine($"  Дистрибутив: {GetPrimaryDistro()}");
        _host?.WriteLine();
        _host?.WriteLine(RunWslCommand("uname -a"));
        _host?.WriteLine();
        _host?.WriteLine(RunWslCommand("cat /etc/os-release | head -5"));
    }

    private void Wsl2Path(string[] args)
    {
        if (!CheckWsl()) { _host?.WriteError("WSL2 не найден."); return; }
        _host?.Write($"  Текущая WSL директория: ");
        _host?.WriteLine(RunWslCommand("pwd"));
    }

    private void Wsl2ToWindows(string[] args)
    {
        if (args.Length == 0) { _host?.WriteError("Использование: wsl2-to-win /home/user/file.txt"); return; }
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e wslpath -w \"{args[0]}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) { _host?.WriteError("Ошибка"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            _host?.WriteLine(result);
        }
        catch (Exception ex) { _host?.WriteError(ex.Message); }
    }

    private void Wsl2ToLinux(string[] args)
    {
        if (args.Length == 0) { _host?.WriteError("Использование: wsl2-to-linux C:\\Users\\file.txt"); return; }
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e wslpath -u \"{args[0]}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc == null) { _host?.WriteError("Ошибка"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            _host?.WriteLine(result);
        }
        catch (Exception ex) { _host?.WriteError(ex.Message); }
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
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            });
            if (proc == null) { _host?.WriteError("Ошибка"); return; }
            var result = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();

            _host?.WriteLine("  WSL2 дистрибутивы:");
            _host?.WriteLine();
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("NAME")) { _host?.WriteLine($"  {trimmed}"); continue; }

                var parts = Regex.Split(trimmed, @"\s+");
                if (parts.Length >= 4)
                {
                    var name = parts[0];
                    if (name.Contains("docker", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var isPrimary = name == _primaryDistro;
                    var isRunning = parts[2] == "Running";
                    _host?.Write($"    {name,-25} ");
                    _host?.Write(parts[1], isRunning ? ConsoleColor.Green : ConsoleColor.DarkGray);
                    _host?.Write("  ");
                    _host?.Write(parts[2], isRunning ? ConsoleColor.Green : ConsoleColor.Red);
                    _host?.WriteLine($"  {parts[3]}");
                    if (isPrimary)
                    {
                        _host?.Write("    " + new string(' ', 25));
                        _host?.Write("← основной", ConsoleColor.Cyan);
                        _host?.WriteLine();
                    }
                }
            }
        }
        catch (Exception ex) { _host?.WriteError(ex.Message); }
    }

    private void Apt(string[] args)
    {
        if (args.Length == 0)
        {
            _host?.WriteLine("  Эмуляция apt через winget");
            _host?.WriteLine();
            _host?.WriteLine("  Команды:");
            _host?.WriteLine("    apt install <pkg>    Установить пакет (winget install)");
            _host?.WriteLine("    apt remove <pkg>     Удалить пакет (winget uninstall)");
            _host?.WriteLine("    apt list              Список установленных (winget list)");
            _host?.WriteLine("    apt search <name>     Поиск пакета (winget search)");
            _host?.WriteLine("    apt update            Обновить источники (winget source update)");
            _host?.WriteLine("    apt upgrade           Обновить пакеты (winget upgrade --all)");
            return;
        }

        switch (args[0].ToLower())
        {
            case "install":
                if (args.Length < 2) { _host?.WriteError("Использование: apt install <пакет>"); return; }
                var pkg = string.Join(" ", args[1..]);
                _host?.WriteLine($"  Устанавливаю: {pkg}");
                _host?.WriteLine();
                var result = RunWinget($"install --accept-package-agreements --accept-source-agreements \"{pkg}\"");
                _host?.WriteLine(result);
                break;

            case "remove":
                if (args.Length < 2) { _host?.WriteError("Использование: apt remove <пакет>"); return; }
                _host?.WriteLine(RunWinget($"uninstall \"{string.Join(" ", args[1..])}\""));
                break;

            case "list":
                _host?.WriteLine(RunWinget("list"));
                break;

            case "search":
                if (args.Length < 2) { _host?.WriteError("Использование: apt search <запрос>"); return; }
                _host?.WriteLine(RunWinget($"search \"{string.Join(" ", args[1..])}\""));
                break;

            case "update":
                _host?.WriteLine(RunWinget("source update"));
                break;

            case "upgrade":
                _host?.WriteLine(RunWinget("upgrade --all"));
                break;

            default:
                _host?.WriteError($"Неизвестная команда: {args[0]}. Используй: install, remove, list, search, update, upgrade");
                break;
        }
    }

    private void WingetCommand(string[] args)
    {
        if (args.Length == 0)
        {
            _host?.WriteLine("  Использование: winget <аргументы>");
            _host?.WriteLine("  Пример: winget install python");
            _host?.WriteLine("  Пример: winget list");
            return;
        }

        _host?.WriteLine(RunWinget(string.Join(" ", args)));
    }
}
