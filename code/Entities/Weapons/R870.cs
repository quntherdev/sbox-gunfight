﻿namespace Facepunch.Gunfight;

[Library( "gunfight_r870" ), HammerEntity]
[EditorModel( "models/r870/w_r870.vmdl" )]
[Title( "R870" ), Category( "Weapons" )]
partial class R870 : GunfightWeapon
{
	public static readonly Model WorldModel = Model.Load( "models/r870/w_r870.vmdl" );
	public override string ViewModelPath => "models/r870/fp_r870.vmdl";
	public override float PrimaryRate => 1;
	public override float SecondaryRate => 1;
	public override AmmoType AmmoType => AmmoType.Buckshot;
	public override int ClipSize => 6;
	public override float ReloadTime => 1.2f;
	public override int Bucket => 2;
	public override int BucketWeight => 200;

	[Net, Predicted]
	public bool StopReloading { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = 6;
	}

	public override void Simulate( Client owner )
	{
		base.Simulate( owner );

		if ( IsReloading && (Input.Pressed( InputButton.PrimaryAttack ) || Input.Pressed( InputButton.SecondaryAttack )) )
		{
			StopReloading = true;
		}
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
		PlaySound( "shotgun1_shoot" );

		//
		// Shoot the bullets
		//
		ShootBullet( 0.2f, 0.3f, 20.0f, 2.0f, 4 );
	}

	public override void AttackSecondary()
	{
		//
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;
	}

	public override void OnReloadFinish()
	{
		var stop = StopReloading;

		StopReloading = false;
		IsReloading = false;

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( AmmoClip >= ClipSize )
			return;

		if ( Owner is GunfightPlayer player )
		{
			var ammo = player.TakeAmmo( AmmoType, 1 );
			if ( ammo == 0 )
				return;

			AmmoClip += ammo;

			if ( AmmoClip < ClipSize && !stop )
			{
				Reload();
			}
			else
			{
				FinishReload();
			}
		}
	}

	[ClientRpc]
	protected virtual void FinishReload()
	{
		ViewModelEntity?.SetAnimParameter( "reload_finished", true );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 3 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );
		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + lastAttack.LerpInverse( 1.2f, 0 ) * 0.5f );

		// center
		{
			var shootEase = 1 + Easing.BounceIn( lastAttack.LerpInverse( 0.3f, 0.0f ) );
			draw.Ring( center, 15 * shootEase, 14 * shootEase );
		}

		// outer lines
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.4f, 0.0f ) );
			var length = 30.0f;
			var gap = 30.0f + shootEase * 50.0f;
			var thickness = 4.0f;
			var extraAngle = 30 * shootEase;

			draw.CircleEx( center + Vector2.Right * gap, length, length - thickness, 32, 220, 320 );
			draw.CircleEx( center - Vector2.Right * gap, length, length - thickness, 32, 40, 140 );

			draw.Color = draw.Color.WithAlpha( 0.1f );
			draw.CircleEx( center + Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 220, 320 );
			draw.CircleEx( center - Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 40, 140 );
		}
	}
}
