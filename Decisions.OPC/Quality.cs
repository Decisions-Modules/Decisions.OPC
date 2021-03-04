using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    public enum OpcQualityStatus
    {
        Bad = 0,
        Uncertain = 64,
        Error = 128,
        Good = 192
    }

    public enum OpcQualitySubStatus
    {
        [Description("Bad - Non-specific")]
        Bad = 0,
        [Description("Bad - Configuration Error")]
        BadConfigurationError = 4,
        [Description("Bad - Not Connected")]
        BadNotConnected = 8,
        [Description("Bad - Device Failure")]
        BadDeviceFailure = 12,
        [Description("Bad - Sensor Failure")]
        BadSensorFailure = 16,
        [Description("Bad - Last Known Value")]
        BadLastKnown = 20,
        [Description("Bad - Comm Failure")]
        BadCommFailure = 24,
        [Description("Bad - Out of Service")]
        BadOutOfService = 28,
        [Description("Bad - Waiting for Initial Data")]
        BadWaitingForInitialData = 32,
        [Description("Uncertain - Non-specific")]
        Uncertain = 64,
        [Description("Uncertain - Last Usable Value")]
        UncertainLastUsableValue = 68,
        [Description("Uncertain - Sensor Not Accurate")]
        UncertainSensorNotAccurate = 80,
        [Description("Uncertain - Engineering Units Exceeded")]
        UncertainEngineeringUnitsExceeded = 84,
        [Description("Uncertain - Sub-Normal")]
        UncertainSubNormal = 88,
        [Description("Good - Non-specific")]
        Good = 192,
        [Description("Good - Local Override")]
        GoodLocalOverride = 216
    }

    public enum OpcQualityLimit
    {
        [Description("Not Limited")]
        NotLimited = 0,
        [Description("Low Limited")]
        LowLimited = 1,
        [Description("High Limited")]
        HighLimited = 2,
        Constant = 3
    }

    [DataContract]
    public class OpcQuality
    {
        [DataMember]
        public OpcQualityStatus Status { get; set; }
        [DataMember]
        public OpcQualitySubStatus SubStatus { get; set; }
        [DataMember]
        public OpcQualityLimit Limit { get; set; }
    }

}
