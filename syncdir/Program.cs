namespace SigTec.NetFx48DevCliTools.SyncDir
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;

  internal class Program
  {
    private static int _newFileCount = 0;
    private static int _replacedFileCount = 0;
    private static int _skippedFileCount = 0;
    private static int _unchangedFileCount = 0;

    private static void Main(string[] args)
    {
      try
      {
        Message.ProgrammInfo();

        if (args.Length == 0)
        {
          Message.UsageHint();
          return;
        }

        var cmd = args[0].ToLowerInvariant();
        var settingsFileName = Path.Combine(Environment.CurrentDirectory, "syncdir.settings.json");
        if (args.Length == 3 && args[1] == "-s")
        {
          settingsFileName = args[2];
        }

        if (cmd == "new")
        {
          CreateSettingsFile(settingsFileName);
          return;
        }

        if (!File.Exists(settingsFileName))
        {
          Message.SettingsFileDoesNotExist(settingsFileName);
          return;
        }

        var settings = new Settings(settingsFileName);

        var preview = false;
        switch (cmd)
        {
          case "view":
            Message.View(settings.LocalDir, settings.RemoteDir);
            preview = true;
            Compare(settings.LocalDir, settings.RemoteDir, settings);
            break;

          case "pull":
            Message.Syncing(settings.RemoteDir, settings.LocalDir);
            Sync(settings.RemoteDir, settings.LocalDir, settings);
            break;

          case "push":
            Message.Syncing(settings.LocalDir, settings.RemoteDir);
            Sync(settings.LocalDir, settings.RemoteDir, settings);
            break;

          default:
            Message.UsageHint();
            break;
        }

        Message.FileStatistics(_newFileCount, _replacedFileCount, _skippedFileCount, _unchangedFileCount, preview);
      }
      catch (Exception ex)
      {
        Message.UnexpectedError(ex);
      }
    }

    private static void CreateSettingsFile(string fileName)
    {
      if (File.Exists(fileName))
      {
        Message.SettingsFileAlreadyExist(fileName); 
        return;
      }

      var assembly = typeof(Program).Assembly;
      var resourceName = typeof(Program).Namespace + ".syncdir.settings.json";

      using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
      {
        if (resourceStream == null)
        {
          throw new Exception($"Resource '{resourceName}' not found.");
        }

        // Create a file stream to write the resource to a file
        using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
          resourceStream.CopyTo(fileStream);
        }
      }

      Process.Start(new ProcessStartInfo()
      {
        UseShellExecute = true,
        FileName = fileName
      });

    }

    internal static void Sync(DirectoryInfo sourceDir, DirectoryInfo targetDir, Settings settings)
    {
      if (!sourceDir.Exists)
      {
        Message.SourceDirDoesNotExist(sourceDir);
        return;
      }
      if (!targetDir.Exists)
      {
        Message.CreateDirectory(targetDir);
        targetDir.Create();
      }

      var targetFiles = targetDir.GetFiles();

      foreach (var sourceFile in sourceDir.GetFiles())
      {
        try
        {
          if (settings.Ignore(sourceFile))
          {
            continue;
          }

          var targetFile = targetFiles.SingleOrDefault(t => sourceFile.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));

          if (targetFile == null)
          {
            Message.CopyNewFile(sourceFile);
            Copy(sourceFile, targetDir, settings);
            _newFileCount++;
          }
          else if (targetFile.LastWriteTimeUtc < sourceFile.LastWriteTimeUtc)
          {
            Message.CopyNewerFile(sourceFile);
            Copy(sourceFile, targetDir, settings);
            _replacedFileCount++;
          }
          else if (targetFile.LastWriteTimeUtc > sourceFile.LastWriteTimeUtc)
          {
            Message.SkipOlderFile(sourceFile);
            _skippedFileCount++;
          }
          else if (targetFile.Length != sourceFile.Length)
          {
            Message.CopyFileWithDifferentSize(sourceFile);
            Copy(sourceFile, targetDir, settings);
            _replacedFileCount++;
          }
          else
          {
            _unchangedFileCount++;
          }
        }
        catch (Exception ex)
        {
          throw new Exception($"Error \"{ex.Message}\" while processing \"{sourceFile.FullName}\".", ex);
        }
      }

      foreach (var sourceSubDir in sourceDir.GetDirectories())
      {
        if (settings.Ignore(sourceSubDir))
        {
          continue;
        }
        var targetSubDir = targetDir.CreateSubdirectory(sourceSubDir.Name);
        Sync(sourceSubDir, targetSubDir, settings);
      }
    }

    internal static void Compare(DirectoryInfo sourceDir, DirectoryInfo targetDir, Settings settings)
    {
      if (!sourceDir.Exists)
      {
        Message.SourceDirDoesNotExist(sourceDir);
        return;
      }
      if (!targetDir.Exists)
      {
        Message.TargetDirDoesNotExist(targetDir);
        return;
      }

      var targetFiles = targetDir.GetFiles();

      foreach (var sourceFile in sourceDir.GetFiles())
      {
        try
        {
          if (settings.Ignore(sourceFile))
          {
            continue;
          }

          var targetFile = targetFiles.SingleOrDefault(t => sourceFile.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));

          if (targetFile == null)
          {
            Message.TargetFileDoesNotExist(sourceFile, targetDir);
            _newFileCount++;
          }
          else if (targetFile.LastWriteTimeUtc < sourceFile.LastWriteTimeUtc)
          {
            Message.FileIsNewer(sourceFile, targetFile);
            _replacedFileCount++;
          }
          else if (targetFile.LastWriteTimeUtc > sourceFile.LastWriteTimeUtc)
          {
            Message.FileIsOlder(sourceFile, targetFile);
            _skippedFileCount++;
          }
          else if (targetFile.Length != sourceFile.Length)
          {
            Message.FileSizeIsDifferent(sourceFile, targetFile);
            _replacedFileCount++;
          }
          else
          {
            _unchangedFileCount++;
          }
        }
        catch (Exception ex)
        {
          throw new Exception($"Error \"{ex.Message}\" while processing \"{sourceFile.FullName}\".", ex);
        }
      }

      foreach (var sourceSubDir in sourceDir.GetDirectories())
      {
        if (settings.Ignore(sourceSubDir))
        {
          continue;
        }
        var targetSubDir = new DirectoryInfo(Path.Combine(targetDir.FullName, sourceSubDir.Name));
        Compare(sourceSubDir, targetSubDir, settings);
      }
    }

    internal static void Copy(FileInfo sourceFile, DirectoryInfo targetDir, Settings settings)
    {
      var targetFileName = Path.Combine(targetDir.FullName, sourceFile.Name);
      var targetFile = new FileInfo(targetFileName);
      if (targetFile.Exists)
      {
        var backupDir = targetDir.CreateSubdirectory(settings.BackupFolderName);
        if (settings.SetHiddenAttributeOnBackupFolder)
        {
          backupDir.Attributes |= FileAttributes.Hidden;
        }
        var backupFileName = Path.Combine(backupDir.FullName, $"{Path.GetFileNameWithoutExtension(targetFile.Name)}_{targetFile.LastWriteTimeUtc:yyyyMMdd_HHmmssfff}{targetFile.Extension}");
        if (File.Exists(backupFileName))
        {
          File.Delete(backupFileName);
        }
        targetFile.MoveTo(backupFileName);
      }
      targetFile = sourceFile.CopyTo(targetFileName);
      targetFile.Attributes = sourceFile.Attributes;
      targetFile.LastWriteTimeUtc = sourceFile.LastWriteTimeUtc;
    }
  }
}