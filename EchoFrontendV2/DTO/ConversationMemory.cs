using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace EchoFrontendV2.DTO
{
    public class ConversationMemory
    {
        //[PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        //[Ignore]  // Not stored directly in SQLite
        public List<Memory> Turns { get; set; } = new();

        //[MaxLength(500)]  // Limit summary length
        public string Summary { get; set; }

        //[Indexed]  // Faster querying
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Additional useful fields
        public string Title { get; set; }
        public bool IsArchived { get; set; }
        public string MetadataJson { get; set; }

        // Helper method for lazy loading
        public async Task LoadTurnsAsync(SqliteConnection connection)
        {
            //Turns = await connection.QueryAsync<Memory>(
            //    "SELECT * FROM Memories WHERE ConversationId = @Id ORDER BY Timestamp",
            //    new { Id });
        }
    }
}
