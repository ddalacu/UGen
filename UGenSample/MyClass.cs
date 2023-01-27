using UGen.Runtime;
using UnityEngine;

namespace UGenSample
{
    public partial class MyClass : MonoBehaviour
    {
        [GetComponent]
        private IHaveData _test;
        
        [GetComponent(required: true)]
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