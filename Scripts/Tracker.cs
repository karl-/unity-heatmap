using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*! \An example tracking script that logs the world position of the object it is attached to.
 *
 *  Set the logs per second and where to output the saved positions.  These can be read by the Heatmap class and turned into a Heatmap.
 */
public class Tracker : MonoBehaviour {

	public float logsPerSecond = 1f;			// Default to one log per second
	private float logSplit;
	private float timer = 0f;
	public List<Vector3> points = new List<Vector3>();
	public string HeatmapTextAssetPath = "Assets/PlayerPoints.txt";

	public void Start()
	{
		logSplit = 1f/logsPerSecond;
	}

	public void Update()
	{
		timer += Time.deltaTime;

		if(timer > logSplit)
		{
			timer = 0f;
			LogPosition(gameObject.transform.position);
		}
	}

	public void OnDisable()
	{
		StringUtility.Vector3ArrayToTextAsset(points.ToArray(), HeatmapTextAssetPath);
	}

	public void LogPosition(Vector3 position)
	{
		points.Add(position);
	}
}
