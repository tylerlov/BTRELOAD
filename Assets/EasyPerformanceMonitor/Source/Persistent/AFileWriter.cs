// Microsoft
using System;
using System.Threading.Tasks;

namespace GUPS.EasyPerformanceMonitor.Persistent
{
    /// <summary>
    /// An abstract base class for writing objects of type <typeparamref name="T"/> to a file.
    /// </summary>
    /// <typeparam name="T">The type of objects to be written to the file.</typeparam>
    /// <remarks>
    /// The <see cref="AFileWriter{T}"/> class provides a common interface for writing objects to a file, 
    /// ensuring consistency in file writing operations. It includes methods for synchronous and asynchronous 
    /// writing, flushing, and proper disposal of resources.
    /// </remarks>
    internal abstract class AFileWriter<T> : IDisposable
    {
        /// <summary>
        /// Gets the file path associated with the file writer.
        /// </summary>
        public String Path { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AFileWriter{T}"/> class with the specified file path.
        /// </summary>
        /// <param name="_Path">The path of the file to be written.</param>
        public AFileWriter(String _Path)
        {
            this.Path = _Path;
        }

        /// <summary>
        /// Writes the specified object of type <typeparamref name="T"/> to the file.
        /// </summary>
        /// <param name="_Object">The object to be written to the file.</param>
        public abstract void Write(T _Object);

        /// <summary>
        /// Asynchronously writes the specified object of type <typeparamref name="T"/> to the file.
        /// </summary>
        /// <param name="_Object">The object to be asynchronously written to the file.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public abstract Task WriteAsync(T _Object);

        /// <summary>
        /// Flushes any buffered data to the file.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Asynchronously flushes any buffered data to the file.
        /// </summary>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public abstract Task FlushAsync();

        /// <summary>
        /// Releases the resources used by the file writer.
        /// </summary>
        public abstract void Dispose();
    }
}
