using System.Reflection;

namespace Editor.ShaderGraphExtras;

public class MultiEditPanel : Widget
{
	private readonly List<BaseNode> _nodes;
	private readonly HashSet<string> _sharedPropNames;
	private readonly bool _allParameterUI;
	private readonly bool _allTextureInput;
	private readonly bool _allHaveUI;
	private readonly bool _allHaveSampler;

	public Action PropertyUpdated { get; set; }

	public MultiEditPanel( Widget parent, List<BaseNode> nodes ) : base( parent )
	{
		_nodes = nodes;
		_sharedPropNames = BuildSharedPropertyNames();
		_allParameterUI = _nodes.All( n => GetUIProperty( n ) is ParameterUI );
		_allTextureInput = _nodes.All( n => GetUIProperty( n ) is TextureInput );
		_allHaveUI = _nodes.All( n => GetUIProperty( n ) != null );
		_allHaveSampler = _nodes.All( n =>
		{
			var prop = n.GetType().GetProperty( "Sampler", BindingFlags.Public | BindingFlags.Instance );
			return prop != null && prop.PropertyType == typeof( Sampler ) && prop.CanRead && prop.CanWrite;
		} );

		Layout = Layout.Column();
		Layout.Spacing = 4;
		Layout.Margin = 8;

		BuildHeader();
		BuildSharedPropertyEditors();
	}

	private void BuildHeader()
	{
		var header = new Label( $"Editing {_nodes.Count} nodes", this );
		header.SetStyles( "font-weight: bold; color: #aaa; padding: 4px 0;" );
		Layout.Add( header );
		Layout.AddSeparator();
		Layout.AddSpacingCell( 4 );
	}

	private void BuildSharedPropertyEditors()
	{
		AddDirectPropertiesControlSheet();
		AddSamplerControlSheet();
		AddUISection();
		AddGroupSubGroupRows();
	}

	// ─── Direct properties via primary node ControlSheet ───

	private void AddDirectPropertiesControlSheet()
	{
		if ( _sharedPropNames.Count == 0 )
			return;

		var primary = _nodes[0];
		var so = primary.GetSerialized();

		var ungrouped = new List<SerializedProperty>();
		var grouped = new Dictionary<string, List<SerializedProperty>>();
		var groupOrder = new List<string>();

		foreach ( var prop in so )
		{
			if ( prop.HasAttribute<HideAttribute>() ) continue;
			if ( !_sharedPropNames.Contains( prop.Name ) ) continue;

			string groupName = null;
			if ( prop.TryGetAttribute<GroupAttribute>( out var groupAttr ) )
				groupName = groupAttr.Value;

			if ( !string.IsNullOrEmpty( groupName ) )
			{
				if ( !grouped.ContainsKey( groupName ) )
				{
					grouped[groupName] = new List<SerializedProperty>();
					groupOrder.Add( groupName );
				}
				grouped[groupName].Add( prop );
			}
			else
			{
				ungrouped.Add( prop );
			}
		}

		if ( ungrouped.Count > 0 )
		{
			var sheet = new ControlSheet();
			foreach ( var prop in ungrouped )
				sheet.AddRow( prop );
			Layout.Add( sheet );
		}

		foreach ( var groupName in groupOrder )
		{
			AddSectionHeader( groupName );

			var sheet = new ControlSheet();
			foreach ( var prop in grouped[groupName] )
				sheet.AddRow( prop );
			Layout.Add( sheet );
		}

		so.OnPropertyChanged += ( changedProp ) =>
		{
			var propInfo = primary.GetType().GetProperty( changedProp.Name, BindingFlags.Public | BindingFlags.Instance );
			if ( propInfo == null ) return;

			var newValue = propInfo.GetValue( primary );
			foreach ( var node in _nodes )
			{
				if ( node == primary ) continue;
				var targetProp = node.GetType().GetProperty( changedProp.Name, BindingFlags.Public | BindingFlags.Instance );
				targetProp?.SetValue( node, newValue );
				MarkDirty( node );
			}
			MarkDirty( primary );
			PropertyUpdated?.Invoke();
		};
	}

