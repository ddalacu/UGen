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
}