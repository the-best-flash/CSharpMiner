using System;

namespace CSharpMiner.Interfaces
{
    public interface IShareResponse
    {
        long Id { get; set; }
        Object Data { get; set; }
        Object[] Error { get; set; }
        int RejectErrorId { get; }
        string RejectReason { get; }
        bool IsLowDifficlutyShare { get; }
        bool JobNotFound { get; }
    }
}
