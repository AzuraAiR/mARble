using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Domino : MonoBehaviour
{   
    public readonly static HashSet<Domino> Pool = new HashSet<Domino>();

    private void OnEnable()
    {
        Domino.Pool.Add(this);
    }
 
    private void OnDisable()
    {
        Domino.Pool.Remove(this);
    }
 
 
    public static Domino FindClosestDomino(Vector3 pos)
    {
        Domino result = null;
        float dist = float.PositiveInfinity;
        var e = Domino.Pool.GetEnumerator();
        while(e.MoveNext())
        {
            float d = (e.Current.transform.position - pos).sqrMagnitude;
            if(d < dist)
            {
                result = e.Current;
                dist = d;
            }
        }
        return result;
    }
 
}