	private HashSet<string> BuildSharedPropertyNames()
	{
		var primaryType = _nodes[0].GetType();
		var result = new HashSet<string>();

		var candidateProps = primaryType.GetProperties( BindingFlags.Public | BindingFlags.Instance )
			.Where( p => p.CanRead && p.CanWrite )
			.Where( p => p.PropertyType != typeof( ParameterUI ) && p.PropertyType != typeof( TextureInput ) )
			.Where( p => p.PropertyType != typeof( Sampler ) );

		foreach ( var prop in candidateProps )
		{
			bool allHave = _nodes.All( n =>
			{
				var p = n.GetType().GetProperty( prop.Name, BindingFlags.Public | BindingFlags.Instance );
				return p != null && p.PropertyType == prop.PropertyType && p.CanRead && p.CanWrite;
			} );

			if ( allHave )
				result.Add( prop.Name );
		}

		return result;
	}

	// ─── UI section (bridged props + proxy ControlSheets) ───

	private void AddUISection()
	{
		bool hasBridged = !_sharedPropNames.Contains( "Name" )
			&& !_allParameterUI && !_allTextureInput
			&& _nodes.All( n => GetBridgedName( n ) != null );

		if ( !_allHaveUI && !hasBridged ) return;

		AddSectionHeader( "UI" );

		if ( hasBridged )
			AddBridgedProperties();

		if ( _allParameterUI )
			AddParameterUIControlSheet();
		else if ( _allTextureInput )
			AddTextureInputControlSheet();
	}

	// ─── Bridged properties across different node types ───

	private static string GetBridgedName( BaseNode node )
	{
		if ( node is IParameterNode pn ) return pn.Name ?? "";
		if ( node is ITextureParameterNode tn ) return tn.UI.Name ?? "";
		return null;
	}

	private static void SetBridgedName( BaseNode node, string value )
	{
		if ( node is IParameterNode pn )
			pn.Name = value;
		else if ( node is ITextureParameterNode tn )
		{
			var ui = tn.UI;
			ui.Name = value;
			tn.UI = ui;
		}
	}

	private static bool? GetBridgedIsAttribute( BaseNode node )
	{
		if ( node is IParameterNode pn ) return pn.IsAttribute;
		if ( node is ITextureParameterNode tn ) return tn.UI.IsAttribute;
		return null;
	}

	private static void SetBridgedIsAttribute( BaseNode node, bool value )
	{
		if ( node is IParameterNode pn )
			pn.IsAttribute = value;
		else if ( node is ITextureParameterNode tn )
		{
			var ui = tn.UI;
			ui.IsAttribute = value;
			tn.UI = ui;
		}
	}

