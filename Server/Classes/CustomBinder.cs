using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Server.Classes
{
    public class CustomBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Assembly currentasm = Assembly.GetExecutingAssembly();

            string[] typesLevel = typeName.Split('.');
            string newType = $"{currentasm.GetName().Name}.{typesLevel[1]}.{typesLevel[2]}";

            return Type.GetType(newType);
        }
    }
}
