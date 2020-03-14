using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Domino : MonoBehaviour {   
    public readonly static HashSet<Domino> Pool = new HashSet<Domino>();

    private void OnEnable() {
        Domino.Pool.Add(this);
    }
 
    private void OnDisable() {
        Domino.Pool.Remove(this);
    }
 
    // Returns closest domino to position pos
    public static Domino FindClosestDomino(Vector3 pos) {
        Domino result = null;
        float shortestDist = float.PositiveInfinity;
        var e = Domino.Pool.GetEnumerator();
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