	private static int? GetBridgedPriority( BaseNode node )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p ) return p.Priority;
		if ( ui is TextureInput t ) return t.Priority;
		return null;
	}

	private static void SetBridgedPriority( BaseNode node, int value )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p )
		{
			p.Priority = value;
			SetUIProperty( node, p );
		}
		else if ( ui is TextureInput t )
		{
			t.Priority = value;
			SetUIProperty( node, t );
		}
	}

	private void AddBridgedProperties()
	{
		bool allHaveIsAttribute = _nodes.All( n => GetBridgedIsAttribute( n ) != null );
		bool allHavePriority = _nodes.All( n => GetBridgedPriority( n ) != null );

		var proxy = new BridgedProxy
		{
			Name = GetBridgedName( _nodes[0] ),
			IsAttribute = allHaveIsAttribute && GetBridgedIsAttribute( _nodes[0] ) == true,
			Priority = allHavePriority ? GetBridgedPriority( _nodes[0] ).Value : 0,
		};

		var so = proxy.GetSerialized();
		var sheet = new ControlSheet();

		foreach ( var prop in so )
		{
			if ( prop.Name == "IsAttribute" && !allHaveIsAttribute ) continue;
			if ( prop.Name == "Priority" && !allHavePriority ) continue;
			sheet.AddRow( prop );
		}

		Layout.Add( sheet );

		int prevPriority = proxy.Priority;
		so.OnPropertyChanged += ( p ) =>
		{
			foreach ( var node in _nodes )
			{
				if ( p.Name == "Name" )
					SetBridgedName( node, proxy.Name );
				else if ( p.Name == "IsAttribute" )
					SetBridgedIsAttribute( node, proxy.IsAttribute );
				else if ( p.Name == "Priority" )
				{
					int delta = proxy.Priority - prevPriority;
					SetBridgedPriority( node, (GetBridgedPriority( node ) ?? 0) + delta );
				}
				MarkDirty( node );
			}
			if ( p.Name == "Priority" )
				prevPriority = proxy.Priority;
			PropertyUpdated?.Invoke();
		};
	}

	public class BridgedProxy
	{
		public string Name { get; set; }
		public bool IsAttribute { get; set; }
		public int Priority { get; set; }
	}

	// ─── Group / Sub Group manual rows ───

	private void AddGroupSubGroupRows()
	{
		if ( !_allHaveUI ) return;

		AddSectionHeader( "Group" );
		AddGroupNameRow( "Group Name", n => GetGroupName( n, primary: true ), ( n, v ) => SetGroupName( n, v, primary: true ), false );
		AddGroupPriorityRow( "Group Priority", n => GetGroupPriority( n, primary: true ), ( n, v ) => SetGroupPriority( n, v, primary: true ) );

		AddSectionHeader( "Sub Group" );
		AddGroupNameRow( "Sub Group Name", n => GetGroupName( n, primary: false ), ( n, v ) => SetGroupName( n, v, primary: false ), true );
		AddGroupPriorityRow( "Sub Group Priority", n => GetGroupPriority( n, primary: false ), ( n, v ) => SetGroupPriority( n, v, primary: false ) );
	}

	private void AddGroupNameRow( string displayName, Func<BaseNode, string> getter, Action<BaseNode, string> setter, bool isSubGroup )
	{
		var row = Layout.AddRow();
		row.Spacing = 8;

		var label = new Label( displayName, this );
		label.FixedWidth = 120;
		row.Add( label );

		var values = _nodes.Select( n => getter( n ) ).Distinct().ToList();
		var comboBox = row.Add( new ComboBox( this ), 1 );
		comboBox.Editable = true;
		comboBox.Insertion = ComboBox.InsertMode.Skip;

		var existingNames = new HashSet<string>();
		var graph = _nodes[0].Graph;
		if ( graph != null )
		{
			foreach ( var node in graph.Nodes )
			{
				var uiProp = GetUIProperty( node as BaseNode );
				if ( uiProp == null ) continue;

				string name = null;
				if ( uiProp is ParameterUI paramUI )
					name = isSubGroup ? paramUI.SecondaryGroup.Name : paramUI.PrimaryGroup.Name;
				else if ( uiProp is TextureInput texUI )
					name = isSubGroup ? texUI.SecondaryGroup.Name : texUI.PrimaryGroup.Name;

				if ( !string.IsNullOrEmpty( name ) )
					existingNames.Add( name );
			}
		}

		comboBox.AddItem( "" );
		foreach ( var name in existingNames )
			comboBox.AddItem( name );

		if ( values.Count == 1 && !string.IsNullOrEmpty( values[0] ) )
			comboBox.CurrentText = values[0];

		comboBox.TextChanged += () =>
		{
			foreach ( var node in _nodes )
			{
				setter( node, comboBox.CurrentText );
				MarkDirty( node );
			}
			PropertyUpdated?.Invoke();
		};
	}

	private void AddGroupPriorityRow( string displayName, Func<BaseNode, int> getter, Action<BaseNode, int> setter )
	{
		var row = Layout.AddRow();
		row.Spacing = 8;

		var label = new Label( displayName, this );
		label.FixedWidth = 120;
		row.Add( label );

		var values = _nodes.Select( n => getter( n ) ).ToList();
		var distinctValues = values.Distinct().ToList();

		var lineEdit = row.Add( new LineEdit( this ), 1 );

		if ( distinctValues.Count == 1 )
			lineEdit.Text = distinctValues[0].ToString();
		else
			lineEdit.PlaceholderText = $"(mixed: {values.Min()}..{values.Max()})";

		lineEdit.EditingFinished += () =>
		{
			var text = lineEdit.Text.Trim();
			if ( string.IsNullOrEmpty( text ) ) return;

			if ( TryParseRelativeInt( text, out int delta ) )
			{
				foreach ( var node in _nodes )
				{
					setter( node, getter( node ) + delta );
					MarkDirty( node );
				}
			}
			else if ( int.TryParse( text, out int absolute ) )
			{
				foreach ( var node in _nodes )
				{
					setter( node, absolute );
					MarkDirty( node );
				}
			}

			// Refresh display
			var newValues = _nodes.Select( n => getter( n ) ).Distinct().ToList();
			if ( newValues.Count == 1 )
			{
				lineEdit.Text = newValues[0].ToString();
				lineEdit.PlaceholderText = "";
			}
			else
			{
				lineEdit.Text = "";
				var allVals = _nodes.Select( n => getter( n ) ).ToList();
				lineEdit.PlaceholderText = $"(mixed: {allVals.Min()}..{allVals.Max()})";
			}

			PropertyUpdated?.Invoke();
		};
	}

	// ─── Sampler ControlSheet proxy ───

	private void AddSamplerControlSheet()
	{
		if ( !_allHaveSampler ) return;

		AddSectionHeader( "Sampler" );

		var first = GetSampler( _nodes[0] );
		var proxy = new SamplerProxy
		{
			Filter = first.Filter,
			AddressU = first.AddressU,
			AddressV = first.AddressV,
		};

		var so = proxy.GetSerialized();
		var sheet = new ControlSheet();
		sheet.AddObject( so );
		Layout.Add( sheet );

		so.OnPropertyChanged += ( p ) =>
		{
			foreach ( var node in _nodes )
			{
				var s = GetSampler( node );
				s.Filter = proxy.Filter;
				s.AddressU = proxy.AddressU;
				s.AddressV = proxy.AddressV;
				SetSampler( node, s );
				MarkDirty( node );
			}
			PropertyUpdated?.Invoke();
		};
	}

	public class SamplerProxy
	{
		public SamplerFilter Filter { get; set; }
		public SamplerAddress AddressU { get; set; }
		public SamplerAddress AddressV { get; set; }
	}

	// ─── TextureInput ControlSheet proxy ───

	private void AddTextureInputControlSheet()
	{
		var first = (TextureInput)GetUIProperty( _nodes[0] );
		var proxy = new TextureInputProxy
		{
			Name = first.Name ?? "",
			IsAttribute = first.IsAttribute,
			Default = first.Default,
			Extension = first.Extension,
			CustomExtension = first.CustomExtension ?? "",
			Processor = first.Processor,
			ColorSpace = first.ColorSpace,
			ImageFormat = first.ImageFormat,
			SrgbRead = first.SrgbRead,
			Priority = first.Priority,
		};

		var so = proxy.GetSerialized();
		var sheet = new ControlSheet();
		sheet.AddObject( so );
		Layout.Add( sheet );

		int prevPriority = proxy.Priority;
		so.OnPropertyChanged += ( p ) =>
		{
			foreach ( var node in _nodes )
			{
				var ui = (TextureInput)GetUIProperty( node );
				ui.Name = proxy.Name;
				ui.IsAttribute = proxy.IsAttribute;
				ui.Default = proxy.Default;
				ui.Extension = proxy.Extension;
				ui.CustomExtension = proxy.CustomExtension;
				ui.Processor = proxy.Processor;
				ui.ColorSpace = proxy.ColorSpace;
				ui.ImageFormat = proxy.ImageFormat;
				ui.SrgbRead = proxy.SrgbRead;
				if ( p.Name == "Priority" )
					ui.Priority = ui.Priority + (proxy.Priority - prevPriority);
				SetUIProperty( node, ui );
				MarkDirty( node );
			}
			if ( p.Name == "Priority" )
				prevPriority = proxy.Priority;
			PropertyUpdated?.Invoke();
		};
	}

	public class TextureInputProxy
	{
		public string Name { get; set; }
		public bool IsAttribute { get; set; }
		public Color Default { get; set; }
		public TextureExtension Extension { get; set; }
		public string CustomExtension { get; set; }
		public TextureProcessor Processor { get; set; }
		public TextureColorSpace ColorSpace { get; set; }
		public TextureFormat ImageFormat { get; set; }
		public bool SrgbRead { get; set; }
		public int Priority { get; set; }
	}

	// ─── ParameterUI ControlSheet proxy ───

	private void AddParameterUIControlSheet()
	{
		var first = (ParameterUI)GetUIProperty( _nodes[0] );
		var proxy = new ParameterUIProxy
		{
			Type = first.Type,
			Step = first.Step,
			Priority = first.Priority,
		};

		var so = proxy.GetSerialized();
		var sheet = new ControlSheet();
		sheet.AddObject( so );
		Layout.Add( sheet );

		int prevPriority = proxy.Priority;
		so.OnPropertyChanged += ( p ) =>
		{
			foreach ( var node in _nodes )
			{
				var ui = (ParameterUI)GetUIProperty( node );
				ui.Type = proxy.Type;
				ui.Step = proxy.Step;
				if ( p.Name == "Priority" )
					ui.Priority = ui.Priority + (proxy.Priority - prevPriority);
				SetUIProperty( node, ui );
				MarkDirty( node );
			}
			if ( p.Name == "Priority" )
				prevPriority = proxy.Priority;
			PropertyUpdated?.Invoke();
		};
	}

	public class ParameterUIProxy
	{
		public UIType Type { get; set; }
		public float Step { get; set; }
		public int Priority { get; set; }
	}

	// ─── Helpers ───

	private void AddSectionHeader( string title )
	{
		Layout.AddSpacingCell( 8 );
		var label = new Label( title, this );
		label.SetStyles( "font-weight: bold; color: #888; padding: 2px 0;" );
		Layout.Add( label );
	}

	private static bool TryParseRelativeInt( string text, out int delta )
	{
		delta = 0;
		if ( text.Length < 2 ) return false;

		if ( text[0] == '+' && int.TryParse( text.Substring( 1 ), out delta ) )
			return true;

		if ( text[0] == '-' && int.TryParse( text.Substring( 1 ), out int absDelta ) )
		{
			delta = -absDelta;
			return true;
		}

		return false;
	}

	private static Sampler GetSampler( BaseNode node )
	{
		var prop = node.GetType().GetProperty( "Sampler", BindingFlags.Public | BindingFlags.Instance );
		return prop != null ? (Sampler)prop.GetValue( node ) : default;
	}

	private static void SetSampler( BaseNode node, Sampler value )
	{
		var prop = node.GetType().GetProperty( "Sampler", BindingFlags.Public | BindingFlags.Instance );
		prop?.SetValue( node, value );
	}

	private static object GetUIProperty( BaseNode node )
	{
		if ( node == null ) return null;
		if ( node is IParameterNode pn ) return pn.UI;
		if ( node is ITextureParameterNode tn ) return tn.UI;

		var prop = node.GetType().GetProperty( "UI", BindingFlags.Public | BindingFlags.Instance );
		if ( prop != null && ( prop.PropertyType == typeof( ParameterUI ) || prop.PropertyType == typeof( TextureInput ) ) )
			return prop.GetValue( node );

		return null;
	}

	private static void SetUIProperty( BaseNode node, object ui )
	{
		if ( node is IParameterNode pn && ui is ParameterUI paramUI )
		{
			pn.UI = paramUI;
			return;
		}

		if ( node is ITextureParameterNode tn && ui is TextureInput texUI )
		{
			tn.UI = texUI;
			return;
		}

		var prop = node.GetType().GetProperty( "UI", BindingFlags.Public | BindingFlags.Instance );
		if ( prop != null && prop.CanWrite )
			prop.SetValue( node, ui );
	}

	private static string GetGroupName( BaseNode node, bool primary )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p )
			return (primary ? p.PrimaryGroup.Name : p.SecondaryGroup.Name) ?? "";
		if ( ui is TextureInput t )
			return (primary ? t.PrimaryGroup.Name : t.SecondaryGroup.Name) ?? "";
		return "";
	}

	private static void SetGroupName( BaseNode node, string value, bool primary )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p )
		{
			var group = primary ? p.PrimaryGroup : p.SecondaryGroup;
			group.Name = value;
			if ( primary ) p.PrimaryGroup = group; else p.SecondaryGroup = group;
			SetUIProperty( node, p );
		}
		else if ( ui is TextureInput t )
		{
			var group = primary ? t.PrimaryGroup : t.SecondaryGroup;
			group.Name = value;
			if ( primary ) t.PrimaryGroup = group; else t.SecondaryGroup = group;
			SetUIProperty( node, t );
		}
		MarkDirty( node );
	}

	private static int GetGroupPriority( BaseNode node, bool primary )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p )
			return primary ? p.PrimaryGroup.Priority : p.SecondaryGroup.Priority;
		if ( ui is TextureInput t )
			return primary ? t.PrimaryGroup.Priority : t.SecondaryGroup.Priority;
		return 0;
	}

	private static void SetGroupPriority( BaseNode node, int value, bool primary )
	{
		var ui = GetUIProperty( node );
		if ( ui is ParameterUI p )
		{
			var group = primary ? p.PrimaryGroup : p.SecondaryGroup;
			group.Priority = value;
			if ( primary ) p.PrimaryGroup = group; else p.SecondaryGroup = group;
			SetUIProperty( node, p );
		}
		else if ( ui is TextureInput t )
		{
			var group = primary ? t.PrimaryGroup : t.SecondaryGroup;
			group.Priority = value;
			if ( primary ) t.PrimaryGroup = group; else t.SecondaryGroup = group;
			SetUIProperty( node, t );
		}
		MarkDirty( node );
	}

	private static void MarkDirty( BaseNode node )
	{
		if ( node is ShaderNode sn )
			sn.IsDirty = true;
	}
}
