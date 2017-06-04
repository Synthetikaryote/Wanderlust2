using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class Block {
	public static int size = 128;
	public GameObject go = null;
	public bool isBuilding = false;
	public bool isActive = false;

	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	Mesh mesh;
	Vector3[] vertices;
	Vector2[] uvs;
	int[] triangles;
	int posX, posY;

	public Block() {
		vertices = new Vector3[(size + 1) * (size + 1)];
		uvs = new Vector2[vertices.Count()];
		triangles = new int[size * size * 6];
	}

	public void GenerateMeshData(ref int[,] data, int dataX, int dataY, int posX, int posY) {
		isBuilding = true;
		this.posX = posX;
		this.posY = posY;
		var triIndex = 0;
		for (int y = 0; y <= size; ++y) {
			for (int x = 0; x <= size; ++x) {
				int index = y * (size + 1) + x;
				vertices[index] = new Vector3(x, data[dataY + y, dataX + x] * WorldGenerator.heightFactor, y);
				uvs[index] = new Vector2((float)x / size, (float)y / size);
				if (x < size && y < size) {
					triangles[triIndex++] = index;
					triangles[triIndex++] = index + size + 1;
					triangles[triIndex++] = index + size + 2;
					triangles[triIndex++] = index;
					triangles[triIndex++] = index + size + 2;
					triangles[triIndex++] = index + 1;
				}
			}
		}
		isBuilding = false;
	}

	// unity-related - can only be run on the main thread
	public void GenerateMesh(Material grass) {
		go = new GameObject("sector_" + posX / size + "_" + posY / size);
		meshFilter = go.AddComponent<MeshFilter>();
		meshRenderer = go.AddComponent<MeshRenderer>();
		mesh = meshFilter.mesh;
		mesh.SetVertices(new List<Vector3>(vertices));
		mesh.SetUVs(0, new List<Vector2>(uvs));
		mesh.triangles = triangles;
		meshRenderer.sharedMaterial = grass;
		go.transform.position = new Vector3(posX, 0, posY);
	}
}

public class Sector {
	public static int size = 1024;
	public int id, dataX, dataY, sectorX, sectorY;
	public bool isDone = false;
	public Block[,] blocks;
	
	public int[,] data;

	public Sector(int id, int dataX, int dataY) {
		this.id = id;
		this.dataX = dataX;
		this.dataY = dataY;
		sectorX = dataX / size;
		sectorY = dataY / size;
		data = new int[size + 1, size + 1];
		int blocksXY = size / Block.size;
		blocks = new Block[blocksXY, blocksXY];
		for (int y = 0; y < blocksXY; ++y)
			for (int x = 0; x < blocksXY; ++x)
				blocks[x, y] = new Block();
	}

	public void Generate() {
		GenerateRect(dataX, dataY, dataX, dataY, dataX + size, dataY + size);
		isDone = true;
	}

	void GenerateRect(int dataX, int dataY, int x1, int y1, int x2, int y2) {
		// make the edges
		GenerateLine(dataX, dataY, x1, y1, x2, y1);
		GenerateLine(dataX, dataY, x1, y2, x2, y2);
		GenerateLine(dataX, dataY, x1, y1, x1, y2);
		GenerateLine(dataX, dataY, x2, y1, x2, y2);
		// fill in the middle
		for (int y = y1 + 1; y < y2; ++y)
			for (int x = x1 + 1; x < x2; ++x)
				GeneratePoint(dataX, dataY, x, y);
		// to do: smooth the middle
	}

	void GenerateLine(int dataX, int dataY, int x1, int y1, int x2, int y2) {
		int dx = x2 > x1 ? 1 : x2 < x1 ? -1 : 0;
		int dy = y2 > y1 ? 1 : y2 < y1 ? -1 : 0;
		for (int x = x1, y = y1; dx != 0 && x <= x2 || dy != 0 && y <= y2; x += dx, y += dy)
			GeneratePoint(dataX, dataY, x, y);
		// to do: smooth the middle
	}

	void GeneratePoint(int dataX, int dataY, int x, int y) {
		data[y - dataY, x - dataX] = Uber.RandomRange(x, y, 0, WorldGenerator.minHeight, WorldGenerator.maxHeight);
	}

	public override string ToString() {
		string result = "";
		for (int y = 0; y <= size; ++y) {
			for (int x = 0; x <= size; ++x) {
				result += data[y, x].ToString().PadLeft(4);
			}
			result += "\n";
		}
		return result;
	}

