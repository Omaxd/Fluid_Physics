using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controls : MonoBehaviour
{
    public MarchingCubes marchingCubes;



    void Update()
    {
        float a;
        float b;

        a = Input.GetAxis("Horizontal");
        b = Input.GetAxis("Vertical");

        Debug.Log(a);
        Debug.Log(b);


        if (a != 0)
        {
            gameObject.transform.Translate(new Vector3(1, 0, 0));
        }

    }

    //void FixedUpdate()
    //{
    //    float x;
    //    float y;

    //    x = Input.GetAxis("Horizontal");
    //    y = Input.GetAxis("Vertical");

    //    Vector3 zz = transform.rotation * Vector3.forward * y * m_MovementSpeed;

    //    m_Rig.velocity = zz;

    //    transform.Rotate(0, x * 5, 0);
    //}
}
