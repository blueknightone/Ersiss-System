using ErsissSystem.Core.Vehicles.VehicleComponents;

namespace ErsissSystem.Core.Vehicles.VehicleComponentFailures
{
    public class ComponentWearFailure : VehicleComponentFailure
    {
        public ComponentWearFailure(VehicleComponent component) : base(component)
        {
            Reason = $"ComponentFailed:{nameof(component)}";
            Message = $"{nameof(component)} has failed due to wear. Replace immediately.";
            HelpLink = $"https://github.com/Enyss/Ersiss-System/wiki";
        }
    }
}