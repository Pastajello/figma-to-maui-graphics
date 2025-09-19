using FigmaSharp.Maui.Graphics.Sample.ViewModels;

namespace FigmaSharp.Maui.Graphics.Sample
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel VM;

        public MainPage()
        {
            InitializeComponent();
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
    }
}