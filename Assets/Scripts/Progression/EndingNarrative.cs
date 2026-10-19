namespace DragonQuest.Progression
{
    public static class EndingNarrative
    {
        public const string KingFreedSpeech =
            "Meu povo,\nhoje volto a falar\ncom minha própria voz.\n\n"
            + "A sombra caiu.\nAren, Lia e Bram\nnos devolveram o amanhã.\n\n"
            + "Nenhuma palavra apaga\no sofrimento que houve.\nQue meus atos comecem\na reparar essa dor.\n\n"
            + "Abriremos os celeiros,\nrestauraremos as casas\ne cuidaremos da terra.\n\n"
            + "Que estas portas\npermaneçam abertas.\nQue o pão alcance\ntodas as mesas.\n\n"
            + "A coroa só tem valor\nquando serve ao povo.";
        public const string KingFreedEpilogue =
            "O rei cumpriu a promessa.\n\n"
            + "Os celeiros se encheram.\nA feira voltou às ruas.\nOficinas abriram as portas.\n\n"
            + "Nos campos, a colheita\nalimentou a ilha inteira.\nNo castelo, o povo\nencontrou abrigo e escuta.\n\n"
            + "Aren, Lia e Bram\nseguiram seu caminho.\nDeixaram para trás\num reino livre.\n\n"
            + "A chama ancestral\npermaneceu acesa.\nE, desta vez,\niluminava dias de paz.";
        public const string HeroPossessedEpilogue =
            "O Rei Rubro caiu.\nPor um instante,\na ilha pareceu livre.\n\n"
            + "Mas a sombra deixou\no corpo do velho rei.\nE encontrou Aren.\n\n"
            + "Agora, seus passos\njá não lhe pertencem.\nO tapete conduz\nao mesmo trono.\n\n"
            + "Do alto do trono,\nos olhos guardam\num brilho violeta.\n\n"
            + "Lia e Bram chamam.\nNenhuma resposta.\nSó o silêncio\nocupa o salão.\n\n"
            + "Um relâmpago revela\na presença que reina\npor trás do novo rosto.\n\n"
            + "O trono tem outro rei.\nA ilha, o mesmo senhor.";
        public const string CastleDestroyedEpilogue =
            "O meteoro rompeu o céu.\nNenhuma armadura deteve\na sua queda.\n\n"
            + "O fogo consumiu o trono,\no Rei Rubro e os heróis.\nAs torres desabaram.\n\n"
            + "Onde antes se erguia\no castelo, restam cinzas\ne pedras sobre o mar.\n\n"
            + "Longe das muralhas,\num velho contempla\no que o fogo deixou.\n\n"
            + "O vento leva os nomes\nde Aren, Lia e Bram.\nA ilha guarda o silêncio.\n\n"
            + "E a lenda aguarda\nquem volte a contá-la.";
        public static string Title(GameEnding ending) => ending==GameEnding.KingFreed?"FINAL BOM / O REI LIBERTADO"
            :ending==GameEnding.CastleDestroyed?"FINAL RUIM / CINZAS DO CASTELO":"TERCEIRO FINAL / O NOVO HOSPEDEIRO";
        public static string[] Pages(GameEnding ending)
        {
            switch(ending)
            {
                case GameEnding.KingFreed: return new[]{
                    "A sombra se desfaz. O rei cai de joelhos, mas sua voz volta a ser apenas sua. A magia que preparava o meteoro desaparece.",
                    "Antes da possessao, ele era um rei justo. Agora livre, agradece a Aren, Lia e Bram e promete reparar o sofrimento causado pelo espirito.",
                    "O rei retoma seu governo. As portas do castelo se abrem aos moradores e a ilha recupera sua liberdade. A chama ancestral permanece acesa."};
                case GameEnding.CastleDestroyed:
                    string[] paragraphs = CastleDestroyedEpilogue.Replace("\n\n", "|").Replace('\n', ' ').Split('|');
                    return new[]{paragraphs[0]+" "+paragraphs[1], paragraphs[2]+" "+paragraphs[3], paragraphs[4]+" "+paragraphs[5]};
                case GameEnding.HeroPossessed: return new[]{
                    "O Rei Rubro cai. Por um instante, Aren acredita ter encerrado o dominio sobre a ilha. Entao uma sombra abandona o corpo do rei.",
                    "A presenca encontra seu novo hospedeiro em Aren. Seus olhos ganham um brilho violeta, e uma segunda voz sussurra: Um trono vazio sempre encontra um novo rei.",
                    "Lia e Bram chamam pelo amigo, mas quem se volta para eles ja nao e apenas Aren. A tirania mudou de rosto. A ilha continua prisioneira."};
                default: return new string[0];
            }
        }
    }
}
