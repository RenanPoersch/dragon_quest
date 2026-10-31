using DragonQuest.Exploration.Layouts;
using UnityEngine;

namespace DragonQuest.Presentation.World.Characters
{
    // Eleva apenas a apresentacao (sprite e sombra); colisores e interacoes conservam coordenadas XY.
    // O tamanho aparente vem da perspectiva real da camera, sem mudar a escala fisica do personagem.
    [DefaultExecutionOrder(50)]
    public sealed class ThroneRoomDepth : MonoBehaviour
    {
        private Transform[] visuals;
        private Vector3[] origins;
        private void OnEnable() => Refresh();
        public void Refresh()
        {
            if(visuals==null)
            {
                visuals=new Transform[transform.childCount]; origins=new Vector3[visuals.Length];
                for(int i=0;i<visuals.Length;i++) { visuals[i]=transform.GetChild(i); origins[i]=visuals[i].localPosition; }
            }
            float height=ThroneRoomLayout.FloorHeight(transform.position);
            for(int i=0;i<visuals.Length;i++) if(visuals[i]!=null) visuals[i].localPosition=origins[i]+Vector3.back*height;
        }
        private void LateUpdate() => Refresh();
        private void OnDisable()
        {
            if(visuals==null) return;
            for(int i=0;i<visuals.Length;i++) if(visuals[i]!=null) visuals[i].localPosition=origins[i];
        }
    }
}
