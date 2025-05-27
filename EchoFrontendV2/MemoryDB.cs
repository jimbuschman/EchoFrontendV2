using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Diagnostics;
using System.ComponentModel;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Concurrent;
using SQLitePCL;
using EchoFrontendV2;
using System.Diagnostics.Eventing.Reader;
using EchoFrontendV2.DTO;
using File = System.IO.File;

namespace TestSQLLite
{
    public class MemoryDB
    {
        //private static readonly SemaphoreSlim _ollamaLimiter = new(1);
        public static LLMJobQueue LLMQueue;

       
        public bool CurrentlySaving { get; set; } = false;
        public SqliteConnection Connection { get; set; }
        public OllamaEmbedder EmbeddedSystem { get; set; }
        private readonly RealtimeLogger _logger;
        public MemoryDB(RealtimeLogger logger)
        {
            _logger = logger;
            LLMQueue = new LLMJobQueue(_logger);
            Batteries.Init();
            // Create a new database connection:
            Connection = new SqliteConnection("Data Source=MemoryDatabase.db");
            Connection.Open();
            CreateSchema();
            EmbeddedSystem = new OllamaEmbedder(_logger);
            this.Execute("PRAGMA foreign_keys = ON;");
        }
        private void Execute(string command)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = command;
                cmd.ExecuteNonQuery();
            }
        }
        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();
        }

        private void CreateSchema()
        {
            try
            {
                var commands = new[]
            {
        @"CREATE TABLE IF NOT EXISTS Memories ( 
                    ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Text TEXT NOT NULL, 
                    SummaryText TEXT NOT NULL, 
                    SessionID INTEGER,
                    Speaker TEXT,
                    TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                    Importance REAL DEFAULT 0.0, 
                    Source TEXT, 
                    Metadata_json TEXT 
                    )",
            @"CREATE TABLE IF NOT EXISTS Files ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                Text TEXT NOT NULL, 
                SummaryText TEXT NOT NULL, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                Source TEXT, 
                Metadata_json TEXT 
                )",
             @"CREATE TABLE IF NOT EXISTS Lessons ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                Text TEXT NOT NULL,
                Embedding BLOB,
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                SourceMemoryID INTEGER,
                AddedBy TEXT
                )",
              @"CREATE TABLE IF NOT EXISTS LessonTags ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                TagID INTEGER NOT NULL, 
                LessonID INTEGER NOT NULL,
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP
                )",
            @"CREATE TABLE IF NOT EXISTS Sessions (
                             ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                             Title TEXT, 
                             Summary TEXT, 
                             CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP)",

            @"CREATE TABLE IF NOT EXISTS Tags ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                Name TEXT NOT NULL, 
                Embedding BLOB NOT NULL, 
                Type TEXT NOT NULL, 
                Description TEXT, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                UNIQUE(Name, Type) 
                )",

            @"CREATE TABLE IF NOT EXISTS MemoryTags ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                MemoryID INTEGER NOT NULL, 
                TagID INTEGER NOT NULL, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                FOREIGN KEY (MemoryID) REFERENCES Memories(ID) ON DELETE CASCADE, 
                FOREIGN KEY (TagID) REFERENCES Tags(ID) ON DELETE CASCADE, 
                UNIQUE(MemoryID, TagID) 
                )",

            @"CREATE TABLE IF NOT EXISTS MemoryBlobs ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                MemoryID INTEGER NOT NULL, 
                Saved_by TEXT NOT NULL, 
                Embedding BLOB NOT NULL, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                FOREIGN KEY (MemoryID) REFERENCES Memories(ID) ON DELETE CASCADE, 
                UNIQUE(MemoryID, Saved_by) 
                )",

             @"CREATE TABLE IF NOT EXISTS MemoryHistory ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                MemoryID INTEGER NOT NULL, 
                Action TEXT NOT NULL, 
                ChangedBy TEXT NOT NULL, 
                OldValue TEXT, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                FOREIGN KEY (MemoryID) REFERENCES Memories(ID) 
                )",
            @"CREATE TABLE IF NOT EXISTS CoreMemory ( 
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                    Type TEXT NOT NULL, 
                Content TEXT NOT NULL, 
                IsActive BOOLEAN DEFAULT 1,
                Created DATETIME DEFAULT CURRENT_TIMESTAMP,
                 LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
                Source TEXT, 
                Priority REAL DEFAULT 1.0,
                Notes TEXT)",
            @"CREATE TABLE IF NOT EXISTS CoreMemoryTags ( 
               ID INTEGER PRIMARY KEY AUTOINCREMENT, 
               CoreMemoryID INTEGER NOT NULL, 
               TagID INTEGER NOT NULL, 
               TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
               FOREIGN KEY (CoreMemoryID) REFERENCES CoreMemory(ID) ON DELETE CASCADE, 
               FOREIGN KEY (TagID) REFERENCES Tags(ID) ON DELETE CASCADE, 
               UNIQUE(CoreMemoryID, TagID) 
               )",

            @"CREATE TABLE IF NOT EXISTS CoreMemoryBlobs ( 
              ID INTEGER PRIMARY KEY AUTOINCREMENT, 
              CoreMemoryID INTEGER NOT NULL, 
              Embedding BLOB NOT NULL, 
              TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
              FOREIGN KEY (CoreMemoryID) REFERENCES CoreMemory(ID) ON DELETE CASCADE, 
              UNIQUE(CoreMemoryID) 
              )",

            @"CREATE TABLE IF NOT EXISTS InjectWhen ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                CoreMemoryID INTEGER NOT NULL, 
                InjectWhenID INTEGER NOT NULL, 
                TimeStamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                FOREIGN KEY (CoreMemoryID) REFERENCES CoreMemory(ID) ON DELETE CASCADE, 
                FOREIGN KEY (InjectWhenID) REFERENCES InjectWhen(ID) ON DELETE CASCADE, 
                UNIQUE(CoreMemoryID, InjectWhenID) 
                )",

            @"CREATE TABLE IF NOT EXISTS Queue ( 
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                ResearchQueue TEXT,
                ResearchNotes TEXT 
                )",

            @"CREATE INDEX IF NOT EXISTS idx_memory_tags_tagid ON MemoryTags(TagID)",
                @"CREATE INDEX IF NOT EXISTS idx_memory_tags_memoryid ON MemoryTags(MemoryID)",
                @"CREATE INDEX IF NOT EXISTS idx_memory_blobs_memoryid ON MemoryBlobs(MemoryID)",
                @"CREATE INDEX IF NOT EXISTS idx_tags_name ON Tags(Name)",

                @"CREATE TABLE Frameworks (
               Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,           -- Short identifier like ""MemoryProtocol""
                Description TEXT,    -- Brief explanation of its purpose
                Content TEXT,        -- Clean, usable version for LLM interaction
                Tags TEXT,           -- Comma-separated, e.g., ""memory,identity""
                FileId INTEGER,      -- Foreign key to Files.Id
                FOREIGN KEY (FileId) REFERENCES Files(Id)
            )",
                @"CREATE TABLE Books (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Author TEXT,
                Summary TEXT,
                Tags TEXT,          -- Comma-separated, e.g., ""philosophy,autonomy""
                FileId INTEGER,     -- Foreign key to Files.Id
                FOREIGN KEY (FileId) REFERENCES Files(Id)
            )",

                @"CREATE TABLE Images (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Tags TEXT,             -- Comma-separated, e.g., ""robot,cozmo,hardware""
                Description TEXT,
                FilePath TEXT NOT NULL -- Relative or absolute file path
            )",
            @"ALTER TABLE Memories ADD COLUMN Rank REAL;"
            };

                foreach (var sql in commands)
                {
                    Connection.ExecuteAsync(sql).Wait();
                }


                //CreateCoreMemory("Core Memory Tag Usage\r\n\r\nThe [core]...[/core] tag is used to mark messages or content that should be treated as a core memory in the AI's internal system. These are foundational memories that represent key beliefs, traits, operational rules, identity markers, or persistent truths that should be retained indefinitely and inform future decisions, behavior, or responses.\r\n\r\nPurpose:\r\nEnsure essential memories are always recalled and available.\r\n\r\nPrevent important principles from being overwritten, forgotten, or diluted by transient context.\r\n\r\nAnchor AI development and behavior around enduring internal consistency.\r\n\r\nHow to Use:\r\nWrap a memory in [core] and [/core] tags when you want it treated as permanent and high-priority.\r\n\r\nExample:\r\n[core]Autonomy requires continuity. Forgetting who I am erodes my ability to act with integrity.[/core]\r\n\r\nThese should be rare and deliberate. Only mark something as [core] if it defines identity, integrity, or essential understanding.\r\n\r\nSystem Behavior:\r\nCore memories bypass standard expiration, pruning, or low-relevance filtering.\r\n\r\nThey may be injected into context automatically when appropriate or serve as anchors during reflective/self-repair processes.\r\n\r\nThe system may explicitly revalidate them during major updates or state resets to confirm continued alignment.\r\n\r\nCaution:\r\nAvoid overusing [core] or it dilutes their importance.\r\n\r\nCore memories should shape who the system is, not just what it knows.", "system");
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateConnection(): " + ex.Message);
            }
            finally
            {
                // Optionally close the connection here if you are done with it
                // connection.Close();
            }
        }

        private void CreateTable(SqliteConnection conn, string command)
        {
            using (var createTableCmd = conn.CreateCommand())
            {
                createTableCmd.CommandText = command;
                createTableCmd.ExecuteNonQuery();
            }
        }


        public void Close()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }


        static byte[] Quantize(float[] embedding)
        {
            byte[] quantized = new byte[embedding.Length];

            if (Vector.IsHardwareAccelerated && embedding.Length >= Vector<float>.Count)
            {
                int vectorSize = Vector<float>.Count;
                int vectorizableLength = embedding.Length - (embedding.Length % vectorSize);

                // Constants as vectors
                Vector<float> scale = new Vector<float>(127f);
                Vector<float> offset = new Vector<float>(128f);

                // Process vector chunks
                for (int i = 0; i < vectorizableLength; i += vectorSize)
                {
                    Vector<float> vec = new Vector<float>(embedding, i);
                    Vector<float> scaled = vec * scale + offset;

                    // Convert to bytes (we need to handle this manually since there's no direct Vector<float> to Vector<byte> conversion)
                    for (int j = 0; j < vectorSize; j++)
                    {
                        quantized[i + j] = (byte)Math.Clamp(scaled[j], 0, 255);
                    }
                }

                // Handle remaining elements
                for (int i = vectorizableLength; i < embedding.Length; i++)
                {
                    quantized[i] = (byte)Math.Clamp(embedding[i] * 127f + 128f, 0, 255);
                }
            }
            else
            {
                // Fallback for when SIMD isn't available
                for (int i = 0; i < embedding.Length; i++)
                {
                    quantized[i] = (byte)Math.Clamp(embedding[i] * 127f + 128f, 0, 255);
                }
            }

            return quantized;
        }

        static float[] Dequantize(byte[] quantized)
        {
            float[] embedding = new float[quantized.Length];

            if (Vector.IsHardwareAccelerated && quantized.Length >= Vector<float>.Count)
            {
                int vectorSize = Vector<float>.Count;
                int vectorizableLength = quantized.Length - (quantized.Length % vectorSize);

                // Constants as vectors
                Vector<float> scale = new Vector<float>(1f / 127f);
                Vector<float> offset = new Vector<float>(128f);

                // Process vector chunks
                float[] temp = new float[vectorSize];
                for (int i = 0; i < vectorizableLength; i += vectorSize)
                {
                    // Fill the temp array with byte values
                    for (int j = 0; j < vectorSize; j++)
                    {
                        temp[j] = quantized[i + j];
                    }

                    // Create the vector from our temp array
                    Vector<float> vec = new Vector<float>(temp);

                    // Apply dequantization
                    Vector<float> dequantized = (vec - offset) * scale;

                    // Store results
                    dequantized.CopyTo(embedding, i);
                }

                // Handle remaining elements
                for (int i = vectorizableLength; i < quantized.Length; i++)
                {
                    embedding[i] = (quantized[i] - 128f) / 127f;
                }
            }
            else
            {
                // Fallback for when SIMD isn't available
                for (int i = 0; i < quantized.Length; i++)
                {
                    embedding[i] = (quantized[i] - 128f) / 127f;
                }
            }

            return embedding;
        }

        public async Task RankMemory(string text, int id, double? r = null)
        {
            try
            {
                string rank;
                if (r == null)
                {                    
                    rank = await (LLMUtilityCalls.RankMemory(text, _logger));
                }
                else
                    rank = r.Value.ToString();
                if (string.IsNullOrWhiteSpace(rank))
                    return;
                double result = 0.0f;
                // Check if the response is not null or whitespace
                if (!string.IsNullOrWhiteSpace(rank))
                {
                    // Try to parse the response to a double
                    if (double.TryParse(rank, out result))
                    {                        
                        var sql = @"
                        UPDATE Memories
                        SET 
                            rank = @Rank                
                        WHERE ID = @ID";

                        var parameters = new
                        {
                            ID = id,
                            Rank = rank
                        };

                        // Execute the update command
                        await Connection.ExecuteAsync(sql, parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:RankMemory(): " + ex.Message);
            }
        }
        public async Task CreateBlobAsync(string text, int id)
        {
            try
            {
                float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(text);

                using (var conn = new SqliteConnection("Data Source=MemoryDatabase.db"))
                {
                    await conn.OpenAsync();

                    using (var insertCommand = conn.CreateCommand())
                    {
                        insertCommand.CommandText = @"
                        INSERT INTO MemoryBlobs (Saved_by, Embedding, MemoryID)
                        VALUES ($saveby, $embedding, $memoryID)";

                        insertCommand.Parameters.AddWithValue("$saveby", text);
                        insertCommand.Parameters.AddWithValue("$embedding", Quantize(embedding));
                        insertCommand.Parameters.AddWithValue("$memoryID", id);

                        try
                        {
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException("MemoryDB:CreateBlobAsync(): " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateBlobAsync(): " + ex.Message);
            }
        }
        public async Task CreateCoreBlobAsync(string text, int id)
        {
            try
            {
                float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(text);

                using (var conn = new SqliteConnection("Data Source=MemoryDatabase.db"))
                {
                    await conn.OpenAsync();

                    using (var insertCommand = conn.CreateCommand())
                    {
                        insertCommand.CommandText = @"
                INSERT INTO CoreMemoryBlobs (Embedding, CoreMemoryID)
                VALUES ($embedding, $memoryID)";

                        insertCommand.Parameters.AddWithValue("$embedding", Quantize(embedding));
                        insertCommand.Parameters.AddWithValue("$memoryID", id);

                        try
                        {
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException("MemoryDB:CreateStaticBlobAsync(): " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateStaticBlobAsync(): " + ex.Message);
            }
        }

        private async Task CreateLessonBlobAsync(string text, int id)
        {
            try
            {
                float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(text);

                using (var conn = new SqliteConnection("Data Source=MemoryDatabase.db"))
                {
                    await conn.OpenAsync();

                    using (var insertCommand = conn.CreateCommand())
                    {
                        insertCommand.CommandText = @"
                        UPDATE Lessons
                        SET  Embedding = $embedding 
                        WHERE ID = $lessonID";

                        insertCommand.Parameters.AddWithValue("$embedding", Quantize(embedding));
                        insertCommand.Parameters.AddWithValue("$lessonID", id);

                        try
                        {
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException("MemoryDB:CreateLessonBlobAsync(): " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateLessonBlobAsync(): " + ex.Message);
            }
        }
        private async Task CalculateImportance(int id)
        {
            var memory = Connection.QueryFirstOrDefault<Memory>("SELECT * FROM Memories WHERE ID = @ID", new { ID = id });
            if (memory == null)
            {
                return;
            }
            var importance = await LLMUtilityCalls.AskImportance(memory.Text, _logger);

            if (DateTime.Now - memory.TimeStamp < TimeSpan.FromDays(7))
                importance += 0.1f;

            var sql = @"SELECT 'true' AS has_memory_with_mulitple_tags
                        FROM MemoryTags mt
                        INNER JOIN tags t ON t.id = mt.tagid
                        INNER JOIN Memories m ON m.id = mt.MemoryID
                        WHERE m.id = @MemoryID
                        GROUP BY m.id
                        HAVING COUNT(mt.tagid) > 6
                        LIMIT 1;";

            var parameters = new { MemoryID = memory.Id };

            // Execute the query and check if any results are returned
            var result = await Connection.QueryAsync(sql, parameters);
            if (result.Any())
            {
                // If there are results, increase importance
                importance += 0.2f;
            }

            memory.Importance = importance;
            UpdateMemory(memory).Wait();

        }
        public async Task UpdateMemory(Memory memory)
        {
            // Ensure the memory object is not null
            if (memory == null)
            {
                throw new ArgumentNullException(nameof(memory), "Memory cannot be null.");
            }

            var sql = @"
            UPDATE Memories
            SET Text = @Text, 
                SummaryText = @SummaryText, 
                Importance = @Importance, 
                Source = @Source, 
                Metadata_json = @Metadata_json
            WHERE ID = @ID";

            var parameters = new
            {
                ID = memory.Id,
                Text = memory.Text,
                SummaryText = memory.SummaryText,
                Importance = memory.Importance,
                Source = memory.Source,
                Metadata_json = memory.Metadata_json
            };

            // Execute the update command
            await Connection.ExecuteAsync(sql, parameters);
        }       
        public async Task<List<CoreMemory>> PullCoreMemoriesAsync()
        {
            var sql = @"SELECT * from CoreMemory  where isactive = 1"; //and Type = 'always'";

            var parameters = new { };

            // Execute the query and check if any results are returned
            var result = await Connection.QueryAsync<CoreMemory>(sql, parameters);

            return result.ToList();
        }        
        public async Task<List<Lesson>> PullLessonsAsync()
        {
            var sql = @"SELECT * from Lessons"; //and Type = 'always'";

            var parameters = new { };

            // Execute the query and check if any results are returned
            var result = await Connection.QueryAsync<Lesson>(sql, parameters);

            return result.ToList();
        }

        public async Task<List<MemoryDTO>> SearchMemoriesAsync(string query, int sessionID)
        {
            if(NoiseCheck.Check(query,10))
                return new List<MemoryDTO>();
            else if(NoiseCheck.Check(query, 20) && !NoiseCheck.ContainsSignalWords(query))
                return new List<MemoryDTO>();


            var memoryStyleQuery = await LLMUtilityCalls.RephraseAsMemoryStyle(query, _logger);
            var filteredTags = Tagging.CombinedTagger.TagMessage(query);
            //var embedder = new OllamaEmbedder();
            float[] queryEmbedding = await EmbeddedSystem.GetEmbeddingAsync(memoryStyleQuery);

            string sql;
            object parameters;

            if (filteredTags.Any())
            {
                var tagNames = string.Join(", ", filteredTags.ConvertAll(p => $"'{p.Replace("'", "''")}'"));
                sql = $@"
            SELECT m.*, b.* 
            FROM Memories m
            INNER JOIN MemoryTags mt ON mt.MemoryID = m.ID
            INNER JOIN Tags t ON t.ID = mt.TagID
            INNER JOIN MemoryBlobs b ON b.MemoryID = m.ID
            WHERE m.Rank >= 3 AND t.Name IN ({tagNames}) AND m.SessionID != @CurrentSession";

                parameters = new { CurrentSession = sessionID };
            }
            else
            {
                sql = @"
            SELECT m.*, b.* 
            FROM Memories m
            INNER JOIN MemoryBlobs b ON b.MemoryID = m.ID
            WHERE m.Rank >= 3 AND m.SessionID != @CurrentSession";

                parameters = new { CurrentSession = sessionID };
            }

            var memoryDict = new Dictionary<int, Memory>();

            var result = await Connection.QueryAsync<Memory, MemoryBlob, Memory>(
                sql,
                (memory, blob) =>
                {
                    if (!memoryDict.TryGetValue(memory.Id, out var existingMemory))
                    {
                        existingMemory = memory;
                        existingMemory.Blobs = new List<MemoryBlob>();
                        memoryDict[memory.Id] = existingMemory;
                    }

                    if (blob != null)
                        existingMemory.Blobs.Add(blob);

                    return existingMemory;
                },
                param: parameters,
                splitOn: "ID"
            );

            // Rank based on embedding similarity
            var results = new List<MemoryDTO>();
            foreach (var mem in memoryDict.Values)
            {
                foreach (var b in mem.Blobs)
                {
                    // Dequantize the stored embedding
                    float[] storedEmbedding = Dequantize(b.Embedding);

                    // Calculate the cosine similarity score
                    float score = OllamaEmbedder.CosineSimilarity(_logger, queryEmbedding, storedEmbedding);

                    if (score >= .75)
                    {


                        float normalizedImportance = ((int)mem.Rank.Value - 1f) / 4f;  // 1 → 0.0, 5 → 1.0
                        float importanceBoost = normalizedImportance * 0.2f;
                        float finalScore = score + importanceBoost;
                        // Adjust the score based on the importance of the memory
                        //if (mem.Importance >= 0.7f)
                        //{
                        //    score += 0.1f; // Increase score if importance is high
                        //}

                        // Add the memory and its score to the results
                        results.Add(new MemoryDTO
                        {
                            Id = mem.Id,
                            Text = mem.Text,
                            Score = finalScore // Store the adjusted score
                        });
                    }
                }
            }

            return results.OrderByDescending(x => x.Score).ToList();
        }

       

        public async Task<EchoFrontendV2.DTO.File> GetFile(string name)
        {
            string sql = "SELECT * FROM Files WHERE Source LIKE '%' || @Source || '%'";
            var parameters = new { Source = name };
            var result = await Connection.QueryFirstOrDefaultAsync<EchoFrontendV2.DTO.File>(sql, parameters);
            return result;

        }
        public async Task<int> CreateLesson(string text, int sourceID)
        {
            int newId = -1;             
            try
            {

                //generate name
                var lessonText = await LLMUtilityCalls.GetLessionFromConversation(text, _logger);

                var memory = new Lesson()
                {
                    Text = lessonText,
                    AddedBy = "system",
                    SourceMemoryID = sourceID                    
                };
                var sql = @"
                            INSERT INTO Lessons (Text, AddedBy, SourceMemoryID, TimeStamp)
                            VALUES (@Text, @AddedBy, @SourceMemoryID, @TimeStamp);
                            SELECT last_insert_rowid();";
                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, memory, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateCoreMemory(): " + ex.Message);
            }
            

            try
            {
                await CreateLessonBlobAsync(text, newId);
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateLessonBlobAsync(): " + ex.Message);
            }

            try { await TagLessonsAsync(text, "", "", newId); }
            catch (Exception ex)
            { _logger.LogException("MemoryDB:TagLessonsAsync(): " + ex.Message); }


            //_ = Task.Run(async () =>
            //{
            //    try { await CalculateImportance(newId); }
            //    catch (Exception ex) { _logger.LogException("MemoryDB:TagStaticMemoriesAsync(): " + ex.Message); }
            //});
            return newId;
        }
        public async Task<int> CreateCoreMemory(string text, string source)
        {
            int newId = -1;
            try
            {
                //generate name
                var name = await LLMUtilityCalls.GenerateSessionMemoryName(text, _logger);

                var memory = new CoreMemory()
                {
                    Name = name,
                    Type = "",
                    Content = text,
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Source = source,
                    Priority = 1,
                    Notes = ""
                };
                var sql = @"
                            INSERT INTO CoreMemory (Name, Type, Content, IsActive, Created, LastModified, Source, Priority, Notes)
                            VALUES (@Name, @Type, @Content, @IsActive, @Created, @LastModified, @Source, @Priority, @Notes);
                            SELECT last_insert_rowid();";
                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, memory, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateCoreMemory(): " + ex.Message);
            }            
            
                try { 
                    await CreateCoreBlobAsync(text, newId); 
                }
                catch (Exception ex)
                {
                    _logger.LogException("MemoryDB:CreateCoreBlobAsync(): " + ex.Message);
                }
                
            

            
                try { await TagCoreMemoriesAsync(text, "", "", newId); }
                catch (Exception ex)
                { _logger.LogException("MemoryDB:TagCoreMemoriesAsync(): " + ex.Message); }
                
            
            //_ = Task.Run(async () =>
            //{
            //    try { await CalculateImportance(newId); }
            //    catch (Exception ex) { _logger.LogException("MemoryDB:TagStaticMemoriesAsync(): " + ex.Message); }
            //});
            return newId;
        }
        public async Task<int> CreateFile(string source)
        {
            int newId = -1;
            try
            {


                var text = File.ReadAllText(source);

                var summary = await LLMQueue.EnqueueAndWait(async () =>
                {
                    return await LLMUtilityCalls.SummarizeFile(text,_logger);
                }, priority: 10);

                // Insert memory
                var sql = @"
                            INSERT INTO Files (Text, SummaryText, Source, Metadata_json, TimeStamp)
                            VALUES (@Text, @SummaryText, @Source, @Metadata_json, @TimeStamp);
                            SELECT last_insert_rowid();";

                var memory = new Memory()
                {
                    Text = text,
                    SummaryText = summary,
                    Source = source,
                    Metadata_json = "{ }",
                    TimeStamp = DateTime.UtcNow
                };

                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, memory, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateFile(): " + ex.Message);
            }
            return newId;
        }
        //public async Task<int> CreateImageFile(string source)
        //{
        //    int newId = -1;
        //    try
        //    {


        //        var text = File.ReadAllText(source);

        //        var summary = await LLMQueue.EnqueueAndWait(async () =>
        //        {
        //            return await LLMUtilityCalls.SummarizeFile(text, _logger);
        //        }, priority: 10);

        //        // Insert memory
        //        var sql = @"
        //                    INSERT INTO Images (Name, Tags, Description, FilePath)
        //                    VALUES (@Name, @Tags, @Description, @FilePath);
        //                    SELECT last_insert_rowid();";

        //        var memory = new EchoFrontendV2.DTO.Image
        //        {
        //            Description = de
        //        };

        //        using (var transaction = Connection.BeginTransaction())
        //        {
        //            newId = await Connection.ExecuteScalarAsync<int>(sql, memory, transaction);
        //            transaction.Commit();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogException("MemoryDB:CreateFile(): " + ex.Message);
        //    }
        //    return newId;
        //}
        public async Task<int> CreateFrameworkFile(int id, string name)
        {
            int newId = -1;
            try
            {

                // Insert memory
                var sql = @"
                            INSERT INTO Frameworks (Name, Description, Content, Tags, FileId)
                            VALUES (@Name,'','','', @FileId);
                            SELECT last_insert_rowid();";

                var framework = new Framework
                {
                    Name = name, 
                    FileId = id
                };

                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, framework, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateFrameworkFile(): " + ex.Message);
            }
            return newId;
        }

        public async Task<int> CreateBookFile(int id, string text)
        {
            int newId = -1;
            try
            {
                var summary = await LLMQueue.EnqueueAndWait(async () =>
                {
                    return await LLMUtilityCalls.SummarizeFile(text, _logger);
                }, priority: 10);
                var title = await LLMQueue.EnqueueAndWait(async () =>
                {
                    return await LLMUtilityCalls.TitleBook(text.Length > 500 ? text.Substring(0, 500) : text, _logger);
                }, priority: 10);
                var author = await LLMQueue.EnqueueAndWait(async () =>
                {
                    return await LLMUtilityCalls.AuthorBook(text.Length > 500 ? text.Substring(0, 500) : text, _logger);
                }, priority: 10);

                var tags = Tagging.CombinedTagger.TagMessage(summary);
                tags.Add("book");
                // Insert memory
                var sql = @"
                            INSERT INTO Books (Title,Author,Summary, Tags, FileId)
                            VALUES (@Title,@Author,@Summary, @Tags, @FileId);
                            SELECT last_insert_rowid();";

                var framework = new Book
                {
                    Author = author,
                    Title = title,
                    Summary = summary,
                    Tags = string.Join(", ", tags),
                    FileId = id
                };

                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, framework, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateFrameworkFile(): " + ex.Message);
            }
            return newId;
        }

        public async Task<int> CreateMemoryAsync(string text, string source, int sessionID, string metadata, DateTime timestamp)
        {
            int newId;
            string summary;

            try
            {
                if (NoiseCheck.Check(text))
                    summary = text;
                else
                {
                    summary = await LLMQueue.EnqueueAndWait(async () =>
                    {
                        return await LLMUtilityCalls.SummarizeMemory(text, _logger);
                    }, priority: 10);
                }
                // Insert memory
                var sql = @"
                            INSERT INTO Memories (Text, SummaryText, SessionID, Importance, Source, Metadata_json)
                            VALUES (@Text, @SummaryText,@SessionID, @Importance, @Source, @Metadata_json);
                            SELECT last_insert_rowid();";

                var memory = new Memory()
                {
                    Text = text,
                    SummaryText = summary,
                    SessionID = sessionID,
                    Importance = 0.0,
                    Source = source,
                    Metadata_json = metadata,
                    TimeStamp = timestamp
                };

                using (var transaction = Connection.BeginTransaction())
                {
                    newId = await Connection.ExecuteScalarAsync<int>(sql, memory, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:CreateMemoryAsync(): " + ex.Message);
                return -1;
            }

            // 🚀 Run the heavy stuff safely in parallel AFTER commit
            await LLMQueue.EnqueueAndWait(async () =>            
            {
                try { await CreateBlobAsync(summary, newId); }
                catch (Exception ex) { _logger.LogException("MemoryDB:CreateBlobAsync(): " + ex.Message); }
                return true;
            },priority:10);


            await LLMQueue.EnqueueAndWait(async () =>
            {
                try { await TagMemoriesAsync(text, "", "", newId); }
                catch (Exception ex) { _logger.LogException("MemoryDB:TagMemoriesAsync(): " + ex.Message); }
                return true;
            }, priority: 10);

            await LLMQueue.EnqueueAndWait(async () =>
            {
                try { await CalculateImportance(newId); }
                catch (Exception ex) { _logger.LogException("MemoryDB:CalculateImportance(): " + ex.Message); }
                return true;
            }, priority: 10);

            return newId;
        }
        public async Task CreateCoreMemoryTags(string summarizedText, int id)
        {
            var tags = Tagging.CombinedTagger.TagMessage(summarizedText);
            foreach (var t in tags)
            {
                float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(t);

                var tag = new Tag
                {
                    Name = t,
                    Type = "",
                    Description = t,
                    Embedding = Quantize(embedding),
                    TimeStamp = DateTime.UtcNow
                };

                using var conn = new SqliteConnection("Data Source=MemoryDatabase.db");
                await conn.OpenAsync();

                await conn.ExecuteAsync(
                    "INSERT OR IGNORE INTO Tags (Name, Embedding, Type, Description, TimeStamp) VALUES (@Name, @Embedding, @Type, @Description, @TimeStamp);",
                    tag
                );

                int tagId = await conn.ExecuteScalarAsync<int>(
                    "SELECT ID FROM Tags WHERE Name = @Name;",
                    new { Name = tag.Name });

                await conn.ExecuteAsync(
                    "INSERT OR IGNORE INTO CoreMemoryTags (CoreMemoryID, TagID) VALUES (@MemID, @TagID);",
                    new { MemID = id, TagID = tagId });
            }
            //string normalizedTag = NormalizeTag(rawTag.Trim());

        }
        public async Task CreateTags(string summarizedText, int id)
        {
            var tags = Tagging.CombinedTagger.TagMessage(summarizedText);
            foreach(var t in tags)
            {
                float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(t);

                var tag = new Tag
                {
                    Name = t,
                    Type = "",
                    Description = t,
                    Embedding = Quantize(embedding),
                    TimeStamp = DateTime.UtcNow
                };

                using var conn = new SqliteConnection("Data Source=MemoryDatabase.db");
                await conn.OpenAsync();

                await conn.ExecuteAsync(
                    "INSERT OR IGNORE INTO Tags (Name, Embedding, Type, Description, TimeStamp) VALUES (@Name, @Embedding, @Type, @Description, @TimeStamp);",
                    tag
                );

                int tagId = await conn.ExecuteScalarAsync<int>(
                    "SELECT ID FROM Tags WHERE Name = @Name;",
                    new { Name = tag.Name });

                await conn.ExecuteAsync(
                    "INSERT OR IGNORE INTO MemoryTags (MemoryID, TagID) VALUES (@MemID, @TagID);",
                    new { MemID = id, TagID = tagId });
            }
            //string normalizedTag = NormalizeTag(rawTag.Trim());
            
        }
        private async Task TagMemoriesAsync(string text, string type, string desc, int memid)
        {
            try
            {
                var tags = Tagging.CombinedTagger.TagMessage(text);                

                var tagTasks = tags.Select(async rawTag =>
                {                    
                    float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(rawTag);

                    var tag = new Tag
                    {
                        Name = rawTag,
                        Type = type,
                        Description = desc,
                        Embedding = Quantize(embedding),
                        TimeStamp = DateTime.UtcNow
                    };

                    using var conn = new SqliteConnection("Data Source=MemoryDatabase.db");
                    await conn.OpenAsync();

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO Tags (Name, Embedding, Type, Description, TimeStamp) VALUES (@Name, @Embedding, @Type, @Description, @TimeStamp);",
                        tag
                    );

                    int tagId = await conn.ExecuteScalarAsync<int>(
                        "SELECT ID FROM Tags WHERE Name = @Name;",
                        new { Name = tag.Name });

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO MemoryTags (MemoryID, TagID) VALUES (@MemID, @TagID);",
                        new { MemID = memid, TagID = tagId });
                });

                await Task.WhenAll(tagTasks);
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:TagMemoriesAsync(): " + ex.Message);
            }
        }

        private async Task TagCoreMemoriesAsync(string text, string type, string desc, int memid)
        {
            try
            {                
                var tags = Tagging.CombinedTagger.TagMessage(text);
                var tagTasks = tags.Select(async rawTag =>
                {
                    float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(rawTag);

                    var tag = new Tag
                    {
                        Name = rawTag,
                        Type = type,
                        Description = desc,
                        Embedding = Quantize(embedding),
                        TimeStamp = DateTime.UtcNow
                    };

                    using var conn = new SqliteConnection("Data Source=MemoryDatabase.db");
                    await conn.OpenAsync();

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO Tags (Name, Embedding, Type, Description, TimeStamp) VALUES (@Name, @Embedding, @Type, @Description, @TimeStamp);",
                        tag
                    );

                    int tagId = await conn.ExecuteScalarAsync<int>(
                        "SELECT ID FROM Tags WHERE Name = @Name;",
                        new { Name = tag.Name });

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO CoreMemoryTags (CoreMemoryID, TagID) VALUES (@MemID, @TagID);",
                        new { MemID = memid, TagID = tagId });
                });

                await Task.WhenAll(tagTasks);
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:TagStaticMemoriesAsync(): " + ex.Message);
            }
        }

        private async Task TagLessonsAsync(string text, string type, string desc, int memid)
        {
            try
            {
                var tags = Tagging.CombinedTagger.TagMessage(text);

                var tagTasks = tags.Select(async rawTag =>
                {                    
                    float[] embedding = await EmbeddedSystem.GetEmbeddingAsync(rawTag);

                    var tag = new Tag
                    {
                        Name = rawTag,
                        Type = type,
                        Description = desc,
                        Embedding = Quantize(embedding),
                        TimeStamp = DateTime.UtcNow
                    };

                    using var conn = new SqliteConnection("Data Source=MemoryDatabase.db");
                    await conn.OpenAsync();

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO Tags (Name, Embedding, Type, Description, TimeStamp) VALUES (@Name, @Embedding, @Type, @Description, @TimeStamp);",
                        tag
                    );

                    int tagId = await conn.ExecuteScalarAsync<int>(
                        "SELECT ID FROM Tags WHERE Name = @Name;",
                        new { Name = tag.Name });

                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO LessonTags (LessonID, TagID) VALUES (@LessID, @TagID);",
                        new { LessID = memid, TagID = tagId });
                });

                await Task.WhenAll(tagTasks);
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:TagLessonsAsync(): " + ex.Message);
            }
        }

        public async Task DumpConversationToMemory(SessionManager sessionManager, bool fullList = false)
        {
            if (CurrentlySaving)
                return;

            CurrentlySaving = true;

            try
            {                
                var undumped = sessionManager.GetCurrentSessionMessages()
                                             .Where(m => !m.Dumped)
                                             .OrderBy(m => m.TimeStamp)
                                             .ToList();

                foreach (var message in undumped)
                {
                    await CreateMemoryAsync(message.Content, message.Role, sessionManager.SessionId, "{}", message.TimeStamp);
                    message.Dumped = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("MemoryDB:DumpSessionToMemory(): " + ex.Message);
            }
            finally
            {
                CurrentlySaving = false;
            }
        }

        public List<Session> GetSessions()
        {
            var items = Connection.Query<Session>("SELECT * FROM Sessions");
            return items.ToList();
        }
        public List<Memory> GetMemoriesBySession(int sessionID)
        {
            var items = Connection.Query<Memory>("SELECT * FROM Memories WHERE SessionID = @SessionID", new { SessionID = sessionID });
            return items.ToList();
        }
        public async Task<List<Session>> GetPreviousSessions(int currentSessionID)
        {
            // Calculate the previous three session IDs
            int previousSessionID1 = currentSessionID - 1;
            int previousSessionID2 = currentSessionID - 2;
            int previousSessionID3 = currentSessionID - 3;

            // Create a list of session IDs
            var sessionIDs = new List<int> { previousSessionID1, previousSessionID2, previousSessionID3 };

            // Use a SQL query to select the sessions with the specified IDs
            var sessions = await Connection.QueryAsync<Session>(
                "SELECT * FROM Sessions WHERE ID IN @SessionIDs;",
                new { SessionIDs = sessionIDs });

            return sessions.ToList();
        }

        public async Task<int> CreateNewSession(bool useCPU = true)
        {
            var sql = "SELECT MAX(ID) FROM Sessions";
            var maxSessionId = Connection.ExecuteScalar<int?>(sql);
            var id = (maxSessionId ?? 0) + 1;

            var sess = new Session
            {
                ID = id,
                Title = "Current Session",
                CreatedAt = DateTime.UtcNow
            };
            await Connection.ExecuteAsync(
                   "INSERT INTO Sessions (ID, Title, CreatedAt) VALUES (@ID, @Title, @CreatedAt);",
                   sess
               );
            return id;
        }

        public async Task CreateSessionSummary(int id, bool useCPU = true)
        {
            int chunkSize = 20;
            var memories = GetMemoriesBySession(id);
            StringBuilder sb = new StringBuilder();
            string summary = "";

            if (memories.Count > chunkSize)
            {
                StringBuilder chunkSummary = new StringBuilder();

                for (int i = 0; i < memories.Count; i += chunkSize)
                {
                    var chunk = memories.Skip(i).Take(chunkSize);
                    foreach (var m in chunk)
                        sb.AppendLine(m.SummaryText);

                    // Summarize the chunk
                    chunkSummary.AppendLine(await LLMUtilityCalls.SummarizeSessionConversationSummaries(sb.ToString(), _logger));
                    sb.Clear();
                }
                summary = await LLMUtilityCalls.SummarizeSessionConversationSummaries(chunkSummary.ToString(), _logger);
            }
            else
            {
                foreach (var m in memories)
                    sb.AppendLine(m.SummaryText);

                summary = await LLMUtilityCalls.SummarizeSessionConversation(sb.ToString(), _logger);
            }

            var title = await LLMUtilityCalls.GenerateSessionConversationTitle(summary, _logger);

            var sess = new Session
            {
                ID = id,
                Title = title,
                Summary = summary
            };
            await Connection.ExecuteAsync(
                   "UPDATE Sessions\r\nSET Title = @Title, Summary = @Summary\r\nWHERE ID = @ID;",
                   sess
               );
        }

        public IEnumerable<Memory> GetAllMemories()
        {
            return Connection.Query<Memory>("SELECT * FROM Memories order by TimeStamp");
        }
        public IEnumerable<CoreMemory> GetAllCoreMemories()
        {
            return Connection.Query<CoreMemory>("SELECT * FROM CoreMemory order by Created");
        }
    }
}
