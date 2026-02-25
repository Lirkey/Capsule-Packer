namespace Capsule_Packer.UiFactory;

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative; // Магия Fluent-синтаксиса здесь

public static class ControlFactory
{
    // Дефолтный метод для создания заголовка по центру сверху
    public static TextBlock CreateTopCenterTitle(string text)
    {
        return new TextBlock()
            .Text(text)
            .FontSize(28)
            .FontWeight(FontWeight.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center) // По центру по горизонтали
            .VerticalAlignment(VerticalAlignment.Top)        // Сверху по вертикали
            .Margin(0, 20, 0, 0);        // Отступ 20 пикселей сверху, чтобы не липло к краю окна
    }
}