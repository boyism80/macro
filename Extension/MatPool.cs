using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace macro.Extension
{
    /// <summary>
    /// Thread-safe Mat queue with idle/busy state separation
    /// Uses manual locking for true thread safety across all operations
    /// </summary>
    public class ConcurrentMatQueue
    {
        private readonly Queue<Mat> _idle = new Queue<Mat>();
        private readonly HashSet<Mat> _busy = new HashSet<Mat>();
        private readonly string _poolKey;
        private readonly object _lock = new object();
        private const int MAX_QUEUE_SIZE = 10;

        public ConcurrentMatQueue(string poolKey)
        {
            _poolKey = poolKey;
        }

        /// <summary>
        /// Get Mat from idle queue or create new one
        /// Moves Mat to busy state for tracking - fully thread-safe
        /// </summary>
        public Mat Get(int rows, int cols, MatType type)
        {
            lock (_lock)
            {
                // Try to get from idle queue first
                while (_idle.Count > 0)
                {
                    var mat = _idle.Dequeue();

                    if (!mat.IsDisposed && mat.Rows == rows && mat.Cols == cols && mat.Type() == type)
                    {
                        _busy.Add(mat);
                        return mat;
                    }

                    // Dispose invalid mat
                    mat.Dispose();
                }

                // Create new Mat if none available
                var newMat = new Mat(rows, cols, type);
                _busy.Add(newMat);
                return newMat;
            }
        }

        /// <summary>
        /// Return Mat to idle queue
        /// Validates that Mat was in busy state - fully thread-safe
        /// </summary>
        public void Return(Mat mat)
        {
            if (mat == null || mat.IsDisposed)
                return;

            lock (_lock)
            {
                // Validate Mat dimensions match this queue
                var expectedKey = $"{mat.Rows}_{mat.Cols}_{(int)mat.Type()}";
                if (expectedKey != _poolKey)
                {
                    throw new InvalidOperationException(
                        $"Mat returned to wrong pool. Expected: {_poolKey}, Actual: {expectedKey}");
                }

                // Check if Mat is in busy state
                if (!_busy.Contains(mat))
                {
                    throw new InvalidOperationException(
                        $"Attempting to return Mat that is not in busy state or was not obtained from this pool. Pool: {_poolKey}");
                }

                // Remove from busy and add to idle (if space available)
                _busy.Remove(mat);

                if (_idle.Count < MAX_QUEUE_SIZE)
                {
                    _idle.Enqueue(mat);
                }
                else
                {
                    mat.Dispose();
                }
            }
        }

        /// <summary>
        /// Get current queue sizes for debugging - thread-safe
        /// </summary>
        public (int Idle, int Busy) GetCounts()
        {
            lock (_lock)
            {
                return (_idle.Count, _busy.Count);
            }
        }

        /// <summary>
        /// Clear all Mats in this queue - thread-safe
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                while (_idle.Count > 0)
                {
                    var mat = _idle.Dequeue();
                    mat.Dispose();
                }

                // Dispose all busy Mats as well
                foreach (var mat in _busy)
                {
                    mat.Dispose();
                }

                _busy.Clear();
            }
        }
    }

    /// <summary>
    /// Global Mat object pool manager for GC optimization
    /// Reduces memory allocation overhead by reusing Mat objects
    /// Thread-safe with idle/busy state tracking for debugging
    /// </summary>
    public static class MatPool
    {
        // Size-based Mat pools (rows_cols_type -> ConcurrentMatQueue)
        private static readonly ConcurrentDictionary<string, ConcurrentMatQueue> _pools = new ConcurrentDictionary<string, ConcurrentMatQueue>();

        // Generate pool key for Mat dimensions and type
        private static string GetKey(int rows, int cols, MatType type)
        {
            return $"{rows}_{cols}_{(int)type}";
        }

        /// <summary>
        /// Get PooledMat object from pool or create new one if not available
        /// Performance: Avoids frequent Mat allocation/deallocation
        /// Thread-safe with busy state tracking
        /// </summary>
        public static PooledMat Get(int rows, int cols, MatType type)
        {
            var key = GetKey(rows, cols, type);

            if (!_pools.TryGetValue(key, out var queue))
            {
                queue = new ConcurrentMatQueue(key);
                _pools[key] = queue;
            }

            var mat = queue.Get(rows, cols, type);
            return PooledMat.FromPool(mat);
        }

        /// <summary>
        /// Internal method for returning Mat to pool without validation
        /// Used by PooledMat to avoid circular calls
        /// </summary>
        internal static void Return(Mat mat)
        {
            if (mat == null || mat.IsDisposed)
                return;

            var key = GetKey(mat.Rows, mat.Cols, mat.Type());

            if (_pools.TryGetValue(key, out var queue))
            {
                queue.Return(mat);
            }
            else
            {
                mat.Dispose();
            }
        }

        /// <summary>
        /// Get PooledMat with same dimensions and type as source
        /// Performance: Optimized for template matching operations
        /// </summary>
        public static PooledMat GetLike(Mat source)
        {
            if (source == null || source.IsDisposed)
                return null;

            return Get(source.Rows, source.Cols, source.Type());
        }

        /// <summary>
        /// Get cloned PooledMat from pool (replaces Mat.Clone())
        /// Performance: Reuses pooled Mat instead of allocating new one
        /// </summary>
        public static PooledMat GetClone(Mat source)
        {
            if (source == null || source.IsDisposed)
                return null;

            var result = Get(source.Rows, source.Cols, source.Type());
            source.CopyTo(result);
            return result;
        }



        /// <summary>
        /// Clear all pools on application shutdown
        /// Performance: Prevents memory leaks
        /// </summary>
        public static void Clear()
        {
            foreach (var queue in _pools.Values)
            {
                queue.Clear();
            }

            _pools.Clear();
        }

        /// <summary>
        /// Get debug information about all pools
        /// </summary>
        public static Dictionary<string, (int Idle, int Busy)> GetPoolStats()
        {
            var stats = new Dictionary<string, (int, int)>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = kvp.Value.GetCounts();
            }
            return stats;
        }
    }
}