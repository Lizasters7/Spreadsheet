grammar LabCalculator;

/*
 * Parser Rules
 */

compileUnit : expression EOF;

expression :
    LPAREN expression RPAREN                                  #ParenthesizedExpr
  | operatorToken=(INC | DEC) LPAREN expression RPAREN        #IncDecExpr
  | operatorToken=(ADD | SUBTRACT) expression                 #UnaryExpr
  | expression operatorToken=(MULTIPLY | DIVIDE) expression   #MultiplicativeExpr
  | expression operatorToken=(ADD | SUBTRACT) expression      #AdditiveExpr
  | expression operatorToken=(EQUAL | NOT_EQUAL | LESS | GREATER | LESS_EQUAL | GREATER_EQUAL) expression  #ComparisonExpr
  | NUMBER                                                    #NumberExpr
  | IDENTIFIER                                                #IdentifierExpr
  | TRUE                                                      #TrueExpr
  | FALSE                                                     #FalseExpr
  ;

/*
 * Lexer Rules
 */

NUMBER : INT ('.' INT)?;
INC : 'inc';
DEC : 'dec';
TRUE : 'true';
FALSE : 'false';
IDENTIFIER : [a-zA-Z]+[1-9][0-9]*;
INT : ('0'..'9')+;
MULTIPLY : '*';
DIVIDE : '/';
SUBTRACT : '-';
ADD : '+';
LPAREN : '(';
RPAREN : ')';
EQUAL         : '=';
NOT_EQUAL     : '<>';
LESS          : '<';
GREATER       : '>';
LESS_EQUAL    : '<=';
GREATER_EQUAL : '>=';
WS : [ \t\r\n] -> channel(HIDDEN);