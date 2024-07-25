using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP.Tools.Unity
{
	public static class VectorConverter
	{
		public static Vector2Int ToInt2(Vector2 vec) => new Vector2Int((int)vec.x, (int)vec.y);
		public static Vector3Int ToInt3(Vector2 vec, int z) => new Vector3Int((int)vec.x, (int)vec.y, z);
		public static Vector2Int ToInt2(Vector3 vec) => new Vector2Int((int)vec.x, (int)vec.y);
		public static Vector3Int ToInt3(Vector3 vec) => new Vector3Int((int)vec.x, (int)vec.y, (int)vec.z);
		public static Vector2Int ToInt2(Vector3Int vec) => new Vector2Int(vec.x, vec.y);
		public static Vector3Int ToInt3(Vector2Int vec, int z) => new Vector3Int(vec.x, vec.y, z);

		public static Vector2 To2(Vector2Int vec) => new Vector2(vec.x, vec.y);
		public static Vector2 To2(Vector3Int vec) => new Vector2(vec.x, vec.y);
		public static Vector3 To3(Vector2 vec) => new Vector3(vec.x, vec.y);
		public static Vector3 To3(Vector2 vec, float z) => new Vector3(vec.x, vec.y, z);
		public static Vector3 To3(Vector2Int vec) => new Vector3(vec.x, vec.y);
		public static Vector3 To3(Vector2Int vec, float z) => new Vector3(vec.x, vec.y, z);
		public static Vector3 To3(Vector3Int vec) => new Vector3(vec.x, vec.y, vec.z);

		public static int[] ToIntArray(Vector2Int vec) => new int[] { vec.x, vec.y };
		public static int[] ToIntArray(Vector3Int vec) => new int[] { vec.x, vec.y, vec.z };
		public static float[] ToFloatArray(Vector2Int vec) => new float[] { vec.x, vec.y };
		public static float[] ToFloatArray(Vector3Int vec) => new float[] { vec.x, vec.y, vec.z };
		public static float[] ToFloatArray(Vector2 vec) => new float[] { vec.x, vec.y };
		public static float[] ToFloatArray(Vector3 vec) => new float[] { vec.x, vec.y, vec.z };
	}
}
