using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinForver : MonoBehaviour 
{
    float rot = 0;
	void Update () 
    {
        rot += 1;
        transform.rotation = Quaternion.Euler(0, rot, 0);
	}
}
