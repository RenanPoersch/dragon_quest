using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Interactions
{
    public enum SecretObject { Peasant, Altar, Inscription, Monolith, ClawStone, HollowTree }

    public sealed class AncientSecretInteraction : Interactable
    {
        private SecretObject kind;
        private MonolithSymbol symbol;
        private AncientSecrets secrets;
        private GameObject light;
        public override bool CanInteract => base.CanInteract && !(kind == SecretObject.Altar && secrets?.AltarSolved == true);
        public void Initialize(SecretObject type, AncientSecrets progress, MonolithSymbol rune = MonolithSymbol.Sun, GameObject glow = null)
        { kind=type; secrets=progress; symbol=rune; light=glow; RefreshLight(); }

        private void OnEnable() => RefreshLight();
        private void Update() => RefreshLight();
        private void RefreshLight()
        {
            if(light==null||secrets==null) return;
            bool visible=kind==SecretObject.Altar?secrets.AltarSolved:kind==SecretObject.Monolith&&secrets.IsLit(symbol);
            if(light.activeSelf!=visible) light.SetActive(visible);
        }
        public static string SymbolName(MonolithSymbol symbol) => symbol==MonolithSymbol.Moon?"Lua":symbol==MonolithSymbol.Star?"Estrela":"Sol";

        public override void Interact(InteractionContext context)
        {
            if (!CanInteract) return;
            DialogueController dialogue=context.Dialogue;
            if(kind==SecretObject.Peasant)
            {
                dialogue.OpenWithCompletion("Camponesa",secrets.LearnPoem,
                    "Minha avó me contava uma lenda quando eu era pequena. Eu sentava pertinho dela e pedia para ouvir de novo, mesmo já sabendo como terminava.",
                    "Hoje já não lembro direito... Era algo sobre chamas, lua e coisas assim. Ela dizia que tinha a ver com aquelas ruínas a sudoeste da ilha.",
                    "Não sei se acredito nessas coisas... Mas ela falava com tanta certeza. Queria poder perguntar de novo.");
            }
            else if(kind==SecretObject.Altar)
            {
                if(!secrets.KnowsPoem) dialogue.Open("Altar antigo",
                    "Uma inscrição em latim está gravada na pedra, você ainda não entende o que significa.");
                else dialogue.OpenChoices("Altar antigo","Uma chama envolve um reino em ruínas. Qual verso da lenda você recita?",LegendPoem.AltarChoices,index=>
                {
                    if(secrets.TrySolveAltar(LegendPoem.AltarChoices[index]))
                        dialogue.Open("Altar antigo","Uma chama desperta no altar. Ao longe, algo parece ceder entre as montanhas.");
                    else dialogue.Open("Altar antigo","...");
                    RefreshLight();
                });
            }
            else if(kind==SecretObject.Inscription)
                dialogue.Open("Inscrição antiga",LegendPoem.Sky,"Os três monólitos parecem esperar a passagem de um mesmo ciclo.");
            else if(kind==SecretObject.Monolith)
            {
                MonolithResult result=secrets.Activate(symbol);
                dialogue.Open("Monolito / "+SymbolName(symbol),result==MonolithResult.Reset
                    ? "..."
                    : result==MonolithResult.Completed?"Os três símbolos se erguem acima das pedras. Um tremor distante atravessa o chão."
                    : result==MonolithResult.AlreadyCompleted?"O ciclo foi completado. Os simbolos permanecem suspensos sobre as pedras."
                    : "O simbolo ganha um brilho suave. As outras pedras continuam em silencio.");
                RefreshLight();
            }
            else if(kind==SecretObject.ClawStone)
                dialogue.Open("Pedra marcada","Sulcos profundos cortam a pedra. Marcas menores seguem para nordeste, ate uma arvore antiga de raizes abertas.",
                    "Entre os sulcos ha vestigios de escamas. O tronco ao fim das marcas parece guardar algo em seu interior.");
            else
                dialogue.Open("Arvore oca",secrets.TryCollectScale()
                    ? "Entre as raizes voce encontra uma Escama ancestral. Ela vibra por um instante, como se respondesse a algo distante."
                    : "A abertura esta vazia. A Escama ancestral continua guardada com a equipe.");
        }
    }
}
