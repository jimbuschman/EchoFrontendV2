using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Embedding { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime TimeStamp { get; set; }
        public Tag()
        {
            TimeStamp = DateTime.Now; // Set default timestamp to current time
        }
    }
}
