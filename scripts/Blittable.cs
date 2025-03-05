using System;

public interface IBlittable
{
	Object[] ToBlittable();

	Object fromBlittable(Object[] data);
}