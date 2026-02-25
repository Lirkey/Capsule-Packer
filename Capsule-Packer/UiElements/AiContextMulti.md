# Сборка проекта для ИИ
**Всего файлов:** 1

## Файл: `AppIntroUi.cs`
```csharp
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage;
using Capsule_Packer.IdeModule;
using Capsule_Packer.Logic.Models;
using Capsule_Packer.UiFactory;

namespace Capsule_Packer.UiElements;

public class AppIntroUi
{
    private readonly AppIntro _model;

    public AppIntroUi(AppIntro model)
    {
        _model = model;
    }

    public Control Build()
{ /* логика скрыта */ }}
```
---
