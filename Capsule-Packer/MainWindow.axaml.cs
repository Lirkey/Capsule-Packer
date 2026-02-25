using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Capsule_Packer.Logic.Models;
using Capsule_Packer.UiElements;

namespace Capsule_Packer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Настраиваем само окно
        this.Title("Capsule Packer - The BEST!")
            .Width(800)
            .Height(600)
            .Background(Brushes.DimGray);

        // 1. Создаем объект с данными (Логика)
        var introData = new AppIntro { TitleText = "Capsule Packer - The BEST!" };

        // 2. Создаем визуальную обертку для этих данных
        var introUi = new AppIntroUi(introData);

        // 3. Закидываем результат в окно (Panel позволяет накладывать слои)
        this.Content(
            new Panel()
                .Children(
                    introUi.Build() // Тот самый вывод текста сверху по центру
                )
        );
    }
}