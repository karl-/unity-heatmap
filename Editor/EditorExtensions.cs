/*
 *	Adds context menu item to create Text Asset
 *	Karl Henkel
 */

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EditorExtensions : Editor {

	[MenuItem("Assets/Create/Text Asset")]
	public static void Create(MenuCommand command)
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);

		if(path == "")
			path = "Assets/";

		// If path is a file, get the parent directory
		if(File.Exists(path))
			path = Path.GetDirectoryName(path);

		path += "/New Text Asset.txt";

		path = AssetDatabase.GenerateUniqueAssetPath(path);

		File.CreateText(path);

		AssetDatabase.ImportAsset(path);

		Selection.activeObject = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
	}

	[MenuItem("GameObject/Create/Sprite _%k")]
	public static void CreateSprite()
	{
		List<Texture2D> imgs = new List<Texture2D>();
		foreach(Texture2D tex in Selection.GetFiltered(typeof(Texture2D), SelectionMode.Deep))
			imgs.Add(tex);

		if(imgs.Count > 0)
		{
			foreach(Texture2D img in imgs)
				CreateSpriteMesh(img);
		}
		else
			CreateSpriteMesh(null);

	}

	public static void CreateSpriteMesh(Texture2D img)
	{
		GameObject go = new GameObject();
		go.name = "New Sprite Object";
		
		ScreenCenter(go);

		Mesh m = new Mesh();
		m.name = "Sprite";

		float scale = (img) ? (float)img.height/(float)img.width : 1f;
		Vector3[] v = new Vector3[4] {
			new Vector3(-.5f, -.5f * scale, 0f),
			new Vector3(.5f, -.5f * scale, 0f),
			new Vector3(-.5f, .5f * scale, 0f),
			new Vector3(.5f, .5f * scale, 0f)
		};
		int[] t = new int[6] {
			2, 1, 0,
			2, 3, 1
		};
		Vector2[] u = new Vector2[4] {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};

		m.vertices = v;
		m.triangles = t;
		m.uv = u;
		m.RecalculateNormals();
		m.Optimize();

		go.AddComponent<MeshFilter>().sharedMesh = m;
		go.AddComponent<MeshRenderer>();
		Material mat = new Material(Shader.Find("Unlit/Transparent"));
		if(img)	mat.mainTexture = img;
		go.GetComponent<MeshRenderer>().sharedMaterial = mat;
	}

	public static void ScreenCenter(GameObject _gameObject)
	{
		// If in the unity editor, attempt to center the object the sceneview or main camera, in that order
		if(SceneView.lastActiveSceneView)
			_gameObject.transform.position = SceneView.lastActiveSceneView.pivot;
		else
			_gameObject.transform.position = ((SceneView)SceneView.sceneViews[0]).pivot;

		Selection.activeObject = _gameObject;
	}
}
