using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The class responsible for creating atlases in real time.
/// </summary>
public abstract class DynamicAtlas
{
	#region Constants & Readonly Fields

	/// <summary>
	/// How many pixels should be around the sprite inside of atlas texture.
	/// Sprite is the same size as it's texture, but it have Padding pixels on each of its sides,
	/// those pixels are copied from texture borders in order to prevent some strange effects that Bilinear filter mode produces.
	/// </summary>
	protected const float Padding = 1f;

	#endregion

	#region Fields

	private static Material atlasingMaterial;

	// TODO: Change to any implementation of Sprite Sheet you use
	private Atlas atlas;

	protected int numberOfUnused;

	/// <summary>
	/// Helper list made for reducing number of instantiations.
	/// </summary>
	private readonly List<Texture> textureHelperList = new List<Texture>(new Texture[] {null});

	/// <summary>
	/// Helper list made for reducing number of instantiations.
	/// </summary>
	private readonly List<string> stringHelperList = new List<string>(new string[] {null});

	private readonly string atlasName;

	#endregion

	#region Properties

	#region Public

	public string AtlasName
	{
		get { return atlasName; }
	}

	/// <summary>
	/// The maximum width and height of the atlas. Dimensions should be a power of 2 for optimized rendering.
	/// </summary>
	public Vector2 MaxSize { get; protected set; }

	/// <summary>
	/// The number of valid sprites in the atlas
	/// </summary>
	public int UsedSpriteCount
	{
		get { return Atlas.SpriteList.Count - numberOfUnused; }
	}

	private static Shader Shader
	{
		get
		{
			// TODO: Change to any shader you use for sprites
			return Shader.Find("Unlit/Texture");
		}
	}

	#endregion

	#region Protected

	/// <summary>
	/// Returns Atlas that holds data about this dynamic atlas.
	/// </summary>
	protected Atlas Atlas
	{
		get
		{
			if (atlas == null)
			{
				// TODO: Instantiate Atlas. If it is a MonoBehaviour you can make them all on one shered game object (DynamicAtlasGO.AddComponent<Atlas>())
				atlas = new Atlas {SpriteMaterial = new Material(Shader)};
			}
			return atlas;
		}
	}

	/// <summary>
	/// The material used for rendering atlas texture.
	/// </summary>
	protected static Material AtlasingMaterial
	{
		get
		{
			if (atlasingMaterial == null)
			{
				atlasingMaterial = new Material(Shader);
			}
			return atlasingMaterial;
		}
	}

	/// <summary>
	/// Gets/sets texture used by Atlas.
	/// </summary>
	protected Texture AtlasTexture
	{
		get
		{
			if (atlas != null)
			{
				return atlas.Texture;
			}
			return null;
		}
		set { Atlas.SpriteMaterial.mainTexture = value; }
	}

	/// <summary>
	/// Gets the size of the current atlas texture.
	/// </summary>
	protected Vector2 TextureSize
	{
		get { return new Vector2(AtlasTexture.width, AtlasTexture.height); }
	}

	#endregion

	#endregion

	#region Constructors

	protected DynamicAtlas(string atlasName, Vector2 maxSize)
	{
		MaxSize = maxSize;
		this.atlasName = atlasName;
	}

	#endregion

	#region Public Methods

	public bool IsEmpty()
	{
		return AtlasTexture == null;
	}

