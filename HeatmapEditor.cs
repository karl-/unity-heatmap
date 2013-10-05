using UnityEngine;
using UnityEditor;
using System.Collections;

public class HeatmapEditor : EditorWindow {

	public enum CameraOrientation {
		DOWN,
		FORWARD,
		MANUAL
	}
	CameraOrientation cameraOrientation = CameraOrientation.DOWN;
	CameraOrientation previousOrientation;

	public static Camera cam;

	Bounds worldBounds;

	// heatmap settings
	int pointRadius = 30;
	Texture2D heatmapOverlay;

	[MenuItem("Window/Heatmap/Heatmap Editor")]
	public static void InitHeatmapWindow()
	{
		ScriptableObject.CreateInstance<HeatmapEditor>().ShowUtility();
	}

	public void OnEnable()
	{
		CreateCamera();
		previousOrientation = cameraOrientation;
		worldBounds = GetWorldBounds();
		AutosizeCamera(cam, cameraOrientation);
	}

	public void OnDisable()
	{
		if(cam) {
			DestroyImmediate(cam.gameObject);
		}

		Heatmap.DestroyHeatmapObjects();
	}

	public void CreateCamera()
	{
		if(HeatmapEditor.cam == null) {
			cam = new GameObject().AddComponent<Camera>();
			cam.gameObject.name = "Heatmap Camera";
		}
		cam.orthographic = true;
	}

	TextAsset heatmapTextAsset;
	public void OnGUI()
	{
		if(cam == null)
			CreateCamera();

		GUILayout.Label("Heatmap Text Asset", EditorStyles.boldLabel);
		heatmapTextAsset = (TextAsset)EditorGUILayout.ObjectField(heatmapTextAsset, typeof(TextAsset), true);

		GUILayout.Space(2);

		GUILayout.Label("Camera Orientation", EditorStyles.boldLabel);

		cameraOrientation = (CameraOrientation)EditorGUILayout.EnumPopup(cameraOrientation);

		if(previousOrientation != cameraOrientation)
		{
			AutosizeCamera(cam, cameraOrientation);
			previousOrientation = cameraOrientation;
		}
		
		// Camera Utility
		if(cameraOrientation == CameraOrientation.MANUAL)
		{
			cam = (Camera)EditorGUILayout.ObjectField(cam, typeof(Camera), true);
		}
		else
		{
			if( cameraOrientation == CameraOrientation.FORWARD)
				cam.transform.rotation = Quaternion.Euler( Vector3.zero );

			if( cameraOrientation == CameraOrientation.DOWN )
				cam.transform.rotation = Quaternion.Euler( new Vector3(90f, 0f, 0f) );

			if(GUI.Button(new Rect(0, Screen.height - 22, Screen.width, 20), "Force Update World Bounds"))
				worldBounds = GetWorldBounds();
		}

		pointRadius = EditorGUILayout.IntField("Point Radius", pointRadius);

		GUILayout.Label("cam - " + cam.name + ": " + cam.pixelWidth + ", " + cam.pixelHeight + "\nscreen: " + Screen.width + ", " + Screen.height);

		// Heatmap tools!
		if(GUILayout.Button("Refresh Heatmap")) {
			if(heatmapTextAsset == null) {
				Debug.LogWarning("No Heatmap log selected!");
				return;
			}
			EditorApplication.ExecuteMenuItem("Window/Game");
			heatmapOverlay = Heatmap.CreateHeatmap(StringUtility.Vector3ArrayWithFile(heatmapTextAsset), cam, pointRadius);
		}

		if(GUILayout.Button("Screenshot"))
			Heatmap.Screenshot("Assets/ImAHeatmap.png", cam);

		if(heatmapOverlay)
			GUILayout.Label(heatmapOverlay);
	}

	public void AutosizeCamera(Camera cam, CameraOrientation orientation)
	{
		switch(orientation)
		{
			case CameraOrientation.DOWN:
				cam.transform.position = new Vector3(worldBounds.center.x, worldBounds.max.y + 1f, worldBounds.center.z);
				cam.orthographicSize = (worldBounds.extents.x > worldBounds.extents.z) ? worldBounds.extents.x : worldBounds.extents.z;
				break;
		
			case CameraOrientation.FORWARD:
				cam.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, worldBounds.min.z - 1f);
				cam.orthographicSize = (worldBounds.extents.x > worldBounds.extents.y) ? worldBounds.extents.x : worldBounds.extents.y;
				break;
		}
	}

	public Bounds GetWorldBounds()
	{
		Object[] allGameObjects = GameObject.FindSceneObjectsOfType(typeof(GameObject));

		GameObject p = new GameObject();
		foreach(GameObject g in allGameObjects)
			g.transform.parent = p.transform;

		Component[] meshFilters = p.GetComponentsInChildren(typeof(MeshFilter));
		
		Vector3 min, max;
		if(meshFilters.Length > 0)
		{
			min = ((MeshFilter)meshFilters[0]).gameObject.transform.TransformPoint(((MeshFilter)meshFilters[0]).sharedMesh.vertices[0]);
			max = min;
		}
		else 
		{
			return new Bounds();
		}

		foreach(MeshFilter mf in meshFilters) {
			Vector3[] v = mf.sharedMesh.vertices;

			for(int i = 0; i < v.Length; i++)
			{
				Vector3 w = mf.gameObject.transform.TransformPoint(v[i]);
				if(w.x > max.x) max.x = w.x;
				if(w.x < min.x) min.x = w.x;
				
				if(w.y > max.y) max.y = w.y;
				if(w.y < min.y) min.y = w.y;

				if(w.z > max.z) max.z = w.z;
				if(w.z < min.z) min.z = w.z;	
			}
		}
		
		p.transform.DetachChildren();
		DestroyImmediate(p);

		Vector3 size = new Vector3( (max.x - min.x), (max.y - min.y), (max.z - min.z) );

		return new Bounds(size/2f + min, size);
	}
}
