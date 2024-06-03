// Microsoft
using System.Threading.Tasks;

namespace GUPS.EasyPerformanceMonitor.Persistent
{
    /// <summary>
    /// Represents a specialized CSV file writer that extends the functionality of <see cref="StringFileWriter"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="CsvFileWriter"/> class provides methods to append lines in CSV format to a file. It inherits
    /// from <see cref="StringFileWriter"/> and extends its capabilities by introducing methods to conveniently append
    /// CSV-formatted lines with specified column values. The class is designed for writing CSV data with a flexible
    /// approach, accommodating both single and multiple column values for each row.
    /// </remarks>
    internal class CsvFileWriter : StringFileWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFileWriter"/> class with the specified file path and flush count.
        /// </summary>
        /// <param name="_Path">The path of the file to be written.</param>
        /// <param name="_FlushCount">The count of lines to accumulate before flushing to the file (default is 100).</param>
        public CsvFileWriter(string _Path, int _FlushCount = 100)
            : base(_Path, _FlushCount)
        {
        }

        /// <summary>
        /// Appends a new line in CSV format with a single column value.
        /// </summary>
        /// <param name="_Value">The column value for the row.</param>
        public void Append(float _Value)
        {
            this.Write(_Value.ToString());
        }

        /// <summary>
        /// Appends a new line in CSV format with a single column value.
        /// </summary>
        /// <param name="_Value">The column value for the row.</param>
        public async Task AppendAsync(float _Value)
        {
            await this.WriteAsync(_Value.ToString());
        }

        /// <summary>
        /// Appends a new line in CSV format with two column values.
        /// </summary>
        /// <param name="_Key">The first column value for the row.</param>
        /// <param name="_Value">The second column value for the row.</param>
        public void Append(int _Key, float _Value)
        {
            this.Write($"{_Key},{_Value}");
        }

        /// <summary>
        /// Appends a new line in CSV format with two column values.
        /// </summary>
        /// <param name="_Key">The first column value for the row.</param>
        /// <param name="_Value">The second column value for the row.</param>
        public async Task AppendAsync(int _Key, float _Value)
        {
            await this.WriteAsync($"{_Key},{_Value}");
        }
    }
}
