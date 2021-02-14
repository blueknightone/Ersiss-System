using ErsissSystem.Core.Vehicles.VehicleComponents;

namespace ErsissSystem.Core.Vehicles.VehicleComponentFailures
{
    public class LowResourceFailure : VehicleComponentFailure
    {
        public LowResourceFailure(VehicleComponent component, ResourceType resourceType) : base(component)
        {
            Reason = $"LowOrZeroResource:{resourceType.ToString()}, {component.Name}";
            Message = $"{resourceType.ToString()} is low or empty. Replenish {resourceType.ToString()} levels.";
            HelpLink = $"https://github.com/Enyss/Ersiss-System/wiki/";
        }
    }
}