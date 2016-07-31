using UnityEngine;
using System;
using System.Collections;

using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour {
	int size = 4;
	float heightFactor = 0.5f;
	int[][,] sectorData;

	// sectorData
	// 0 1 2
	// 3 4 5
	// 6 7 8
	void Start () {
		sectorData = new int[9][,];
		for (int i = 0; i < sectorData.Length; ++i) {
			sectorData[i] = new int[size + 1, size + 1];
			int x = (i % 3) * size;
			int y = (i / 3) * size;
			GenerateRect(ref sectorData[i], x, y, x, y, x + size, y + size);
		}
		Debug.Log("Done.");
	}

	// Update is called once per frame
	void Update () {
	
	}

	void GeneratePoint(ref int[,] data, int dataX, int dataY, int x, int y) {
		SeedWithPos(x, y);
		data[y - dataY, x - dataX] = Mathf.RoundToInt(Random.Range(-size * heightFactor * 0.5f, size * heightFactor * 0.5f));
	}
	
	void GenerateLine(ref int[,] data, int dataX, int dataY, int x1, int y1, int x2, int y2) {
		int dx = x2 > x1 ? 1 : x2 < x1 ? -1 : 0;
		int dy = y2 > y1 ? 1 : y2 < y1 ? -1 : 0;
		for (int x = x1, y = y1; dx != 0 && x <= x2 || dy != 0 && y <= y2; x += dx, y += dy)
			GeneratePoint(ref data, dataX, dataY, x, y);
		// to do: smooth the middle
	}

	void GenerateRect(ref int[,] data, int dataX, int dataY, int x1, int y1, int x2, int y2) {
		// make the edges
		GenerateLine(ref data, dataX, dataY, x1, y1, x2, y1);
		GenerateLine(ref data, dataX, dataY, x1, y2, x2, y2);
		GenerateLine(ref data, dataX, dataY, x1, y1, x1, y2);
		GenerateLine(ref data, dataX, dataY, x2, y1, x2, y2);
		// fill in the middle
		for (int y = y1 + 1; y < y2; ++y)
			for (int x = x1 + 1; x < x2; ++x)
				GeneratePoint(ref data, dataX, dataY, x, y);
		// to do: smooth the middle
	}

	void SeedWithPos(int x, int y) {
		Random.seed = (x * 0x1f1f1f1f) ^ y;
	}
}
