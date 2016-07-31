using UnityEngine;
using System.Collections;

public class Uber : MonoBehaviour {

    static Uber instance;
    public static Uber Instance()
    {
        if (instance == null)
        {
            var go = Instantiate(new GameObject("Uber"));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Uber>();
        }
        return instance;
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
