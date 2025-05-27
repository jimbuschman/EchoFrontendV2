using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class Framework
    {
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } // Short identifier like "MemoryProtocol"
        public string Description { get; set; } // Brief explanation of its purpose
        public string Content { get; set; } // Clean, usable version for LLM interaction
        public string Tags { get; set; } // Comma-separated, e.g., "memory,identity"
        public int? FileId { get; set; } // Foreign key to Files.Id

        // Navigation property for the related File
        public virtual File File { get; set; }
    }

    public class Image
    {
        // Property for the Id column
        public int Id { get; set; }

        // Property for the Name column
        public string Name { get; set; }

        // Property for the Tags column
        public string Tags { get; set; } // Comma-separated tags

        // Property for the Description column
        public string Description { get; set; }

        // Property for the FilePath column
        public string FilePath { get; set; }

        // Constructor
        public Image(int id, string name, string tags, string description, string filePath)
        {
            Id = id;
            Name = name;
            Tags = tags;
            Description = description;
            FilePath = filePath;
        }

        // Default constructor
        public Image() { }
    }
    public class Book
    {
        public int Id { get; set; } // Primary Key
        public string Title { get; set; } // Title of the book, cannot be null
        public string Author { get; set; } // Author of the book
        public string Summary { get; set; } // Summary of the book
        public string Tags { get; set; } // Comma-separated tags, e.g., "philosophy,autonomy"
        public int? FileId { get; set; } // Foreign key to Files.Id

        // Navigation property for the related File
        public virtual File File { get; set; }
    }
    public class File
    {
        public int Id { get; set; } // Primary Key
        public string Text { get; set; } // Text content
        public string SummaryText { get; set; } // Summary of the text
        public DateTime TimeStamp { get; set; } // Timestamp of creation
        public string Source { get; set; } // Source of the file
        public string MetadataJson { get; set; } // Metadata in JSON format
    }

}
