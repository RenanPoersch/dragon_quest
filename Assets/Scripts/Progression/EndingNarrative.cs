namespace DragonQuest.Progression
{
    public static class EndingNarrative
    {
        public const string KingFreedSpeech =
            "Meu povo,\nhoje volto a falar\ncom minha própria voz.\n\n"
            + "Aren, Lia e Bram\nnos devolveram o amanhã.\n\n"
            + "Nenhuma palavra pode\napagar o sofrimento\nque houve, mas eu\nprometo: vamos reparar\nessa dor, juntos.\n\n"
            + "Os celeiros serão\nabertos,\nrestauraremos nossas\ncasas e cuidaremos\nde nossa terra.\n\n"
            + "Que estas portas\npermaneçam abertas.\nQue o pão alcance\ntodas as mesas.\n\n"
            + "Pois a coroa\nsó tem valor\nquando serve ao povo.";
        public const string KingFreedEpilogue =
            "O rei cumpriu a promessa.\n\n"
            + "Os celeiros se encheram.\nA feira voltou às ruas.\nOficinas abriram as portas.\n\n"
            + "Nos campos, a colheita\nalimentou a ilha inteira.\nNo castelo, o povo\nencontrou abrigo e escuta.\n\n"
            + "Aren, Lia e Bram\nseguiram seu caminho.\nDeixaram para trás\num reino livre.\n\n"
            + "A chama ancestral\npermaneceu acesa.\nE, desta vez,\niluminava dias de paz.";
        public const string HeroPossessedEpilogue =
            "O Rei Rubro caiu.\nPor um instante,\na ilha pareceu livre.\n\n"
            + "Mas uma sombra\nmisteriosa deixou\no corpo do velho rei...\ne encontrou\num novo hospedeiro.\n\n"
            + "Agora, seus passos já\nnão lhe pertencem mais.\n\n"
            + "Do alto do castelo,\nos olhos guardam\napenas um brilho vazio.\n\n"
            + "Nenhuma resposta.\nSó o silêncio\nocupa o salão.\n\n"
            + "Um vulto revela\na presença que reina.\n\n"
            + "O trono tem outro rei.\nA ilha, o mesmo senhor.";
        public const string CastleDestroyedEpilogue =
            "O meteoro rompeu o céu.\nNada pôde deter\na sua queda.\n\n"
            + "Onde antes se erguia\no castelo, restam cinzas\ne pedras sobre o mar.\n\n"
            + "O vento leva os nomes\nde Aren, Lia e Bram.\nA ilha guarda o silêncio.\n\n"
            + "Longe das muralhas,\num velho contempla\no que o fogo deixou.\nAinda na esperança\nde dias melhores.\n\n"
            + "E apenas com a lembrança\nda lenda e o desejo\nde encontrar quem\nvolte a contá-la.";
        public static string Title(GameEnding ending) => ending==GameEnding.KingFreed?"FINAL BOM / O REI LIBERTADO"
            :ending==GameEnding.CastleDestroyed?"FINAL RUIM / CINZAS DO CASTELO":"TERCEIRO FINAL / O NOVO HOSPEDEIRO";
    }
}
