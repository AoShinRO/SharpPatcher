namespace SharpPatcher
{
    public static class Config
    {

        public static readonly string patchUrl = "http://127.0.0.1/kpatcher";
        public static readonly string patchArchive = "patch_notes.txt"; //srv side archive
        public static readonly string mainFilePath = "server.grf";
        public static readonly string indicesFilePath = "patch.ini"; //client side archive
        public static readonly string weburlPath = "links.ini"; //client side archive

        #region Links
        public static readonly string DiscordLink = "https://discord.com";
        public static readonly string FacebookLink = "https://facebook.com"; 
        public static readonly string NewsLink = "https://google.com";
        public static readonly string RegisterLink = "https://seuro.com/register"; 
        #endregion Links


    }
}
