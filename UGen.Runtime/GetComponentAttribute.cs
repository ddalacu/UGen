using System;

namespace UGen.Runtime
{
    public enum Where
    {
        This = 0,
        Parent = 1,
        Child = 2,
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class GetComponentAttribute : Attribute
    {
        public bool Required { get; }

        public Where Flags { get; }

        public GetComponentAttribute(Where flags = Where.This, bool required = true)
        {
            Flags = flags;
            Required = required;
        }
    }
}