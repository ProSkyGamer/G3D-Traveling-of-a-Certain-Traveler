using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICanClimb
{
    public void ClimbWall(Vector3 addToPosition);
    public void JumpClimbing(Vector3 addToPosition);
    public void StartClimbing(RaycastHit wallHit);
    public void StopClimbing();
}
