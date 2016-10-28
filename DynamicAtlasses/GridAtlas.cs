using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Objects of this class should contain fixed sized textures.
/// Textures are arranged in grid order inside of atlas texture.
/// </summary>
public class GridAtlas : DynamicAtlas
{
	#region Fields

	private readonly int initialCapacity;

	#endregion

	#region Public Properties

	/// <summary>
	/// Width and height of the single sprite. If added textures have different sizes, than they will be stretched to this size
	/// but when they are applied to object of appropriate size texture will act as it is in the original size.
	/// </summary>
	public Vector2 SpriteSize { get; private set; }

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new texture atlas with defined max size and sprite size.
	/// </summary>
	/// <param name="maxSize">The maximum size of the atlas. Max size x and y should be power of two.</param>
	/// <param name="spriteSize">The size of the sprites in the atlas.</param>
	/// <param name="initialCapacity">Initial capacity is good to set if minimum number of sprites can be predicted, it is measured in number of sprites.</param>
	public GridAtlas(string atlasName, Vector2 maxSize, Vector2 spriteSize, int initialCapacity) : base(atlasName, maxSize)
	{
		SpriteSize = spriteSize;
		this.initialCapacity = initialCapacity;
	}

	#endregion

	#region Private Methods

	protected override bool AddTextures(List<Texture> textures)
	{
		// The number of sprites after adding all sprites.
		// Number of sprites cannot be reduced because unused are not removed just replaced with new ones.
		int spriteCount = Mathf.Max(Atlas.SpriteList.Count, textures.Count + UsedSpriteCount);
		if (spriteCount < initialCapacity)
		{
			spriteCount = initialCapacity;
		}

		// This is the needed texture size
		Vector2 newTextureSize = GetMinimumTextureSize(spriteCount, SpriteSize, (int)MaxSize.x);

		// If the required size is larger than we can do, abort the process
		if (newTextureSize.x > MaxSize.x || newTextureSize.y > MaxSize.y)
		{
			Debug.LogError(string.Format("Atlas: {0} with maximum size: {1} cannot support size {2}", AtlasName, MaxSize, newTextureSize));
			return false;
		}

		bool atlasTextureChanged = false;

		if (AtlasTexture == null)
		{
			AtlasTexture = CreateRenderTexture(newTextureSize);
			Graphics.SetRenderTarget((RenderTexture)AtlasTexture);
			atlasTextureChanged = true;
		}
		else if (newTextureSize.x > AtlasTexture.width || newTextureSize.y > AtlasTexture.height)
		{
			RenderTexture newTexture = CreateRenderTexture(newTextureSize);
			Graphics.SetRenderTarget(newTexture);
			DrawOldAtlas(newTexture, CalculateSpritePosition);
			atlasTextureChanged = true;
		}
		else
		{
			Graphics.SetRenderTarget(AtlasTexture as RenderTexture);
		}

		DrawNewTextures(textures, CalculateSpritePosition, Atlas.SpriteList.Count, true);

		// Remove the render target
		Graphics.SetRenderTarget(null);

		if (atlasTextureChanged)
		{
			// TODO: Inform Sprites that are using this atlas that texture has changed
		}

		return true;
	}

	private Rect CalculateSpritePosition(int index, Texture newTexture)
	{
		float spriteWidth = SpriteSize.x + 2f * Padding;
		float spriteHeight = SpriteSize.y + 2f * Padding;

		int spritesPerWidth = Mathf.FloorToInt(newTexture.width / spriteWidth);
		float x = (index % spritesPerWidth) * spriteWidth;
		float y = (index / spritesPerWidth) * spriteHeight;
		return new Rect(x, y, spriteWidth, spriteHeight);
	}

	#endregion

	#region Public Methods

	public void ChangeSize(Vector2 maxSize, Vector2 spriteSize)
	{
		if (AtlasTexture == null)
		{
			// only if atlas is empty this action can be performed
			MaxSize = maxSize;
			SpriteSize = spriteSize;
		}
	}

	/// <summary>
	/// Returns the minimum required texture size to fit the sprites in it. The returned texture
	/// size will be a power of 2.
	/// </summary>
	/// <param name="spriteCount">The number of sprites needed to fit in.</param>
	/// <param name="spriteSize">The size of the textures.</param>
	/// <returns>The minimum size.</returns>
	public static Vector2 GetMinimumTextureSize(int spriteCount, Vector2 spriteSize, int maxTextureWidth)
	{
		int maxInOneRow = Mathf.FloorToInt(maxTextureWidth / (spriteSize.x + 2f * Padding));
		if (maxInOneRow == 0)
		{
			Debug.LogError(string.Format("Sprites with size: {0} cannot fit in atlas with maximum width: {1}", spriteSize, maxTextureWidth));
			return Vector2.zero;
		}
		if (maxInOneRow >= spriteCount)
		{
			int minWidth = Mathf.CeilToInt(spriteCount * (spriteSize.x + 2f * Padding));
			return new Vector2(Mathf.NextPowerOfTwo(minWidth), Mathf.NextPowerOfTwo(Mathf.CeilToInt(spriteSize.y + 2f * Padding)));
		}

		int numberOfRows = Mathf.CeilToInt(((float)spriteCount) / maxInOneRow);
		int minHeight = Mathf.CeilToInt(numberOfRows * (spriteSize.y + 2f * Padding));
		return new Vector2(maxTextureWidth, Mathf.NextPowerOfTwo(minHeight));
	}

	#endregion
}
