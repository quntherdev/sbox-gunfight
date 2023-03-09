namespace Facepunch.Gunfight;

[Flags]
public enum GamemodeType
{
	Gunfight = 1 << 1,
	KillConfirmed = 1 << 2,
	War = 1 << 3,
	Any = 1 << 4
}

public static class GamemodeTypeMethods
{
	public static string GetIdent( this GamemodeType type )
	{
		return type switch
		{
			GamemodeType.Gunfight => "GunfightGamemode",
			GamemodeType.KillConfirmed => "KillConfirmedGamemode",
			GamemodeType.War => "WarGamemode",
			_ => null
		};
	}

	public static string[] GetArray( this GamemodeType type )
	{
		List<string> tags = new();

		foreach ( var enumValue in Enum.GetValues<GamemodeType>() )
		{
			if ( type.HasFlag( enumValue ) )
			{
				tags.Add( enumValue.GetIdent() );
			}
		}

		return tags.ToArray();
	}
}

public partial class GamemodeSystem
{
	[ConVar.Replicated( "gunfight_gamemode" )]
	public static string SelectedGamemode { get; set; } = "WarGamemode";

	private static Gamemode current;
	public static Gamemode Current
	{
		get
		{
			if ( Game.IsServer ) return current;

			if ( !current.IsValid() )
				current = Entity.All.FirstOrDefault( x => x is Gamemode ) as Gamemode;

			return current;
		}
		set
		{
			current = value;
		}
	}

	protected static Gamemode FetchGamemodeEntity()
	{
		// First, see if the map has a gamemode we want to use already
		var gamemode = Entity.All.FirstOrDefault( x => x is Gamemode ) as Gamemode;

		// If not, use game preferences to create one.
		if ( !gamemode.IsValid() && !string.IsNullOrEmpty( SelectedGamemode ) )
		{
			var gamemodeEntity = TypeLibrary.Create<Gamemode>( SelectedGamemode );
			if ( gamemodeEntity.IsValid() )
			{
				Log.Info( $"Found gamemode from TypeLibrary - {SelectedGamemode}" );
				gamemode = gamemodeEntity;
			}
			else
			{
				Log.Warning( "No gamemode found while fetching." );
			}
		}

		return gamemode;
	}

	public static void SetupGamemode()
	{
		Current = FetchGamemodeEntity();

		if ( Current.IsValid() )
		{
			Current.Initialize();
		}
	}
}
