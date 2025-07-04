namespace PriceCalculator;

public static class Settings
{
    public const string DIRECTORY_PATH = @"D:\Производственная практика 2025\outFiles\";

    public const string RABBIT_HOSTNAME = "localhost";
    public const int CONNECTION_TIMEOUT_SECONDS = 10;
    public const string RECEIVE_QUEUE = "toPrice";

    public const int MAX_CONCURRENT = 4; // max concurrent processing
}