	public Block GetBlock(int blockX, int blockY) {
		var withinSectorBlockX = blockX - sectorX * Sector.size / Block.size;
		var withinSectorBlockY = blockY - sectorY * Sector.size / Block.size;
		return blocks[withinSectorBlockX, withinSectorBlockY];
	}
}

public class WorldGenerator : MonoBehaviour {
	public static int minHeight = -1;
	public static int maxHeight = 1;
	public static float heightFactor = .3f;
	public static float viewDist = 1024f;
	List<Sector> sectors;
	public List<Block> blocks;
	public Material material;

	// sectorData
	// 0 1 2
	// 3 4 5
	// 6 7 8
	IEnumerator Start () {
		//material.mainTextureScale = new Vector2(Sector.size, Sector.size);
		yield return GenerateSectors();
		Debug.Log("Done sectors in " + Time.realtimeSinceStartup + " seconds.");
		yield return GenerateBlocks();
		Debug.Log("Done blocks in " + Time.realtimeSinceStartup + " seconds.");
	}

	IEnumerator GenerateSectors()
	{
		sectors = new List<Sector>(9);
		for (int i = 0; i < sectors.Capacity; ++i)
		{
			int x = ((i % 3) - 1) * Sector.size;
			int y = ((i / 3) - 1) * Sector.size;
			StartCoroutine(ThreadedJob<Sector>.Do(() =>
			{
				var sector = new Sector(i + 1, x, y);
				sector.Generate();
				return sector;
			}, sector =>
			{
				sectors.Add(sector);
				Debug.Log("Done " + sectors.Count + " in " + Time.realtimeSinceStartup + " seconds.");
			}));
			yield return null;
		}
		while (sectors.Count < 9 || sectors.Any(s => !s.isDone))
			yield return null;
	}

	IEnumerator GenerateBlocks() {
		float viewDistBlocks = viewDist / Block.size;
		int numBlocks = Mathf.CeilToInt(Mathf.PI * viewDistBlocks * viewDistBlocks);
		blocks = new List<Block>(numBlocks);
		int blockDist = Mathf.CeilToInt(viewDist / Block.size);
		int px = Mathf.FloorToInt(Uber.Instance.playerPos.x / Block.size);
		int py = Mathf.FloorToInt(Uber.Instance.playerPos.y / Block.size);
		for (int i = 0; i < numBlocks; ++i) {
			// to do: this search could be better.  there must be a better way to find the closest
			int closestX = 0, closestY = 0;
			Sector closestSector = null;
			float closestDistSq = float.MaxValue;
			// iterate over the square around the player to find the next nearest block
			for (int y = py - blockDist; y < py + blockDist; ++y) {
				for (int x = px - blockDist; x < px + blockDist; ++x) {
					var sector = GetSector(x, y);
					if (sector == null)
						continue;
					var block = sector.GetBlock(x, y);
					if (!block.isActive) {
						int dx = px - x;
						int dy = py - y;
						float distSq = dx * dx + dy * dy;
						if (distSq < closestDistSq)
						{
							closestSector = sector;
							closestDistSq = distSq;
							closestX = x;
							closestY = y;
						}
					}
				}
			}
			// making a copy of these variables for lambda storage purposes
			Sector blockSector = closestSector;
			int x2 = closestX * Block.size;
			int y2 = closestY * Block.size;
			var buildBlock = blockSector.GetBlock(closestX, closestY);
			buildBlock.isActive = true;
			buildBlock.isBuilding = true;
			blocks.Add(buildBlock);
			StartCoroutine(ThreadedJob<Block>.Do(() => {
				// create the block at the found block x and y in world coords
				buildBlock.GenerateMeshData(ref blockSector.data, x2 - blockSector.dataX, y2 - blockSector.dataY, x2, y2);
				return buildBlock;
			}, block => {
				block.GenerateMesh(material);
				Debug.Log("Done " + blocks.Count + " in " + Time.realtimeSinceStartup + " seconds.");
			}));
			yield return null;
		}
		while (blocks.Count < numBlocks)
			yield return null;
	}

	Sector GetSector(int blockX, int blockY) {
		int sectorX = Mathf.FloorToInt((float)blockX / Sector.size);
		int sectorY = Mathf.FloorToInt((float)blockY / Sector.size);
		foreach (var sector in sectors)
			if (sector.sectorX == sectorX && sector.sectorY == sectorY)
				return sector;
		return null;
	}
	Block GetBlock(int blockX, int blockY) {
		var sector = GetSector(blockX, blockY);
		if (sector != null)
			return sector.GetBlock(blockX, blockY);
		return null;
	}
}
