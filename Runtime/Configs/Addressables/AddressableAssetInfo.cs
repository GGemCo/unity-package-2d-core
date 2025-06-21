namespace GGemCo.Scripts
{
    public class AddressableAssetInfo
    {
        public string Key { get; }
        public string Path { get; }
        public string Label { get; }
        public string Etc1 { get; }

        public AddressableAssetInfo(string key, string path, string label = null, string etc1 = null)
        {
            Key = key;
            Path = path;
            Label = label;
            Etc1 = etc1;
        }
    }
}