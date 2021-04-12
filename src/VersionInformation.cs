using System.Collections.Generic;

namespace Launcher.src
{
    public class VersionInformation
    {
        public class TManifest
        {
            public int TotalFiles { get; set; }
            public int TotalFolders { get; set; }
            public string LauncherVersion { get; set; }
        }

        public Dictionary<string, string> Files { get; set; }
        public List<string> Folders { get; set; }
        public TManifest Manifest { get; set; }
    }
}
