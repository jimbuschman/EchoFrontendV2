using EchoFrontendV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSQLLite
{
    public class MemoryManager
    {
        public int GlobalTokenBudget { get; set; }
        public const int OVERHEAD_TOKENS = 1000;
        private readonly RealtimeLogger _logger;
        public class PoolConfig
        {
            public double Percentage { get; set; }
            public int? HardCap { get; set; } // Optional
            public int RolloverPriority { get; set; } = 2;
        }

        private readonly Dictionary<string, MemoryPool> _pools = new();
        private readonly Dictionary<string, PoolConfig> _config = new();

        public MemoryManager(RealtimeLogger logger, int globalBudget)
        {
            _logger = logger;
            GlobalTokenBudget = globalBudget;
        }

        public void ConfigurePool(string name, double percentage, int priority, int? hardCap = null)
        {
            _config[name] = new PoolConfig { Percentage = percentage, HardCap = hardCap, RolloverPriority = priority};
        }

        public void InitializePools()
        {
            int totalAllocated = 0;

            foreach (var (name, cfg) in _config)
            {
                int baseBudget = (int)(GlobalTokenBudget * cfg.Percentage);
                int cappedBudget = cfg.HardCap.HasValue ? Math.Min(baseBudget, cfg.HardCap.Value) : baseBudget;

                _pools[name] = new MemoryPool(name, cappedBudget, cfg.HardCap);
                totalAllocated += cappedBudget;
            }

            // Rollover handling
            int unusedTokens = GlobalTokenBudget - totalAllocated;
            if (unusedTokens > 0)
            {
                // Sort pools by rollover priority (3 = highest)
                var expandable = _config
                    .Where(kvp => kvp.Value.RolloverPriority > 0)
                    .OrderByDescending(kvp => kvp.Value.RolloverPriority)
                    .Select(kvp => kvp.Key)
                    .ToList();

                int perPoolBonus = unusedTokens / Math.Max(1, expandable.Count);
                foreach (var poolName in expandable)
                {
                    _pools[poolName].MaxTokenBudget += perPoolBonus;
                }
            }
        }

        public void AddMemory(string poolName, MemoryItem item)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                if(pool.UsedTokens + item.EstimatedTokens > pool.MaxTokenBudget)
                {
                    TrimPool(poolName);
                }
                pool.Add(item);
            }
        }

        public List<MemoryItem> GatherMemory(int tokenBudget)
        {
            int remaining = tokenBudget;
            var result = new List<MemoryItem>();

            foreach (var pool in _pools.Values)
            {
                var entries = pool.GetTopEntries(remaining);
                foreach (var entry in entries)
                {
                    if (remaining >= entry.EstimatedTokens)
                    {
                        result.Add(entry);
                        remaining -= entry.EstimatedTokens;
                    }
                }
            }

            return result;
        }

        public void TrimPool(string poolName)//, Action<List<MemoryItem>> onSummarize)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                while (pool.UsedTokens > pool.MaxTokenBudget)
                {
                    var oldest = pool.GetOldestItems(4); // you’d need to add this method
                    if (!oldest.Any()) break;

                    if (poolName == "ActiveSession")
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await GetSummary(oldest); }
                            catch (Exception ex) { _logger.LogException("MemoryManager:TrimPool(): " + ex.ToString()); }
                        });
                    }
                    pool.RemoveItems(oldest);                    
                }
            }            
        }

        public async Task GetSummary(List<MemoryItem> messages)
        {
            string summary = string.Empty;
            string text = string.Empty;
            foreach (var message in messages)
            {
                text += message.Text + " ";
            }
            if (!string.IsNullOrWhiteSpace(text))
            {

                summary = await LLMUtilityCalls.SummarizeSessionChunk(text,_logger,text);

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    OllamaChat.memoryManager.AddMemory("RecentHistory", new MemoryItem { Text = summary, EstimatedTokens = TokenEstimator.EstimateTokens(summary), SessionRole = "system", PriorityScore = 1 });
                    _logger.LogTrack("Summarized Old Session Data:");
                    _logger.LogTrack("-" + summary);
                }
            }

        }           

        public void PrintUsage()
        {            
            _logger.LogWarning("=== MEMORY USAGE ===");
            foreach (var pool in _pools.Values)
            {
                _logger.LogWarning($"{pool.Name,-15}: {pool.UsedTokens} / {pool.MaxTokenBudget} tokens used");                
            }
            _logger.LogWarning("=====================");
        }
    }


    public class MemoryItem
    {
        public int SessionID { get; set; }
        public string Text { get; set; }
        public int EstimatedTokens { get; set; }
        public double PriorityScore { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string SessionRole { get; set; }
    }

    public class MemoryPool
    {
        public string Name { get; }
        public int MaxTokenBudget { get; set; }
        public int? HardCapTokens { get; set; }

        private readonly List<MemoryItem> _items = new();

        public MemoryPool(string name, int maxTokens, int? hardCap = null)
        {
            Name = name;
            MaxTokenBudget = maxTokens;
            HardCapTokens = hardCap;
        }

        public void Add(MemoryItem item)
        {
            if (_items.Any(s=> s.Text == item.Text))
                return;
            _items.Add(item);
            _items.Sort((a, b) => b.PriorityScore.CompareTo(a.PriorityScore));
        }

        public List<MemoryItem> GetTopEntries(int availableTokens)
        {
            var selected = new List<MemoryItem>();
            int used = 0;

            // Respect the smaller of external availableTokens or this pool's cap
            int effectiveCap = Math.Min(availableTokens, HardCapTokens ?? MaxTokenBudget);

            foreach (var item in _items)
            {
                if (used + item.EstimatedTokens <= effectiveCap)
                {
                    selected.Add(item);
                    used += item.EstimatedTokens;
                }
                else break;
            }

            return selected;
        }
        public List<MemoryItem> GetOldestItems(int count)
        {
            return _items.OrderBy(s => s.TimeStamp).Take(count).ToList();
        }

        public void RemoveItems(List<MemoryItem> itemsToRemove)
        {
            foreach (var item in itemsToRemove)
                _items.Remove(item);
        }
        public int UsedTokens => _items.Sum(i => i.EstimatedTokens);
    }


}
