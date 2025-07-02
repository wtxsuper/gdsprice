namespace QuantityCalculator;

public static class Settings
{
    public const string RABBIT_HOSTNAME = "localhost";
    public const int CONNECTION_TIMEOUT_SECONDS = 10;
    public const string RECEIVE_QUEUE = "toCalc";
    public const string SEND_QUEUE = "toPrice";
}
