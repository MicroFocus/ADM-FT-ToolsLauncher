namespace HpToolsLauncher.Common
{
    internal sealed class TestSetItem(int id, string name, string path)
    {
        public int ID => id;
        public string Name => name;
        public string Path => path;
    }
}