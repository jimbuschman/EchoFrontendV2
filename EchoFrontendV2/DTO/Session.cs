using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class Session
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
