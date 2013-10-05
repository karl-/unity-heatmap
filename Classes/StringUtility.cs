using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*! \A utility class for manipulating strings.
 *
 *  Contains a number of methods that make creating and reading Heatmap compatible text files very easy.
 */
public class StringUtility : MonoBehaviour {

	/*!	<summary>
	*	Takes an array of GameObjects and returns a text asset containing all of their world positions.
	*	</summary>
	*	<remarks>
	*	This text asset may be read using Vector3ArrayWithFile().
	*	</remarks>
	*	<param name="gos">An array of gameObjects to extract Vector3 data from.</param>
	*	<param name="path">The path to save the resulting text asset to.  Project relative.</param>
	*/
	public static void GameObjectArrayToTextAsset(GameObject[] gos, string path)
	{
#if !UNITY_WEBPLAYER
		string str = StringUtility.StringWithGameObjects(gos);
		System.IO.File.WriteAllText(path, str);
#endif	
	}

	/*! <summary>
	*	Takes a Vector3 array and returns a text asset containing all the data in string format.
	*	</summary>
	*	<remarks>
	*	This text asset may be read using Vector3ArrayWithFile().
	*	</remarks>
	*	<param name="pos">An Vector3 array that will be used to populate the text.</param>
	*	<param name="path">The path to save the resulting text asset to.  Project relative.</param>
	*/
	public static void Vector3ArrayToTextAsset(Vector3[] pos, string path)
	{
#if !UNITY_WEBPLAYER
		string str = StringUtility.StringWithVector3Array(pos);
		System.IO.File.WriteAllText(path, str);	
#endif
	}

	/*! <summary>
	 *	Translates an array of gameObjects into a string of Vector3s.
	 *	</summary>
	 *	<param name="gos">
	 *	The gameObject array.
	 *	</param>
	 */
	public static string StringWithGameObjects(GameObject[] gos)
	{
		string str = "";
		for(int i = 0; i < gos.Length; i++)
		{
			str += gos[i].transform.position + "\n";
		}
		return str;			
	}

	/*! <summary>
	 *	Translates an array of Vector3 points into a string of Vector3s.
	 *	</summary>
	 *	<param name="points">
	 *	The Vector3 array.
	 *	</param>
	 */
	public static string StringWithVector3Array(Vector3[] points)
	{
		string str = "";
		for(int i = 0; i < points.Length; i++)
		{
			str += points[i] + "\n";
		}
		return str;				
	}

	/*! <summary>
	 *	Reads a string of Vector3 data and returns an array of Vector3s.
	 *	</summary>
	 *	<param name="txt">
	 *	The text asset to extract data from.
	 *	</param>
	 */	
	public static Vector3[] Vector3ArrayWithFile(TextAsset txt)
	{
		string[] lines = txt.text.Split('\n');
		List<Vector3> points = new List<Vector3>();

		for(int i = 0; i < lines.Length; i++)
		{
			points.Add(StringUtility.Vector3WithString(lines[i]));
		}

		return points.ToArray();
	}

	/*! <summary>
	 *	Given a string, this method attempts to parse a Vector3 from it.
	 *	</summary>
	 *	<param name="str">
	 *	The string to attempt parsing.
	 *	</param>
	 */
	public static Vector3 Vector3WithString(string str)
	{
		str = str.Replace("(", "");
		str = str.Replace(")", "");
		string[] split = str.Split(',');
		if(split.Length < 3)
			return new Vector3(0f, 0f, 0f);

		float v0 = float.Parse(split[0], System.Globalization.CultureInfo.InvariantCulture);
		float v1 = float.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);
		float v2 = float.Parse(split[2], System.Globalization.CultureInfo.InvariantCulture);
		return new Vector3(v0, v1, v2);
	}
}
