using UnityEngine;
using UnityEditor;

namespace Antypodish.NodePathFinding.DOTS
{
    /// <summary>
    /// To get right mask, collision filter will require use of ( 1 << filter value )
    /// Combining filters then can be done by addition.
    /// For details see game object / entities collider filters, collide with, belongs to.
    /// </summary>
    public enum CollisionFilters
    {
        PathNodes = 9,

        Floor     = 1,
        Walls     = 2,
        Ramps     = 3,
        // Other  = 4,
        Raycast   = 5
    }
}