using System;
using OpenCvSharp;

namespace macro.Extension
{
    /// <summary>
    /// Mat wrapper that automatically returns to MatPool when disposed
    /// This eliminates the need for manual MatPool.Return() calls
    /// </summary>
    public class PooledMat : Mat
    {
        private readonly Mat _underlyingMat;
        private bool _isFromPool;
        private bool _disposed = false;

        /// <summary>
        /// Private constructor - use static factory methods instead
        /// </summary>
        /// <param name="mat">Mat from pool</param>
        /// <param name="isFromPool">Whether this Mat came from MatPool</param>
        private PooledMat(Mat mat, bool isFromPool) : base(mat.Rows, mat.Cols, mat.Type())
        {
            _underlyingMat = mat;
            _isFromPool = isFromPool;
            // Copy data from underlying mat
            mat.CopyTo(this);
        }

        /// <summary>
        /// Private constructor for creating independent PooledMat
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <param name="type">Mat type</param>
        private PooledMat(int rows, int cols, MatType type) : base(rows, cols, type)
        {
            _underlyingMat = null;
            _isFromPool = false;
        }

        /// <summary>
        /// Creates a PooledMat from pool (will be returned to pool on dispose)
        /// </summary>
        /// <param name="mat">Mat from MatPool</param>
        /// <returns>PooledMat that returns to pool on dispose</returns>
        internal static PooledMat FromPool(Mat mat)
        {
            return new PooledMat(mat, true);
        }

        /// <summary>
        /// Creates a weak reference PooledMat (won't be returned to pool on dispose)
        /// Use this for referencing existing Mat objects without pool management
        /// Similar to C++ weak_ptr - doesn't own the resource
        /// </summary>
        /// <param name="mat">Existing Mat to reference</param>
        /// <returns>PooledMat that won't return to pool on dispose</returns>
        public static PooledMat AsReference(Mat mat)
        {
            return new PooledMat(mat, false);
        }

        /// <summary>
        /// Alias for AsReference - creates a weak reference PooledMat
        /// </summary>
        /// <param name="mat">Existing Mat to reference</param>
        /// <returns>PooledMat that won't return to pool on dispose</returns>
        public static PooledMat Weak(Mat mat)
        {
            return AsReference(mat);
        }

        /// <summary>
        /// Creates an independent PooledMat with specified dimensions
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <param name="type">Mat type</param>
        /// <returns>Independent PooledMat</returns>
        public static new PooledMat Create(int rows, int cols, MatType type)
        {
            return new PooledMat(rows, cols, type);
        }

        /// <summary>
        /// Marks this Mat as not from pool (won't be returned to pool on dispose)
        /// </summary>
        public void MarkAsNonPooled()
        {
            _isFromPool = false;
        }

        /// <summary>
        /// Creates a clone that will also be returned to pool when disposed
        /// </summary>
        /// <returns>Cloned PooledMat</returns>
        public new PooledMat Clone()
        {
            return MatPool.GetClone(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _isFromPool && _underlyingMat != null && !_underlyingMat.IsDisposed)
                {
                    // Copy data back to underlying mat before returning to pool
                    this.CopyTo(_underlyingMat);
                    MatPool.Return(_underlyingMat);
                }

                // Always dispose the wrapper
                base.Dispose(disposing);
                _disposed = true;
            }
        }

        /// <summary>
        /// Force disposal without returning to pool
        /// Used when MatPool needs to actually dispose the Mat
        /// </summary>
        internal void ForceDispose()
        {
            _isFromPool = false;
            base.Dispose(true);
        }
    }
}