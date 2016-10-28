/// <summary>
/// Sprite data used in dynamic atlases,
/// has some additional info.
/// </summary>
public class DynamicSpriteData : SpriteData
{
	/// <summary>
	/// Gets or sets number of references to the coresponding sprite.
	/// If count is 0 than nobody uses coresponding sprite.
	/// </summary>
	public int ReferenceCount { get; set; }

	/// <summary>
	/// True if sprite has been lost because atlas texture got lost.
	/// </summary>
	public bool SpriteLost { get; set; }

	/// <summary>
	/// Returns whether this coresponding sprite inside of atlas is currently used.
	/// If it is not used than it can be overriden by another sprite.
	/// </summary>
	public bool IsUsed()
	{
		return ReferenceCount > 0;
	}

	public DynamicSpriteData(string name) : base(name) {}

	/// <summary>
	/// Resets sprite's reference count; so it becomes unused.
	/// </summary>
	public void Reset()
	{
		ReferenceCount = 0;
		SpriteLost = false;
	}

	/// <summary>
	/// Increases reference count to sprite represented by this sprite data.
	/// </summary>
	public void Acquire()
	{
		if (ReferenceCount < 0)
		{
			// fail safe
			ReferenceCount = 0;
		}
		ReferenceCount++;
	}

	/// <summary>
	/// Decreases reference count to sprite represented by this sprite data.
	/// </summary>
	public void Release()
	{
		ReferenceCount--;
		if (ReferenceCount < 0)
		{
			// fail safe
			ReferenceCount = 0;
		}
	}
}
