using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class MemoryTag
    {
        public int ID { get; set; } // Primary Key
        public int MemoryID { get; set; } // Foreign Key referencing Memories(ID)
        public int TagID { get; set; } // Foreign Key referencing Tags(ID)
        public DateTime TimeStamp { get; set; } // Timestamp with default value

        public MemoryTag()
        {
            TimeStamp = DateTime.Now; // Set default timestamp to current time
        }
    }
}
