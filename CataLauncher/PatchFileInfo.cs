using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CataLauncher
{
    public class PatchFileInfo
    {
        public string url { get; set; }
        public string file { get; set; }
        public string md5hash { get; set; }
        public long totalbytes { get; set; }

        public PatchFileInfo(string URL, string File)
        {
            url = URL;
            file = File;
        }
    }
}
