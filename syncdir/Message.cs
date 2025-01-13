namespace SigTec.NetFx48DevCliTools.SyncDir
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Reflection;

  internal static class Message
  {
    private static readonly Process _process = Process.GetCurrentProcess();
    private static readonly Assembly _assembly = typeof(Message).Assembly;

    internal static void CopyFileWithDifferentSize(FileInfo sourceFile)
      => WriteLine(ConsoleColor.Blue, $"copy differnt {sourceFile.FullName}");

    internal static void CopyNewerFile(FileInfo sourceFile)
      => WriteLine(ConsoleColor.Blue, $"copy newer {sourceFile.FullName}");

    internal static void CopyNewFile(FileInfo sourceFile)
      => WriteLine(ConsoleColor.Green, $"copy new {sourceFile.FullName}");

    internal static void CreateDirectory(DirectoryInfo targetDir)
      => WriteLine(ConsoleColor.Green, $"create {targetDir.FullName}");

    internal static void FileIsNewer(FileInfo sourceFile, FileInfo targetFile)
      => WriteLine(ConsoleColor.Blue, $"files {sourceFile.FullName} is newer than {targetFile.FullName}");

    internal static void FileIsOlder(FileInfo sourceFile, FileInfo targetFile)
      => WriteLine(ConsoleColor.Yellow, $"files {sourceFile.FullName} is older than {targetFile.FullName}");

    internal static void FileSizeIsDifferent(FileInfo sourceFile, FileInfo targetFile)
      => WriteLine(ConsoleColor.Yellow, $"files are of same age, but size is different: {sourceFile.FullName} {sourceFile.Length} bytes, {targetFile.FullName} {targetFile.Length} bytes");

    internal static void FileStatistics(int newFileCount, int replacedFileCount, int skippedFileCount, int unchangedFileCount, bool preview)
      => WriteLine(ConsoleColor.Cyan, preview ?
        $"{newFileCount} additional files, {replacedFileCount} newer files, {skippedFileCount} older files, {unchangedFileCount} files equal" :
        $"{newFileCount} files added (new), {replacedFileCount} files replaced (newer), {skippedFileCount} files skipped (older), {unchangedFileCount} files unchanged (same).");

    internal static void ProgrammInfo()
      => WriteLine(ConsoleColor.Cyan, $"{_process.ProcessName} Version {_assembly.GetName().Version} {_assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}");

    internal static void SettingsFileAlreadyExist(string filename) 
      => WriteLine(ConsoleColor.Red, $"settings file already exists: {filename}");

    internal static void SettingsFileDoesNotExist(string filename)
      => WriteLine(ConsoleColor.Red, $"settings file not found: {filename}");

    internal static void SkipOlderFile(FileInfo sourceFile)
      => WriteLine(ConsoleColor.Yellow, $"skip older {sourceFile.FullName}");

    internal static void SourceDirDoesNotExist(DirectoryInfo sourceDir)
      => WriteLine(ConsoleColor.Red, $"source dir not found: {sourceDir.FullName}");

    internal static void Syncing(DirectoryInfo sourceDir, DirectoryInfo targetDir)
      => WriteLine(ConsoleColor.Cyan, $"sync {sourceDir.FullName} to {targetDir.FullName}");

    internal static void TargetDirDoesNotExist(DirectoryInfo sourceDir)
      => WriteLine(ConsoleColor.Green, $"corresponding target dir not found: {sourceDir.FullName}");

    internal static void TargetFileDoesNotExist(FileInfo sourceFile, DirectoryInfo targetDir)
      => WriteLine(ConsoleColor.Green, $"corresponding target file not found: {sourceFile.FullName}");

    internal static void UnexpectedError(Exception ex)
      => WriteLine(ConsoleColor.Red, $"an unexpected error occured: {ex.GetType().Name} {ex.Message}");

    internal static void UsageHint()
    {
      var ussageHint = $@"usage: {_process.ProcessName} <cmd> [<options>]
where <cmd> is:
  view          show differences between local and remote directories
  pull          pulls changes from remote to local directory
  push          pushes changes from local to remote directory
  new           initializes a new syncdir.settings.json in the current directory
                (or the given filename using the -s otion)

the local and remove directory are taken from file ""syncdir.settings.json"" in the current directory
(if not specified otherwise using the -s otion).

optional <options> are:
  -s <filename> use the settings file provided instead of the default ""syncdir.settings.json""
";
      Console.WriteLine(ussageHint);
      var projectUrl = _assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(a => a.Key.Equals("ProjectUrl", StringComparison.InvariantCultureIgnoreCase))
        .Select(a => a.Value)
        .FirstOrDefault();
      if(!string.IsNullOrEmpty(projectUrl))
      {
        Console.WriteLine($"visit {projectUrl} to learn more about this tool.");
      }
    }

    internal static void View(DirectoryInfo sourceDir, DirectoryInfo targetDir)
      => WriteLine(ConsoleColor.Cyan, $"compare {sourceDir.FullName} to {targetDir.FullName}");

    private static void WriteLine(ConsoleColor color, string message)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(message);
      Console.ForegroundColor = oldColor;
    }
  }
}