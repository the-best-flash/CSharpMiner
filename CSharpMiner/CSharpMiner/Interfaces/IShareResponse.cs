using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.Interfaces
{
    public interface IShareResponse
    {
        int Id { get; set; }
        Object Data { get; set; }
        Object[] Error { get; set; }
        int RejectErrorId { get; }
        string RejectReason { get; }
        bool IsLowDifficlutyShare { get; }
        bool JobNotFound { get; }
    }
}
