using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    [DataContract]
    public class OpcInitialData
    {
        [DataMember]
        public OpcNode[] Nodes { get; set; }

        [DataMember]
        public BaseTagValue[] Values { get; set; }
    }

    [DataContract]
    public class OpcNode
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FullPath { get; set; }

        [DataMember]
        public OpcNode[] Children { get; set; }

        [DataMember]
        public string ItemId { get; set; }

        [DataMember]
        public string TypeName { get; set; }
    }
}
