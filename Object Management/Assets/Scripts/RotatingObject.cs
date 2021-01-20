using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingObject : PersistableObject {

    [SerializeField]
    Vector3 angularVelocity;

	// Update is called once per frame
	void FixedUpdate () {
        transform.Rotate(angularVelocity * Time.deltaTime);	
	}
}
