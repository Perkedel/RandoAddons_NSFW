using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// TODO: Any more? Are there any more class types who want some?
// TODO: Rotation not writing/reading
// TODO: My plan for the property problem (some being circular without any way to detect it, is to travel down each property, counting which types have been visited, and canceling the save if an object ends back where it started)
namespace Sandbox
{
	internal static class BinaryClass
	{
		// TODO: Need to think about nested recursion loops
		internal static bool CanTravel<T>( TypeDescription typeToWatchOutFor, TypeDescription currentType, T target )
		{
			if ( currentType == null )
			{
				return true;
			}

			if ( target != null )
			{
				if ( typeToWatchOutFor.TargetType == target.GetType() )
				{
					return false;
				}

				foreach ( var field in currentType.Fields )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( field.IsStatic || field.ReadOnly || field.IsProperty || field.IsMethod )
						continue;

					object obj = field.GetValue( target );

					if ( obj != null )
					{
						if ( typeToWatchOutFor.TargetType == field.FieldType )
						{
							return false;
						}
					}
				}

				foreach ( var property in currentType.Properties )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( property.IsStatic || property.IsIndexer || !property.IsGetMethodPublic || !property.IsSetMethodPublic || !property.IsPublic || !property.CanWrite || !property.CanRead )
						continue;

					object obj = property.GetValue( target );

					if ( obj != null )
					{
						if ( typeToWatchOutFor.TargetType == property.PropertyType )
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		internal interface IKeyValuePairWrapper
		{
			public Type GetKeyType();
			public Type GetValueType();
			public object GetKey( object target );
			public object GetValue( object target );
			public object Create( object Key, object Value );
			public bool IsKeyValuePair( Type type );
		}

		internal class KeyValuePairWrapper<TKey, TValue> : IKeyValuePairWrapper
		{
			public object Create( object Key, object Value )
			{
				return KeyValuePair.Create((TKey)Key, (TValue)Value);
			}

			public object GetKey( object target )
			{
				return ((KeyValuePair<TKey, TValue>)target).Key;
			}

			public Type GetKeyType()
			{
				return typeof( TKey );
			}

			public object GetValue( object target )
			{
				return ((KeyValuePair<TKey, TValue>)target).Value;
			}

			public Type GetValueType()
			{
				return typeof( TValue );
			}

			public bool IsKeyValuePair(Type type)
			{
				return (type == typeof( KeyValuePair<TKey, TValue> ));
			}
		}

		static bool IsGenericType( Type interfaceType, Type genericType )
		{
			// NOTE: This is still quite hacky, but at least it's slightly better than the alternative online...
			try
			{
				Type targetType = TypeLibrary.GetType( genericType ).MakeGenericType( TypeLibrary.GetGenericArguments( interfaceType ) );
				return interfaceType == targetType;
			}
			catch
			{
				return false;
			}
		}

		internal static void WriteDefined( BinaryWriter writer, object value, Type valueType )
		{
			switch ( value )
			{
				case bool val:
					writer.Write( val );
					break;
				case byte val:
					writer.Write( val );
					break;
				case sbyte val:
					writer.Write( val );
					break;
				case char val:
					writer.Write( val );
					break;
				case decimal val:
					writer.Write( val );
					break;
				case double val:
					writer.Write( val );
					break;
				case float val:
					writer.Write( val );
					break;
				case int val:
					writer.Write( val );
					break;
				case uint val:
					writer.Write( val );
					break;
				case long val:
					writer.Write( val );
					break;
				case ulong val:
					writer.Write( val );
					break;
				case short val:
					writer.Write( val );
					break;
				case ushort val:
					writer.Write( val );
					break;
				case string val:
					writer.Write( val );
					break;
				default:
					{
						WriteObject( writer, value, TypeLibrary.GetType( valueType ) );
						break;
					}
			}
		}

		internal static bool WriteArray(BinaryWriter writer, Type targetType, object valueTarget)
		{
			if ( targetType != typeof( string ) && typeof( IEnumerable ).IsAssignableFrom( targetType ) )
			{
				Type typeToUse;
				if ( typeof( Array ).IsAssignableFrom( targetType ) )
				{
					var array = Array.CreateInstanceFromArrayType( targetType, 1 );
					typeToUse = array.GetValue( 0 ).GetType();
				}
				else
				{
					typeToUse = TypeLibrary.GetGenericArguments( targetType )[0];
				}

				writer.Write( ((IEnumerable)valueTarget).Cast<object>().Count() );
				foreach ( var item in ((IEnumerable)valueTarget) )
				{
					WriteDefined( writer, item, typeToUse );
				}
				return true;
			}
			return false;
		}

		static bool WriteSpecialTypes( BinaryWriter writer, Type targetType, object target )
		{
			if ( targetType.IsGenericType && TypeLibrary.GetGenericArguments( targetType ).Count() == 2 )
			{
				var wrapper = TypeLibrary.GetType( typeof( KeyValuePairWrapper<,> ) ).CreateGeneric<object>( TypeLibrary.GetGenericArguments( targetType ) );
				if(((IKeyValuePairWrapper)wrapper).IsKeyValuePair( targetType ) )
				{
					WriteDefined( writer, ((IKeyValuePairWrapper)wrapper).GetKey( target ), ((IKeyValuePairWrapper)wrapper).GetKeyType() );
					WriteDefined( writer, ((IKeyValuePairWrapper)wrapper).GetValue( target ), ((IKeyValuePairWrapper)wrapper).GetValueType() );
					return true;
				}
			}

			if ( IsGenericType( targetType, typeof( Dictionary<,> ) ) )
			{
				int count = ((IDictionary)target).Count;
				writer.Write( count );
				for(int i = 0; i < count; i++ )
				{
					var key = ((IDictionary)target).Keys.OfType<object>().ElementAt(i);
					var value = ((IDictionary)target).Values.OfType<object>().ElementAt(i);
					WriteDefined( writer, key, key.GetType() );
					WriteDefined( writer, value, value.GetType() );
				}
				return true;
			}
			
			if ( WriteArray( writer, targetType, target ) )
			{
				return true;
			}
			return false;
		}

		internal static void WriteObject( BinaryWriter writer, object target, TypeDescription type )
		{
			if ( target != null )
			{
				if ( WriteSpecialTypes( writer, target.GetType(), target ) )
				{
					return;
				}

				foreach( var field in type.Fields )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( field.IsStatic || field.ReadOnly || field.IsProperty || field.IsMethod )
						continue;

					object obj = field.GetValue( target );

					if ( obj != null )
					{
						writer.Write( true );
						if ( obj is Enum enm )
						{
							writer.Write( Convert.ToInt32( enm ) );
							continue;
						}

						if ( WriteSpecialTypes( writer, field.FieldType, obj ) )
						{
							continue;
						}

						if ( !CanTravel( type, TypeLibrary.GetType( field.FieldType ), obj ) )
							continue;

						WriteDefined( writer, field.GetValue( target ), field.FieldType );
					}
					else
					{
						writer.Write( false );
					}
				}

				foreach ( var property in type.Properties )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( property.IsStatic || property.IsIndexer || !property.IsGetMethodPublic || !property.IsSetMethodPublic || !property.IsPublic || !property.CanWrite || !property.CanRead )
						continue;

					object obj = property.GetValue( target );

					if ( obj != null )
					{
						writer.Write( true );
						if ( obj is Enum enm )
						{
							writer.Write( Convert.ToInt32( enm ) );
							continue;
						}

						if(WriteSpecialTypes( writer, property.PropertyType, obj ) )
						{
							continue;
						}

						if ( !CanTravel( type, TypeLibrary.GetType( property.PropertyType ), obj ) )
							continue;

						WriteDefined( writer, property.GetValue( target ), property.PropertyType );
					}
					else
					{
						writer.Write( false );
					}
				}
			}
		}

