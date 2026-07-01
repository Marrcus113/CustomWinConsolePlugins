# CustomWinConsolePlugins

Репозиторий плагинов для [CustomWinConsole](https://github.com/Marrcus113/CustomWinConsole).

## Формат плагина

Плагин — это .NET DLL, компилируемый под `net10.0`, реализующий интерфейс `ICwcPlugin`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CustomWinConsole.SDK" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Пример плагина

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

### Установка плагина

```bash
plugin install MyCommands
```

Скачивает DLL из релизов этого репозитория или из указанного URL.

### Команды управления

| Команда | Описание |
|---------|----------|
| `plugin list` | Список установленных плагинов |
| `plugin install <name>` | Установить плагин |
| `plugin remove <name>` | Удалить плагин |
| `plugin repo` | Показать репозиторий плагинов |

## Как добавить свой плагин

1. Создай проект по шаблону выше
2. Реализуй `ICwcPlugin`
3. Собери `dotnet build -c Release`
4. Открой Issue или Pull Request в этот репозиторий

## Релизы

Плагины публикуются как GitHub Releases с asset'ами `.dll`.
