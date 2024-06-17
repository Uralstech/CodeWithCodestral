using CodeWithCodestral.ViewModels;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;

namespace CodeWithCodestral
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            
            builder.Services.AddSingleton(FileSaver.Default);
            builder.Services.AddSingleton(Connectivity.Current);
            builder.Services.AddSingleton(FileSystem.Current);
            builder.Services.AddSingleton(FilePicker.Default);

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();

            return builder.Build();
        }
    }
}
