namespace DragonQuest.Progression
{
    public static class EndingNarrative
    {
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
                case GameEnding.CastleDestroyed: return new[]{
                    "O meteoro flamejante atravessa o ceu. Nenhum portador vivo da armadura consegue deter sua queda.",
                    "A explosao consome o trono, o tirano e a equipe. As torres desabam; do castelo restam apenas pedras queimadas.",
                    "O silencio cobre a ilha. A jornada termina entre as cinzas, e a antiga lenda nao encontra quem possa conta-la."};
                case GameEnding.HeroPossessed: return new[]{
                    "O tirano cai. Por um instante, Aren acredita ter encerrado o dominio sobre a ilha. Entao uma sombra abandona o corpo do rei.",
                    "A presenca encontra seu novo hospedeiro em Aren. Seus olhos ganham um brilho violeta, e uma segunda voz sussurra: Um trono vazio sempre encontra um novo rei.",
                    "Lia e Bram chamam pelo amigo, mas quem se volta para eles ja nao e apenas Aren. A tirania mudou de rosto. A ilha continua prisioneira."};
                default: return new string[0];
            }
        }
    }
}
