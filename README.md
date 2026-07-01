# CustomWinConsolePlugins

Репозиторий плагинов для [CustomWinConsole](https://github.com/Marrcus113/CustomWinConsole).

## Список плагинов

| Плагин | Версия | Описание |
|--------|--------|----------|
| [bash_WSL2](src/bash_WSL2/) | 1.1.0 | WSL2 + apt-эмуляция через winget. bash, apt, wsl2-distro, конвертация путей |

---

## Формат плагина

Плагин — это .NET DLL под `net10.0`, реализующий `ICwcPlugin`.

### Шаблон проекта

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CustomWinConsole\CustomWinConsole.csproj" />
  </ItemGroup>
</Project>
```

### Пример

```csharp
using CustomWinConsole;

public class MyCommands : ICwcPlugin
{
    public string Name => "MyCommands";
    public string Version => "1.0.0";
    public string Description => "Мои крутые команды";

    public void Register(IPluginHost host)
    {
        host.RegisterCommand("hello", args =>
        {
            host.Write("Привет из плагина!", ConsoleColor.Green);
        });

        host.RegisterCommand("math", args =>
        {
            if (args.Length < 2) { host.WriteError("Использование: math <a> <b>"); return; }
            if (int.TryParse(args[0], out var a) && int.TryParse(args[1], out var b))
                host.WriteLine($"{a} + {b} = {a + b}");
        });
    }
}
```

### API плагина

```csharp
interface ICwcPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    void Register(IPluginHost host);
}

interface IPluginHost
{
    void RegisterCommand(string name, Action<string[]> handler);
    void Write(string text, ConsoleColor color = ConsoleColor.Gray);
    void WriteLine(string text = "");
    void WriteError(string text);
    string CurrentDirectory { get; }
}
```

## Управление плагинами

| Команда | Описание |
|---------|----------|
| `plugin list` | Список установленных плагинов |
| `plugin install <name>` | Установить плагин |
| `plugin remove <name>` | Удалить плагин |
| `plugin repo` | Показать репозиторий |

## Как добавить свой плагин

1. Форкни репозиторий на GitHub
2. Клонируй форк:
   ```bash
   git clone https://github.com/ТВОЙ_НИК/CustomWinConsolePlugins.git
   cd CustomWinConsolePlugins
   ```
3. Создай папку `src/<имя_плагина>/` и напиши плагин
4. Собери плагин (нужен CustomWinConsole рядом):
   ```bash
   dotnet build -c Release
   ```
5. Закоммить и запушь:
   ```bash
   git add src/<имя_плагина>/
   git commit -m "Добавлен плагин <имя>"
   git push
   ```
6. Открой Pull Request на github.com/Marrcus113/CustomWinConsolePlugins

## Установка плагина себе (локально)

```bash
# Сборка
dotnet build -c Release

# Копирование
copy src\ИмяПлагина\bin\Release\net10.0\ИмяПлагина.dll %USERPROFILE%\.customwinconsole\plugins\
```
