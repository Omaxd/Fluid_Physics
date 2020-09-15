using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderExecution : MonoBehaviour
{
    // Start is called before the first frame update


    public Shader shader;
    void Start()
    {
        gameObject.GetComponent<Renderer>().material = new Material(shader);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
