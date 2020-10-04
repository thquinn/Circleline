using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code {
    public class Util {
        public static int CountTrue(params bool[] arr) {
            return arr.Count(b => b);
        }

        static Camera mainCamera;
        public static Collider GetMouseCollider(LayerMask layerMask) {
            if (mainCamera == null) mainCamera = Camera.main;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) {
                return null;
            }
            return hit.collider;
        }
        public static Vector3 GetMouseCollisionPoint(LayerMask layerMask) {
            if (mainCamera == null) mainCamera = Camera.main;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) {
                return Vector3.zero;
            }
            return hit.point;
        }

        public static float EaseTrack(float one, float two, float t) {
            if (one == two) {
                return one;
            }
            float linear = Mathf.Lerp(one, two, t);
            bool oneIsInt = Mathf.Floor(one) == one;
            bool twoIsInt = Mathf.Floor(two) == two;
            if (oneIsInt && twoIsInt) {
                return linear;
            }
            float eased;
            if (Mathf.Abs(one - two) < .5f) {
                eased = oneIsInt ? EasingFunction.EaseInQuad(one, two, t) : EasingFunction.EaseOutQuad(one, two, t);
            } else {
                eased = oneIsInt ? EasingFunction.EaseOutQuad(one, two, t) : EasingFunction.EaseInQuad(one, two, t);
            }
            return Mathf.Lerp(linear, eased, .4f);
        }
    }
}
