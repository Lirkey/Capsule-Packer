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
    {
        // 1. Создаем контролы ЗАРАНЕЕ, чтобы иметь к ним доступ в событиях кнопок
        var urlInput = new TextBox()
            .Watermark("Вставь ссылку на GitHub...");

        var folderInput = new TextBox()
            .Watermark("...или выбери локальную папку проекта");

        // 2. Строим интерфейс
        return new Grid()
            .Margin(20)
            // Добавили ряды: Заголовок, GitHub, Локальная папка, Кнопка упаковки
            .Rows("Auto, Auto, Auto, Auto, *")
            .Cols("*, Auto")
            .Children(

                // Ряд 0: Заголовок
                ControlFactory.CreateTopCenterTitle(_model.TitleText)
                    .Row(0).Col(0).ColSpan(2).Margin(0, 0, 0, 30),

                // Ряд 1: Ввод GitHub (растягиваем на две колонки)
                urlInput
                    .Row(1).Col(0).ColSpan(2).Margin(0, 0, 0, 10),

                // Ряд 2: Ввод локальной папки (колонка 0)
                folderInput
                    .Row(2).Col(0).Margin(0, 0, 10, 10),

                // Ряд 2: Кнопка выбора папки (колонка 1)
                new Button()
                    .Content("Выбрать папку")
                    .Row(2).Col(1).Margin(0, 0, 0, 10)
                    .OnClick(async e =>
                    {
                        // Берем элемент, по которому кликнули (e.Source)
                        var topLevel = TopLevel.GetTopLevel(e.Source as Control);
                        var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                        {
                            Title = "Выберите папку с проектом"
                        });

                        if (folders.Count > 0)
                        {
                            folderInput.Text = folders[0].Path.LocalPath;
                        }
                    }),

                // Ряд 3: Кнопка "Pack it!" (по центру, под всем остальным)
                new Button()
                    .Content("Pack it!")
                    .Classes("Primary") // Если потом захочешь добавить стили в фабрику
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Row(3).Col(0).ColSpan(2).Margin(0, 20, 0, 0)
                    .OnClick(e => 
                    {
                        string targetPath = string.IsNullOrWhiteSpace(folderInput.Text) 
                            ? "C:/Fake/Github/Project" 
                            : folderInput.Text;

                        var projectWindow = new ProjectWindow(targetPath);
                        projectWindow.Show();
                    })
            );
    }
}