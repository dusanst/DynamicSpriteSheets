using System;
using UnityEngine;
using System.Collections.Generic;

public class Atlas
{
	private List<SpriteData> spriteList = new List<SpriteData>();
	private Dictionary<string, SpriteData> spriteDictionary;

	public Texture Texture
	{
		get { return SpriteMaterial != null ? SpriteMaterial.mainTexture : null; }
	}

	public Material SpriteMaterial { get; set; }

	public List<SpriteData> SpriteList
	{
		get { return spriteList; }
		set { spriteList = value; }
	}

	public SpriteData GetSprite(string name)
	{
		if (string.IsNullOrEmpty(name)) { return null; }

		if (spriteDictionary == null)
		{
			spriteDictionary = new Dictionary<string, SpriteData>();
			foreach (var spriteData in spriteList)
			{
				spriteDictionary.Add(spriteData.Name, spriteData);
			}
		}

		SpriteData foundSprite;
		spriteDictionary.TryGetValue(name, out foundSprite);
		return foundSprite;
	}

	public void ClearAllSpriteData()
	{
		if (spriteDictionary != null)
		{
			spriteDictionary.Clear();
			spriteDictionary = null;
		}
		if (SpriteList != null)
		{
			SpriteList.Clear();
		}
	}

	public void RenameSprite(string oldName, string newName)
	{
		SpriteData spriteData;
		if (spriteDictionary.TryGetValue(oldName, out spriteData))
		{
			spriteDictionary.Remove(oldName);
			spriteData.SetName(newName);
			spriteDictionary.Add(newName, spriteData);
		}
	}

	public void AddSprite(DynamicSpriteData spriteData)
	{
		if (spriteData == null || spriteDictionary.ContainsKey(spriteData.Name))
		{
			return;
		}
		spriteDictionary.Add(spriteData.Name, spriteData);
		SpriteList.Add(spriteData);
	}

	public void RemoveAllSpriteData(Predicate<SpriteData> match)
	{
		if (match == null)
		{
			return;
		}

		for (int i = 0; i < SpriteList.Count; i++)
		{
			if (match(SpriteList[i]))
			{
				spriteDictionary.Remove(SpriteList[i].Name);
			}
		}

		SpriteList.RemoveAll(match);
	}
}
