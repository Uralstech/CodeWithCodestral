using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;

namespace CodeWithCodestral.SaveSystem;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class JsonAppData
{
    public string MistralApiKey = string.Empty;
    public bool AllowAiToOverrideCode;
    public string LastOpenFile = string.Empty;
}

public static class JsonSaveSystem
{
    private const string DataFile = "AppData.json";

    public static JsonAppData? JsonAppData { get; private set; } = null;

    public static void ResetJsonAppData()
    {
        JsonAppData ??= new();
    }

    public static async Task<bool> SaveJsonData(IFileSystem fileSystem)
    {
        if (JsonAppData is null)
            return false;

        string path = Path.Combine(fileSystem.AppDataDirectory, DataFile);
        try
        {
            await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(JsonAppData));
            return true;
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Could not save file {exception.Message}");
            return false;
        }
    }

    public static async Task<bool> ReadJsonData(IFileSystem fileSystem)
    {
        string path = Path.Combine(fileSystem.AppDataDirectory, DataFile);
        try
        {
            string jsonData = await File.ReadAllTextAsync(path);

            JsonAppData = JsonConvert.DeserializeObject<JsonAppData>(jsonData);
            return JsonAppData is not null;
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Could not save file {exception.Message}");
            return false;
        }
    }
}
