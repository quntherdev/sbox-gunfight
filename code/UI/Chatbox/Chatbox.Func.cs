namespace Facepunch.Gunfight.UI;

public partial class Chatbox
{
	[ConCmd.Client( "gunfight_chat_add", CanBeCalledFromServer = true )]
	public static void AddChatEntry( string name, string message, string playerId = "0", string lobbyState = null, bool isMessage = true )
	{
		Current?.AddEntry( name, message, long.Parse( playerId ), lobbyState, isMessage );

		// Only log clientside if we're not the listen server host
		if ( !Global.IsListenServer )
		{
			Log.Info( $"{name}: {message}" ); 
		}
	}

	public static void AddChatEntry( To target, string name, string message, long playerId = 0, string lobbyState = null, bool isMessage = true)
	{
		AddChatEntry( target, name, message, playerId.ToString(), lobbyState, isMessage);
	}

	[ConCmd.Admin( "gunfight_debug_chat_msg" )]
	public static void DebugMsg()
	{
		var cl = ConsoleSystem.Caller;

		Chatbox.AddChatEntry( To.Everyone, cl.Name, "has joined the game", cl.PlayerId, null, false );
	}

	[ConCmd.Admin( "gunfight_debug_chat_other" )]
	public static void DebugMsgOther()
	{
		Chatbox.AddChatEntry( To.Everyone, "Eagle One Development Team", "has joined the game", 76561197967441886, null, false );
		Chatbox.AddChatEntry( To.Everyone, "Eagle One Development Team", "what's up", 76561197967441886, null, true );
	}

	[ConCmd.Client( "gunfight_chat_addinfo", CanBeCalledFromServer = true )]
	public static void AddInformation( string message, long playerId = 0 )
	{
		Current?.AddEntry( null, message, playerId );
	}

	[ConCmd.Server( "gunfight_say" )]
	public static void Say( string message )
	{
		Assert.NotNull( ConsoleSystem.Caller );

		// todo - reject more stuff
		if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

		Log.Info( $"{ConsoleSystem.Caller}: {message}" );
		AddChatEntry( To.Everyone, ConsoleSystem.Caller.Name, message, ConsoleSystem.Caller.PlayerId );
	}
}