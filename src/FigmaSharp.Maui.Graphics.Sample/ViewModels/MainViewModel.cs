using FigmaSharp.Maui.Graphics.Sample.PropertyConfigure;
using FigmaSharp.Maui.Graphics.Sample.Services;
using FigmaSharp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FigmaSharp.Models;
using UIKit;

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

    public partial class NodeModel : ObservableObject
    {
        [ObservableProperty] private string _name;
        [ObservableProperty] private string _code;
        [ObservableProperty] private CompilationResult _compilationResult;
        [ObservableProperty] private bool _isSelected;
        public bool IsLoaded;
        [ObservableProperty] private FigmaNode _node;
    }

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private string _token;
        [ObservableProperty] private string _fileId;
        [ObservableProperty] private string _code;
        [ObservableProperty] private bool _isGenerating;
        [ObservableProperty] private ObservableCollection<string> _log;
        [ObservableProperty] private ObservableCollection<FigmaPage> _pages;
        [ObservableProperty] private NodeModel? _selectedNodeModel;
        [ObservableProperty] private float _scale = 1;
        [ObservableProperty] private ObservableCollection<FlatNode> _treeNodes = new();
        private readonly HashSet<string> _knownIds = new();

        private readonly Compiler _compiler;
        private RemoteNodeProvider _remoteNodeProvider;
        private CodeRenderService _codeRenderer;

        public MainViewModel()
        {
            Drawable = new MyDrawable(this);
            DrawableSet?.Invoke();
#if DEBUG
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
        public event Action Recompiled;


        public ICommand GenerateCommand => new Command(async () => await GenerateCodeAsync());
        public ICommand ExportCommand => new Command(async () => await Export());
        public ICommand CompileCommand => new Command(async () => await Compile());
        public ICommand TapNodeCommand => new AsyncRelayCommand<NodeModel>(OnTapNodeCommand);

        partial void OnScaleChanged(float value)
        {
            RedrawRequested?.Invoke();
        }

        async Task GenerateCodeAsync()
        {
            DrawableSet?.Invoke();
            try
            {
                TreeNodes.Clear();
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

                var treeNodes = new List<FlatNode>();
                foreach (var page in Pages)
                {
                    treeNodes.AddRange(MapFigmaNodes(new List<FigmaNode>()
                    {
                        page.Node
                    }));
                }

                _knownIds.Clear();

                foreach (var fn in treeNodes)
                {
                    if (_knownIds.Add(fn.Id))
                    {
                    }
                }

                TreeNodes = new ObservableCollection<FlatNode>(treeNodes);

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


        async Task OnTapNodeCommand(NodeModel nodeModel)
        {
            try
            {
                Console.WriteLine($"------------------------------------");
                Console.WriteLine($"Start operation for node: {nodeModel?.Node?.name}");
                var watch = new Stopwatch();
                watch.Start();
                if (SelectedNodeModel != null)
                {
                    SelectedNodeModel.IsSelected = false;
                }

                SelectedNodeModel = null;

                if (!nodeModel.IsLoaded)
                {
                    IsGenerating = true;

                    var nodess = await _remoteNodeProvider.LoadAsync(FileId, nodeModel.Node.id, nodeModel?.Node, 3);
                    Console.WriteLine($"after load");

                    var nodes = nodeModel.Node.GetNodes();

                    var newNodes = MapFigmaNodes(nodes);

                    var treeNodes = new List<FlatNode>(TreeNodes);
                    foreach (var fn in newNodes)
                    {
                        if (_knownIds.Add(fn.Id))
                        {
                            treeNodes.Add(fn);
                        }
                    }

                    TreeNodes = new ObservableCollection<FlatNode>(treeNodes);
                    nodeModel.IsLoaded = true;
                }

                await GeneratePageSourceCode(nodeModel);

                SelectedNodeModel = nodeModel;
                SelectedNodeModel.IsSelected = true;
                watch.Stop();
                var millis = watch.ElapsedMilliseconds;
                Console.WriteLine($"All time in method: {millis} ms");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                IsGenerating = false;
                RedrawRequested?.Invoke();
            }
        }

        IEnumerable<FlatNode> MapFigmaNodes(IEnumerable<FigmaNode> figmaNodes)
        {
            if (figmaNodes == null || !figmaNodes.Any())
                return Enumerable.Empty<FlatNode>();

            var nodes = new List<FlatNode>();
            foreach (var node in figmaNodes)
            {
                if (node != null)
                {
                    nodes.Add(new FlatNode
                    {
                        Id = node.id,
                        ParentId = node.Parent?.id,
                        Name = string.IsNullOrEmpty(node.name) ? $"Node {node.id}" : node.name,
                        Depth = node.ComputeDepth(),
                        Tag = new NodeModel { Node = node }
                    });
                }
            }

            var groupedNodes = nodes
                .GroupBy(f => f.Id)
                .Select(g => g.First());

            return groupedNodes;
        }

        private async Task GeneratePageSourceCode(NodeModel nodeModel)
        {
            var stringBuilder = new StringBuilder();
            Log.Add($"Node {nodeModel.Node.id} found successfully.");

            Log.Add($"Generating source code for page {nodeModel.Node.name}...");

            var codeNode = new CodeNode(nodeModel.Node);

            _codeRenderer.GetCode(stringBuilder, codeNode);

            var code = stringBuilder.ToString();

            nodeModel.Code = code;
            nodeModel.CompilationResult = await CompileCodeAsync(code);

            Log.Add($"Source Code for page {nodeModel.Node.name} generated successfully.");
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
                SelectedNodeModel.CompilationResult = await CompileCodeAsync(_selectedNodeModel.Code);
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