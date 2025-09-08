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
                propertyChanged: (b, o, n) => ((ExplorerTreeView)b).OnItemsSourceChanged((IEnumerable<FlatNode>)n));

        public IEnumerable<FlatNode> ItemsSource
        {
            get => (IEnumerable<FlatNode>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        
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
            // start with empty content
            Content = new ScrollView
            {
                Content = new VerticalStackLayout { Spacing = 0 }
            };
        }

        void OnItemsSourceChanged(IEnumerable<FlatNode> flat)
        {
            var rootStack = new VerticalStackLayout { Spacing = 0 };
            if (flat == null)
            {
                ((ScrollView)Content).Content = rootStack;
                return;
            }

            var nodes = BuildHierarchy(flat);
            // render
            foreach (var r in nodes)
                RenderNode(r, rootStack);

            ((ScrollView)Content).Content = rootStack;
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
                    n.Children.Sort((a, b) => string.Compare(a.Model.Name, b.Model.Name, StringComparison.OrdinalIgnoreCase));
                    SortRecursively(n.Children);
                }
            }
            SortRecursively(roots);

            return roots;
        }

        void RenderNode(TreeNode node, Layout parentLayout)
        {
            var depth = Math.Max(0, node.Model.Depth);

            // container for one row
            var row = new Grid
            {
                Padding = new Thickness(8, 6, 8, 6),
                HeightRequest = 40,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // toggle
                    new ColumnDefinition { Width = GridLength.Auto }, // icon (optional)
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
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
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
                // spacer
                var spacer = new BoxView { WidthRequest = 28, HeightRequest = 1, Opacity = 0 };
                row.Add(spacer, 0, 0);
            }

            // optional icon - file/folder (simple text)
            var iconLabel = new Label
            {
                Text = node.Children.Any() ? "📁" : "📄",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 14
            };
            row.Add(iconLabel, 1, 0);

            // label with tap gesture
            var lbl = new Label
            {
                Text = node.Model.Name,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            };
            row.Add(lbl, 2, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                SelectedNode = node.Model;
                RowTappedCommand?.Execute(SelectedNode.Tag as NodeModel);
                NodeTapped?.Invoke(this, node.Model);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    var original = row.BackgroundColor;
                    row.BackgroundColor = Colors.Red;
                    await Task.Delay(100);
                    row.BackgroundColor = original;
                });
            };
            row.GestureRecognizers.Add(tap);

            parentLayout.Children.Add(row);

            VerticalStackLayout childrenContainer = null;
            if (node.Children.Any())
            {
                childrenContainer = new VerticalStackLayout { Spacing = 0,
                    IsVisible = false };
                parentLayout.Children.Add(childrenContainer);

                // toggling behavior
                toggle.Clicked += (s, e) =>
                {
                    var now = !childrenContainer.IsVisible;
                    childrenContainer.IsVisible = now;
                    toggle.Text = now ? "▼" : "▶";

                    // optional lazy render: if first time visible and container empty -> render children
                    if (now && childrenContainer.Children.Count == 0)
                    {
                        foreach (var child in node.Children)
                            RenderNode(child, childrenContainer);
                    }
                };

                // we don't render children until expanded (lazy) - good for big trees
            }
        }

        // Public helper: expand all (caveat: will force rendering)
        public void ExpandAll()
        {
            if (Content is ScrollView sc && sc.Content is Layout root)
                ExpandCollapseRec(root, true);
        }
        
        public void CollapseAll()
        {
            if (Content is ScrollView sc && sc.Content is Layout root)
                ExpandCollapseRec(root, false);
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
