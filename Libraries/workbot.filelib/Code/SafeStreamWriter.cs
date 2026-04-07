using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Sandbox
{
	/// <summary>
	/// This is a replacement for System.IO.StreamWriter,
	/// since even the safe constructors of that class are not whitelisted
	/// </summary>
	public class SafeStreamWriter : TextWriter
	{
		Stream targetStream;
		Encoding targetEncoding;
		bool flushOnWrite;
		/// <summary>
		/// Creates a new <see cref="SafeStreamWriter"/>,
		/// with the option to set the encoding and disabling automatic flushing
		/// </summary>
		public SafeStreamWriter( Stream stream, Encoding encoding = null, bool autoFlush = true )
		{
			if(encoding == null)
			{
				encoding = Encoding.ASCII;
			}
			targetStream = stream;
			targetEncoding = encoding;
			flushOnWrite = autoFlush;
		}

		public override Encoding Encoding => targetEncoding;

		protected override void Dispose( bool disposing )
		{
			targetStream.Dispose();
		}

		public override void Flush()
		{
			targetStream.Flush();
		}

		public override void Write( char value )
		{
			targetStream.Write( Encoding.GetBytes(new char[] { value } ) );
			if( flushOnWrite )
			{
				Flush();
			}
		}
	}
}
