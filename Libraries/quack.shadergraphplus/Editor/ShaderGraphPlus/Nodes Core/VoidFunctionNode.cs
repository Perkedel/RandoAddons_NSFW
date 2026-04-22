using ShaderGraphPlus.Utilities;

namespace ShaderGraphPlus.Nodes;

public enum VoidFunctionArgumentType
{
	Input,
	Output
}

public struct VoidFunctionArgument
{
	public string TargetProperty;
	public string DefaultTargetProperty;
	public ResultType ResultType;
	public string VarName;
	public VoidFunctionArgumentType ArgumentType;

	public VoidFunctionArgument( string targetProperty, string varName, VoidFunctionArgumentType argumentType, ResultType resultType )
	{
		TargetProperty = targetProperty;
		DefaultTargetProperty = "";
		ArgumentType = argumentType;
		ResultType = resultType;
		VarName = varName;
	}

	public VoidFunctionArgument( string targetProperty, string defaultTargetProperty, string varName, VoidFunctionArgumentType argumentType, ResultType resultType )
	: this( targetProperty, varName, argumentType, resultType )
	{
		if ( ArgumentType != VoidFunctionArgumentType.Input && !string.IsNullOrWhiteSpace( defaultTargetProperty ) )
		{
			EdtiorSound.OhFiddleSticks();
			throw new Exception( $"`defaultTargetProperty` should not be set if the argument type is not an `{VoidFunctionArgumentType.Input}`" );
		}

		DefaultTargetProperty = !string.IsNullOrWhiteSpace( defaultTargetProperty ) ? defaultTargetProperty : "";
	}

}

internal interface IVoidFunctionNode
{
	public void Register( GraphCompiler compiler );
	public void RegisterVoidFunction( GraphCompiler compiler );
}

public abstract class VoidFunctionNode : ShaderNodePlus, IVoidFunctionNode
{
	/// <summary>
	/// Register anything that this node uses.
	/// </summary>
	/// <param name="compiler"></param>
	public virtual void Register( GraphCompiler compiler )
	{
	}

	public virtual void BuildFunctionCall( GraphCompiler compiler, ref List<VoidFunctionArgument> args, ref string functionName, ref string functionCall )
	{
	}

	public void RegisterVoidFunction( GraphCompiler compiler )
	{
		Dictionary<string, string> outputs = new();
		var args = new List<VoidFunctionArgument>();
		var functionName = "";
		var functionCall = "";

		BuildFunctionCall( compiler, ref args, ref functionName, ref functionCall );

		Assert.False( args.Count == 0, $"Void Function Node {DisplayInfo.Name} has no arguments!!!" );

		if ( !string.IsNullOrWhiteSpace( functionName ) )
		{
			compiler.RegisterVoidFunction( functionCall, Identifier, args, out var functionOutputs );

			foreach ( var funcOutput in functionOutputs )
			{
				var propertyInfo = this.GetType().GetProperty( funcOutput.userAssigned );

				if ( propertyInfo != null )
				{
					propertyInfo.SetValue( this, funcOutput.compilerAssigned, null );
				}
			}
		}

		Assert.True( !string.IsNullOrWhiteSpace( functionName ), $"Void Function Node {DisplayInfo.Name} must return a function name!!!" );
	}
}
