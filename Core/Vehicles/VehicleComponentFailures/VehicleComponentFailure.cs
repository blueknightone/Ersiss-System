using System;
using ErsissSystem.Core.Vehicles.VehicleComponents;

namespace ErsissSystem.Core.Vehicles.VehicleComponentFailures
{
    [Serializable]
    public abstract class VehicleComponentFailure
    {
        public string Reason { get; protected set; }
        public string Message { get; protected set; }
        public string HelpLink { get; protected set; }

        protected VehicleComponentFailure(VehicleComponent component)
        {
            Reason = $"VehicleComponentFailure:{component.Name}";
            Message = $"{component.Name} has failed with the issue: {Reason}";
            HelpLink = $"https://github.com/Enyss/Ersiss-System/wiki";
        }

        public virtual string GetFormattedMessage()
        {
            return $"{Reason}/n" +
                   $"{Message}/n" +
                   $"Additional Information: {HelpLink}";
        }
    }
}