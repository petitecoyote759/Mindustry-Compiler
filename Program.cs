using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Mindustry_Compiler
{
    public static class Compiler
    {
        #region Token Structs
        internal interface Token
        {
            internal string text { get; }
        }

        internal struct VariableToken : Token
        {
            public string text { get; }
            public VariableToken(string text) { this.text = text; }
        }
        internal struct KeywordToken : Token
        {
            public string text { get; }
            public KeywordToken(string text) { this.text = text; }
        }
        internal struct OperationToken : Token
        {
            public string text { get; }
            public OperationToken(string text) { this.text = text; }
        }
        #endregion






        public static void Compile(string code)
        {
            List<List<string>> textSegments = LexicalAnalysis(code);
            Token[][] tokenSequences = SyntaxicalAnalysis(textSegments);
            string outputCode = CodeGeneration(tokenSequences);

            Console.WriteLine($"Code: \n\n{outputCode}");
        }


        #region Lexical Constants
        private static readonly Regex lexicalRegex = new Regex(@"((?:"".*?"")|(?:[a-zA-Z]\w*[;: ()]*)|(?:[=<>\.]+ ?))", RegexOptions.Compiled);
        private static readonly char[] tokenEndings = new char[] { ';', ':' };
        const bool displaySequences = false;
        #endregion
        private static List<List<string>> LexicalAnalysis(string code)
        {
            
            Match[] matches = lexicalRegex.Matches(code).ToArray();
            string[] tokens = (from Match match in matches
                               select match.ToString()).ToArray();

            List<List<string>> sequences = new List<List<string>>();
            List<string> currentSequence = new List<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                string currentToken = tokens[i].Trim();

                currentSequence.Add(currentToken);
                if (tokenEndings.Any(currentToken.EndsWith)) // if the current token ends with any of the endings, means end of sequence
                {
                    if (displaySequences)
                    {
                        Console.WriteLine("Sequence: \n");
                        foreach (string token in currentSequence)
                        {
                            Console.WriteLine($"{{{token}}}");
                        }
                        Console.WriteLine("\n\n");
                    }
                    sequences.Add(currentSequence);
                    currentSequence = new List<string>();
                }
            }

            return sequences;
        }



        #region Syntaxical Constants
        private static readonly string[] keywords = new string[] { "if", "def", "print(" };
        private static readonly string[] operators = new string[] { "<", "=", "==", ">", "<=", ">=", "." };
        private static readonly Regex variableMatcher = new Regex(@"[a-zA-Z]\w*[;: ()]*", RegexOptions.Compiled);
        const bool displayTokenTypes = true;
        #endregion
        private static Token[][] SyntaxicalAnalysis(List<List<string>> textSegments)
        {
            Token[][] tokens = new Token[10][];

            for (int i = 0; i < textSegments.Count; i++)
            {
                List<string> sequence = textSegments[i];
                tokens[i] = new Token[sequence.Count];


                if (displayTokenTypes) { Console.WriteLine("Sequence:\n"); }

                for (int j = 0; j < sequence.Count; j++)
                {
                    string segment = sequence[j];
                    if (keywords.Any((string keyword) => keyword == segment))
                    {
                        tokens[i][j] = new KeywordToken(segment);
                    }
                    else if (operators.Any((string operatorText) => operatorText == segment))
                    {
                        tokens[i][j] = new OperationToken(segment);
                    }
                    else if (variableMatcher.IsMatch(segment))
                    {
                        tokens[i][j] = new VariableToken(segment);
                    }
                    else
                    {
                        throw new InvalidDataException($"The code segment is not in correct syntax. Segment : {segment}");
                    }


                    if (displayTokenTypes) { Console.WriteLine($"Token : {(tokens[i][j]).GetType().ToString().Split('+').Last()} - {{{tokens[i][j].text}}}"); }
                }

                if (displayTokenTypes) { Console.WriteLine("\n\n"); }
            }



            return tokens;
        }



        #region Code Generation Constant

        #endregion
        private static string CodeGeneration(Token[][] tokens)
        {
            List<string> outputCode = new List<string>();

            /*
            
            So each sequence should start with either a keyword or a variable.
            if its a variable -> should either be a . or a = after

            */

            foreach (Token[] section in tokens)
            {

            }







            StringBuilder builder = new StringBuilder();
            foreach (string line in outputCode)
            {
                builder.AppendLine(line);
            }
            return builder.ToString();
        }







        private static void Main()
        {
            Compile("""
                
                powerIn = battery1.powerNetIn;
                powerOut = battery1.powerNetOut;
                if powerIn < powerOut:
                   	diode.enabled = false;

                def printTest:
                   	print("frog", message1);
                
                """);
        }


    }
}