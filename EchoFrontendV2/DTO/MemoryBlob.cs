using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class MemoryBlob
    {
        public int Id { get; set; }
        public int MemoryID { get; set; }
        public string Saved_by { get; set; }
        public byte[] Embedding { get; set; }
    }
}
