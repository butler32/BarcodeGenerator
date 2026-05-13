using Avalonia.Platform.Storage;

namespace BarcodeGenerator.Avalonia.Services;

public interface IDialogService
{
    Task<string?> SaveFileAsync(string suggestedName);
    Task<string?> SaveFolderAsync(string title = "Select output folder");
}

public sealed class DialogService : IDialogService
{
    public global::Avalonia.Controls.Window? MainWindow { get; set; }

    public async Task<string?> SaveFileAsync(string suggestedName)
    {
        if (MainWindow is null) return null;

        var topLevel = global::Avalonia.Controls.TopLevel.GetTopLevel(MainWindow);
        if (topLevel is null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PNG",
            SuggestedFileName = suggestedName,
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }
            ]
        });

        return file?.Path.LocalPath;
    }

    public async Task<string?> SaveFolderAsync(string title = "Select output folder")
    {
        if (MainWindow is null) return null;

        var topLevel = global::Avalonia.Controls.TopLevel.GetTopLevel(MainWindow);
        if (topLevel is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
