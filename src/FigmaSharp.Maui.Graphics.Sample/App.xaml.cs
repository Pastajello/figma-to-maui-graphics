namespace FigmaSharp.Maui.Graphics.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //var token = "INSERT HERE YOUR FIGMA PERSONAL ACCESS TOKEN";
            //FigmaApplication.Init(token);

            MainPage = new AppShell();
        }
    }
}