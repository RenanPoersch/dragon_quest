namespace DragonQuest.Content
{
    public static class DialogueCatalog
    {
        public static readonly DialogueDefinition Citizen = new DialogueDefinition("city-citizen", "Aldeão",
                "Ah, então vocês são os aventureiros que vieram do sul? Devem estar cansados da viagem.",
                "Podem dormir e se recuperar na pousada. E, se precisarem de mantimentos, deem uma olhada nas lojas da cidade.",
                "Não temos muita coisa por aqui, mas deve ser o suficiente.");

        public static readonly DialogueDefinition Traveler = new DialogueDefinition("city-traveler", "Viajante",
                "Encontrei marcas incomuns numa pedra ao norte. Não parecem ter sido feitas por nenhum animal que eu conheça.",
                "Havia esse objeto vermelho perto dela...");

        public static readonly DialogueDefinition Explorer = new DialogueDefinition("city-explorer", "Exploradora", "Cavar, cavar... Explorar, explorar...");

        public static readonly DialogueDefinition QuarryKeeper = new DialogueDefinition("quarry-keeper", "Guardião da pedreira",
                "Acho que existe algum mecanismo nessas pedras. Os símbolos, o jeito como estão dispostas... Mas não tenho certeza. Pode ser só coisa da minha cabeça.",
                "Volto aqui sempre que preciso pensar. Fico olhando para elas e imaginando quantas coisas neste mundo a gente ainda não entende... Às vezes esqueço até de ir embora.");

        public static readonly DialogueDefinition Gardener = new DialogueDefinition("forest-gardener", "Jardineiro",
                "Já houve um tempo em que estes jardins viviam cheios. As crianças vinham buscar flores... Hoje eu cuido deste cantinho. É o que as costas ainda permitem.",
                "Perdi minha casa, enterrei amigos... Até fome eu passei. Tem manhã em que a gente acorda e pensa se ainda vale a pena levantar.",
                "Mas aí eu venho aqui e encontro uma folha nova, uma flor que abriu durante a noite... E já começo a pensar no que vou plantar amanhã.",
                "Eu ainda quero ver este jardim florir de novo. Tem de haver dias melhores... Até para um velho como eu.");

        public static readonly DialogueDefinition KingConfrontation = new DialogueDefinition("king-confrontation", "Rei Rubro",
            "Todo caminho nesta ilha termina diante do meu trono. Curvem-se, ou o próprio céu cairá sobre vocês!");

        public static readonly DialogueDefinition BridgeGuards = new DialogueDefinition("bridge-guards", "Guarda Real", "Alto! Ninguém atravessa esta ponte. Por ordem do Rei Rubro, vocês não passarão! Preparem-se para lutar!");
    }
}
