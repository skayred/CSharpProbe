using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CSharpProbe
{
    [DataContract]
    class Result
    {
        [DataMember]
        internal double maintainability;

        [DataMember]
        internal int loc;

        [DataMember]
        internal double coverage;

        [DataMember]
        internal string revision;

        [DataMember]
        internal string datetime;

        [DataMember]
        internal List<ResultModule> modules;

        public Result(double maintainability, int loc, double coverage, string revision, string datetime, List<ResultModule> modules)
        {
            this.maintainability = maintainability;
            this.loc = loc;
            this.coverage = coverage;
            this.revision = revision;
            this.datetime = datetime;
            this.modules = modules;
        }
    }
}