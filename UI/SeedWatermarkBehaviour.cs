using System;
using UnityEngine;

namespace SeededRun
{
    internal sealed class SeedWatermarkBehaviour : MonoBehaviour
    {
        public SeedWatermarkBehaviour(IntPtr pointer) : base(pointer)
        {
        }

        private void Update()
        {
            SeedWatermark.Update();
        }

        private void OnDestroy()
        {
            SeedWatermark.Destroy();
        }
    }
}
