using UnityEngine;

namespace DragonQuest.Exploration
{
    public sealed class ObliqueCharacter : MonoBehaviour
    {
        private Transform anchor, left, right;
        private PlayerMovement movement;
        private Vector3 previous;
        private float phase;

        public void Initialize(Transform physicalAnchor, Transform leftLeg, Transform rightLeg)
        { anchor=physicalAnchor; left=leftLeg; right=rightLeg; movement=anchor.GetComponent<PlayerMovement>(); previous=anchor.position; }

        private void LateUpdate()
        {
            if(anchor==null||movement==null) return;
            float distance=Vector2.Distance(anchor.position,previous); previous=anchor.position;
            // Teleporte entre mapas nao deve produzir um passo gigante.
            bool walking=distance>0.0001f && distance<0.5f;
            if(walking) phase+=distance*9;
            Vector2 direction=movement.FacingDirection;
            transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.x,-direction.y)*Mathf.Rad2Deg);
            float angle=walking?Mathf.Sin(phase)*24:0;
            left.localRotation=Quaternion.Euler(angle,0,0); right.localRotation=Quaternion.Euler(-angle,0,0);
        }
    }
}
