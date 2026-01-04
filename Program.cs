using System.Reflection.Emit;
using System.Runtime.InteropServices;
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
            public override string ToString() => text;
        }
        internal struct KeywordToken : Token
        {
            public string text { get; }
            public KeywordToken(string text) { this.text = text; }
            public override string ToString() => text;
        }
        internal struct OperationToken : Token
        {
            public string text { get; }
            public OperationToken(string text) { this.text = text; }
            public override string ToString() => text;
        }
        internal struct ValueToken : Token
        {
            public string text { get; }
            public ValueToken(string text) { this.text = text; }
            public override string ToString() => text;
        }
        #endregion






        public static void Compile(string code)
        {
            List<List<string>> textSegments = LexicalAnalysis(code);
            Token[][] tokenSequences = SyntaxicalAnalysis(textSegments);
            string outputCode = CodeGeneration(tokenSequences);

            //Console.WriteLine($"Code: \n\n{GetNumberedCode(outputCode)}");
            Console.WriteLine($"Code: \n\n{(outputCode)}");
        }

        private static string GetNumberedCode(string initialCode)
        {
            string[] lines = initialCode.Split('\n')[..^1];
            int maxNumLength = (lines.Length - 1).ToString().Length;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                builder.AppendLine($"{(i).ToString().PadRight(maxNumLength)}. {line}");
            }
            return builder.ToString();
        }


        #region Lexical Constants
        private static readonly Regex lexicalRegex = new Regex(@"((?:"".*?"")|(?:[a-zA-Z]\w*[;: ()]?)|(?:[=<>\.\+\-/\*\^]+ ?)|(?:\d+[;: ()]?))", RegexOptions.Compiled);
        private static readonly char[] tokenEndings = new char[] { ';', ':', ')' };
        const bool displaySequences = false;
        #endregion
        private static List<List<string>> LexicalAnalysis(string code)
        {
            
            MatchCollection matches = lexicalRegex.Matches(code);
            string[] tokens = (from Match match in matches
                               select match.ToString()).ToArray();

            List<List<string>> sequences = new List<List<string>>();
            List<string> currentSequence = new List<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                string currentToken = tokens[i].Trim();

                
                if (tokenEndings.Any(currentToken.EndsWith)) // if the current token ends with any of the endings, means end of sequence
                {
                    currentSequence.Add(currentToken[..^1]);
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
                    continue;
                }
                currentSequence.Add(currentToken);
            }

            return sequences;
        }



        #region Syntaxical Constants
        private static readonly string[] keywords = new string[] { "if", "endif", "def", "enddef", "for", "endfor", "while", "endwhile", "print(" };
        private static readonly string[] operators = new string[] { "<", "=", "==", ">", "<=", ">=", ".", "+", "-", "*", "/", "^" };
        private static readonly Regex variableMatcher = new Regex(@"[a-zA-Z]\w*[;: ()]*", RegexOptions.Compiled);
        private static readonly Regex valueMatcher = new Regex(@"\d+", RegexOptions.Compiled);
        const bool displayTokenTypes = true;
        #endregion
        private static Token[][] SyntaxicalAnalysis(List<List<string>> textSegments)
        {
            Token[][] tokens = new Token[textSegments.Count()][];

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
                    else if (valueMatcher.IsMatch(segment))
                    {
                        tokens[i][j] = new ValueToken(segment);
                    }
                    else
                    {
                        throw new InvalidDataException($"The code segment is not in correct syntax. Segment : {{{segment}}}");
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

            Stack<int> ifTargetStack = new Stack<int>();
            Stack<int> forTargetStack = new Stack<int>();

            /*
            
            So each sequence should start with either a keyword or a variable.
            if its a variable -> should either be a . or a = after

            */
            foreach (Token[] section in tokens)
            {
                if (section.Length == 0) { throw new Exception("Invalid quantity of tokens."); }

                if (section[0] is VariableToken)
                {
                    // next should be an operator
                    // different operators are  "<", "=", "==", ">", "<=", ">=", "."
                    // lets do equals first, means an assignment

                    if (section[1].text == "=")
                    {
                        // assignment
                        // means it needs a variable token
                        if (section.Length <= 2) { throw new Exception("Invalid quantity of tokens in variable assignment."); }
                        // next should be a variable, there may be an operator and then another variable, and that may loop forever
                        if (section[2] is not VariableToken && section[2] is not ValueToken) { throw new Exception("Invalid token syntax"); }


                        if (section.Length % 2 != 1) { throw new Exception("Invalid quantity of tokens in variable assignment."); }

                        if (section.Length == 3) { outputCode.Add($"set {section[0].text} {section[2].text}"); }
                        else 
                        {
                            outputCode.Add(GetOperandText(section[0], section[2], section[4], section[3])); 
                        }

                        if (section.Length <= 5) { continue; }
                        for (int i = 5; i < section.Length; i += 2)
                        {
                            // in format a = b + c - d / e - f + g;
                            // goes to
                            /*

                            a = b + c
                            a = a - d
                            a = a / e
                            a = a - f
                            a = a + g

                            */

                            // op format : op add output in1 in2
                            outputCode.Add(GetOperandText(section[0], section[0], section[i + 1], section[i]));
                        }
                    }
                }

                if (section[0] is KeywordToken)
                {
                    if (section[0].text == "if")
                    {
                        // should then go if variable/value operator variable/value
                        if (section.Length != 4) { throw new Exception("Invalid if statement syntax. Required : {if value operator value}"); }

                        

                        if (!arithmeticOperands.ContainsKey(section[2].text)) { throw new Exception($"Operator {{{section[2].text}}} not recognised."); }

                        outputCode.Add($"jump {outputCode.Count + 2} {arithmeticOperands[section[2].text]} {section[1].text} {section[3].text}");

                        ifTargetStack.Push(outputCode.Count); // will target this jump line
                        outputCode.Add("jump - always");
                    }
                    if (section[0].text == "endif")
                    {
                        int target = ifTargetStack.Pop();
                        outputCode[target] = outputCode[target].Replace("-", outputCode.Count.ToString());
                    }

                    if (section[0].text == "for")
                    {
                        outputCode.Add($"set {section[1]} {section[3]}");
                        outputCode.Add($"jump {outputCode.Count + 2} always");
                        if (section.Length > 8)
                        {
                            outputCode.Add(GetOperandText(section[9], section[11], section[13], section[12]));
                        }
                        else
                        {
                            outputCode.Add($"op add {section[1]} {section[1]} 1");
                        }
                        outputCode.Add(GetOperandText(new VariableToken("compilerForLoopJumpChecker"), section[5], section[7], section[6]));
                        forTargetStack.Push(outputCode.Count);
                        outputCode.Add($"jump - equal compilerForLoopJumpChecker false");
                    }
                    if (section[0].text == "endfor")
                    {
                        int target = forTargetStack.Pop();
                        outputCode[target] = outputCode[target].Replace("-", (outputCode.Count + 1).ToString());
                        outputCode.Add($"jump {target - 2} always");
                    }

                    if (section[0].text == "print(")
                    {
                        outputCode.Add($"print {section[1]}");
                        outputCode.Add($"printflush {section[2]}");
                    }
                }
            }


            StringBuilder builder = new StringBuilder();
            foreach (string line in outputCode)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine("end");
            return builder.ToString();
        }


        private static readonly Dictionary<string, string> arithmeticOperands = new Dictionary<string, string>()
        {
            { "==", "equals" },
            { "<", "lessThan" },
            { ">", "greaterThan" },
            { "+", "add" },
            { "-", "sub" },
            { "*", "mul" },
            { "/", "div" },
            { "^", "pow" },
            { ">=", "greaterThanEq" },
            { "<=", "lessThanEq" },
        };
        private static string GetOperandText(Token output, Token in1, Token in2, Token opToken)
        {
            if (opToken is not OperationToken operand) { throw new Exception($"Invalid operator {opToken}"); }
            if (!arithmeticOperands.ContainsKey(operand.text) && operand.text != ".") { throw new Exception($"Operand {operand.text} not supported."); }


            if (arithmeticOperands.ContainsKey(operand.text))
            {
                return $"op {arithmeticOperands[operand.text]} {output.text} {in1.text} {in2.text}";
            }
            else if (operand.text == ".")
            {
                return $"sensor {output.text} {in1.text} {in2.text}";
            }
            else
            {
                throw new Exception($"Operand {operand} is not recognised");
            }
        }













        private static void Main()
        {
            string code;
            if (File.Exists("code.txt")) { code = File.ReadAllText("code.txt"); }
            else { code = File.ReadAllText("..\\..\\..\\code.txt"); }
            Compile(code);
        }


    }
}