using System;

namespace UnityEngine
{
    public class Component
    {
        public Component GetComponent(Type type) => throw new NotImplementedException();

        public Component GetComponentInChildren(Type type) => throw new NotImplementedException();

        public Component GetComponentInParent(Type type) => throw new NotImplementedException();
    }

    public class MonoBehaviour : Component
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponent : Attribute
    {
        public RequireComponent(Type type) => throw new NotImplementedException();
    }

    public static class Debug
    {
        public static void Log(object message, object context = null) => throw new NotImplementedException();

        public static void LogError(object message, object context = null) => throw new NotImplementedException();
    }
}