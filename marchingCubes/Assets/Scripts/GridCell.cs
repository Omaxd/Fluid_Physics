using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public double[] values = new double[8];

    public Vector3[] p = new Vector3[8];
    public double[] val = { 0, 0, 0, 0, 0, 0, 0, 0 };

    public GridCell(GridCell gr)
    {
        this.val = gr.val;
    }
    public GridCell()
    {

    }
}
