using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate; // Для подсветки
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextMateSharp.Grammars; // Для грамматики C#

namespace Capsule_Packer.UiElements;

// --- 1. КЛАСС-РЕРАЙТЕР ДЛЯ СЖАТИЯ КОДА ---
public class CodeCompressorRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<string> _hiddenMethods;
    private readonly HashSet<string> _hiddenClasses;

    public CodeCompressorRewriter(HashSet<string> hiddenMethods, HashSet<string> hiddenClasses)
    {
        _hiddenMethods = hiddenMethods;
        _hiddenClasses = hiddenClasses;
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Если сняли галочку с класса — сжимаем его целиком
        if (_hiddenClasses.Contains(node.Identifier.Text))
        {
            return node.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>())
                       .WithCloseBraceToken(SyntaxFactory.Token(
                           SyntaxFactory.TriviaList(SyntaxFactory.Comment("\n    /* весь класс сжат */\n")),
                           SyntaxKind.CloseBraceToken,
                           SyntaxFactory.TriviaList()));
        }
        return base.VisitClassDeclaration(node);
    }

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Если сняли галочку с метода — сжимаем только его
        if (_hiddenMethods.Contains(node.Identifier.Text))
        {
            var emptyBody = SyntaxFactory.Block()
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(SyntaxFactory.Comment(" /* логика скрыта */ ")),
                    SyntaxKind.CloseBraceToken,
                    SyntaxFactory.TriviaList()
                ));

            return node.WithBody(emptyBody)
                       .WithExpressionBody(null)
                       .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
        }
        return base.VisitMethodDeclaration(node);
    }
}

// --- 2. ГЛАВНОЕ ОКНО ПРОЕКТА ---
public class ProjectWindow : Window
{
    private readonly TextEditor _textEditor;
    private SyntaxTree _currentTree;
    
    // Храним скрытые элементы
    private readonly HashSet<string> _hiddenMethods = new();
    private readonly HashSet<string> _hiddenClasses = new();
    
    private string _currentFileName = "None";
    private readonly string _baseFolderPath;

    // "Корзина" для сборки
    private readonly Dictionary<string, string> _stagedFiles = new();
    private readonly TextBlock _stagingStatusText;

    public ProjectWindow(string folderPath)
    {
        _baseFolderPath = folderPath;

        this.Title("Capsule Packer IDE")
            .Width(1100).Height(700)
            .WindowStartupLocation(WindowStartupLocation.CenterScreen)
            .Background(Brushes.DimGray);

        string folderName = new DirectoryInfo(folderPath).Name;
        string shortPathBase = $".../{folderName}";

        var pathText = new TextBlock().Text(shortPathBase).FontSize(16).FontWeight(FontWeight.Bold).Margin(10);
        var scriptsListPanel = new StackPanel().Spacing(5).Margin(5);
        var navigatorPanel = new StackPanel().Spacing(5).Margin(5);

        // Инициализация редактора
        _textEditor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            Margin = new Avalonia.Thickness(5),
            IsReadOnly = false,
            Document = new TextDocument(""),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        // Подключаем TextMate для подсветки C# (Тёмная тема)
        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = _textEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("csharp"));

        // --- НИЖНЯЯ ПАНЕЛЬ С КНОПКАМИ ---
        _stagingStatusText = new TextBlock()
            .Text("Файлов в сборке: 0")
            .VerticalAlignment(VerticalAlignment.Center)
            .FontWeight(FontWeight.Bold)
            .Margin(10, 0);

        var stageButton = new Button()
            .Content("➕ В сборку")
            .Background(Brushes.SteelBlue).Foreground(Brushes.White).Margin(5)
            .OnClick(e => 
            {
                if (!string.IsNullOrEmpty(_textEditor.Document.Text) && _currentFileName != "None")
                {
                    _stagedFiles[_currentFileName] = _textEditor.Document.Text;
                    _stagingStatusText.Text = $"Файлов в сборке: {_stagedFiles.Count}";
                }
            });

        var buildButton = new Button()
            .Content("💾 Сохранить MD")
            .Background(Brushes.DarkGreen).Foreground(Brushes.White).Margin(5)
            .OnClick(e => 
            {
                var md = BuildFinalMarkdownString();
                if (!string.IsNullOrEmpty(md))
                {
                    File.WriteAllText(Path.Combine(_baseFolderPath, "AiContextMulti.md"), md);
                    _stagingStatusText.Text = "✅ Сохранено в файл!";
                }
            });

