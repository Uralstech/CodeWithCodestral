using CodeWithCodestral.MistralApi;
using CodeWithCodestral.SaveSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeWithCodestral.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string UserCodeMessageFormat = "This is my code:\n{0}";

    private static MainPage CurrentPage => (MainPage)AppShell.Current.CurrentPage;

    [ObservableProperty]
    private string _codeText;

    [ObservableProperty]
    private string _chatPrompt;

    private readonly IConnectivity _connectivity;
    private readonly IFileSystem _fileSystem;
    private readonly IFilePicker _filePicker;

    private readonly MistralChatCompletionRequest _chatCompletionRequest;

    public MainViewModel(IConnectivity connectivity, IFilePicker filePicker, IFileSystem fileSystem)
    {
        _connectivity = connectivity;
        _filePicker = filePicker;
        _fileSystem = fileSystem;

        _chatCompletionRequest = new()
        {
            Messages =
            {
                new MistralChatMessage
                {
                    Role = MistralChatMessageRole.System,
                    Content = "You are Codestral, a code wizard who helps users improve their code by advising them on best practices, " +
                    "improving code efficiency and/or readability based on their preferences and also by providing general assistance " +
                    "to the them. Any code you provide will replace the users code, so be mindful."
                },
            }
        };
    }

    [RelayCommand]
    private async Task OnSendPrompt()
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert("Internet Connection", "You need to be connected to the internet to be able to access Codestral Code Help.", "Ok");
            return;
        }

        if (!await CheckAndGetMistralApiKey() || string.IsNullOrWhiteSpace(ChatPrompt))
            return;

        MainPage mainPage = CurrentPage;

        MistralChatMessage userMessage = new()
        {
            Role = MistralChatMessageRole.User,
            Content = ChatPrompt,
        };

        mainPage.AddMessage(userMessage);
        _chatCompletionRequest.Messages.Add(userMessage);

        ChatPrompt = string.Empty;
        _chatCompletionRequest.Messages.Add(new MistralChatMessage
        {
            Role = MistralChatMessageRole.User,
            Content = string.Format(UserCodeMessageFormat, CodeText)
        });

        MistralChatMessage? response = MistralApi.MistralApi.ExtractChatResponse(await MistralApi.MistralApi.ChatCompletion(_chatCompletionRequest));
        if (response is null)
        {
            await Shell.Current.DisplayAlert("Oh No!", "Something went wrong! The Mistral API failed to return a response.", "Ok");
            return;
        }

        _chatCompletionRequest.Messages.Add(response);
        if (response.Content.Contains("```"))
        {
            string rewrite = response.Content;
                (int start, int end) = (-1, -1);

                bool wasBacktick = false;
                for (int i = 0; i < rewrite.Length; i++)
                {
                    switch (rewrite[i])
                    {
                        case '`':
                            if (start > -1)
                            {
                                end = i;
                                break;
                            }

                            wasBacktick = true;
                            break;
                        case '\n':
                            if (wasBacktick)
                            {
                                start = Math.Min(rewrite.Length - 1, i + 1);
                                wasBacktick = false;
                            }
                            break;
                        default: break;
                    }

                    if (end > -1)
                        break;
                }

                rewrite = rewrite[start..end];

            if (JsonSaveSystem.JsonAppData?.AllowAiToOverrideCode == false)
            {
                if (await Shell.Current.DisplayAlert("Codestral Code Help", $"Codestral wants to rewrite your code:\n\n{rewrite}", "Allow", "Cancel"))
                    CodeText = rewrite;
            }
            else
                CodeText = rewrite;
        }

        mainPage.AddMessage(response);
    }

    [RelayCommand]
    private async Task OnSave(bool saveAs)
    {
        if (string.IsNullOrEmpty(JsonSaveSystem.JsonAppData?.LastOpenFile) || saveAs)
        {
            FileResult? result = await _filePicker.PickAsync(new PickOptions()
            {
                PickerTitle = "Save the file.",
            });

            if (result is null)
                return;

            JsonSaveSystem.ResetJsonAppData();
            JsonSaveSystem.JsonAppData!.LastOpenFile = result.FullPath;

            await JsonSaveSystem.SaveJsonData(_fileSystem);
        }

        SaveFile(JsonSaveSystem.JsonAppData.LastOpenFile);
    }

    [RelayCommand]
    private async Task OnNew()
    {
        if (!string.IsNullOrEmpty(JsonSaveSystem.JsonAppData?.LastOpenFile) || !string.IsNullOrWhiteSpace(CodeText))
        {
            if (!await Shell.Current.DisplayAlert("Discard unsaved data?", "Do you want to discard unsaved data?", "Yes", "No"))
                return;
        }

        JsonSaveSystem.ResetJsonAppData();
        JsonSaveSystem.JsonAppData!.LastOpenFile = string.Empty;

        await JsonSaveSystem.SaveJsonData(_fileSystem);
        CodeText = string.Empty;
    }

    [RelayCommand]
    private async Task OnOpen()
    {
        if (!string.IsNullOrEmpty(JsonSaveSystem.JsonAppData?.LastOpenFile) || !string.IsNullOrWhiteSpace(CodeText))
        {
            if (!await Shell.Current.DisplayAlert("Discard unsaved data?", "Do you want to discard unsaved data?", "Yes", "No"))
                return;
        }

        FileResult? result = await _filePicker.PickAsync(new PickOptions()
        {
            PickerTitle = "Choose the file to open.",
        });

        if (result is null)
            return;

        JsonSaveSystem.ResetJsonAppData();
        string fallbackPath = JsonSaveSystem.JsonAppData!.LastOpenFile;

        JsonSaveSystem.JsonAppData.LastOpenFile = result.FullPath;
        await JsonSaveSystem.SaveJsonData(_fileSystem);

        LoadFile(JsonSaveSystem.JsonAppData.LastOpenFile, fallbackPath);
    }
}