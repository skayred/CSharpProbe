using System.Runtime.Serialization;

namespace CSharpProbe
{
    [DataContract]
    class ResultModule
    {
        [DataMember]
        internal string name;

        [DataMember]
        internal double maintainability;

        public ResultModule(string name, double maintainabilityIndex)
        {
            this.name = name;
            this.maintainability = maintainabilityIndex;
        }
    }
}