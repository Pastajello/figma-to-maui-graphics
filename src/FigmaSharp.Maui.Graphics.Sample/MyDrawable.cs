using FigmaSharp.Maui.Graphics.Sample.ViewModels;

namespace FigmaSharp.Maui.Graphics.Sample;

public class MyDrawable : IDrawable
{
    private readonly MainViewModel _vm;

    public MyDrawable(MainViewModel vm)
    {
        _vm = vm;
        _vm.RedrawRequested += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _graphicsView?.Invalidate();
            });
        };
        _vm.Recompiled += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _vm.SelectedNodeModel.CompilationResult?.Clean();
            });
        };
    }

    private GraphicsView? _graphicsView;
    public void SetGraphicsView(GraphicsView view)
    {
        _graphicsView = view;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Translate(_vm.OffsetX, _vm.OffsetY);
        canvas.Scale(_vm.Scale,_vm.Scale);
        _vm.SelectedNodeModel.CompilationResult?.Draw(canvas, dirtyRect);
    }
}