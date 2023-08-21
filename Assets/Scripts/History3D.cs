using System;
using UnityEngine;

public class History3D
{
    string currentState;
    Tuple<Vector3Int, string> step;

    public History3D(string currentState, Tuple<Vector3Int, string> step)
    {
        CurrentState = currentState;
        Step = step;
    }

    public string CurrentState { get => currentState; set => currentState = value; }
    public Tuple<Vector3Int, string> Step { get => step; set => step = value; }
}
