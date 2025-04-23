using System;


namespace GodotNetworking
{
	public interface IBlittable
	{
		Object[] ToBlittable();

		Object fromBlittable(Object[] data);
	}
}