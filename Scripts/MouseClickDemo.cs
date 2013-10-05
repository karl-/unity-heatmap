using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MouseClickDemo : MonoBehaviour {

	// public Texture2D tex;
	List<GameObject> points = new List<GameObject>();			//!< A list of all points to be fed to the heatmap generator.


	// Heatmap Settings
	public int pointRadius = 40;								//!< How large of a radius should a point on the heatmap be?

#if UNITY_EDITOR && !UNITY_WEBPLAYER

	public TextAsset presetCoordinatesFile;
	public bool usePresetCoordinates = false;					//!< If true, this attempts to parse the presetCoordinatesFile and draw a heatmap
	[HideInInspector]
	public bool generatePresetCoordinates = false;				//!< If set to true, clicks will be logged to a text file.
																///	Clicks will be logged to a text file that can then be loaded w/ the toggleable usePresetCoordinates (nice for debugging).  Commented out as this may be confusing for someone first playing around with the editor.
#endif
	
	public bool createPrimitive = false;						//!< If true, a primitive will be drawn the raycast collision point

	public new Camera camera;									//!< Which camera to render the heatmap from -- best to use orthographic.
	private GameObject p; 										//!< Parent GameObject if primitives are generated.

	Rect resetRect = new Rect(5, 5, 100, 30);	
	Rect screenshotRect = new Rect(5, 40, 100, 30);

	public void OnGUI()
	{
		if(GUI.Button(resetRect, "Reset")) {
			Heatmap.DestroyHeatmapObjects();
			ClearPoints();
#if !UNITY_WEBPLAYER
			usePresetCoordinates = false;
#endif
		}

#if !UNITY_WEBPLAYER
		if(GUI.Button(screenshotRect, "Screenshot"))
		{
			StartCoroutine(TakeScreenshot());
		}
#endif
	}

	public void Start()
	{
		p = new GameObject();

#if UNITY_EDITOR && !UNITY_WEBPLAYER
		if(!usePresetCoordinates || presetCoordinatesFile == null)
			return;

		Vector3[] positionArray = StringUtility.Vector3ArrayWithFile(presetCoordinatesFile);

		for(int i = 0; i < positionArray.Length; i++)
		{
			GameObject go = (createPrimitive) ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject();
			go.transform.position = positionArray[i];
			go.name = "Point: " + i;
			go.transform.parent = p.transform;
			points.Add(go);
		}

		Texture2D heatmapImage = Heatmap.CreateHeatmap(positionArray, null, pointRadius);
		Heatmap.CreateRenderPlane(heatmapImage);
#endif
	}

	public Vector3[] PositionArrayWithGameObjects(GameObject[] g)
	{
		Vector3[] v = new Vector3[g.Length];
		for(int i = 0; i < g.Length; i++)
			v[i] = g[i].transform.position;
		return v;
	}

	public void Update() {
#if UNITY_EDITOR && !UNITY_WEBPLAYER
		if(usePresetCoordinates)
			return;
#endif		
		if(Input.GetMouseButtonDown(0))
		{
			if( GUIToScreenRect(resetRect).Contains(Input.mousePosition) || GUIToScreenRect(screenshotRect).Contains(Input.mousePosition) )
				return;

			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit, Mathf.Infinity)) {
				AddPoint(hit.point);
			}
		}
	}

	void AddPoint(Vector3 pos)
	{
		GameObject go = (createPrimitive) ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject();
		go.transform.position = pos;
		go.transform.parent = p.transform;

		points.Add(go);
#if UNITY_EDITOR && !UNITY_WEBPLAYER
		if(generatePresetCoordinates)
		{	
			string str = StringUtility.StringWithGameObjects(points.ToArray());

			string path = presetCoordinatesFile ? AssetDatabase.GetAssetPath(presetCoordinatesFile) : AssetDatabase.GenerateUniqueAssetPath("Assets/DebugPoints.txt");

			File.WriteAllText(path, str);

			// Heatmap.CreateHeatmap(PositionArrayWithGameObjects(points.ToArray()), camera, pointRadius);
		}
		else
#endif		
		{
			Texture2D heatmapImage = Heatmap.CreateHeatmap(PositionArrayWithGameObjects(points.ToArray()), camera, pointRadius);
			Heatmap.CreateRenderPlane(heatmapImage);
		}
	}

	void ClearPoints()
	{
		for(int i = 0; i < points.Count; i++)
			DestroyImmediate(points[i]);
		points.Clear();
	}

	public Rect GUIToScreenRect(Rect guiRect)
	{
		return new Rect(guiRect.x, Screen.height - (guiRect.y + guiRect.height), guiRect.width, guiRect.height);
	}

	public IEnumerator TakeScreenshot()
	{
		int i = 0;
		while(File.Exists("Assets/Screenshot" + i + ".png")) {
			i++;
			yield return 0;
		}
		string path = "Assets/Screenshot" + i + ".png";
		Heatmap.Screenshot(path, camera);
	}

#if UNITY_EDITOR

	public void OnDisable()
	{
		AssetDatabase.Refresh();
	}

#endif	
}
