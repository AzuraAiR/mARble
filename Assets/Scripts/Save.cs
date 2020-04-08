using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Save
{
	public List<SerializableVector3> dominoTransforms = new List<SerializableVector3>();
	public List<SerializableQuaternion> dominoRotations = new List<SerializableQuaternion>();
}
