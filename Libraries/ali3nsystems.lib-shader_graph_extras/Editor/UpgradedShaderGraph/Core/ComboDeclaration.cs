namespace Editor.ShaderGraphExtras;
public struct ComboName
{
	[Hide]
	public string Name { get; set; }

	public static implicit operator string( ComboName comboName ) => comboName.Name;
	public static implicit operator ComboName( string name ) => new ComboName { Name = name };

	public override string ToString() => Name ?? "";
}
public struct ComboGroup
{
	[Hide]
	public string Group { get; set; }

	public static implicit operator string( ComboGroup comboGroup ) => comboGroup.Group;
	public static implicit operator ComboGroup( string name ) => new ComboGroup { Group = name };

	public override string ToString() => Group ?? "";
}
public enum ComboType
{
	Bool,
	Enum
}
public enum ComboMode
{
	Static,
	Dynamic
}
public class ComboDeclaration
{
	public string HLSLName { get; set; }
	public string DisplayName { get; set; }
	public string Group { get; set; }
	public ComboType Type { get; set; }
	public ComboMode Mode { get; set; }
	public int Range { get; set; }
	public string[] Labels { get; set; }
	public int Value { get; set; }
	public bool IsValid()
	{
		if ( string.IsNullOrWhiteSpace( HLSLName ) || !IsValidHLSLIdentifier( HLSLName ) )
			return false;

		if ( string.IsNullOrWhiteSpace( DisplayName ) )
			return false;

		if ( Type == ComboType.Enum && Range < 1 )
			return false;

		int maxValue = Type == ComboType.Bool ? 1 : Range;
		if ( Value < 0 || Value > maxValue )
			return false;

		if ( Labels != null && Labels.Length > 0 )
		{
			int expectedLabels = Type == ComboType.Bool ? 2 : (Range + 1);
			if ( Labels.Length != expectedLabels )
				return false;
		}

		return true;
	}

	private static bool IsValidHLSLIdentifier( string name )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
			return false;

		if ( !char.IsLetter( name[0] ) && name[0] != '_' )
			return false;

		return name.All( c => char.IsLetterOrDigit( c ) || c == '_' );
	}

	public string GetComboVariableName()
	{
		return Mode == ComboMode.Static ? $"S_{HLSLName}" : $"D_{HLSLName}";
	}

	public string GetFeatureName()
	{
		return $"F_{HLSLName}";
	}
}
