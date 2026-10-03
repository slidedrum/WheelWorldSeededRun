namespace SeededRun
{
    internal static class SeedParser
    {
        internal static bool TryConvertToInt32(string text, out int seed)
        {
            if (string.IsNullOrEmpty(text))
            {
                seed = 0;
                return false;
            }

            // FNV-1a is stable across runtimes; string.GetHashCode() is not.
            unchecked
            {
                uint hash = 2166136261u;

                foreach (char character in text)
                {
                    hash ^= (byte)character;
                    hash *= 16777619u;
                    hash ^= (byte)(character >> 8);
                    hash *= 16777619u;
                }

                seed = (int)hash;
            }

            return true;
        }
    }
}