        var copyButton = new Button()
            .Content("📋 Копировать MD")
            .Background(Brushes.DarkOrchid).Foreground(Brushes.White).Margin(5)
            .OnClick(async e => 
            {
                var md = BuildFinalMarkdownString();
                if (!string.IsNullOrEmpty(md))
                {
                    // Avalonia 11 способ работы с буфером обмена
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(md);
                        _stagingStatusText.Text = "🚀 Скопировано в буфер!";
                    }
                }
            });

        var bottomPanel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Right)
            .Margin(10)
            .Children(_stagingStatusText, stageButton, buildButton, copyButton);

        // --- ЗАГРУЗКА ФАЙЛОВ И НАВИГАТОР ---
        string[] files = Directory.Exists(folderPath) 
            ? Directory.GetFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly) 
            : new[] { "FakeFile.cs" };

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);

            scriptsListPanel.Children.Add(new Button()
                .Content($"📄 {fileName}")
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .OnClick(e => 
                {
                    _currentFileName = fileName;
                    pathText.Text = $"{shortPathBase}/{fileName}";
                    
                    // Сбрасываем скрытые элементы при смене файла
                    _hiddenMethods.Clear(); 
                    _hiddenClasses.Clear();

                    string codeText = File.Exists(file) ? File.ReadAllText(file) : "public class Fake { void Test() {} }";
                    _currentTree = CSharpSyntaxTree.ParseText(codeText);
                    UpdateEditorText(); 

                    navigatorPanel.Children.Clear();
                    navigatorPanel.Children.Add(new TextBlock().Text("Структура файла").FontWeight(FontWeight.Bold).Margin(0, 0, 0, 10));

                    var root = _currentTree.GetCompilationUnitRoot();
                    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classNode in classes)
                    {
                        string className = classNode.Identifier.Text;
                        
                        // Чекбокс для КЛАССА
                        navigatorPanel.Children.Add(new CheckBox()
                            .Content($"class {className}")
                            .IsChecked(true)
                            .FontWeight(FontWeight.Bold)
                            .OnChecked(ev => { _hiddenClasses.Remove(className); UpdateEditorText(); })
                            .OnUnchecked(ev => { _hiddenClasses.Add(className); UpdateEditorText(); }));

                        var methods = classNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        
                        foreach (var methodNode in methods)
                        {
                            string methodName = methodNode.Identifier.Text;
                            string returnType = methodNode.ReturnType.ToString();
                            
                            // Чекбокс для МЕТОДА
                            navigatorPanel.Children.Add(new CheckBox()
                                .Content($"{returnType} {methodName}()")
                                .IsChecked(true)
                                .Margin(20, 0, 0, 0)
                                .OnChecked(ev => { _hiddenMethods.Remove(methodName); UpdateEditorText(); })
                                .OnUnchecked(ev => { _hiddenMethods.Add(methodName); UpdateEditorText(); }));
                        }
                    }
                }));
        }

        // --- СБОРКА ОКНА ---
        this.Content(
            new DockPanel().Children(
                pathText.Dock(Dock.Top),
                bottomPanel.Dock(Dock.Bottom),
                
                new Grid()
                    .Cols("250, *")
                    .Children(
                        new Grid()
                            .Rows("*, *").Col(0)
                            .Children(
                                new Border().BorderBrush(Brushes.DarkGray).BorderThickness(0, 0, 1, 1).Child(new ScrollViewer().Content(scriptsListPanel)).Row(0),
                                new Border().BorderBrush(Brushes.DarkGray).BorderThickness(0, 0, 1, 0).Child(new ScrollViewer().Content(navigatorPanel)).Row(1)
                            ),
                        _textEditor.Col(1)
                    )
            )
        );
    }

    // --- ЛОГИКА ОБНОВЛЕНИЯ И СБОРКИ ---
    private void UpdateEditorText()
    {
        if (_currentTree == null) return;

        // Передаем оба HashSet в рерайтер
        var rewriter = new CodeCompressorRewriter(_hiddenMethods, _hiddenClasses);
        var newRoot = rewriter.Visit(_currentTree.GetRoot());
        
        _textEditor.Document = new TextDocument(newRoot.ToFullString());
    }

    private string BuildFinalMarkdownString()
    {
        if (_stagedFiles.Count == 0) return string.Empty;

        var mdBuilder = new StringBuilder();
        mdBuilder.AppendLine("# Сборка проекта для ИИ");
        mdBuilder.AppendLine($"**Всего файлов:** {_stagedFiles.Count}\n");

        foreach (var file in _stagedFiles)
        {
            mdBuilder.AppendLine($"## Файл: `{file.Key}`");
            mdBuilder.AppendLine("```csharp");
            mdBuilder.AppendLine(file.Value);
            mdBuilder.AppendLine("```");
            mdBuilder.AppendLine("---");
        }

        return mdBuilder.ToString();
    }
}