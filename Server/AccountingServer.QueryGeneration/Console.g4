grammar Console;

@parser::members
{
	protected const int EOF = Eof;
}

@lexer::members
{
	protected const int EOF = Eof;
	protected const int HIDDEN = Hidden;
}

/*
 * Parser Rules
 */

command
	:	(vouchers | groupedQuery | asset | amort | otherCommand) EOF
	;

otherCommand
	:	Check | Titles | Launch | Connect | Shutdown | Backup | Mobile | Fetch | Help | Exit
	;

groupedQuery
	:	voucherDetailQuery subtotal
	;
	
chart
	:	'c::' NameString
	|	'c||' chartArea ('||' hyperItem)+
	|	'c:' NameString ':' chartArea ('||' hyperItem)+
	;

chartArea
	:	hyperItemAtom ('|' hyperItemAtom)* '|' ('_' Float)? ('^' Float)?
	;

report
	:	'r::' NameString
	|	'r||' hyperItem
	|	'r:' NameString ':' chartArea ('||' hyperItem)+
	;

hyperItem
	:	name hyperItem ('|' hyperItem)+
	|	hyperItemAtom
	;

hyperItemAtom
	:	name groupedQuery coef?
	;

name
	:	DollarQuotedString ':'
	;

coef
	:	'*' (Float | Percent)
	;

subtotal
	:	SubtotalMark=('`' | '``')
		SubtotalFields=('t' | 's' | 'c' | 'r' | 'd' | 'w' | 'm' | 'f' | 'b' | 'y')*
		AggregationMethod=('D' | 'x' | 'X')?
	;
	
voucherDetailQuery
	:	vouchers emit
	|	voucherQuery
	;

emit
	:	Op='A' | ':' details
	;

vouchers
	:	vouchersB
	|	voucherQuery
	;

vouchersB
	:	vouchersB Op='*' vouchersB
	|	vouchersB Op=('+' | '-') vouchersB
	|	Op=('+' | '-') vouchersB
	|	'{' voucherQuery '}'
	|	'{' vouchersB '}'
	;

voucherQuery
	:	details? Op=('A' | 'E')? range? DollarQuotedString? PercentQuotedString? VoucherType?
	;

details
	:	details Op='*' details
	|	details Op=('+' | '-') details
	|	Op=('+' | '-') details
	|	detailQuery
	|	'(' details ')'
	;

detailQuery
	:	(DetailTitle | DetailTitleSubTitle)? SingleQuotedString? DoubleQuotedString? Direction=('d' | 'c')?
	;

range
	:	'[]' | Core=rangeCore | '[' Core=rangeCore ']'
	;

rangeCore
	:	RangeNull | RangeAllNotNull
	|	Begin=rangeCertainPoint ('~'|'-') End=rangeCertainPoint?
	|	Op=('~'|'-') End=rangeCertainPoint
	|	Certain=rangeCertainPoint
	;
	
rangePoint
	:	RangeNull | All='[]'
	|	rangeCertainPoint
	;

rangeCertainPoint
	:	rangeYear
	|	rangeMonth
	|	rangeWeek
	|	rangeDay
	;

rangeYear
	:	RangeAYear
	;
	
rangeMonth
	:	Modifier=('@'|'#')? (RangeAMonth | RangeDeltaMonth)
	;

rangeWeek
	:	RangeDeltaWeek
	;

rangeDay
	:	RangeADay | RangeDeltaDay
	;
	
asset
	:	assetList | assetQuery | assetRegister | assetUnregister | assetResetSoft | assetResetHard | assetApply | assetCheck
	;
assetList
	:	'a' (AOAll | AOList)? rangePoint? aoQ?
	;
assetQuery
	:	'a' AOQuery aoQ?
	;
assetRegister
	:	'a' AORegister aoQ? (':' voucherDetailQuery)?
	;
assetUnregister
	:	'a' AOUnregister aoQ? range?
	;
assetRedep
	:	'a' ARedep aoQ?
	;
assetResetSoft
	:	'a' AOResetSoft aoQ? range?
	;
assetResetHard
	:	'a' AOResetHard aoQ? (':' voucherDetailQuery)?
	;
assetApply
	:	'a' AOApply AOCollapse? aoQ? range?
	;
assetCheck
	:	'a' AOCheck aoQ?
	;
