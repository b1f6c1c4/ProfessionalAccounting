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
	:	'c::' DollarQuotedString
	|	'c||' chartArea ('||' hyperItem)+
	|	'c:' DollarQuotedString ':' chartArea ('||' hyperItem)+
	;

chartArea
	:	hyperItemAtom ('|' hyperItemAtom)* '|' ('_' Float)? ('^' Float)?
	;

report
	:	'r::' DollarQuotedString
	|	'r||' hyperItem
	|	'r:' DollarQuotedString ':' chartArea ('||' hyperItem)+
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
		(Aggregation='D' (Ranged='[' rangeCore? ']')?)?
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
	:	assetList | assetQuery | assetRegister | assetUnregister | assetRedep | assetResetSoft | assetResetHard | assetApply | assetCheck
	;
assetList
	:	'a' (AOAll | AOList)? rangePoint? distributedQ?
	;
assetQuery
	:	'a' AOQuery distributedQ?
	;
assetRegister
	:	'a' AORegister distributedQ? range? (':' vouchers)?
	;
assetUnregister
	:	'a' AOUnregister distributedQ? range? (':' vouchers)?
	;
assetRedep
	:	'a' ARedep distributedQ?
	;
assetResetSoft
	:	'a' AOResetSoft distributedQ? range?
	;
assetResetHard
	:	'a' AOResetHard distributedQ? (':' vouchers)?
	;
assetApply
	:	'a' AOApply AOCollapse? distributedQ? range?
	;
assetCheck
	:	'a' AOCheck distributedQ?
	;
amort
	:	amortList | amortQuery | amortRegister | amortUnregister | amortReamo | amortResetSoft | amortApply | amortCheck
	;
amortList
	:	'o' (AOAll | AOList)? rangePoint? distributedQ?
	;
amortQuery
	:	'o' AOQuery distributedQ?
	;
amortRegister
	:	'o' AORegister distributedQ? range? (':' vouchers)?
	;
amortUnregister
	:	'o' AOUnregister distributedQ? range? (':' vouchers)?
	;
amortReamo
	:	'o' OReamo distributedQ?
	;
amortResetSoft
	:	'o' AOResetSoft distributedQ? range?
	;
amortApply
	:	'o' AOApply AOCollapse? distributedQ? range?
	;
amortCheck
	:	'o' AOCheck distributedQ?
	;

distributedQ
	:	distributedQ Op='*' distributedQ
	|	distributedQ Op=('+' | '-') distributedQ
	|	Op=('+' | '-') distributedQ
	|	distributedQAtom
	|	'(' distributedQ ')'
	;
distributedQAtom
	:	Guid? DollarQuotedString? PercentQuotedString? ('[[' rangeCore ']]')?
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
