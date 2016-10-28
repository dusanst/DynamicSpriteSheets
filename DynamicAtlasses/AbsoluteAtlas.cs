using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objects of this class can contain textures of differing sizes.
/// As a result this atlas can be used for inserting various kinds of textures,
/// unlike GridAtlas which is supposed to contain only textures with same size.
/// </summary>
public class AbsoluteAtlas : DynamicAtlas
{
	#region Fields

	/// <summary>
	/// Reference to the packer used for packing textures in atlas.
	/// </summary>
	private readonly TexturePacker packer;

	/// <summary>
	/// Initial size of the atlas texture.
	/// </summary>
	private readonly Vector2 initialSize;

	private static readonly Vector2 DefaultInitialSize = new Vector2(64, 64);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates AbsoluteAtlas.
	/// </summary>
	/// <param name="maxSize">Max size of the atlas texture.</param>
	public AbsoluteAtlas(string atlasName, Vector2 maxSize) : this(atlasName, maxSize, DefaultInitialSize) {}

	/// <summary>
	/// Creates AbsoluteAtlas.
	/// Setting initial size is good if minimum size of textures can be predicted.
	/// </summary>
	/// <param name="maxSize">Max size of the atlas texture. Must be greater or equal than 64 x 64.</param>
	/// <param name="initialSize">Initial size of the atlas texture. Must be smaller or equal to MaxSize</param>
	public AbsoluteAtlas(string atlasName, Vector2 maxSize, Vector2 initialSize)
		: base(atlasName, maxSize)
	{
		this.initialSize = initialSize;
		if (initialSize.x > maxSize.x || initialSize.y > maxSize.y)
		{
			Debug.LogError("Absolute atlas initial size can not be greater than maximum size of it. " + atlasName);
		}
		packer = new TexturePacker(initialSize, false);
	}

	#endregion

	#region Methods

	protected override bool AddTextures(List<Texture> textures)
	{
		Vector2 minimalTextureSize = Vector2.zero;
		List<Rect> texturesToAdd = new List<Rect>();
		List<Rect> positions;
		if (TryFindingMinimalAtlasTextureSize(textures, ref minimalTextureSize) && packer.binWidth == (int)minimalTextureSize.x
			&& packer.binHeight == (int)minimalTextureSize.y)
		{
			// Minimal texture size is equal to the current size of packer, just try adding new textures

			foreach (Texture texture in textures)
			{
				texturesToAdd.Add(new Rect(0, 0, texture.width + 2f * Padding, texture.height + 2f * Padding));
			}

			positions = packer.Insert(texturesToAdd, TexturePacker.FreeRectChoiceHeuristic.RectBestShortSideFit);

			if (positions != null)
			{
				// there is enough position in texture, just draw new textures.

				bool atlasTextureNotCreated = AtlasTexture == null;
				if (atlasTextureNotCreated)
				{
					AtlasTexture = CreateRenderTexture(initialSize);
				}

				// Get ready to render
				Graphics.SetRenderTarget(AtlasTexture as RenderTexture);

				DrawNewTextures(textures, positions, 0, false);

				// Remove the render target
				Graphics.SetRenderTarget(null);

				if (atlasTextureNotCreated)
				{
					// TODO: Inform Sprites that are using this atlas that texture has changed
				}

				return true;
			}
		}

		// we don't need unused sprite data in list
		Atlas.RemoveAllSpriteData((spriteData) => !((DynamicSpriteData)spriteData).IsUsed());

		numberOfUnused = 0;

		Vector2 newTextureSize = Vector2.zero;
		if (!TryFindingMinimalAtlasTextureSize(textures, ref newTextureSize))
		{
			return false;
		}

		while (true)
		{
			texturesToAdd.Clear();
			foreach (var data in Atlas.SpriteList)
			{
				texturesToAdd.Add(new Rect(0, 0, data.Width + 2f * Padding, data.Height + 2f * Padding));
			}
			foreach (Texture texture in textures)
			{
				texturesToAdd.Add(new Rect(0, 0, texture.width + 2f * Padding, texture.height + 2f * Padding));
			}

			packer.Init(newTextureSize, false);
			positions = packer.Insert(texturesToAdd, TexturePacker.FreeRectChoiceHeuristic.RectBestShortSideFit);

			if (positions != null)
			{
				RenderTexture newTexture = CreateRenderTexture(newTextureSize);
				// Get ready to render
				Graphics.SetRenderTarget(newTexture);

				DrawOldAtlas(newTexture, positions);
				DrawNewTextures(textures, positions, Atlas.SpriteList.Count, false);

				// Remove the render target
				Graphics.SetRenderTarget(null);

				// TODO: Inform Sprites that are using this atlas that texture has changed

				return true;
			}

			if (!TryResizingSmallerEdge(ref newTextureSize))
			{
				return false;
			}
		}
	}