		internal static object ReadDefined( BinaryReader br, Type objType )
		{
			switch ( Type.GetTypeCode( objType ) )
			{
				case TypeCode.Boolean:
					return br.ReadBoolean();
				case TypeCode.Byte:
					return br.ReadByte();
				case TypeCode.SByte:
					return br.ReadSByte();
				case TypeCode.Char:
					return br.ReadChar();
				case TypeCode.Decimal:
					return br.ReadDecimal();
				case TypeCode.Double:
					return br.ReadDouble();
				case TypeCode.Single:
					return br.ReadSingle();
				case TypeCode.Int32:
					return br.ReadInt32();
				case TypeCode.UInt32:
					return br.ReadUInt32();
				case TypeCode.Int64:
					return br.ReadInt64();
				case TypeCode.UInt64:
					return br.ReadUInt64();
				case TypeCode.Int16:
					return br.ReadInt16();
				case TypeCode.UInt16:
					return br.ReadUInt16();
				case TypeCode.String:
					return br.ReadString();
				default:
					{
						object type = null;
						if ( objType.IsGenericType && TypeLibrary.GetGenericArguments( objType ).Count() > 0 )
						{
							type = TypeLibrary.GetType( objType ).CreateGeneric<object>( TypeLibrary.GetGenericArguments( objType ) );
						}
						else
						{
							type = TypeLibrary.GetType( objType ).Create<object>();
						}
						ReadObject( br, ref type, TypeLibrary.GetType( objType ) );
						return type;
					}
			}
		}

