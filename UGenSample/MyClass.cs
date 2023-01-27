using UGen.Runtime;
using UGenSample;
using UnityEngine;

namespace UGenSample
{
    public partial class MyClass : MonoBehaviour
    {
        [GetComponent]
        private IHaveData _test;

        [GetComponent()]
        private Transform _value;

        [GetComponent(Where.Child)]
        private Transform _childTransform;


        [GetComponent(Where.Parent)]
        private Transform _parentTransform;

        public void Test()
        {
            InitializeComponent();
        }
    }
}