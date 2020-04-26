using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marble : MonoBehaviour
{
    public readonly static HashSet<Marble> Pool = new HashSet<Marble>();

    private void OnEnable() {
        Marble.Pool.Add(this);
    }
 
    private void OnDisable() {
        Marble.Pool.Remove(this);
    }

    public static Marble AwakenMarbles() {
        Marble result = null;
        var e = Marble.Pool.GetEnumerator();
        while(e.MoveNext()) {
            
            e.Current.GetComponent<Rigidbody>().WakeUp();
            
        }
        return result;
    }


}
