﻿using FigmaSharp.Maui.Graphics.Sample.PropertyConfigure;
using FigmaSharp.Maui.Graphics.Sample.Services;
using FigmaSharp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FigmaSharp.Models;

namespace FigmaSharp.Maui.Graphics.Sample.ViewModels
{
    public partial class FigmaPage : ObservableObject
    {
        [ObservableProperty] private string _name;
        [ObservableProperty] private string _code;
        [ObservableProperty] private FigmaNode node;
        [ObservableProperty] private CompilationResult _compilationResult;
        [ObservableProperty] private bool _isSelected;
        public bool IsLoaded;
    }

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private string _token;
        [ObservableProperty] private string _fileId;
        [ObservableProperty] private string _code;
        [ObservableProperty] private bool _isGenerating;
        [ObservableProperty] private ObservableCollection<string> _log;
        [ObservableProperty] private ObservableCollection<FigmaPage> _pages;
        [ObservableProperty] private FigmaPage _selectedPage;
        private readonly Compiler _compiler;
        private RemoteNodeProvider _remoteNodeProvider;
        private CodeRenderService _codeRenderer;

        public MainViewModel()
        {
            Drawable = new MyDrawable(this);
            DrawableSet?.Invoke();
#if DEBUG
            // INSERT YOUR FIGMA ACCESS TOKEN
            Token = "";
            // INSERT THE FILE ID
            FileId = "";
#endif
            Log = new ObservableCollection<string>();
            _compiler = new Compiler();
        }

        public IDrawable Drawable { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }

        public event Action RedrawRequested;
        public event Action DrawableSet;

        public ICommand GenerateCommand => new Command(async () => await GenerateCodeAsync());
        public ICommand ExportCommand => new Command(async () => await Export());
        public ICommand ChangeSelectedPageCommand => new AsyncRelayCommand<FigmaPage>(ChangeSelectedPage);
        public float Scale { get; set; } = 1;

        async Task ChangeSelectedPage(FigmaPage page)
        {
            if (SelectedPage != null)
            {
                SelectedPage.IsSelected = false;
            }

            SelectedPage = null;

            await Task.Delay(1);

            if (!page.IsLoaded)
            {
                IsGenerating = true;
                await _remoteNodeProvider.LoadAsync(FileId, page.Node.id);
                page.Node = _codeRenderer.NodeProvider.Nodes[1];

                await GeneratePageSourceCode(page);
                page.IsLoaded = true;
                IsGenerating = false;
            }

            SelectedPage = page;
            SelectedPage.IsSelected = true;

            RedrawRequested?.Invoke();
        }

        async Task GenerateCodeAsync()
        {
            DrawableSet?.Invoke();
            try
            {
                if (string.IsNullOrEmpty(Token))
                {
                    var message =
                        "In order to obtain the necessary information from Figma, it is necessary to use a Personal Access Token.";
                    Log.Add(message);
                    DialogService.Instance.DisplayAlert("Information", message);
                    return;
                }

                FigmaApplication.Init(Token);
                if (string.IsNullOrEmpty(FileId))
                {
                    var message =
                        "In order to obtain the necessary information from Figma, it is necessary to use a FileId.";
                    Log.Add(message);
                    DialogService.Instance.DisplayAlert("Information", message);
                    return;
                }

                IsGenerating = true;
                Log.Add("Request the data to the Figma API.");

                _remoteNodeProvider = new RemoteNodeProvider();
                await _remoteNodeProvider.LoadAsync(FileId);

                Log.Add($"Data obtained successfully. {_remoteNodeProvider.Nodes.Count} nodes found.");

                Log.Add("Initializing the code generator.");

                var converters = AppContext.Current.GetFigmaConverters();
                var codePropertyConfigure = new CodePropertyConfigure();
                _codeRenderer = new CodeRenderService(_remoteNodeProvider, converters, codePropertyConfigure);

                Log.Add("Code generator initialized successfully.");

                Pages = new ObservableCollection<FigmaPage>(
                    _codeRenderer.NodeProvider.Nodes.Where(x => x.type == "CANVAS").Select(x => new FigmaPage()
                    {
                        Name = x.name,
                        Node = x,
                    }));

                RedrawRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private async Task GeneratePageSourceCode(FigmaPage page)
        {
            var stringBuilder = new StringBuilder();
            Log.Add($"Node {page.Node.id} found successfully.");

            Log.Add($"Generating source code for page {page.Name}...");

            var codeNode = new CodeNode(page.Node);

            _codeRenderer.GetCode(stringBuilder, codeNode);

            var code = stringBuilder.ToString();

            Log.Add($"Source Code for page {page.Name} generated successfully.");
            page.Code = code;
            page.CompilationResult = await CompileCodeAsync(code);

            Log.Add($"Source Code for page {page.Name} generated successfully.");
        }

        async Task<CompilationResult> CompileCodeAsync(string code)
        {
            if (_compiler == null)
                return null;

            Log.Add("Compiling the generated source code...");

            string sourceCode = string.Format(@"         
                using Microsoft.Maui.Graphics;

                public void Draw(ICanvas canvas, RectF dirtyRect)
                {{
                {0}
                }}", code);

            var compilationResult = await _compiler.CompileAsync(sourceCode);

            return compilationResult;
        }

        async Task Export()
        {
            if (string.IsNullOrEmpty(Code))
            {
                string message = "The generated code is not correct.";
                Log.Add(message);
                DialogService.Instance.DisplayAlert("Information", message);
                return;
            }

#if MACCATALYST || WINDOWS
            try
            {
                var folderPicker = new FolderPicker();
                string folder = await folderPicker.PickFolder();
                string path = Path.Combine(folder, "FigmaToMauiGraphics.txt");
                await File.WriteAllTextAsync(path, Code);

                string message = "The file has been created successfully.";
                Log.Add(message);
                DialogService.Instance.DisplayAlert("Information", message);
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
#endif
        }
    }
}