	/// <summary>
	/// Acquires the one texture in atlas.
	/// If texture is already in atlas than reference count to texture is increased,
	/// and true is returned.
	/// </summary>
	/// <returns>True, if texture is in atlas; otherwise, false.</returns>
	public bool AcquireByName(string name)
	{
		if (atlas != null && name != null)
		{
			var spriteData = Atlas.GetSprite(name) as DynamicSpriteData;
			if (spriteData != null && !spriteData.SpriteLost)
			{
				if (!spriteData.IsUsed())
				{
					numberOfUnused--;
				}

				spriteData.Acquire();
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns whether specified texture is currently in the atlas.
	/// True even if that texture is currently unused, or lost.
	/// </summary>
	public bool ContainsTexture(string name)
	{
		if (atlas != null && name != null)
		{
			var spriteData = Atlas.GetSprite(name);
			return spriteData != null;
		}
		return false;
	}

	/// <summary>
	/// Acquires the one texture in atlas.
	/// If texture is already in atlas than reference count to texture is increased,
	/// if texture is not in atlas than it is added in atlas.
	/// Texture is added in atlas if there is enough space in atlas.
	/// </summary>
	/// <param name="texture">Texture to be acquired.</param>
	/// <returns>True, if texture is added or is already in atlas; false, if texture inserting failed.</returns>
	public bool Acquire(Texture texture)
	{
		textureHelperList[0] = texture;
		bool retValue = Acquire(textureHelperList);
		if (textureHelperList.Count == 0)
		{
			textureHelperList.Add(null);
		}
		return retValue;
	}

	/// <summary>
	/// Acquires a list of textures to the atlas.
	/// If texture is already in atlas than reference count to texture is increased,
	/// if texture is not in atlas than it is added in atlas.
	/// Texture is added in atlas if there is enough space in atlas.
	/// </summary>
	/// <param name="textures">Textures to be acquired.</param>
	/// <returns>True, if all textures are added or already in atlas; otherwise, false.</returns>
	public bool Acquire(List<Texture> textures)
	{
		if (textures == null)
		{
			return false;
		}

		// We want to increase reference count of textures which are already in atlas
		for (int i = 0; i < textures.Count; i++)
		{
			var data = Atlas.GetSprite(textures[i].name) as DynamicSpriteData;
			if (data != null)
			{
				if (!data.SpriteLost)
				{
					data.Acquire();
				}
				else
				{
					OverwriteOldTexture(textures[i]);
				}
			}
		}

		// we don't won't either duplicate textures with same name in atlas, or null textures
		textures.RemoveAll((texture) => texture == null || Atlas.GetSprite(texture.name) != null);

		if (textures.Count > 0)
		{
			bool returnVal = AddTextures(textures);
			return returnVal;
		}
		// all textures already in atlas
		return true;
	}

	/// <summary>
	/// Releases texture with provided name in atlas if there is such.
	/// When the last user releases texture from atlas than it is marked as unused, and can be run over by other texture.
	/// </summary>
	/// <param name="textureName">Name of the texture that will be released.</param>
	public void Release(string textureName)
	{
		stringHelperList[0] = textureName;
		Release(stringHelperList);
	}

	/// <summary>
	/// Releases all textures with provided names in atlas.
	/// When the last user releases texture from atlas than it is marked as unused, and can be run over by other texture.
	/// Requires:
	/// Every name in provided list is unique in that list.
	/// </summary>
	/// <param name="textureNames">Names of textures that will be released.</param>
	public void Release(List<string> textureNames)
	{
		if (textureNames != null && textureNames.Count > 0 && atlas != null)
		{
			// If sprite's OnDestroy method is called after DynamicAtlasManager's OnApplicationQuit 
			// calling Atlas property would cause creating new GameObject; Because it was previously deleted from DynamicAtlasManager.

			for (int i = 0; i < textureNames.Count; i++)
			{
				var spriteData = Atlas.GetSprite(textureNames[i]) as DynamicSpriteData;
				if (spriteData != null && spriteData.IsUsed())
				{
					spriteData.Release();
					if (!spriteData.IsUsed())
					{
						numberOfUnused++;
					}
				}
			}
		}
	}

	public void OverwriteOldTexture(Texture texture)
	{
		textureHelperList[0] = texture;
		OverwriteOldTextures(textureHelperList);
	}

	/// <summary>
	/// Overwrites textures in atlas by provided textures.
	/// Provided textures should have same name as textures from atlas.
	/// </summary>
	public void OverwriteOldTextures(List<Texture> textures)
	{
		// Get ready to render
		Graphics.SetRenderTarget(AtlasTexture as RenderTexture);

		// Mesh we will write to.
		Mesh mesh = new Mesh();
		mesh.MarkDynamic();

		// Initialize the data we need to fill
		Vector3[] positions = new Vector3[4];
		Vector2[] uvs = new Vector2[4];
		int[] triangles = new int[6];

		for (int i = 0; i < textures.Count; i++)
		{
			var spriteData = Atlas.GetSprite(textures[i].name) as DynamicSpriteData;

			if (spriteData == null)
			{
				Debug.LogWarning("Can not find sprite with name: " + textures[i].name + " in atlas while trying to overwrite it");
				continue;
			}

			if (!spriteData.IsUsed())
			{
				numberOfUnused--;
			}

			spriteData.Acquire();

			if (!spriteData.SpriteLost)
			{
				// someone already regenerated this sprite
				continue;
			}

			spriteData.SpriteLost = false;

			SetPosition(new Rect(spriteData.X - Padding, spriteData.Y - Padding, spriteData.Width + Padding, spriteData.Height + Padding),
						TextureSize, spriteData, positions, 0);

			// Set the UVs
			SetupUvs(uvs, textures[i]);

			// Set the triangles
			SetTriangles(0, triangles);

			mesh.Clear();
			// Add the data to the mesh
			mesh.vertices = positions;
			mesh.uv = uvs;
			mesh.triangles = triangles;

			// Set the texture to render to the new atlas
			AtlasingMaterial.mainTexture = textures[i];
			// Activate the material
			AtlasingMaterial.SetPass(0);

			// Draw the texture to the atlas
			Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
			MarkTextureForReusage();
		}

		// Cleanup the mesh, otherwise it will leak, unity won't do this on his own
		Object.Destroy(mesh);

		// Remove the render target
		Graphics.SetRenderTarget(null);
	}

	private static void SetupUvs(Vector2[] uvs, Texture texture)
	{
		uvs[0] = new Vector2(0 - (Padding / texture.width), 1 + (Padding / texture.height));
		uvs[1] = new Vector2(1 + (Padding / texture.width), 1 + (Padding / texture.height));
		uvs[2] = new Vector2(0 - (Padding / texture.width), 0 - (Padding / texture.height));
		uvs[3] = new Vector2(1 + (Padding / texture.width), 0 - (Padding / texture.height));
	}

	/// <summary>
	/// Marks all textures in atlas as unused.
	/// </summary>
	public void Clear()
	{
		if (atlas != null)
		{
			for (int i = 0; i < Atlas.SpriteList.Count; i++)
			{
				var spriteData = Atlas.SpriteList[i] as DynamicSpriteData;
				if (spriteData != null && spriteData.IsUsed())
				{
					spriteData.Reset();
					numberOfUnused++;
				}
			}
		}
	}

	/// <summary>
	/// Destroys the atlas and the resources that it uses.
	/// </summary>
	public void Destroy()
	{
		if (atlas != null)
		{
			numberOfUnused = 0;

			atlas.ClearAllSpriteData();

			if (AtlasTexture != null)
			{
				Object.Destroy(AtlasTexture);
				AtlasTexture = null;
			}
		}
	}

	#endregion

	#region Protected Methods

	/// <summary>
	/// Adds a list of textures to the atlas.
	/// Requires:
	/// textures list is not null or empty.
	/// </summary>
	protected abstract bool AddTextures(List<Texture> textures);

	/// <summary>
	/// Helper function that draws the old atlas texture to the new texture, and destroys the old AtlasTexture.
	/// Also updates sprite list of the atlas.
	/// If Atlas.spriteList is null or empty doesn't have effect.
	/// Requires:
	/// AtlasTexture is not null.
	/// Render target is set to newTexture.
	/// </summary>
	/// <param name="newTexture">Texture in which to write old atlas.</param>
	/// <param name="texturePositions">Positions of old textures inside of the new texture, should correspond to order in Atlas.spriteList.</param>
	protected void DrawOldAtlas(RenderTexture newTexture, List<Rect> texturePositions)
	{
		DrawOldAtlas(newTexture, (i, texture) => texturePositions[i]);
	}

	/// <summary>
	/// Helper function that draws the old atlas texture to the new texture, and destroys the old AtlasTexture.
	/// Also updates sprite list of the atlas.
	/// If Atlas.spriteList is null or empty doesn't have effect.
	/// Requires:
	/// AtlasTexture is not null.
	/// Render target is set to newTexture.
	/// </summary>
	/// <param name="newTexture">Texture in which to write old atlas.</param>
	/// <param name="texturePositionFunc"> Function that returns position of old texture inside of the new texture, depending on order in Atlas.spriteList.</param>
	protected void DrawOldAtlas(RenderTexture newTexture, Func<int, Texture, Rect> texturePositionFunc)
	{
		if (Atlas.SpriteList == null || Atlas.SpriteList.Count == 0)
		{
			AtlasTexture = newTexture;
			return;
		}

		Vector2 newTextureSize = new Vector2(newTexture.width, newTexture.height);

		// We will use one mesh to draw everything
		Mesh mesh = new Mesh();

		// If we had made the atlas before, we will need to keep the old sprites intact
		// We will render the old part of the atlas as a single draw call, so we need some
		// variables to do this.
		// Initialize the data we need to fill
		int numberOfVertices = Atlas.SpriteList.Count * 4;
		Vector3[] positions = new Vector3[numberOfVertices];
		Vector2[] uvs = new Vector2[numberOfVertices];
		int[] triangles = new int[Atlas.SpriteList.Count * 6];

		// Update all old sprites positions, uvs and triangles
		for (int index = 0; index < Atlas.SpriteList.Count; index++)
		{
			// Set the UV coordinates of sprite in old atlas texture
			SetUvs(index, uvs);
			// Set the positions of vertices and sprite data
			// This will change positions in spriteData, SetUvs must be called before
			SetPosition(texturePositionFunc(index, newTexture), newTextureSize, Atlas.SpriteList[index], positions, index);
			// Set the triangles
			SetTriangles(index, triangles);
		}

		// Add the data to the mesh
		mesh.vertices = positions;
		mesh.uv = uvs;
		mesh.triangles = triangles;

		// Set the texture to the atlasing material
		AtlasingMaterial.mainTexture = AtlasTexture;
		// Activate the material
		AtlasingMaterial.SetPass(0);

		// Draw the mesh to the new atlas
		Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

		Object.Destroy(mesh);

		Object.Destroy(AtlasTexture);

		AtlasTexture = newTexture;

		MarkTextureForReusage();
	}

	/// <summary>
	/// Draws all new textures on atlas texture and creates/updates list of sprite data.
	/// Requires:
	/// textures is not null or empty; texturePositions contain position for every texture starting from indexOfFirstTexturePosition.
	/// Render target is set to desired texture.
	/// </summary>
	/// <param name="textures">Textures to be added to atlas texture.</param>
	/// <param name="texturePositions">Positions of new textures in AtlasTexture. Positions of new textures to be added should start from index specified by third argument.</param>
	/// <param name="indexOfFirstTexturePosition">Index of position of the first new texture in texturePositions list.</param>
	/// <param name="fillUnusedPlaces">Whether unused sprites should be swapped with new textures.</param>
	protected void DrawNewTextures(List<Texture> textures, List<Rect> texturePositions, int indexOfFirstTexturePosition, bool fillUnusedPlaces)
	{
		DrawNewTextures(textures, (i, texture) => texturePositions[i], indexOfFirstTexturePosition, fillUnusedPlaces);
	}

	/// <summary>
	/// Draws all new textures on atlas texture and creates/updates list of sprite data.
	/// Requires:
	/// textures is not null or empty; texturePositions contain position for every texture starting from indexOfFirstTexturePosition.
	/// Render target is set to desired texture.
	/// </summary>
	/// <param name="textures">Textures to be added to atlas texture.</param>
	/// <param name="texturePositionsFunc">This func should return positions of new textures in AtlasTexture and it will be called with index of element and atlas texture. Positions of new textures to be added should start from index specified by third argument.</param>
	/// <param name="indexOfFirstTexturePosition">Index of position of first texture in texturePositions list.</param>
	/// <param name="fillUnusedPlaces">Whether unused sprites should be swapped with new textures.</param>
	protected void DrawNewTextures(List<Texture> textures, Func<int, Texture, Rect> texturePositionsFunc, int indexOfFirstTexturePosition,
									bool fillUnusedPlaces)
	{
		// Mesh we will write to.
		Mesh mesh = new Mesh();
		mesh.MarkDynamic();

		// Initialize the data we need to fill
		Vector3[] positions = new Vector3[4];
		Vector2[] uvs = new Vector2[4];
		int[] triangles = new int[6];

		int j = 0;
		for (int i = 0; i < textures.Count; i++)
		{
			DynamicSpriteData spriteData = null;

			if (fillUnusedPlaces)
			{
				// first fill empty places, if there is no more empty places fill unused places
				Rect spritePosition = texturePositionsFunc(i + indexOfFirstTexturePosition, AtlasTexture);
				if (spritePosition.xMax > AtlasTexture.width || spritePosition.yMax > AtlasTexture.height)
				{
					spriteData = FindFirstUnusedSprite(ref j);
				}
			}

			if (spriteData != null)
			{
				// unused sprite found
				numberOfUnused--;
				Atlas.RenameSprite(spriteData.Name, textures[i].name);
				SetPosition(texturePositionsFunc(j - 1, AtlasTexture), TextureSize, spriteData, positions, 0);
				indexOfFirstTexturePosition--;
			}
			else
			{
				spriteData = new DynamicSpriteData(textures[i].name);
				Atlas.AddSprite(spriteData);

				// Set the positions, and sprite data.
				SetPosition(texturePositionsFunc(i + indexOfFirstTexturePosition, AtlasTexture), TextureSize, spriteData, positions, 0);
			}

			spriteData.Reset();
			spriteData.Acquire();

			// Set the UVs
			SetupUvs(uvs, textures[i]);

			// Set the triangles
			SetTriangles(0, triangles);

			mesh.Clear();
			// Add the data to the mesh
			mesh.vertices = positions;
			mesh.uv = uvs;
			mesh.triangles = triangles;

			// Set the texture to render to the new atlas
			AtlasingMaterial.mainTexture = textures[i];
			// Activate the material
			AtlasingMaterial.SetPass(0);

			// Draw the texture to the atlas
			Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
			MarkTextureForReusage();
		}

		// Cleanup the mesh, otherwise it will leak, unity won't do this on his own
		Object.Destroy(mesh);
	}

	protected RenderTexture CreateRenderTexture(Vector2 textureSize)
	{
		return new RenderTexture((int)textureSize.x, (int)textureSize.y, 0, RenderTextureFormat.ARGB32) {name = atlasName};
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// A helper method that sets the UVs in the UV array based on a sprite data.
	/// </summary>
	/// <param name="index">Index of sprite data in atlas sprite list.</param>
	/// <param name="uvs">Array of uvs that will be filled.</param>
	private void SetUvs(int index, Vector2[] uvs)
	{
		DynamicSpriteData sprite = Atlas.SpriteList[index] as DynamicSpriteData;
		if (sprite == null)
		{
			Debug.LogError("Sprite data in atlas is null or of wrong type.");
			return;
		}

		// Get the x, y, width and height in UV space
		float x = (sprite.X - Padding) / TextureSize.x;
		float y = (sprite.Y - Padding) / TextureSize.y;
		float w = (sprite.Width + 2f * Padding) / TextureSize.x;
		float h = (sprite.Height + 2f * Padding) / TextureSize.y;

		// Apply
		uvs[index * 4] = new Vector2(x, 1 - y);
		uvs[index * 4 + 1] = new Vector2(x + w, 1 - y);
		uvs[index * 4 + 2] = new Vector2(x, 1 - (y + h));
		uvs[index * 4 + 3] = new Vector2(x + w, 1 - (y + h));
	}

	/// <summary>
	/// A helper method that sets the triangle indices correctly.
	/// </summary>
	/// <param name="i">The index of the quad that has to be written.</param>
	/// <param name="triangles">The array where to write the indices.</param>
	private static void SetTriangles(int i, int[] triangles)
	{
		// The first triangle
		triangles[i * 6] = i * 4;
		triangles[i * 6 + 1] = i * 4 + 1;
		triangles[i * 6 + 2] = i * 4 + 2;

		// The second triangle
		triangles[i * 6 + 3] = i * 4 + 1;
		triangles[i * 6 + 4] = i * 4 + 3;
		triangles[i * 6 + 5] = i * 4 + 2;
	}

	/// <summary>
	/// A helper function that sets the position in the positions array. It also updates sprite position and size.
	/// </summary>
	/// <param name="texturePosition">Sprite screen size.</param>
	/// <param name="atlasTextureSize">Atlas texture size.</param>
	/// <param name="spriteData">SpriteData that is used for current texture.</param>
	/// <param name="positions">Array of positions that will be filled.</param>
	/// <param name="index">Index in a positions array for current texture.</param>
	private static void SetPosition(Rect texturePosition, Vector2 atlasTextureSize, SpriteData spriteData, Vector3[] positions, int index)
	{
		Vector2 spriteScreenSize = new Vector2(texturePosition.width, texturePosition.height) * 2f;
		spriteScreenSize.InverseScale(atlasTextureSize);

		Vector2 screenPosition = new Vector2(texturePosition.x, texturePosition.y);
		screenPosition.InverseScale(atlasTextureSize);
		screenPosition = screenPosition * 2f - new Vector2(1, 1);

		// Update sprite data with new position
		spriteData.SetRect(texturePosition, Padding);

		// Set the positions in an array
		positions[index * 4] = screenPosition;
		positions[index * 4 + 1] = screenPosition + new Vector2(spriteScreenSize.x, 0);
		positions[index * 4 + 2] = screenPosition + new Vector2(0, spriteScreenSize.y);
		positions[index * 4 + 3] = screenPosition + new Vector2(spriteScreenSize.x, spriteScreenSize.y);
	}

	/// <summary>
	/// Returns sprite data of first unused sprite starting from startingIndex.
	/// If such is not found returns null.
	/// </summary>
	private DynamicSpriteData FindFirstUnusedSprite(ref int index)
	{
		while (index < Atlas.SpriteList.Count)
		{
			var spriteData = Atlas.SpriteList[index] as DynamicSpriteData;
			index++;
			if (spriteData != null && (!spriteData.IsUsed()))
			{
				return spriteData;
			}
		}
		return null;
	}

	// TODO: This method needs to be triggered on every update (or occasionally) to regenerate texture if it's removed from memory.
	/// <summary>
	///  Checks if render texture is lost, and regenerates atlas's texture if that is case.
	/// </summary>
	public void UpdateHandler()
	{
		RenderTexture atlasTexture = AtlasTexture as RenderTexture;
		if (atlasTexture != null && !atlasTexture.IsCreated())
		{
			if (atlas != null)
			{
				RenderTexture.active = atlasTexture;
				GL.Clear(true, true, new Color32(0, 0, 0, 0));
				RenderTexture.active = null;
				for (int i = 0; i < atlas.SpriteList.Count; i++)
				{
					var spriteData = atlas.SpriteList[i] as DynamicSpriteData;
					if (spriteData == null)
					{
						Debug.LogError("Sprite data in atlas is null or of wrong type.");
						return;
					}

					spriteData.Reset();
					spriteData.SpriteLost = true;
				}
				numberOfUnused = atlas.SpriteList.Count;
				// TODO: Inform Sprites that are using this atlas that texture has been changed
			}

		}
	}

	/// <summary>
	/// This will prevent Unity from throwing warning every time texture is written it before it's content was cleared.
	/// Because texture is reused multiple time this needs to be called after every write.
	/// </summary>
	private void MarkTextureForReusage()
	{
		((RenderTexture)AtlasTexture).MarkRestoreExpected();
	}

	#endregion
}
