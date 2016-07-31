using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class Sector {
	public static int size = 1024;
	public int id, x, y, dataX, dataY;
	public bool isDone = false;
	System.Random rand = new System.Random();

	int[,] data;

	public Sector(int id, int dataX, int dataY, int x, int y) {
		data = new int[size + 1, size + 1];
		this.id = id;
		this.dataX = dataX;
		this.dataY = dataY;
		this.x = x;
		this.y = y;
	}

	public void Generate() {
		GenerateRect(dataX, dataY, x, y, x + size, y + size);
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
		data[y - dataY, x - dataX] = Uber.RandomRange(x, y, 0, (int)(-size * WorldGenerator.heightFactor * 0.5f + 0.5f), (int)(size * WorldGenerator.heightFactor * 0.5f + 0.5f));
	}
}

public class WorldGenerator : MonoBehaviour {
	public static float heightFactor = 0.5f;
	List<Sector> sectors;

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

		sectors = new List<Sector>(9);
		for (int i = 0; i < sectors.Capacity; ++i) {
			int x = (i % 3) * Sector.size;
			int y = (i / 3) * Sector.size;
			StartCoroutine(ThreadedJob<Sector>.Do(() => {
				var sector = new Sector(i + 1, x, y, x, y);
				sector.Generate();
				return sector;
			}, sector => {
				sectors.Add(sector);
				Debug.Log("Done " + sectors.Count + " in " + Time.realtimeSinceStartup + " seconds.");
			}));
			yield return null;
		}
	}

	// Update is called once per frame
	void Update () {
		//if (sectors.TrueForAll(s => s.isDone))
		//	Debug.Log("Done in " + Time.realtimeSinceStartup + " seconds.");
	}
}
