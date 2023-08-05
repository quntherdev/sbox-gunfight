﻿namespace Facepunch.Gunfight;

public partial class GunfightPlayer : AnimatedEntity, IHudMarker
{
	[Net] public float Armour { get; set; }
	[Net] public float MaxHealth { get; set; }
	[Net] public PlayerInventory Inventory { get; set; }
	[Net, Predicted] public TimeSince TimeSinceDropped { get; set; }
	[Net] public CapturePointEntity CapturePoint { get; set; }
	[Net] public string PlayerLocation { get; set; } = "";
	public string SpawnPointTag { get; set; } = null;
	[Net, Predicted] public PlayerController Controller { get; set; }

	[Net] public Entity ActiveChild { get; set; }
	[ClientInput] public Entity ActiveChildInput { get; set; }

	public bool IsRegen { get; set; }

	public bool IsAiming => Controller?.IsAiming ?? false;

	Sound HeartbeatSound { get; set; }

	public GunfightCamera PlayerCamera { get; set; } = new();

	/// <summary>
	/// Create a physics hull for this player. The hull stops physics objects and players passing through
	/// the player. It's basically a big solid box. It also what hits triggers and stuff.
	/// The player doesn't use this hull for its movement size.
	/// </summary>
	public virtual void CreateHull()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		//var capsule = new Capsule( new Vector3( 0, 0, 16 ), new Vector3( 0, 0, 72 - 16 ), 32 );
		//var phys = SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, capsule );


		//	phys.GetBody(0).RemoveShadowController();

