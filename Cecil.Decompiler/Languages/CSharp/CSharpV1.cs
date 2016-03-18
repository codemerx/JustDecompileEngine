using Mono.Cecil;
using System.Collections.Generic;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Decompiler.GotoElimination;
using Telerik.JustDecompiler.Steps;
using Telerik.JustDecompiler.Steps.SwitchByString;

namespace Telerik.JustDecompiler.Languages.CSharp
{
    public class CSharpV1 : CSharp
    {
        new protected static HashSet<string> languageSpecificGlobalKeywords;
        new protected static HashSet<string> languageSpecificContextualKeywords;

        static CSharpV1()
        {
            CSharpV1.languageSpecificGlobalKeywords = new HashSet<string>(CSharp.languageSpecificGlobalKeywords);

            //list taken from http://msdn.microsoft.com/en-us/library/x53a06bb.aspx -> MSDN list of C# keywords

            string[] GlobalKeywords =
            {
                "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal",
                "default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float",
                "for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new",
                "null","object","operator","out","override","params","private","protected","public","readonly","ref","return",
                "sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch","this","throw","true","try",
                "typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while"
            };
            foreach (string word in GlobalKeywords)
            {
                CSharpV1.languageSpecificGlobalKeywords.Add(word);
            }

            CSharpV1.languageSpecificContextualKeywords = new HashSet<string>(CSharp.languageSpecificContextualKeywords);

            //list taken from http://msdn.microsoft.com/en-us/library/x53a06bb.aspx -> MSDN list of C# contextual keywords

            string[] contextualKeywords =
            {
                "add","alias","ascending","descending","dynamic","from","get","global","group","into","join","let","orderby",
                "partial","remove","select","set","value","var","where","yield"
            };
            foreach (string word in contextualKeywords)
            {
                CSharpV1.languageSpecificContextualKeywords.Add(word);
            }
        }

        public override string Name
        {
            get
            {
                return "C#1";
            }
        }

        public override bool IsLanguageKeyword(string word)
        {
            return base.IsLanguageKeyword(word, CSharpV1.languageSpecificGlobalKeywords, CSharpV1.languageSpecificContextualKeywords);
        }

        public override bool IsGlobalKeyword(string word)
        {
            return IsGlobalKeyword(word, CSharpV1.languageSpecificGlobalKeywords);
        }
    }
}
