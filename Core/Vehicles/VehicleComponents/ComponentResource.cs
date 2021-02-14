using System;

namespace ErsissSystem.Core.Vehicles.VehicleComponents
{
    /// <summary>
    /// Represents an accepted resource used by the component.
    /// </summary>
    [Serializable]
    public class ComponentResource
    {
        /// <summary>
        /// The type of resource. 
        /// </summary>
        public ResourceType type = ResourceType.ANY;
            
        /// <summary>
        /// The internal buffer for the resource.
        /// </summary>
        public float buffer = 0f;
            
        /// <summary>
        /// The maximum capacity of the resource's internal buffer.  
        /// </summary>
        public float bufferCapacity = float.MaxValue;
            
        /// <summary>
        /// The minimum buffer level the component must have to operate.
        /// </summary>
        /// <remarks>
        /// Set to zero to e disable.
        /// </remarks>
        public float minimumOperationAmount = 0f;
            
        /// <summary>
        /// How many units of the resource are consumed or produced by the component each update.
        /// </summary>
        /// <remarks>
        /// Set to allow the component to always run.
        /// </remarks>
        public float unitsPerTick = 0f;
    }
}