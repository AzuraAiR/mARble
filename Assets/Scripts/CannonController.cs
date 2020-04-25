using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : MonoBehaviour
{   

    public GameObject cannonBall;
    Rigidbody cannonballRB;
    private Transform shotPos;
    public GameObject explosion;
    private float firePower;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {   
        if (other.tag == "Marble") {

            FireCannon();
        }
    }

    public void FireCannon() {
        shotPos = this.transform.Find("shotPos").transform;
        // shotPos.rotation = transform.rotation;
        firePower = 600; //15
        GameObject cannonBallCopy = Instantiate(cannonBall, shotPos.position, shotPos.rotation) as GameObject;
        cannonballRB = cannonBallCopy.GetComponent<Rigidbody>();
        cannonballRB.AddForce(shotPos.forward * firePower);
        Instantiate(explosion, shotPos.position, shotPos.rotation);

    }
}