	/// <summary>
	/// Tries finding minimal atlas texture size for storing newTextures in Atlas.
	/// Calculations are based on minimal area which new textures should occupy.
	/// </summary>
	/// <returns>True, if minimal possible texture size is not greater than MaxSize; otherwise, false.</returns>
	/// <param name="newTextures">New textures which should be added in Atlas.</param>
	/// <param name="minimalTextureSize">Minimal texture size is stored in this field if method returns true.</param>
	private bool TryFindingMinimalAtlasTextureSize(List<Texture> newTextures, ref Vector2 minimalTextureSize)
	{
		float minimalTextureArea = MinimalAtlasTextureArea(newTextures);

		if (minimalTextureArea > MaxSize.x * MaxSize.y)
		{
			return false;
		}

		minimalTextureSize = initialSize;
		while (true)
		{
			if (minimalTextureSize.x * minimalTextureSize.y >= minimalTextureArea)
			{
				return true;
			}
			if (!TryResizingSmallerEdge(ref minimalTextureSize))
			{
				return false;
			}
		}
	}

	/// <summary>
	/// Returns minimal atlas texture area after adding newTextures in atlas.
	/// </summary>
	/// <returns>The atlas texture area.</returns>
	/// <param name="newTextures">New textures which should be added in atlas.</param>
	private float MinimalAtlasTextureArea(List<Texture> newTextures)
	{
		float area = 0;
		foreach (var spriteData in Atlas.SpriteList)
		{
			area += (spriteData.Width + 2f * Padding) * (spriteData.Height + (2f * Padding));
		}
		foreach (var texture in newTextures)
		{
			area += (texture.width + 2f * Padding) * (texture.height + 2f * Padding);
		}
		return area;
	}

	/// <summary>
	/// Resizes one vector's edge by factor of two if it can fit MaxSize after resizing.
	/// First tries resizing smaller edge.
	/// </summary>
	/// <returns>True, if resizing succeeded, otherwise false.</returns>
	/// <param name="newTextureSize">New texture size.</param>
	private bool TryResizingSmallerEdge(ref Vector2 newTextureSize)
	{
		if (newTextureSize.x * 2f <= MaxSize.x && newTextureSize.y * 2f <= MaxSize.y)
		{
			// we want to resize smaller side
			if (newTextureSize.x <= newTextureSize.y)
			{
				newTextureSize = new Vector2(newTextureSize.x * 2f, newTextureSize.y);
			}
			else
			{
				newTextureSize = new Vector2(newTextureSize.x, newTextureSize.y * 2f);
			}
		}
		else if (newTextureSize.x * 2f <= MaxSize.x)
		{
			newTextureSize = new Vector2(newTextureSize.x * 2f, newTextureSize.y);
		}
		else if (newTextureSize.y * 2f <= MaxSize.y)
		{
			newTextureSize = new Vector2(newTextureSize.x, newTextureSize.y * 2f);
		}
		else
		{
			return false;
		}
		return true;
	}

	#endregion
}
