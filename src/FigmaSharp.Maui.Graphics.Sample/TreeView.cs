using System.Collections.Specialized;
using System.Windows.Input;
using FigmaSharp.Maui.Graphics.Sample.ViewModels;
using Grid = Microsoft.Maui.Controls.Grid;
using Layout = Microsoft.Maui.Controls.Layout;

namespace FigmaSharp.Maui.Graphics.Sample
{
    public class FlatNode
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
        public int Depth { get; set; }
        public object Tag { get; set; }
    }

    public class TreeNode
    {
        public FlatNode Model { get; set; }
        public List<TreeNode> Children { get; } = new List<TreeNode>();
    }

    public class ExplorerTreeView : ContentView
    {
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable<FlatNode>),
                typeof(ExplorerTreeView),
                propertyChanged: (b, o, n) =>
                    ((ExplorerTreeView)b).OnItemsSourceChanged((IEnumerable<FlatNode>)n));

        public IEnumerable<FlatNode> ItemsSource
        {
            get => (IEnumerable<FlatNode>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private INotifyCollectionChanged _observable;
        private VerticalStackLayout _rootStack;

        public static readonly BindableProperty RowTappedCommandProperty =
            BindableProperty.Create(
                nameof(RowTappedCommand),
                typeof(ICommand),
                typeof(ExplorerTreeView));

        public ICommand RowTappedCommand
        {
            get => (ICommand)GetValue(RowTappedCommandProperty);
            set => SetValue(RowTappedCommandProperty, value);
        }

        public event EventHandler<FlatNode> NodeTapped;

        public FlatNode SelectedNode { get; private set; }

        public ExplorerTreeView()
        {
            _rootStack = new VerticalStackLayout { Spacing = 0 };
            Content = new ScrollView { Content = _rootStack };
        }

        void OnItemsSourceChanged(IEnumerable<FlatNode> flat)
        {
            if (_observable != null)
                _observable.CollectionChanged -= OnCollectionChanged;

            _observable = flat as INotifyCollectionChanged;

            if (_observable != null)
                _observable.CollectionChanged += OnCollectionChanged;

            RebuildTree(flat);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (FlatNode fn in e.NewItems)
                    AddNodeToTree(fn);
            }
            else
            {
                // reset, remove itp. -> prościej przebudować
                RebuildTree(ItemsSource);
            }
        }

        void RebuildTree(IEnumerable<FlatNode> flat)
        {
            _rootStack.Children.Clear();

            if (flat == null) return;

            var nodes = BuildHierarchy(flat);
            foreach (var r in nodes)
                RenderNode(r, _rootStack);
        }

        List<TreeNode> BuildHierarchy(IEnumerable<FlatNode> flat)
        {
            var dict = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in flat)
            {
                if (string.IsNullOrEmpty(f.Id)) continue;
                if (!dict.ContainsKey(f.Id))
                    dict[f.Id] = new TreeNode { Model = f };
                else
                    dict[f.Id].Model = f;
            }

            var roots = new List<TreeNode>();

            foreach (var pair in dict.Values)
            {
                var parentId = pair.Model.ParentId;
                if (!string.IsNullOrEmpty(parentId) && dict.TryGetValue(parentId, out var parent))
                {
                    parent.Children.Add(pair);
                }
                else
                {
                    roots.Add(pair);
                }
            }

            void SortRecursively(IEnumerable<TreeNode> list)
            {
                foreach (var n in list)
                {
                    n.Children.Sort((a, b) =>
                        string.Compare(a.Model.Name, b.Model.Name, StringComparison.OrdinalIgnoreCase));
                    SortRecursively(n.Children);
                }
            }
            SortRecursively(roots);

            return roots;
        }

        void RenderNode(TreeNode node, Layout parentLayout)
        {
            var depth = Math.Max(0, node.Model.Depth);

            var row = new Grid
            {
                Padding = new Thickness(8, 6, 8, 6),
                HeightRequest = 40,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // toggle
                    new ColumnDefinition { Width = GridLength.Auto }, // icon
                    new ColumnDefinition { Width = GridLength.Star }  // label
                },
                Margin = new Thickness(depth * 16, 0, 0, 0)
            };

            Button toggle = null;
            if (node.Children.Any())
            {
                toggle = new Button
                {
                    Text = "▶",
                    FontSize = 30,
                    WidthRequest = 28,
                    HeightRequest = 28,
                    Padding = 0,
                    BorderColor = Colors.Transparent,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };
                row.Add(toggle, 0, 0);
            }
            else
            {
                row.Add(new BoxView { WidthRequest = 28, Opacity = 0 }, 0, 0);
            }

            var iconLabel = new Label
            {
                Text = node.Children.Any() ? "📁" : "📄",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 14
            };
            row.Add(iconLabel, 1, 0);

            var lbl = new Label
            {
                Text = node.Model.Name,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            };
            row.Add(lbl, 2, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                SelectedNode = node.Model;
                RowTappedCommand?.Execute(SelectedNode.Tag as NodeModel);
                NodeTapped?.Invoke(this, node.Model);

                var original = row.BackgroundColor;
                row.BackgroundColor = Colors.Red;
                await Task.Delay(100);
                row.BackgroundColor = original;
            };
            row.GestureRecognizers.Add(tap);

            parentLayout.Children.Add(row);

            if (node.Children.Any())
            {
                var childrenContainer = new VerticalStackLayout { Spacing = 0, IsVisible = false };
                parentLayout.Children.Add(childrenContainer);

                toggle.Clicked += (s, e) =>
                {
                    var now = !childrenContainer.IsVisible;
                    childrenContainer.IsVisible = now;
                    toggle.Text = now ? "▼" : "▶";

                    if (now && childrenContainer.Children.Count == 0)
                    {
                        foreach (var child in node.Children)
                            RenderNode(child, childrenContainer);
                    }
                };
            }
        }

        void AddNodeToTree(FlatNode fn)
        {
            // Na razie najprościej: rebuild całego drzewa
            // Można zoptymalizować i szukać konkretnego rodzica
            RebuildTree(ItemsSource);
        }

        public void ExpandAll()
        {
            if (_rootStack != null)
                ExpandCollapseRec(_rootStack, true);
        }

        public void CollapseAll()
        {
            if (_rootStack != null)
                ExpandCollapseRec(_rootStack, false);
        }

        void ExpandCollapseRec(Layout layout, bool expand)
        {
            foreach (var child in layout.Children)
            {
                if (child is VerticalStackLayout sl && sl.Children.Count > 0)
                {
                    sl.IsVisible = expand;
                    ExpandCollapseRec(sl, expand);
                }
                else if (child is Layout nested)
                {
                    ExpandCollapseRec(nested, expand);
                }
            }
        }
    }
}
