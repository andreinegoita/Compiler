using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

class MiniLangProcessor
{
    public static void Main(string[] args)
    {
        string errorLogPath = "errors.txt";
        List<string> errorMessages = new List<string>();

        if (!File.Exists("program.mini"))
        {
            Console.WriteLine("Eroare: Fișierul program.mini nu exista.");
            return;
        }

        string sourceProgram = File.ReadAllText("program.mini");


        AntlrInputStream inputStream = new AntlrInputStream(sourceProgram);
        MiniLangLexer lexer = new MiniLangLexer(inputStream);
        CommonTokenStream tokenStream = new CommonTokenStream(lexer);
        MiniLangParser parser = new MiniLangParser(tokenStream);

        using (StreamWriter tokenWriter = new StreamWriter("tokens.txt"))
        {
            foreach (var token in tokenStream.GetTokens())
            {
                string tokenType = lexer.Vocabulary.GetSymbolicName(token.Type);
                string lexeme = token.Text;
                int line = token.Line;

                // Format: <token, lexeme, line number>
                tokenWriter.WriteLine($"<{tokenType}, {lexeme}, {line}>");
            }
        }

        
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new MiniLangErrorListener(errorMessages)); 

       
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new MiniLangErrorListenerLexer(errorMessages));


        IParseTree tree = parser.program();

        Console.WriteLine("Erori si avertismente: ");
        foreach (var error in errorMessages)
        {
            Console.WriteLine(error);
        }

        try
        {
            File.WriteAllLines(errorLogPath, errorMessages);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare la salvarea fisierului: {ex.Message}");
        }
        MiniLangVisitor visitor = new MiniLangVisitor(lexer);

        visitor.Visit(tree);

        File.WriteAllText("tokens.txt", visitor.GetTokens());
        File.WriteAllText("globalVariables.txt", visitor.GetGlobalVariables());
        File.WriteAllText("functions.txt", visitor.GetFunctions());
        File.WriteAllText("localVariables.txt", visitor.GetLocalVariables());
        File.WriteAllText("controlStructures.txt", visitor.GetControlStructures());
    }
}

class MiniLangErrorListener : IAntlrErrorListener<int>
{
    private List<string> _errorMessages;
    public MiniLangErrorListener(List<string> errorMessages)
    {
        _errorMessages = errorMessages;
    }
    
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int line, int charPositionInLine, string msg, RecognitionException e)
    {
       
        if (msg.Contains("illegal character"))
        {
            _errorMessages.Add($"[Lexical Error] Line {line}, Position {charPositionInLine}: {msg}");
        }
        else
        {
            _errorMessages.Add($"[Syntax Error] Line {line}, Position {charPositionInLine}: {msg}");
        }
    }

    
    void IAntlrErrorListener<int>.SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
       
        _errorMessages.Add($"[Detailed Syntax Error] Line {line}, Position {charPositionInLine}: {msg} (Offending symbol: {offendingSymbol})");
    }
}

public class MiniLangErrorListenerLexer : IAntlrErrorListener<IToken>
{
    private List<string> _errorMessages;
    public MiniLangErrorListenerLexer(List<string> errorMessages)
    {
        _errorMessages = errorMessages;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        
        _errorMessages.Add($"[Syntax Error] Line {line}, Position {charPositionInLine}: {msg}");
    }

    public void SyntaxErrorr(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        
        _errorMessages.Add($"[Detailed Syntax Error] Line {line}, Position {charPositionInLine}: {msg} (Offending symbol: {offendingSymbol.Text})");
    }
}




class MiniLangVisitor : MiniLangBaseVisitor<object>
{
    private string tokens = "";
    private string globalVariables = "";
    private string functions = "";
    private string localVariables = "";
    private string controlStructures = "";

    private HashSet<string> globalVariableNames = new HashSet<string>();
    private HashSet<string> functionSignatures = new HashSet<string>();
    private HashSet<string> localVariableNames = new HashSet<string>();
    private MiniLangLexer lexer;  

    public MiniLangVisitor(MiniLangLexer lexer)
    {
        this.lexer = lexer;
    }


    public override object VisitTerminal(ITerminalNode node)
    {
        var token = node.Symbol;
        string tokenType = lexer.Vocabulary.GetSymbolicName(token.Type); 
        string lexeme = token.Text;
        int line = token.Line;

        
       tokens += $"<{tokenType}, {lexeme}, {line}>\n";


        return base.VisitTerminal(node);
    }




    public override object VisitVariableDeclaration(MiniLangParser.VariableDeclarationContext context)
    {
        string variableType = context.KEYWORD()?.GetText() ?? "unknown";
        string variableName = context.IDENTIFIER()?.GetText() ?? "unknown";
        string initializationValue = context.expression()?.GetText() ?? "";

        var parentContext = context.Parent;

        if (parentContext is MiniLangParser.ProgramContext || parentContext is MiniLangParser.GlobalVariableDeclarationContext)
        {
            if (!globalVariableNames.Add(variableName))
            {
                Console.WriteLine($"[Semantic Error] Global variable '{variableName}' is already defined.");
                return base.VisitVariableDeclaration(context);
            }

            globalVariables += $"{variableType} {variableName}";
            if (!string.IsNullOrEmpty(initializationValue))
            {
                globalVariables += $" = {initializationValue}";
            }
            globalVariables += "\n";
        }
        else if (parentContext is MiniLangParser.LocalVariableDeclarationContext || parentContext is MiniLangParser.BlockContext)
        {
            if (!localVariableNames.Add(variableName))
            {
                Console.WriteLine($"[Semantic Error] Local variable '{variableName}' is already defined in this block.");
                return base.VisitVariableDeclaration(context);
            }

            localVariables += $"{variableType} {variableName}";
            if (!string.IsNullOrEmpty(initializationValue))
            {
                localVariables += $" = {initializationValue}";
            }
            localVariables += "\n";
        }

        if (context.expression() != null)
        {
            if ((variableType == "int" && !int.TryParse(initializationValue, out _)) ||
                (variableType == "double" && !double.TryParse(initializationValue, out _)) ||
                (variableType == "string" && !initializationValue.StartsWith("\"")))
            {
                Console.WriteLine($"[Semantic Error] Incompatible type for variable '{variableName}'.");
            }
        }

        return base.VisitVariableDeclaration(context);
    }



