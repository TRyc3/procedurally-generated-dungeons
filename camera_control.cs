using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class camera_control : MonoBehaviour
{
    public Manager generator;
    private Camera thisCamera;

    void Start()
    {
        thisCamera = GetComponent<Camera>();
        thisCamera.transform.position = new Vector3(generator.width / 2, generator.height / 2, -1);
        thisCamera.orthographicSize = Mathf.Sqrt(generator.width * generator.height / 4);
    }
}
