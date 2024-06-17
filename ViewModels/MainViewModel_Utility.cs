using CodeWithCodestral.SaveSystem;
using System.Diagnostics;

namespace CodeWithCodestral.ViewModels;

public partial class MainViewModel
{
    private static string GetCodeBlock(string text)
    {
        (int start, int end) = (-1, -1);

        bool wasBacktick = false;
        for (int i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '`':
                    if (start > -1)
                    {
                        end = i;
                        break;
                    }

                    if (text[Math.Max(0, i - 2)..Math.Min(text.Length - 1, i + 1)] == "```")
                        wasBacktick = true;
                    break;
                case '\n':
                    if (wasBacktick)
                    {
                        start = Math.Min(text.Length - 1, i + 1);
                        wasBacktick = false;
                    }

                    break;

                default: break;
            }

            if (end > -1)
                break;
        }

        return text[start..end];
    }

    public async Task RunSetupProcedures()
    {
        if (await JsonSaveSystem.ReadJsonData(_fileSystem))
        {
            MistralApi.MistralApi.ApiKey = JsonSaveSystem.JsonAppData!.MistralApiKey;
            if (!string.IsNullOrEmpty(JsonSaveSystem.JsonAppData.LastOpenFile))
                LoadFile(JsonSaveSystem.JsonAppData.LastOpenFile);

            return;
        }

        await CheckAndGetMistralApiKey(true);

        bool allowCodeOverwrite =
            await Shell.Current.DisplayAlert("Codestral Code Help", "Do you want to allow Codestral Code Help to overwrite any open code file to improve it?", "Yes", "No");

        JsonSaveSystem.ResetJsonAppData();
        JsonSaveSystem.JsonAppData!.MistralApiKey = MistralApi.MistralApi.ApiKey;
        JsonSaveSystem.JsonAppData!.AllowAiToOverrideCode = allowCodeOverwrite;

        if (!await JsonSaveSystem.SaveJsonData(_fileSystem))
            await Shell.Current.DisplayAlert("Failed Save", "For some reason, the app's save data could not be saved.", "Ok");
    }

    private async Task<bool> CheckAndGetMistralApiKey(bool isSaving=false)
    {
        if (string.IsNullOrWhiteSpace(MistralApi.MistralApi.ApiKey))
        {
            string key = await Shell.Current.DisplayPromptAsync("Mistral API Key", "Please enter your Mistral API key. You will not be able to use Codestral Code Help if you do not enter your API key.");
            if (string.IsNullOrWhiteSpace(key))
                return false;

            MistralApi.MistralApi.ApiKey = key;

            if (isSaving)
                return true;

            JsonSaveSystem.ResetJsonAppData();
            JsonSaveSystem.JsonAppData!.MistralApiKey = key;

            await JsonSaveSystem.SaveJsonData(_fileSystem);
        }

        return true;
    }

    private async void LoadFile(string filePath, string? fallbackPath = null)
    {
        try
        {
            string data = await File.ReadAllTextAsync(filePath);
            CodeText = data;
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Could not read file {filePath}: {exception.Message}");

            await Shell.Current.DisplayAlert("File Read Failed", $"Could not read file: \"{filePath}\".", "Ok");

            if (JsonSaveSystem.JsonAppData is not null)
                JsonSaveSystem.JsonAppData.LastOpenFile = fallbackPath ?? string.Empty;
            
            await JsonSaveSystem.SaveJsonData(_fileSystem);
        }
    }

    private async void SaveFile(string filePath)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, CodeText);
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Could not save file {filePath}: {exception.Message}");

            await Shell.Current.DisplayAlert("File Save Failed", $"Could not save file: \"{filePath}\".", "Ok");
        }
    }
}
