using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocket4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace SuperSocket.Management.AgentClient
{
    class AgentWebSocket : JsonWebSocket
    {
        private static Type m_DynamicTypeInterface = typeof(IDynamicMetaObjectProvider);

        public AgentWebSocket(string uri)
            : base(uri)
        {

        }

        protected override string SerializeObject(object target)
        {
            return JsonConvert.SerializeObject(target);
        }

        protected override object DeserializeObject(string json, Type type)
        {
            //if(type == typeof(JObject))
            //    return JObject.Parse(json);

            return JsonConvert.DeserializeObject(json, type);
        }
    }
}
