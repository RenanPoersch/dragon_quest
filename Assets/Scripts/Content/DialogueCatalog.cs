using DragonQuest.Progression;

namespace DragonQuest.Content
{
    public static class DialogueCatalog
    {
        public static readonly DialogueDefinition Citizen = new DialogueDefinition("city-citizen", DialogueSpeaker.Citizen, "Aldeão",
                "Ah, então vocês são os aventureiros que vieram do sul? Devem estar cansados da viagem.",
                "Podem dormir e se recuperar na pousada. E, se precisarem de mantimentos, deem uma olhada nas lojas da cidade.",
                "Não temos muita coisa por aqui, mas deve ser o suficiente.");

        public static readonly DialogueDefinition Traveler = new DialogueDefinition("city-traveler", DialogueSpeaker.Traveler, "Viajante",
                "Encontrei marcas incomuns numa pedra ao norte. Não parecem ter sido feitas por nenhum animal que eu conheça.",
                "Havia esse objeto vermelho perto dela...");

        public static readonly DialogueDefinition Explorer = new DialogueDefinition("city-explorer", DialogueSpeaker.Explorer, "Exploradora", "Cavar, cavar... Explorar, explorar...");

        public static readonly DialogueDefinition QuarryKeeper = new DialogueDefinition("quarry-keeper", DialogueSpeaker.QuarryKeeper, "Guardião da pedreira",
                "Acho que existe algum mecanismo nessas pedras. Os símbolos, o jeito como estão dispostas... Mas não tenho certeza. Pode ser só coisa da minha cabeça.",
                "Volto aqui sempre que preciso pensar. Fico olhando para elas e imaginando quantas coisas neste mundo a gente ainda não entende... Às vezes esqueço até de ir embora.");

        public static readonly DialogueDefinition Gardener = new DialogueDefinition("forest-gardener", DialogueSpeaker.Gardener, "Jardineiro",
                "Já houve um tempo em que estes jardins viviam cheios. As crianças vinham buscar flores... Hoje eu cuido deste cantinho. É o que as costas ainda permitem.",
                "Perdi minha casa, enterrei amigos... Até fome eu passei. Tem manhã em que a gente acorda e pensa se ainda vale a pena levantar.",
                "Mas aí eu venho aqui e encontro uma folha nova, uma flor que abriu durante a noite... E já começo a pensar no que vou plantar amanhã.",
                "Eu ainda quero ver este jardim florir de novo. Tem de haver dias melhores... Até para um velho como eu.");

        public static readonly DialogueDefinition KingConfrontation = new DialogueDefinition("king-confrontation", DialogueSpeaker.Tyrant, "Rei Rubro",
            "Todo caminho nesta ilha termina diante do meu trono. Curvem-se, ou o próprio céu cairá sobre vocês!");

        public static readonly DialogueDefinition BridgeGuards = new DialogueDefinition("bridge-guards", DialogueSpeaker.RoyalGuard, "Guarda Real", "Alto! Ninguém atravessa esta ponte. Por ordem do Rei Rubro, vocês não passarão! Preparem-se para lutar!");

        public static readonly DialogueDefinition PeasantLegend = new DialogueDefinition("peasant-legend", DialogueSpeaker.Peasant, "Camponesa",
            "Minha avó me contava uma lenda quando eu era pequena. Eu sentava pertinho dela e pedia para ouvir de novo, mesmo já sabendo como terminava.",
            "Hoje já não lembro direito... Era algo sobre chamas, lua e coisas assim. Ela dizia que tinha a ver com aquelas ruínas a sudoeste da ilha.",
            "Não sei se acredito nessas coisas... Mas ela falava com tanta certeza. Queria poder perguntar de novo.");

        public static readonly DialogueDefinition UnknownAltar = new DialogueDefinition("altar-unknown", DialogueSpeaker.Aren, "Altar antigo",
            "Uma inscrição em latim está gravada na pedra, você ainda não entende o que significa.");

        public static readonly DialogueDefinition AltarVerse = DialogueDefinition.Choice("altar-verse", DialogueSpeaker.Aren, "Altar antigo",
            "Uma chama envolve um reino em ruínas. Qual verso da lenda você recita?", LegendPoem.AltarChoices);

        public static readonly DialogueDefinition AltarAwakened = new DialogueDefinition("altar-awakened", DialogueSpeaker.Aren, "Altar antigo",
            "Uma chama desperta no altar. Ao longe, algo parece ceder entre as montanhas.");

        public static readonly DialogueDefinition AltarSilent = new DialogueDefinition("altar-silent", DialogueSpeaker.Aren, "Altar antigo", "...");

        public static readonly DialogueDefinition AncientInscription = new DialogueDefinition("ancient-inscription", DialogueSpeaker.Aren, "Inscrição antiga",
            LegendPoem.Sky, "Os três monólitos parecem esperar a passagem de um mesmo ciclo.");

