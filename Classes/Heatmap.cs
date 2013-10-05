using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
/*! \The Heatmap class is responsible for creating the "heat" and textures.
 *
 *  Contains all methods necessary for creating a Heatmap.
 */
public class Heatmap : ScriptableObject {

#if UNITY_EDITOR
	[MenuItem("Window/Heatmap Documentation")]
	public static void OpenDocs()
	{
		Application.OpenURL("http://paraboxstudios.com/heatmap/docs/annotated.html");
	}
#endif

	public static GameObject projectionPlane;	//!< A static reference to the plane which is used to display a heatmap.

	public static int RED_THRESHOLD = 235; 		//!< Red threshold.
												///	Minimum alpha a point must have to be red.
	public static int GREEN_THRESHOLD = 200;	//!< Green threshold.
												///	Minimum alpha a point must have to be green.
	public static int BLUE_THRESHOLD = 150;		//!< Blue threshold.	
												///	Minimum alpha a point must have to be Blue.
	public static int MINIMUM_THRESHOLD = 100;	//!< Minimum threshold.	
												///	Minimum alpha a point must have to be rendered at all.

	/*!	<summary>
	*	Creates a Heatmap image given a set of world points.
	*	</summary>
	*	<remarks>
	*	This method accepts a series of world points, and returns a transparent overlay of the heatmap.  Usually you will want to pair this call with CreateRenderPlane() to actually view the heatmap.
	*	</remarks>
	*	<param name="worldPoints">An array of Vector3 points to be translated into heatmap points.</param>
	*	<param name="cam">The camera to render from.  If passed a null value, CreateHeatmap() attempts to use Camera.main.</param>
	*	<param name="radius">Raidus in pixels that each point should create.  Larger radii produce denser heatmaps, and vice-versa.</param>
	*	<returns>Returns a new Texture2D containing a transparent overlay of the heatmap.</returns>
	*/
	public static Texture2D CreateHeatmap(Vector3[] worldPoints, Camera cam, int radius) {

		if(cam == null) {
			if(Camera.main == null) {
				Debug.LogWarning("No camera found.  Returning an empty texture.");
				return new Texture2D(0, 0);
			}
			else
				cam = Camera.main;
		}

		// Create new texture
		// Texture2D map = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);	
		Texture2D map = new Texture2D( (int)cam.pixelWidth, (int)cam.pixelHeight, TextureFormat.ARGB32, false);	

		// Set texture to alpha-fied state
		map.SetPixels(Heatmap.ColorArray(new Color(1f, 1f, 1f, 0f), map.width*map.height), 0);

		// Convert world to screen points
		Vector2[] points = new Vector2[worldPoints.Length];
		for(int i = 0; i < worldPoints.Length; i++) {
			points[i] = cam.WorldToScreenPoint(worldPoints[i]);
		}		

		/*** Generate Grayscale Values ***/
		{
			int x2;							// the offset x val in img coordinates
		   	int y2;							// the offset y val in img coordinates (0,0) - (maxX, maxY)
		   	float pointAlpha = .9f;			// The alpha that the darkest pixel will be in a poitn.
			Color color = new Color(1f, 1f, 1f, pointAlpha);
			int lineWidth = 1;//(int)(radius * .05f);
			Dictionary<Vector2, Color> pixelAlpha = new Dictionary<Vector2, Color>();

			for(int i = 0; i < points.Length; i++)			// generate alpha add for each point and a specified circumference
			{
				pixelAlpha.Clear();
				for(int r = 0; r < radius; r+=lineWidth)	// draw and fill them circles
				{
					for(int angle=0; angle<360; angle++)
					{
						x2 = (int)(r * Mathf.Cos(angle))+(int)points[i].x;
						y2 = (int)(r * Mathf.Sin(angle))+(int)points[i].y;

						// This could be sped up
						for(int y = y2; y > y2-lineWidth; y--) {
							for(int x = x2; x < x2+lineWidth; x++) {
								Vector2 coord = new Vector2(x, y);

								if(pixelAlpha.ContainsKey(coord))
									pixelAlpha[coord] = color;
								else
									pixelAlpha.Add(new Vector2(x, y), color);
							}		
						}
					}
					color = new Color(color.r, color.g, color.b, color.a - (pointAlpha/( (float)radius/lineWidth)) );
				}

				// Since the radial fill code overwrites it's own pixels, make sure to only add finalized alpha to
				// old values.
				foreach (KeyValuePair<Vector2, Color> keyval in pixelAlpha)
				{
					Vector2 coord = keyval.Key;
					Color previousColor = map.GetPixel((int)coord.x, (int)coord.y);
					Color newColor = keyval.Value;
					map.SetPixel((int)coord.x, (int)coord.y, new Color(newColor.r, newColor.b, newColor.g, newColor.a + previousColor.a));
				}

				// Reset color for next point
				color = new Color(color.r, color.g, color.b, pointAlpha);
			}
		}

		map.Apply();

		map.SetPixels( Colorize(map.GetPixels(0)), 0);

		map.Apply();

		return map;
	}

