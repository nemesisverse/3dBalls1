// IFallingBlock.cs
using UnityEngine;

public interface IFallingBlock
{
    Transform transform { get; }
    bool enabled { get; }
}