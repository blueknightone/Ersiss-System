using System;
using System.Collections.Generic;
using ErsissSystem.Core.Vehicles.VehicleComponentFailures;
using ErsissSystem.Utilities;
using Godot;

namespace ErsissSystem.Core.Vehicles.VehicleComponents
{
    public class VehicleComponent : Node
    {
        #region Properties

        /// <summary>
        /// Returns true if component has been switched on.
        /// </summary>
        public bool IsOn { get; private set; } = false;

        /// <summary>
        /// Is true if all conditions for activation are met.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// How much operational time component has for wear and tear.
        /// </summary>
        public float TimeOnComponent { get; private set; } = 0f;

        #endregion

        #region Signals

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

        #endregion

        #region Exports

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

        #endregion

        #region Private Members

        /// <summary>
        /// The current failure state of the component, if any 
        /// </summary>
        private VehicleComponentFailure componentFailureState = null;

        #endregion

        #region Godot Lifecycle Methods

        public override void _Process(float delta)
        {
            if (!IsActive) return;
            
            if (CheckRunnable())
            {
                ProcessResources();
            }
            else
            {
                HandleComponentFailed();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggles the component's state to on and attempts to activate.
        /// </summary>
        public void ToggleOn()
        {
            IsOn = true;
            Activate();
        }

        /// <summary>
        /// Toggles the component off and deactivates
        /// </summary>
        public void ToggleOff()
        {
            IsOn = false;
            Deactivate();
        }

        /// <summary>
        /// Informs observers if component has been activated or not.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            EmitSignal(nameof(ComponentActivated));
        }

        
        /// <summary>
        /// Informs observers when component fails and/or deactivates.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            EmitSignal(nameof(ComponentDeactivated));
        }

        /// <summary>
        /// Returns the total amount of a resource's internal buffers.
        /// </summary>
        /// <param name="resourceType">The resource type to report on.</param>
        /// <param name="direction">Target the component's input or output buffers.</param>
        /// <returns>Returns a Vector2 where x equals the total amount stored and y equals the total capacity.</returns>
        public Vector2 GetResourceBufferLevels(ResourceType resourceType, ResourceBufferDirection direction)
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
        

        #endregion

        #region Private Methods

        /// <summary>
        /// Informs observers and attempts to deactivate the part when the component fails.
        /// </summary>
        private void HandleComponentFailed()
        {
            EmitSignal(nameof(ComponentFailed), componentFailureState);
            Deactivate();
        }

        /// <summary>
        /// Converts inputResources into outputResources according to each resources <code>unitsPerTick</code> property.
        /// </summary>
        private void ProcessResources()
        {
            var resourceOverflow = new Dictionary<ResourceType, float>();
            
            foreach (ComponentResource resource in inputResources)
            {
                if (resource.buffer < resource.minimumOperationAmount)
                {
                    componentFailureState = new LowResourceFailure(this, resource.type);
                    HandleComponentFailed();
                    break;
                }
                
                float overflow = UpdateResourceBuffer(resource, resource.unitsPerTick);
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

        /// <summary>
        /// Filters a given list of <code>ComponentResources</code> by a given resource type.
        /// </summary>
        /// <param name="resources">The list to filter</param>
        /// <param name="resourceType">The type to filter by.</param>
        /// <returns>Returns an IEnumerable list of resources matching the give resource type.</returns>
        private IEnumerable<ComponentResource> FilterResources(List<ComponentResource> resources,
            ResourceType resourceType)
        {
            yield return resources.Find(resource => resource.type == resourceType);
        }

        /// <summary>
        /// Handles any overflow from resource processing.
        /// </summary>
        /// <param name="remaining">The resources which overflow their buffers.</param>
        private void HandleOverflow(Dictionary<ResourceType, float> remaining)
        {
            // By default, just null the overflow
        }

        /// <summary>
        /// Determines if there are enough of each input resource to run the component.
        /// </summary>
        /// <returns>Returns true if all resource buffers have at least <code>ComponentResource.minimumOperationAmount</code></returns>
        private bool CheckRunnable()
        {
            foreach (ComponentResource resource in inputResources)
            {
                if (resource.buffer >= resource.minimumOperationAmount) continue;

                // If any resource does not have enough in its buffer to operate, return false;
                componentFailureState = new LowResourceFailure(this, resource.type);
                return false;
            }

            componentFailureState = null;
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

        #endregion
    }
}