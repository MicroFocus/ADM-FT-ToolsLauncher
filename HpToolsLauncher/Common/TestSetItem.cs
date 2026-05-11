namespace HpToolsLauncher.Common
{

    internal sealed class TestSetItem(int id, string name, string path)
    {
        public int ID { get; } = id;
        public string Name { get; } = name;
        public string Path { get; } = path;
    }

}