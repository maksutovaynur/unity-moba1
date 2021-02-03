using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPlayerFollower : MonoBehaviour
{
    public Transform target;
    private Vector3 velocity;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            var pos = target.position;
            pos.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, 0.2f);
        }
            
    }
}
