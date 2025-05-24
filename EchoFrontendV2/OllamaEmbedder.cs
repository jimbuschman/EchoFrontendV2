using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using EchoFrontendV2;

namespace TestSQLLite
{


    public class OllamaEmbedder
    {
        private readonly HttpClient _httpClient = new();
        private const string OllamaUrl = "http://localhost:11434/api/embeddings";
        private readonly RealtimeLogger _logger;
        public OllamaEmbedder(RealtimeLogger logger)
        {
            _logger = logger;
        }

        private class OllamaEmbeddingResponse
        {
            public float[] embedding { get; set; }
        }
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogException("OllamaEmbedder:GetEmbeddingAsync(): Text cannot be null or empty");                
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            }
            var request = new
            {
                model = "nomic-embed-text",
                prompt = text,
                options = new
                {
                    // Add any required options here to ensure consistent output
                    embedding_only = true , // If supported by your model
                    num_gpu = 0
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(OllamaUrl, request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(json)
                    ?? throw new Exception("Null response from embedding service");

                if (result.embedding == null || result.embedding.Length == 0)
                    throw new Exception("Empty embedding returned");

                // Optional: Validate expected dimension
                const int EXPECTED_DIMENSION = 768; // Set your expected dimension
                if (result.embedding.Length != EXPECTED_DIMENSION)
                {
                    throw new Exception($"Unexpected embedding dimension: {result.embedding.Length} (expected {EXPECTED_DIMENSION})");
                }

                return result.embedding;
            }
            catch (Exception ex)
            {
                _logger.LogException("OllamaEmbedder:GetEmbeddingAsync(): "+ex.ToString());
                throw new Exception($"Failed to generate embedding for text: {text}", ex);
            }
        }

        // Convert float[] to SQLite BLOB
        private static byte[] ConvertToBlob(float[] embedding)
        {
            byte[] bytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        // Convert BLOB back to float[]
        public static float[] ConvertFromBlob(byte[] blob)
        {
            float[] embedding = new float[blob.Length / sizeof(float)];
            Buffer.BlockCopy(blob, 0, embedding, 0, blob.Length);
            return embedding;
        }

        public static float CosineSimilarity(RealtimeLogger logger, float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                logger.LogException("OllamaEmbedder:CosineSimilarity(): Arrays must be the same length");
                throw new ArgumentException("Arrays must be the same length");
            }

            float dot = 0, magA = 0, magB = 0;

            // Check if Vector<T> is hardware accelerated and vectors are large enough
            if (Vector.IsHardwareAccelerated && a.Length >= Vector<float>.Count)
            {
                int vectorSize = Vector<float>.Count;
                int vectorizableLength = a.Length - (a.Length % vectorSize);

                // Process chunks that fit into SIMD vectors
                for (int i = 0; i < vectorizableLength; i += vectorSize)
                {
                    Vector<float> va = new Vector<float>(a, i);
                    Vector<float> vb = new Vector<float>(b, i);

                    dot += Vector.Dot(va, vb);
                    magA += Vector.Dot(va, va);
                    magB += Vector.Dot(vb, vb);
                }

                // Process remaining elements
                for (int i = vectorizableLength; i < a.Length; i++)
                {
                    dot += a[i] * b[i];
                    magA += a[i] * a[i];
                    magB += b[i] * b[i];
                }
            }
            else
            {
                // Fall back to regular loop for small arrays or when SIMD isn't available
                for (int i = 0; i < a.Length; i++)
                {
                    dot += a[i] * b[i];
                    magA += a[i] * a[i];
                    magB += b[i] * b[i];
                }
            }

            float denominator = MathF.Sqrt(magA) * MathF.Sqrt(magB);

            // Handle zero vectors
            return denominator > 0 ? dot / denominator : 0;
        }
    }
}
