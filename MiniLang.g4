grammar MiniLang;

// Unitatea de compilare
program: (globalVariableDeclaration | functionDeclaration)+ EOF;

// Declarații globale
globalVariableDeclaration: variableDeclaration;

// Declarații de variabile
variableDeclaration: KEYWORD IDENTIFIER ('=' expression)? ';';

// Definiții de funcții
functionDeclaration:
	KEYWORD IDENTIFIER '(' parameterList? ')' block;

// Liste de parametri
parameterList: parameter (',' parameter)*;
parameter: KEYWORD IDENTIFIER;

// Blocuri de cod
block: '{' localVariableDeclaration* statement* '}';

// Declarații locale de variabile (în interiorul funcțiilor)
localVariableDeclaration: variableDeclaration;

// Declarații generale
statement:
	variableDeclaration
	| expression ';'
	| controlStructure
	| RETURN expression? ';';

// Structuri de control
controlStructure: ifStatement | whileStatement | forStatement;

ifStatement: 'if' '(' expression ')' block ('else' block)?;
whileStatement: 'while' '(' expression ')' block;
forStatement:
	'for' '(' variableDeclaration? expression? ';' expression? ')' block;

// Expresii
expression:
	IDENTIFIER
	| NUMBER
	| STRING
	| expression operator = ('*' | '/' | '+' | '-') expression
	| IDENTIFIER '=' expression
	| IDENTIFIER '++'
	| '++' IDENTIFIER
	| IDENTIFIER '--'
	| '--' IDENTIFIER
	| IDENTIFIER '+=' expression
	| IDENTIFIER '-=' expression
	| IDENTIFIER '*=' expression
	| IDENTIFIER '/=' expression
	| IDENTIFIER '%=' expression
	| '(' expression ')'
	| NOT expression
	| expression ('==' | '!=' | '<' | '<=' | '>' | '>=') expression
	| expression ('&&' | '||') expression
	| functionCall;

functionCall: IDENTIFIER '(' argumentList? ')';
argumentList: expression (',' expression)*;

// Tipuri de date
KEYWORD: 'int' | 'float' | 'double' | 'string' | 'void';

// Operatori și simboluri
NOT: '!';
RETURN: 'return';
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
NUMBER: [0-9]+ ('.' [0-9]+)?;
STRING: '"' .*? '"';
COMMENT: '//' ~[\r\n]* -> skip;
COMMENT_BLOCK: '/*' .*? '*/' -> skip;
WS: [ \r\n\t]+ -> skip;