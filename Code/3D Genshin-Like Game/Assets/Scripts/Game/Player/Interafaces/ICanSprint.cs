using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICanSprint
{
    public void StartSprint(Vector3 addToPosition);
    public void Sprint(Vector3 addToPosition);
}
