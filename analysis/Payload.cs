using System.Runtime.Serialization;

namespace CSharpProbe
{
    [DataContract]
    class Payload
    {
        [DataMember]
        internal string probe_id;

        [DataMember]
        internal int value;

        [DataMember(Name = "params")]
        internal Result result;

        [DataMember]
        internal string quality_characteristics;

        public Payload(string probeID, Result result)
        {
            probe_id = probeID;
            value = 0;
            this.result = result;
            quality_characteristics = "performance";
        }
    }
}