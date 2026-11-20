using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace fiskaltrust.AndroidLauncher.Models
{
    public class WorkerConfiguration
    {
        public List<dynamic> QueuePLList { get; } = new List<dynamic>();        

        public List<dynamic> QueueList { get; } = new List<dynamic>();

        public List<dynamic> SignaturCreationUnitPLList { get; } = new List<dynamic>();
    }
}
