﻿using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Facepunch.Gunfight;

public class Stance : Panel
{
	public Image Icon;
	
	protected GunfightPlayer Player => Game.LocalPawn as GunfightPlayer;
	protected PlayerController PlayerController => Player?.Controller as PlayerController;
	
	public Stance()
	{
		Icon = Add.Image( "ui/stance/stand.png", "icon" );
	}

	public override void Tick()
	{
		var player = GunfightCamera.Target;
		if ( player == null || PlayerController == null ) 
		{
			Style.Display = DisplayMode.None;
			return;
		}

		Style.Display = DisplayMode.Flex;

		if ( PlayerController.Duck.IsActive )
		{
			Icon.SetTexture( "ui/stance/duck.png" );
		}
		else if ( PlayerController.IsSprinting )
		{
			Icon.SetTexture( "ui/stance/run.png" );
		}
		else
		{
			Icon.SetTexture( "ui/stance/stand.png" );
		}
	}
}
