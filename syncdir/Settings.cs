namespace SigTec.NetFx48DevCliTools.SyncDir
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text.Json;
  using System.Text.RegularExpressions;

  public class Settings
  {
    private readonly IEnumerable<Regex> _ignoreFileRegex;
    private readonly IEnumerable<Regex> _ignoreDirRegex;

    public DirectoryInfo RemoteDir { get; }
    public DirectoryInfo LocalDir { get; }
    public string BackupFolderName { get; }
    public bool SetHiddenAttributeOnBackupFolder { get; }

    public class SettingsContainer
    {
      public string RemotePath { get; set; }
      public string LocalPath { get; set; }
      public string BackupFolderName { get; set; }
      public bool SetHiddenAttributeOnBackupFolder { get; set; }
      public IReadOnlyList<string> IgnoreFiles { get; }
      public IReadOnlyList<string> IgnoreDirs { get; }
    };

    public Settings(string jsonFileName)
    {
      var json = File.ReadAllText(jsonFileName);
      var container = JsonSerializer.Deserialize<SettingsContainer>(json)
        ?? throw new InvalidOperationException("failed to parse settings file");
      this.LocalDir = new DirectoryInfo(container.LocalPath);
      this.RemoteDir = new DirectoryInfo(container.RemotePath);
      this.BackupFolderName = container.BackupFolderName;
      this.SetHiddenAttributeOnBackupFolder = container.SetHiddenAttributeOnBackupFolder;

      this._ignoreFileRegex = container.IgnoreFiles.Select(x => CreateRegex(x)).ToList();
      this._ignoreDirRegex = container.IgnoreDirs.Select(x => CreateRegex(x)).ToList();
    }

    private static Regex CreateRegex(string pattern)
    {
      var regex = $"^{Regex.Escape(pattern)}$"
            .Replace("\\*", ".*")
            .Replace("\\?", ".");
      return new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public bool Ignore(FileInfo file) => _ignoreFileRegex.Any(x => x.IsMatch(file.Name));

    public bool Ignore(DirectoryInfo dir) => _ignoreDirRegex.Any(x => x.IsMatch(dir.Name));
  }
}