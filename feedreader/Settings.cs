namespace feedreader
{
    public static class Settings
    {
        public const string DIRECTORY_PATH = @"D:\Производственная практика 2025\inFiles";
        public const string SCHEMA_PATH = @"schema.json";
        public const int MAX_ATTEMPTS = 5;
        public const string RABBIT_HOSTNAME = "localhost";
        public const int CONNECTION_TIMEOUT_SECONDS = 3;
        public const int READ_FILE_TIMEOUT_SECONDS = 120;
    }
}
