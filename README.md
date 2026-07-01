# CustomWinConsolePlugins

Репозиторий плагинов для [CustomWinConsole](https://github.com/Marrcus113/CustomWinConsole).

## Список плагинов

| Плагин | Версия | Описание |
|--------|--------|----------|
| [bash_WSL2](src/bash_WSL2/) | 1.0.0 | Интеграция с WSL2: bash-команды, конвертация путей, дистрибутивы |

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

1. Форкни этот репозиторий
2. Создай папку `src/<имя_плагина>/`
3. Напиши плагин по шаблону
4. Открой Pull Request

## Сборка плагина

```bash
# Из корня CustomWinConsole
dotnet build -c Release

# Скопируй DLL в папку плагинов
copy src\МойПлагин\bin\Release\net10.0\МойПлагин.dll %USERPROFILE%\.customwinconsole\plugins\
```
