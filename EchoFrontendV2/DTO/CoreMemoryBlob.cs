using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class CoreMemoryBlob
    {
        public int ID { get; set; } // Primary Key
        public int CoreMemoryID { get; set; } // Foreign Key
        public byte[] Embedding { get; set; } // BLOB data
        public DateTime TimeStamp { get; set; } // DATETIME
    }
}