		internal static bool ReadArray( BinaryReader br, Type targetType, out Array array )
		{
			array = null;
			if ( targetType != typeof( string ) && typeof( IEnumerable ).IsAssignableFrom( targetType ) )
			{
				int count = br.ReadInt32();
				Type typeToUse;
				if ( typeof( Array ).IsAssignableFrom( targetType ) )
				{
					array = Array.CreateInstanceFromArrayType( targetType, count );
					typeToUse = array.GetValue( 0 ).GetType();
				}
				else
				{
					typeToUse = TypeLibrary.GetGenericArguments( targetType )[0];
					array = Array.CreateInstance( typeToUse, count );
				}

				for ( int i = 0; i < count; i++ )
				{
					var arrayVal = ReadDefined( br, typeToUse );
					array.SetValue( arrayVal, i );
				}
				return true;
			}
			return false;
		}

		static bool ReadSpecialTypes<T>( BinaryReader br, Type targetType, ref T target)
		{
			if ( targetType.IsGenericType && TypeLibrary.GetGenericArguments( targetType ).Count() == 2 )
			{
				var wrapper = TypeLibrary.GetType( typeof( KeyValuePairWrapper<,> ) ).CreateGeneric<object>( TypeLibrary.GetGenericArguments( targetType ) );
				if ( ((IKeyValuePairWrapper)wrapper).IsKeyValuePair( targetType ) )
				{
					object key = ReadDefined( br, ((IKeyValuePairWrapper)wrapper).GetKeyType() );
					object value = ReadDefined( br, ((IKeyValuePairWrapper)wrapper).GetValueType() );
					target = (T)(object)((IKeyValuePairWrapper)wrapper).Create( key, value );
					return true;
				}
			}

			if ( IsGenericType( targetType, typeof( Dictionary<,> ) ) )
			{
				var dict = TypeLibrary.GetType( typeof(Dictionary<,>) ).CreateGeneric<object>( TypeLibrary.GetGenericArguments( targetType ) );
				int count = br.ReadInt32();
				for(int i = 0; i < count; i++)
				{
					object obj1 = ReadDefined( br, TypeLibrary.GetGenericArguments( targetType )[0] );
					object obj2 = ReadDefined( br, TypeLibrary.GetGenericArguments( targetType )[1] );
					((IDictionary)dict).Add( obj1, obj2 );
				}
				target = (T)(object)dict;
				return true;
			}
			
			if ( ReadArray( br, targetType, out var targetArray ) )
			{
				target = (T)Json.Deserialize( Json.Serialize( targetArray ), targetType );
				return true;
			}

			return false;
		}

		internal static void ReadObject<T>( BinaryReader br, ref T target, TypeDescription type )
		{
			if ( target != null )
			{
				if(ReadSpecialTypes( br, target.GetType(), ref target ) )
				{
					return;
				}

				foreach ( var field in type.Fields )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( field.IsStatic || field.ReadOnly || field.IsProperty || field.IsMethod )
						continue;

					if ( br.ReadBoolean() )
					{
						object obj = field.GetValue( target );

						if ( obj is Enum enm )
						{
							field.SetValue( target, br.ReadInt32() );
							continue;
						}

						if ( ReadSpecialTypes( br, field.FieldType, ref obj ) )
						{
							field.SetValue( target, obj );
							continue;
						}

						if ( !CanTravel( type, TypeLibrary.GetType( field.FieldType ), obj ) )
							continue;

						field.SetValue( target, ReadDefined( br, field.FieldType ) );
					}
					else
					{
						field.SetValue( target, null );
					}
				}

				foreach ( var property in type.Properties )
				{
					// HACKHACK: This is the best i can do here given the whitelist restrictions... (I can only DREAM of the absolute horrors this will cause!)
					if ( property.IsStatic || property.IsIndexer || !property.IsGetMethodPublic || !property.IsSetMethodPublic || !property.IsPublic || !property.CanWrite || !property.CanRead )
						continue;

					if ( br.ReadBoolean() )
					{
						object obj = property.GetValue( target );

						if ( obj is Enum enm )
						{
							property.SetValue( target, br.ReadInt32() );
							continue;
						}

						if(ReadSpecialTypes(br, property.PropertyType, ref obj))
						{
							property.SetValue( target, obj );
							continue;
						}

						if ( !CanTravel( type, TypeLibrary.GetType( property.PropertyType ), obj ) )
							continue;

						property.SetValue( target, ReadDefined( br, property.PropertyType ) );
					}
					else
					{
						property.SetValue( target, null );
					}
				}
			}
		}
	}
}
