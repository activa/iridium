namespace Iridium.DB
{
    public struct StringPair
    {
        public StringPair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key;
        public string Value;
    }
}