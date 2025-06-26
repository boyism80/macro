using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace macro.Extension
{
    /// <summary>
    /// Global Mat object pool manager for GC optimization
    /// Reduces memory allocation overhead by reusing Mat objects
    /// </summary>
    public static class MatPool
    {
        // Size-based Mat pools (rows_cols_type -> Mat pool)
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<Mat>> _pools = new ConcurrentDictionary<string, ConcurrentQueue<Mat>>();
        
        // Maximum objects per pool to prevent memory bloat
        private const int MAX_POOL_SIZE = 10;
        
        // Generate pool key for Mat dimensions and type
        private static string GetKey(int rows, int cols, MatType type)
        {
            return $"{rows}_{cols}_{(int)type}";
        }
        
        /// <summary>
        /// Get Mat object from pool or create new one if not available
        /// Performance: Avoids frequent Mat allocation/deallocation
        /// </summary>
        public static Mat Get(int rows, int cols, MatType type)
        {
            var key = GetKey(rows, cols, type);
            
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new ConcurrentQueue<Mat>();
                _pools[key] = pool;
            }
            
            if (pool.TryDequeue(out var mat))
            {
                if (!mat.IsDisposed && mat.Rows == rows && mat.Cols == cols && mat.Type() == type)
                {
                    return mat;
                }
                
                // Dispose if wrong size or type
                mat.Dispose();
            }
            
            return new Mat(rows, cols, type);
        }
        
        /// <summary>
        /// Return Mat object to pool for reuse
        /// Performance: Enables object reuse instead of GC collection
        /// </summary>
        public static void Return(Mat mat)
        {
            if (mat == null || mat.IsDisposed)
                return;
                
            var key = GetKey(mat.Rows, mat.Cols, mat.Type());
            
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new ConcurrentQueue<Mat>();
                _pools[key] = pool;
            }
            
            // Limit pool size to prevent memory bloat
            if (pool.Count < MAX_POOL_SIZE)
            {
                pool.Enqueue(mat);
            }
            else
            {
                mat.Dispose();
            }
        }
        
        /// <summary>
        /// Get Mat with same dimensions and type as source
        /// Performance: Optimized for template matching operations
        /// </summary>
        public static Mat GetLike(Mat source)
        {
            if (source == null || source.IsDisposed)
                return null;
                
            return Get(source.Rows, source.Cols, source.Type());
        }
        
        /// <summary>
        /// Get cloned Mat from pool (replaces Mat.Clone())
        /// Performance: Reuses pooled Mat instead of allocating new one
        /// </summary>
        public static Mat GetClone(Mat source)
        {
            if (source == null || source.IsDisposed)
                return null;
                
            var result = Get(source.Rows, source.Cols, source.Type());
            source.CopyTo(result);
            return result;
        }
        
        /// <summary>
        /// Get ROI Mat from pool (replaces new Mat(source, roi))
        /// Performance: Avoids allocation for region-of-interest operations
        /// </summary>
        public static Mat GetRoi(Mat source, Rect roi)
        {
            if (source == null || source.IsDisposed)
                return null;
                
            var result = Get(roi.Height, roi.Width, source.Type());
            using (var roiMat = new Mat(source, roi))
            {
                roiMat.CopyTo(result);
            }
            return result;
        }
        
        /// <summary>
        /// Clear all pools on application shutdown
        /// Performance: Prevents memory leaks
        /// </summary>
        public static void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.TryDequeue(out var mat))
                {
                    mat.Dispose();
                }
            }
            
            _pools.Clear();
        }
    }
} 