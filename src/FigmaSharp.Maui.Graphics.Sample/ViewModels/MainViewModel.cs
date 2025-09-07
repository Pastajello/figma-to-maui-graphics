using FigmaSharp.Maui.Graphics.Sample.PropertyConfigure;
using FigmaSharp.Maui.Graphics.Sample.Services;
using FigmaSharp.Services;
using System.Collections.ObjectModel;
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
        [ObservableProperty] private float _scale = 1;

        private readonly Compiler _compiler;
        private RemoteNodeProvider _remoteNodeProvider;
        private CodeRenderService _codeRenderer;

        public MainViewModel()
        {
            Drawable = new MyDrawable(this);
            DrawableSet?.Invoke();
#if DEBUG
            Token = "figd_IM4B7-AQdA6spGvo5CEnk8I2hWSWmwOR--y0mYCg";
            // INSERT THE FILE ID
            // FileId = "b2J77o04FzVZNOFyZ3NVtd";
            FileId = "JfcelfM7TT1XOqOcbiPtb8";


#endif
            Log = new ObservableCollection<string>();
            _compiler = new Compiler();
        }

        public IDrawable Drawable { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }

        public event Action RedrawRequested;
        public event Action DrawableSet;
        public event Action Recompiled;
        

        public ICommand GenerateCommand => new Command(async () => await GenerateCodeAsync());
        public ICommand ExportCommand => new Command(async () => await Export());
        public ICommand CompileCommand => new Command(async () => await Compile());
        public ICommand ChangeSelectedPageCommand => new AsyncRelayCommand<FigmaPage>(ChangeSelectedPage);

        async Task ChangeSelectedPage(FigmaPage page)
        {
            if (SelectedPage != null)
            {
                SelectedPage.IsSelected = false;
            }

            SelectedPage = null;

            if (!page.IsLoaded)
            {
                IsGenerating = true;
                await Task.Run(async () => await _remoteNodeProvider.LoadAsync(FileId, page.Node.id, 20));
                page.Node = _codeRenderer.NodeProvider.Nodes.FirstOrDefault(x => x.id == page.Node.id);
                var imagesIThink = _codeRenderer.NodeProvider.Nodes.Where(x => x is RectangleVector);

                var frames = _codeRenderer.NodeProvider.Nodes
                    .Where(node => node is FigmaFrame)
                    .Select(node => node as FigmaFrame);
                foreach (var image in
                         frames
                             .Where(x => x.fills.Any(fill => fill.type == "IMAGE")))
                {
                    var imageUrl = $"https://api.figma.com/v1/images/:{FileId}?ids=:{image.fills.First(x=>x.type=="IMAGE").imageRef}";
                    int i = 5;
                }

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
                    _codeRenderer.NodeProvider.Nodes.Where(x => x.type == "CANVAS" && x.name != "---").Select(x =>
                        new FigmaPage()
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
                using Microsoft.Maui.Graphics.Platform;
                
                public void Draw(ICanvas canvas, RectF dirtyRect)
                {{
                {0}
                }}", code);

            var compilationResult = await _compiler.CompileAsync(sourceCode);

            return compilationResult;
        }

        async Task Compile()
        {
            try
            {
                IsGenerating = true;
                _selectedPage.CompilationResult = await CompileCodeAsync(_selectedPage.Code);
                Recompiled?.Invoke();
                RedrawRequested?.Invoke();
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                IsGenerating = false;
            }
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