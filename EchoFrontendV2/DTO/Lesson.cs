namespace EchoFrontendV2.DTO
{
    using System;

    public class Lesson
    {
        public int ID { get; set; } // Primary Key
        public string Text { get; set; } // Text of the lesson
        public byte[] Embedding { get; set; } // Embedding as a byte array
        public DateTime TimeStamp { get; set; } // Timestamp of when the lesson was added
        public int? SourceMemoryID { get; set; } // Nullable foreign key
        public string AddedBy { get; set; } // User who added the lesson

        // Constructor
        public Lesson()
        {
            TimeStamp = DateTime.Now; // Set default timestamp to current time
        }
    }

}
