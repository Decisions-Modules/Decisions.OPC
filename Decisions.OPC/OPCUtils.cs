using DecisionsFramework.ServiceLayer.Services.ContextData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    public static class DataPairExtensions
    {
        // from Decisions.Agent.Handlers.Helpers.DataPairExtensions
        public static T GetValueByKey<T>(this DataPair[] data, string key)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            DataPair pair = data.FirstOrDefault(d => (d.Name == key));
            if (pair == null)
                throw new Exception(string.Format("Data is not found by name: {0}", key));
            if (pair.OutputValue == null)
                return default(T);
            if (false == pair.OutputValue is T)
                throw new Exception(string.Format("Value ({0}) is not type of {1}", key, typeof(T).Name));
            return (T)pair.OutputValue;
        }

    }

}
