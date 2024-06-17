using System;

namespace ProjectDawn.Impostor
{
    public class ReloadAttribute : Attribute
    {
        public string Path;
        public ReloadAttribute(string path)
        {
            Path = path;
        }
    }
}
