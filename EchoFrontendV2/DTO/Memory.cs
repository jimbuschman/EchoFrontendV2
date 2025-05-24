using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class Memory
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string SummaryText { get; set; }

        public int SessionID { get; set; }
        public string SpeakerRole { get; set; }
        public DateTime TimeStamp { get; set; }
        private double _importance;
        public double Importance
        {
            get => _importance;
            set
            {
                _importance = Math.Min(value, 1.0f);                
            }
        }
        public string Source { get; set; }
        public double? Rank { get; set; }
        public string Metadata_json { get; set; }
        public List<MemoryBlob> Blobs { get; set; } = new();
    }
}
