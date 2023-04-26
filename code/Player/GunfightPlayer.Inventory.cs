namespace Facepunch.Gunfight;

public partial class GunfightPlayer
{
	protected int GetSlotIndexFromInput( string action )
	{
		return action switch
		{
			"Slot1" => 0,
			"Slot2" => 1,
			"Slot3" => 2,
			"Slot4" => 3,
			"Slot5" => 4,
			_ => -1
		};
	}

	protected void TrySlotFromInput( string action )
	{
		if ( Input.Pressed( action ) )
		{
			Input.Clear( action );

			if ( Inventory.GetSlot( GetSlotIndexFromInput( action ) ) is Entity weapon )
				ActiveChildInput = weapon;
		}
	}

	public void BuildWeaponInput()
	{
		for ( int i = 1; i < 6; i++ )
		{
			TrySlotFromInput( $"Slot{i}" );
		}
	}
}
