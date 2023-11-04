using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorConverter
{
	public static Vector2Int ToInt2(Vector2 vec) => new((int)vec.x, (int)vec.y);
	public static Vector3Int ToInt3(Vector2 vec, int z) => new((int)vec.x, (int)vec.y, z);
	public static Vector2Int ToInt2(Vector3 vec) => new((int)vec.x, (int)vec.y);
	public static Vector3Int ToInt3(Vector3 vec) => new((int)vec.x, (int)vec.y, (int)vec.z);
	public static Vector2Int ToInt2(Vector3Int vec) => new(vec.x, vec.y);
	public static Vector3Int ToInt3(Vector2Int vec, int z) => new(vec.x, vec.y, z);

	public static Vector2 To2(Vector2Int vec) => new(vec.x, vec.y);
	public static Vector2 To2(Vector3Int vec) => new(vec.x, vec.y);
	public static Vector3 To3(Vector2Int vec) => new(vec.x, vec.y);
	public static Vector3 To3(Vector2Int vec, float z) => new(vec.x, vec.y, z);
	public static Vector3 To3(Vector3Int vec) => new(vec.x, vec.y, vec.z);
}
