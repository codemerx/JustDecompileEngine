using System.Collections.Generic;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV3 : CSharpV2
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV3()
        {
            CSharpV3.languageSpecificGlobalKeywords = new HashSet<string>(CSharpV2.languageSpecificGlobalKeywords);
            CSharpV3.languageSpecificContextualKeywords = new HashSet<string>(CSharpV2.languageSpecificContextualKeywords);
        }

        public override int Version
        {
            get
            {
                return 3;
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV3.languageSpecificGlobalKeywords, CSharpV3.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV3.languageSpecificGlobalKeywords);
        }
    }
}
