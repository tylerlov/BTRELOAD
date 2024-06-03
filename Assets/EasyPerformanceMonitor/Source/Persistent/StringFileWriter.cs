// Microsoft
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GUPS.EasyPerformanceMonitor.Persistent
{
    /// <summary>
    /// A file writer specifically designed for writing lines of text to a file using a <see cref="StringBuilder"/> buffer.
    /// </summary>
    /// <remarks>
    /// The <see cref="StringFileWriter"/> extends the abstract class <see cref="AFileWriter{T}"/>, providing methods to write
    /// lines of text to a file. It uses a <see cref="StringBuilder"/> buffer to accumulate lines before flushing them to the file,
    /// optimizing file-writing operations. The flushing behavior is controlled by a specified flush count, ensuring efficient
    /// use of resources.
    /// </remarks>
    internal class StringFileWriter : AFileWriter<String>
    {
        /// <summary>
        /// The lock object for the string builder to ensure thread safety.
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// The count until the string builder writes into the file.
        /// </summary>
        private int flushCount;

        /// <summary>
        /// The string builder with the accumulated content.
        /// </summary>
        private StringBuilder stringBuilder;

        /// <summary>
        /// The current count of appended lines in the buffer.
        /// </summary>
        private int lineCount;

        /// <summary>
        /// The writer used to write to the file.
        /// </summary>
        private StreamWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringFileWriter"/> class with the specified file path and flush count.
        /// </summary>
        /// <param name="_Path">The path of the file to be written.</param>
        /// <param name="_FlushCount">The count of lines to accumulate before flushing to the file (default is 10).</param>
        public StringFileWriter(String _Path, int _FlushCount = 10)
            : base(_Path)
        {
            this.flushCount = _FlushCount;
            this.stringBuilder = new StringBuilder();
            this.lineCount = 0;
        }

        /// <summary>
        /// Writes a line of text to the file and flushes to the file if the specified flush count is reached.
        /// </summary>
        /// <param name="_Line">The line of text to be written to the file.</param>
        public override void Write(String _Line)
        {
            lock (this.lockObject)
            {
                this.stringBuilder.AppendLine(_Line);
                this.lineCount += 1;
            }

            if (this.lineCount >= flushCount)
            {
                this.Flush();
            }
        }

        /// <summary>
        /// Asynchronously writes a line of text to the file and flushes to the file if the specified flush count is reached.
        /// </summary>
        /// <param name="_Line">The line of text to be asynchronously written to the file.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public override async Task WriteAsync(String _Line)
        {
            lock (this.lockObject)
            {
                this.stringBuilder.AppendLine(_Line);
                this.lineCount += 1;
            }

            if (this.lineCount >= flushCount)
            {
                await this.FlushAsync();
            }
        }

        /// <summary>
        /// Flushes the accumulated content in the buffer to the file.
        /// </summary>
        public override void Flush()
        {
            String var_Content = String.Empty;

            lock (this.lockObject)
            {
                var_Content = this.stringBuilder.ToString();
                this.stringBuilder.Clear();
                this.lineCount = 0;
            }

            lock (this.lockObject)
            {
                if (this.writer == null)
                {
                    if (!File.Exists(this.Path))
                    {
                        this.writer = File.CreateText(this.Path);
                    }
                    else
                    {
                        this.writer = File.AppendText(this.Path);
                    }
                }
            }

            this.writer.Write(var_Content);
            this.writer.Flush();
        }

        /// <summary>
        /// Asynchronously flushes the accumulated content in the buffer to the file.
        /// </summary>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public override async Task FlushAsync()
        {
            String var_Content = String.Empty;

            lock (this.lockObject)
            {
                var_Content = this.stringBuilder.ToString();
                this.stringBuilder.Clear();
                this.lineCount = 0;
            }

            lock (this.lockObject)
            {
                if (this.writer == null)
                {
                    if (!File.Exists(this.Path))
                    {
                        this.writer = File.CreateText(this.Path);
                    }
                    else
                    {
                        this.writer = File.AppendText(this.Path);
                    }
                }
            }

            await this.writer.WriteAsync(var_Content);
            await this.writer.FlushAsync();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="StringFileWriter"/>.
        /// </summary>
        public override void Dispose()
        {
            if (this.writer != null)
            {
                this.writer.Dispose();
            }
        }
    }
}
