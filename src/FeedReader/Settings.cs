namespace FeedReader
{
    public static class Settings
    {
        public const string DIRECTORY_PATH = @"D:\Производственная практика 2025\inFiles";
        public const string SCHEMA_PATH = @"schema.json";
        public const int MAX_ATTEMPTS = 5;
        public const int READ_FILE_TIMEOUT_SECONDS = 120;

        public const string RABBIT_HOSTNAME = "localhost";
        public const string SEND_QUEUE = "toQuantity";
        public const int CONNECTION_TIMEOUT_SECONDS = 10;

        public const int MAX_CONCURRENT = 4;

        public const bool IS_DEBUG = true; // debug mode
    }
}