        private static readonly DialogueDefinition MonolithReset = new DialogueDefinition("monolith-reset", DialogueSpeaker.Aren, "Monolito / {0}", "...");
        private static readonly DialogueDefinition MonolithCompleted = new DialogueDefinition("monolith-completed", DialogueSpeaker.Aren, "Monolito / {0}",
            "Os três símbolos se erguem acima das pedras. Um tremor distante atravessa o chão.");
        private static readonly DialogueDefinition MonolithAlreadyCompleted = new DialogueDefinition("monolith-already-completed", DialogueSpeaker.Aren, "Monolito / {0}",
            "O ciclo foi completado.");
        private static readonly DialogueDefinition MonolithActivated = new DialogueDefinition("monolith-activated", DialogueSpeaker.Aren, "Monolito / {0}",
            "O símbolo ganha um brilho suave. As outras pedras continuam em silêncio.");

        public static DialogueDefinition Monolith(MonolithSymbol symbol, MonolithResult result)
        {
            DialogueDefinition definition = result == MonolithResult.Reset ? MonolithReset
                : result == MonolithResult.Completed ? MonolithCompleted
                : result == MonolithResult.AlreadyCompleted ? MonolithAlreadyCompleted : MonolithActivated;
            string symbolName = symbol == MonolithSymbol.Moon ? "Lua" : symbol == MonolithSymbol.Star ? "Estrela" : "Sol";
            return definition.Format(symbolName);
        }

        public static readonly DialogueDefinition ClawStone = new DialogueDefinition("claw-stone", DialogueSpeaker.Aren, "Pedra marcada", LegendPoem.Forest);

        public static readonly DialogueDefinition HollowTreeScale = new DialogueDefinition("hollow-tree-scale", DialogueSpeaker.Aren, "Árvore oca",
            "Entre as raízes você encontra uma Escama ancestral. Ela vibra por um instante, como se respondesse a algo distante.");

        public static readonly DialogueDefinition AncestralScaleReceived = DialogueDefinition.Notice("ancestral-scale-received", "Recebeu Escama ancestral");

        public static readonly DialogueDefinition PassageOpened = new DialogueDefinition("ancestral-passage-opened", DialogueSpeaker.Aren, "Barreira ancestral",
            "A Escama ancestral vibra. Os três selos se desfazem e a barreira mágica ao redor do vulcão se dissipa.");

        private const string PassageDescription = "Uma barreira mágica envolve o vulcão. Três selos brilham sobre a superfície.";
        public static readonly DialogueDefinition PassageSealed = new DialogueDefinition("ancestral-passage-sealed", DialogueSpeaker.Aren, "Barreira ancestral", PassageDescription);
        public static readonly DialogueDefinition PassageResisting = new DialogueDefinition("ancestral-passage-resisting", DialogueSpeaker.Aren, "Barreira ancestral",
            PassageDescription, "A escama vibra, mas alguns selos ainda resistem.");

        private static readonly DialogueDefinition ChestGoldTemplate = new DialogueDefinition("square-chest-gold", DialogueSpeaker.Aren, "Baú da praça", "Você encontrou {0} moedas de ouro!");
        public static DialogueDefinition ChestGold(int amount) => ChestGoldTemplate.Format(amount);
        public static readonly DialogueDefinition ChestEmpty = new DialogueDefinition("square-chest-empty", DialogueSpeaker.Aren, "Baú da praça", "O baú está vazio");

        private const string DragonOutburst = "Minha mente... DEIXE-ME EM PAZ!";
        private static readonly DialogueDefinition SleepingDragon = new DialogueDefinition("dragon-sleeping-confrontation", DialogueSpeaker.Dragon, "Dragão corrompido",
            "O dragão dorme sobre o basalto quente. Ao ouvir seus passos, abre os olhos, ergue a cabeça e estende as asas. O covil estremece com seu rugido.", DragonOutburst);
        private static readonly DialogueDefinition AwakeDragon = new DialogueDefinition("dragon-awake-confrontation", DialogueSpeaker.Dragon, "Dragão corrompido", DragonOutburst);
        public static DialogueDefinition DragonConfrontation(bool wasSleeping) => wasSleeping ? SleepingDragon : AwakeDragon;

        private static readonly string[] DragonEssencePages =
        {
            "As vozes... Minha mente... eu... eu... sou eu novamente! Estou livre!",
            "Meu coração está em dívida com vocês. Espero que isso ajude em sua jornada..."
        };
        public static readonly DialogueDefinition DragonEssence = new DialogueDefinition("dragon-essence", DialogueSpeaker.DragonEssence, "Essência do dragão", DragonEssencePages);
        public static readonly DialogueDefinition FreedDragonEssence = new DialogueDefinition("dragon-essence-freed", DialogueSpeaker.DragonEssence, "Essência liberta", DragonEssencePages);

        public static readonly DialogueDefinition Inn = DialogueDefinition.Choice("inn-services", DialogueSpeaker.Innkeeper, "Hospedeira", "Bem-vindos à pousada. O que desejam?",
            new[] { "Salvar", "Passar a noite" });

        public static readonly DialogueDefinition PhoenixReceived = DialogueDefinition.Notice("phoenix-received", "Recebeu Phoenix");
        public static readonly DialogueDefinition DraconicArmorReceived = DialogueDefinition.Notice("draconic-armor-received", "Recebeu Armadura Dracônica");
    }
}
