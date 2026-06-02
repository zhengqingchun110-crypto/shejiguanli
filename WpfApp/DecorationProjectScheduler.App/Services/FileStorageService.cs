using System.IO;

namespace DecorationProjectScheduler.App.Services;

public sealed class FileStorageService
{
    private readonly string _rootDirectory;

    public FileStorageService(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(_rootDirectory);
    }

    public string RootDirectory => _rootDirectory;

    public string SaveProjectFile(string projectCode, string category, string sourcePath)
    {
        var categoryDirectory = Path.Combine(_rootDirectory, projectCode, category);
        Directory.CreateDirectory(categoryDirectory);

        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(categoryDirectory, fileName);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return destinationPath;
    }
}
