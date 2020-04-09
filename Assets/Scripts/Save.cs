using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Save
{
	public List<SerializableVector3> objectTransforms = new List<SerializableVector3>();
	public List<SerializableVector3> objectScales = new List<SerializableVector3>();
	public List<SerializableQuaternion> objectRotations = new List<SerializableQuaternion>();
	public List<string>objectTags = new List<string>();
}
