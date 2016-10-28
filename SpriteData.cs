using UnityEngine;

public class SpriteData
{
	public string Name { get; private set; }

	public int X { get; private set; }
	public int Y { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }

	public SpriteData(string name)
	{
		Name = name;
	}

	public void SetRect(Rect rect, float padding)
	{
		SetRect(Mathf.FloorToInt(rect.xMin + padding), Mathf.FloorToInt(rect.yMin + padding), Mathf.FloorToInt(rect.width - 2f * padding),
				Mathf.FloorToInt(rect.height - 2f * padding));
	}

	public void SetRect(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public void SetName(string newName)
	{
		Name = newName;
	}
}