    public override object VisitFunctionDeclaration(MiniLangParser.FunctionDeclarationContext context)
    {
        string returnType = context.KEYWORD().GetText();
        string functionName = context.IDENTIFIER().GetText();
        string functionType = functionName == "main" ? "main" : "iterative";

        if (context.block().GetText().Contains(functionName))
        {
            functionType = "recursive";
        }

        string parameters = context.parameterList()?.GetText() ?? "";
        if (context.parameterList() != null)
        {
            foreach (var param in context.parameterList().parameter())
            {
                string paramType = param.KEYWORD().GetText();
                string paramName = param.IDENTIFIER().GetText();
                if (localVariableNames.Contains(paramName))
                {
                    Console.WriteLine($"[Semantic Error] Parameter '{paramName}' conflicts with a local variable.");
                }
                parameters += $"{paramType} {paramName}, ";
            }
            parameters = parameters.TrimEnd(',', ' ');
        }

        string signature = $"{functionName}({parameters})";
        if (functionSignatures.Contains(signature))
        {
            Console.WriteLine($"[Semantic Error] Function '{functionName}' with parameters '{parameters}' is already defined.");
        }
        else
        {
            functionSignatures.Add(signature);
            functions += $"Function: {functionName}\nParameters: {parameters}\n\n";
        }

        string localVars = "";
        foreach (var statement in context.block().statement())
        {
            if (statement.variableDeclaration() != null)
            {
                string localVarType = statement.variableDeclaration().KEYWORD().GetText();
                string localVarName = statement.variableDeclaration().IDENTIFIER().GetText();
                string localVarValue = statement.variableDeclaration().expression()?.GetText() ?? "null";
                localVars += $"{localVarType} {localVarName} = {localVarValue}\n";
            }
        }

        string controlStructs = "";
        foreach (var statement in context.block().statement())
        {
            if (statement.controlStructure() != null)
            {
                controlStructs += VisitControlStructure(statement.controlStructure()) as string;
            }
        }

        functions += $"Function: {functionName}\n";
        functions += $"Type: {functionType}\n";
        functions += $"Return Type: {returnType}\n";
        functions += $"Parameters: {parameters}\n";
        functions += $"Local Variables:\n{localVars}\n";
        functions += $"Control Structures:\n{controlStructs}\n\n";

        foreach (var param in context.parameterList()?.parameter() ?? Array.Empty<MiniLangParser.ParameterContext>())
        {
            string paramName = param.IDENTIFIER().GetText();
            if (localVariableNames.Contains(paramName))
            {
                Console.WriteLine($"[Semantic Error] Parameter '{paramName}' conflicts with a local variable.");
            }
        }


        return base.VisitFunctionDeclaration(context);
    }

    public override object VisitBlock(MiniLangParser.BlockContext context)
    {
        localVariableNames.Clear();
        foreach (var statement in context.statement())
        {
            if (statement.variableDeclaration() != null)
            {
                string variableName = statement.variableDeclaration().IDENTIFIER().GetText();
                if (!localVariableNames.Add(variableName))
                {
                    Console.WriteLine($"[Semantic Error] Local variable '{variableName}' is already defined.");
                }
            }
        }
        return base.VisitBlock(context);
    }

    public override object VisitControlStructure(MiniLangParser.ControlStructureContext context)
    {
        string controlStructureDetails = "";

        if (context.ifStatement() != null)
        {
            controlStructureDetails = "If statement: " + context.ifStatement().GetText();
        }
        else if (context.whileStatement() != null)
        {
            controlStructureDetails = "While statement: " + context.whileStatement().GetText();
        }
        else if (context.forStatement() != null)
        {
            controlStructureDetails = "For statement: " + context.forStatement().GetText();
        }

        controlStructures += controlStructureDetails + "\n";

        return controlStructures;
    }

    public override object VisitFunctionCall(MiniLangParser.FunctionCallContext context)
    {
        string functionName = context.IDENTIFIER().GetText();
        string arguments = context.argumentList()?.GetText() ?? "";

        string signature = $"{functionName}({arguments})";
        if (functionSignatures.Contains(signature))
        {
            Console.WriteLine($"[Semantic Error] Function '{functionName}' with arguments '{arguments}' is not defined.");
        }

        return base.VisitFunctionCall(context);
    }






    public string GetTokens() => tokens;
    public string GetGlobalVariables() => globalVariables;
    public string GetFunctions() => functions;
    public string GetLocalVariables() => localVariables;
    public string GetControlStructures() => controlStructures;
}
