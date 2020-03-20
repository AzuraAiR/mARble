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
    
    public static Ramp FindClosestRamp(Vector3 pos) {
        Ramp result = null;
        float shortestDist = float.PositiveInfinity;
        var e = Ramp.Pool.GetEnumerator();
        while(e.MoveNext()) {
            float currDist = (e.Current.transform.position - pos).sqrMagnitude;
            // float currDist = Mathf.Sqrt(Mathf.Pow(e.Current.transform.position.x-pos.x, 2) + Mathf.Pow(e.Current.transform.position.y-pos.y, 2));
            if(currDist < shortestDist) {
                result = e.Current;
                shortestDist = currDist;
            }
        }
        return result;
    }

}
