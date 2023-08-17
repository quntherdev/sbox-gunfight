﻿using System.Text.Json;

namespace Facepunch.Gunfight;

public class JsonPersistenceSystem : IPersistenceSystem
{
	public readonly Dictionary<string, Dictionary<string, object>> Data = new();
	protected string GetFilePath( string bucket ) => $"{bucket}.json";

	protected Dictionary<string, object> GetAll( string bucket )
	{
		var path = GetFilePath( bucket );

		if ( !FileSystem.Data.FileExists( path ) )
			return new();

		return FileSystem.Data.ReadJson<Dictionary<string, object>>( GetFilePath( bucket ) );
	}

	public virtual T Get<T>( string bucket, string name, T defValue = default( T ) )
	{
		var data = GetAll( bucket );
		if ( !data.TryGetValue( name, out object value ) )
			return defValue;

		if ( value is JsonElement )
			return JsonSerializer.Deserialize<T>( ((JsonElement)value).GetRawText() );

		return (T)value;
	}

	public bool Set( string bucket, string name, object value )
	{
		var data = GetAll( bucket );
		data[name] = value;

		FileSystem.Data.WriteJson( GetFilePath( bucket ), data );

		return true;
	}

	public bool Remove( string bucket, string name )
	{
		var data = GetAll( bucket );
		data.Remove( name );

		FileSystem.Data.WriteJson( GetFilePath( bucket ), data );

		return true;
	}
}
