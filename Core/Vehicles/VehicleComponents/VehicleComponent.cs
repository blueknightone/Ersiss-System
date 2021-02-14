using System;
using System.Collections.Generic;
using ErsissSystem.Core.Vehicles.VehicleComponentFailures;
using ErsissSystem.Utilities;
using Godot;

namespace ErsissSystem.Core.Vehicles.VehicleComponents
{
    public class VehicleComponent : Node
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
            /// How many units of the resource are consumed by the component each update.
            /// </summary>
            /// <remarks>
            /// Set to allow the component to always run.
            /// </remarks>
            public float unitsConsumedPerTick = 0f;
        }

        /// <summary>
        /// Returns true if component has been switched on.
        /// </summary>
        public bool IsOn { get; private set; } = false;

        /// <summary>
        /// Is true if all conditions for activation are met.
        /// </summary>
        public bool IsActive => IsOn && componentFailureState is null && CanRun();

        /// <summary>
        /// How much operational time component has for wear and tear.
        /// </summary>
        public float TimeOnComponent { get; private set; } = 0f;

        /// <summary>
        /// Emits when the component is activated.
        /// </summary>
        [Signal]
        public delegate void ComponentActivated();

        /// <summary>
        /// Emits when the component experiences a failure.
        /// </summary>
        /// <param name="failure">The <code>VehicleComponentFailure</code> the part has experienced.</param>
        [Signal]
        public delegate void ComponentFailed(VehicleComponentFailure failure);

        /// <summary>
        /// Emits when the component has been deactivated.
        /// </summary>
        [Signal]
        public delegate void ComponentDeactivated();

        /// <summary>
        /// The resources the component takes in to convert into output resources.
        /// </summary>
        /// <remarks>
        /// If the component only stores another, mn
        /// </remarks>
        [Export] private List<ComponentResource> inputResources = new List<ComponentResource>();

        /// <summary>
        /// The resources the component produces to pass on to others.
        /// </summary>
        [Export] private List<ComponentResource> outputResources = new List<ComponentResource>();

        /// <summary>
        /// The starting chance percentage that a component will fail.
        /// </summary>
        /// <remarks>
        /// Set to zero to disable part failure. Set to 100 to always fail.
        /// </remarks>
        [Export(PropertyHint.Range, "0, 100")] private float startingFailureChance = 0f;

        /// <summary>
        /// When to increase the starting failure chance and by how much.
        /// </summary>
        [Export] private List<Vector2> failureChanceIncreaseSteps = new List<Vector2>();

        /// <summary>
        /// The current failure state of the component, if any 
        /// </summary>
        private VehicleComponentFailure componentFailureState = null;

        public void ToggleOn()
        {
            IsOn = true;
            Activate();
        }

        public void ToggleOff()
        {
            IsOn = false;
            Deactivate();
        }

        public void Activate()
        {
            if (IsOn && componentFailureState == null && CanRun())
            {
                EmitSignal(nameof(ComponentActivated));
            }
        }

        public void Deactivate()
        {
            if (componentFailureState != null)
            {
                EmitSignal(nameof(ComponentFailed), componentFailureState);
            }
            EmitSignal(nameof(ComponentDeactivated));
        }

        public void ProcessResources()
        {
            var resourceOverflow = new Dictionary<ResourceType, float>();
            
            foreach (ComponentResource resource in inputResources)
            {
                if (resource.buffer < resource.minimumOperationAmount)
                {
                    componentFailureState = new LowResourceFailure(this, resource.type);
                    EmitSignal(nameof(ComponentFailed), componentFailureState);
                }
                
                float overflow = UpdateResourceBuffer(resource, resource.unitsConsumedPerTick);
                if (!MathG.Approximately(overflow, 0f))
                {
                    resourceOverflow[resource.type] += overflow;
                }
            }

            if (resourceOverflow.Count > 0)
            {
                HandleOverflow(resourceOverflow);
            }
        }

        public Vector2 GetResourceBuffer(ResourceType resourceType, ResourceBufferDirection direction)
        {
            var resourceTotal = new Vector2();
            switch (direction)
            {
                case ResourceBufferDirection.INPUT:
                    foreach (ComponentResource resource in FilterResources(inputResources, resourceType))
                    {
                        resourceTotal.x = resource.buffer;
                        resourceTotal.y = resource.bufferCapacity;
                    }
                    break;
                case ResourceBufferDirection.OUTPUT:
                    foreach (ComponentResource resource in FilterResources(outputResources, resourceType))
                    {
                        resourceTotal.x = resource.buffer;
                        resourceTotal.y = resource.bufferCapacity;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            return resourceTotal;
        }

        private IEnumerable<ComponentResource> FilterResources(List<ComponentResource> resources,
            ResourceType resourceType)
        {
            yield return resources.Find(resource => resource.type == resourceType);
        }

        private void HandleOverflow(Dictionary<ResourceType, float> remaining)
        {
            // By default, just null the overflow
        }

        private bool CanRun()
        {
            foreach (ComponentResource resource in inputResources)
            {
                if (resource.buffer >= resource.minimumOperationAmount) continue;

                // If any resource does not have enough in its buffer to operate, return false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds or removes a given amount of a resource to that resource's internal buffer.
        /// Positive amounts fill, negative amounts drain.
        /// </summary>
        /// <param name="resource">The resource to add/remove to.</param>
        /// <param name="amount">The amount to add (positive) or remove (negative)</param>
        /// <returns>Any leftover resource amount in the case that the buffer is fully filled or drained.</returns>
        private float UpdateResourceBuffer(ComponentResource resource, float amount)
        {
            float nextBuffer = resource.buffer + amount;

            // Handle buffer filled to capacity
            if (nextBuffer > resource.bufferCapacity)
            {
                nextBuffer = resource.bufferCapacity - resource.buffer;
                resource.buffer = resource.bufferCapacity;
                return nextBuffer;
            }

            // Handle buffer drained completely
            if (nextBuffer < 0f)
            {
                nextBuffer = amount - resource.buffer;
                resource.buffer = 0f;
                return nextBuffer;
            }

            // Handle nextBuffer with 0f to bufferCapacity.
            resource.buffer = nextBuffer;
            return 0f;
        }
    }
}