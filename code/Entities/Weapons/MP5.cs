﻿namespace Facepunch.Gunfight;

[Library( "gunfight_mp5" ), HammerEntity]
[EditorModel( "models/mp5/w_mp5.vmdl" )]
[Title( "MP5" ), Category( "Weapons" )]
partial class MP5 : ShooterWeapon
{
	public static readonly Model WorldModel = Model.Load( "models/mp5/w_mp5.vmdl" );
	public override string ViewModelPath => "models/mp5/fp_mp5.vmdl";

	public override float PrimaryRate => 13.0f;
	public override float SecondaryRate => 1.0f;
	public override int ClipSize => 30;
	public override float ReloadTime => 4.0f;
	public override int Bucket => 1;
	public override int BucketWeight => 100;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = 20;
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			if ( AvailableAmmo() > 0 )
			{
				Reload();
			}
			return;
		}

		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();
		PlaySound( "smg1_shoot" );

		//
		// Shoot the bullets
		//
		ShootBullet( 0.1f, 1.5f, 12.0f, 3.0f );

	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 2 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );
		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

		// center circle
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.1f, 0.0f ) );
			var length = 2.0f + shootEase * 2.0f;
			draw.Circle( center, length );
		}


		draw.Color = draw.Color.WithAlpha( draw.Color.a * 0.2f );

		// outer lines
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.2f, 0.0f ) );
			var length = 3.0f + shootEase * 2.0f;
			var gap = 30.0f + shootEase * 50.0f;
			var thickness = 2.0f;

			draw.Line( thickness, center + Vector2.Up * gap + Vector2.Left * length, center + Vector2.Up * gap - Vector2.Left * length );
			draw.Line( thickness, center - Vector2.Up * gap + Vector2.Left * length, center - Vector2.Up * gap - Vector2.Left * length );

			draw.Line( thickness, center + Vector2.Left * gap + Vector2.Up * length, center + Vector2.Left * gap - Vector2.Up * length );
			draw.Line( thickness, center - Vector2.Left * gap + Vector2.Up * length, center - Vector2.Left * gap - Vector2.Up * length );
		}
	}

}
