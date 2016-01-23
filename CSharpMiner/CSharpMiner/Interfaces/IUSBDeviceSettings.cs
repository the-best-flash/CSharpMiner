namespace CSharpMiner.Interfaces
{
    public interface IUSBDeviceSettings
    {
        string Port { get; set; }
        int WatchdogTimeout { get; set; }
        int PollFrequency { get; set; }
    }
}
