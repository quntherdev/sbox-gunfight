﻿namespace Facepunch.Gunfight;

[Library( "1911" )]
public partial class USP : GunfightWeapon
{
	private static Model USPModel = Cloud.Model( "https://asset.party/facepunch/w_usp" );
	private static Model USPViewModel = Cloud.Model( "https://asset.party/facepunch/v_usp" );
	
	public override Model WeaponModel => USPModel;
	public override Model WeaponViewModel => USPViewModel;
	
	public override void CreateViewModel()
	{
		ViewModelEntity = new ViewModel();
		ViewModelEntity.Weapon = this;
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.Model = USPViewModel;

		var arms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
		arms.SetParent( ViewModelEntity, true );
		arms.EnableViewmodelRendering = true;
		
		ViewModelEntity.SetBodyGroup( 2, 1 );
		ViewModelEntity.SetBodyGroup( 4, 2 );
	}
}