	/*!	<summary>
	*	Creates a gameObject in front of the camera, and applies the supplied texture.
	*	</summary>
	*	<remarks>
	*	Works best with an orthographic camera.  It builds the mesh using camera dimensions translated into world space.  Use this in conjunction with CreateHeatmap() to create a heatmap and capture a screenshot with the heatmap texture overlaying the world.
	*	</remarks>
	*	<param name="map">The heatmap image.</param>
	*/
	public static void CreateRenderPlane(Texture2D map)
	{
		CreateRenderPlane(map, null);
	}

	/*!	<summary>
	*	Creates a gameObject in front of the camera, and applies the supplied texture.
	*	</summary>
	*	<remarks>
	*	Works best with an orthographic camera.  It builds the mesh using camera dimensions translated into world space.  Use this in conjunction with CreateHeatmap() to create a heatmap and capture a screenshot with the heatmap texture overlaying the world.
	*	</remarks>
	*	<param name="map">The heatmap image.</param>
	*	<param name="cam">The camera to render from.  If passed a null value, CreateRenderPlane() attempts to use Camera.main.</param>
	*/
	public static void CreateRenderPlane(Texture2D map, Camera cam)
	{
		if(cam == null) {
			if(Camera.main == null) {
				Debug.LogWarning("No camera found.  Plane not created.");
				return;
			}
			else
				cam = Camera.main;
		}

		// Create Plane to project Heatmap
		Mesh m = new Mesh();
		Vector3[] vertices = new Vector3[4];
		int[] triangles = new int[6] {
			2, 1, 0,
			2, 3, 1
		};

		vertices[0] = cam.ScreenToWorldPoint(new Vector2(0f, 0f));
		vertices[1] = cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, 0f));
		vertices[2] = cam.ScreenToWorldPoint(new Vector2(0f, cam.pixelHeight));
		vertices[3] = cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, cam.pixelHeight));

		Vector2[] uvs = new Vector2[4];

		uvs[0] = new Vector2(0f, 0f);
		uvs[1] = new Vector2(1f, 0f);
		uvs[2] = new Vector2(0f, 1f);
		uvs[3] = new Vector2(1f, 1f);

		m.vertices = vertices;
		m.triangles = triangles;
		m.uv = uvs;
		m.RecalculateNormals();
		m.Optimize();

        // Hook it all up
		if(projectionPlane == null) {
			projectionPlane = new GameObject();
			projectionPlane.AddComponent<MeshFilter>();
			projectionPlane.AddComponent<MeshRenderer>();	
		} else {
			DestroyImmediate(projectionPlane.GetComponent<MeshFilter>().sharedMesh);
			DestroyImmediate(projectionPlane.GetComponent<MeshRenderer>().sharedMaterial.mainTexture);
		}

		projectionPlane.GetComponent<MeshFilter>().sharedMesh = m;
		MeshRenderer mr = projectionPlane.GetComponent<MeshRenderer>();
		Material mat = (Material)Resources.Load("UnlitMaterial");
		mat.mainTexture = map;
		mr.sharedMaterial = mat;

		projectionPlane.name = "Heatmap Render Plane";
		// Move the heatmap gameobject in front of the camera
		projectionPlane.transform.position = new Vector3(0f, 0f, 0f);
		projectionPlane.transform.Translate(Vector3.forward, cam.transform);
	}

	/*!	<summary>
	*	Creates and saves a screenshot.
	*	</summary>
	*	<remarks>
	*	Call this to take a screenshot with the current camera.  Will not overwrite images if path already exists.
	*	</remarks>
	*	<param name="map">The path to save screenshot image to.  Path is relative to Unity project. Ex: "Assets/MyScreenshot.png"</param>
	*	<returns>
	*	Returns a string containing the actual path image was saved to.  This may be different than passed string if the path previously existed.
	*	</returns>
	*/	
	public static string Screenshot(string path)
	{
		return Heatmap.Screenshot(path, (Camera)null);
	}

	/*!	<summary>
	*	Creates and saves a screenshot.
	*	</summary>
	*	<remarks>
	*	Call this to take a screenshot with the current camera.  Will not overwrite images if path already exists.
	*	</remarks>
	*	<param name="path">The path to save screenshot image to.  Path is relative to Unity project. Ex: "Assets/MyScreenshot.png"</param>
	*	<param name="cam">The camera to render from.  If passed a null value, CreateRenderPlane() attempts to use Camera.main.</param>
	*	<returns>
	*	Returns a string containing the actual path image was saved to.  This may be different than passed string if the path previously existed.
	*	</returns>
	*/	
	public static string Screenshot(string path, Camera cam)
	{
		if(cam == null) {
			if(Camera.main == null)
				return "Error!  No camera found.";
			else
				cam = Camera.main;
		}

		foreach(Camera c in Camera.allCameras)
			c.enabled = false;

		cam.enabled = true;
		int i = 0;

		while(System.IO.File.Exists(path))
			path = path.Replace(".png", ++i + ".png");

		Application.CaptureScreenshot(path, 4);

#if UNITY_EDITOR
		AssetDatabase.Refresh();
#endif

		return path;
	}

	/*! \brief Destroy any temporary objects created by the Heatmap class.
	 *
	 *  By default, CreateHeatmap() creates a plane situated in front of the
	 *	camera to display the resulting heatmap.  Call Release() to destroy 
	 *	the heatmap image and mesh.
	 */
	public static void DestroyHeatmapObjects()
	{
		if(projectionPlane) {
			if(projectionPlane.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != null)
				DestroyImmediate(projectionPlane.GetComponent<MeshRenderer>().sharedMaterial.mainTexture);
			DestroyImmediate(projectionPlane);
		}		
	}
	
	public static Color[] ColorArray(Color col, int arraySize)
	{
		Color[] colArr = new Color[arraySize];
		for(int i  = 0; i < colArr.Length; i++)
		{
			colArr[i] = col;
		}
		return colArr;
	}

	/*
	 *	!!!
	 *	The Colorize() function is a modified version of the Colorize() method found
	 *	in the Mapex library.  This is the license associated with it.  This license
	 *	does not apply to the rest of the codebase included in this project, as it is
	 *	covered by the Unity Asset Store EULA.
	 *
	 * Copyright (C) 2011 by Vinicius Carvalho (vinnie@androidnatic.com)
	 *
	 * Permission is hereby granted, free of charge, to any person obtaining a copy
	 * of this software and associated documentation files (the "Software"), to deal
	 * in the Software without restriction, including without limitation the rights
	 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	 * copies of the Software, and to permit persons to whom the Software is
	 * furnished to do so, subject to the following conditions:
	 * 
	 * The above copyright notice and this permission notice shall be included in
	 * all copies or substantial portions of the Software.
	 * 
	 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	 * THE SOFTWARE.
	 */
	public static Color[] Colorize(Color[] pixels) {
		for (int i = 0; i < pixels.Length; i++) {
			
			float r = 0, g = 0, b = 0, tmp = 0;
			pixels[i] *= 255f;

			float alpha = pixels[i].a;

			if (alpha == 0) {
				continue;
			}

			if (alpha <= 255 && alpha >= RED_THRESHOLD) {
				tmp = 255 - alpha;
				r = 255 - tmp;
				g = tmp * 12f;
			} else if (alpha <= (RED_THRESHOLD - 1) && alpha >= GREEN_THRESHOLD) {
				tmp = (RED_THRESHOLD - 1) - alpha;
				r = 255 - (tmp * 8f);
				g = 255;
			} else if (alpha <= (GREEN_THRESHOLD - 1) && alpha >= BLUE_THRESHOLD) {
				tmp = (GREEN_THRESHOLD - 1) - alpha;
				g = 255;
				b = tmp * 5;
			} else if (alpha <= (BLUE_THRESHOLD - 1) && alpha >= MINIMUM_THRESHOLD) {
				tmp = (BLUE_THRESHOLD - 1) - alpha;
				g = 255 - (tmp * 5f);
				b = 255;
			} else
				b = 255;
			pixels[i] = new Color(r, g, b, alpha / 2f);
			pixels[i] = NormalizeColor(pixels[i]);
		}

		return pixels;
	}

	public static Color NormalizeColor(Color col)
	{
		return new Color( col.r / 255f, col.g / 255f, col.b / 255f, col.a / 255f);
	}

	public static Bounds WorldBounds()
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
