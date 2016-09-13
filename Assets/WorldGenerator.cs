using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class Sector {
	public static int size = 8;
	public int id, x, y, dataX, dataY;
	public bool isDone = false;
	public GameObject go = null;

	int[,] data;
	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	Mesh mesh;
	Vector3[] vertices;
	Vector2[] uvs;
	int[] triangles;

	public Sector(int id, int dataX, int dataY, int x, int y) {
		this.id = id;
		this.dataX = dataX;
		this.dataY = dataY;
		this.x = x;
		this.y = y;
		data = new int[size + 1, size + 1];
		vertices = new Vector3[(size + 1) * (size + 1)];
		uvs = new Vector2[vertices.Count()];
		triangles = new int[size * size * 6];
	}

	public void Generate() {
		GenerateRect(dataX, dataY, x, y, x + size, y + size);
		GenerateMeshData();
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

	void GenerateMeshData() {
		var triIndex = 0;
		for (int y = 0; y <= size; ++y) {
			for (int x = 0; x <= size; ++x) {
				int index = y * (size + 1) + x;
				vertices[index] = new Vector3(x, data[y, x] * WorldGenerator.heightFactor, y);
				uvs[index] = new Vector2(x, y);
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
	}

	// unity-related - can only be run on the main thread
	public void GenerateMesh(Material grass) {
		go = new GameObject("sector_" + dataX / size + "_" + dataY / size);
		meshFilter = go.AddComponent<MeshFilter>();
		meshRenderer = go.AddComponent<MeshRenderer>();
		mesh = meshFilter.mesh;
		mesh.SetVertices(new List<Vector3>(vertices));
		mesh.SetUVs(0, new List<Vector2>(uvs));
		mesh.triangles = triangles;
		meshRenderer.sharedMaterial = grass;
		go.transform.position = new Vector3(dataX, 0, dataY);
	}
}

public class WorldGenerator : MonoBehaviour {
	public static int minHeight = -1;
	public static int maxHeight = 1;
	public static float heightFactor = .3f;
	List<Sector> sectors;
	public Material grass;

	// sectorData
	// 0 1 2
	// 3 4 5
	// 6 7 8
	IEnumerator Start () {
		//sectors = new List<Sector>(9);
		//for (int i = 0; i < sectors.Capacity; ++i) {
		//	int x = (i % 3) * Sector.size;
		//	int y = (i / 3) * Sector.size;
		//	var sector = new Sector(i + 1, x, y, x, y);
		//	StartCoroutine(sector.Generate());
		//	sectors.Add(sector);
		//}
		//yield return null;
		Debug.Log("Number Of Logical Processors: " + Environment.ProcessorCount);
		yield return GenerateSectors();
		Debug.Log("Done in " + Time.realtimeSinceStartup + " seconds.");
		Debug.Log(sectors[0].dataX + ", " + sectors[0].dataY + "\n" + sectors[0]);
		Debug.Log(sectors[1].dataX + ", " + sectors[1].dataY + "\n" + sectors[1]);
	}

	IEnumerator GenerateSectors()
	{
		sectors = new List<Sector>(9);
		for (int i = 0; i < sectors.Capacity; ++i)
		{
			int x = (i % 3) * Sector.size;
			int y = (i / 3) * Sector.size;
			StartCoroutine(ThreadedJob<Sector>.Do(() => {
				var sector = new Sector(i + 1, x, y, x, y);
				sector.Generate();
				return sector;
			}, sector => {
				sectors.Add(sector);
				sector.GenerateMesh(grass);
				Debug.Log("Done " + sectors.Count + " in " + Time.realtimeSinceStartup + " seconds.");
			}));
			yield return null;
		}
		while (sectors.Count < 9 || sectors.Any(s => !s.isDone))
			yield return null;
	}

	// Update is called once per frame
	void Update () {
	}
}
