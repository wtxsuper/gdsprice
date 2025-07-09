namespace QuantityCalculator;

public static class Settings
{
    public const string RABBIT_HOSTNAME = "localhost";
    public const int CONNECTION_TIMEOUT_SECONDS = 10;
    public const string RECEIVE_QUEUE = "toQuantity";
    public const string SEND_QUEUE = "toPrice";

    public const int MAX_CONCURRENT = 4; // max concurrent processing

    public const bool IS_DEBUG = true; // debug mode
}
