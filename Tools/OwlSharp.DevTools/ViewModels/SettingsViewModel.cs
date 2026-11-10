using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OwlSharp.Core;
using Avalonia.Platform.Storage;

namespace OwlSharp.DevTools.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private static readonly string _tkConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "totk", "Config.json");

    private static readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OwlSharp.DevTools", "Config.json");
    
    public static readonly SettingsPageViewModel Shared = Load();
    
    [ObservableProperty]
    private string? _gameDumpFolderPath;
    
    public static SettingsPageViewModel Load()
    {
        SettingsPageViewModel? result;

        if (!File.Exists(_configFilePath)) {
            result = new SettingsPageViewModel();
            goto FetchTkConfig;
        }

        using (FileStream fs = File.OpenRead(_configFilePath)) {
            result = JsonSerializer.Deserialize<SettingsPageViewModel>(fs)
                     ?? new SettingsPageViewModel();
        }

    FetchTkConfig:
        if (!File.Exists(_tkConfigPath)) {
            return result;
        }

        using FileStream tkConfigFs = File.OpenRead(_tkConfigPath);
        if (JsonSerializer.Deserialize<TkConfig>(tkConfigFs) is not TkConfig tkConfig) {
            return result;
        }

        result.GameDumpFolderPath = tkConfig.GamePath;
        return result;
    }

    [RelayCommand]
    [property: JsonIgnore]
    private void Save()
    {
        if (Path.GetDirectoryName(_configFilePath) is string folder) {
            Directory.CreateDirectory(folder);
        }

        using FileStream fs = File.Create(_configFilePath);
        JsonSerializer.Serialize(fs, this);
    }

    private record TkConfig(string GamePath);
}