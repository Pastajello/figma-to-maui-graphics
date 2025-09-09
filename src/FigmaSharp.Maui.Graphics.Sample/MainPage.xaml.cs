using FigmaSharp.Maui.Graphics.Sample.ViewModels;
using FigmaSharp.Models;

namespace FigmaSharp.Maui.Graphics.Sample
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel VM;

        public MainPage()
        {
            InitializeComponent();
            tree.NodeTapped += (s, node) =>
            {
                // node.Tag zawiera oryginalny FigmaNode — zrób co chcesz
                var figma = node.Tag as FigmaNode;
                // np. open details / go to page / highlight
            };

            // załóżmy, że masz figmaNodes od Figma API:
         
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            if (BindingContext is MainViewModel vm)
            {
                VM = vm;
                vm.DrawableSet -= VmOnDrawableSet;
                vm.DrawableSet += VmOnDrawableSet;
              
            }
        }
        
        

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            if (BindingContext is MainViewModel vm)
            {
                vm.DrawableSet -= VmOnDrawableSet;
            }
        }

        private void VmOnDrawableSet()
        {
            if (VM.Drawable is MyDrawable drawable)
            {
                drawable.SetGraphicsView(graphicsView);
            }
        }
        
     
// mapowanie
     

        private Point _lastPan;

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _lastPan = new Point((float)e.TotalX, (float)e.TotalY);
                    break;

                case GestureStatus.Running:
                    float dx = (float)(e.TotalX - _lastPan.X);
                    float dy = (float)(e.TotalY - _lastPan.Y);

                    VM.OffsetX += dx;
                    VM.OffsetY += dy;

                    _lastPan = new Point((float)e.TotalX, (float)e.TotalY);

                    graphicsView.Invalidate();
                    break;
            }
        }

        private double _startScale = 1;
        private float _startOffsetX;
        private float _startOffsetY;

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                _startScale = VM.Scale;
                _startOffsetX = VM.OffsetX;
                _startOffsetY = VM.OffsetY;
            }
            else if (e.Status == GestureStatus.Running)
            {
#if MACCATALYST
                // Some weird bug, there's no Started state when pinching from touchbar in macos?
                // and it only catches once, weird
                _startScale = VM.Scale;
                _startOffsetX = VM.OffsetX;
                _startOffsetY = VM.OffsetY;
#endif
                double scale = _startScale * e.Scale;

                scale = Math.Clamp(scale, 0.1, 10.0);

                float centerX = (float)(e.ScaleOrigin.X * graphicsView.Width);
                float centerY = (float)(e.ScaleOrigin.Y * graphicsView.Height);

                VM.OffsetX = _startOffsetX + centerX * (float)(1 - e.Scale);
                VM.OffsetY = _startOffsetY + centerY * (float)(1 - e.Scale);

                VM.Scale = (float)scale;
                graphicsView.Invalidate();
            }
        }
    }
}