		// TODO - investigate this? if we don't set movetype then the lerp is too much. Can we control lerp amount?
		// if so we should expose that instead, that would be awesome.
		EnableHitboxes = true;
	}

	[ClientRpc]
	public void RpcSetupCamera()
	{
		PlayerCamera = new();
	}
	
	public void Respawn()
	{
		Game.AssertServer();

		MaxHealth = 100;
		LifeState = LifeState.Alive;
		Health = MaxHealth;
		Velocity = Vector3.Zero;

		CreateHull();

		ResetInterpolation();

		SetModel( "models/citizen/citizen.vmdl" );

		RpcSetupCamera( To.Single( Client ) );

		Controller = new PlayerController();

		Inventory?.DeleteContents();
		Inventory = new PlayerInventory( this );

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		ClearAmmo();
		Clothing.DressEntity( this );

		GiveAll();
		ClearEffects();
		ClearKillStreak();

		Transmit = TransmitType.Always;
		Tags.Add( "player" );

		GameManager.Current?.MoveToSpawnpoint( this );
	}

	public GunfightPlayer()
	{
		if ( Game.IsClient )
		{
			HeartbeatSound = Sound.FromScreen( "sounds/heartbeat.sound" );
			HeartbeatSound.SetVolume( 0f );
		}
	}

	public void GiveAll()
	{
		var overrideLoadout = GamemodeSystem.Current?.PlayerLoadout( this ) ?? false;
		// Use a default loadout
		if ( !overrideLoadout )
		{
			GiveAmmo( AmmoType.Pistol, MaxAmmo( AmmoType.Pistol ) );
			GiveAmmo( AmmoType.SMG, MaxAmmo( AmmoType.SMG ) );
			GiveAmmo( AmmoType.Rifle, MaxAmmo( AmmoType.Rifle ) );
			GiveAmmo( AmmoType.DMR, MaxAmmo( AmmoType.DMR ) );
			GiveAmmo( AmmoType.Sniper, MaxAmmo( AmmoType.Sniper ) );
			GiveAmmo( AmmoType.Shotgun, MaxAmmo( AmmoType.Shotgun ) );

			var primary = WeaponDefinition.Random( WeaponDefinition.FindFromSlot( WeaponSlot.Primary ) );
			GiveWeapon( primary, true );

			var secondary = WeaponDefinition.Random( WeaponDefinition.FindFromSlot( WeaponSlot.Secondary ) );
			GiveWeapon( secondary );
		}
	}

	public GunfightWeapon GiveWeapon( string name, bool makeActive = false, params string[] attachments )
	{
		var wpn = WeaponDefinition.CreateWeapon( name );
		Inventory.Add( wpn, makeActive );
		return wpn;
	}

	public GunfightWeapon GiveWeapon( WeaponDefinition def, bool makeActive = false, params string[] attachments )
	{
		var wpn = WeaponDefinition.CreateWeapon( def );
		Inventory.Add( wpn, makeActive );
		return wpn;
	}

	[ClientRpc]
	public void ClearEffects()
	{
		// STUB
	}

	[ClientRpc]
	public void RpcShowDeathCamera( ModelEntity killer )
	{
		PlayerCamera = new GunfightDeathCamera( killer );
	}

	public override void OnKilled()
	{
		GameManager.Current?.OnKilled( this );

		// Default life state is Respawning, this means the player will handle respawning after a few seconds
		LifeState newLifeState = LifeState.Respawning;

		// Inform the active gamemode
		GamemodeSystem.Current?.OnPlayerKilled( this, LastDamage, out newLifeState );

		TimeSinceKilled = 0;
		LifeState = newLifeState;
		StopUsing();
		Client?.AddInt( "deaths", 1 );


		if ( LastDamage.Attacker.IsValid() && LastDamage.Attacker is GunfightPlayer player )
		{
			Progression.GiveAward( player.Client, "Kill" );
			player.AddKill();
		}
		
		RpcShowDeathCamera( To.Single( Client ), LastDamage.Attacker as GunfightPlayer );

		if ( CapturePoint.IsValid() )
		{
			CapturePoint.RemovePlayer( this );
		}

		var primary = Inventory.PrimaryWeapon;
		if ( primary.IsValid() && Inventory.Drop( primary ) )
		{
			ActiveChild = null;
			primary.StartDecaying();
		}

		Inventory.DeleteContents();

		if ( LastDamage.HasTag( "blast" ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gib.vpcf" );
				if ( particles != null )
				{
					particles.SetPosition( 0, Position + Vector3.Up * 40 );
				}
			}
		}
		else
		{
			BecomeRagdollOnClient( Velocity, LastDamage.Position, LastDamage.Force, LastDamage.BoneIndex );
		}

		ClearEffects();

		Controller = null;

		// CameraMode = new GunfightDeathCamera( LastDamage.Attacker.IsValid() ? LastDamage.Attacker : this );

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
		{
			child.EnableDrawing = false;
		}

		// Inform the active gamemode
		GamemodeSystem.Current?.PostPlayerKilled( this, LastDamage );
	}

	[GameEvent.Tick.Client]
	protected void HeartbeatTick()
	{
		if ( this != Game.LocalPawn ) return;

		var hp = Health;
		if ( LifeState != LifeState.Alive ) hp = 100;

		var vol = 1 - hp.LerpInverse( 0, 25f, true ).Remap( 0, 1, 0.5f, 1 );
		HeartbeatSound.SetVolume( vol );
	}

	Entity lastWeapon;
	void SimulateAnimation( PlayerController controller )
	{
		if ( controller == null )
			return;
		
		CitizenAnimationHelper animHelper = new CitizenAnimationHelper( this );

		animHelper.WithWishVelocity( controller.WishVelocity );
		animHelper.WithVelocity( controller.Velocity );
		animHelper.WithLookAt( AimRay.Position + AimRay.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
		animHelper.AimAngle = EyeRotation;
		animHelper.FootShuffle = 0f;
		var height = controller.CurrentEyeHeight.LerpInverse( 32, 64, true );
		animHelper.DuckLevel = MathX.Lerp( animHelper.DuckLevel, 1 - height, Time.Delta * 10.0f );
		animHelper.VoiceLevel = ( Game.IsClient && Client.IsValid() ) ? Client.Voice.LastHeard < 0.5f ? Client.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = GroundEntity != null;
		animHelper.IsSitting = controller.HasTag( "sitting" );
		animHelper.IsNoclipping = controller.HasTag( "noclip" );
		animHelper.IsClimbing = controller.HasTag( "climbing" );
		animHelper.IsSwimming = this.GetWaterLevel() >= 0.5f;
		animHelper.IsWeaponLowered = false;

		SetAnimParameter( "skid", controller.Slide.IsActive ? 1 : 0 );

		if ( controller.HasEvent( "jump" ) ) animHelper.TriggerJump();
		if ( ActiveChild != lastWeapon ) animHelper.TriggerDeploy();

		if ( ActiveChild is GunfightWeapon weapon )
		{
			SetAnimParameter( "holdtype", weapon.WeaponDefinition?.HoldType.ToInt() ?? 1 );
		}
		else
		{
			animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
			animHelper.AimBodyWeight = 0.5f;
		}


		lastWeapon = ActiveChild;
	}

	/// <summary>
	/// Applies flashbang-like ear ringing effect to the player.
	/// </summary>
	/// <param name="strength">Can be approximately treated as duration in seconds.</param>
	[ClientRpc]
	public void Deafen( float strength )
	{
		Audio.SetEffect( "flashbang", strength, velocity: 20.0f, fadeOut: 4.0f * strength );
	}

	TimeSince TimeSinceKilled;
	public override void Simulate( IClient cl )
	{
		Rotation = LookInput.WithPitch( 0f ).ToRotation();
		
		SimulateWeapons( cl );
		
		if ( LifeState == LifeState.Respawning )
		{
			if ( TimeSinceKilled > 3 && Game.IsServer )
			{
				Respawn();
			}

			return;
		}

		Controller?.Simulate( cl, this );
		SimulateAnimation( Controller );

		if ( Input.Pressed( "View" ) )
		{
			if ( GamemodeSystem.Current?.AllowThirdPerson ?? false )
			{
				GunfightCamera.IsThirdPerson ^= true;
			}
		}

		if ( LifeState != LifeState.Alive )
			return;

		TickPlayerUse();

		if ( TimeSinceDamage > 5f && ( GamemodeSystem.Current?.CanPlayerRegenerate( this ) ?? true ) )
		{
			PassiveHeal();
			if ( Health != MaxHealth )
			{
				IsRegen = true;
			}
			else
			{
				IsRegen = false;
			}
		}
		else
		{
			IsRegen = false;
		}
	}

	protected void PassiveHeal()
	{
		Health += 10f * Time.Delta;
		Health = Health.Clamp( 0, MaxHealth );
	}

	public override void OnChildAdded( Entity child )
	{
		Inventory?.OnChildAdded( child );
	}

	public override void OnChildRemoved( Entity child )
	{
		Inventory?.OnChildRemoved( child );
	}

	public override void StartTouch( Entity other )
	{
		if ( TimeSinceDropped < 1f ) return;

		base.StartTouch( other );

		if ( Game.IsServer && other is GunfightWeapon weapon )
		{
			var ammoType = weapon.AmmoType;

			// Must have a weapon with the correct ammo type in inventory
			if ( !Inventory.HasWeaponWithAmmoType( ammoType ) )
				return;

			var taken = GiveAmmo( ammoType, weapon.AmmoClip );

			weapon.AmmoClip -= taken;

			if ( weapon.AmmoClip <= 0 )
				weapon.Delete();
		}
	}
	
	DamageInfo LastDamage;

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( GamemodeSystem.Current.IsValid() && !GamemodeSystem.Current.AllowDamage )
			return;

		var attacker = info.Attacker as GunfightPlayer;
		if ( attacker.IsValid() && !GamemodeSystem.Current.AllowFriendlyFire && Gamemode.FriendlyFireOverride == false )
		{
			if ( attacker.Team == Team )
				return;
		}

		LastDamage = info;

		// Headshot
		var isHeadshot = info.Hitbox.HasTag( "head" );
		if ( isHeadshot )
		{
			info.Damage *= 2.5f;
		}

		if ( info.HasTag( "bullet" ) )
		{
			Sound.FromScreen( To.Single( Client ), "sounds/player/damage_taken_shot.sound" );
		}

		this.ProceduralHitReaction( info );

		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;

		if ( Game.IsServer && Armour > 0 )
		{
			Armour -= info.Damage;

			if ( Armour < 0 )
			{
				info.Damage = Armour * -1;
				Armour = 0;
			}
			else
			{
				info.Damage = 0;
			}
		}

		if ( info.HasTag( "blast" ) )
		{
			Deafen( To.Single( Client ), info.Damage.LerpInverse( 0, 60 ) );
		}

		if ( Health > 0 && info.Damage > 0 )
		{
			Health -= info.Damage;
			if ( Health <= 0 )
			{
				Health = 0;
				OnKilled();
			}
		}

		TimeSinceDamage = 0;

		if ( attacker.IsValid() )
		{
			if ( attacker != this )
			{
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ), isHeadshot );
			}

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
		}

		//
		// Add a score to the killer
		//
		if ( LifeState != LifeState.Alive && info.Attacker != null )
		{
			if ( info.Attacker.Client != null && info.Attacker != this )
			{
				info.Attacker.Client.AddInt( "kills" );
			}
		}
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv, bool isHeadshot )
	{
		if ( isHeadshot )
		{
			Sound.FromScreen( "ui.hit" ).SetPitch( 1.25f );
		}
		else
		{
			Sound.FromScreen( "ui.hit" );
		}
		HitIndicator.Current?.OnHit( pos, amount, isHeadshot );
	}

	[Net, Predicted] public TimeSince TimeSinceDamage { get; set; }

	[ClientRpc]
	public void TookDamage( Vector3 pos, bool headshot = false )
	{
		DamageIndicator.Current?.OnHit( pos, headshot );
	}

	[ClientRpc]
	public void PlaySoundFromScreen( string sound )
	{
		Sound.FromScreen( sound );
	}

	float GetFootstepDelay()
	{
		return Controller.GetMechanic<DuckMechanic>()?.IsActive ?? false ? 0.5f : 0.18f;
	}
	
	TimeSince timeSinceLastFootstep = 0;

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( !Game.IsServer )
			return;

		if ( LifeState != LifeState.Alive )
			return;

		if ( timeSinceLastFootstep < GetFootstepDelay() )
			return;

		var ctrl = Controller as PlayerController;

		// No footsteps while sliding
		if ( ctrl.Slide.IsActive )
			return;

		volume *= Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 0.2f;
		
		// Sneaky beaky
		if ( Controller.GetMechanic<DuckMechanic>()?.IsActive ?? false ) volume *= 0.45f;

		timeSinceLastFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		if ( ctrl.IsSprinting )
		{
			var sound = PlaySound( "sounds/player/foley/gear/player.walk.gear.sound" );
			sound.SetVolume( volume );
		}

		tr.Surface.DoFootstep( this, tr, foot, volume * 20 );
	}

	[ConCmd.Admin( "gunfight_debug_sethp" )]
	public static void Debug_SetHP( int hp )
	{
		var pawn = ConsoleSystem.Caller.Pawn as GunfightPlayer;
		if ( pawn.IsValid() )
		{
			pawn.Health = hp;
		}
	}
	[ConCmd.Admin( "gunfight_debug_damage" )]
	public static void Debug_Damage( int amt )
	{
		var pawn = ConsoleSystem.Caller.Pawn as GunfightPlayer;
		if ( pawn.IsValid() )
		{
			pawn.TakeDamage( DamageInfo.FromBullet( pawn.Position, 100f, amt ) );
		}
	}

	[ConCmd.Server( "kill" )]
	public static void Suicide()
	{
		var pawn = ConsoleSystem.Caller.Pawn as GunfightPlayer;
		pawn?.TakeDamage( DamageInfo.Generic( pawn.MaxHealth ) );
	}
}
