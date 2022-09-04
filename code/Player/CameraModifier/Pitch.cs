namespace Facepunch.Gunfight.ScreenShake;

public class Pitch : CameraModifier
{
	float Length = 5.0f;
	float Size = 1.0f;

	TimeSince lifeTime = 0;

	public Pitch( float length = 1.5f, float size = 1.0f )
	{
		Length = length;
		Size = size;
	}

	public override bool Update( ref CameraSetup cam )
	{
		var delta = ((float)lifeTime).LerpInverse( 0, Length, true );
		delta = Easing.EaseOut( delta );

		cam.Rotation *= Rotation.From( new( Size * ( 1 - delta ), 0, Size * ( 1 - delta ) ) );

		return lifeTime < Length;
	}
}
