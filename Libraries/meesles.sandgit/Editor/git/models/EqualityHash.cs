using System.Text;

namespace Sandbox.git.models;

/// <summary>
/// Creates a stable hash from values for structural equality checks.
/// </summary>
public static class EqualityHash {
	public static string Create(
		params object[] values) {
		if ( values == null || values.Length == 0 )
			return string.Empty;

		var sb = new StringBuilder();
		for ( var i = 0; i < values.Length; i++ ) {
			if ( i > 0 )
				sb.Append('|');

			var v = values[i];
			if ( v == null )
				sb.Append('n');
			else
				sb.Append(v.ToString());
		}

		return sb.ToString();
	}
}
