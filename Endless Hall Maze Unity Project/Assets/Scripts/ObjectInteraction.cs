using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static GameMacros;

public class ObjectInteraction : MonoBehaviour
{
    private int color = COLOR_NULL;
    public int Color
    {
        get { return color; }
        set { color = value; }
    }
}
