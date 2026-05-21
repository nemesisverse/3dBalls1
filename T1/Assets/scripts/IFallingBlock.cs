// IFallingBlock.cs
using UnityEngine;

public interface IFallingBlock
{
    Transform transform { get; }
    bool enabled { get; }

    // --- added for BlockCycler ---
    int StartIndex { get; set; }
    int CurrentIndex { get; }
    void StopMovement();
}