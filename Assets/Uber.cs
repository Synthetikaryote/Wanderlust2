using UnityEngine;
using System.Collections;

// splitting vertices: http://answers.unity3d.com/questions/798510/flat-shading.html
// ddx and ddy fragment shader: http://answers.unity3d.com/questions/847167/rendering-hard-edges-via-vertex-shader.html#answer-849582

public class Uber : MonoBehaviour {
	public Vector3 playerPos = Vector3.zero;

    static Uber instance;
    public static Uber Instance
    {
		get
		{
			if (instance == null)
			{
				var go = Instantiate(new GameObject("Uber"));
				DontDestroyOnLoad(go);
				instance = go.AddComponent<Uber>();
			}
			return instance;
		}
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
	
	}

	static uint prime = 4294967291;
	static uint ord = 4294967290;
	static uint generator = 4294967279;
	public static float getFloat(uint x, uint y, uint seed) {
		//will return values 1=> x >0; replace 'ord' with 'prime' to get 1> x >0
		//one call to modPow would be enough if all data fits into an ulong
		uint sy = modPow(generator, (((ulong)seed) << 32) + (ulong)y, prime);
		//uint xs = modPow(generator, (((ulong)x) << 32) + (ulong)seed, prime);
		uint xy = modPow(generator, (((ulong)sy) << 32) + (ulong)x, prime);
		return ((float)xy) / ord;
	}
	public static int RandomRange(int x, int y, int seed, int low, int high) {
		return low + (int)(getFloat((uint)x, (uint)y, (uint)seed) * (high - low));
	}
	static uint modPow(uint bb, ulong e, uint m) {
		ulong b = bb;
		ulong ret = 1;
		while (e > 0) {
			if (e % 2 == 1) {
				ret = (ret * b) % m;
			}
			e = e >> 1;
			b = (b * b) % m;
		}
		return (uint)ret;
	}
}