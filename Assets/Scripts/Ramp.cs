using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ramp : MonoBehaviour
{
    public readonly static HashSet<Ramp> Pool = new HashSet<Ramp>();

    private void OnEnable() {
        Ramp.Pool.Add(this);
    }
 
    private void OnDisable() {
        Ramp.Pool.Remove(this);
    }


}
