using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marble : MonoBehaviour
{
            // Returns closest domino to position pos
    public static Ramp FindClosestRamp(Vector3 pos) {
        Ramp result = null;
        float shortestDist = float.PositiveInfinity;
        var e = Ramp.Pool.GetEnumerator();
        while(e.MoveNext()) {
            float currDist = (e.Current.transform.position - pos).sqrMagnitude;
            if(currDist < shortestDist) {
                result = e.Current;
                shortestDist = currDist;
            }
        }
        return result;
    }
}
