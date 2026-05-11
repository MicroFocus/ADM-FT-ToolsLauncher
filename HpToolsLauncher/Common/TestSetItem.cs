namespace HpToolsLauncher.Common
{
    internal sealed class TestSetItem
    {
        public int ID { get; }
        public string Name { get; }
        public string Path { get; }

        public TestSetItem(int id, string name, string path)
        {
            ID = id;
            Name = name;
            Path = path;
        }
    }
}