amort
	:	amortList | amortQuery | amortRegister | amortUnregister | amortResetSoft | amortResetHard | amortApply | amortCheck
	;
amortList
	:	'o' (AOAll | AOList)? rangePoint? aoQ?
	;
amortQuery
	:	'o' AOQuery aoQ?
	;
amortRegister
	:	'o' AORegister aoQ? (':' voucherDetailQuery)?
	;
amortUnregister
	:	'o' AOUnregister aoQ? range?
	;
amortReamo
	:	'o' OReamo aoQ?
	;
amortResetSoft
	:	'o' AOResetSoft aoQ? range?
	;
amortResetHard
	:	'o' AOResetHard aoQ? (':' voucherDetailQuery)?
	;
amortApply
	:	'o' AOApply AOCollapse? aoQ? range?
	;
amortCheck
	:	'o' AOCheck aoQ?
	;

aoQ
	:	aoQAtom ('+' aoQAtom)*
	;

aoQAtom
	:	SingleQuotedString | DoubleQuotedString | Guid
	;
	
/*
 * Lexer Rules
 */
 
Launch
	:	'lan'
	|	'launch'
	;
Connect
	:	'con'
	|	'connect'
	;
Shutdown
	:	'shu'
	|	'shutdown'
	;
Backup
	:	'backup'
	;
Mobile
	:	'mob'
	|	'mobile'
	;
Fetch
	:	'fetch'
	;
Help
	:	'help'
	|	'?'
	;
Titles
	:	'titles'
	|	'T' | 't'
	;
Exit
	:	'exit'
	;
Check
	:	'chk' [1-2]
	;
	
AOAll
	:	'-all'
	;
AOList
	:	'-li'
	|	'-list'
	;
AOQuery
	:	'-q'
	|	'-query'
	;
AORegister
	:	'-reg'
	|	'-register'
	;
AOUnregister
	:	'-unr'
	|	'-unregister'
	;
ARedep
	:	'-rd'
	|	'-redep'
	;
OReamo
	:	'-ra'
	|	'-reamo'
	;
AOResetSoft
	:	'-reset-soft'
	;
AOResetHard
	:	'-reset-hard'
	;
AOApply
	:	'-ap'
	|	'-apply'
	;
AOCollapse
	:	'-co'
	|	'-collapse'
	;
AOCheck
	:	'-chk'
	;

Guid
	:	H H H H H H H H '-' H H H H '-' H H H H '-' H H H H '-' H H H H H H H H H H H H
	;

fragment H
	:	[0-9A-Za-z] 
	;

RangeNull
	:	'null'
	;
RangeAllNotNull
	:	'~null'
	;

RangeAYear
	:	[1-2] [0-9] [0-9] [0-9]
	;
RangeAMonth
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeDeltaMonth
	:	'0'
	|	'-' [1-9] [0-9]*
	;
RangeADay
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
	;
RangeDeltaDay
	:	'.'+
	;
RangeDeltaWeek
	:	','+
	;

VoucherType
	:	'Ordinal' | 'Carry' | 'Amortization' | 'Depreciation' | 'Devalue' | 'AnnualCarry' | 'Uncertain'
	;

PercentQuotedString
	:	'%' ('%%'|~('%'))* '%'
	;

DollarQuotedString
	:	'$' ('$$'|~('$'))* '$'
	;
	
DoubleQuotedString
	:	'"' ('""'|~('"'))* '"'
	;

SingleQuotedString
	:	'\'' ('\'\''|~('\''))* '\''
	;

DetailTitle
	:	'T' [1-9] [0-9] [0-9] [0-9]
	;

DetailTitleSubTitle
	:	'T' [1-9] [0-9] [0-9] [0-9] [0-9] [0-9]
	;

Float
	:	'F' ('+' | '-')? [0-9]+ '.'? [0-9]*
	|	'F' ('+' | '-')? '.' [0-9]+
	;

Percent
	:	'P' ('+' | '-')? [0-9]+ '.'? [0-9]* '%'
	|	'P' ('+' | '-')? '.' [0-9]+ '%'
	;
	
Intersect
	: '*'
	;
Union
	: '+'
	;
Substract
	: '-'
	;

WS
	:	' ' -> channel(HIDDEN)
	;
